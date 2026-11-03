using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay
{
    // Wall running. Airborne, holding a direction at >= WallRunMinSpeed, with a wall beside you
    // (not straight ahead): you attach, momentum is turned along the wall, and you rise at
    // WallRunUpSpeed under the weaker WallRunGravity, a gentle arc up then slowly down.
    // The run ends on release, on steering away, when the wall ends, when too slow, on landing,
    // or after WallRunMaxTime. Jump during the run (or within WallJumpGrace after it) is a wall
    // jump: you launch toward where you look (up to WallJumpAimTurn from the wall line, never into
    // it, and at least WallJumpPush away from it), and gain WallJumpBoost; air steering pauses for
    // WallJumpSteerLock so the kick reads, and it counts as a chain. Ending a run without jumping
    // turns you toward where you look too (LeaveSurface).
    // The same wall can't be re-grabbed for WallRunCooldown, but a different wall can, so
    // wall-to-wall chains work.
    public sealed partial class PlayerController
    {
        bool wallRunning;
        Vector3 wallNormal;
        Vector3 lastWallNormal;
        float wallSide;                         // +1 wall on the right, -1 on the left
        float wallRunTime;
        float wallGrace = float.MaxValue;       // since leaving a wall without jumping
        float wallCooldown;
        float airSteerLock;                     // > 0 right after a wall jump: no air steering

        /// <summary>True while running along a wall.</summary>
        public bool IsWallRunning => wallRunning;

        /// <summary>+1 if the wall being run is on the right, -1 on the left, 0 when not wall running.</summary>
        public float WallSide => wallRunning ? wallSide : 0f;

        bool CanWallJump => wallRunning || wallGrace < config.WallJumpGrace;

        void UpdateWallRun(Vector3 wish, bool hasInput, float dt)
        {
            wallGrace += dt;
            wallCooldown -= dt;

            if (!wallRunning)
            {
                TryStartWallRun(wish, hasInput);
                return;
            }

            wallRunTime += dt;
            float speed = Vector3.Dot(horizontal, Momentum.WallTangent(horizontal, wallNormal));
            // Only a sharp push away (input more than ~65 deg off the wall line) detaches. Looking
            // around while holding forward must not knock you off the wall.
            bool steeringAway = hasInput && Vector3.Dot(wish, wallNormal) > config.WallDetachInput;
            if (!hasInput || steeringAway || wallRunTime >= config.WallRunMaxTime
                || speed < config.WallRunMinSpeed * 0.6f || !ProbeWall(-wallNormal, out var hit))
            {
                EndWallRun();
                LeaveSurface(wallNormal); // drop off the wall the way you face, minus the push into it
                return;
            }
            wallNormal = Flat(hit.normal); // follow curved walls
        }

        void TryStartWallRun(Vector3 wish, bool hasInput)
        {
            float speed = horizontal.magnitude;
            if (!hasInput || speed < config.WallRunMinSpeed) return;

            var dir = horizontal / speed;
            var right = Vector3.Cross(Vector3.up, dir);
            // Sides first, then the front diagonals: a wall approached at an angle (the usual
            // wall-to-wall jump) is seen before contact, not only once the capsule touches it.
            float maxApproach = Mathf.Sin(config.WallRunMaxApproachAngle * Mathf.Deg2Rad);
            for (int i = 0; i < 4; i++)
            {
                var probe = i switch { 0 => right, 1 => -right, 2 => (dir + right).normalized, _ => (dir - right).normalized };
                if (!ProbeWall(probe, out var hit)) continue;
                var n = Flat(hit.normal);
                if (wallCooldown > 0f && lastWallNormal != Vector3.zero && Vector3.Angle(n, lastWallNormal) < 30f) continue;
                if (Vector3.Dot(dir, -n) > maxApproach) continue; // too close to head-on: a collision, not a run
                StartWallRun(n, Vector3.Dot(right, -n) >= 0f ? 1f : -1f, speed);
                return;
            }
        }

        void StartWallRun(Vector3 normal, float side, float speed)
        {
            wallRunning = true;
            wallNormal = normal;
            wallSide = side;
            wallRunTime = 0f;
            horizontal = Momentum.WallTangent(horizontal, normal) * speed;
            verticalVelocity = config.WallRunUpSpeed; // start of the gentle arc
            carryingMomentum = true;
            EventBus.Publish(new PlayerWallRan(transform.position, side, speed));
        }

        void WallRunMove(float dt)
        {
            var along = Momentum.WallTangent(horizontal, wallNormal);
            float speed = Mathf.MoveTowards(Vector3.Dot(horizontal, along), 0f, config.WallRunFriction * dt);
            // A slight push into the wall keeps contact on curves; collisions remove it on flat walls.
            horizontal = along * speed - wallNormal * 0.5f;
        }

        void EndWallRun()
        {
            if (!wallRunning) return;
            wallRunning = false;
            wallGrace = 0f;
            lastWallNormal = wallNormal;
            wallCooldown = config.WallRunCooldown;
        }

        void WallJump()
        {
            var normal = wallRunning ? wallNormal : lastWallNormal;
            EndWallRun();

            var flat = new Vector3(horizontal.x, 0f, horizontal.z);
            float boost = Momentum.ChainBoost(flat.magnitude, config.WallJumpBoost * StreakMultiplier, config.MoveSpeed, config.MaxSpeed);
            horizontal = Vector3.ClampMagnitude(
                Momentum.WallJump(flat, normal, config.WallJumpPush, boost, CameraForward(), config.WallJumpAimTurn), config.MaxSpeed);
            verticalVelocity = config.JumpVelocity * config.WallJumpUpFactor;
            airSteerLock = config.WallJumpSteerLock;

            streak.Extend();
            OnBoost();
            coyote.Consume();
            jumpBuffer = 0f;
            wallGrace = float.MaxValue;
            chainTimer = float.MaxValue;
            carryingMomentum = true;
            EventBus.Publish(new PlayerJumped(transform.position, chained: true));
        }

        /// <summary>Raycast from the capsule centre for a near-vertical wall in <paramref name="direction"/>.</summary>
        bool ProbeWall(Vector3 direction, out RaycastHit hit)
        {
            var origin = transform.position + Vector3.up * controller.center.y;
            float distance = controller.radius + config.WallCheckDistance;
            // The ray starts inside our own capsule, and rays ignore colliders they start inside.
            return Physics.Raycast(origin, direction, out hit, distance, ~0, QueryTriggerInteraction.Ignore)
                   && Mathf.Abs(hit.normal.y) < 0.3f;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z).normalized;

        void ResetWallRun()
        {
            airSteerLock = 0f;
            wallRunning = false;
            wallGrace = float.MaxValue;
            lastWallNormal = Vector3.zero;
            wallCooldown = 0f;
        }
    }
}
