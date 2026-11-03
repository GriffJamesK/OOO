using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Gameplay.Look
{
    /// <summary>Test hook for the look, via MCP: <c>return Kit.Gameplay.Look.LookDebug.Report();</c></summary>
    public static class LookDebug
    {
        /// <summary>
        /// Active global profile, the fog actually in RenderSettings, and how many local fog
        /// volumes (OfficeKit.FogZone) influence the main camera right now, so a fog-zone layout can
        /// be checked by teleporting around and reading this.
        /// </summary>
        public static string Report()
        {
            var look = Object.FindAnyObjectByType<LookController>();
            if (look == null) return "Look: no LookController (is a play scene loaded?)";
            var profile = look.GetComponent<Volume>().sharedProfile;
            var c = RenderSettings.fogColor;
            return $"profile={(profile ? profile.name : "none")} " +
                   $"fog={RenderSettings.fog} {RenderSettings.fogStartDistance:0.#}-{RenderSettings.fogEndDistance:0.#}m " +
                   $"#{ColorUtility.ToHtmlStringRGB(c)} localFogVolumes={LocalFogVolumesAtCamera()}";
        }

        /// <summary>
        /// Counts enabled local Volumes whose profile carries an active KitFog and whose colliders
        /// are within blend distance of the camera's volume trigger: the same test VolumeManager uses.
        /// </summary>
        public static int LocalFogVolumesAtCamera()
        {
            var trigger = LookController.ResolveTrigger(out var mask);
            if (trigger == null) return 0;
            var position = trigger.position;
            int count = 0;
            foreach (var v in VolumeManager.instance.GetVolumes(mask))
            {
                if (v == null || v.isGlobal || !v.enabled || v.weight <= 0f) continue;
                var profile = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;
                if (profile == null || !profile.TryGet(out KitFog fog) || !fog.active) continue;

                float closest = float.PositiveInfinity;
                foreach (var col in v.colliders)
                {
                    if (col == null || !col.enabled) continue;
                    closest = Mathf.Min(closest, (col.ClosestPoint(position) - position).sqrMagnitude);
                }
                if (closest <= v.blendDistance * v.blendDistance) count++;
            }
            return count;
        }
    }
}
