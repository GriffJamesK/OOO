using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Kit.Gameplay.Audio
{
    /// <summary>
    /// Self-installing: synthesises placeholder clips at start (no audio assets) and plays them
    /// from gameplay events: footsteps, jumps, landings, slides, wall runs, threshold crossings,
    /// tile clicks, respawns, per-area ambience and the ending tone.
    /// This static half owns install, the clip cache and debug history; the playback logic lives
    /// on <see cref="AudioDirectorHost"/>, the hidden "[OOO Audio]" object it creates.
    /// </summary>
    public static class AudioDirector
    {
        public const string HostName = "[OOO Audio]";
        const int RecentCapacity = 5;

        static AudioDirectorHost host;
        static readonly Dictionary<SoundId, AudioClip> sfx = new Dictionary<SoundId, AudioClip>();
        static readonly Dictionary<string, AudioClip> ambience = new Dictionary<string, AudioClip>();
        static readonly List<string> recent = new List<string>();
        static double synthMs;

        /// <summary>The running host, or null outside play mode.</summary>
        public static AudioDirectorHost Host => host;
        /// <summary>One-shot clips built so far.</summary>
        public static int SfxClipCount => sfx.Count;
        /// <summary>Ambience loops built so far (they are built lazily, one per frame or on first entry).</summary>
        public static int AmbienceClipCount => ambience.Count;
        /// <summary>Total time spent synthesising this session, ms.</summary>
        public static double SynthMilliseconds => synthMs;
        /// <summary>The last few sounds played, oldest first.</summary>
        public static IReadOnlyList<string> Recent => recent;

        /// <summary>
        /// Domain reload is off: the host and every runtime clip from the last session were
        /// destroyed when play mode ended, so forget them, and never leave the listener paused.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            host = null;
            sfx.Clear();
            ambience.Clear();
            recent.Clear();
            synthMs = 0;
            AudioListener.pause = false;
        }

        /// <summary>Creates the one hidden, persistent audio host after the first scene loads. Guarded so it never installs twice.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (host != null) return;
            var go = new GameObject(HostName) { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(go);
            host = go.AddComponent<AudioDirectorHost>();
        }

        /// <summary>The clip for a one-shot, synthesised on first request.</summary>
        internal static AudioClip Clip(SoundId id)
        {
            if (sfx.TryGetValue(id, out var clip) && clip != null) return clip;
            clip = MakeClip(id.ToString(), () => ProcSynth.Build(id));
            sfx[id] = clip;
            return clip;
        }

        /// <summary>True when the ambience for this key is already built.</summary>
        internal static bool HasAmbience(string key) => ambience.TryGetValue(key, out var c) && c != null;

        /// <summary>The ambience loop for an ambience key ("A1".."A7" or ""), synthesised on first request.</summary>
        internal static AudioClip Ambience(string key)
        {
            if (ambience.TryGetValue(key, out var clip) && clip != null) return clip;
            clip = MakeClip("Ambience_" + (key.Length > 0 ? key : "Room"), () => ProcSynth.Ambience(key));
            ambience[key] = clip;
            return clip;
        }

        /// <summary>Remembers a played sound for AudioDebug.Report.</summary>
        internal static void Remember(string name)
        {
            recent.Add(name);
            if (recent.Count > RecentCapacity) recent.RemoveAt(0);
        }

        /// <summary>Destroys every runtime clip (the host calls this when it goes away).</summary>
        internal static void ReleaseClips()
        {
            foreach (var c in sfx.Values) if (c != null) Object.Destroy(c);
            foreach (var c in ambience.Values) if (c != null) Object.Destroy(c);
            sfx.Clear();
            ambience.Clear();
        }

        internal static void Forget(AudioDirectorHost h)
        {
            if (host == h) host = null;
        }

        /// <summary>Synthesises samples into a new mono AudioClip, timing the work for the report.</summary>
        static AudioClip MakeClip(string name, System.Func<float[]> synth)
        {
            var watch = Stopwatch.StartNew();
            float[] data = synth();
            var clip = AudioClip.Create(name, data.Length, 1, ProcSynth.SampleRate, false);
            clip.SetData(data, 0);
            synthMs += watch.Elapsed.TotalMilliseconds;
            return clip;
        }
    }
}
