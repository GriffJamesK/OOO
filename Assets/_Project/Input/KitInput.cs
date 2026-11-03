using UnityEngine;

namespace Kit.Input
{
    /// <summary>
    /// The one shared, enabled KitActions instance. Every system reads input from here, so
    /// rebinding or disabling a map affects the whole game at once, and nothing allocates its
    /// own copy of the action asset.
    /// </summary>
    public static class KitInput
    {
        static KitActions actions;

        /// <summary>Shared actions, created and enabled on first access.</summary>
        public static KitActions Actions
        {
            get
            {
                if (actions == null)
                {
                    actions = new KitActions();
                    actions.Enable();
                    // Fires on app quit and on exiting play mode in the Editor. Disposing here stops
                    // the wrapper's "Disable() has not been called" leak warning on script reload.
                    Application.quitting -= Reset;
                    Application.quitting += Reset;
                }
                return actions;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only cleanup. With Domain Reload off the shared instance can outlive a play
        /// session or a test run; releasing it before every script reload and on exiting play mode
        /// stops the generated wrapper's "Disable() has not been called" leak assert.
        /// </summary>
        [UnityEditor.InitializeOnLoadMethod]
        static void HookEditorCleanup()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Reset;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Reset;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        {
            if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode) Reset();
        }
#endif

        /// <summary>
        /// Throws away the shared instance. Runs on play-mode enter (Domain Reload is off), and
        /// tests call it after InputTestFixture.Setup so actions bind to the test devices.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            if (actions == null) return;
            actions.Disable();
            actions.Dispose();
            actions = null;
        }
    }
}
