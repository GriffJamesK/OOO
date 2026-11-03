using Kit.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Kit.Editor
{
    public static partial class KitBuilder
    {
        static Font uiFont;
        static Sprite uiSprite;

        /// <summary>
        /// Overlay canvas at 1920x1080 reference with MainMenu, PauseMenu, GameOver and HUD panels,
        /// plus its own EventSystem so a scene holding the prefab gets working UI input with no setup.
        /// </summary>
        static void BuildUICanvas()
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var root = new GameObject("UI_Canvas", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var dim = new Color(0.04f, 0.05f, 0.08f, 0.72f);

            var main = Panel(root.transform, "MainMenu", dim);
            Label(main, "Title", "OOO", 160, new Vector2(0f, 200f), new Vector2(1600f, 220f));
            var play = UIButton(main, "PlayButton", "Play", new Vector2(0f, -40f));
            var quitMain = UIButton(main, "QuitButton", "Quit", new Vector2(0f, -150f));

            var pause = Panel(root.transform, "PauseMenu", dim);
            Label(pause, "Title", "PAUSED", 96, new Vector2(0f, 220f), new Vector2(1200f, 140f));
            var resume = UIButton(pause, "ResumeButton", "Resume", new Vector2(0f, 40f));
            var restartPause = UIButton(pause, "RestartButton", "Restart", new Vector2(0f, -70f));
            var quitPause = UIButton(pause, "QuitButton", "Quit", new Vector2(0f, -180f));

            var over = Panel(root.transform, "GameOver", dim);
            var overLabel = Label(over, "Title", "GAME OVER", 96, new Vector2(0f, 180f), new Vector2(1200f, 140f));
            var restartOver = UIButton(over, "RestartButton", "Restart", new Vector2(0f, -20f));
            var quitOver = UIButton(over, "QuitButton", "Quit", new Vector2(0f, -130f));

            var hud = Panel(root.transform, "HUD", Color.clear);
            var score = Label(hud, "Score", "Score 0", 40, Vector2.zero, new Vector2(500f, 60f));
            Anchor(score.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -40f));
            score.alignment = TextAnchor.UpperLeft;
            var hint = Label(hud, "Hint", "Esc / Start: pause    ` : debug overlay", 24, Vector2.zero, new Vector2(900f, 40f));
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 30f));

            // The ending card: no buttons, never blocks input; UIRouter fades it on GameCompleted.
            var ending = Panel(root.transform, "Ending", Color.clear);
            Label(ending, "Title", "OOO", 200, new Vector2(0f, 40f), new Vector2(1600f, 260f));

            var es = new GameObject("EventSystem");
            es.transform.SetParent(root.transform, false);
            es.AddComponent<EventSystem>().firstSelectedGameObject = play.gameObject;
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var router = root.AddComponent<UIRouter>();
            Set(router, "mainMenu", main);
            Set(router, "pauseMenu", pause);
            Set(router, "gameOver", over);
            Set(router, "hud", hud);
            Set(router, "ending", ending);
            Set(router, "gameOverLabel", overLabel);
            Set(router, "scoreLabel", score);
            Set(router, "playButton", play);
            Set(router, "resumeButton", resume);
            SetArray(router, "restartButtons", restartPause, restartOver);
            SetArray(router, "quitButtons", quitMain, quitPause, quitOver);

            // Only the main menu is visible in the saved prefab; UIRouter takes over at runtime.
            foreach (var g in new[] { pause, over, hud, ending }) g.alpha = 0f;

            SavePrefab(root, PrefabDir + "/UI/UI_Canvas.prefab");
        }

        static CanvasGroup Panel(Transform parent, string name, Color background)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (background.a > 0f)
            {
                var img = go.AddComponent<Image>();
                img.color = background;
            }
            return go.AddComponent<CanvasGroup>();
        }

        static Text Label(Component parent, string name, string text, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;
            var t = go.AddComponent<Text>();
            t.font = uiFont;
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return t;
        }

        static Button UIButton(Component parent, string name, string text, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(420f, 90f);
            var img = go.AddComponent<Image>();
            img.sprite = uiSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.16f, 0.18f, 0.24f, 1f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.55f, 0.85f, 1f);
            colors.selectedColor = new Color(0.55f, 0.85f, 1f);
            colors.pressedColor = new Color(0.35f, 0.65f, 0.9f);
            button.colors = colors;
            var label = Label(go.GetComponent<RectTransform>(), "Label", text, 40, Vector2.zero, rt.sizeDelta);
            label.color = Color.white;
            return button;
        }

        static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = offset;
        }
    }
}
