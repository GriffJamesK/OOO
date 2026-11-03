// See-through threshold surface (Portals system). Shows the partner opening's side, rendered by
// the view's own camera into _PortalTex, sampled in screen space so the picture lines up with
// the opening from any angle. Unlit and fog-free: the portal camera already lit and fogged that
// view from the right distance, and the main camera grades the whole frame once. Cull Off, so
// the opening works from both sides (and from inside the near-plane box). Writes depth like any
// opaque, with DepthOnly/DepthNormals passes so SSAO and depth priming see a flat opening, not
// the room behind it. _FillAmount 1 shows _FillColor instead (a view that is not rendering).
Shader "Kit/Portal"
{
    Properties
    {
        [NoScaleOffset] _PortalTex ("Portal Texture (set per view)", 2D) = "black" {}
        _FillColor ("Fill Colour", Color) = (0.165, 0.196, 0.235, 1)
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    TEXTURE2D(_PortalTex);
    SAMPLER(sampler_PortalTex);

    CBUFFER_START(UnityPerMaterial)
        half4 _FillColor;
        half _FillAmount;
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
            Name "PortalUnlit"
            // ForwardOnly: drawn in the forward pass whichever rendering path the renderer uses.
            Tags { "LightMode" = "UniversalForwardOnly" }
            ZWrite On ZTest LEqual Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                // positionNDC is URP's replacement for the deprecated ComputeScreenPos (same maths,
                // platform y-flip included): xy / w is 0..1 across the screen.
                o.screenPos = p.positionNDC;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                half3 view = SAMPLE_TEXTURE2D(_PortalTex, sampler_PortalTex, uv).rgb;
                return half4(lerp(view, _FillColor.rgb, _FillAmount), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(Attributes input) : SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }
            ZWrite On Cull Off

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

            // Seen from behind, the face normal points away from the camera; flip it so SSAO reads a
            // surface facing the viewer from either side.
            half4 frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float3 n = NormalizeNormalPerPixel(i.normalWS);
                return half4(IS_FRONT_VFACE(face, n, -n), 0.0);
            }
            ENDHLSL
        }
    }
}
