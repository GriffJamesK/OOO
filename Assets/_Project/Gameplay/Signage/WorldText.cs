using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Gameplay.Signage
{
    /// <summary>How a piece of diegetic text is drawn; each maps to a material in WorldTextConfig.</summary>
    public enum WorldTextStyle
    {
        DarkOnLight,
        LightOnDark,
        Brass,
        Glow
    }

    /// <summary>
    /// Diegetic text in the world: signage, nameplates, memos, whiteboards, plate numbers. A
    /// TextMesh with a fog- and light-aware material (Kit/WorldText), so words sit in the room
    /// instead of glowing through the fog like UI. Works in edit mode, so builders create and set
    /// it directly.
    /// <para>
    /// Orientation: like every TextMesh, the text reads correctly when viewed from the transform's
    /// -Z side (looking along +Z). OfficeKit.Sign therefore yaws the text 180 deg so it faces the
    /// sign's forward. Keep that convention when placing text by hand.
    /// </para>
    /// <para>
    /// Size: <see cref="LetterHeight"/> is the height of a capital letter in local metres; fontSize
    /// and characterSize are derived from it (see <see cref="ComputeSizing"/>), so the atlas stays
    /// crisp at any size. The font atlas is bound per renderer with a MaterialPropertyBlock, so the
    /// shared style materials are never modified, and re-bound whenever the dynamic atlas rebuilds.
    /// </para>
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldText : MonoBehaviour
    {
        [SerializeField, TextArea] string text = "";
        [Tooltip("Height of a capital letter in metres.")]
        [SerializeField] float letterHeight = 0.1f;
        [SerializeField] WorldTextStyle style = WorldTextStyle.DarkOnLight;
        [SerializeField] TextAnchor anchor = TextAnchor.MiddleCenter;

        static readonly List<WorldText> live = new List<WorldText>();
        static bool subscribed;
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static MaterialPropertyBlock block;

        TextMesh textMesh;
        MeshRenderer meshRenderer;

        public string Text => text;
        public float LetterHeight => letterHeight;
        public WorldTextStyle Style => style;
        public TextAnchor Anchor => anchor;

        /// <summary>Every enabled WorldText (edit and play mode); used to re-bind atlases and by tests/debug.</summary>
        public static IReadOnlyList<WorldText> Live => live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            live.Clear();
            Font.textureRebuilt -= OnFontTextureRebuilt;
            subscribed = false;
            block = null;
        }

        void OnEnable()
        {
            if (!live.Contains(this)) live.Add(this);
            if (!subscribed)
            {
                Font.textureRebuilt -= OnFontTextureRebuilt;
                Font.textureRebuilt += OnFontTextureRebuilt;
                subscribed = true;
            }
            // Property blocks aren't serialized, so a freshly loaded sign needs its atlas bound again.
            Apply();
        }

        void OnDisable()
        {
            live.Remove(this);
            if (live.Count == 0 && subscribed)
            {
                Font.textureRebuilt -= OnFontTextureRebuilt;
                subscribed = false;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector edits re-apply on the next editor tick: adding components or touching renderers
        /// inside OnValidate itself makes Unity log SendMessage warnings.
        /// </summary>
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Apply(); };
        }
#endif

        /// <summary>Sets content and look; rebuilds the TextMesh and material.</summary>
        public void Set(string content, float height, WorldTextStyle textStyle, TextAnchor textAnchor = TextAnchor.MiddleCenter)
        {
            text = content ?? "";
            letterHeight = Mathf.Max(0.005f, height);
            style = textStyle;
            anchor = textAnchor;
            Apply();
        }

        /// <summary>Creates a child text object (edit or play mode).</summary>
        public static WorldText Create(Transform parent, string name, string content, Vector3 localPosition, Quaternion localRotation,
            float height, WorldTextStyle textStyle, TextAnchor textAnchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            var wt = go.AddComponent<WorldText>();
            wt.Set(content, height, textStyle, textAnchor);
            return wt;
        }

        /// <summary>
        /// TextMesh sizing for a capital height of <paramref name="capHeight"/> metres. fontSize is
        /// the atlas resolution (pixels per em), chosen from PixelsPerMetre so small text stays
        /// cheap and big text stays sharp; characterSize then scales the em so that
        /// capHeight = CapHeightPerEm * characterSize * fontSize * WorldUnitsPerFontPixel.
        /// </summary>
        public static void ComputeSizing(WorldTextConfig cfg, float capHeight, out int fontSize, out float characterSize)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(capHeight * cfg.PixelsPerMetre), cfg.MinFontSize, cfg.MaxFontSize);
            characterSize = capHeight / (cfg.CapHeightPerEm * fontSize * cfg.WorldUnitsPerFontPixel);
        }

        /// <summary>The capital height the given sizing produces, by the same model (the inverse of ComputeSizing).</summary>
        public static float CapHeightFor(WorldTextConfig cfg, int fontSize, float characterSize)
            => cfg.CapHeightPerEm * characterSize * fontSize * cfg.WorldUnitsPerFontPixel;

        /// <summary>
        /// The live config. Outside play mode ConfigLookup may only hold a code-made default (no
        /// GameManager has registered assets), so in the editor the real Config asset is loaded
        /// instead; builders and edit-mode tests then get the built materials.
        /// </summary>
        public static WorldTextConfig ResolveConfig()
        {
            var cfg = ConfigLookup.Get<WorldTextConfig>();
#if UNITY_EDITOR
            if (cfg != null && !UnityEditor.EditorUtility.IsPersistent(cfg))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldTextConfig>("Assets/_Project/Config/WorldTextConfig.asset");
                if (asset != null) cfg = asset;
            }
