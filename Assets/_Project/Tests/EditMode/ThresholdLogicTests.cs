using Kit.Gameplay.Thresholds;
using NUnit.Framework;
using UnityEngine;

namespace Kit.Tests.EditMode
{
    /// <summary>
    /// The building's one rule, as pure logic (GDD F2, F4, F7): how plates are read, when two
    /// thresholds pair, how a pair maps space ("front in, front out"), and when a segment crosses.
    /// </summary>
    public class ThresholdLogicTests
    {
        [TestCase("104", "104")]
        [TestCase("1O4", "104")]
        [TestCase("ooo", "000")]
        [TestCase("O0o", "000")]
        [TestCase(" 3 7 ", "37")]
        [TestCase("_3_7", "37")]
        [TestCase("___", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("a1", "A1")]
        public void Normalize_ReadsPlatesLikeTheBuilding(string raw, string expected)
        {
            Assert.AreEqual(expected, ThresholdNumber.Normalize(raw));
        }

        [Test]
        public void ExactlyTwoMatchingNumbers_Pair()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 }, ThresholdPairing.Resolve(new[] { "12", "12" }));
            CollectionAssert.AreEqual(new[] { 2, -1, 0, -1 }, ThresholdPairing.Resolve(new[] { "7", "8", "7", "9" }));
        }

        [Test]
        public void OneOrThreeOrMore_NeverPair()
        {
            CollectionAssert.AreEqual(new[] { -1 }, ThresholdPairing.Resolve(new[] { "5" }));
            CollectionAssert.AreEqual(new[] { -1, -1, -1 }, ThresholdPairing.Resolve(new[] { "5", "5", "5" }));
            CollectionAssert.AreEqual(new[] { -1, -1, -1, -1 }, ThresholdPairing.Resolve(new[] { "5", "5", "5", "5" }));
        }

        [Test]
        public void EmptyNumbers_NeverPair()
        {
            CollectionAssert.AreEqual(new[] { -1, -1 }, ThresholdPairing.Resolve(new[] { "", "___" }));
        }

        [Test]
        public void LetterO_PairsWithZero()
        {
            CollectionAssert.AreEqual(new[] { 1, 0 }, ThresholdPairing.Resolve(new[] { "1O4", "104" }));
            CollectionAssert.AreEqual(new[] { 1, 0 }, ThresholdPairing.Resolve(new[] { "OOO", "000" }));
        }

        [Test]
        public void PairMap_FrontIn_FrontOut()
        {
            var a = new Pose(new Vector3(0f, 1f, 10f), Quaternion.LookRotation(Vector3.back));   // front faces -Z
            var b = new Pose(new Vector3(50f, 1f, 0f), Quaternion.LookRotation(Vector3.right));  // front faces +X
            var map = ThresholdMath.PairMap(a, b);

            // Just behind A (crossed front-to-back) comes out just in front of B.
            Vector3 behindA = a.position - a.forward * 0.1f;
            Vector3 mapped = map.MultiplyPoint3x4(behindA);
            Assert.Less(Vector3.Distance(mapped, b.position + b.forward * 0.1f), 0.001f, $"mapped to {mapped}");

            // Moving into A's front (along -A.forward) comes out moving along B.forward.
            Vector3 dir = map.MultiplyVector(-a.forward);
            Assert.Less(Vector3.Angle(dir, b.forward), 0.01f);

            // Up stays up for upright openings, and the map is rigid.
            Assert.Less(Vector3.Angle(map.MultiplyVector(Vector3.up), Vector3.up), 0.01f);
            Assert.AreEqual(1f, map.MultiplyVector(Vector3.right).magnitude, 0.0001f);

            // A point to A's right comes out to B's left (the 180 deg turn about up).
            Vector3 rightOfA = a.position + a.rotation * Vector3.right * 0.4f - a.forward * 0.1f;
            Vector3 mappedRight = map.MultiplyPoint3x4(rightOfA);
            Vector3 expected = b.position + b.rotation * Vector3.left * 0.4f + b.forward * 0.1f;
            Assert.Less(Vector3.Distance(mappedRight, expected), 0.001f);
        }

        [Test]
        public void PairMap_FloorHatchToTiltedWall_TurnsAFallIntoALaunch()
        {
            var hatch = new Pose(Vector3.zero, Quaternion.LookRotation(Vector3.up, Vector3.forward));
            var exitForward = new Vector3(Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad), 0f);
            var exit = new Pose(new Vector3(100f, 20f, 0f), Quaternion.LookRotation(exitForward, Vector3.up));
            var map = ThresholdMath.PairMap(hatch, exit);
            Assert.Less(Vector3.Angle(map.MultiplyVector(Vector3.down), exitForward), 0.01f, "Falling into the hatch leaves along the exit's front.");
        }

        [Test]
        public void Crossing_DetectsDirection_InsideTheRectangleOnly()
        {
            var opening = new Pose(Vector3.zero, Quaternion.identity); // front faces +Z
            var size = new Vector2(1f, 2f);
            Assert.AreEqual(CrossingSide.FrontToBack, ThresholdMath.Crossing(opening, size, new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, -0.3f)));
            Assert.AreEqual(CrossingSide.BackToFront, ThresholdMath.Crossing(opening, size, new Vector3(0f, 0.5f, -0.3f), new Vector3(0f, 0.5f, 0.3f)));
            Assert.AreEqual(CrossingSide.None, ThresholdMath.Crossing(opening, size, new Vector3(0.8f, 0f, 0.3f), new Vector3(0.8f, 0f, -0.3f)), "outside the width");
            Assert.AreEqual(CrossingSide.None, ThresholdMath.Crossing(opening, size, new Vector3(0f, 1.2f, 0.3f), new Vector3(0f, 1.2f, -0.3f)), "above the head");
            Assert.AreEqual(CrossingSide.None, ThresholdMath.Crossing(opening, size, new Vector3(0f, 0f, 0.3f), new Vector3(0f, 0f, 0.1f)), "didn't reach the plane");
            Assert.AreEqual(CrossingSide.FrontToBack, ThresholdMath.Crossing(opening, size, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, -0.3f)), "on the plane counts as the front");
        }
    }
}
