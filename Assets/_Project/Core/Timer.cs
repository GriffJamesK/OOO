using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit.Core
{
    /// <summary>
    /// Delayed and repeating callbacks without writing a coroutine each time. All timers share
    /// one TimerRunner, so there is one Update for the whole game instead of one per timer.
    /// </summary>
    public static class Timer
    {
        /// <summary>Runs <paramref name="action"/> once after <paramref name="seconds"/> of scaled time.</summary>
        public static TimerHandle After(float seconds, Action action) => TimerRunner.Scheduler.After(seconds, action);

        /// <summary>Runs <paramref name="action"/> every <paramref name="seconds"/> of scaled time until cancelled.</summary>
        public static TimerHandle Every(float seconds, Action action) => TimerRunner.Scheduler.Every(seconds, action);
    }

    /// <summary>
    /// The timer bookkeeping, driven by Tick(dt). The runner ticks the shared instance; tests make
    /// their own and tick it by hand, so timing tests need no play mode and no waiting.
    /// </summary>
    public sealed class TimerScheduler
    {
        readonly List<TimerHandle> timers = new List<TimerHandle>();
        readonly List<TimerHandle> added = new List<TimerHandle>();

        public int Count => timers.Count + added.Count;

        public TimerHandle After(float seconds, Action action) => Add(new TimerHandle(seconds, action, false));

        public TimerHandle Every(float seconds, Action action) => Add(new TimerHandle(Mathf.Max(0.0001f, seconds), action, true));

        TimerHandle Add(TimerHandle handle)
        {
            added.Add(handle); // merged on the next Tick so callbacks can add timers safely
            return handle;
        }

        /// <summary>Advances every timer by deltaTime and fires the ones that are due.</summary>
        public void Tick(float deltaTime)
        {
            if (added.Count > 0) { timers.AddRange(added); added.Clear(); }

            for (int i = timers.Count - 1; i >= 0; i--)
            {
                var t = timers[i];
                if (!t.IsCancelled) t.Advance(deltaTime);
                if (t.IsDone) timers.RemoveAt(i);
            }
        }

        public void Clear() { timers.Clear(); added.Clear(); }
    }

    /// <summary>A scheduled callback. Keep it if you might need to Cancel.</summary>
    public sealed class TimerHandle
    {
        readonly float interval;
        readonly Action action;
        readonly bool repeat;
        float remaining;

        public bool IsCancelled { get; private set; }
        public bool IsDone => IsCancelled || (!repeat && remaining <= 0f);

        internal TimerHandle(float seconds, Action action, bool repeat)
        {
            interval = seconds; remaining = seconds; this.action = action; this.repeat = repeat;
        }

        public void Cancel() => IsCancelled = true;

        internal void Advance(float dt)
        {
            remaining -= dt;
            while (remaining <= 0f && !IsCancelled)
            {
                action?.Invoke();
                if (!repeat) return;
                remaining += interval;
            }
        }
    }

    /// <summary>The single MonoBehaviour that ticks every Timer. Self-installs before the first scene loads.</summary>
    [AddComponentMenu("")]
    public sealed class TimerRunner : MonoBehaviour
    {
        static TimerScheduler scheduler;

        /// <summary>The shared scheduler; creating it also ensures the runner exists.</summary>
        public static TimerScheduler Scheduler
        {
            get
            {
                if (scheduler == null) scheduler = new TimerScheduler();
                KitHost.Ensure<TimerRunner>();
                return scheduler;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => scheduler = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install() => KitHost.Ensure<TimerRunner>();

        void Update() => scheduler?.Tick(Time.deltaTime);
    }
}
