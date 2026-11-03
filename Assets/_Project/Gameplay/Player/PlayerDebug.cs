using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>Test hooks for the player, called through MCP execute_code: <c>return Kit.Gameplay.PlayerDebug.Report();</c></summary>
    public static class PlayerDebug
    {
        /// <summary>Position, grounded and input state of the live player.</summary>
        public static string Report()
        {
            var p = Object.FindAnyObjectByType<PlayerController>();
            if (p == null) return "Player: none";
            return $"pos={p.transform.position:F2} grounded={p.IsGrounded} input={p.InputEnabled} " +
                   $"speed={p.HorizontalSpeed:0.0} sliding={p.IsSliding} wall={p.IsWallRunning} surf={p.IsSurfing} low={p.IsLow} chain={p.ChainCount} " +
                   $"fov={p.CurrentFieldOfView:0.0} roll={p.CurrentCameraRoll:0.0}";
        }

        /// <summary>Moves the player with momentum cleared; handy for testing a course section or taking screenshots.</summary>
        public static void Teleport(Vector3 position)
        {
            var p = Object.FindAnyObjectByType<PlayerController>();
            if (p != null) p.Respawn(new Pose(position, p.transform.rotation));
        }
    }
}
