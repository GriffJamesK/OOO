using System.Text;

namespace Kit.Gameplay.Audio
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Audio.AudioDebug.Report();</c></summary>
    public static class AudioDebug
    {
        /// <summary>Clips built, current ambience, and the last few sounds played.</summary>
        public static string Report()
        {
            var host = AudioDirector.Host;
            var sb = new StringBuilder();
            sb.Append("audio: ");
            if (host == null) sb.Append("not installed (play mode only)");
            else
            {
                string key = host.AmbienceKey;
                sb.Append("listener=").Append(host.ListenerPresent ? "yes" : "NO")
                  .Append(" paused=").Append(host.Paused ? "yes" : "no")
                  .Append(" player=").Append(host.HasPlayer ? "yes" : "no")
                  .Append(" | ambience='").Append(key == null ? "(none)" : key).Append("'")
                  .Append(" gains ").Append(host.AmbienceGain(0).ToString("0.00")).Append('/').Append(host.AmbienceGain(1).ToString("0.00"))
                  .Append(" duck ").Append(host.Duck.ToString("0.00"));
            }
            sb.Append(" | clips ").Append(AudioDirector.SfxClipCount).Append(" sfx + ")
              .Append(AudioDirector.AmbienceClipCount).Append('/').Append(ProcSynth.AmbienceIds.Length).Append(" ambience, synth ")
              .Append(AudioDirector.SynthMilliseconds.ToString("0")).Append(" ms");
            sb.Append(" | last: ").Append(AudioDirector.Recent.Count == 0 ? "(none)" : string.Join(", ", AudioDirector.Recent));
            return sb.ToString();
        }
    }
}
