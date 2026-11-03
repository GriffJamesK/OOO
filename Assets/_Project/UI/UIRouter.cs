using Kit.Core.Events;
using Kit.Core.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Kit.UI
{
    /// <summary>
    /// Shows the right panel for the current game state and turns button clicks into request
    /// events. It knows nothing about Gameplay: it listens to GameEvents and publishes
    /// PlayRequested / RestartRequested / PauseToggleRequested / QuitRequested.
    /// </summary>
    public sealed class UIRouter : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] CanvasGroup mainMenu;
        [SerializeField] CanvasGroup pauseMenu;
        [SerializeField] CanvasGroup gameOver;
        [SerializeField] CanvasGroup hud;
        [Tooltip("Ending card shown on GameCompleted (fades in, holds, fades out; never blocks input).")]
        [SerializeField] CanvasGroup ending;

        [Tooltip("OOO has no HUD: off keeps the HUD panel hidden for the whole game.")]
        [SerializeField] bool showHud;

        [Header("Widgets")]
        [SerializeField] Text gameOverLabel;
        [SerializeField] Text scoreLabel;

        [Header("Buttons")]
        [SerializeField] Button playButton;
        [SerializeField] Button resumeButton;
        [SerializeField] Button[] restartButtons;
        [SerializeField] Button[] quitButtons;

        [Tooltip("Panel fade time, seconds (unscaled, so it works while paused).")]
        [SerializeField] float fadeDuration = 0.2f;

        readonly System.Collections.Generic.Dictionary<CanvasGroup, TweenHandle> fades =
            new System.Collections.Generic.Dictionary<CanvasGroup, TweenHandle>();

        void Awake()
        {
            if (playButton) playButton.onClick.AddListener(() => EventBus.Publish(new PlayRequested()));
            if (resumeButton) resumeButton.onClick.AddListener(() => EventBus.Publish(new PauseToggleRequested()));
            foreach (var b in restartButtons) if (b) b.onClick.AddListener(() => EventBus.Publish(new RestartRequested()));
            foreach (var b in quitButtons) if (b) b.onClick.AddListener(() => EventBus.Publish(new QuitRequested()));
        }

        void OnEnable()
        {
            EventBus.Subscribe<GameStarted>(OnGameStarted);
            EventBus.Subscribe<GamePaused>(OnGamePaused);
            EventBus.Subscribe<GameOver>(OnGameOver);
            EventBus.Subscribe<ScoreChanged>(OnScoreChanged);
            EventBus.Subscribe<GameCompleted>(OnGameCompleted);
            ShowOnly(mainMenu, instant: true);
            if (ending) { ending.alpha = 0f; ending.interactable = false; ending.blocksRaycasts = false; }
            if (scoreLabel) scoreLabel.text = "Score 0";
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<GameStarted>(OnGameStarted);
            EventBus.Unsubscribe<GamePaused>(OnGamePaused);
            EventBus.Unsubscribe<GameOver>(OnGameOver);
            EventBus.Unsubscribe<ScoreChanged>(OnScoreChanged);
            EventBus.Unsubscribe<GameCompleted>(OnGameCompleted);
        }

        CanvasGroup PlayPanel => showHud ? hud : null;

        void OnGameStarted(GameStarted _)
        {
            ShowOnly(PlayPanel);
            if (ending) ending.alpha = 0f;
            if (scoreLabel) scoreLabel.text = "Score 0";
        }

        void OnGamePaused(GamePaused e) => ShowOnly(e.Paused ? pauseMenu : PlayPanel);

        /// <summary>
        /// The player walked out: the title fades in over the world (3 s), holds (6 s) and fades
        /// away, while play continues. Scaled time, so pausing holds the card where it is.
        /// </summary>
        void OnGameCompleted(GameCompleted _)
        {
            if (!ending) return;
            Tween.To(0f, 1f, 3f, Ease.OutQuad, a => { if (ending) ending.alpha = a; },
                () => Tween.To(1f, 1f, 6f, Ease.Linear, null,
                    () => Tween.To(1f, 0f, 3f, Ease.InOutSine, a => { if (ending) ending.alpha = a; })));
        }

        void OnGameOver(GameOver e)
        {
            if (gameOverLabel) gameOverLabel.text = e.Won ? "You Win" : "Game Over";
            ShowOnly(gameOver);
        }

        void OnScoreChanged(ScoreChanged e)
        {
            if (scoreLabel) scoreLabel.text = "Score " + e.Score;
        }

        void ShowOnly(CanvasGroup panel, bool instant = false)
        {
            foreach (var g in new[] { mainMenu, pauseMenu, gameOver, hud })
                if (g) SetVisible(g, g == panel, instant);
        }

        /// <summary>Fades a panel in or out; interaction is cut immediately so hidden buttons never take clicks.</summary>
        void SetVisible(CanvasGroup group, bool visible, bool instant)
        {
            group.interactable = visible;
            group.blocksRaycasts = visible;
            float target = visible ? 1f : 0f;
            // A quick toggle (pause/unpause) must cancel the previous fade, or two tweens fight.
            if (fades.TryGetValue(group, out var running)) { running.Cancel(); fades.Remove(group); }
            if (instant || !Application.isPlaying || Mathf.Approximately(group.alpha, target))
            {
                group.alpha = target;
                return;
            }
            fades[group] = Tween.To(group.alpha, target, fadeDuration, Ease.OutQuad,
                a => { if (group) group.alpha = a; }, () => fades.Remove(group), unscaledTime: true);
        }
    }
}
