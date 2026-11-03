using System;

namespace Kit.Gameplay.Audio
{
    // Building blocks: noise, oscillators, envelopes, filters, a small feedback-delay room and
    // loop helpers. All operate on plain float buffers at SampleRate.
    public static partial class ProcSynth
    {
        /// <summary>
        /// xorshift32: tiny, fast and identical everywhere, unlike System.Random whose algorithm
        /// may differ between runtimes. Determinism is what lets tests pin buffers down.
        /// </summary>
        public struct Rng
        {
            uint state;

            public Rng(int seed)
            {
                state = (uint)seed * 2654435761u ^ 0x9E3779B9u;
                if (state == 0) state = 0x6D2B79F5u;
            }

            /// <summary>Next uniform value in [-1, 1).</summary>
            public float NextSigned()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return (state >> 8) * (2f / 16777216f) - 1f;
            }
        }

        const int SineTableSize = 4096;
        static readonly float[] sineTable = BuildSineTable();

        /// <summary>One sine cycle, plus a guard sample so interpolation never wraps the index.</summary>
        static float[] BuildSineTable()
        {
            var t = new float[SineTableSize + 1];
            for (int i = 0; i <= SineTableSize; i++) t[i] = (float)Math.Sin(2.0 * Math.PI * i / SineTableSize);
            return t;
        }

        /// <summary>sin(2 pi phase), phase in cycles, by table lookup. Several times faster than Math.Sin, which matters for the chord and the hums.</summary>
        public static float Sin01(double phase)
        {
            double frac = phase - Math.Floor(phase);
            double x = frac * SineTableSize;
            int i = (int)x;
            if (i >= SineTableSize) i = SineTableSize - 1;
            float f = (float)(x - i);
            return sineTable[i] + (sineTable[i + 1] - sineTable[i]) * f;
        }

        /// <summary>A deterministic string hash (string.GetHashCode may be randomised per process).</summary>
        public static int StableHash(string s)
        {
            unchecked
            {
                int h = (int)2166136261;
                if (s != null) foreach (char c in s) h = (h ^ c) * 16777619;
                return h;
            }
        }

        // ---------------------------------------------------------------- sources

        /// <summary>Seeded white noise in [-1, 1).</summary>
        public static float[] WhiteNoise(int n, int seed)
        {
            var rng = new Rng(seed);
            var buf = new float[n];
            for (int i = 0; i < n; i++) buf[i] = rng.NextSigned();
            return buf;
        }

