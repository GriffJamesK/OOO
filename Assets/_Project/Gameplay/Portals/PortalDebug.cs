using System.Text;
using UnityEngine;

namespace Kit.Gameplay.Portals
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Portals.PortalDebug.Report();</c></summary>
    public static class PortalDebug
    {
        /// <summary>
        /// Views alive, views rendering this frame, and per view: its threshold and partner, whether
        /// it renders, its distance to the main camera and whether the near-plane box is up.
        /// </summary>
        public static string Report()
        {
            var config = ConfigLookup.Get<PortalConfig>();
            var main = PortalSystem.MainCamera;
            var sb = new StringBuilder();
            sb.Append($"portals views={PortalSystem.Views.Count} active={PortalSystem.ActiveViewCount}/{config.MaxActive}")
              .Append($" maxDistance={config.MaxDistance:0.#} main={(main != null ? main.name : "none")}")
              .Append($" material={(config.SurfaceMaterial != null ? config.SurfaceMaterial.name : "runtime fallback")}");

            foreach (var v in PortalSystem.Views)
            {
                if (v == null) continue;
                var t = v.Threshold;
                string number = t != null ? t.Number : "?";
                string from = t != null ? t.name : "(destroyed)";
                string to = t != null && t.Partner != null ? t.Partner.name : "-";
                string distance = float.IsInfinity(v.Distance) ? "n/a" : v.Distance.ToString("0.0") + "m";
                var target = v.ViewCamera != null ? v.ViewCamera.targetTexture : null;
                sb.Append($"\n  [{number}] {from} -> {to}  {(v.Rendering ? "RENDERING" : "idle")}  d={distance}")
                  .Append($"  surface={(v.Extruded ? "box" : "quad")} side={v.ApproachSide:+0;-0;0}")
                  .Append(target != null ? $"  rt={target.width}x{target.height}" : "");
            }
            return sb.ToString();
        }

        /// <summary>The view on the threshold named <paramref name="thresholdName"/>, or null.</summary>
        public static PortalView Find(string thresholdName)
        {
            foreach (var v in PortalSystem.Views)
                if (v != null && v.Threshold != null && v.Threshold.name == thresholdName) return v;
            return null;
        }

        /// <summary>Saves what a view's camera rendered last to <paramref name="path"/> (PNG, HDR values clamped, no grade) for visual checks.</summary>
        public static string SaveViewTexture(string thresholdName, string path)
        {
            var v = Find(thresholdName);
            var rt = v != null && v.ViewCamera != null ? v.ViewCamera.targetTexture : null;
            if (rt == null) return $"no rendering view on '{thresholdName}'";

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            if (Application.isPlaying) Object.Destroy(tex);
            else Object.DestroyImmediate(tex);
            return $"saved {rt.width}x{rt.height} to {path}";
        }
    }
}
