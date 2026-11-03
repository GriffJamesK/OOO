using System.Text;
using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Feel
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Feel.FeelDebug.Report();</c></summary>
    public static class FeelDebug
    {
        /// <summary>Install state, pool sizes, jolt counters, material source and the last few effects, in one line.</summary>
        public static string Report()
        {
            var cfg = ConfigLookup.Get<FeelConfig>();
            var sb = new StringBuilder();
            sb.Append("feel: installed=").Append(FeelSystem.Installed ? "yes" : "no")
              .Append(" | pool active ").Append(FeelSystem.PoolActive)
              .Append(" inactive ").Append(FeelSystem.PoolInactive)
              .Append(" (cap ").Append(cfg.MaxActiveEffects).Append(')')
              .Append(" | played ").Append(FeelSystem.EffectsPlayed)
              .Append(" | shakes ").Append(FeelSystem.ShakeCount)
              .Append(" last ").Append(FeelSystem.LastShakeAmplitude.ToString("0.000")).Append(" m/")
              .Append(FeelSystem.LastShakeDuration.ToString("0.00")).Append(" s")
              .Append(" | material ").Append(cfg.DustMaterial != null ? cfg.DustMaterial.name : "(runtime stand-in)");
            sb.Append(" | last: ");
            bool any = false;
            foreach (var r in FeelSystem.Recent)
            {
                if (any) sb.Append(", ");
                sb.Append(r);
                any = true;
            }
            if (!any) sb.Append("(none)");
            return sb.ToString();
        }

        /// <summary>
        /// Plays a landing as if the player fell at <paramref name="fallSpeed"/> m/s, on the floor
        /// 1.5 m ahead of the main camera (so it can be seen in a screenshot), and returns the report.
        /// </summary>
        public static string Land(float fallSpeed = 15f)
        {
            var cam = Camera.main;
            Vector3 at = Vector3.zero;
            if (cam != null)
            {
                Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                at = cam.transform.position + flatForward * 1.5f + Vector3.down * 1.6f;
            }
            EventBus.Publish(new PlayerLanded(at, fallSpeed));
            return Report();
        }
    }
}
