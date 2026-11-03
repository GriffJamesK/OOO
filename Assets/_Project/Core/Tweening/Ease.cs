using UnityEngine;

namespace Kit.Core.Tweening
{
    /// <summary>Easing curves. Small on purpose: these five cover nearly all prototype feel work.</summary>
    public enum Ease
    {
        Linear,
        OutQuad,
        OutBack,
        OutElastic,
        InOutSine
    }

    /// <summary>Maps normalised time (0..1) through an Ease curve.</summary>
    public static class Easing
    {
        /// <summary>Returns the eased progress for t in [0,1]. OutBack/OutElastic overshoot past 1 mid-curve by design.</summary>
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + c1 * u * u;
                }
                case Ease.OutElastic:
                {
                    if (t <= 0f || t >= 1f) return t;
                    const float c4 = 2f * Mathf.PI / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case Ease.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                default:
                    return t;
            }
        }
    }
}
