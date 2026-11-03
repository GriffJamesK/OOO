using System;
using UnityEngine;

namespace Kit.Core
{
    /// <summary>
    /// Owns the single hidden "[Kit Runtime]" GameObject that every self-installing runner
    /// (timers, tweens, shake, debug overlay) lives on. Centralising it means scenes never
    /// need plumbing objects placed by hand, and any scene works when played directly.
    /// </summary>
    public static class KitHost
    {
        static GameObject host;

        /// <summary>
        /// Enter Play Mode runs with Domain Reload off, so statics survive between sessions.
        /// The old host was destroyed when play mode exited; forget it here.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => host = null;

        /// <summary>Returns the runner component of type T, creating the host and component on first use.</summary>
        public static T Ensure<T>() where T : Component
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException(
                    $"{typeof(T).Name} needs play mode. In edit-mode tests, drive the pure scheduler/state classes directly.");

            if (host == null)
            {
                host = new GameObject("[Kit Runtime]");
                UnityEngine.Object.DontDestroyOnLoad(host);
            }

            var component = host.GetComponent<T>();
            return component != null ? component : host.AddComponent<T>();
        }
    }
}
