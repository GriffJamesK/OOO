using System;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// A fixed, screwed-on number plate (brass). It never changes at runtime, so its Changed event
    /// never fires; the number is shown with WorldText in the brass style. Built by OfficeKit.Threshold.
    /// </summary>
    public sealed class BrassPlate : MonoBehaviour, INumberSource
    {
        [SerializeField] string number = "";

        public string Raw => number;

        // A brass plate never changes; the event exists only to satisfy INumberSource.
        public event Action Changed { add { } remove { } }

        /// <summary>
        /// Builder entry point (edit mode): sets the number and builds the plate visual (a small
        /// brass-coloured backing with the number in WorldText, Brass style) facing this transform's forward.
        /// </summary>
        public void Setup(string plateNumber, float letterHeight)
        {
            number = plateNumber ?? "";
            // STUB: the Thresholds worker builds the plate visual here.
        }
    }
}