#endif
            return cfg;
        }

        /// <summary>The configured font, or Unity's built-in LegacyRuntime font when none is set.</summary>
        static Font ResolveFont(WorldTextConfig cfg)
        {
            if (cfg != null && cfg.Font != null) return cfg.Font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>The style's material, or null when the config has none (the caller then falls back to the font's own material).</summary>
        static Material StyleMaterial(WorldTextConfig cfg, WorldTextStyle s)
        {
            if (cfg == null || cfg.StyleMaterials == null) return null;
            int i = (int)s;
            return i >= 0 && i < cfg.StyleMaterials.Length ? cfg.StyleMaterials[i] : null;
        }

        /// <summary>Makes sure the TextMesh and MeshRenderer exist, then pushes text, size, font and material onto them.</summary>
        void Apply()
        {
            if (textMesh == null && !TryGetComponent(out textMesh)) textMesh = gameObject.AddComponent<TextMesh>();
            if (meshRenderer == null && !TryGetComponent(out meshRenderer)) meshRenderer = gameObject.AddComponent<MeshRenderer>();

            var cfg = ResolveConfig();
            var font = ResolveFont(cfg);

            textMesh.font = font;
            textMesh.text = text;
            textMesh.anchor = anchor;
            textMesh.alignment = TextAlignment.Center;
            textMesh.richText = false;
            textMesh.color = Color.white; // colour comes from the style material
            textMesh.lineSpacing = cfg != null ? cfg.LineSpacing : 1f;
            if (cfg != null)
            {
                ComputeSizing(cfg, letterHeight, out int size, out float charSize);
                textMesh.fontSize = size;
                textMesh.characterSize = charSize;
            }
            else
            {
                textMesh.fontSize = 64;
                textMesh.characterSize = letterHeight / (0.72f * 64 * 0.1f);
            }

            meshRenderer.shadowCastingMode = ShadowCastingMode.Off; // text casts no shadow
            meshRenderer.receiveShadows = true;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var mat = StyleMaterial(cfg, style);
            if (mat != null)
            {
                meshRenderer.sharedMaterial = mat;
                BindAtlas();
            }
            else
            {
                // Fallback: the font's own material already carries its atlas, so no property block.
                meshRenderer.sharedMaterial = font != null ? font.material : null;
                meshRenderer.SetPropertyBlock(null);
            }
        }

        /// <summary>Points this renderer's _MainTex at the font's current atlas without touching the shared material.</summary>
        void BindAtlas()
        {
            if (meshRenderer == null || textMesh == null || textMesh.font == null) return;
            var atlas = textMesh.font.material != null ? textMesh.font.material.mainTexture : null;
            if (atlas == null) return;
            block ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            block.SetTexture(MainTexId, atlas);
            meshRenderer.SetPropertyBlock(block);
        }

        /// <summary>The dynamic atlas was regenerated (new glyphs or sizes): every text using that font re-binds it.</summary>
        static void OnFontTextureRebuilt(Font font)
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var wt = live[i];
                if (wt == null) { live.RemoveAt(i); continue; }
                if (wt.textMesh != null && wt.textMesh.font == font && wt.meshRenderer != null
                    && wt.meshRenderer.sharedMaterial != font.material)
                    wt.BindAtlas();
            }
        }

        /// <summary>The TextMesh this component drives (created on first Apply).</summary>
        public TextMesh Mesh => textMesh;

        /// <summary>The renderer this component drives (created on first Apply).</summary>
        public MeshRenderer Renderer => meshRenderer;
    }
}
