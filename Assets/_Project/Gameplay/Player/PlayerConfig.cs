using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>
    /// Every number behind the first-person feel. The owner tunes these in Play Mode; edits to
    /// the asset persist, and KitBuilder never overwrites an existing config, so tuned values are
    /// treated as deliberate. Units: metres, seconds, degrees.
    /// </summary>
    [CreateAssetMenu(menuName = "Kit/Config/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ConfigAsset
    {
        [Header("Movement")]
        [Tooltip("Walk speed, m/s.")] public float MoveSpeed = 6f;
        [Tooltip("Multiplier applied while Sprint is held.")] public float SprintMultiplier = 1.5f;
        [Tooltip("Seconds from standstill to full speed. Releasing all input on the ground stops instantly.")]
        public float AccelerationTime = 0.2f;
        [Tooltip("Speed fraction (y) over acceleration progress (x), both 0..1.")]
        public AnimationCurve AccelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("How fast the body turns to face movement in third-person/top-down, degrees/s.")] public float TurnSpeed = 720f;

        [Header("Jump and gravity")]
        [Tooltip("Apex height of a jump, m.")] public float JumpHeight = 1.5f;
        [Tooltip("Vertical acceleration, m/s^2 (negative).")] public float Gravity = -30f;
        [Tooltip("Grace period after walking off a ledge during which Jump still works, s.")] public float CoyoteTime = 0.1f;
        [Tooltip("A Jump pressed this long before landing still fires on landing, s. Makes hop chains fair.")]
        public float JumpBufferTime = 0.12f;

        [Header("Momentum (slides and hop chains)")]
        [Tooltip("Hard cap on horizontal speed, m/s.")] public float MaxSpeed = 26f;
        [Tooltip("After landing, momentum is kept for this long before ground friction bites, s. Hop or slide inside it to chain.")]
        public float ChainWindow = 0.15f;
        [Tooltip("How fast speed above walking pace bleeds off on the ground once the chain window closes, m/s^2.")]
        public float MomentumFriction = 14f;
        [Tooltip("Speed added by a chained hop (a jump from a slide or just after landing), m/s at walk speed; shrinks toward MaxSpeed.")]
        public float HopBoost = 0.6f;
        [Tooltip("How fast you can turn above-walking-speed momentum on the ground, degrees/s.")] public float GroundSteer = 360f;

        [Header("Air control (Source-style)")]
        [Tooltip("Air acceleration as a multiple of MoveSpeed per second (like sv_airaccelerate). Higher = stronger air strafing.")]
        public float AirAccelerate = 12f;
        [Tooltip("Most speed a single air-accelerate step can add along your input direction, m/s (Source's 30 u/s cap). " +
                 "Strafing while turning gains speed through this.")]
        public float AirWishSpeedCap = 0.6f;
        [Tooltip("Easy air control on top of strafing: how fast held input turns your momentum in the air, degrees/s. 0 = pure Source.")]
        public float AirSteer = 120f;
        [Tooltip("In the air, acceleration toward walking pace when slower than it (so a standing jump still moves), m/s^2.")]
        public float AirWalkAcceleration = 12f;

        [Header("Surfing (slopes steeper than the slope limit)")]
        [Tooltip("Air acceleration while surfing, like Source surf servers' high sv_airaccelerate. Strafing into the ramp holds you up.")]
        public float SurfAirAccelerate = 100f;
        [Tooltip("Gravity multiplier while surfing. 0.5 matches Source's gravity at kit scale; the kit's normal -30 is tuned for weighty jumps.")]
        public float SurfGravityScale = 0.5f;
        [Tooltip("Surf settings stay on this long after losing ramp contact, so brief contact gaps don't flicker them, s.")]
        public float SurfGrace = 0.15f;
        [Tooltip("Leaving a surf ramp, or a wall run without jumping, turns your momentum up to this far toward where you look (never back into the surface), degrees. You leave in the direction you face instead of drifting with the surface.")]
        public float LeaveSurfaceTurn = 90f;

        [Header("Stairs and ground")]
        [Tooltip("After walking off a step or down a ramp, snap down to walkable ground this far below instead of falling, m.")]
        public float GroundSnapDistance = 0.45f;
        [Tooltip("Seconds the camera takes to ease out a step-up or step-down on flat ground, so stairs glide instead of jerk.")]
        public float StepSmoothTime = 0.1f;

        [Header("Chain streak")]
        [Tooltip("Each consecutive chained jump multiplies the next slide/hop boost by 1 + this. Chains pick up more speed the longer they run.")]
        public float StreakBoostPerChain = 0.15f;
        [Tooltip("The streak multiplier stops growing after this many consecutive chains.")]
        public int StreakMaxChains = 8;
        [Tooltip("How far a slide-jump can snap your momentum toward where you are looking, degrees. 180 = any direction.")]
        public float SlideJumpTurn = 120f;

        [Header("Slide")]
        [Tooltip("Minimum horizontal speed needed to start a slide, m/s.")] public float SlideMinStartSpeed = 5f;
        [Tooltip("Speed added when a slide starts, m/s at walk speed; shrinks toward MaxSpeed.")] public float SlideBoost = 2f;
        [Tooltip("Slides started sooner than this after the last boosted slide get no boost, s. Stops boost spam.")]
        public float SlideBoostCooldown = 0.6f;
        [Tooltip("Deceleration while sliding, m/s^2. Low = long slides.")] public float SlideFriction = 4f;
        [Tooltip("The slide ends below this speed, m/s.")] public float SlideEndSpeed = 3f;
        [Tooltip("How fast input can turn the slide, degrees/s.")] public float SlideSteer = 60f;
        [Tooltip("Capsule height while sliding, m.")] public float SlideHeight = 1.0f;
        [Tooltip("Eye height while sliding, m.")] public float SlideEyeHeight = 0.8f;
        [Tooltip("Speed limit while stuck low under a ceiling after a slide, m/s.")] public float CrouchSpeed = 3f;

        [Header("Wall run")]
        [Tooltip("Minimum horizontal speed to start a wall run, m/s.")] public float WallRunMinSpeed = 7f;
        [Tooltip("How far beside the capsule a wall can be detected, m.")] public float WallCheckDistance = 0.45f;
        [Tooltip("Steepest approach that still attaches, degrees off the wall line. Steeper than this is a head-on collision.")]
        public float WallRunMaxApproachAngle = 75f;
        [Tooltip("Upward speed given when a wall run starts, m/s. With WallRunGravity this makes the gentle arc.")]
        public float WallRunUpSpeed = 3.5f;
        [Tooltip("Gravity while wall running, m/s^2 (negative, weaker than normal gravity).")] public float WallRunGravity = -7f;
        [Tooltip("Longest a single wall run lasts, s.")] public float WallRunMaxTime = 1.4f;
        [Tooltip("Speed lost while wall running, m/s^2.")] public float WallRunFriction = 1.5f;
        [Tooltip("After leaving a wall, the same wall cannot be grabbed again for this long, s. Other walls can.")]
        public float WallRunCooldown = 0.5f;
        [Tooltip("A Jump this soon after sliding off a wall still counts as a wall jump, s.")] public float WallJumpGrace = 0.15f;
        [Tooltip("How hard a wall jump pushes away from the wall, m/s. Sets the take-off angle; total speed comes from WallJumpBoost.")]
        public float WallJumpPush = 6f;
        [Tooltip("Speed added by a wall jump, m/s at walk speed (scaled by the chain streak, shrinks toward MaxSpeed).")]
        public float WallJumpBoost = 1.0f;
        [Tooltip("Wall jump height as a fraction of a normal jump.")] public float WallJumpUpFactor = 0.9f;
        [Tooltip("How far a wall jump can turn from the wall line toward where you look, degrees. 90 = straight away from the wall, so you always leave the way you face.")] public float WallJumpAimTurn = 90f;
        [Tooltip("After a wall jump, air steering is off for this long, so the kick away from the wall reads, s.")]
        public float WallJumpSteerLock = 0.25f;
        [Tooltip("Input pointing away from the wall beyond this (dot with the wall normal, 0..1) detaches. 0.9 = about 65 deg off the wall line.")]
        public float WallDetachInput = 0.9f;
        [Tooltip("Camera roll away from the wall while wall running, degrees.")] public float WallRunCameraTilt = 12f;
        [Tooltip("How quickly the camera roll eases in and out, 1/s.")] public float CameraTiltResponse = 10f;

        [Header("First-person camera feel")]
        [Tooltip("Vertical field of view at walking pace and below, degrees.")] public float FieldOfView = 95f;
        [Tooltip("Extra FOV at MaxSpeed, degrees. Scales with horizontal speed between walk and max speed.")]
        public float SpeedFovKick = 35f;
        [Tooltip("Momentary extra FOV on every slide or hop boost, degrees. Sells each speed gain.")]
        public float BoostFovPunch = 6f;
        [Tooltip("How long the boost FOV punch takes to fade, s.")]
        public float BoostFovPunchTime = 0.35f;
        [Tooltip("FOV kick fraction (y) over speed fraction between walk and max speed (x). Ease-out so sprinting already reads.")]
        public AnimationCurve SpeedFovCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2.2f), new Keyframe(1f, 1f, 0f, 0f));
        [Tooltip("How quickly FOV eases toward its target, 1/s.")] public float FovResponse = 8f;
        [Tooltip("Vertical head-bob amplitude at full walk speed, m.")] public float HeadBobAmplitude = 0.018f;
        [Tooltip("Metres walked per full bob cycle (two footsteps).")] public float HeadBobStride = 3.2f;
        [Tooltip("Sideways sway as a fraction of the vertical bob.")] public float HeadBobSway = 0.35f;
        [Tooltip("Landing dip per m/s of fall speed, m.")] public float LandingDipPerSpeed = 0.012f;
        [Tooltip("Largest landing dip, m.")] public float LandingDipMax = 0.18f;
        [Tooltip("Seconds for the head to recover from a landing dip.")] public float LandingRecoverTime = 0.22f;
        [Tooltip("How quickly the eye moves between standing and slide height, 1/s.")] public float EyeHeightResponse = 14f;

        /// <summary>Launch speed that reaches JumpHeight under Gravity: v = sqrt(2 h |g|).</summary>
        public float JumpVelocity => Mathf.Sqrt(2f * JumpHeight * -Gravity);

        protected override void Sanitize()
        {
            MoveSpeed = Mathf.Max(0f, MoveSpeed);
            SprintMultiplier = Mathf.Max(1f, SprintMultiplier);
            AccelerationTime = Mathf.Max(0f, AccelerationTime);
            JumpHeight = Mathf.Max(0f, JumpHeight);
            Gravity = Mathf.Min(-0.01f, Gravity);
            CoyoteTime = Mathf.Max(0f, CoyoteTime);
            JumpBufferTime = Mathf.Max(0f, JumpBufferTime);
            TurnSpeed = Mathf.Max(0f, TurnSpeed);
            MaxSpeed = Mathf.Max(MoveSpeed * SprintMultiplier, MaxSpeed);
            ChainWindow = Mathf.Max(0f, ChainWindow);
            MomentumFriction = Mathf.Max(0f, MomentumFriction);
            HopBoost = Mathf.Max(0f, HopBoost);
            AirAccelerate = Mathf.Max(0f, AirAccelerate);
            GroundSteer = Mathf.Max(0f, GroundSteer);
            AirWishSpeedCap = Mathf.Max(0f, AirWishSpeedCap);
            AirSteer = Mathf.Max(0f, AirSteer);
            AirWalkAcceleration = Mathf.Max(0f, AirWalkAcceleration);
            SurfAirAccelerate = Mathf.Max(0f, SurfAirAccelerate);
            SurfGravityScale = Mathf.Clamp(SurfGravityScale, 0f, 2f);
            SurfGrace = Mathf.Max(0f, SurfGrace);
            LeaveSurfaceTurn = Mathf.Clamp(LeaveSurfaceTurn, 0f, 180f);
            GroundSnapDistance = Mathf.Clamp(GroundSnapDistance, 0f, 1f);
            StepSmoothTime = Mathf.Clamp(StepSmoothTime, 0f, 0.5f);
            StreakBoostPerChain = Mathf.Max(0f, StreakBoostPerChain);
            StreakMaxChains = Mathf.Max(0, StreakMaxChains);
            SlideJumpTurn = Mathf.Clamp(SlideJumpTurn, 0f, 180f);
            SlideMinStartSpeed = Mathf.Max(0f, SlideMinStartSpeed);
            SlideBoost = Mathf.Max(0f, SlideBoost);
            SlideBoostCooldown = Mathf.Max(0f, SlideBoostCooldown);
            SlideFriction = Mathf.Max(0f, SlideFriction);
            SlideEndSpeed = Mathf.Clamp(SlideEndSpeed, 0f, SlideMinStartSpeed);
            SlideSteer = Mathf.Max(0f, SlideSteer);
            SlideHeight = Mathf.Clamp(SlideHeight, 0.5f, 1.8f);
            SlideEyeHeight = Mathf.Clamp(SlideEyeHeight, 0.2f, SlideHeight);
            CrouchSpeed = Mathf.Max(0f, CrouchSpeed);
            WallRunMinSpeed = Mathf.Max(0f, WallRunMinSpeed);
            WallCheckDistance = Mathf.Clamp(WallCheckDistance, 0.05f, 2f);
            WallRunMaxApproachAngle = Mathf.Clamp(WallRunMaxApproachAngle, 10f, 89f);
            WallRunUpSpeed = Mathf.Max(0f, WallRunUpSpeed);
            WallRunGravity = Mathf.Min(0f, WallRunGravity);
            WallRunMaxTime = Mathf.Max(0.05f, WallRunMaxTime);
            WallRunFriction = Mathf.Max(0f, WallRunFriction);
            WallRunCooldown = Mathf.Max(0f, WallRunCooldown);
            WallJumpGrace = Mathf.Max(0f, WallJumpGrace);
            WallJumpPush = Mathf.Max(0f, WallJumpPush);
            WallJumpBoost = Mathf.Max(0f, WallJumpBoost);
            WallJumpUpFactor = Mathf.Max(0f, WallJumpUpFactor);
            WallJumpAimTurn = Mathf.Clamp(WallJumpAimTurn, 0f, 180f);
            WallJumpSteerLock = Mathf.Max(0f, WallJumpSteerLock);
            WallDetachInput = Mathf.Clamp01(WallDetachInput);
            WallRunCameraTilt = Mathf.Clamp(WallRunCameraTilt, 0f, 45f);
            CameraTiltResponse = Mathf.Max(0.01f, CameraTiltResponse);
            FieldOfView = Mathf.Clamp(FieldOfView, 30f, 140f);
            SpeedFovKick = Mathf.Clamp(SpeedFovKick, 0f, 140f - FieldOfView);
            BoostFovPunch = Mathf.Clamp(BoostFovPunch, 0f, 30f);
            BoostFovPunchTime = Mathf.Max(0.01f, BoostFovPunchTime);
            FovResponse = Mathf.Max(0.01f, FovResponse);
            HeadBobAmplitude = Mathf.Max(0f, HeadBobAmplitude);
            HeadBobStride = Mathf.Max(0.1f, HeadBobStride);
            LandingDipPerSpeed = Mathf.Max(0f, LandingDipPerSpeed);
            LandingDipMax = Mathf.Max(0f, LandingDipMax);
            LandingRecoverTime = Mathf.Max(0.01f, LandingRecoverTime);
            EyeHeightResponse = Mathf.Max(0.01f, EyeHeightResponse);
        }
    }
}