        /// <summary>Seeded pink noise (Paul Kellet's filter): softer and more natural than white, the base of air, wind and cloth.</summary>
        public static float[] PinkNoise(int n, int seed)
        {
            var rng = new Rng(seed);
            var buf = new float[n];
            float b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;
            for (int i = 0; i < n; i++)
            {
                float w = rng.NextSigned();
                b0 = 0.99886f * b0 + w * 0.0555179f;
                b1 = 0.99332f * b1 + w * 0.0750759f;
                b2 = 0.96900f * b2 + w * 0.1538520f;
                b3 = 0.86650f * b3 + w * 0.3104856f;
                b4 = 0.55000f * b4 + w * 0.5329522f;
                b5 = -0.7616f * b5 - w * 0.0168980f;
                buf[i] = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + w * 0.5362f) * 0.11f;
                b6 = w * 0.115926f;
            }
            return buf;
        }

        /// <summary>A sine at a fixed frequency.</summary>
        public static float[] Sine(int n, float freq, float amp)
        {
            var buf = new float[n];
            for (int i = 0; i < n; i++) buf[i] = amp * Sin01((double)freq * i / SampleRate);
            return buf;
        }

        /// <summary>A triangle wave: brighter than a sine, still soft. Used for the high CRT whine.</summary>
        public static float[] Triangle(int n, float freq, float amp)
        {
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                double p = (double)freq * i / SampleRate;
                double frac = p - Math.Floor(p);
                buf[i] = amp * (float)(4.0 * Math.Abs(frac - 0.5) - 1.0);
            }
            return buf;
        }

        /// <summary>A sine gliding from f0 toward f1 with time constant tau (s): the drop that makes a thump feel heavy.</summary>
        public static float[] SineSweep(int n, float f0, float f1, float tau)
        {
            var buf = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = f1 + (f0 - f1) * (float)Math.Exp(-t / tau);
                buf[i] = Sin01(phase);
                phase += f / (double)SampleRate;
            }
            return buf;
        }

        // ---------------------------------------------------------------- envelopes

        /// <summary>
        /// Linear ADSR over the whole buffer (seconds; sustain is a level). The release always ends
        /// on the last sample, so the shape fits any length.
        /// </summary>
        public static float[] Adsr(int n, float attack, float decay, float sustain, float release)
        {
            var env = new float[n];
            int a = Math.Max(1, Samples(attack)), d = Math.Max(1, Samples(decay)), r = Math.Max(1, Samples(release));
            int relStart = Math.Max(0, n - r);
            for (int i = 0; i < n; i++)
            {
                float v;
                if (i < a) v = i / (float)a;
                else if (i < a + d) v = 1f - (1f - sustain) * (i - a) / d;
                else v = sustain;
                if (i >= relStart) v *= 1f - (i - relStart) / (float)(n - relStart);
                env[i] = v;
            }
            return env;
        }

        /// <summary>A short linear attack then exponential decay with time constant tau: the shape of every tap and thump.</summary>
        public static float[] DecayEnvelope(int n, float attack, float tau)
        {
            var env = new float[n];
            int a = Math.Max(1, Samples(attack));
            for (int i = 0; i < n; i++)
            {
                if (i < a) env[i] = i / (float)a;
                else env[i] = (float)Math.Exp(-(i - a) / (tau * SampleRate));
            }
            return env;
        }

        /// <summary>
        /// Smooth random wobble in [0, 1] (cosine-interpolated random points, rateHz per second):
        /// gusts of wind and drifting HVAC, so loops never sound like a static drone.
        /// </summary>
        public static float[] SlowRandom(int n, int seed, float rateHz)
        {
            var rng = new Rng(seed);
            int step = Math.Max(1, (int)(SampleRate / rateHz));
            int points = n / step + 2;
            var p = new float[points];
            for (int i = 0; i < points; i++) p[i] = 0.5f + 0.5f * rng.NextSigned();
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                int k = i / step;
                float f = (i - k * step) / (float)step;
                float w = 0.5f - 0.5f * (float)Math.Cos(Math.PI * f);
                buf[i] = p[k] + (p[k + 1] - p[k]) * w;
            }
            return buf;
        }

        // ---------------------------------------------------------------- filters and mixing

        /// <summary>One-pole low-pass in place. Gentle 6 dB/octave slope: run it twice for a duller result.</summary>
        public static void LowPass(float[] buf, float cutoffHz)
        {
            float a = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoffHz / SampleRate);
            float y = 0;
            for (int i = 0; i < buf.Length; i++) { y += a * (buf[i] - y); buf[i] = y; }
        }

        /// <summary>One-pole high-pass in place (input minus its low-pass). Removes rumble and DC.</summary>
        public static void HighPass(float[] buf, float cutoffHz)
        {
            float a = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoffHz / SampleRate);
            float y = 0;
            for (int i = 0; i < buf.Length; i++) { y += a * (buf[i] - y); buf[i] -= y; }
        }

        /// <summary>One-pole low-pass whose cutoff follows a curve (Hz per sample index): whooshes and wind.</summary>
        public static void LowPassSweep(float[] buf, Func<int, float> cutoffAt)
        {
            float y = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float a = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoffAt(i) / SampleRate);
                y += a * (buf[i] - y);
                buf[i] = y;
            }
        }

        /// <summary>
        /// One-pole low-pass for a signal that repeats every buf.Length samples: one warm-up pass
        /// settles the filter, the second pass is kept, so the result still loops seamlessly.
        /// </summary>
        public static void PeriodicLowPass(float[] buf, float cutoffHz)
        {
            float a = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoffHz / SampleRate);
            float y = 0;
            for (int i = 0; i < buf.Length; i++) y += a * (buf[i] - y);
            for (int i = 0; i < buf.Length; i++) { y += a * (buf[i] - y); buf[i] = y; }
        }

        /// <summary>dst[i] *= env[i].</summary>
        public static void Multiply(float[] dst, float[] env)
        {
            int n = Math.Min(dst.Length, env.Length);
            for (int i = 0; i < n; i++) dst[i] *= env[i];
        }

        /// <summary>Adds src * gain into dst starting at offset, clipped to dst's length.</summary>
        public static void MixInto(float[] dst, float[] src, float gain, int offset)
        {
            for (int i = 0; i < src.Length; i++)
            {
                int j = i + offset;
                if (j < 0) continue;
                if (j >= dst.Length) break;
                dst[j] += src[i] * gain;
            }
        }

        /// <summary>Adds src * gain into a loop starting at offset, wrapping past the end, so events can sit anywhere in a loop.</summary>
        public static void MixIntoWrapped(float[] loop, float[] src, float gain, int offset)
        {
            int n = loop.Length;
            for (int i = 0; i < src.Length; i++) loop[((i + offset) % n + n) % n] += src[i] * gain;
        }

        /// <summary>
        /// A simple room: four parallel feedback delays (prime-ish lengths around 30-45 ms) with a
        /// damping low-pass in each loop, blended in place. feedback sets the tail length, damping
        /// darkens it (0..1), wet is the mix. Enough to put sounds in a concrete space without a convolver.
        /// </summary>
        public static void FeedbackRoom(float[] buf, float feedback, float damping, float wet)
        {
            int[] delays = { Samples(0.0297f), Samples(0.0371f), Samples(0.0411f), Samples(0.0437f) };
            var lines = new float[delays.Length][];
            var idx = new int[delays.Length];
            var lp = new float[delays.Length];
            for (int k = 0; k < delays.Length; k++) lines[k] = new float[delays[k]];
            for (int i = 0; i < buf.Length; i++)
            {
                float x = buf[i];
                float sum = 0;
                for (int k = 0; k < delays.Length; k++)
                {
                    float o = lines[k][idx[k]];
                    lp[k] = o * (1f - damping) + lp[k] * damping;
                    lines[k][idx[k]] = x + feedback * lp[k];
                    idx[k] = (idx[k] + 1) % delays[k];
                    sum += o;
                }
                buf[i] = x * (1f - wet) + sum * 0.25f * wet;
            }
        }

        /// <summary>Linear fade-in and fade-out (seconds) so a one-shot never starts or stops with a click.</summary>
        public static void FadeEdges(float[] buf, float fadeIn, float fadeOut)
        {
            int n = buf.Length;
            int a = Math.Min(n, Math.Max(1, Samples(fadeIn)));
            int r = Math.Min(n, Math.Max(1, Samples(fadeOut)));
            for (int i = 0; i < a; i++) buf[i] *= i / (float)a;
            for (int i = 0; i < r; i++) buf[n - 1 - i] *= i / (float)r;
        }

        /// <summary>Scales the buffer so its absolute peak equals target. Each sound's target sets its loudness relative to the others.</summary>
        public static void Normalize(float[] buf, float target)
        {
            float peak = Peak(buf);
            if (peak < 1e-9f) return;
            float g = target / peak;
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        }

        /// <summary>Largest absolute sample.</summary>
        public static float Peak(float[] buf)
        {
            float peak = 0;
            foreach (float v in buf) { float a = Math.Abs(v); if (a > peak) peak = a; }
            return peak;
        }

        // ---------------------------------------------------------------- loops

        /// <summary>
        /// Folds a buffer of n + f samples into a seamless n-sample loop: the first f samples are
        /// an equal-power crossfade from the overrun tail (which continues straight on from the
        /// end) into the head. Used for noise, which can't be made periodic any other way.
        /// </summary>
        public static float[] LoopCrossfade(float[] src, int n)
        {
            int f = src.Length - n;
            var loop = new float[n];
            Array.Copy(src, loop, n);
            for (int i = 0; i < f && i < n; i++)
            {
                double w = (i / (double)f) * Math.PI * 0.5;
                loop[i] = (float)(src[i] * Math.Sin(w) + src[n + i] * Math.Cos(w));
            }
            return loop;
        }

        /// <summary>
        /// Rotates a seamless loop so it starts at its quietest rising zero crossing. Rotation keeps
        /// the loop seamless, and the first and last samples end up next to zero, so starting or
        /// stopping the clip abruptly can't click either.
        /// </summary>
        public static float[] RotateToZeroCrossing(float[] loop)
        {
            int n = loop.Length;
            int best = 0;
            float bestScore = float.MaxValue;
            for (int k = 0; k < n; k++)
            {
                float prev = loop[(k + n - 1) % n], cur = loop[k];
                if (!(prev <= 0f && cur > 0f)) continue;
                float score = Math.Abs(prev) + Math.Abs(cur);
                if (score < bestScore) { bestScore = score; best = k; }
            }
            var outBuf = new float[n];
            for (int i = 0; i < n; i++) outBuf[i] = loop[(best + i) % n];
            return outBuf;
        }

        /// <summary>Snaps a frequency to a whole number of cycles per ambience loop, so tones repeat seamlessly.</summary>
        public static double LoopFrequency(double hz) => Math.Max(1, Math.Round(hz * AmbienceSeconds)) / AmbienceSeconds;

        /// <summary>Adds a sine that completes whole cycles over the loop.</summary>
        static void AddLoopTone(float[] loop, double hz, float amp)
        {
            double f = LoopFrequency(hz);
            for (int i = 0; i < loop.Length; i++) loop[i] += amp * Sin01(f * i / SampleRate);
        }
    }
}
