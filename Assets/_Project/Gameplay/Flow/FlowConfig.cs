using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Flow
{
    /// <summary>Tunables for respawn, screen fades and the ending. Asset: Config/FlowConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Flow Config", fileName = "FlowConfig")]
    public sealed class FlowConfig : ConfigAsset
    {
        [Tooltip("Seconds the player must stand grounded before a spot counts as safe ground.")]
        public float SafeGroundDwell = 0.3f;

        [Tooltip("Fade to black before a respawn (s).")]
        public float FadeOut = 0.25f;

        [Tooltip("Fade back in after a respawn (s).")]
        public float FadeIn = 0.4f;

        [Tooltip("Seconds after stepping out of the 000 door before GameCompleted (the title card).")]
        public float EndingTitleDelay = 6f;

        protected override void Sanitize()
        {
            SafeGroundDwell = Mathf.Max(0f, SafeGroundDwell);
            FadeOut = Mathf.Max(0f, FadeOut);
            FadeIn = Mathf.Max(0f, FadeIn);
            EndingTitleDelay = Mathf.Max(0f, EndingTitleDelay);
        }
    }
}
