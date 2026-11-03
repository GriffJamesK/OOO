using UnityEngine;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// Self-installing: gives every spawned player a SafeGroundTracker, answers PlayerFellOut and
    /// FallZones with a fade-out, respawn at the last safe ground and fade-in, and runs the ending
    /// (PlayerCrossedThreshold into an Exterior threshold -> wait -> GameCompleted).
    /// </summary>
    public static class FlowSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // STUB: implemented by the Flow worker.
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // STUB: implemented by the Flow worker.
        }
    }
}
