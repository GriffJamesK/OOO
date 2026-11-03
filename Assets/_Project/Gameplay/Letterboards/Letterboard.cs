using System;
using Kit.Gameplay.Thresholds;
using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>
    /// A felt letterboard with Rows x Columns slots, each holding one glyph or nothing (' ').
    /// Used two ways: as a threshold's number plate (one row; Raw = its glyphs, so moving tiles
    /// rewires the building) and as a sign (the lobby directory, the cafeteria menu), which is a
    /// supply of tiles. Each slot has a trigger collider (LetterSlot) for the interaction raycast.
    /// The board faces its transform's forward; slots are laid out left to right, top to bottom.
    /// </summary>
    public sealed class Letterboard : MonoBehaviour, INumberSource
    {
        [SerializeField] int rows = 1;
        [SerializeField] int columns = 3;
        [Tooltip("Row-major slot contents; ' ' = empty.")]
        [SerializeField] string glyphs = "   ";

        public int Rows => rows;
        public int Columns => columns;
        public int SlotCount => rows * columns;

        /// <summary>All slots in order, ' ' for empty (row-major, no separators).</summary>
        public string Raw => glyphs;

        /// <summary>Raised after any pull or place (and by Setup).</summary>
        public event Action Changed;

        /// <summary>Glyph in a slot, or ' ' when empty.</summary>
        public char GlyphAt(int slot) => slot >= 0 && slot < glyphs.Length ? glyphs[slot] : ' ';

        /// <summary>
        /// Builder entry point (edit mode). <paramref name="content"/> lists rows separated by '\n';
        /// '_' marks an empty slot; short rows are padded with empty slots. Builds the felt backing,
        /// tile visuals and slot colliders.
        /// </summary>
        public void Setup(int rowCount, int columnCount, string content)
        {
            rows = Mathf.Max(1, rowCount);
            columns = Mathf.Max(1, columnCount);
            // STUB: the Letterboard worker parses content, builds visuals and LetterSlot colliders.
            glyphs = new string(' ', rows * columns);
            Changed?.Invoke();
        }

        /// <summary>Takes the tile out of a slot. False when the slot is empty or out of range.</summary>
        public bool TryPull(int slot, out char glyph)
        {
            // STUB: implemented by the Letterboard worker.
            glyph = ' ';
            return false;
        }

        /// <summary>Presses a tile into an empty slot. False when the slot is full or out of range.</summary>
        public bool TryPlace(int slot, char glyph)
        {
            // STUB: implemented by the Letterboard worker.
            return false;
        }
    }
}
