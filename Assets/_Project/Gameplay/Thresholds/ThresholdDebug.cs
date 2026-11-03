namespace Kit.Gameplay.Thresholds
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Thresholds.ThresholdDebug.Report();</c></summary>
    public static class ThresholdDebug
    {
        /// <summary>Every threshold: name, number, partner, and distance to the player.</summary>
        public static string Report()
        {
            // STUB: implemented by the Thresholds worker.
            return $"thresholds={ThresholdRegistry.All.Count}";
        }
    }
}
