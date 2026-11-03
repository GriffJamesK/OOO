using System;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// Anything that gives a threshold its number: a fixed BrassPlate or a Letterboard whose tiles
    /// the player can move. Raw is what is shown (letters, digits, '_' or ' ' for empty slots);
    /// pairing always goes through ThresholdNumber.Normalize, never Raw.
    /// </summary>
    public interface INumberSource
    {
        /// <summary>The characters currently shown on the plate.</summary>
        string Raw { get; }

        /// <summary>Raised whenever Raw changes, so the registry re-pairs.</summary>
        event Action Changed;
    }
}
