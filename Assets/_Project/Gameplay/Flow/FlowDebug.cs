namespace Kit.Gameplay.Flow
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Flow.FlowDebug.Report();</c></summary>
    public static class FlowDebug
    {
        /// <summary>Safe ground, last safe pose, fade alpha, respawn and ending state in one line.</summary>
        public static string Report()
        {
            var t = FlowSystem.Tracker;
            string safe = t == null
                ? "tracker=none"
                : t.HasSafeGround
                    ? $"safe=yes pose={t.LastSafePose.position:F2} yaw={t.LastSafePose.rotation.eulerAngles.y:0} dwell={t.Dwell:0.00}"
                    : $"safe=no dwell={t.Dwell:0.00}";
            var p = FlowSystem.Player;
            string pos = p != null ? $"player={p.transform.position:F2}" : "player=none";
            return $"{pos} {safe} fade={ScreenFade.Alpha:0.00} respawning={FlowSystem.IsRespawning} ending={FlowSystem.EndingFired}";
        }

        /// <summary>Triggers a respawn now, as if the player fell (for checking the fade by eye).</summary>
        public static void Respawn() => FlowSystem.Respawn();
    }
}
