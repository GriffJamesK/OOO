using System.Collections.Generic;
using Kit.Core;
using Kit.Core.Events;
using Kit.Core.Feel;
using Kit.Core.Pooling;
using Kit.Gameplay.Letterboards;
using UnityEngine;

namespace Kit.Gameplay.Feel
{
    /// <summary>
    /// Self-installing impact feel, kept quiet to suit the game's mood: a small camera jolt and a
    /// low ring of concrete dust on hard landings, a faint dust burst when a boosted slide starts, a
    /// tiny puff of felt fibres when a letterboard tile is pulled or placed, and (optionally) a very
    /// faint swirl of floor dust under the exit opening after a fast threshold crossing. There is no
    /// combat, so no hit-stop, and nothing ever plays at the player on a crossing (crossings must
    /// feel seamless). All numbers come from FeelConfig; bursts are pooled FeelEffects built in code.
    /// </summary>
    public static class FeelSystem
    {
        const int RecentCapacity = 6;

        // Bursts at the feet start a little above the floor, so the soft-particle fade blends their
        // lower edge into the floor instead of the floor hiding half of every dust blob.
        const float FootLift = 0.08f;

        static readonly List<FeelEffect> active = new List<FeelEffect>();
        static readonly Queue<string> recent = new Queue<string>();

        static bool installed;
        static ObjectPool<FeelEffect> pool;
        static FeelEffect template;

        /// <summary>Camera jolts requested so far this session (counted even when there is no camera, for tests).</summary>
        public static int ShakeCount { get; private set; }

        /// <summary>Amplitude (m) of the last jolt requested.</summary>
        public static float LastShakeAmplitude { get; private set; }

        /// <summary>Duration (s) of the last jolt requested.</summary>
        public static float LastShakeDuration { get; private set; }

        /// <summary>Bursts played so far this session.</summary>
        public static int EffectsPlayed { get; private set; }

        /// <summary>True once the event handlers are subscribed this session.</summary>
        public static bool Installed => installed;

        /// <summary>Bursts playing now.</summary>
        public static int PoolActive => pool != null ? pool.CountActive : 0;

        /// <summary>Bursts parked in the pool.</summary>
        public static int PoolInactive => pool != null ? pool.CountInactive : 0;

        /// <summary>Every burst instance the pool has built (never more than FeelConfig.MaxActiveEffects).</summary>
        public static int PoolTotal => PoolActive + PoolInactive;

        /// <summary>The last few bursts and jolts, newest last, for FeelDebug.</summary>
        public static IEnumerable<string> Recent => recent;

        static FeelConfig Cfg => ConfigLookup.Get<FeelConfig>();

        /// <summary>Domain reload is off: forget the last session's pool (its objects died with play mode) and counters.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            recent.Clear();
            installed = false;
            pool = null;
            template = null;
            ShakeCount = 0;
            LastShakeAmplitude = 0f;
            LastShakeDuration = 0f;
            EffectsPlayed = 0;
        }

