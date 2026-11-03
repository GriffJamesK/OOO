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
        /// <summary>Rigid map from opening A's space to opening B's space (see class summary).</summary>
        public static Matrix4x4 PairMap(Pose a, Pose b)
        {
            // STUB: implemented by the Thresholds worker. See ThresholdLogicTests.
            return Matrix4x4.identity;
        }

        /// <summary>
        /// Did the segment from <paramref name="from"/> to <paramref name="to"/> pass through the
        /// rectangle of <paramref name="size"/> (width, height) centred on <paramref name="opening"/>,
        /// whose forward is the front normal? A point exactly on the plane counts as not yet crossed.
        /// </summary>
        public static CrossingSide Crossing(Pose opening, Vector2 size, Vector3 from, Vector3 to)
        {
            // STUB: implemented by the Thresholds worker. See ThresholdLogicTests.
            return CrossingSide.None;
        }
    }
}
