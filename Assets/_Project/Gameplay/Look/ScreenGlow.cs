using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// A slow brightness pulse on an emissive screen (the green CRTs in the open plan), so the
    /// machines feel left on rather than painted. Uses a MaterialPropertyBlock (no material copies).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class ScreenGlow : MonoBehaviour
    {
        // STUB: the Look worker implements the pulse.
    }
}
