using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// Lives on the player (added by ThresholdSystem on PlayerSpawned). On PlayerController.Moved it
    /// tests the segment from the previous to the current capsule centre against every paired
    /// threshold nearby; a crossing calls PlayerController.TeleportThrough and publishes
    /// PlayerCrossedThreshold. At most one teleport per frame, so a pair can't ping-pong.
    /// </summary>
    public sealed class ThresholdTraveller : MonoBehaviour
    {
        // How close (m) to the exit plane a post-teleport start point may be and still be treated
        // as "just came out of it". Not a design tunable: it only absorbs float error in the pair
        // map (about 1e-5 m at 200 m from the origin), so it lives here rather than in the config.
        const float ExitPlaneTolerance = 1e-3f;

        PlayerController player;
        CharacterController body;
        Threshold justExited;

        /// <summary>The player this traveller is bound to.</summary>
        public PlayerController Player => player;

        /// <summary>
        /// Attaches to a player. Called by ThresholdSystem on every PlayerSpawned (respawns too), so
        /// it is idempotent: re-binding the same player never double-subscribes.
        /// </summary>
        public void Bind(PlayerController target)
        {
            if (player != null) player.Moved -= OnMoved;
            player = target;
            body = target != null ? target.GetComponent<CharacterController>() : null;
            justExited = null;
            if (player != null) player.Moved += OnMoved;
        }

        void OnDestroy()
        {
            if (player != null) player.Moved -= OnMoved;
            player = null;
            body = null;
            justExited = null;
        }

        /// <summary>
        /// The player finished this frame's move. Tests the capsule centre's path against nearby
        /// paired thresholds and teleports through the first one it reached. The segment uses the
        /// current capsule offset at both ends, so a slide lowering the capsule mid-frame doesn't
        /// read as a vertical move.
        /// </summary>
        void OnMoved(PlayerController p, Vector3 previousPosition)
        {
            if (p == null || p != player) return;
            var cfg = ConfigLookup.Get<ThresholdConfig>();
            Vector3 offset = p.transform.rotation * (body != null ? body.center : Vector3.zero);
            Vector3 from = previousPosition + offset;
            Vector3 to = p.transform.position + offset;

            // previousPosition after a teleport is the exit point itself, a hair from the partner's
            // plane. If float error put it on the wrong side, this frame's segment would "cross"
            // the partner straight back. Ignore the exit threshold for this one frame when the
            // segment starts on its plane; a real return crossing starts further away.
            var exited = justExited;
            justExited = null;

            float searchSq = cfg.CrossingSearchRadius * cfg.CrossingSearchRadius;
            Threshold entry = null;
            float firstAlong = float.MaxValue;
            var paired = ThresholdRegistry.Paired;
            for (int i = 0; i < paired.Count; i++)
            {
                var t = paired[i];
                if (t == null || t.Partner == null || !t.isActiveAndEnabled) continue;
                var pose = t.Pose;
                if (ThresholdMath.SqrDistanceToSegment(pose.position, from, to) > searchSq) continue;
                if (ReferenceEquals(t, exited) && Mathf.Abs(ThresholdMath.SignedDistance(pose, from)) < ExitPlaneTolerance) continue;
                if (ThresholdMath.Crossing(pose, t.Size, from, to, out float along) == CrossingSide.None) continue;
                if (along >= firstAlong) continue;
                firstAlong = along;
                entry = t;
            }
            if (entry == null) return;

            var exit = entry.Partner;
            p.TeleportThrough(entry.PairMap, cfg.MaxExitSpeed);
            justExited = exit;
            EventBus.Publish(new PlayerCrossedThreshold(entry, exit, p.Velocity.magnitude));
        }
    }
}
