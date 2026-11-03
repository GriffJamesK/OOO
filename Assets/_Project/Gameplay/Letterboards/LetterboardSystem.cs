using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>Self-installing: gives every spawned player a TileCarrier (kit rule: plumbing installs itself).</summary>
    public static class LetterboardSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // STUB: implemented by the Letterboard worker.
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // STUB: subscribe to PlayerSpawned and attach a TileCarrier.
        }
    }
}
