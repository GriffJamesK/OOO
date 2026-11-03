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
            ShowOnly(mainMenu, instant: true);
            if (scoreLabel) scoreLabel.text = "Score 0";
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<GameStarted>(OnGameStarted);
            EventBus.Unsubscribe<GamePaused>(OnGamePaused);
            EventBus.Unsubscribe<GameOver>(OnGameOver);
            EventBus.Unsubscribe<ScoreChanged>(OnScoreChanged);
        }

        void OnGameStarted(GameStarted _)
        {
            ShowOnly(hud);
            if (scoreLabel) scoreLabel.text = "Score 0";
        }

        void OnGamePaused(GamePaused e) => ShowOnly(e.Paused ? pauseMenu : hud);

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
