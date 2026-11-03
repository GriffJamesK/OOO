using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>Camera rig tunables. One asset per rig variant (ThirdPerson, FPS, TopDown) under Config/.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Camera Config", fileName = "CameraConfig")]
    public sealed class CameraConfig : ConfigAsset
    {
        [Tooltip("Distance from the target, m. Ignored by the FPS rig.")] public float Distance = 6f;
        [Tooltip("Follow smoothing, seconds. 0 = rigid.")] public float Damping = 0.3f;
        [Tooltip("Vertical field of view, degrees.")] public float FieldOfView = 60f;
        [Tooltip("Mouse look speed, degrees per pixel.")] public float MouseSensitivity = 0.12f;
        [Tooltip("Gamepad look speed at full stick, degrees/s.")] public float GamepadLookSpeed = 180f;
        [Tooltip("Lowest look angle, degrees (negative looks up at the target from below).")] public float PitchMin = -10f;
        [Tooltip("Highest look angle, degrees.")] public float PitchMax = 60f;
        [Tooltip("Starting pitch, degrees. TopDown uses this as its fixed angle.")] public float StartPitch = 15f;

        protected override void Sanitize()
        {
            Distance = Mathf.Max(0f, Distance);
            Damping = Mathf.Max(0f, Damping);
            FieldOfView = Mathf.Clamp(FieldOfView, 10f, 120f);
            PitchMin = Mathf.Clamp(PitchMin, -89f, 89f);
            PitchMax = Mathf.Clamp(PitchMax, PitchMin, 89f);
            StartPitch = Mathf.Clamp(StartPitch, PitchMin, PitchMax);
        }
    }
}
