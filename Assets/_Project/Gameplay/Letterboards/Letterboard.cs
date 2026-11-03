using System;
using Kit.Core.Events;
using Kit.Gameplay.Signage;
using Kit.Gameplay.Thresholds;
using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>
    /// A felt letterboard with Rows x Columns slots, each holding one glyph or nothing (' ').
    /// Used two ways: as a threshold's number plate (one row; Raw = its glyphs, so moving tiles
    /// rewires the building) and as a sign (the lobby directory, the cafeteria menu), which is a
    /// supply of tiles. Each slot has a trigger collider (LetterSlot) for the interaction raycast.
    /// The board faces its transform's forward; slots are laid out left to right, top to bottom,
    /// centred on the transform. The glyph string and the per-slot references are serialized, so a
    /// board built by an editor builder survives scene save and reads the same in play mode.
    /// </summary>
    public sealed class Letterboard : MonoBehaviour, INumberSource
    {
        const string FeltName = "Felt";
        const string SlotPrefix = "Slot_";
        const string GlyphName = "Glyph";

        [SerializeField] int rows = 1;
        [SerializeField] int columns = 3;
        [Tooltip("Row-major slot contents; ' ' = empty.")]
        [SerializeField] string glyphs = "   ";
        [Tooltip("Per-slot collider objects, the parents of each glyph. Built by Setup.")]
        [SerializeField] LetterSlot[] slots = Array.Empty<LetterSlot>();
        [Tooltip("Per-slot glyph text (null until a tile first sits in that slot). Built by Setup.")]
        [SerializeField] WorldText[] slotTexts = Array.Empty<WorldText>();

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
        /// Builder entry point (edit mode; tests call it in play mode too). <paramref name="content"/>
        /// lists rows separated by '\n'; '_' marks an empty slot; short rows are padded with empty
        /// slots and extra rows or columns are dropped. Rebuilds the felt backing, tile visuals and
        /// slot colliders from scratch, so re-running a builder never stacks duplicates.
        /// </summary>
        public void Setup(int rowCount, int columnCount, string content)
        {
            rows = Mathf.Max(1, rowCount);
            columns = Mathf.Max(1, columnCount);
            glyphs = Parse(rows, columns, content);
            BuildVisuals();
            Changed?.Invoke();
        }

        /// <summary>Takes the tile out of a slot. False when the slot is empty or out of range.</summary>
        public bool TryPull(int slot, out char glyph)
        {
            glyph = GlyphAt(slot);
            if (slot < 0 || slot >= SlotCount || glyph == ' ') { glyph = ' '; return false; }

            SetGlyph(slot, ' ');
            Changed?.Invoke();
            if (Application.isPlaying) EventBus.Publish(new TilePulled(this, slot, glyph));
            return true;
        }

        /// <summary>Presses a tile into an empty slot. False when the slot is full or out of range.</summary>
        public bool TryPlace(int slot, char glyph)
        {
            if (slot < 0 || slot >= SlotCount || GlyphAt(slot) != ' ' || glyph == ' ' || glyph == '_') return false;

            SetGlyph(slot, glyph);
            Changed?.Invoke();
            if (Application.isPlaying) EventBus.Publish(new TilePlaced(this, slot, glyph));
            return true;
        }

        /// <summary>Local centre of a slot on the board's face (z = 0 is the felt's front surface).</summary>
        public Vector3 SlotLocalCentre(int slot)
        {
            var cfg = ConfigLookup.Get<LetterboardConfig>();
            Vector2 size = cfg.SlotSize;
            Vector2 extent = GridExtent(cfg);
            int r = slot / columns, c = slot % columns;
            float x = -extent.x * 0.5f + size.x * 0.5f + c * (size.x + cfg.TileGap);
            float y = extent.y * 0.5f - size.y * 0.5f - r * (size.y + cfg.TileGap);
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Turns builder content into the fixed-length row-major glyph string. Pure, so the
        /// padding rules ('_' empty, short rows padded) live in one place.
        /// </summary>
        static string Parse(int rowCount, int columnCount, string content)
        {
            var result = new char[rowCount * columnCount];
            for (int i = 0; i < result.Length; i++) result[i] = ' ';
            if (string.IsNullOrEmpty(content)) return new string(result);

            string[] lines = content.Replace("\r", "").Split('\n');
            for (int r = 0; r < rowCount && r < lines.Length; r++)
            {
                string line = lines[r];
                for (int c = 0; c < columnCount && c < line.Length; c++)
                {
                    char ch = line[c];
                    result[r * columnCount + c] = ch == '_' ? ' ' : ch;
                }
            }
            return new string(result);
        }

        /// <summary>Width and height of the slot grid (no margin).</summary>
        Vector2 GridExtent(LetterboardConfig cfg)
        {
            return new Vector2(columns * cfg.SlotSize.x + (columns - 1) * cfg.TileGap,
                               rows * cfg.SlotSize.y + (rows - 1) * cfg.TileGap);
        }

        static float GlyphHeight(LetterboardConfig cfg) => cfg.SlotSize.y * cfg.GlyphScale;

        /// <summary>
        /// Writes one slot's glyph and updates only that slot's text, so a pull or place in play
        /// mode never rebuilds the whole board (and never touches the other slots' colliders).
        /// </summary>
        void SetGlyph(int slot, char glyph)
        {
            var chars = glyphs.ToCharArray();
            chars[slot] = glyph;
            glyphs = new string(chars);

            if (slotTexts == null || slotTexts.Length != SlotCount) Array.Resize(ref slotTexts, SlotCount);
            var text = slotTexts[slot];
            if (glyph == ' ')
            {
                if (text != null) text.gameObject.SetActive(false);
                return;
            }

            var cfg = ConfigLookup.Get<LetterboardConfig>();
            if (text == null)
            {
                bool hasSlot = slots != null && slot < slots.Length && slots[slot] != null;
                var parent = hasSlot ? slots[slot].transform : transform;
                slotTexts[slot] = CreateGlyph(parent, hasSlot ? Vector3.zero : SlotLocalCentre(slot), glyph, cfg);
            }
            else
            {
                text.Set(glyph.ToString(), GlyphHeight(cfg), WorldTextStyle.LightOnDark);
                text.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// A white tile glyph slightly proud of the felt. Yawed 180 degrees because a WorldText
        /// reads from its -Z side and the board's front is +Z.
        /// </summary>
        WorldText CreateGlyph(Transform parent, Vector3 localCentre, char glyph, LetterboardConfig cfg)
        {
            var pos = localCentre + new Vector3(0f, 0f, cfg.GlyphProud);
            var wt = WorldText.Create(parent, GlyphName, glyph.ToString(), pos, Quaternion.Euler(0f, 180f, 0f),
                GlyphHeight(cfg), WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter);
            wt.gameObject.layer = gameObject.layer;
            return wt;
        }

        /// <summary>
        /// Rebuilds the felt backing and one Slot_i (trigger collider + LetterSlot + glyph) per slot.
        /// Old children are removed first: immediately in edit mode, and detached then destroyed in
        /// play mode so lookups this frame (GetComponentsInChildren) already see only the new ones.
        /// </summary>
        void BuildVisuals()
        {
            ClearChildren();
            var cfg = ConfigLookup.Get<LetterboardConfig>();
            Vector2 extent = GridExtent(cfg);

            // Felt backing: thin, behind the slots (front face at z = 0). No collider: the wall the
            // board hangs on blocks the player, and a collider here could only steal raycasts.
            var felt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            felt.name = FeltName;
            DestroyImmediate(felt.GetComponent<Collider>());
            felt.layer = gameObject.layer;
            felt.transform.SetParent(transform, false);
            felt.transform.localPosition = new Vector3(0f, 0f, -cfg.FeltDepth * 0.5f);
            felt.transform.localScale = new Vector3(extent.x + 2f * cfg.FeltMargin, extent.y + 2f * cfg.FeltMargin, cfg.FeltDepth);
            if (cfg.FeltMaterial != null) felt.GetComponent<Renderer>().sharedMaterial = cfg.FeltMaterial;

            int count = SlotCount;
            slots = new LetterSlot[count];
            slotTexts = new WorldText[count];
            for (int i = 0; i < count; i++)
            {
                var slotGo = new GameObject(SlotPrefix + i);
                slotGo.layer = gameObject.layer;
                slotGo.transform.SetParent(transform, false);
                slotGo.transform.localPosition = SlotLocalCentre(i);

                // Trigger box from the felt's face forward, so the view ray meets it before the felt.
                var box = slotGo.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(cfg.SlotSize.x, cfg.SlotSize.y, cfg.SlotDepth);
                box.center = new Vector3(0f, 0f, cfg.SlotDepth * 0.5f);

                var slot = slotGo.AddComponent<LetterSlot>();
                slot.Setup(this, i);
                slots[i] = slot;

                char g = glyphs[i];
                if (g != ' ') slotTexts[i] = CreateGlyph(slotGo.transform, Vector3.zero, g, cfg);
            }
        }

        /// <summary>Removes every generated child (felt, slots and anything a previous Setup left).</summary>
        void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.transform.SetParent(null, false);
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}
