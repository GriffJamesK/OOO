using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Audio
{
    /// <summary>Volumes and timing for the procedural placeholder audio. Asset: Config/AudioConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Audio Config", fileName = "AudioConfig")]
    public sealed class AudioConfig : ConfigAsset
    {
        [Range(0f, 1f)] public float MasterVolume = 0.8f;
        [Range(0f, 1f)] public float SfxVolume = 0.7f;
        [Range(0f, 1f)] public float AmbienceVolume = 0.5f;

        [Tooltip("Metres walked per footstep.")]
        public float FootstepStride = 1.9f;

        [Tooltip("Ambience crossfade between areas (s).")]
        public float AmbienceCrossfade = 2f;

        protected override void Sanitize()
        {
            FootstepStride = Mathf.Max(0.3f, FootstepStride);
            AmbienceCrossfade = Mathf.Max(0f, AmbienceCrossfade);
        }
    }
}
