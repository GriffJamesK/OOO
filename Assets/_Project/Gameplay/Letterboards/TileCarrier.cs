using System.Collections.Generic;
using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>
    /// Lives on the player (added by LetterboardSystem on PlayerSpawned). On Interact it raycasts
    /// from the main camera along its real forward (with pitch) against LetterSlot triggers within
    /// LetterboardConfig.Reach: a full slot is pulled into the hand, an empty slot receives the
    /// oldest carried tile. Carried tiles show as small plates at the lower right of the view
    /// (layer ViewModel), which is a hand, not a HUD.
    /// </summary>
    public sealed class TileCarrier : MonoBehaviour
    {
        readonly List<char> carried = new List<char>();

        /// <summary>Tiles in hand, oldest first.</summary>
        public IReadOnlyList<char> Carried => carried;

        /// <summary>Adds a tile directly (tests, debug). False when the hand is full.</summary>
        public bool Give(char glyph)
        {
            // STUB: implemented by the Letterboard worker (respect MaxCarried, refresh the view).
            return false;
        }

        /// <summary>Empties the hand (tests, debug, restart).</summary>
        public void Clear()
        {
            // STUB: implemented by the Letterboard worker (also refresh the view).
            carried.Clear();
        }

        /// <summary>
        /// Interacts with whatever slot the ray from <paramref name="origin"/> along
        /// <paramref name="direction"/> hits within reach. Returns true if a tile moved.
        /// Called on Interact with the main camera's ray; public so tests can aim exactly.
        /// </summary>
        public bool InteractAlong(Vector3 origin, Vector3 direction)
        {
            // STUB: implemented by the Letterboard worker.
            return false;
        }
    }
}
