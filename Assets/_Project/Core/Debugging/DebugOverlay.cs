using System.Collections.Generic;
using System.Text;
using Kit.Core.FSM;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Kit.Core.Debugging
{
    /// <summary>
    /// On-screen readout toggled with the backquote key: FPS, timeScale, every live state
    /// machine, and any key/value a system chooses to Log. Quicker than the console for values
    /// that change every frame, and visible in screenshots taken through MCP.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class DebugOverlay : MonoBehaviour
    {
        static readonly Dictionary<string, string> values = new Dictionary<string, string>();
        static bool visible;

        readonly StringBuilder text = new StringBuilder(256);
        float smoothedDelta = 1f / 60f;
        GUIStyle style;

        /// <summary>Whether the overlay is drawn. Also settable from code or MCP.</summary>
        public static bool Visible { get => visible; set => visible = value; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            values.Clear();
            visible = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install() => KitHost.Ensure<DebugOverlay>();

        /// <summary>Shows <paramref name="value"/> under <paramref name="key"/>; call each frame or on change.</summary>
        public static void Log(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;
            values[key] = value == null ? "null" : value.ToString();
        }

        /// <summary>Reads back a logged value. Used by tests and Debug.Report hooks.</summary>
        public static bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value);

        void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.1f);
            var kb = Keyboard.current;
            if (kb != null && kb.backquoteKey.wasPressedThisFrame) visible = !visible;
        }

        void OnGUI()
        {
            if (!visible) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, richText = false };
                style.normal.textColor = Color.white;
            }

            text.Clear();
            text.Append("FPS ").Append(Mathf.RoundToInt(1f / Mathf.Max(0.0001f, smoothedDelta)))
                .Append("   timeScale ").Append(Time.timeScale.ToString("0.00")).Append('\n');
            foreach (var m in StateMachineRegistry.All)
                text.Append("FSM ").Append(m.Name).Append(": ").Append(m.CurrentName).Append('\n');
            foreach (var kv in values)
                text.Append(kv.Key).Append(": ").Append(kv.Value).Append('\n');

            var content = new GUIContent(text.ToString());
            var size = style.CalcSize(content);
            GUI.Box(new Rect(10, 10, size.x + 10, size.y + 6), content, style);
        }
    }
}
