// Fake volumetric light shaft (skylights, window beams) for URP, after inside_02's skylight in fog.
// The mesh (built by LightShaft.cs) is a closed frustum: top rectangle at object y = 0, widening to
// the bottom at y = -length. Only its back faces are drawn, with ZTest Always, so every pixel the
// shaft covers is shaded exactly once, from outside or inside. Each pixel marches its view ray
// through the frustum (clipped by the depth texture, so floors and walls cut it correctly) and adds
// up a soft density: fading toward the sides, toward the top and bottom, near the camera (no wall of
// light when you stand in it) and with scene fog, and brighter when you look up toward the light.
// Additive (Blend One One), ZWrite Off. Shape, tint and the LookConfig numbers come per renderer
// through a MaterialPropertyBlock.
Shader "Kit/LightShaft"
{
    Properties
    {
        _Tint ("Tint (alpha = strength)", Color) = (0.8, 0.86, 0.95, 0.25)
        _ShaftShape ("Shape (half width, length, half depth, bottom widen)", Vector) = (1.5, 12, 1.5, 1.18)
        _Intensity ("Intensity", Float) = 1
        _EdgeSoftness ("Edge Softness", Range(0.05, 1)) = 0.7
        _BottomBrightness ("Bottom Brightness", Range(0, 1)) = 0.35
        _TopFade ("Top Fade (fraction of length)", Range(0.001, 0.5)) = 0.06
        _BottomFade ("Bottom Fade (fraction of length)", Range(0.001, 0.5)) = 0.12
        _NearFadeStart ("Camera Fade Start (m)", Float) = 0.5
        _NearFadeEnd ("Camera Fade End (m)", Float) = 5
        _FogFade ("Fog Fade", Range(0, 1)) = 0.7
        _ForwardScatter ("Forward Scatter", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "DisableBatching" = "True" }

        Pass
        {
            Name "LightShaft"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

            #define SHAFT_STEPS 10

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float4 _ShaftShape;
                float _Intensity;
                float _EdgeSoftness;
                float _BottomBrightness;
                float _TopFade;
                float _BottomFade;
                float _NearFadeStart;
                float _NearFadeEnd;
                float _FogFade;
                float _ForwardScatter;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float eyeDepth : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.eyeDepth = -p.positionVS.z;
                return o;
            }

            // Clips the ray o + t*d against the half-space dot(n, p) <= dist (Cyrus-Beck).
            void ClipPlane(float3 o, float3 d, float3 n, float dist, inout float t0, inout float t1)
            {
                float denom = dot(n, d);
                float num = dist - dot(n, o);
                if (abs(denom) < 1e-6)
                {
                    if (num < 0) t1 = -1;
                }
                else if (denom > 0) t1 = min(t1, num / denom);
                else t0 = max(t0, num / denom);
            }

            // Fraction of light that survives the scene fog at this eye depth (1 when fog is off).
            half FogVisibility(float eyeDepth)
            {
                return MixFogColor(half3(1, 1, 1), half3(0, 0, 0), ComputeFogFactorZ0ToFar(eyeDepth)).r;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float hx = _ShaftShape.x, len = max(_ShaftShape.y, 0.001), hz = _ShaftShape.z, widen = _ShaftShape.w;
                float3 camWS = GetCameraPositionWS();

                // The view ray in object space, parameterised so t = 1 is this back-face pixel.
                float3 o = TransformWorldToObject(camWS);
                float3 d = TransformWorldToObject(i.positionWS) - o;
                float rayLength = distance(i.positionWS, camWS);

                // Stop at the first opaque surface: eye depth grows linearly along the ray.
                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float t0 = 0, t1 = min(1e5, sceneEye / max(i.eyeDepth, 1e-4));

                // Frustum = six half-spaces. The sides lean out by k per metre of depth.
                float kx = hx * (widen - 1) / len, kz = hz * (widen - 1) / len;
                ClipPlane(o, d, float3(0, 1, 0), 0, t0, t1);
                ClipPlane(o, d, float3(0, -1, 0), len, t0, t1);
                ClipPlane(o, d, float3(1, kx, 0), hx, t0, t1);
                ClipPlane(o, d, float3(-1, kx, 0), hx, t0, t1);
                ClipPlane(o, d, float3(0, kz, 1), hz, t0, t1);
                ClipPlane(o, d, float3(0, kz, -1), hz, t0, t1);
                if (t1 <= t0) return 0;

                // Looking up the shaft toward its light reads brighter (forward scattering).
                float3 rayDirWS = (i.positionWS - camWS) / max(rayLength, 1e-4);
                float3 shaftUpWS = normalize(TransformObjectToWorldDir(float3(0, 1, 0)));
                float towardLight = saturate(dot(rayDirWS, shaftUpWS));
                half phase = 1 + _ForwardScatter * towardLight * towardLight * towardLight * towardLight;

                // March with a per-pixel jitter, so ten steps don't band.
                float jitter = InterleavedGradientNoise(i.positionCS.xy, 0);
                float dt = (t1 - t0) / SHAFT_STEPS;
                float soft = max(_EdgeSoftness, 0.001);
                float nearRange = max(_NearFadeEnd - _NearFadeStart, 0.001);
                half density = 0;
                [unroll]
                for (int s = 0; s < SHAFT_STEPS; s++)
                {
                    float t = t0 + (s + jitter) * dt;
                    float3 q = o + d * t;
                    float h = saturate(-q.y / len);                 // 0 at the top, 1 at the bottom
                    float scale = 1 + (widen - 1) * h;
                    float nx = abs(q.x) / max(hx * scale, 1e-4);
                    float nz = abs(q.z) / max(hz * scale, 1e-4);
                    half across = smoothstep(1, 1 - soft, nx) * smoothstep(1, 1 - soft, nz);
                    half along = lerp(1, _BottomBrightness, h) * smoothstep(0, _TopFade, h) * smoothstep(1, 1 - _BottomFade, h);
                    float dist = t * rayLength;
                    half nearFade = saturate((dist - _NearFadeStart) / nearRange);
                    half fog = lerp(1, FogVisibility(t * i.eyeDepth), _FogFade);
                    density += across * along * nearFade * fog;
                }

                // Path length in metres, relative to the shaft's width: crossing the middle of the
                // shaft sideways scores about 1. 1 - exp(-x) keeps long looks up the shaft from blowing out.
                float pathMetres = density * dt * rayLength;
                float optical = pathMetres / max(hx + hz, 0.01);
                half glow = (1 - exp(-optical)) * phase;
                return half4(_Tint.rgb * (_Tint.a * _Intensity * glow), 0);
            }
            ENDHLSL
        }
    }
}
