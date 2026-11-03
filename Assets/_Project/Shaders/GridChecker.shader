// World-space scale-reference surface: one cell per _CellSize metres regardless of mesh UVs or
// scale, projected on the dominant axis so floors, walls and ceilings all read correctly.
// _Mode 0 = checker (ground default), 1 = subtle grid lines (walls). Lit by the main light with
// shadows and fog; casts shadows and writes depth/normals so SSAO works on greybox geometry.
Shader "Kit/GridChecker"
{
    Properties
    {
        _ColorA ("Color A / Base", Color) = (0.80, 0.78, 0.75, 1)
        _ColorB ("Color B / Lines", Color) = (0.72, 0.70, 0.67, 1)
        _CellSize ("Cell Size (m)", Float) = 1
        [Enum(Checker,0,Lines,1)] _Mode ("Mode", Float) = 0
        _LineWidth ("Line Width (m)", Range(0.001, 0.2)) = 0.02
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        half4 _ColorA;
        half4 _ColorB;
        float _CellSize;
        float _Mode;
        float _LineWidth;
    CBUFFER_END

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
    };
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // Projects world position onto the plane facing the dominant normal axis.
            float2 PlanarCoords(float3 positionWS, float3 n)
            {
                float3 a = abs(n);
                if (a.y >= a.x && a.y >= a.z) return positionWS.xz;
                if (a.x >= a.z) return positionWS.zy;
                return positionWS.xy;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                float2 coords = PlanarCoords(i.positionWS, n) / max(_CellSize, 0.001);

                float2 cell = floor(coords);
                half checker = fmod(abs(cell.x + cell.y), 2.0);

                float2 distToLine = abs(frac(coords + 0.5) - 0.5) * max(_CellSize, 0.001);
                float2 aa = fwidth(coords) * max(_CellSize, 0.001);
                float2 lineMask = 1.0 - smoothstep(_LineWidth * 0.5 - aa, _LineWidth * 0.5 + aa, distToLine);
                half lines = max(lineMask.x, lineMask.y);

                half blend = _Mode < 0.5 ? checker : lines;
                half3 albedo = lerp(_ColorA.rgb, _ColorB.rgb, blend);

                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(screenUV);
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, light.direction));
                half3 color = albedo * (SampleSH(n) * ao.indirectAmbientOcclusion
                                        + light.color * ndl * light.shadowAttenuation * ao.directAmbientOcclusion);
                return half4(MixFog(color, i.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            float4 vert(Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirection = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(Attributes input) : SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(i.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
