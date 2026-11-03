using System.Text;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// The building's reading of a plate. Uppercase, empty slots ('_' and spaces) dropped, and the
    /// letter O read as the digit 0, because the building can't tell them apart (the K4 secret:
    /// letters from "OUT OF OFFICE" make 000). Pure, so edit-mode tests cover it.
    /// </summary>
    public static class ThresholdNumber
    {
        /// <summary>
        /// Normalised number for pairing; "" means no number (never pairs). Blank slots are dropped
        /// rather than kept as gaps so "_3_7" on a letterboard reads the same as a brass "37": the
        /// building only cares which characters are shown, not where they sit.
        /// </summary>
        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (c == '_' || char.IsWhiteSpace(c)) continue;
                char u = char.ToUpperInvariant(c);
                sb.Append(u == 'O' ? '0' : u);
            }
            return sb.ToString();
        }
    }
}
