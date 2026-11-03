using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Gameplay.Feel
{
    /// <summary>
    /// The particle material recipe, shared by the editor builder (which saves Mat_Particle_Dust)
    /// and the runtime stand-in used when no material is assigned (isolated tests). URP's material
    /// inspector normally derives blend states and keywords from the surface options; a material
    /// made from code never passes through that inspector, so this sets them explicitly.
    /// </summary>
    public static class FeelMaterials
    {
        public const string ShaderName = "Universal Render Pipeline/Particles/Unlit";

        static Material fallbackMaterial;
        static Texture2D fallbackTexture;

        /// <summary>Domain reload is off: drop the last session's stand-in so it is rebuilt fresh.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (fallbackMaterial != null) Object.DestroyImmediate(fallbackMaterial);
            if (fallbackTexture != null) Object.DestroyImmediate(fallbackTexture);
            fallbackMaterial = null;
            fallbackTexture = null;
        }

        /// <summary>
        /// Makes <paramref name="m"/> an alpha-blended, soft, unlit particle material on
        /// <paramref name="texture"/>: transparent queue, no depth write, soft particles (fades where
        /// it meets the floor instead of cutting a hard line) and camera fading (dust right in front
        /// of the lens thins out instead of filling the view). Colour comes from the particles.
        /// </summary>
        public static void ConfigureDust(Material m, Texture texture)
        {
            if (m == null) return;
            m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", Color.white);

            m.SetFloat("_Surface", 1f);   // transparent
            m.SetFloat("_Blend", 0f);     // alpha
            m.SetFloat("_AlphaClip", 0f);
            m.SetFloat("_ColorMode", 0f); // multiply: particle colour tints the white texture
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.SetFloat("_BlendOp", (float)BlendOp.Add);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHAMODULATE_ON");
            m.DisableKeyword("_COLOROVERLAY_ON");
            m.DisableKeyword("_COLORCOLOR_ON");
            m.DisableKeyword("_COLORADDSUBDIFF_ON");

            const float softNear = 0f, softFar = 0.6f;
            m.SetFloat("_SoftParticlesEnabled", 1f);
            m.SetFloat("_SoftParticlesNearFadeDistance", softNear);
            m.SetFloat("_SoftParticlesFarFadeDistance", softFar);
            m.SetVector("_SoftParticleFadeParams", new Vector4(softNear, 1f / (softFar - softNear), 0f, 0f));
            m.EnableKeyword("_SOFTPARTICLES_ON");

            const float camNear = 0.3f, camFar = 1.1f;
            m.SetFloat("_CameraFadingEnabled", 1f);
            m.SetFloat("_CameraNearFadeDistance", camNear);
            m.SetFloat("_CameraFarFadeDistance", camFar);
            m.SetVector("_CameraFadeParams", new Vector4(camNear, 1f / (camFar - camNear), 0f, 0f));
            m.EnableKeyword("_FADING_ON");
            m.enableInstancing = true;
        }

        /// <summary>
        /// Pixels of a soft, slightly irregular white dot (alpha only), <paramref name="size"/> square.
        /// Perlin noise breaks up the circle so a cloud of them reads as dust rather than as discs.
        /// Deterministic, so the builder's PNG never changes between builds.
        /// </summary>
        public static Color32[] SoftDotPixels(int size)
        {
            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half, dy = (y - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float noise = Mathf.PerlinNoise(x * 0.11f + 3.7f, y * 0.11f + 9.1f);
                    float edge = Mathf.Clamp01(1f - r * (0.9f + 0.25f * noise));
                    float a = edge * edge * (3f - 2f * edge); // smoothstep: no visible rim
                    a *= 0.8f + 0.2f * noise;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            return pixels;
        }

        /// <summary>
        /// The material to draw with: the configured one, else a runtime stand-in built once from the
        /// same recipe, else null (the caller hides its renderer rather than drawing magenta).
        /// </summary>
        public static Material Resolve(FeelConfig cfg)
        {
            if (cfg != null && cfg.DustMaterial != null) return cfg.DustMaterial;
            if (fallbackMaterial != null) return fallbackMaterial;

            var shader = Shader.Find(ShaderName);
            if (shader == null) return null;
            const int size = 32;
            fallbackTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Feel_FallbackDot",
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            fallbackTexture.SetPixels32(SoftDotPixels(size));
            fallbackTexture.Apply(false, true);
            fallbackMaterial = new Material(shader) { name = "Feel_FallbackDust", hideFlags = HideFlags.DontSave };
            ConfigureDust(fallbackMaterial, fallbackTexture);
            return fallbackMaterial;
        }
    }
}
