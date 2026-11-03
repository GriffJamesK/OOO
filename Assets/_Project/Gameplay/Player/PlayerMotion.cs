using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>
    /// Acceleration/deceleration ramp. Kept as a pure struct, separate from the MonoBehaviour,
    /// so edit-mode tests can step it with a manual delta time.
    /// </summary>
    public struct SpeedRamp
    {
        /// <summary>0 = stopped, 1 = full speed. Moves linearly; the config curves shape the output.</summary>
        public float Progress { get; private set; }
        public bool Accelerating { get; private set; }

        /// <summary>Advances the ramp and returns the speed fraction (0..1) for this frame.</summary>
        public float Step(bool hasInput, float accelTime, float decelTime, AnimationCurve accelCurve, AnimationCurve decelCurve, float dt)
        {
            Accelerating = hasInput;
            float duration = hasInput ? accelTime : decelTime;
            float target = hasInput ? 1f : 0f;
            Progress = duration <= 0f ? target : Mathf.MoveTowards(Progress, target, dt / duration);
            return Evaluate(hasInput ? accelCurve : decelCurve, Progress);
        }

        /// <summary>
        /// Sets progress so the curve outputs <paramref name="fraction"/>. Used when momentum (from a
        /// slide or hop) drops back to walking pace, so the ramp picks up exactly where speed is
        /// instead of snapping to zero or full speed. Assumes the curve rises monotonically.
        /// </summary>
        public void SnapToFraction(float fraction, AnimationCurve curve)
        {
            fraction = Mathf.Clamp01(fraction);
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 16; i++)
            {
                float mid = (lo + hi) * 0.5f;
                if (Evaluate(curve, mid) < fraction) lo = mid; else hi = mid;
            }
            Progress = (lo + hi) * 0.5f;
        }

        static float Evaluate(AnimationCurve curve, float t) =>
            curve != null && curve.length > 0 ? Mathf.Clamp01(curve.Evaluate(t)) : t;
    }

    /// <summary>
    /// Coyote time: Jump is still allowed for a short grace period after leaving the ground,
    /// which makes ledge jumps feel fair. Pure struct for testing.
    /// </summary>
    public struct CoyoteTimer
    {
        float remaining;

        public bool CanJump => remaining > 0f;

        /// <summary>Call once per frame with the controller's grounded state.</summary>
        public void Tick(bool grounded, float coyoteTime, float dt)
        {
            remaining = grounded ? coyoteTime : remaining - dt;
        }

        /// <summary>Spends the grace period so one ledge gives one jump.</summary>
        public void Consume() => remaining = 0f;
    }

    /// <summary>
    /// Counts consecutive chained jumps. The longer a chain runs, the bigger each boost gets, so
    /// sustained slide-jumping keeps picking up speed instead of levelling off. Pure for testing.
    /// </summary>
    public struct ChainStreak
    {
        public int Count { get; private set; }

        /// <summary>Boost multiplier for the next chain: 1 + perChain x streak (streak capped at maxChains).</summary>
        public float Multiplier(float perChain, int maxChains) => 1f + perChain * Mathf.Min(Count, Mathf.Max(0, maxChains));

        public void Extend() => Count++;
        public void Break() => Count = 0;
    }

    /// <summary>
    /// Momentum maths for slides and hop chains, kept pure so the speed-gain rules are testable
    /// without a scene.
    /// </summary>
    public static class Momentum
    {
        /// <summary>
        /// Speed added by a chained slide or hop. Full <paramref name="boost"/> at walking pace,
        /// shrinking linearly to zero at <paramref name="maxSpeed"/>, so chaining always pays but
        /// can never run away.
        /// </summary>
        public static float ChainBoost(float speed, float boost, float walkSpeed, float maxSpeed)
        {
            if (maxSpeed <= walkSpeed) return 0f;
            float headroom = Mathf.Clamp01((maxSpeed - speed) / (maxSpeed - walkSpeed));
            return Mathf.Min(boost * headroom, Mathf.Max(0f, maxSpeed - speed));
        }

        /// <summary>
        /// Source-engine air acceleration (PM_AirAccelerate). Speed can only be added along
        /// <paramref name="wishDir"/>, and only until the velocity's component along it reaches
        /// <paramref name="cap"/>. So holding forward at speed adds nothing, while strafing and
        /// turning the view keeps the wish direction nearly perpendicular to the velocity and adds a
        /// little speed every step: air strafing. Horizontal only.
        /// </summary>
        public static Vector3 AirAccelerate(Vector3 velocity, Vector3 wishDir, float wishSpeed, float accelerate, float cap, float dt)
        {
            if (wishDir.sqrMagnitude < 0.0001f || wishSpeed <= 0f) return velocity;
            float current = Vector3.Dot(velocity, wishDir);
            float add = Mathf.Min(wishSpeed, cap) - current;
            if (add <= 0f) return velocity;
            float step = Mathf.Min(accelerate * wishSpeed * dt, add);
            return velocity + wishDir * step;
        }

        /// <summary>
        /// Source-engine ClipVelocity: removes the part of <paramref name="velocity"/> pushing into
        /// a surface. Slide along walls, stop at ceilings, and on a ramp too steep to stand on,
        /// turn a fall into a slide along it (surfing).
        /// </summary>
        public static Vector3 ClipVelocity(Vector3 velocity, Vector3 normal)
        {
            float into = Vector3.Dot(velocity, normal);
            return into < 0f ? velocity - normal * into : velocity;
        }

        /// <summary>Turns a horizontal direction toward <paramref name="wish"/> by at most maxDegrees, keeping it flat.</summary>
        public static Vector3 Steer(Vector3 direction, Vector3 wish, float maxDegrees)
        {
            if (wish.sqrMagnitude < 0.0001f) return direction;
            if (direction.sqrMagnitude < 0.0001f) return wish.normalized;
            var turned = Vector3.RotateTowards(direction.normalized, wish.normalized, maxDegrees * Mathf.Deg2Rad, 0f);
            turned.y = 0f;
            return turned.normalized;
        }

        /// <summary>
        /// Flat direction along a wall, on the side of <paramref name="direction"/>. Wall runs move
        /// along this so the player hugs the wall instead of drifting into or away from it.
        /// </summary>
        public static Vector3 WallTangent(Vector3 direction, Vector3 wallNormal)
        {
            var n = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
            var along = Vector3.Cross(Vector3.up, n);            // one of the two flat tangents
            if (Vector3.Dot(along, direction) < 0f) along = -along;
            return along.normalized;
        }

        /// <summary>
        /// Horizontal velocity after a wall jump. You launch toward <paramref name="aim"/> (turned at
        /// most <paramref name="maxAimTurn"/> degrees from the run direction, never into the wall), so
        /// you leave the way you look. If that would hug the wall, the launch is instead the run
        /// direction tilted away by <paramref name="push"/> (as a velocity ratio), so you always clear
        /// it. Total speed = speed + boost: aim and push only set the take-off direction.
        /// </summary>
        public static Vector3 WallJump(Vector3 horizontal, Vector3 wallNormal, float push, float boost, Vector3 aim, float maxAimTurn)
        {
            float speed = horizontal.magnitude;
            var n = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
            var along = speed > 0.01f ? WallTangent(horizontal, n) : Vector3.zero;
            var dir = along;
            if (along != Vector3.zero && aim.sqrMagnitude > 0.0001f)
            {
                dir = Steer(along, aim, maxAimTurn);
                if (Vector3.Dot(dir, n) < 0f) dir = along; // aiming into the wall: just run along it
            }
            var minimum = (along * Mathf.Max(speed, 0.01f) + n * push).normalized;
            var launch = Vector3.Dot(dir, n) >= Vector3.Dot(minimum, n) ? dir : minimum;
            return launch * (speed + boost);
        }

        /// <summary>
        /// FOV offset for a horizontal speed: 0 at walk speed, <paramref name="maxKick"/> at max
        /// speed, shaped by <paramref name="curve"/> (x = speed fraction, y = kick fraction).
        /// </summary>
        public static float SpeedFovKick(float speed, float walkSpeed, float maxSpeed, float maxKick, AnimationCurve curve)
        {
            float t = maxSpeed > walkSpeed ? Mathf.Clamp01((speed - walkSpeed) / (maxSpeed - walkSpeed)) : 0f;
            float shaped = curve != null && curve.length > 0 ? Mathf.Clamp01(curve.Evaluate(t)) : t;
            return maxKick * shaped;
        }
    }
}
