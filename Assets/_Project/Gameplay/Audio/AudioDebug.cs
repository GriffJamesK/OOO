namespace Kit.Gameplay.Audio
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Audio.AudioDebug.Report();</c></summary>
    public static class AudioDebug
    {
        /// <summary>Clips built, current ambience, and the last few sounds played.</summary>
        public static string Report()
        {
            // STUB: implemented by the Audio worker.
            return "audio: stub";
        }
    }
}
