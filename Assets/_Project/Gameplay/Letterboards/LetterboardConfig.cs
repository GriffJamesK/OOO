using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>Tunables for letterboards and carrying tiles. Asset: Config/LetterboardConfig.asset.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Letterboard Config", fileName = "LetterboardConfig")]
    public sealed class LetterboardConfig : ConfigAsset
    {
        [Tooltip("Tiles the player can hold at once.")]
        public int MaxCarried = 3;

        [Tooltip("Interaction reach from the eye (m).")]
        public float Reach = 2.6f;

        [Tooltip("Slot width and height on the board (m).")]
        public Vector2 SlotSize = new Vector2(0.16f, 0.2f);

        [Tooltip("Gap between slots (m).")]
        public float TileGap = 0.02f;

        [Tooltip("Felt backing material (black felt). Assigned by the Letterboard builder.")]
        public Material FeltMaterial;

        [Tooltip("Tile body material (white plastic). Assigned by the Letterboard builder.")]
        public Material TileMaterial;

        protected override void Sanitize()
        {
            MaxCarried = Mathf.Max(1, MaxCarried);
            Reach = Mathf.Max(0.5f, Reach);
            TileGap = Mathf.Max(0f, TileGap);
        }
    }
}
