using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Signage
{
    /// <summary>
    /// Font, per-style materials and resolution for WorldText. Asset: Config/WorldTextConfig.asset.
    /// The two scale constants (WorldUnitsPerFontPixel, CapHeightPerEm) turn a requested capital
    /// height in metres into TextMesh fontSize/characterSize; tune CapHeightPerEm after a
    /// screenshot if signs read too big or small.
    /// </summary>
    [CreateAssetMenu(menuName = "Kit/Config/World Text Config", fileName = "WorldTextConfig")]
    public sealed class WorldTextConfig : ConfigAsset
    {
        [Tooltip("Font for all world text (LegacyRuntime.ttf by default).")]
        public Font Font;

        [Tooltip("Materials by WorldTextStyle order: DarkOnLight, LightOnDark, Brass, Glow. Assigned by the Signage builder.")]
        public Material[] StyleMaterials = new Material[4];

        [Tooltip("Font texture pixels per metre of letter height; higher is crisper and costs atlas space.")]
        public float PixelsPerMetre = 600f;

        [Tooltip("Smallest and largest TextMesh fontSize (atlas pixels per em) WorldText will request.")]
        public int MinFontSize = 16;
        public int MaxFontSize = 160;

        [Tooltip("TextMesh world units per font pixel at characterSize 1 and scale 1 (Unity's TextMesh draws an em of fontSize pixels as fontSize/10 units).")]
        public float WorldUnitsPerFontPixel = 0.1f;

        [Tooltip("Capital letter height as a fraction of the em (about 0.72 for Arial/Liberation Sans, the LegacyRuntime font).")]
        public float CapHeightPerEm = 0.72f;

        [Tooltip("TextMesh line spacing for multi-line text (1 = the font's natural line height).")]
        public float LineSpacing = 1f;

        protected override void Sanitize()
        {
            PixelsPerMetre = Mathf.Clamp(PixelsPerMetre, 50f, 4000f);
            MinFontSize = Mathf.Clamp(MinFontSize, 4, 512);
            MaxFontSize = Mathf.Clamp(MaxFontSize, MinFontSize, 512);
            WorldUnitsPerFontPixel = Mathf.Max(0.0001f, WorldUnitsPerFontPixel);
            CapHeightPerEm = Mathf.Clamp(CapHeightPerEm, 0.2f, 1.5f);
            LineSpacing = Mathf.Clamp(LineSpacing, 0.5f, 3f);
            if (StyleMaterials == null || StyleMaterials.Length != 4) System.Array.Resize(ref StyleMaterials, 4);
        }
    }
}
