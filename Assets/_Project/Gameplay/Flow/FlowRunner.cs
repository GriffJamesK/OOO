using UnityEngine;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// Gives the static FlowSystem a per-frame tick. Lives on the hidden "[Kit Runtime]" host
    /// (KitHost), so no scene needs a plumbing object placed.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class FlowRunner : MonoBehaviour
    {
        void Update() => FlowSystem.Tick();
    }
}
