using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Signage
{
    /// <summary>Font, per-style materials and resolution for WorldText. Asset: Config/WorldTextConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/World Text Config", fileName = "WorldTextConfig")]
    public sealed class WorldTextConfig : ConfigAsset
    {
        [Tooltip("Font for all world text (LegacyRuntime.ttf by default).")]
        public Font Font;

        [Tooltip("Materials by WorldTextStyle order: DarkOnLight, LightOnDark, Brass, Glow. Assigned by the Signage builder.")]
        public Material[] StyleMaterials = new Material[4];

        [Tooltip("Font texture pixels per metre of letter height; higher is crisper and costs atlas space.")]
        public float PixelsPerMetre = 600f;

        protected override void Sanitize()
        {
            PixelsPerMetre = Mathf.Clamp(PixelsPerMetre, 50f, 4000f);
            if (StyleMaterials == null || StyleMaterials.Length != 4) System.Array.Resize(ref StyleMaterials, 4);
        }
    }
}
