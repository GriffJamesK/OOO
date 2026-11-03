namespace Kit.Gameplay.Flow
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Flow.FlowDebug.Report();</c></summary>
    public static class FlowDebug
    {
        /// <summary>Last safe ground, fade alpha, and ending state.</summary>
        public static string Report()
        {
            // STUB: implemented by the Flow worker.
            return $"fade={ScreenFade.Alpha:0.00}";
        }
    }
}
