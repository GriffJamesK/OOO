using System;

namespace Kit.Gameplay.Audio
{
    /// <summary>The placeholder one-shot sounds ProcSynth can build. Ambiences are built by area id instead.</summary>
    public enum SoundId
    {
        StepConcrete0, StepConcrete1, StepConcrete2,
        StepCarpet0, StepCarpet1, StepCarpet2,
        Jump, Land, Slide, WallRun,
        Threshold, TilePull, TilePlace, Respawn, Ending,
    }

    /// <summary>
    /// Pure procedural synthesis of every placeholder sound as mono float buffers, so the
    /// prototype ships with no audio files and edit-mode tests can inspect the exact samples.
    /// Each sound uses a fixed seed (its own xorshift generator, not System.Random or
    /// UnityEngine.Random) so a buffer is identical on every run and every machine.
    /// The palette aims for the GDD's mood: soft, foggy, slightly uncanny, never sci-fi.
    /// </summary>
    public static partial class ProcSynth
    {
        /// <summary>Low rate keeps synthesis cheap; the sounds are dull and soft, so nothing needed sits above 11 kHz.</summary>
        public const int SampleRate = 22050;

        /// <summary>Length of every ambience loop, seconds. Loop tones are tuned to whole cycles over this length.</summary>
        public const float AmbienceSeconds = 8f;

        /// <summary>Area ids with their own ambience; "" is the fallback room tone.</summary>
        public static readonly string[] AmbienceIds = { "A1", "A2", "A3", "A4", "A5", "A6", "A7", "" };

        /// <summary>Length in seconds of each one-shot. The director and the tests both read this.</summary>
        public static float Seconds(SoundId id)
        {
            switch (id)
            {
                case SoundId.StepConcrete0: case SoundId.StepConcrete1: case SoundId.StepConcrete2: return 0.18f;
                case SoundId.StepCarpet0: case SoundId.StepCarpet1: case SoundId.StepCarpet2: return 0.2f;
                case SoundId.Jump: return 0.35f;
                case SoundId.Land: return 0.35f;
                case SoundId.Slide: return 0.6f;
                case SoundId.WallRun: return 0.5f;
                case SoundId.Threshold: return 1.2f;
                case SoundId.TilePull: return 0.14f;
                case SoundId.TilePlace: return 0.14f;
                case SoundId.Respawn: return 0.9f;
                case SoundId.Ending: return 6f;
                default: return 0.1f;
            }
        }

        /// <summary>Sample count for a duration at SampleRate.</summary>
        public static int Samples(float seconds) => (int)Math.Round(seconds * SampleRate);

        /// <summary>Builds one one-shot. Deterministic: the seed is derived from the id.</summary>
        public static float[] Build(SoundId id)
        {
            int seed = 1009 + 7919 * (int)id;
            switch (id)
            {
                case SoundId.StepConcrete0: return StepConcrete(0, seed);
                case SoundId.StepConcrete1: return StepConcrete(1, seed);
                case SoundId.StepConcrete2: return StepConcrete(2, seed);
                case SoundId.StepCarpet0: return StepCarpet(0, seed);
                case SoundId.StepCarpet1: return StepCarpet(1, seed);
                case SoundId.StepCarpet2: return StepCarpet(2, seed);
                case SoundId.Jump: return Jump(seed);
                case SoundId.Land: return Land(seed);
                case SoundId.Slide: return Slide(seed);
                case SoundId.WallRun: return WallRun(seed);
                case SoundId.Threshold: return Threshold(seed);
                case SoundId.TilePull: return TileClick(seed, place: false);
                case SoundId.TilePlace: return TileClick(seed, place: true);
                case SoundId.Respawn: return Respawn(seed);
                case SoundId.Ending: return EndingTone(seed);
                default: return new float[Samples(0.1f)];
            }
        }

        // ---------------------------------------------------------------- footsteps

        /// <summary>Concrete: a crisp filtered noise tap over a short low thump. Variation shifts tone and seed.</summary>
        static float[] StepConcrete(int variation, int seed)
        {
            int n = Samples(Seconds(SoundId.StepConcrete0));
            var tap = WhiteNoise(n, seed);
            HighPass(tap, 350f + 60f * variation);
            LowPass(tap, 3200f - 300f * variation);
            Multiply(tap, DecayEnvelope(n, 0.001f, 0.018f + 0.003f * variation));

            var thump = SineSweep(n, 95f - 8f * variation, 55f, 0.06f);
            Multiply(thump, DecayEnvelope(n, 0.002f, 0.035f));
            MixInto(tap, thump, 0.9f, 0);

            FeedbackRoom(tap, 0.25f, 0.2f, 0.4f);
            FadeEdges(tap, 0.0005f, 0.02f);
            Normalize(tap, 0.7f);
            return tap;
        }

