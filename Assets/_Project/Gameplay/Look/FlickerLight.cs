using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// A fluorescent light that now and then flickers (rarely, briefly) to make an empty office
    /// feel lived in and slightly wrong. Drives the Light on this object and, optionally, an
    /// emissive renderer. Timing comes from LookConfig.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class FlickerLight : MonoBehaviour
    {
        [Tooltip("Optional emissive fixture that dims with the light.")]
        [SerializeField] Renderer fixture;

        public Renderer Fixture => fixture;

        /// <summary>Builder entry point (edit mode).</summary>
        public void Setup(Renderer emissiveFixture) => fixture = emissiveFixture;

        // STUB: the Look worker implements the flicker.
    }
}
