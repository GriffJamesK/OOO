using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Feel
{
    /// <summary>
    /// Tunables for impact feel: the landing jolt and the dust, felt and grit particles. The game is
    /// quiet and melancholic, so every default here is deliberately small: a jolt you feel more than
    /// see, dust that settles rather than sparkles. Asset: Config/FeelConfig.asset (OfficeBuilder.Feel
    /// assigns DustMaterial). Ranges are (min, max) and each particle picks a value between them.
    /// </summary>
    [CreateAssetMenu(menuName = "Kit/Config/Feel Config", fileName = "FeelConfig")]
    public sealed class FeelConfig : ConfigAsset
    {
        [Header("Shared")]
        [Tooltip("Soft round particle material (URP Particles/Unlit, alpha blended). Built by OfficeBuilder.Feel. " +
                 "Without it a runtime stand-in is used, or the effect is invisible if even that is unavailable.")]
        public Material DustMaterial;

        [Tooltip("Concrete dust colour; alpha is the peak opacity of one particle.")]
        public Color DustColor = new Color(0.78f, 0.75f, 0.70f, 0.30f);

        [Tooltip("A second, slightly darker dust tone; each particle picks between the two.")]
        public Color DustColorDark = new Color(0.62f, 0.60f, 0.57f, 0.24f);

        [Tooltip("Air drag on dust (1/s). Dust kicks out, then stalls and hangs.")]
        public float DustDrag = 3f;

        [Tooltip("Gravity on dust as a fraction of world gravity. Tiny, so it settles slowly.")]
        public float DustGravity = 0.03f;

        [Tooltip("Size at the end of a dust particle's life, relative to its start (dust spreads as it thins).")]
        public float DustGrowth = 1.7f;

        [Tooltip("Effects alive at once. A new one past this recycles the oldest, so the pool never grows past it.")]
        public int MaxActiveEffects = 12;

        [Tooltip("Effects built when the pool is first created, so the first landing never hitches.")]
        public int PoolPrewarm = 4;

        [Header("Landing")]
        [Tooltip("Fall speed (m/s) at or above which a landing makes dust and a jolt. A normal jump lands at about 7.")]
        public float LandMinFallSpeed = 9f;

        [Tooltip("Fall speed (m/s) at which the landing response reaches its normal maximum.")]
        public float LandFullFallSpeed = 18f;

        [Tooltip("Fall speed (m/s) above which a landing counts as a big fall (the A5 launch arrival) and gets a little more.")]
        public float LandBigFallSpeed = 20f;

        [Tooltip("Camera jolt amplitude (m) at LandMinFallSpeed and at LandFullFallSpeed.")]
        public Vector2 LandShake = new Vector2(0.012f, 0.03f);

        [Tooltip("Camera jolt amplitude (m) for a big fall.")]
        public float LandShakeBig = 0.05f;

        [Tooltip("Hard cap on the landing jolt (m), whatever the fall.")]
        public float LandShakeCap = 0.06f;

        [Tooltip("Jolt duration (s) at LandMinFallSpeed and at LandFullFallSpeed.")]
        public Vector2 LandShakeDuration = new Vector2(0.12f, 0.2f);

        [Tooltip("Jolt duration (s) for a big fall.")]
        public float LandShakeDurationBig = 0.3f;

        [Tooltip("Dust particles at LandMinFallSpeed and at LandFullFallSpeed.")]
        public Vector2Int LandDustCount = new Vector2Int(12, 22);

        [Tooltip("Dust particles for a big fall.")]
        public int LandDustCountBig = 30;

        [Tooltip("Radius (m) of the ring of dust round the feet; a big fall uses BigFallScale times this.")]
        public float LandDustRadius = 0.35f;

        [Tooltip("Outward speed (m/s) of landing dust.")]
        public Vector2 LandDustSpeed = new Vector2(0.6f, 1.3f);

        [Tooltip("Start size (m) of landing dust.")]
        public Vector2 LandDustSize = new Vector2(0.25f, 0.55f);

        [Tooltip("Lifetime (s) of landing dust.")]
        public Vector2 LandDustLifetime = new Vector2(0.9f, 1.3f);

        [Tooltip("Cone angle (deg from straight up) of the dust ring. Near 90 keeps it low along the floor.")]
        public float LandDustConeAngle = 80f;

        [Tooltip("Big falls scale the ring's radius, speed and size by this.")]
        public float BigFallScale = 1.35f;

        [Header("Slide")]
        [Tooltip("Dust particles in the burst when a boosted slide starts.")]
        public int SlideDustCount = 9;

        [Tooltip("Radius (m) of the slide burst at the feet.")]
        public float SlideDustRadius = 0.25f;

        [Tooltip("Outward speed (m/s) of slide dust.")]
        public Vector2 SlideDustSpeed = new Vector2(0.4f, 0.9f);

        [Tooltip("Start size (m) of slide dust.")]
        public Vector2 SlideDustSize = new Vector2(0.18f, 0.38f);

        [Tooltip("Lifetime (s) of slide dust.")]
        public Vector2 SlideDustLifetime = new Vector2(0.7f, 1.1f);

        [Tooltip("Opacity multiplier for slide dust (fainter than a landing).")]
        public float SlideDustAlpha = 0.75f;

        [Header("Letterboard tiles")]
        [Tooltip("Particles in the puff when a tile is pulled or placed.")]
        public int TileParticleCount = 7;

        [Tooltip("Felt fibre colour (dark, like the board's felt).")]
        public Color FibreColor = new Color(0.16f, 0.15f, 0.15f, 0.75f);

        [Tooltip("Pale grit colour mixed in with the fibres.")]
        public Color TileDustColor = new Color(0.80f, 0.78f, 0.74f, 0.45f);

        [Tooltip("Distance (m) in front of the felt where the puff appears.")]
        public float TileOffset = 0.03f;

        [Tooltip("Radius (m) of the puff.")]
        public float TileRadius = 0.05f;

        [Tooltip("Start size (m) of fibres and grit.")]
        public Vector2 TileSize = new Vector2(0.012f, 0.035f);

        [Tooltip("Initial drift speed (m/s).")]
        public Vector2 TileSpeed = new Vector2(0.03f, 0.12f);

        [Tooltip("Lifetime (s): fibres drift down slowly.")]
        public Vector2 TileLifetime = new Vector2(1.6f, 2.6f);

        [Tooltip("Gravity on fibres as a fraction of world gravity.")]
        public float TileGravity = 0.02f;

        [Tooltip("Air turbulence on fibres (m/s), so they wander as they fall.")]
        public float TileNoise = 0.06f;

        [Header("Threshold exit")]
        [Tooltip("A very faint swirl of floor dust under the exit opening when arriving fast. Nothing ever plays at the player.")]
        public bool ExitSwirlEnabled = true;

        [Tooltip("Crossing speed (m/s) at or above which the exit swirl plays.")]
        public float ExitSwirlMinSpeed = 15f;

        [Tooltip("Particles in the exit swirl.")]
        public int ExitSwirlCount = 8;

        [Tooltip("Radius (m) of the swirl.")]
        public float ExitSwirlRadius = 0.5f;

        [Tooltip("Orbit speed of the swirl (rad/s).")]
        public float ExitSwirlOrbit = 1.4f;

        [Tooltip("Lifetime (s) of the swirl.")]
        public Vector2 ExitSwirlLifetime = new Vector2(1.2f, 1.8f);

        [Tooltip("Opacity multiplier for the swirl (fainter than a landing).")]
        public float ExitSwirlAlpha = 0.5f;

        /// <summary>Keeps every value in a range that can't produce errors (negative counts, inverted ranges, zero pools).</summary>
        protected override void Sanitize()
        {
            DustDrag = Mathf.Max(0f, DustDrag);
            DustGravity = Mathf.Max(0f, DustGravity);
            DustGrowth = Mathf.Max(0.1f, DustGrowth);
            MaxActiveEffects = Mathf.Clamp(MaxActiveEffects, 1, 64);
            PoolPrewarm = Mathf.Clamp(PoolPrewarm, 0, MaxActiveEffects);

            LandMinFallSpeed = Mathf.Max(0f, LandMinFallSpeed);
            LandFullFallSpeed = Mathf.Max(LandMinFallSpeed + 0.01f, LandFullFallSpeed);
            LandBigFallSpeed = Mathf.Max(LandMinFallSpeed, LandBigFallSpeed);
            LandShake = Ordered(LandShake, 0f);
            LandShakeBig = Mathf.Max(0f, LandShakeBig);
            LandShakeCap = Mathf.Max(0f, LandShakeCap);
            LandShakeDuration = Ordered(LandShakeDuration, 0f);
            LandShakeDurationBig = Mathf.Max(0f, LandShakeDurationBig);
            LandDustCount = new Vector2Int(Mathf.Clamp(LandDustCount.x, 0, 200), Mathf.Clamp(Mathf.Max(LandDustCount.x, LandDustCount.y), 0, 200));
            LandDustCountBig = Mathf.Clamp(LandDustCountBig, 0, 200);
            LandDustRadius = Mathf.Max(0f, LandDustRadius);
            LandDustSpeed = Ordered(LandDustSpeed, 0f);
            LandDustSize = Ordered(LandDustSize, 0.001f);
            LandDustLifetime = Ordered(LandDustLifetime, 0.05f);
            LandDustConeAngle = Mathf.Clamp(LandDustConeAngle, 0f, 90f);
            BigFallScale = Mathf.Max(0f, BigFallScale);

            SlideDustCount = Mathf.Clamp(SlideDustCount, 0, 200);
            SlideDustRadius = Mathf.Max(0f, SlideDustRadius);
            SlideDustSpeed = Ordered(SlideDustSpeed, 0f);
            SlideDustSize = Ordered(SlideDustSize, 0.001f);
            SlideDustLifetime = Ordered(SlideDustLifetime, 0.05f);
            SlideDustAlpha = Mathf.Clamp01(SlideDustAlpha);

            TileParticleCount = Mathf.Clamp(TileParticleCount, 0, 200);
            TileOffset = Mathf.Max(0f, TileOffset);
            TileRadius = Mathf.Max(0f, TileRadius);
            TileSize = Ordered(TileSize, 0.001f);
            TileSpeed = Ordered(TileSpeed, 0f);
            TileLifetime = Ordered(TileLifetime, 0.05f);
            TileGravity = Mathf.Max(0f, TileGravity);
            TileNoise = Mathf.Max(0f, TileNoise);

            ExitSwirlMinSpeed = Mathf.Max(0f, ExitSwirlMinSpeed);
            ExitSwirlCount = Mathf.Clamp(ExitSwirlCount, 0, 200);
            ExitSwirlRadius = Mathf.Max(0f, ExitSwirlRadius);
            ExitSwirlLifetime = Ordered(ExitSwirlLifetime, 0.05f);
            ExitSwirlAlpha = Mathf.Clamp01(ExitSwirlAlpha);
        }

        /// <summary>A (min, max) range with min at least <paramref name="floor"/> and max at least min.</summary>
        static Vector2 Ordered(Vector2 range, float floor)
        {
            float a = Mathf.Max(floor, range.x);
            return new Vector2(a, Mathf.Max(a, range.y));
        }
    }
}
