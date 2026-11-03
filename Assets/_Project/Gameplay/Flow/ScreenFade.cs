using System;
using Kit.Core.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// Full-screen fade to and from black that is not UI (the game has no HUD and UI never
    /// references Gameplay). Self-installing on first use. Used by respawns and the ending.
    /// <para>
    /// It is a hidden global URP Volume (priority 1000, above the look profile) whose runtime
    /// profile sets ColorAdjustments.colorFilter to black; the fade is the volume's weight, so the
    /// filter blends linearly from the scene's own grade to black. Colour grading runs after bloom,
    /// so even bright lights go fully black. The main camera has post-processing on and URP's
    /// volume layer mask defaults to Default, which is the layer this object sits on.
    /// </para>
    /// </summary>
    public static class ScreenFade
    {
        static GameObject host;
        static Volume volume;
        static VolumeProfile profile;
        static TweenHandle tween;

        /// <summary>Current darkness, 0 = clear, 1 = black.</summary>
        public static float Alpha => volume != null ? volume.weight : 0f;

        /// <summary>Domain reload is off: the previous session's objects are gone, so forget them.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            host = null;
            volume = null;
            profile = null;
            tween = null;
        }

        /// <summary>
        /// Fades to <paramref name="alpha"/> over <paramref name="duration"/> seconds (unscaled, so it
        /// runs while paused), then calls <paramref name="onDone"/>. A running fade is cancelled
        /// (its onDone does not fire). A duration of 0 applies at once and calls onDone immediately.
        /// </summary>
        public static void FadeTo(float alpha, float duration, Action onDone = null)
        {
            alpha = Mathf.Clamp01(alpha);
            if (!Ensure()) { onDone?.Invoke(); return; }

            tween?.Cancel();
            tween = null;

            if (duration <= 0f)
            {
                SetWeight(alpha);
                onDone?.Invoke();
                return;
            }

            tween = Tween.To(volume.weight, alpha, duration, Ease.InOutSine, SetWeight, onDone, unscaledTime: true);
        }

        static void SetWeight(float w)
        {
            if (volume == null) return;
            volume.weight = w;
            // A zero-weight volume does nothing, but disabling it also skips it in the volume stack.
            volume.enabled = w > 0f;
        }

        /// <summary>Creates the hidden fade volume the first time it is needed (play mode only).</summary>
        static bool Ensure()
        {
            if (volume != null) return true;
            if (!Application.isPlaying) return false;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "ScreenFade (runtime)";
            profile.hideFlags = HideFlags.DontSave;
            var grade = profile.Add<ColorAdjustments>(true);
            grade.colorFilter.Override(Color.black);

            host = new GameObject("[Screen Fade]") { layer = 0, hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(host);
            volume = host.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1000f;
            volume.sharedProfile = profile;
            volume.weight = 0f;
            volume.enabled = false;

            // The profile is a runtime ScriptableObject that would otherwise leak past play mode.
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
            return true;
        }

        static void OnQuitting()
        {
            Application.quitting -= OnQuitting;
            tween?.Cancel();
            if (host != null) UnityEngine.Object.Destroy(host);
            if (profile != null) UnityEngine.Object.Destroy(profile);
            host = null;
            volume = null;
            profile = null;
            tween = null;
        }
    }
}
