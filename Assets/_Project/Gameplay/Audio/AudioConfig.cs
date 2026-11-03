using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Audio
{
    /// <summary>Volumes and timing for the procedural placeholder audio. Asset: Config/AudioConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ConfigAsset
    {
        [Header("Mix")]
        [Range(0f, 1f)] public float MasterVolume = 0.8f;
        [Range(0f, 1f)] public float SfxVolume = 0.7f;
        [Range(0f, 1f)] public float AmbienceVolume = 0.5f;
        [Tooltip("The ending chord (a music category of one).")]
        [Range(0f, 1f)] public float EndingVolume = 0.8f;

        [Header("Per-sound levels (x Sfx)")]
        [Range(0f, 1f)] public float FootstepVolume = 0.5f;
        [Range(0f, 1f)] public float MovementVolume = 0.6f;
        [Range(0f, 1f)] public float ThresholdVolume = 0.9f;
        [Range(0f, 1f)] public float TileVolume = 0.8f;
        [Range(0f, 1f)] public float RespawnVolume = 0.7f;

        [Header("Footsteps")]
        [Tooltip("Metres walked per footstep.")]
        public float FootstepStride = 1.9f;
        [Tooltip("Random pitch spread per footstep (+/-), so steps don't machine-gun.")]
        [Range(0f, 0.3f)] public float FootstepPitchJitter = 0.06f;

        [Header("Landing")]
        [Tooltip("Fall speed (m/s, downward) below which a landing makes no thump.")]
        public float LandMinSpeed = 3f;
        [Tooltip("Fall speed (m/s) at which the landing thump is full volume and lowest pitch.")]
        public float LandMaxSpeed = 18f;
        [Tooltip("Landing pitch at LandMinSpeed and at LandMaxSpeed (harder landings sound heavier).")]
        public Vector2 LandPitchRange = new Vector2(1.1f, 0.8f);

        [Header("Threshold crossing")]
        [Tooltip("Crossing pitch at standstill and at ThresholdPitchSpeed.")]
        public Vector2 ThresholdPitchRange = new Vector2(0.94f, 1.08f);
        [Tooltip("Speed (m/s) mapped to the top of ThresholdPitchRange.")]
        public float ThresholdPitchSpeed = 26f;

        [Header("Ambience")]
        [Tooltip("Ambience crossfade between areas (s).")]
        public float AmbienceCrossfade = 2f;
        [Tooltip("Ambience level while paused (x normal).")]
        [Range(0f, 1f)] public float PauseAmbienceDuck = 0.35f;
        [Tooltip("Ambience level under the ending chord (x normal).")]
        [Range(0f, 1f)] public float EndingAmbienceDuck = 0.3f;
        [Tooltip("Time (s) to duck or unduck the ambience.")]
        public float DuckTime = 1.5f;

        [Header("Voices")]
        [Tooltip("Pooled 2D sources for one-shots.")]
        public int SfxVoices = 10;

        protected override void Sanitize()
        {
            FootstepStride = Mathf.Max(0.3f, FootstepStride);
            AmbienceCrossfade = Mathf.Max(0f, AmbienceCrossfade);
            LandMinSpeed = Mathf.Max(0f, LandMinSpeed);
            LandMaxSpeed = Mathf.Max(LandMinSpeed + 0.1f, LandMaxSpeed);
            ThresholdPitchSpeed = Mathf.Max(0.1f, ThresholdPitchSpeed);
            DuckTime = Mathf.Max(0.01f, DuckTime);
            SfxVoices = Mathf.Clamp(SfxVoices, 2, 32);
        }
    }
}
