using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>Tunables for threshold travel. Asset: Config/ThresholdConfig.asset (read via ConfigLookup).</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Threshold Config", fileName = "ThresholdConfig")]
    public sealed class ThresholdConfig : ConfigAsset
    {
        [Tooltip("Speed through a threshold is clamped to this (m/s). Keeps the A5 launch predictable whatever the fall height.")]
        public float MaxExitSpeed = 26f;

        [Tooltip("Only thresholds within this distance (m) of the player are tested for crossings each frame.")]
        public float CrossingSearchRadius = 30f;

        [Tooltip("Depth of the frame geometry OfficeKit builds around an opening (m).")]
        public float FrameDepth = 0.25f;

        [Tooltip("Width of the frame's jambs and head (m).")]
        public float FrameWidth = 0.12f;

        protected override void Sanitize()
        {
            MaxExitSpeed = Mathf.Max(1f, MaxExitSpeed);
            CrossingSearchRadius = Mathf.Max(1f, CrossingSearchRadius);
            FrameDepth = Mathf.Max(0.01f, FrameDepth);
            FrameWidth = Mathf.Max(0.01f, FrameWidth);
        }
    }
}
