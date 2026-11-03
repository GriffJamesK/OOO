using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// A fluorescent light that now and then flickers (rarely, briefly) to make an empty office
    /// feel lived in and slightly wrong. Drives the Light on this object and, optionally, an
    /// emissive renderer. Timing comes from LookConfig.
    ///
    /// Gaps between flickers are random around LookConfig.FlickerInterval (exponential, like a
    /// failing tube, so they never fall into a rhythm), and each light starts at a random point,
    /// so a ceiling of them never flickers in sync. A flicker is a short burst of irregular
    /// dim/bright steps. Scaled time: the building holds its breath while the game is paused.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class FlickerLight : MonoBehaviour
    {
        [Tooltip("Optional emissive fixture that dims with the light.")]
        [SerializeField] Renderer fixture;

        public Renderer Fixture => fixture;

        /// <summary>True during a flicker burst (for tests and debugging).</summary>
        public bool Flickering => burstLeft > 0f;

        Light lamp;
        LookConfig config;
        float baseIntensity;
        EmissiveSlot slot;
        float untilNext;
        float burstLeft;
        float stepLeft;

        /// <summary>Builder entry point (edit mode).</summary>
        public void Setup(Renderer emissiveFixture) => fixture = emissiveFixture;

        void OnEnable()
        {
            lamp = GetComponent<Light>();
            config = LookAssets.Config;
            baseIntensity = lamp.intensity;
            slot = EmissiveSlot.Find(fixture);
            burstLeft = 0f;
            // Start part-way through a gap so lights enabled together don't share a schedule.
            untilNext = NextGap(config) * Random.value;
        }

        /// <summary>Starts a flicker burst now (tests, scripted moments). The burst still ends on its own.</summary>
        public void FlickerNow()
        {
            if (lamp == null || burstLeft > 0f) return;
            StartBurst();
        }

        /// <summary>A burst always opens with a drop-out (that is what catches the eye), then runs irregular steps.</summary>
        void StartBurst()
        {
            burstLeft = Random.Range(config.FlickerMinDuration, config.FlickerMaxDuration);
            stepLeft = Random.Range(config.FlickerStepMin, config.FlickerStepMax);
            Apply(config.FlickerDimLevel);
        }

        void OnDisable()
        {
            if (lamp != null) lamp.intensity = baseIntensity;
            slot.Clear();
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (burstLeft <= 0f)
            {
                untilNext -= dt;
                if (untilNext > 0f) return;
                StartBurst();
                return;
            }

            burstLeft -= dt;
            if (burstLeft <= 0f)
            {
                lamp.intensity = baseIntensity;
                slot.Clear();
                untilNext = NextGap(config);
                return;
            }

            stepLeft -= dt;
            if (stepLeft > 0f) return;
            stepLeft = Random.Range(config.FlickerStepMin, config.FlickerStepMax);
            // Mostly drop-outs to near dark, with the odd half-strength stutter and brief recoveries.
            float roll = Random.value;
            float level = roll < 0.5f ? config.FlickerDimLevel : roll < 0.75f ? Random.Range(0.35f, 0.7f) : 1f;
            Apply(level);
        }

        void Apply(float level)
        {
            lamp.intensity = baseIntensity * level;
            slot.Set(level);
        }

        /// <summary>Seconds to the next flicker: exponential around the configured average, clamped so it is never back-to-back or absurdly long.</summary>
        static float NextGap(LookConfig config)
        {
            float mean = config.FlickerInterval;
            float gap = -Mathf.Log(Mathf.Max(1e-4f, 1f - Random.value)) * mean;
            return Mathf.Clamp(gap, mean * 0.2f, mean * 4f);
        }
    }
}
