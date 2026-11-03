namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// The building's reading of a plate. Uppercase, empty slots ('_' and spaces) dropped, and the
    /// letter O read as the digit 0, because the building can't tell them apart (the K4 secret:
    /// letters from "OUT OF OFFICE" make 000). Pure, so edit-mode tests cover it.
    /// </summary>
    public static class ThresholdNumber
    {
        /// <summary>Normalised number for pairing; "" means no number (never pairs).</summary>
        public static string Normalize(string raw)
        {
            // STUB: implemented by the Thresholds worker. See ThresholdLogicTests.
            return raw ?? "";
        }
    }
}
