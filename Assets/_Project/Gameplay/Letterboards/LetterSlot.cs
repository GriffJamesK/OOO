using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>
    /// One slot of a Letterboard: a trigger BoxCollider the TileCarrier's raycast hits, so the
    /// carrier knows which board and slot the player is looking at.
    /// </summary>
    public sealed class LetterSlot : MonoBehaviour
    {
        [SerializeField] Letterboard board;
        [SerializeField] int index;

        public Letterboard Board => board;
        public int Index => index;

        /// <summary>Builder entry point (edit mode).</summary>
        public void Setup(Letterboard owner, int slotIndex)
        {
            board = owner;
            index = slotIndex;
        }
    }
}
