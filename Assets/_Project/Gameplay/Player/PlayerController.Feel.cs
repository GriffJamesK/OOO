using UnityEngine;

namespace Kit.Gameplay
{
    // First-person camera feel: speed-based FOV, head bob, landing dip, slide eye height and
    // wall-run roll. Split from the main file per the partial-file convention so movement and
    // feel can be tuned separately.
    public sealed partial class PlayerController
    {
        Vector3 eyeLocalPosition;
        float eyeHeight;
        float bobPhase;
        float bobAmplitude;
        float dip;
        float dipVelocity;
        float fieldOfView;
        float boostPunch;
        float cameraRoll;
        float stepSmooth;          // camera offset that eases out step pops (negative = camera below body)
        float stepSmoothVelocity;

        /// <summary>FOV the first-person camera should use this frame: base FOV, eased speed kick, and any boost punch.</summary>
        public float CurrentFieldOfView => SpeedFieldOfView + boostPunch;

        /// <summary>Camera roll (Cinemachine Dutch) this frame, degrees: leans away from the wall while wall running.</summary>
        public float CurrentCameraRoll => cameraRoll;

        float SpeedFieldOfView => fieldOfView > 0f ? fieldOfView : (config != null ? config.FieldOfView : 60f);

        void InitFeel()
        {
            if (cameraTarget != null) eyeLocalPosition = cameraTarget.localPosition;
            eyeHeight = eyeLocalPosition.y;
            fieldOfView = config != null ? config.FieldOfView : 0f;
        }

        /// <summary>A slide or hop just added speed: punch the FOV out so the gain is felt, not just measured.</summary>
        void OnBoost() => boostPunch = config.BoostFovPunch;

        void ResetFeel()
        {
            dip = dipVelocity = boostPunch = cameraRoll = bobAmplitude = stepSmooth = stepSmoothVelocity = 0f;
            eyeHeight = eyeLocalPosition.y;
            if (config != null) fieldOfView = config.FieldOfView;
        }

        void UpdateFeel(float dt, float landingFallSpeed, float stepPop)
        {
            // FOV widens with horizontal speed (walk -> MaxSpeed), so slides and hop chains read as fast.
            float targetFov = config.FieldOfView + Momentum.SpeedFovKick(HorizontalSpeed, config.MoveSpeed,
                config.MaxSpeed, config.SpeedFovKick, config.SpeedFovCurve);
            fieldOfView = Mathf.Lerp(SpeedFieldOfView, targetFov, 1f - Mathf.Exp(-config.FovResponse * dt));
            boostPunch = Mathf.MoveTowards(boostPunch, 0f, config.BoostFovPunch / config.BoostFovPunchTime * dt);

            // Lean away from the wall: wall on the right -> head tilts left (positive Dutch).
            cameraRoll = Mathf.Lerp(cameraRoll, WallSide * config.WallRunCameraTilt, 1f - Mathf.Exp(-config.CameraTiltResponse * dt));

            if (cameraTarget == null) return;

            float eyeTarget = IsLow ? config.SlideEyeHeight : eyeLocalPosition.y;
            eyeHeight = Mathf.Lerp(eyeHeight, eyeTarget, 1f - Mathf.Exp(-config.EyeHeightResponse * dt));

            // Bob only while walking or running on the ground; it advances with distance walked, so
            // it matches footsteps at any speed, and fades in and out instead of snapping.
            bool walking = IsGrounded && !IsLow;
            float walk01 = config.MoveSpeed > 0f ? Mathf.Clamp01(HorizontalSpeed / config.MoveSpeed) : 0f;
            if (walking) bobPhase = (bobPhase + HorizontalSpeed * dt / config.HeadBobStride) % 1f;
            float targetAmplitude = walking ? config.HeadBobAmplitude * walk01 : 0f;
            bobAmplitude = Mathf.MoveTowards(bobAmplitude, targetAmplitude, config.HeadBobAmplitude * 6f * dt);
            float angle = bobPhase * Mathf.PI * 2f;
            float bobY = Mathf.Sin(angle * 2f) * bobAmplitude;            // two dips per cycle: one per step
            float swayX = Mathf.Cos(angle) * bobAmplitude * config.HeadBobSway;

            if (landingFallSpeed > 0f)
                dip = Mathf.Max(dip, Mathf.Min(config.LandingDipMax, landingFallSpeed * config.LandingDipPerSpeed));
            dip = Mathf.SmoothDamp(dip, 0f, ref dipVelocity, config.LandingRecoverTime, Mathf.Infinity, dt);

            // Stairs: the body pops a step height in one frame; hold the camera where it was and ease
            // it after the body, so going up or down steps glides instead of jerking.
            stepSmooth = Mathf.Clamp(stepSmooth - stepPop, -0.6f, 0.6f);
            stepSmooth = config.StepSmoothTime > 0f
                ? Mathf.SmoothDamp(stepSmooth, 0f, ref stepSmoothVelocity, config.StepSmoothTime, Mathf.Infinity, dt)
                : 0f;

            cameraTarget.localPosition = new Vector3(eyeLocalPosition.x + swayX, eyeHeight + bobY - dip + stepSmooth, eyeLocalPosition.z);
        }
    }
}
