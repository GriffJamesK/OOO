namespace Kit.Gameplay.Letterboards
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Letterboards.LetterboardDebug.Report();</c></summary>
    public static class LetterboardDebug
    {
        /// <summary>Carried tiles and the nearest board's contents.</summary>
        public static string Report()
        {
            // STUB: implemented by the Letterboard worker.
            return "letterboard: stub";
        }
    }
}
