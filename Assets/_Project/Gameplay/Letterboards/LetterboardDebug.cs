using System.Text;
using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>Test hooks, called through MCP execute_code: <c>return Kit.Gameplay.Letterboards.LetterboardDebug.Report();</c></summary>
    public static class LetterboardDebug
    {
        const float NearRadius = 5f;

        /// <summary>Carried tiles and the nearest board's contents (within 5 m of the player).</summary>
        public static string Report()
        {
            var sb = new StringBuilder();
            var carrier = Object.FindAnyObjectByType<TileCarrier>();
            if (carrier == null)
            {
                sb.Append("letterboard: no TileCarrier (no player spawned?)");
            }
            else
            {
                int max = ConfigLookup.Get<LetterboardConfig>().MaxCarried;
                sb.Append($"letterboard: carried [{new string(ToArray(carrier))}] ({carrier.Carried.Count}/{max})");
            }

            var boards = Object.FindObjectsByType<Letterboard>(FindObjectsInactive.Exclude);
            sb.Append($"; boards {boards.Length}");
            if (carrier != null)
            {
                Vector3 p = carrier.transform.position;
                Letterboard nearest = null;
                float best = NearRadius;
                foreach (var b in boards)
                {
                    float d = Vector3.Distance(p, b.transform.position);
                    if (d <= best) { best = d; nearest = b; }
                }
                if (nearest == null) sb.Append($"; no board within {NearRadius:0} m");
                else sb.Append($"; nearest '{nearest.name}' at {best:0.0} m ({nearest.Rows}x{nearest.Columns}) \"{nearest.Raw.Replace(' ', '_')}\"");
            }
            return sb.ToString();
        }

        /// <summary>Puts a tile in the player's hand. False when there's no carrier or the hand is full.</summary>
        public static bool Give(char glyph)
        {
            var carrier = Object.FindAnyObjectByType<TileCarrier>();
            return carrier != null && carrier.Give(glyph);
        }

        /// <summary>Empties the player's hand.</summary>
        public static void Clear()
        {
            var carrier = Object.FindAnyObjectByType<TileCarrier>();
            if (carrier != null) carrier.Clear();
        }

        static char[] ToArray(TileCarrier carrier)
        {
            var list = carrier.Carried;
            var result = new char[list.Count];
            for (int i = 0; i < result.Length; i++) result[i] = list[i];
            return result;
        }
    }
}
