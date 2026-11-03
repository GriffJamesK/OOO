using System;

namespace Kit.Gameplay.Audio
{
    // Seamless 8 s ambience loops, one per area. Noise layers are folded with LoopCrossfade,
    // tonal layers use whole cycles per loop, events wrap around the loop end, and the finished
    // loop is rotated to start on a zero crossing.
    public static partial class ProcSynth
    {
        /// <summary>
        /// Builds the ambience loop for an area id ("A1".."A7"; sub-zones such as "A7_Outside" use
        /// their area's prefix; anything else gets a low room tone). Deterministic per id.
        /// </summary>
        public static float[] Ambience(string areaId)
        {
            string id = AmbienceKey(areaId);
            int seed = StableHash("amb:" + id);
            int n = Samples(AmbienceSeconds);
            float[] loop;
            switch (id)
            {
                case "A1": loop = WaitingRoom(n, seed); break;
                case "A2": loop = AtriumAir(n, seed); break;
                case "A3": loop = OpenPlanBuzz(n, seed); break;
                case "A4": loop = HollowHall(n, seed); break;
                case "A5": loop = StairwellWind(n, seed); break;
                case "A6": loop = WarmHum(n, seed); break;
                case "A7": loop = OutsideWind(n, seed); break;
                default: loop = RoomTone(n, seed); break;
            }
            return RotateToZeroCrossing(loop);
        }

        /// <summary>Maps any zone id to the ambience it uses: "A1".."A7" by prefix, else "".</summary>
        public static string AmbienceKey(string areaId)
        {
            if (string.IsNullOrEmpty(areaId) || areaId.Length < 2) return "";
            char a = char.ToUpperInvariant(areaId[0]), d = areaId[1];
            if (a != 'A' || d < '1' || d > '7') return "";
            if (areaId.Length > 2 && char.IsDigit(areaId[2])) return "";
            return "A" + d;
        }

        /// <summary>
        /// A noise layer for a loop: n + 1 s of noise shaped by <paramref name="shape"/>, then
        /// crossfaded so it repeats seamlessly. A DC-blocking high-pass runs first, so the rotation
        /// to a zero crossing always finds one.
        /// </summary>
        static float[] NoiseLoop(int n, int seed, bool pink, Action<float[]> shape)
        {
            int f = Samples(1f);
            var src = pink ? PinkNoise(n + f, seed) : WhiteNoise(n + f, seed);
            shape(src);
            HighPass(src, 20f);
            return LoopCrossfade(src, n);
        }

        /// <summary>A naive sawtooth tuned to whole cycles per loop, softened by a periodic low-pass: the ballast buzz of old fluorescent tubes.</summary>
        static float[] LoopBuzz(int n, double hz, float cutoff)
        {
            double f = LoopFrequency(hz);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                double p = f * i / SampleRate;
                buf[i] = (float)(2.0 * (p - Math.Floor(p)) - 1.0);
            }
            PeriodicLowPass(buf, cutoff);
            PeriodicLowPass(buf, cutoff * 1.5f);
            return buf;
        }

        /// <summary>Multiplies a loop by 1 - depth + depth * (0.5 + 0.5 sin), with the rate snapped to whole cycles per loop.</summary>
        static void LoopTremolo(float[] loop, double hz, float depth)
        {
            double f = LoopFrequency(hz);
            for (int i = 0; i < loop.Length; i++)
                loop[i] *= 1f - depth + depth * (0.5f + 0.5f * Sin01(f * i / SampleRate));
        }

        /// <summary>A1 Waiting Room: 60 Hz fluorescent hum with harmonics, a faint ballast buzz and a waiting-room clock (tick, tock).</summary>
        static float[] WaitingRoom(int n, int seed)
        {
            var loop = NoiseLoop(n, seed, true, b => { LowPass(b, 400f); LowPass(b, 600f); });
            Normalize(loop, 0.12f);
            AddLoopTone(loop, 60, 0.2f);
            AddLoopTone(loop, 120, 0.13f);
            AddLoopTone(loop, 180, 0.06f);
            AddLoopTone(loop, 240, 0.04f);
            AddLoopTone(loop, 300, 0.02f);
            var buzz = LoopBuzz(n, 120, 2200f);
            LoopTremolo(buzz, 0.5, 0.4f);
            MixInto(loop, buzz, 0.035f, 0);

            int len = Samples(0.05f);
            for (int s = 0; s < 8; s++)
            {
                var tick = WhiteNoise(len, seed + 100 + s);
                HighPass(tick, 2000f);
                Multiply(tick, DecayEnvelope(len, 0.0003f, 0.002f));
                var ping = Sine(len, s % 2 == 0 ? 3100f : 2650f, 1f);
                Multiply(ping, DecayEnvelope(len, 0.0003f, 0.006f));
                MixInto(tick, ping, 0.4f, 0);
                FeedbackRoom(tick, 0.3f, 0.3f, 0.4f);
                MixIntoWrapped(loop, tick, 0.14f, Samples(0.3f + s));
            }
            Normalize(loop, 0.4f);
            return loop;
        }

