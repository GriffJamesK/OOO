using UnityEngine;

namespace Kit.Gameplay.Portals
{
    /// <summary>
    /// Self-installing: keeps one PortalView per paired threshold (rebuilt on ThresholdsRepaired)
    /// and, before each frame renders, places and renders the views the main camera can see.
    /// </summary>
    public static class PortalSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // STUB: implemented by the Portals worker.
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // STUB: subscribe to ThresholdsRepaired and RenderPipelineManager.beginContextRendering.
        }

        /// <summary>Number of portal views currently rendering (for tests and PortalDebug).</summary>
        public static int ActiveViewCount => 0; // STUB
    }
}
