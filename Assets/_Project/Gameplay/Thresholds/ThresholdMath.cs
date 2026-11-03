using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>Which way a segment went through a threshold plane.</summary>
    public enum CrossingSide
    {
        None,
        FrontToBack,
        BackToFront
    }

    /// <summary>
    /// Geometry of threshold pairs, pure. Convention: "front in, front out". PairMap(a, b) =
    /// b.localToWorld * R(180 deg about local up) * a.worldToLocal, so crossing A front-to-back
    /// comes out in front of B moving away from it, and back-to-front comes out behind B.
    /// </summary>
    public static class ThresholdMath
    {
        static readonly Matrix4x4 HalfTurn = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));

        /// <summary>
        /// Rigid map from opening A's space to opening B's space (see class summary). The half turn
        /// about local up is what makes "into A's front" become "out of B's front": without it a
        /// player would come out walking into B's back face.
        /// </summary>
        public static Matrix4x4 PairMap(Pose a, Pose b)
        {
            var aToWorld = Matrix4x4.TRS(a.position, a.rotation, Vector3.one);
            var bToWorld = Matrix4x4.TRS(b.position, b.rotation, Vector3.one);
            return bToWorld * HalfTurn * aToWorld.inverse;
        }

        /// <summary>
        /// Did the segment from <paramref name="from"/> to <paramref name="to"/> pass through the
        /// rectangle of <paramref name="size"/> (width, height) centred on <paramref name="opening"/>,
        /// whose forward is the front normal? A point exactly on the plane counts as the front side
        /// (not yet crossed when going in, already out when coming back).
        /// </summary>
        public static CrossingSide Crossing(Pose opening, Vector2 size, Vector3 from, Vector3 to)
        {
            return Crossing(opening, size, from, to, out _);
        }

        /// <summary>
        /// As the overload without <paramref name="along"/>, also giving where along the segment
        /// (0 = from, 1 = to) the plane was hit, so a caller testing several openings can take the
        /// one the player actually reached first.
        /// </summary>
        public static CrossingSide Crossing(Pose opening, Vector2 size, Vector3 from, Vector3 to, out float along)
        {
            along = 0f;
            Vector3 normal = opening.rotation * Vector3.forward;
            float d0 = Vector3.Dot(from - opening.position, normal);
            float d1 = Vector3.Dot(to - opening.position, normal);
            bool frontBefore = d0 >= 0f;
            bool frontAfter = d1 >= 0f;
            if (frontBefore == frontAfter) return CrossingSide.None;

            // The signs differ, so d0 - d1 can't be zero.
            along = d0 / (d0 - d1);
            Vector3 hit = Vector3.LerpUnclamped(from, to, along);
            Vector3 local = Quaternion.Inverse(opening.rotation) * (hit - opening.position);
            if (Mathf.Abs(local.x) > size.x * 0.5f || Mathf.Abs(local.y) > size.y * 0.5f) return CrossingSide.None;
            return frontBefore ? CrossingSide.FrontToBack : CrossingSide.BackToFront;
        }

        /// <summary>Signed distance of a point from the opening's plane (positive = in front).</summary>
        public static float SignedDistance(Pose opening, Vector3 point)
        {
            return Vector3.Dot(point - opening.position, opening.rotation * Vector3.forward);
        }

        /// <summary>
        /// Squared distance from a point to the closest point of a segment. Used as the broad phase
        /// so a fast fall that starts and ends far from a hatch still finds it.
        /// </summary>
        public static float SqrDistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 seg = to - from;
            float lenSq = seg.sqrMagnitude;
            float t = lenSq > 1e-12f ? Mathf.Clamp01(Vector3.Dot(point - from, seg) / lenSq) : 0f;
            return (from + seg * t - point).sqrMagnitude;
        }
    }
}
