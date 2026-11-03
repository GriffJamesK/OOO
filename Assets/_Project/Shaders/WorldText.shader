// Diegetic world text (signage, nameplates, memos, plate numbers) drawn from a TextMesh's dynamic
// font atlas. Unlike the built-in GUI/Text shader it is alpha-tested and opaque-queued, lit by the
// main light (with shadows), additional lights (Forward and Forward+/cluster loop) and ambient SH,
// and fogged, so words sit in the room instead of glowing through the fog like UI. _EmissionColor
// is added on top for lit signs and CRT screens. The font atlas arrives per renderer through a
// MaterialPropertyBlock (_MainTex) from WorldText, so the shared style materials are never edited.
// TextMesh meshes carry no normals, so the normal is the quad's -Z (its reading side), flipped to
// face the viewer because the text is two-sided (Cull Off).
Shader "Kit/WorldText"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font Atlas", 2D) = "white" {}
        _Color ("Color", Color) = (0.11, 0.11, 0.12, 1)
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
        _Specular ("Specular Strength", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_MainTex);
    SAMPLER(sampler_MainTex);

    CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        half4 _Color;
        half4 _EmissionColor;
        half _Specular;
        half _Smoothness;
        half _Cutoff;
    CBUFFER_END

    struct Attributes
    {
        float4 positionOS : POSITION;
        float2 uv : TEXCOORD0;
        half4 color : COLOR;
    };

    // Glyph coverage times the material and TextMesh vertex alpha; below the cutoff the pixel is discarded.
    half GlyphAlpha(float2 uv, half vertexAlpha)
    {
        half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * _Color.a * vertexAlpha;
        clip(a - _Cutoff);
        return a;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" "IgnoreProjector" = "True" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldDir(float3(0, 0, -1)); // TextMesh reads from its -Z side
                o.uv = input.uv;
                o.color = input.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            // Lambert diffuse plus an optional Blinn-Phong highlight (brass) for one light.
            half3 Shade(Light light, half3 n, half3 v, half3 albedo, half specPower)
            {
                half atten = light.distanceAttenuation * light.shadowAttenuation;
                half ndl = saturate(dot(n, light.direction));
                half3 h = SafeNormalize(light.direction + v);
                half spec = _Specular * pow(saturate(dot(n, h)), specPower) * step(0.0001, ndl);
                return light.color * atten * (albedo * ndl + spec);
            }

            half4 frag(Varyings i) : SV_Target
            {
                half alpha = GlyphAlpha(i.uv, i.color.a);

                half3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 n = normalize(i.normalWS);
                n = dot(n, v) < 0 ? -n : n; // two-sided: light the side we're looking at
                half3 albedo = _Color.rgb * i.color.rgb;
                half specPower = exp2(10.0 * _Smoothness + 1.0);

                half3 color = albedo * SampleSH(n);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                color += Shade(mainLight, n, v, albedo, specPower);

            #if defined(_ADDITIONAL_LIGHTS)
                // LIGHT_LOOP_BEGIN reads inputData.positionWS / normalizedScreenSpaceUV for the cluster lookup.
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                half4 shadowMask = half4(1, 1, 1, 1);

                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                {
                    color += Shade(GetAdditionalLight(dirIndex, i.positionWS, shadowMask), n, v, albedo, specPower);
                }
                #endif

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    color += Shade(GetAdditionalLight(lightIndex, i.positionWS, shadowMask), n, v, albedo, specPower);
                LIGHT_LOOP_END
            #endif

                color += _EmissionColor.rgb;
                return half4(MixFog(color, i.fogFactor), alpha);
            }
            ENDHLSL
        }

        // Depth passes so the text survives depth priming (ZTest Equal in the forward pass) and
        // shows up in the depth/normals textures SSAO reads, cut out exactly like the colour pass.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            half frag(Varyings i) : SV_Target
            {
                GlyphAlpha(i.uv, i.color.a);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldDir(float3(0, 0, -1));
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                GlyphAlpha(i.uv, i.color.a);
                float3 n = normalize(i.normalWS);
                n = dot(n, GetWorldSpaceViewDir(i.positionWS)) < 0 ? -n : n;
                return half4(n, 0.0);
            }
            ENDHLSL
        }
    }
}
