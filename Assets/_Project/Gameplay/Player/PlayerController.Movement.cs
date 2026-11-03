using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay
{
    // Ground, air and slide movement, Source-engine style:
    //   - Ground: walking eases in over AccelerationTime. Release all input on walkable ground and you
    //     stop that frame, unless you jump on that same frame, which keeps your speed (bunny hopping).
    //     Anything faster than walking is "momentum": steered, and bled off by ground friction once
    //     ChainWindow after landing has passed.
    //   - Air: no drag. Momentum.AirAccelerate (PM_AirAccelerate) means strafing while turning the
    //     view gains speed (air strafing). On top of that, holding forward steers momentum toward
    //     the view (AirSteer, scaled by the forward part of the input) for easy air control. It turns
    //     toward the view, not the input direction, so W+A never drags you sideways; a pure strafe
    //     gets none, so air strafing and surfing stay Source-like.
    //   - Leaving a surf ramp, or a wall run without jumping, turns momentum toward where you look
    //     (LeaveSurfaceTurn, never back into the surface), so you leave in the direction you face.
    //   - Collisions: velocity is clipped against every surface hit (PM_ClipVelocity). Walls let you
    //     slide along, ceilings stop upward motion, and slopes steeper than the controller's slope
    //     limit count as air, not ground: you slide along them and can strafe to stay on (surfing).
    //     While surfing, air acceleration is SurfAirAccelerate and gravity is scaled by
    //     SurfGravityScale, so a ramp can be ridden for its whole length.
    // On top of that, the kit's chain moves:
    //   - A slide (Slide while moving >= SlideMinStartSpeed) adds SlideBoost, then decays slowly.
    //   - A slide-jump (Jump while sliding, or within ChainWindow of letting go of Slide) keeps all
    //     speed, adds HopBoost, and snaps momentum up to SlideJumpTurn degrees toward where you look.
    //   - A hop within ChainWindow of landing also chains (keeps speed, adds HopBoost).
    //   - Holding Slide when you land starts a new slide straight away.
    //   - Wall runs and wall jumps live in PlayerController.WallRun.cs; a wall jump is also a chain.
    // Every chained jump extends a streak that scales the next boost; boosts shrink near MaxSpeed.
    public sealed partial class PlayerController
    {
        static readonly Collider[] overlapBuffer = new Collider[8];

        Vector3 horizontal;
        bool sliding;
        bool crouchBlocked;
        bool carryingMomentum;
        bool onWalkableGround;
        bool surfing;
        float surfTimer;                          // > 0: surf air-accelerate and gravity apply
        Vector3 surfNormal;                       // normal of the ramp being surfed
        float jumpBuffer;
        float chainTimer = float.MaxValue;        // since the last landing or slide release
        float timeSinceSlideEnd = float.MaxValue;
        float lastSlideBoostTime = float.NegativeInfinity;
        float standingHeight;
        Vector3 standingCenter;
        ChainStreak streak;

        readonly Vector3[] contacts = new Vector3[8];
        int contactCount;
        float bestUpNormal = -1f;

        /// <summary>True while sliding.</summary>
        public bool IsSliding => sliding;

        /// <summary>True while on a slope too steep to stand on: you slide along it like in the air.</summary>
        public bool IsSurfing => surfing;

        /// <summary>True while the capsule is low: sliding, or stuck under a ceiling after a slide.</summary>
        public bool IsLow => sliding || crouchBlocked;

        /// <summary>Consecutive chained jumps in the current chain (0 = no chain running).</summary>
        public int ChainCount => streak.Count;

        /// <summary>Seconds left on a buffered Jump press (0 = none). For tests and debug reports.</summary>
        public float JumpBufferRemaining => jumpBuffer;

        float StreakMultiplier => streak.Multiplier(config.StreakBoostPerChain, config.StreakMaxChains);

        /// <summary>Surfaces with an up-normal at least this (cos of the slope limit) are walkable ground.</summary>
        float WalkableNormalY => Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);

        void InitMovement()
        {
            standingHeight = controller.height;
            standingCenter = controller.center;
        }

        /// <summary>
        /// Replaces the current velocity, e.g. for launch pads, knockback or tests. Treated as carried
        /// momentum: it is kept in the air and steered or bled off on the ground like any other.
        /// </summary>
        public void SetVelocity(Vector3 velocity)
        {
            horizontal = new Vector3(velocity.x, 0f, velocity.z);
            verticalVelocity = velocity.y;
            carryingMomentum = true;
            HorizontalSpeed = horizontal.magnitude;
        }

        void Simulate(Vector3 wish, float inputMagnitude, float forwardInput, bool hasInput, bool slideHeld, bool slidePressed, bool grounded, float dt)
        {
            chainTimer += dt;
            timeSinceSlideEnd += dt;
            UpdateSurfExit(grounded, dt);
            if (grounded) { EndWallRun(); ResetWallRun(); } // touching ground clears all wall state
            if (sliding && !grounded) EndSlide(); // slid off a ledge: momentum carries into the air
            if (crouchBlocked && !sliding) TryStand();
            if (!sliding && grounded && slidePressed) TryStartSlide(wish);
            if (grounded && !sliding && chainTimer >= config.ChainWindow) streak.Break(); // stood still too long
            if (!grounded && !sliding) UpdateWallRun(wish, hasInput, dt);

            // A jump this frame skips ground friction entirely, which is what makes bunny hopping keep speed.
            bool groundJump = inputEnabled && jumpBuffer > 0f && grounded && coyote.CanJump && !crouchBlocked;

            if (wallRunning) WallRunMove(dt);
            else if (sliding) SlideMove(wish, hasInput, slideHeld, dt);
            else if (grounded) GroundMove(wish, inputMagnitude, hasInput, groundJump, dt);
            else AirMove(wish, inputMagnitude, forwardInput, hasInput, dt);

            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f; // keep grounded on slopes
            if (inputEnabled && jumpBuffer > 0f)
            {
                if (!grounded && CanWallJump) WallJump();
                else if (coyote.CanJump && !crouchBlocked) Jump();
            }
            float gravity = wallRunning ? config.WallRunGravity : config.Gravity * (surfTimer > 0f ? config.SurfGravityScale : 1f);
            verticalVelocity += gravity * dt;
        }

        /// <summary>Clears all momentum and movement state: used on respawn and teleport.</summary>
        void ResetMotion()
        {
            horizontal = Vector3.zero;
            verticalVelocity = 0f;
            ramp = default;
            coyote = default;
            streak.Break();
            jumpBuffer = 0f;
            chainTimer = float.MaxValue;
            timeSinceSlideEnd = float.MaxValue;
            carryingMomentum = false;
            sliding = false;
            crouchBlocked = false;
            SetLow(false);
            ResetWallRun();
            contactCount = 0;
            bestUpNormal = -1f;
            onWalkableGround = false;
            surfing = false;
            surfTimer = 0f;
            surfNormal = Vector3.zero;
            HorizontalSpeed = 0f;
        }

        void GroundMove(Vector3 wish, float inputMagnitude, bool hasInput, bool jumpingNow, float dt)
        {
            if (!hasInput)
            {
                // Nothing held: stop dead, unless this frame is a hop, which must keep its speed.
                if (!jumpingNow)
                {
                    horizontal = Vector3.zero;
                    ramp = default;
                    carryingMomentum = false;
                }
                return;
            }

            float pace = crouchBlocked ? config.CrouchSpeed : config.MoveSpeed * (sprinting ? config.SprintMultiplier : 1f);
            float top = Mathf.Max(0.01f, pace * inputMagnitude);
            float speed = horizontal.magnitude;

            if (speed > top + 0.01f)
            {
                // Faster than walking: steer the momentum, and bleed it off once the chain window closes
                // (never on a hop frame).
                var dir = Momentum.Steer(horizontal, wish, config.GroundSteer * dt);
                if (chainTimer >= config.ChainWindow && !jumpingNow) speed = Mathf.MoveTowards(speed, top, config.MomentumFriction * dt);
                horizontal = dir * speed;
                moveDirection = dir;
                carryingMomentum = true;
                return;
            }

            if (carryingMomentum)
            {
                // Hand back to the walking ramp at the current speed, with no jump in velocity.
                if (speed > 0.01f) moveDirection = horizontal / speed;
                ramp.SnapToFraction(speed / top, config.AccelerationCurve);
                carryingMomentum = false;
            }

            moveDirection = wish;
            float speed01 = ramp.Step(true, config.AccelerationTime, config.AccelerationTime,
                config.AccelerationCurve, config.AccelerationCurve, dt);
            horizontal = moveDirection * (top * speed01);
        }

        void AirMove(Vector3 wish, float inputMagnitude, float forwardInput, bool hasInput, float dt)
        {
            carryingMomentum = true;
            airSteerLock -= dt;
            if (!hasInput || airSteerLock > 0f) return; // no air drag: momentum is kept
            float wishSpeed = config.MoveSpeed * (sprinting ? config.SprintMultiplier : 1f) * inputMagnitude;

            // Source air acceleration: strafing while turning gains speed, and strafing into a surf
            // ramp holds you on it. Surf ramps use a much higher rate, like Source surf servers.
            float accelerate = surfTimer > 0f ? config.SurfAirAccelerate : config.AirAccelerate;
            horizontal = Momentum.AirAccelerate(horizontal, wish, wishSpeed, accelerate, config.AirWishSpeedCap, dt);

            // Easy air control on top (owner request): holding *forward* turns your momentum toward the
            // view, scaled by how much of the input is forward. It aims at the view, not the input
            // direction: steering toward W+A's diagonal pulled you sideways off ramps. A pure strafe gets
            // no steering, so air strafing and surfing behave like Source.
            float speed = horizontal.magnitude;
            if (speed < wishSpeed) speed = Mathf.MoveTowards(speed, wishSpeed, config.AirWalkAcceleration * dt);
            float steer = config.AirSteer * Mathf.Clamp01(forwardInput) * dt;
            horizontal = Momentum.Steer(horizontal, CameraForward(), steer) * Mathf.Min(speed, config.MaxSpeed);
        }

        void SlideMove(Vector3 wish, bool hasInput, bool slideHeld, float dt)
        {
            carryingMomentum = true;
            float speed = Mathf.MoveTowards(horizontal.magnitude, 0f, config.SlideFriction * dt);
            var dir = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : transform.forward;
            if (hasInput) dir = Momentum.Steer(dir, wish, config.SlideSteer * dt);
            horizontal = dir * speed;
            if (!slideHeld || speed < config.SlideEndSpeed) EndSlide();
        }

        void TryStartSlide(Vector3 wish)
        {
            float speed = horizontal.magnitude;
            if (speed < config.SlideMinStartSpeed) return;

            var dir = speed > 0.01f ? horizontal / speed : wish;
            bool boosted = Time.time - lastSlideBoostTime >= config.SlideBoostCooldown;
            if (boosted)
            {
                speed += Momentum.ChainBoost(speed, config.SlideBoost * StreakMultiplier, config.MoveSpeed, config.MaxSpeed);
                lastSlideBoostTime = Time.time;
                OnBoost();
            }
            horizontal = dir * Mathf.Min(speed, config.MaxSpeed);
            sliding = true;
            carryingMomentum = true;
            SetLow(true);
            EventBus.Publish(new PlayerSlid(transform.position, horizontal.magnitude, boosted));
        }

        void EndSlide()
        {
            sliding = false;
            chainTimer = 0f;          // letting go of Slide opens the same window as a landing
            timeSinceSlideEnd = 0f;
            TryStand();
        }

        void Jump()
        {
            bool slideJump = sliding || timeSinceSlideEnd < config.ChainWindow;
            bool chained = slideJump || chainTimer < config.ChainWindow;
            verticalVelocity = config.JumpVelocity;

            float speed = horizontal.magnitude;
            if (chained && speed > 0.01f)
            {
                var dir = horizontal / speed;
                // A slide-jump snaps momentum toward where you are looking: sharp turns out of slides.
                if (slideJump) dir = Momentum.Steer(dir, CameraForward(), config.SlideJumpTurn);
                speed += Momentum.ChainBoost(speed, config.HopBoost * StreakMultiplier, config.MoveSpeed, config.MaxSpeed);
                horizontal = dir * Mathf.Min(speed, config.MaxSpeed);
                streak.Extend();
                OnBoost();
            }
            if (sliding) EndSlide();

            coyote.Consume();
            jumpBuffer = 0f;
            chainTimer = float.MaxValue;        // one landing or slide grants one chained hop
            timeSinceSlideEnd = float.MaxValue;
            carryingMomentum = true;
            EventBus.Publish(new PlayerJumped(transform.position, chained));
        }

        void OnLanded(bool slideHeld, Vector3 wish)
        {
            chainTimer = 0f;
            if (slideHeld && !sliding) TryStartSlide(wish); // landing with Slide held chains into a slide
        }

        /// <summary>
        /// After a Move: works out whether we're on walkable ground or surfing, snaps down onto
        /// steps and ramps instead of falling off them, then clips velocity against every surface
        /// that isn't ground (walls, ceilings, too-steep slopes).
        /// Stairs are the tricky case: the capsule's rounded bottom touching a step edge reports a
        /// slanted normal, so a raycast confirms the real floor; and step risers below step-offset
        /// height are skipped while grounded, because the controller climbs them itself.
        /// Uses hit normals, not controller.velocity: comparing against the measured velocity
        /// ratcheted speed down from float noise at very high frame rates.
        /// </summary>
        void ResolveContacts(bool wasOnGround)
        {
            float walkable = WalkableNormalY;
            float feetY = FeetY;

            bool touchingBelow = controller.isGrounded;
            bool walkableContact = bestUpNormal >= walkable;
            float floorDistance = 0f, floorNormalY = 0f;
            bool floorBelow = ProbeFloor(out floorDistance, out floorNormalY) && floorNormalY >= walkable;
            onWalkableGround = touchingBelow && (walkableContact || (floorBelow && floorDistance <= controller.stepOffset + controller.skinWidth));

            // Walking off a step or down a ramp: stay glued to the ground instead of falling.
            if (!onWalkableGround && wasOnGround && verticalVelocity <= 0f && !wallRunning
                && floorBelow && floorDistance <= config.GroundSnapDistance)
            {
                controller.Move(Vector3.down * floorDistance);
                onWalkableGround = true;
                verticalVelocity = -2f;
            }
            // Any upward-facing contact too steep to stand on. Not gated on controller.isGrounded: the
            // controller often reports a steep face as a side hit, which dropped surfing mid-ramp.
            surfing = !onWalkableGround && bestUpNormal > 0.05f;
            if (surfing) surfNormal = bestUpContact;
            GroundIsFlat = onWalkableGround && floorBelow && floorNormalY > 0.98f;

            var velocity = horizontal + Vector3.up * verticalVelocity;
            for (int i = 0; i < contactCount; i++)
            {
                if (contacts[i].y >= walkable) continue;
                bool stepRiser = onWalkableGround && contactPoints[i].y - feetY <= controller.stepOffset + 0.05f;
                if (!stepRiser) velocity = Momentum.ClipVelocity(velocity, contacts[i]);
            }
            horizontal = new Vector3(velocity.x, 0f, velocity.z);
            verticalVelocity = velocity.y;

            contactCount = 0;
            bestUpNormal = -1f;
            bestUpContact = Vector3.zero;
            HorizontalSpeed = horizontal.magnitude;
        }

        /// <summary>True when standing on flat walkable ground. Step pops are only smoothed here, so ramps don't lag.</summary>
        bool GroundIsFlat { get; set; }

        float FeetY => transform.position.y + standingCenter.y - standingHeight * 0.5f;

        /// <summary>
        /// Straight-down ray from just above the feet: the floor's distance below the feet and its
        /// normal. Straight down sees a step's flat top, where the capsule's contact normal on the
        /// edge would be slanted.
        /// </summary>
        bool ProbeFloor(out float distanceBelowFeet, out float normalY)
        {
            const float above = 0.35f;
            var origin = new Vector3(transform.position.x, FeetY + above, transform.position.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, above + config.GroundSnapDistance + controller.skinWidth, ~0, QueryTriggerInteraction.Ignore))
            {
                distanceBelowFeet = Mathf.Max(0f, hit.distance - above);
                normalY = hit.normal.y;
                return true;
            }
            distanceBelowFeet = 0f;
            normalY = 0f;
            return false;
        }

        readonly Vector3[] contactPoints = new Vector3[8];
        Vector3 bestUpContact;

        /// <summary>
        /// Keeps surf settings on through brief contact gaps, and notices when you have really left
        /// the ramp (no ramp just beside you): then momentum turns toward
        /// where you look, so you leave the ramp the way you face instead of drifting with it.
        /// </summary>
        void UpdateSurfExit(bool grounded, float dt)
        {
            if (grounded) { surfTimer = 0f; return; }
            if (surfing) { surfTimer = config.SurfGrace; return; }
            if (surfTimer <= 0f) return;
            surfTimer -= dt;
            if (!RampBeside(surfNormal))
            {
                surfTimer = 0f;
                LeaveSurface(surfNormal);
            }
            else if (surfTimer < 0f) surfTimer = 0f; // still against it: no snap, just plain air rules
        }

        /// <summary>Whether the surface with <paramref name="normal"/> is still right against the capsule's bottom sphere.</summary>
        bool RampBeside(Vector3 normal)
        {
            var sphereCentre = new Vector3(transform.position.x, FeetY + controller.radius, transform.position.z);
            return Physics.Raycast(sphereCentre, -normal, controller.radius + controller.skinWidth + 0.2f, ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Leaving a surface (surf ramp, or a wall run ended without a jump): keep the speed, but
        /// turn up to LeaveSurfaceTurn toward where you look, never back into the surface.
        /// </summary>
        void LeaveSurface(Vector3 surfaceNormal)
        {
            float speed = horizontal.magnitude;
            if (speed < 0.01f) return;
            var away = new Vector3(surfaceNormal.x, 0f, surfaceNormal.z).normalized;
            var facing = CameraForward();
            float into = Vector3.Dot(facing, away);
            if (into < 0f) facing -= away * into; // looking into the surface: go along it instead
            if (facing.sqrMagnitude < 0.0001f) return;
            horizontal = Momentum.Steer(horizontal, facing, config.LeaveSurfaceTurn) * speed;
        }

        /// <summary>Collects every surface hit during this frame's Move, and the most upward-facing one.</summary>
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > bestUpNormal) { bestUpNormal = hit.normal.y; bestUpContact = hit.normal; }
            if (contactCount < contacts.Length)
            {
                contacts[contactCount] = hit.normal;
                contactPoints[contactCount] = hit.point;
                contactCount++;
            }
        }

        void TryStand()
        {
            if (!HeadroomClear()) { crouchBlocked = true; return; }
            crouchBlocked = false;
            SetLow(false);
        }

        void SetLow(bool low)
        {
            float height = low ? config.SlideHeight : standingHeight;
            float footOffset = standingCenter.y - standingHeight * 0.5f; // keep the feet where they are
            controller.height = height;
            controller.center = new Vector3(standingCenter.x, footOffset + height * 0.5f, standingCenter.z);
        }

        /// <summary>Whether a standing capsule fits here, ignoring the player's own colliders.</summary>
        bool HeadroomClear()
        {
            float r = controller.radius * 0.95f;
            float footOffset = standingCenter.y - standingHeight * 0.5f;
            var feet = transform.position + Vector3.up * footOffset;
            var bottom = feet + Vector3.up * (controller.radius + 0.05f);
            var top = feet + Vector3.up * (standingHeight - controller.radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, r, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (overlapBuffer[i] != controller && !overlapBuffer[i].transform.IsChildOf(transform)) return false;
            return true;
        }
    }
}
