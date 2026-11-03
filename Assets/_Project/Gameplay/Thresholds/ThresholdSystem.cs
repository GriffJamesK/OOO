using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// Self-installing: on every PlayerSpawned, makes sure the player has a ThresholdTraveller.
    /// Nothing is placed in scenes for it (kit rule: plumbing installs itself).
    /// </summary>
    public static class ThresholdSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // STUB: implemented by the Thresholds worker.
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // STUB: subscribe to PlayerSpawned and attach a ThresholdTraveller.
        }
    }
}
