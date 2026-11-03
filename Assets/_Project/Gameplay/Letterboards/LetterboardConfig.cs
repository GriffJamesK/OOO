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

        [Header("Board build")]
        [Tooltip("Glyph letter height as a fraction of SlotSize.y.")]
        public float GlyphScale = 0.75f;

        [Tooltip("How far a glyph sits in front of the felt (m).")]
        public float GlyphProud = 0.004f;

        [Tooltip("Felt border around the slot grid (m).")]
        public float FeltMargin = 0.04f;

        [Tooltip("Felt backing thickness (m).")]
        public float FeltDepth = 0.02f;

        [Tooltip("Depth of each slot's trigger collider, proud of the felt (m).")]
        public float SlotDepth = 0.03f;

        [Header("Tiles in hand (view model)")]
        [Tooltip("Camera-local position of the newest carried tile (right, down, ahead; m).")]
        public Vector3 ViewOffset = new Vector3(0.17f, -0.14f, 0.3f);

        [Tooltip("Carried tile plate size (width, height; m).")]
        public Vector2 ViewTileSize = new Vector2(0.032f, 0.04f);

        [Tooltip("Spacing between carried tiles along the row (m).")]
        public float ViewTileSpacing = 0.04f;

        [Tooltip("Yaw of the carried tiles toward the view centre (deg).")]
        public float ViewTileYaw = -12f;

        [Tooltip("How far carried tiles lag behind quick turns (m per rad/s of view turn). 0 = no sway.")]
        public float ViewSway = 0.004f;

        protected override void Sanitize()
        {
            MaxCarried = Mathf.Max(1, MaxCarried);
            Reach = Mathf.Max(0.5f, Reach);
            TileGap = Mathf.Max(0f, TileGap);
            SlotSize = new Vector2(Mathf.Max(0.01f, SlotSize.x), Mathf.Max(0.01f, SlotSize.y));
            GlyphScale = Mathf.Clamp(GlyphScale, 0.1f, 1.5f);
            FeltMargin = Mathf.Max(0f, FeltMargin);
            FeltDepth = Mathf.Max(0.001f, FeltDepth);
            SlotDepth = Mathf.Max(0.005f, SlotDepth);
            ViewTileSize = new Vector2(Mathf.Max(0.005f, ViewTileSize.x), Mathf.Max(0.005f, ViewTileSize.y));
            ViewTileSpacing = Mathf.Max(0f, ViewTileSpacing);
            ViewSway = Mathf.Max(0f, ViewSway);
        }
    }
}
