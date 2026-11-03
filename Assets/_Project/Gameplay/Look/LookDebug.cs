using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Gameplay.Look
{
    /// <summary>Test hook for the look, via MCP: <c>return Kit.Gameplay.Look.LookDebug.Report();</c></summary>
    public static class LookDebug
    {
        /// <summary>Active Volume profile and the fog actually in RenderSettings.</summary>
        public static string Report()
        {
            var look = Object.FindAnyObjectByType<LookController>();
            if (look == null) return "Look: no LookController (is Sandbox loaded?)";
            var profile = look.GetComponent<Volume>().sharedProfile;
            return $"profile={(profile ? profile.name : "none")} " +
                   $"fog={RenderSettings.fog} {RenderSettings.fogStartDistance:0}-{RenderSettings.fogEndDistance:0}m";
        }
    }
}
