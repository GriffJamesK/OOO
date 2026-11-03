using System.Collections.Generic;
using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// Every enabled threshold, and the current pairs. Re-pairs whenever a threshold registers,
    /// unregisters or its number source raises Changed, then publishes ThresholdsRepaired. Static
    /// because pairing is global: a number pairs anywhere in the building. Re-pairing is immediate
    /// (same frame), so a tile pressed into a plate rewires the building before the next step.
    /// </summary>
    public static class ThresholdRegistry
    {
        // A ThresholdsRepaired subscriber that changes a plate re-enters Repair; this caps how many
        // follow-up passes one call may run, so two listeners fighting over a plate can't hang the game.
        const int MaxRepairPasses = 8;

        static readonly List<Threshold> all = new List<Threshold>();
        // sources[i] is the number source all[i] was subscribed to (null = none), kept side by side
        // so it can be unsubscribed even after the threshold or the plate is destroyed.
        static readonly List<INumberSource> sources = new List<INumberSource>();
        static readonly List<Threshold> paired = new List<Threshold>();
        static readonly List<string> numbers = new List<string>();
        static bool repairing;
        static bool repairAgain;

        /// <summary>All enabled thresholds.</summary>
        public static IReadOnlyList<Threshold> All => all;

        /// <summary>Thresholds that currently have a partner (both halves of every pair).</summary>
        public static IReadOnlyList<Threshold> Paired => paired;

        /// <summary>Number of pairs after the last Repair.</summary>
        public static int PairCount { get; private set; }

        /// <summary>Drops last session's thresholds and subscriptions (domain reload is off).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            foreach (var s in sources) if (s != null) s.Changed -= OnSourceChanged;
            all.Clear();
            sources.Clear();
            paired.Clear();
            numbers.Clear();
            repairing = false;
            repairAgain = false;
            PairCount = 0;
        }

        /// <summary>Adds a threshold, listens to its plate, and re-pairs. Called from Threshold.OnEnable.</summary>
        public static void Register(Threshold t)
        {
            if (t == null) return;
            int index = IndexOf(t);
            if (index < 0)
            {
                all.Add(t);
                sources.Add(null);
                index = all.Count - 1;
            }
            Listen(index, t.Source);
            Repair();
        }

        /// <summary>Removes a threshold, stops listening to its plate, and re-pairs. Called from Threshold.OnDisable.</summary>
        public static void Unregister(Threshold t)
        {
            if (ReferenceEquals(t, null)) return;
            int index = IndexOf(t);
            if (index >= 0) RemoveAt(index);
            t.Partner = null; // no longer in the list, so Repair won't clear it
            Repair();
        }

        /// <summary>
        /// Recomputes every pair now and publishes ThresholdsRepaired. Safe to call from a
        /// ThresholdsRepaired or Changed handler: the nested call is folded into another pass.
        /// </summary>
        public static void Repair()
        {
            if (repairing) { repairAgain = true; return; }
            repairing = true;
            try
            {
                int passes = 0;
                do
                {
                    repairAgain = false;
                    RepairOnce();
                } while (repairAgain && ++passes < MaxRepairPasses);
            }
            finally
            {
                repairing = false;
                repairAgain = false;
            }
        }

        /// <summary>One pairing pass: prune destroyed thresholds, apply the rule, set partners, publish.</summary>
        static void RepairOnce()
        {
            PruneDestroyed();
            numbers.Clear();
            foreach (var t in all) numbers.Add(t.Number);
            int[] partners = ThresholdPairing.Resolve(numbers);

            paired.Clear();
            int pairCount = 0;
            for (int i = 0; i < all.Count; i++)
            {
                int p = partners[i];
                all[i].Partner = p >= 0 ? all[p] : null;
                if (p < 0) continue;
                paired.Add(all[i]);
                if (p > i) pairCount++;
            }
            PairCount = pairCount;
            EventBus.Publish(new ThresholdsRepaired(pairCount));
        }

        /// <summary>A plate changed (a tile pulled or placed): the whole building re-pairs.</summary>
        static void OnSourceChanged() => Repair();

        /// <summary>Subscribes entry <paramref name="index"/> to <paramref name="source"/>, dropping any older subscription.</summary>
        static void Listen(int index, INumberSource source)
        {
            var old = sources[index];
            if (ReferenceEquals(old, source)) return;
            if (old != null) old.Changed -= OnSourceChanged;
            sources[index] = source;
            if (source != null) source.Changed += OnSourceChanged;
        }

        /// <summary>Removes entry <paramref name="index"/> and its plate subscription.</summary>
        static void RemoveAt(int index)
        {
            var s = sources[index];
            if (s != null) s.Changed -= OnSourceChanged;
            all.RemoveAt(index);
            sources.RemoveAt(index);
        }

        /// <summary>
        /// Drops thresholds destroyed without OnDisable reaching us (scene unload order, editor
        /// teardown), so pairing never reads a dead object.
        /// </summary>
        static void PruneDestroyed()
        {
            for (int i = all.Count - 1; i >= 0; i--)
                if (all[i] == null) RemoveAt(i);
        }

        /// <summary>Index by reference: Unity's == treats every destroyed object as equal to null.</summary>
        static int IndexOf(Threshold t)
        {
            for (int i = 0; i < all.Count; i++)
                if (ReferenceEquals(all[i], t)) return i;
            return -1;
        }
    }
}
