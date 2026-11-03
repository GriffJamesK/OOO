namespace Kit.Gameplay.Portals
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Portals.PortalDebug.Report();</c></summary>
    public static class PortalDebug
    {
        /// <summary>Views alive, views rendering this frame, and their thresholds.</summary>
        public static string Report()
        {
            // STUB: implemented by the Portals worker.
            return $"portals active={PortalSystem.ActiveViewCount}";
        }
    }
}
