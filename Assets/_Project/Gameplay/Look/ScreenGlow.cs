using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// A slow brightness pulse on an emissive screen (the green CRTs in the open plan), so the
    /// machines feel left on rather than painted. Uses a MaterialPropertyBlock (no material copies).
    /// Each screen gets its own phase and a slightly different rate, so a room of them breathes
    /// unevenly instead of pulsing together. Screens out of view are skipped.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class ScreenGlow : MonoBehaviour
    {
        Renderer screen;
        LookConfig config;
        EmissiveSlot slot;
        float phase;
        float rateScale;

        void OnEnable()
        {
            screen = GetComponent<Renderer>();
            config = LookAssets.Config;
            slot = EmissiveSlot.Find(screen);
            phase = Random.value;
            rateScale = Random.Range(0.8f, 1.25f);
            if (!slot.Valid) enabled = false; // nothing emissive to drive
        }

        void OnDisable() => slot.Clear();

        void Update()
        {
            if (!screen.isVisible) return;
            float t = Time.time * config.ScreenGlowRate * rateScale + phase;
            // A main swell plus a faint faster ripple: reads as an old tube, not a metronome.
            float wave = Mathf.Sin(t * 2f * Mathf.PI) * 0.8f + Mathf.Sin(t * 2f * Mathf.PI * 2.7f + 1.3f) * 0.2f;
            slot.Set(1f + config.ScreenGlowAmount * wave);
        }
    }
}