        /// <summary>
        /// Subscribes on the first scene load and prewarms the pool then, so the first hard landing
        /// never pays for building particle systems mid-run.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            EnsureInstalled();
            if (Application.isPlaying) EnsurePool(Cfg);
        }

        /// <summary>
        /// Subscribes the handlers. Each is unsubscribed first, so calling this again (tests, a second
        /// scene load) can never subscribe twice and play every effect double.
        /// </summary>
        public static void EnsureInstalled()
        {
            EventBus.Unsubscribe<PlayerLanded>(OnLanded);
            EventBus.Unsubscribe<PlayerSlid>(OnSlid);
            EventBus.Unsubscribe<TilePulled>(OnTilePulled);
            EventBus.Unsubscribe<TilePlaced>(OnTilePlaced);
            EventBus.Unsubscribe<PlayerCrossedThreshold>(OnCrossed);

            EventBus.Subscribe<PlayerLanded>(OnLanded);
            EventBus.Subscribe<PlayerSlid>(OnSlid);
            EventBus.Subscribe<TilePulled>(OnTilePulled);
            EventBus.Subscribe<TilePlaced>(OnTilePlaced);
            EventBus.Subscribe<PlayerCrossedThreshold>(OnCrossed);
            installed = true;
        }

        /// <summary>Ends every playing burst now and returns it to the pool (tests, scene resets).</summary>
        public static void ClearEffects()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var e = active[i];
                if (e != null) e.Finish();
                else active.RemoveAt(i);
            }
        }

        // ---------------------------------------------------------------- handlers

        /// <summary>
        /// Hard landings only (normal jumps land under LandMinFallSpeed and get nothing). The jolt and
        /// the dust both scale with fall speed and are capped; a big fall (the A5 launch) gets a step more.
        /// </summary>
        static void OnLanded(PlayerLanded e)
        {
            var cfg = Cfg;
            if (e.FallSpeed < cfg.LandMinFallSpeed) return;
            bool big = e.FallSpeed >= cfg.LandBigFallSpeed;
            float t = Mathf.InverseLerp(cfg.LandMinFallSpeed, cfg.LandFullFallSpeed, e.FallSpeed);

            float amplitude = big ? cfg.LandShakeBig : Mathf.Lerp(cfg.LandShake.x, cfg.LandShake.y, t);
            float duration = big ? cfg.LandShakeDurationBig : Mathf.Lerp(cfg.LandShakeDuration.x, cfg.LandShakeDuration.y, t);
            RequestShake(Mathf.Min(amplitude, cfg.LandShakeCap), duration);

            float scale = big ? cfg.BigFallScale : 1f;
            int count = big ? cfg.LandDustCountBig : Mathf.RoundToInt(Mathf.Lerp(cfg.LandDustCount.x, cfg.LandDustCount.y, t));
            var spec = new DustSpec
            {
                Label = big ? "land-big" : "land",
                Count = count,
                Shape = DustShape.Ring,
                Radius = cfg.LandDustRadius * scale,
                ConeAngle = cfg.LandDustConeAngle,
                Speed = cfg.LandDustSpeed * scale,
                Size = cfg.LandDustSize * Mathf.Lerp(1f, scale, 0.5f),
                Lifetime = cfg.LandDustLifetime,
                ColorA = cfg.DustColor,
                ColorB = cfg.DustColorDark,
                Gravity = cfg.DustGravity,
                Drag = cfg.DustDrag,
                Growth = cfg.DustGrowth,
            };
            Spawn(spec, e.Position + Vector3.up * FootLift, $"{spec.Label} {e.FallSpeed:0.0} m/s x{count}");
        }

        /// <summary>A short, fainter burst at the feet when a slide starts with a boost (unboosted re-slides stay clean).</summary>
        static void OnSlid(PlayerSlid e)
        {
            if (!e.Boosted) return;
            var cfg = Cfg;
            var spec = new DustSpec
            {
                Label = "slide",
                Count = cfg.SlideDustCount,
                Shape = DustShape.Ring,
                Radius = cfg.SlideDustRadius,
                ConeAngle = cfg.LandDustConeAngle,
                Speed = cfg.SlideDustSpeed,
                Size = cfg.SlideDustSize,
                Lifetime = cfg.SlideDustLifetime,
                ColorA = Faded(cfg.DustColor, cfg.SlideDustAlpha),
                ColorB = Faded(cfg.DustColorDark, cfg.SlideDustAlpha),
                Gravity = cfg.DustGravity,
                Drag = cfg.DustDrag,
                Growth = cfg.DustGrowth,
            };
            Spawn(spec, e.Position + Vector3.up * FootLift, $"slide {e.Speed:0.0} m/s");
        }

        static void OnTilePulled(TilePulled e) => TilePuff(e.Board, e.Slot, "tile-pull");
        static void OnTilePlaced(TilePlaced e) => TilePuff(e.Board, e.Slot, "tile-place");

        /// <summary>
        /// The "pickup" particle: a few dark felt fibres and pale grit, just in front of the slot,
        /// drifting down slowly and wandering a little. No board (or a destroyed one) means no puff.
        /// </summary>
        static void TilePuff(Letterboard board, int slot, string label)
        {
            if (board == null || slot < 0) return;
            var cfg = Cfg;
            var t = board.transform;
            Vector3 position = t.TransformPoint(board.SlotLocalCentre(slot)) + t.forward * cfg.TileOffset;
            var spec = new DustSpec
            {
                Label = label,
                Count = cfg.TileParticleCount,
                Shape = DustShape.Puff,
                Radius = cfg.TileRadius,
                Speed = cfg.TileSpeed,
                Size = cfg.TileSize,
                Lifetime = cfg.TileLifetime,
                ColorA = cfg.FibreColor,
                ColorB = cfg.TileDustColor,
                Gravity = cfg.TileGravity,
                Drag = cfg.DustDrag,
                Growth = 1f,
                Noise = cfg.TileNoise,
            };
            Spawn(spec, position, $"{label} slot {slot}");
        }

        /// <summary>
        /// Optional and deliberately faint: a slow swirl of floor dust under the exit opening, as if
        /// the arrival stirred the air. Placed at the opening's sill, never on the player or camera.
        /// </summary>
        static void OnCrossed(PlayerCrossedThreshold e)
        {
            var cfg = Cfg;
            if (!cfg.ExitSwirlEnabled || e.To == null || e.Speed < cfg.ExitSwirlMinSpeed) return;
            var t = e.To.transform;
            Vector3 sill = t.position - t.up * (e.To.Size.y * 0.5f) + Vector3.up * FootLift;
            var spec = new DustSpec
            {
                Label = "exit-swirl",
                Count = cfg.ExitSwirlCount,
                Shape = DustShape.Ring,
                Radius = cfg.ExitSwirlRadius,
                ConeAngle = 88f,
                Speed = new Vector2(0.05f, 0.2f),
                Size = cfg.LandDustSize,
                Lifetime = cfg.ExitSwirlLifetime,
                ColorA = Faded(cfg.DustColor, cfg.ExitSwirlAlpha),
                ColorB = Faded(cfg.DustColorDark, cfg.ExitSwirlAlpha),
                Gravity = cfg.DustGravity,
                Drag = cfg.DustDrag,
                Growth = cfg.DustGrowth,
                Orbit = cfg.ExitSwirlOrbit,
            };
            Spawn(spec, sill, $"exit-swirl {e.Speed:0.0} m/s");
        }

        // ---------------------------------------------------------------- plumbing

        /// <summary>Asks Juice for a jolt on the main camera and records it (the record is kept even without a camera, for tests).</summary>
        static void RequestShake(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            ShakeCount++;
            LastShakeAmplitude = amplitude;
            LastShakeDuration = duration;
            Remember($"shake {amplitude:0.000} m {duration:0.00} s");
            var cam = Camera.main;
            if (cam != null) Juice.Shake(cam, amplitude, duration);
        }

        /// <summary>
        /// Takes a burst from the pool and plays it. At the MaxActiveEffects cap the oldest burst
        /// is ended and reused instead, so a flurry of landings never grows the pool.
        /// </summary>
        static void Spawn(in DustSpec spec, Vector3 position, string note)
        {
            if (spec.Count <= 0 || !Application.isPlaying) return;
            var cfg = Cfg;
            if (!EnsurePool(cfg)) return;

            active.RemoveAll(a => a == null);
            for (int guard = active.Count; active.Count >= cfg.MaxActiveEffects && guard > 0; guard--)
            {
                var oldest = active[0];
                oldest.Finish();
                if (active.Count > 0 && active[0] == oldest) active.RemoveAt(0); // never loop on a stuck entry
            }

            var effect = pool.Get();
            effect.Play(spec, position, Quaternion.identity, FeelMaterials.Resolve(cfg));
            EffectsPlayed++;
            Remember(note);
        }

        /// <summary>
        /// Builds the pool on first use under the hidden Kit host (it survives scene loads). If the
        /// host was destroyed the old pool holds dead objects, so it is thrown away and rebuilt.
        /// </summary>
        static bool EnsurePool(FeelConfig cfg)
        {
            if (pool != null && template != null) return true;
            active.Clear();
            var host = KitHost.Ensure<FeelRunner>().transform;
            template = FeelEffect.CreateTemplate(host);
            pool = ObjectPool.ForPrefab(template, cfg.PoolPrewarm, host);
            return true;
        }

        internal static void Track(FeelEffect e)
        {
            if (e != null && !active.Contains(e)) active.Add(e);
        }

        internal static void Forget(FeelEffect e) => active.Remove(e);

        static void Remember(string note)
        {
            recent.Enqueue(note);
            while (recent.Count > RecentCapacity) recent.Dequeue();
        }

        static Color Faded(Color c, float alpha) => new Color(c.r, c.g, c.b, c.a * alpha);
    }
}
