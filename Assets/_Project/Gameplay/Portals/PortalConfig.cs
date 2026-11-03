using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Portals
{
    /// <summary>Tunables for see-through thresholds. Asset: Config/PortalConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Portal Config", fileName = "PortalConfig")]
    public sealed class PortalConfig : ConfigAsset
    {
        [Tooltip("Portals further than this from the main camera (m) are not rendered.")]
        public float MaxDistance = 45f;

        [Tooltip("At most this many portal views render per frame (nearest first).")]
        public int MaxActive = 4;

        [Tooltip("When the camera is this close to a portal plane (m), the surface extrudes behind the plane so a crossing never shows the real room.")]
        public float NearExtrude = 0.6f;

        [Tooltip("Render texture size as a fraction of the screen.")]
        public float ResolutionScale = 1f;

        [Tooltip("Material on the portal surface (Kit/Portal shader). Assigned by the Portals builder.")]
        public Material SurfaceMaterial;

        protected override void Sanitize()
        {
            MaxDistance = Mathf.Max(1f, MaxDistance);
            MaxActive = Mathf.Clamp(MaxActive, 0, 16);
            NearExtrude = Mathf.Max(0f, NearExtrude);
            ResolutionScale = Mathf.Clamp(ResolutionScale, 0.25f, 1f);
        }
    }
}
