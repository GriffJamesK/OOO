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
        /// <summary>Who holds a number so far: the first two indices seen, and how many share it.</summary>
        struct Holders
        {
            public int First;
            public int Second;
            public int Count;
        }

        /// <summary>
        /// For each index into <paramref name="rawNumbers"/>, the index of its partner, or -1.
        /// Numbers are normalised with ThresholdNumber.Normalize first, so letter O and digit 0 match.
        /// </summary>
        public static int[] Resolve(IReadOnlyList<string> rawNumbers)
        {
            if (rawNumbers == null) return new int[0];
            var partners = new int[rawNumbers.Count];
            var holders = new Dictionary<string, Holders>();
            for (int i = 0; i < partners.Length; i++)
            {
                partners[i] = -1;
                string number = ThresholdNumber.Normalize(rawNumbers[i]);
                if (number.Length == 0) continue; // a blank plate is no number at all
                holders.TryGetValue(number, out var h);
                if (h.Count == 0) h.First = i;
                else if (h.Count == 1) h.Second = i;
                h.Count++;
                holders[number] = h;
            }

            foreach (var h in holders.Values)
            {
                if (h.Count != 2) continue; // alone or crowded: an ordinary opening
                partners[h.First] = h.Second;
                partners[h.Second] = h.First;
            }
            return partners;
        }
    }
}