        /// <summary>A2 Atrium: the vast space's air, a deep rumble under distant drifting HVAC with a faint motor tone.</summary>
        static float[] AtriumAir(int n, int seed)
        {
            var rumble = NoiseLoop(n, seed, true, b => { LowPass(b, 90f); LowPass(b, 120f); });
            Normalize(rumble, 0.5f);
            var hvac = NoiseLoop(n, seed + 1, false, b =>
            {
                LowPass(b, 700f);
                HighPass(b, 180f);
                Multiply(b, SlowRandom(b.Length, seed + 2, 0.6f));
            });
            Normalize(hvac, 0.12f);
            MixInto(rumble, hvac, 1f, 0);
            AddLoopTone(rumble, 87.5, 0.04f);
            AddLoopTone(rumble, 175, 0.015f);
            Normalize(rumble, 0.55f);
            return rumble;
        }

        /// <summary>A3 Open Plan: a stronger flickering fluorescent buzz and a faint high CRT whine over soft room air.</summary>
        static float[] OpenPlanBuzz(int n, int seed)
        {
            var loop = NoiseLoop(n, seed, true, b => { LowPass(b, 500f); });
            Normalize(loop, 0.12f);
            AddLoopTone(loop, 60, 0.1f);
            AddLoopTone(loop, 120, 0.08f);
            var buzz = LoopBuzz(n, 120, 3200f);
            LoopTremolo(buzz, 0.25, 0.3f);
            MixInto(loop, buzz, 0.11f, 0);
            var whine = Triangle(n, (float)LoopFrequency(7812.5), 1f);
            MixInto(loop, whine, 0.006f, 0);
            Normalize(loop, 0.5f);
            return loop;
        }

        /// <summary>A4 Cafeteria: a big hollow room tone, low noise through resonant delays so the hall itself rings faintly.</summary>
        static float[] HollowHall(int n, int seed)
        {
            var loop = NoiseLoop(n, seed, true, b =>
            {
                LowPass(b, 300f);
                HighPass(b, 40f);
                FeedbackRoom(b, 0.75f, 0.3f, 0.8f);
            });
            Normalize(loop, 0.45f);
            AddLoopTone(loop, 55, 0.025f);
            Normalize(loop, 0.5f);
            return loop;
        }

        /// <summary>A5 Stair Core: wind moving through a concrete shaft, gusting, its colour sweeping, ringing in the stairwell.</summary>
        static float[] StairwellWind(int n, int seed)
        {
            var loop = NoiseLoop(n, seed, true, b =>
            {
                var sweep = SlowRandom(b.Length, seed + 1, 0.4f);
                LowPassSweep(b, i => 250f + 650f * sweep[i]);
                Multiply(b, SlowRandom(b.Length, seed + 2, 0.5f));
                FeedbackRoom(b, 0.6f, 0.4f, 0.6f);
                HighPass(b, 60f);
            });
            Normalize(loop, 0.5f);
            return loop;
        }

        /// <summary>A6 Executive Wing: a quiet warm hum, low harmonics only, over carpet-deadened air.</summary>
        static float[] WarmHum(int n, int seed)
        {
            var loop = NoiseLoop(n, seed, true, b => { LowPass(b, 250f); LowPass(b, 400f); });
            Normalize(loop, 0.1f);
            AddLoopTone(loop, 60, 0.18f);
            AddLoopTone(loop, 120, 0.05f);
            Normalize(loop, 0.28f);
            return loop;
        }

        /// <summary>A7 Outside: wide, soft wind, slow gusts with a little high air on top.</summary>
        static float[] OutsideWind(int n, int seed)
        {
            var loop = NoiseLoop(n, seed, true, b =>
            {
                var sweep = SlowRandom(b.Length, seed + 1, 0.3f);
                LowPassSweep(b, i => 400f + 800f * sweep[i]);
                Multiply(b, SlowRandom(b.Length, seed + 2, 0.35f));
                HighPass(b, 80f);
            });
            Normalize(loop, 0.45f);
            var air = NoiseLoop(n, seed + 3, false, b => { LowPass(b, 4000f); HighPass(b, 1500f); });
            Normalize(air, 0.04f);
            MixInto(loop, air, 1f, 0);
            Normalize(loop, 0.5f);
            return loop;
        }

        /// <summary>Unknown area: a low room tone, so silence never gives away a gap in the zones.</summary>
        static float[] RoomTone(int n, int seed)
        {
            var loop = NoiseLoop(n, seed, true, b => { LowPass(b, 180f); HighPass(b, 25f); });
            Normalize(loop, 0.3f);
            AddLoopTone(loop, 60, 0.02f);
            Normalize(loop, 0.35f);
            return loop;
        }
    }
}
