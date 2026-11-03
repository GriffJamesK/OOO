using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// Tunables for light flicker, screen glow and light shafts. Asset: Config/LookConfig.asset.
    /// Shaft numbers are pushed to every LightShaft's renderer when it is enabled, so tuning the
    /// asset in Play Mode takes effect on the next enable (or LightShaft.Refresh).
    /// </summary>
    [CreateAssetMenu(menuName = "Kit/Config/Look Config", fileName = "LookConfig")]
    public sealed class LookConfig : ConfigAsset
    {
        [Header("Flicker")]
        [Tooltip("Average seconds between flickers of a FlickerLight (each light randomises around this).")]
        public float FlickerInterval = 14f;

        [Tooltip("Shortest flicker burst, seconds.")]
        public float FlickerMinDuration = 0.1f;

        [Tooltip("Longest flicker burst, seconds.")]
        public float FlickerMaxDuration = 0.6f;

        [Tooltip("Shortest time one flicker level is held, seconds (the burst is a run of irregular steps).")]
        public float FlickerStepMin = 0.02f;

        [Tooltip("Longest time one flicker level is held, seconds.")]
        public float FlickerStepMax = 0.09f;

        [Tooltip("Darkest a flicker gets, as a fraction of the light's normal intensity.")]
        [Range(0f, 1f)] public float FlickerDimLevel = 0.08f;

        [Header("Screen glow")]
        [Tooltip("CRT glow pulse speed (cycles per second).")]
        public float ScreenGlowRate = 0.35f;

        [Tooltip("CRT glow pulse depth: emission swings by this fraction either way.")]
        [Range(0f, 1f)] public float ScreenGlowAmount = 0.12f;

        [Header("Light shafts")]
        [Tooltip("Light shaft material (Kit/LightShaft). Assigned by the Look builder.")]
        public Material LightShaftMaterial;

        [Tooltip("How much wider a shaft is at its bottom than at its top (1 = straight box).")]
        public float ShaftBottomWiden = 1.18f;

        [Tooltip("Overall shaft brightness multiplier (on top of each shaft's tint alpha).")]
        public float ShaftIntensity = 1f;

        [Tooltip("Fraction of the shaft's half-width over which its sides fade out (1 = soft all the way to the centre).")]
        [Range(0.05f, 1f)] public float ShaftEdgeSoftness = 0.7f;

        [Tooltip("Brightness at the bottom of the shaft relative to the top.")]
        [Range(0f, 1f)] public float ShaftBottomBrightness = 0.35f;

        [Tooltip("Fraction of the shaft's length over which it fades in at the top.")]
        [Range(0.001f, 0.5f)] public float ShaftTopFade = 0.06f;

        [Tooltip("Fraction of the shaft's length over which it fades out at the bottom.")]
        [Range(0.001f, 0.5f)] public float ShaftBottomFade = 0.12f;

        [Tooltip("Light closer to the camera than this (m) is invisible, so standing in a shaft never puts a wall of light in your face.")]
        public float ShaftNearFadeStart = 0.5f;

        [Tooltip("Light is at full strength from this distance (m) on.")]
        public float ShaftNearFadeEnd = 5f;

        [Tooltip("How much scene fog swallows the shaft with distance (0 = never, 1 = like any surface).")]
        [Range(0f, 1f)] public float ShaftFogFade = 0.7f;

        [Tooltip("Extra brightness when looking up the shaft toward its light (forward scattering in fog).")]
        public float ShaftForwardScatter = 1.5f;

        protected override void Sanitize()
        {
            FlickerInterval = Mathf.Max(0.5f, FlickerInterval);
            FlickerMinDuration = Mathf.Max(0.02f, FlickerMinDuration);
            FlickerMaxDuration = Mathf.Max(FlickerMinDuration, FlickerMaxDuration);
            FlickerStepMin = Mathf.Max(0.005f, FlickerStepMin);
            FlickerStepMax = Mathf.Max(FlickerStepMin, FlickerStepMax);
            ScreenGlowRate = Mathf.Max(0f, ScreenGlowRate);
            ShaftBottomWiden = Mathf.Max(0.1f, ShaftBottomWiden);
            ShaftIntensity = Mathf.Max(0f, ShaftIntensity);
            ShaftNearFadeStart = Mathf.Max(0f, ShaftNearFadeStart);
            ShaftNearFadeEnd = Mathf.Max(ShaftNearFadeStart + 0.01f, ShaftNearFadeEnd);
            ShaftForwardScatter = Mathf.Max(0f, ShaftForwardScatter);
        }
    }
}
