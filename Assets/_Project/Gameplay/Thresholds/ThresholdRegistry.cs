using System.Collections.Generic;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// Every enabled threshold, and the current pairs. Re-pairs whenever a threshold registers,
    /// unregisters or its number source raises Changed, then publishes ThresholdsRepaired. Static
    /// because pairing is global: a number pairs anywhere in the building.
    /// </summary>
    public static class ThresholdRegistry
    {
        static readonly List<Threshold> all = new List<Threshold>();

        /// <summary>All enabled thresholds.</summary>
        public static IReadOnlyList<Threshold> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        /// <summary>Adds a threshold and re-pairs. Called from Threshold.OnEnable.</summary>
        public static void Register(Threshold t)
        {
            // STUB: implemented by the Thresholds worker (subscribe to t.Source.Changed, re-pair).
            if (t != null && !all.Contains(t)) all.Add(t);
        }

        /// <summary>Removes a threshold and re-pairs. Called from Threshold.OnDisable.</summary>
        public static void Unregister(Threshold t)
        {
            // STUB: implemented by the Thresholds worker.
            all.Remove(t);
        }

        /// <summary>Recomputes every pair now and publishes ThresholdsRepaired.</summary>
        public static void Repair()
        {
            // STUB: implemented by the Thresholds worker using ThresholdPairing.Resolve.
        }
    }
}
