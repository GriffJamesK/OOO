using UnityEngine;

namespace Kit.Gameplay.Signage
{
    /// <summary>How a piece of diegetic text is drawn; each maps to a material in WorldTextConfig.</summary>
    public enum WorldTextStyle
    {
        DarkOnLight,
        LightOnDark,
        Brass,
        Glow
    }

    /// <summary>
    /// Diegetic text in the world: signage, nameplates, memos, whiteboards, plate numbers. A
    /// TextMesh with a fog- and light-aware material, so words sit in the room instead of glowing
    /// through the fog like UI. Works in edit mode, so builders create and set it directly.
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldText : MonoBehaviour
    {
        [SerializeField, TextArea] string text = "";
        [Tooltip("Height of a capital letter in metres.")]
        [SerializeField] float letterHeight = 0.1f;
        [SerializeField] WorldTextStyle style = WorldTextStyle.DarkOnLight;
        [SerializeField] TextAnchor anchor = TextAnchor.MiddleCenter;

        public string Text => text;
        public float LetterHeight => letterHeight;
        public WorldTextStyle Style => style;
        public TextAnchor Anchor => anchor;

        /// <summary>Sets content and look; rebuilds the TextMesh and material.</summary>
        public void Set(string content, float height, WorldTextStyle textStyle, TextAnchor textAnchor = TextAnchor.MiddleCenter)
        {
            text = content ?? "";
            letterHeight = Mathf.Max(0.005f, height);
            style = textStyle;
            anchor = textAnchor;
            // STUB: the Signage worker applies this to a TextMesh with the style's material.
        }

        /// <summary>Creates a child text object (edit or play mode).</summary>
        public static WorldText Create(Transform parent, string name, string content, Vector3 localPosition, Quaternion localRotation,
            float height, WorldTextStyle textStyle, TextAnchor textAnchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            var wt = go.AddComponent<WorldText>();
            wt.Set(content, height, textStyle, textAnchor);
            return wt;
        }
    }
}
