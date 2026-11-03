using System.Collections.Generic;
using Kit.Core.Tweening;
using UnityEngine;

namespace Kit.Core.Feel
{
    /// <summary>
    /// One-line game-feel effects. These are the polish-checklist staples, so they live in Core
    /// where every prototype can call them without writing the same coroutine again.
    /// </summary>
    public static class Juice
    {
        static readonly Dictionary<Transform, (TweenHandle handle, Vector3 baseScale)> punches =
            new Dictionary<Transform, (TweenHandle, Vector3)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => punches.Clear();

        /// <summary>
        /// Scale punch: the transform bulges by <paramref name="strength"/> and wobbles back to
        /// its original scale. Re-punching mid-punch restarts from the original scale, never drifts.
        /// </summary>
        public static TweenHandle Punch(Transform target, Vector3 strength, float duration)
        {
            if (target == null) return null;
            var baseScale = target.localScale;
            if (punches.TryGetValue(target, out var running))
            {
                running.handle.Cancel();
                baseScale = running.baseScale;
            }

            var handle = Tween.To(0f, 1f, duration, Ease.Linear,
                t => { if (target != null) target.localScale = baseScale + strength * PunchCurve(t); },
                () => punches.Remove(target), unscaledTime: true);
            punches[target] = (handle, baseScale);
            return handle;
        }

        /// <summary>Decaying oscillation used by Punch: 0 at t=0 and t=1, peaks early. Public so it can be tested.</summary>
        public static float PunchCurve(float t)
        {
            t = Mathf.Clamp01(t);
            if (t >= 1f) return 0f;
            return Mathf.Sin(t * Mathf.PI * 3f) * (1f - t);
        }

        /// <summary>
        /// Random positional shake of the camera's rendered view. Applied just before rendering and
        /// removed right after, so it works with Cinemachine (which rewrites the transform each frame).
        /// </summary>
        public static void Shake(Camera camera, float amplitude, float duration)
        {
            if (camera == null) return;
            KitHost.Ensure<JuiceRunner>().AddShake(camera, amplitude, duration);
        }

        /// <summary>
        /// Freezes time briefly on impacts (timeScale dips to near zero, then restores). Uses
        /// real time so the pause itself is not slowed down. Overlapping calls extend the stop.
        /// </summary>
        public static void HitStop(float seconds)
        {
            if (seconds <= 0f) return;
            KitHost.Ensure<JuiceRunner>().HitStop(seconds);
        }
    }
}
