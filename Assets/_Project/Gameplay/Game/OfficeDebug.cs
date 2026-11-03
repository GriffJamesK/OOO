using System.Collections.Generic;
using Kit.Gameplay.Letterboards;
using Kit.Gameplay.Thresholds;
using Unity.Cinemachine;
using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>
    /// Test hooks for the OOO world, called through MCP execute_code, e.g.
    /// <c>Kit.Gameplay.OfficeDebug.GoTo("A4"); return Kit.Gameplay.OfficeDebug.Report();</c>.
    /// Teleports to named points, aims the first-person camera for screenshots, and sets
    /// letterboard thresholds directly so routes (like the ending) can be checked without playing
    /// them by hand.
    /// </summary>
    public static class OfficeDebug
    {
        /// <summary>Named teleport points (feet position) and the yaw to face, one or more per area.</summary>
        static readonly Dictionary<string, (Vector3 position, float yaw)> points = new Dictionary<string, (Vector3, float)>
        {
            { "A1", (new Vector3(-40f, 16.05f, 0f), 90f) },
            { "A1_Corridor", (new Vector3(-22f, 16.05f, 0f), 0f) },
            { "A2", (new Vector3(0f, 0.05f, -18f), 0f) },
            { "A2_L3", (new Vector3(0f, 12.05f, 27f), 180f) },
            { "A2_L4", (new Vector3(-17.5f, 16.05f, 0f), 90f) },
            { "Pod", (new Vector3(8f, 24.05f, 0f), 90f) },
            { "A3", (new Vector3(0f, 12.05f, 33f), 0f) },
            { "A4", (new Vector3(25f, 4.05f, 0f), 90f) },
            { "A5", (new Vector3(-22f, 0.05f, -24f), 270f) },
            { "A5_Top", (new Vector3(-26f, 24.05f, -26f), 270f) },
            { "A6", (new Vector3(23f, 24.05f, 0f), 90f) },
            { "A7", (new Vector3(116f, 0.05f, 0f), 270f) },
        };

        static PlayerController Player => Object.FindAnyObjectByType<PlayerController>();

        /// <summary>Area, position, speed, and the thresholds summary.</summary>
        public static string Report()
        {
            var p = Player;
            string where = p != null ? $"pos={p.transform.position:F2} speed={p.HorizontalSpeed:0.0} grounded={p.IsGrounded}" : "player=none";
            return $"area={AreaZone.Current} {where} | {ThresholdDebug.Report()}";
        }

        /// <summary>Names accepted by GoTo.</summary>
        public static string Points() => string.Join(", ", points.Keys);

        /// <summary>Teleports the player to a named point (see Points) facing its yaw, momentum cleared.</summary>
        public static string GoTo(string name)
        {
            if (!points.TryGetValue(name, out var target)) return $"unknown point '{name}'. Known: {Points()}";
            return Teleport(target.position, target.yaw, 0f);
        }

        /// <summary>Teleports the player and aims the view (yaw degrees, pitch degrees: positive looks down).</summary>
        public static string Teleport(Vector3 feet, float yaw, float pitch)
        {
            var p = Player;
            if (p == null) return "no player (enter play mode and press Play)";
            var gm = Object.FindAnyObjectByType<GameManager>();
            var pose = new Pose(feet, Quaternion.Euler(0f, yaw, 0f));
            if (gm != null) gm.RespawnPlayer(pose); else p.Respawn(pose);
            Look(yaw, pitch);
            return Report();
        }

        /// <summary>Sets the first-person view direction directly (screenshots), so mouse drift doesn't matter.</summary>
        public static void Look(float yaw, float pitch)
        {
            var panTilt = Object.FindAnyObjectByType<CinemachinePanTilt>();
            if (panTilt == null) return;
            panTilt.PanAxis.Value = yaw;
            panTilt.TiltAxis.Value = pitch;
        }

        /// <summary>
        /// Overwrites the glyphs of the letterboard on the named threshold GameObject (e.g. the
        /// lobby cloakroom), as if the player had placed the tiles. Returns the new number and partner.
        /// </summary>
        public static string SetPlate(string thresholdName, string glyphs)
        {
            foreach (var t in ThresholdRegistry.All)
            {
                if (t == null || t.name != thresholdName) continue;
                var board = t.GetComponentInChildren<Letterboard>();
                if (board == null) return $"{thresholdName} has no letterboard";
                for (int i = 0; i < board.SlotCount; i++)
                {
                    board.TryPull(i, out _);
                    if (glyphs != null && i < glyphs.Length && glyphs[i] != ' ' && glyphs[i] != '_') board.TryPlace(i, glyphs[i]);
                }
                return $"{thresholdName} reads '{t.Number}', partner={(t.Partner != null ? t.Partner.name : "none")}";
            }
            return $"no threshold named {thresholdName}";
        }
    }
}
