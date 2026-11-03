using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// Sits on the scene's global Volume and copies the fog into RenderSettings each frame, so fog
    /// is part of the look like every other post effect. URP fog lives in Lighting settings, not in
    /// Volumes, so this is the bridge.
    ///
    /// The fog is read from the blended volume stack at the main camera, not only from this
    /// Volume's profile: local box Volumes with a KitFog override (OfficeKit.FogZone) then give
    /// per-area fog that blends in over their blend distance, exactly like local post effects.
    /// The stack is private, so evaluating it never disturbs the stack URP renders with. With only
    /// the global volume in the scene the result is that profile's fog, as before.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class LookController : MonoBehaviour
    {
        Volume volume;
        VolumeStack stack;

        Volume Volume => volume != null ? volume : (volume = GetComponent<Volume>());

        void OnEnable() => ApplyFog();
        void LateUpdate() => ApplyFog();

        void OnDisable()
        {
            if (stack == null) return;
            if (stack.isValid) VolumeManager.instance.DestroyStack(stack);
            stack = null;
        }

        /// <summary>
        /// The transform and layer mask URP blends volumes with for the main camera: its volume
        /// trigger (the camera itself unless set) and its volume layer mask (everything when the
        /// camera has no URP data). Trigger is null when there is no main camera, which makes the
        /// stack consider global volumes only. Shared with LookDebug so both agree on "at the camera".
        /// </summary>
        public static Transform ResolveTrigger(out LayerMask mask)
        {
            mask = ~0;
            var cam = Camera.main;
            if (cam == null) return null;
            if (cam.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                mask = data.volumeLayerMask;
                if (data.volumeTrigger != null) return data.volumeTrigger;
            }
            return cam.transform;
        }

        /// <summary>The fog to show this frame: the blended stack's KitFog, or this profile's when the stack can't be evaluated.</summary>
        KitFog CurrentFog()
        {
            var blended = BlendedFog();
            if (blended != null) return blended;
            var profile = Volume.sharedProfile;
            return profile != null && profile.TryGet(out KitFog own) ? own : null;
        }

        /// <summary>
        /// Evaluates every volume at the camera into the private stack. Null until URP has
        /// initialised the VolumeManager (it does so when the pipeline is created), so the first
        /// frames fall back to this Volume's own profile instead of throwing.
        /// </summary>
        KitFog BlendedFog()
        {
            var manager = VolumeManager.instance;
            if (!manager.isInitialized) return null;
            if (stack == null || !stack.isValid) stack = manager.CreateStack();

            var trigger = ResolveTrigger(out var mask);
            manager.Update(stack, trigger, mask);
            return stack.GetComponent<KitFog>();
        }

        void ApplyFog()
        {
            var fog = CurrentFog();
            if (fog == null || !fog.IsActive()) return;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fog.start.value;
            RenderSettings.fogEndDistance = fog.end.value;
            RenderSettings.fogColor = fog.color.value;
        }
    }
}
