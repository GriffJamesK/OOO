using UnityEngine;

namespace Kit.Gameplay.Audio
{
    /// <summary>
    /// Self-installing: synthesises placeholder clips at start (no audio assets) and plays them
    /// from gameplay events: footsteps, jumps, landings, slides, wall runs, threshold crossings,
    /// tile clicks, respawns, per-area ambience and the ending tone.
    /// </summary>
    public static class AudioDirector
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // STUB: implemented by the Audio worker.
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // STUB: implemented by the Audio worker.
        }
    }
}
