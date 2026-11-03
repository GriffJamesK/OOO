using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>Tunables for light flicker, screen glow and light shafts. Asset: Config/LookConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Look Config", fileName = "LookConfig")]
    public sealed class LookConfig : ConfigAsset
    {
        [Tooltip("Average seconds between flickers of a FlickerLight.")]
        public float FlickerInterval = 14f;

        [Tooltip("CRT glow pulse speed (cycles per second).")]
        public float ScreenGlowRate = 0.35f;

        [Tooltip("Light shaft material (Kit/LightShaft). Assigned by the Look builder.")]
        public Material LightShaftMaterial;

        protected override void Sanitize()
        {
            FlickerInterval = Mathf.Max(0.5f, FlickerInterval);
            ScreenGlowRate = Mathf.Max(0f, ScreenGlowRate);
        }
    }
}
