using System.Text;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Thresholds.ThresholdDebug.Report();</c></summary>
    public static class ThresholdDebug
    {
        /// <summary>
        /// Every threshold: name, number, partner, distance to the player and whether it is within
        /// the crossing search radius, plus the pair count. One line each, so a failing test message
        /// shows the whole building's wiring.
        /// </summary>
        public static string Report()
        {
            var all = ThresholdRegistry.All;
            var player = Object.FindAnyObjectByType<PlayerController>();
            float radius = ConfigLookup.Get<ThresholdConfig>().CrossingSearchRadius;
            var sb = new StringBuilder();
            sb.Append($"thresholds={all.Count} pairs={ThresholdRegistry.PairCount} player={(player != null ? player.transform.position.ToString("F2") : "none")}");
            foreach (var t in all)
            {
                if (t == null) { sb.Append("\n  (destroyed)"); continue; }
                string number = t.Number.Length > 0 ? t.Number : "-";
                string partner = t.Partner != null ? t.Partner.name : "-";
                string distance = "-";
                if (player != null)
                {
                    float d = Vector3.Distance(player.transform.position, t.transform.position);
                    distance = $"{d:0.0}m{(d <= radius ? " near" : "")}";
                }
                sb.Append($"\n  {t.name} #{number} -> {partner} dist={distance}{(t.Exterior ? " exterior" : "")}");
            }
            return sb.ToString();
        }
    }
}
