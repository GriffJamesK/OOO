using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// Sits on the scene's global Volume and copies the profile's KitFog into RenderSettings each
    /// frame, so fog is part of the look profile like every other post effect. Swap the Volume's
    /// profile and the fog follows.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class LookController : MonoBehaviour
    {
        Volume volume;

        Volume Volume => volume != null ? volume : (volume = GetComponent<Volume>());

        void OnEnable() => ApplyFog();
        void LateUpdate() => ApplyFog();

        void ApplyFog()
        {
            var profile = Volume.sharedProfile;
            if (profile == null || !profile.TryGet(out KitFog fog) || !fog.IsActive()) return;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fog.start.value;
            RenderSettings.fogEndDistance = fog.end.value;
            RenderSettings.fogColor = fog.color.value;
        }
    }
}
