using Kit.Core.Pooling;
using UnityEngine;

namespace Kit.Gameplay.Feel
{
    /// <summary>How a burst is emitted: a low cone ring (dust kicked out along the floor) or a small sphere (a puff in the air).</summary>
    public enum DustShape { Ring, Puff }

    /// <summary>
    /// Everything one burst needs, filled from FeelConfig by FeelSystem. One pooled ParticleSystem
    /// plays every kind of burst, so the pool stays small and owner tuning in Play Mode applies to
    /// the very next burst (nothing is baked into a template).
    /// </summary>
    public struct DustSpec
    {
        public string Label;
        public int Count;
        public DustShape Shape;
        public float Radius;
        public float ConeAngle;
        public Vector2 Speed;
        public Vector2 Size;
        public Vector2 Lifetime;
        public Color ColorA;
        public Color ColorB;
        public float Gravity;
        public float Drag;
        public float Growth;
        public float Noise;
        public float Orbit;
    }

    /// <summary>
    /// A pooled, code-configured particle burst (no prefab asset). It plays one DustSpec and
    /// returns itself to the pool once its particles are gone. Despawn is decided by polling
    /// IsAlive rather than the stop-action callback, because that callback can arrive a frame late
    /// and would then despawn the next burst this instance was reused for.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class FeelEffect : PooledBehaviour
    {
        const float SystemDuration = 0.05f;

        static readonly ParticleSystem.Burst[] burst = new ParticleSystem.Burst[1];
        static Gradient fadeGradient;

        ParticleSystem system;
        ParticleSystemRenderer systemRenderer;
        bool spawned;
        bool playing;
        float earliestEnd;
        float hardEnd;

        /// <summary>The label of the burst playing now (for FeelDebug), or null while pooled.</summary>
        public string Label { get; private set; }

        /// <summary>Domain reload is off: rebuild the shared gradient each session.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => fadeGradient = null;

        ParticleSystem Particles
        {
            get
            {
                if (system == null)
                {
                    system = GetComponent<ParticleSystem>();
                    systemRenderer = GetComponent<ParticleSystemRenderer>();
                }
                return system;
            }
        }

        /// <summary>
        /// Builds the inactive template the pool clones: a ParticleSystem with the settings no burst
        /// changes (one short non-looping emission, world space, no shadows). Inactive, so nothing
        /// plays or simulates on the template itself.
        /// </summary>
        public static FeelEffect CreateTemplate(Transform parent)
        {
            var go = new GameObject("FeelEffect");
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>(); // inactive and never played, so its duration can be set

            var main = ps.main;
            main.duration = SystemDuration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;
            main.maxParticles = 256;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.arc = 360f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeGradient());

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.alignment = ParticleSystemRenderSpace.View;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            psr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            psr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            psr.maxParticleSize = 2f;
            psr.enabled = false;

            return go.AddComponent<FeelEffect>();
        }

        /// <summary>Fade in quickly, hold, then thin out to nothing: dust appears, hangs, and is gone.</summary>
        static Gradient FadeGradient()
        {
            if (fadeGradient != null) return fadeGradient;
            fadeGradient = new Gradient();
            fadeGradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f),
                });
            return fadeGradient;
        }

        /// <summary>
        /// Configures the pooled system for <paramref name="spec"/> at <paramref name="position"/>
        /// and plays it once. Without a material the renderer stays off: the burst still runs (so
        /// bookkeeping and tests behave the same) but draws nothing, never magenta.
        /// </summary>
        public void Play(in DustSpec spec, Vector3 position, Quaternion rotation, Material material)
        {
            var ps = Particles;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.SetPositionAndRotation(position, rotation);
            Label = spec.Label;

            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.Lifetime.x, spec.Lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(spec.Speed.x, spec.Speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.Size.x, spec.Size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(spec.ColorA, spec.ColorB);
            main.gravityModifier = spec.Gravity;

            var emission = ps.emission;
            burst[0] = new ParticleSystem.Burst(0f, (short)Mathf.Clamp(spec.Count, 0, 256));
            emission.SetBursts(burst);

            var shape = ps.shape;
            shape.radiusThickness = spec.Shape == DustShape.Ring ? 0f : 1f; // ring: emit from the rim only
            if (spec.Shape == DustShape.Ring)
            {
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = spec.ConeAngle;
                shape.rotation = new Vector3(-90f, 0f, 0f); // the cone's axis points up, so the ring lies on the floor
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.rotation = Vector3.zero;
            }
            shape.radius = Mathf.Max(0.0001f, spec.Radius);

            // All velocity curves stay in Constant mode: Unity warns when they are mixed.
            var vel = ps.velocityOverLifetime;
            vel.enabled = spec.Orbit != 0f;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = 0f; vel.y = 0f; vel.z = 0f;
            vel.orbitalX = 0f; vel.orbitalY = spec.Orbit; vel.orbitalZ = 0f;
            vel.radial = 0f;
            vel.speedModifier = 1f;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = spec.Drag > 0f;
            limit.limit = 100f; // only the drag is wanted, never a hard clamp
            limit.dampen = 0f;
            limit.drag = spec.Drag;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, spec.Growth));

            var noise = ps.noise;
            noise.enabled = spec.Noise > 0f;
            noise.strength = spec.Noise;
            noise.frequency = 0.6f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;

            systemRenderer.sharedMaterial = material;
            systemRenderer.enabled = material != null;

            ps.Play(true);
            playing = true;
            earliestEnd = Time.time + SystemDuration;
            hardEnd = Time.time + SystemDuration + spec.Lifetime.y + 0.5f;
        }

        /// <summary>Returns to the pool once the burst has emitted and every particle has died (or a safety time passes).</summary>
        void Update()
        {
            if (!playing) return;
            float now = Time.time;
            if (now < earliestEnd) return;
            if (!Particles.IsAlive(true) || now >= hardEnd) Finish();
        }

        /// <summary>Ends this burst now and returns it to the pool. Safe to call twice: a pooled effect is never released again.</summary>
        public void Finish()
        {
            if (!spawned) return;
            spawned = false;
            playing = false;
            Despawn();
        }

        /// <summary>Fresh from the pool: tracked as active until it finishes.</summary>
        public override void OnSpawned()
        {
            spawned = true;
            playing = false;
            FeelSystem.Track(this);
        }

        /// <summary>Back to the pool: untracked, particles cleared so a reuse starts empty.</summary>
        public override void OnDespawned()
        {
            spawned = false;
            playing = false;
            Label = null;
            FeelSystem.Forget(this);
            if (gameObject.activeInHierarchy) Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