        /// <summary>Carpet: duller and softer than concrete, a pink-noise pad with a quieter thump.</summary>
        static float[] StepCarpet(int variation, int seed)
        {
            int n = Samples(Seconds(SoundId.StepCarpet0));
            var pad = PinkNoise(n, seed);
            LowPass(pad, 800f + 120f * variation);
            LowPass(pad, 1400f);
            HighPass(pad, 120f);
            Multiply(pad, DecayEnvelope(n, 0.006f, 0.035f + 0.004f * variation));

            var thump = SineSweep(n, 75f - 5f * variation, 50f, 0.08f);
            Multiply(thump, DecayEnvelope(n, 0.004f, 0.04f));
            MixInto(pad, thump, 0.6f, 0);

            FadeEdges(pad, 0.001f, 0.03f);
            Normalize(pad, 0.5f);
            return pad;
        }

        // ---------------------------------------------------------------- movement

        /// <summary>Jump: a soft cloth whoosh, pink noise through a low-pass that opens and closes.</summary>
        static float[] Jump(int seed)
        {
            int n = Samples(Seconds(SoundId.Jump));
            var air = PinkNoise(n, seed);
            HighPass(air, 250f);
            LowPassSweep(air, i =>
            {
                float t = i / (float)n;
                return 500f + 2200f * (float)Math.Sin(Math.PI * Math.Min(1f, t * 1.4f));
            });
            Multiply(air, Adsr(n, 0.07f, 0.1f, 0.4f, 0.15f));
            FadeEdges(air, 0.002f, 0.02f);
            Normalize(air, 0.45f);
            return air;
        }

        /// <summary>Land: a heavy low thump plus a dull body slap. Playback scales volume and pitch by fall speed.</summary>
        static float[] Land(int seed)
        {
            int n = Samples(Seconds(SoundId.Land));
            var thump = SineSweep(n, 80f, 42f, 0.12f);
            Multiply(thump, DecayEnvelope(n, 0.002f, 0.08f));

            var slap = PinkNoise(n, seed);
            LowPass(slap, 700f);
            LowPass(slap, 1200f);
            Multiply(slap, DecayEnvelope(n, 0.001f, 0.045f));
            MixInto(thump, slap, 1.6f, 0);

            FeedbackRoom(thump, 0.35f, 0.25f, 0.4f);
            FadeEdges(thump, 0.0005f, 0.04f);
            Normalize(thump, 0.8f);
            return thump;
        }

        /// <summary>Slide: a 0.6 s fabric-on-floor hiss with a faint flutter so it isn't a flat noise.</summary>
        static float[] Slide(int seed)
        {
            int n = Samples(Seconds(SoundId.Slide));
            var hiss = WhiteNoise(n, seed);
            HighPass(hiss, 700f);
            LowPass(hiss, 4200f);
            LowPass(hiss, 6000f);
            var env = Adsr(n, 0.03f, 0.08f, 0.7f, 0.3f);
            for (int i = 0; i < n; i++)
                env[i] *= 0.85f + 0.15f * Sin01(13f * i / SampleRate);
            Multiply(hiss, env);
            FadeEdges(hiss, 0.002f, 0.03f);
            Normalize(hiss, 0.45f);
            return hiss;
        }

        /// <summary>Wall run: a scuffing rub, band-passed noise in grainy bursts like soles catching a wall.</summary>
        static float[] WallRun(int seed)
        {
            int n = Samples(Seconds(SoundId.WallRun));
            var rub = WhiteNoise(n, seed);
            HighPass(rub, 300f);
            LowPass(rub, 1900f);
            LowPass(rub, 2600f);
            var env = Adsr(n, 0.02f, 0.1f, 0.6f, 0.25f);
            for (int i = 0; i < n; i++)
            {
                float g = Math.Abs(Sin01(4.5f * i / SampleRate));
                env[i] *= 0.35f + 0.65f * g * g;
            }
            Multiply(rub, env);
            FeedbackRoom(rub, 0.3f, 0.2f, 0.4f);
            FadeEdges(rub, 0.002f, 0.03f);
            Normalize(rub, 0.5f);
            return rub;
        }

        // ---------------------------------------------------------------- the rule

        /// <summary>
        /// Threshold crossing, the signature sound: a soft low pressure swell (two slightly beating
        /// sub tones under muffled air) that blooms over about 0.5 s, sags in pitch as it releases,
        /// then rings out in a short dark room tail. Beating, not a synth sweep, is what makes it
        /// feel like the building breathing rather than a sci-fi portal.
        /// </summary>
        static float[] Threshold(int seed)
        {
            int n = Samples(Seconds(SoundId.Threshold));
            int swell = Samples(0.5f);
            var body = new float[n];
            double pa = 0, pb = 0;
            for (int i = 0; i < swell; i++)
            {
                float t = i / (float)swell;
                float sag = 1f - 0.18f * t * t;
                pa += 46f * sag / SampleRate;
                pb += 49.3f * sag / SampleRate;
                float env = (float)Math.Pow(Math.Sin(Math.PI * Math.Pow(t, 0.7)), 2);
                body[i] = env * (0.6f * Sin01(pa) + 0.45f * Sin01(pb) + 0.12f * Sin01(pa * 2.01));
            }

            var air = PinkNoise(n, seed);
            LowPassSweep(air, i =>
            {
                float t = Math.Min(1f, i / (float)swell);
                return 120f + 480f * (float)Math.Sin(Math.PI * t);
            });
            HighPass(air, 40f);
            var airEnv = new float[n];
            for (int i = 0; i < swell; i++)
            {
                float t = i / (float)swell;
                airEnv[i] = (float)Math.Pow(Math.Sin(Math.PI * t), 3);
            }
            Multiply(air, airEnv);
            MixInto(body, air, 1.4f, 0);

            FeedbackRoom(body, 0.62f, 0.45f, 0.55f);
            FadeEdges(body, 0.002f, 0.15f);
            Normalize(body, 0.85f);
            return body;
        }

