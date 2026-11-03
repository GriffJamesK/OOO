using System;
using System.Collections.Generic;
using Kit.Gameplay.Audio;
using NUnit.Framework;

namespace Kit.Tests.EditMode
{
    /// <summary>
    /// The procedural placeholder audio (ProcSynth) as pure data: every sound has its declared
    /// length, holds only finite samples, is audible but never clips, one-shots start and end
    /// silent, ambience loops start and end at zero and wrap seamlessly, and the same seed always
    /// gives the same samples.
    /// </summary>
    public class AudioTests
    {
        static IEnumerable<SoundId> AllSounds() => (SoundId[])Enum.GetValues(typeof(SoundId));
        static IEnumerable<string> AllAmbiences() => ProcSynth.AmbienceIds;

        /// <summary>Fails on the first NaN or Infinity, naming the sample.</summary>
        static void AssertFinite(float[] buf, string name)
        {
            for (int i = 0; i < buf.Length; i++)
                Assert.IsFalse(float.IsNaN(buf[i]) || float.IsInfinity(buf[i]), $"{name}: sample {i} is {buf[i]}");
        }

        static void AssertPeak(float[] buf, string name)
        {
            float peak = ProcSynth.Peak(buf);
            Assert.Greater(peak, 0.05f, $"{name}: too quiet (peak {peak})");
            Assert.LessOrEqual(peak, 1f, $"{name}: clips (peak {peak})");
        }

        [TestCaseSource(nameof(AllSounds))]
        public void OneShot_HasExpectedLength_IsFinite_AndPeaksInRange(SoundId id)
        {
            var buf = ProcSynth.Build(id);
            Assert.AreEqual(ProcSynth.Samples(ProcSynth.Seconds(id)), buf.Length, $"{id}: length");
            AssertFinite(buf, id.ToString());
            AssertPeak(buf, id.ToString());
        }

        [TestCaseSource(nameof(AllSounds))]
        public void OneShot_StartsAndEndsSilent(SoundId id)
        {
            var buf = ProcSynth.Build(id);
            Assert.Less(Math.Abs(buf[0]), 0.001f, $"{id}: first sample");
            Assert.Less(Math.Abs(buf[buf.Length - 1]), 0.001f, $"{id}: last sample");
        }

        [TestCaseSource(nameof(AllAmbiences))]
        public void Ambience_HasExpectedLength_IsFinite_AndPeaksInRange(string id)
        {
            var buf = ProcSynth.Ambience(id);
            Assert.AreEqual(ProcSynth.Samples(ProcSynth.AmbienceSeconds), buf.Length, $"'{id}': length");
            AssertFinite(buf, id);
            AssertPeak(buf, id);
        }

        [TestCaseSource(nameof(AllAmbiences))]
        public void Ambience_StartsAndEndsNearZero_AndWrapsWithoutAJump(string id)
        {
            var buf = ProcSynth.Ambience(id);
            float first = buf[0], last = buf[buf.Length - 1];
            Assert.Less(Math.Abs(first), 0.02f, $"'{id}': first sample {first}");
            Assert.Less(Math.Abs(last), 0.02f, $"'{id}': last sample {last}");
            Assert.Less(Math.Abs(first - last), 0.03f, $"'{id}': wrap jump {first - last}");
        }

        [Test]
        public void SameSeed_SameBuffer_DifferentSeed_DifferentBuffer()
        {
            CollectionAssert.AreEqual(ProcSynth.WhiteNoise(512, 42), ProcSynth.WhiteNoise(512, 42));
            CollectionAssert.AreEqual(ProcSynth.PinkNoise(512, 42), ProcSynth.PinkNoise(512, 42));
            CollectionAssert.AreNotEqual(ProcSynth.WhiteNoise(512, 42), ProcSynth.WhiteNoise(512, 43));
        }

        [Test]
        public void Sounds_AreDeterministic()
        {
            foreach (var id in AllSounds())
                CollectionAssert.AreEqual(ProcSynth.Build(id), ProcSynth.Build(id), id.ToString());
            CollectionAssert.AreEqual(ProcSynth.Ambience("A1"), ProcSynth.Ambience("A1"));
            CollectionAssert.AreEqual(ProcSynth.Ambience(""), ProcSynth.Ambience(""));
        }

        [Test]
        public void FootstepVariations_Differ()
        {
            CollectionAssert.AreNotEqual(ProcSynth.Build(SoundId.StepConcrete0), ProcSynth.Build(SoundId.StepConcrete1));
            CollectionAssert.AreNotEqual(ProcSynth.Build(SoundId.StepCarpet1), ProcSynth.Build(SoundId.StepCarpet2));
        }

        [TestCase("A1", "A1")]
        [TestCase("A7_Outside", "A7")]
        [TestCase("a3", "A3")]
        [TestCase("A0", "")]
        [TestCase("A8", "")]
        [TestCase("A12", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        [TestCase("Lobby", "")]
        public void AmbienceKey_MapsZonesToAreas(string zone, string expected)
        {
            Assert.AreEqual(expected, ProcSynth.AmbienceKey(zone));
        }

        [Test]
        public void LoopCrossfade_WrapsSeamlessly()
        {
            int n = 1000;
            var src = new float[n + 200];
            for (int i = 0; i < src.Length; i++) src[i] = i * 0.001f; // a ramp: the tail continues the end
            var loop = ProcSynth.LoopCrossfade(src, n);
            Assert.AreEqual(n, loop.Length);
            Assert.AreEqual(src[n], loop[0], 1e-5f, "loop start continues straight on from the loop end");
            Assert.AreEqual(src[n - 1], loop[n - 1], 1e-6f);
        }
    }
}
