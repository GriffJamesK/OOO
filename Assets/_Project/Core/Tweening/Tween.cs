using System;
using System.Collections;
using UnityEngine;

namespace Kit.Core.Tweening
{
    /// <summary>
    /// Minimal float tween: everything else (position, scale, alpha, colour) is built from a
    /// float and a lambda. Runs as a coroutine on the self-installed TweenRunner; after the
    /// one-off handle and coroutine, each frame allocates nothing.
    /// </summary>
    public static class Tween
    {
        /// <summary>
        /// Animates from -> to over duration seconds, calling onUpdate with the eased value every
        /// frame and once more with exactly <paramref name="to"/> at the end.
        /// </summary>
        /// <param name="unscaledTime">True for UI and effects that must keep moving while paused (timeScale 0).</param>
        public static TweenHandle To(float from, float to, float duration, Ease ease, Action<float> onUpdate,
            Action onComplete = null, bool unscaledTime = false)
        {
            var handle = new TweenHandle();
            var runner = KitHost.Ensure<TweenRunner>();
            handle.Attach(runner, runner.StartCoroutine(Run(new TweenState(from, to, duration, ease), onUpdate, onComplete, unscaledTime, handle)));
            return handle;
        }

        static IEnumerator Run(TweenState state, Action<float> onUpdate, Action onComplete, bool unscaled, TweenHandle handle)
        {
            onUpdate?.Invoke(state.Value);
            while (!state.IsDone)
            {
                yield return null;
                state.Step(unscaled ? Time.unscaledDeltaTime : Time.deltaTime);
                onUpdate?.Invoke(state.Value);
            }
            handle.MarkDone();
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// The pure maths of a tween, separated from the coroutine so edit-mode tests can step it
    /// with a manual delta time.
    /// </summary>
    public struct TweenState
    {
        public readonly float From, To, Duration;
        public readonly Ease Ease;
        public float Elapsed { get; private set; }

        public TweenState(float from, float to, float duration, Ease ease)
        {
            From = from; To = to; Duration = Mathf.Max(0f, duration); Ease = ease; Elapsed = 0f;
        }

        public bool IsDone => Elapsed >= Duration;

        /// <summary>Current eased value; exactly To once done, so callers never see a near-miss.</summary>
        public float Value => IsDone ? To : Mathf.LerpUnclamped(From, To, Easing.Evaluate(Ease, Elapsed / Duration));

        public void Step(float deltaTime) => Elapsed = Mathf.Min(Duration, Elapsed + Mathf.Max(0f, deltaTime));
    }

    /// <summary>Lets a caller stop a running tween, for example when a new tween replaces it.</summary>
    public sealed class TweenHandle
    {
        MonoBehaviour owner;
        Coroutine routine;

        /// <summary>True once the tween finished or was cancelled.</summary>
        public bool IsDone { get; private set; }

        internal void Attach(MonoBehaviour runner, Coroutine coroutine)
        {
            owner = runner;
            // The coroutine may already have completed synchronously (zero duration).
            if (!IsDone) routine = coroutine;
        }

        internal void MarkDone() { IsDone = true; routine = null; }

        /// <summary>Stops the tween where it is. onComplete does not fire.</summary>
        public void Cancel()
        {
            if (IsDone) return;
            if (owner != null && routine != null) owner.StopCoroutine(routine);
            MarkDone();
        }
    }

    /// <summary>Host for tween coroutines. Self-installs so no scene needs it placed.</summary>
    [AddComponentMenu("")]
    public sealed class TweenRunner : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install() => KitHost.Ensure<TweenRunner>();
    }
}