        /// <summary>
        /// Felt-and-plastic tile clicks. Pull: the plastic tab snaps first, then felt drags.
        /// Place: a soft felt press, then the crisp seat of the tab.
        /// </summary>
        static float[] TileClick(int seed, bool place)
        {
            int n = Samples(Seconds(SoundId.TilePull));
            var outBuf = new float[n];

            var felt = PinkNoise(n, seed);
            LowPass(felt, 1400f);
            HighPass(felt, 150f);
            Multiply(felt, DecayEnvelope(n, 0.002f, place ? 0.012f : 0.02f));

            var click = WhiteNoise(n, seed + 1);
            HighPass(click, 1800f);
            Multiply(click, DecayEnvelope(n, 0.0003f, 0.002f));
            var ping = Sine(n, place ? 2300f : 2700f, 1f);
            Multiply(ping, DecayEnvelope(n, 0.0003f, 0.006f));
            MixInto(click, ping, 0.5f, 0);

            int lag = Samples(place ? 0.03f : 0.018f);
            if (place) { MixInto(outBuf, felt, 1f, 0); MixInto(outBuf, click, 0.8f, lag); }
            else { MixInto(outBuf, click, 0.8f, 0); MixInto(outBuf, felt, 0.7f, lag); }

            FeedbackRoom(outBuf, 0.2f, 0.15f, 0.35f);
            FadeEdges(outBuf, 0.0003f, 0.02f);
            Normalize(outBuf, 0.6f);
            return outBuf;
        }

        /// <summary>Respawn: a soft reversed swell, a thump with its room tail played backwards, so it breathes in and resolves.</summary>
        static float[] Respawn(int seed)
        {
            int n = Samples(Seconds(SoundId.Respawn));
            var buf = PinkNoise(n, seed);
            LowPass(buf, 900f);
            HighPass(buf, 80f);
            Multiply(buf, DecayEnvelope(n, 0.002f, 0.12f));
            var tone = Sine(n, 220f, 0.25f);
            MixInto(tone, Sine(n, 329.63f, 0.18f), 1f, 0);
            Multiply(tone, DecayEnvelope(n, 0.01f, 0.2f));
            MixInto(buf, tone, 1f, 0);
            FeedbackRoom(buf, 0.65f, 0.4f, 0.6f);
            Array.Reverse(buf);
            FadeEdges(buf, 0.01f, 0.03f);
            Normalize(buf, 0.5f);
            return buf;
        }

        /// <summary>
        /// The ending: a warm, open D add9 chord (D3 A3 E4 F#4 A4), slightly detuned pairs for
        /// warmth, strummed slowly and swelling in over 1.5 s, then letting go. Open fifths and the
        /// added ninth read as relief; the soft, low voicing keeps it melancholic.
        /// </summary>
        static float[] EndingTone(int seed)
        {
            int n = Samples(Seconds(SoundId.Ending));
            var buf = new float[n];
            float[] notes = { 146.83f, 220f, 329.63f, 369.99f, 440f };
            float[] gains = { 0.5f, 0.42f, 0.32f, 0.26f, 0.2f };
            var rng = new Rng(seed);
            for (int k = 0; k < notes.Length; k++)
            {
                int start = Samples(0.14f * k);
                double detune = 1.0 + 0.0018 * (rng.NextSigned() + 1.2f);
                double a = 0, b = 0;
                for (int i = start; i < n; i++)
                {
                    // Phase accumulators, so the slow vibrato bends pitch without drifting.
                    double t = (i - start) / (double)SampleRate;
                    double f = notes[k] * (1.0 + 0.0015 * Sin01(4.3 * t + k * 0.2));
                    float s = Sin01(a) + Sin01(b) + 0.3f * (Sin01(2.0 * a) + Sin01(2.0 * b)) + 0.1f * Sin01(3.0 * a);
                    buf[i] += gains[k] * s;
                    a += f / SampleRate;
                    b += f * detune / SampleRate;
                }
            }
            Multiply(buf, Adsr(n, 1.5f, 0.8f, 0.8f, 3f));
            LowPass(buf, 2500f);
            FeedbackRoom(buf, 0.5f, 0.35f, 0.5f);
            FadeEdges(buf, 0.01f, 0.4f);
            Normalize(buf, 0.7f);
            return buf;
        }
    }
}
