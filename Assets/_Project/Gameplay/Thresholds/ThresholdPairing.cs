using System.Collections.Generic;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// The pairing rule, pure: a non-empty normalised number pairs only when exactly two entries
    /// share it. One alone, or three and more, stay ordinary openings. Pure so the rule is tested
    /// without scenes.
    /// </summary>
    public static class ThresholdPairing
    {
        /// <summary>
        /// For each index into <paramref name="rawNumbers"/>, the index of its partner, or -1.
        /// Numbers are normalised with ThresholdNumber.Normalize first.
        /// </summary>
        public static int[] Resolve(IReadOnlyList<string> rawNumbers)
        {
            // STUB: implemented by the Thresholds worker. See ThresholdLogicTests.
            var partners = new int[rawNumbers.Count];
            for (int i = 0; i < partners.Length; i++) partners[i] = -1;
            return partners;
        }
    }
}
