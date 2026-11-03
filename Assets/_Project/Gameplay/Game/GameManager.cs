using System.Collections;
using Kit.Core.Events;
using Kit.Core.FSM;
using Kit.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kit.Gameplay
{
    /// <summary>Top-level game flow states, shown in the DebugOverlay as "FSM Game".</summary>
    public enum GameState
    {
        Loading,
        MainMenu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Owns the game flow: loads the play scene additively, spawns the player, and moves between
    /// menu, play, pause and game over. UI never calls it directly; it reacts to request events
    /// (PlayRequested, RestartRequested...) and announces results with GameEvents.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] GameConfig config;

        StateMachine<GameState> fsm;
        PlayerController player;
        Pose spawnPose;
        bool pendingWin;

        public GameConfig Config => config;
        public GameState State => fsm != null ? fsm.Current : GameState.Loading;
        public PlayerController Player => player;

        void Awake()
        {
            fsm = new StateMachine<GameState>("Game");
            fsm.Configure(GameState.Playing, enter: () => Time.timeScale = 1f)
               .Configure(GameState.Paused,
                    enter: () => { Time.timeScale = 0f; EventBus.Publish(new GamePaused(true)); },
                    exit: () => { Time.timeScale = 1f; EventBus.Publish(new GamePaused(false)); })
               .Configure(GameState.GameOver, enter: () => EventBus.Publish(new GameOver(pendingWin)));
        }

        void OnEnable()
        {
            EventBus.Subscribe<PlayRequested>(OnPlayRequested);
            EventBus.Subscribe<RestartRequested>(OnRestartRequested);
            EventBus.Subscribe<PauseToggleRequested>(OnPauseToggleRequested);
            EventBus.Subscribe<QuitRequested>(OnQuitRequested);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<PlayRequested>(OnPlayRequested);
            EventBus.Unsubscribe<RestartRequested>(OnRestartRequested);
            EventBus.Unsubscribe<PauseToggleRequested>(OnPauseToggleRequested);
            EventBus.Unsubscribe<QuitRequested>(OnQuitRequested);
        }

        void OnDestroy()
        {
            fsm?.Dispose();
            Time.timeScale = 1f;
        }

        IEnumerator Start()
        {
            fsm.Transition(GameState.Loading);
            yield return LoadPlayScene(reload: false);
            fsm.Transition(GameState.MainMenu);
        }

        void Update()
        {
            if (fsm == null) return; // scripts were recompiled during play; restart play mode
            if (KitInput.Actions.UI.Pause.WasPressedThisFrame()) TogglePause();
            if (player != null && player.transform.position.y < config.KillHeight) RespawnPlayer();
            fsm.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>Puts a fallen player back at the spawn point with momentum cleared, and tells the camera to snap.</summary>
        public void RespawnPlayer()
        {
            if (player == null) return;
            player.Respawn(spawnPose);
            EventBus.Publish(new PlayerSpawned(player.transform, player.CameraTarget));
        }

        /// <summary>Ends the run. Called by game rules (or GameDebug) when the player wins or loses.</summary>
        public void EndGame(bool won)
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            pendingWin = won;
            fsm.Transition(GameState.GameOver);
        }

        /// <summary>Reloads the play scene, respawns the player and starts playing.</summary>
        public void Restart()
        {
            if (State == GameState.Loading) return;
            StartCoroutine(RestartRoutine());
        }

        void OnPlayRequested(PlayRequested _)
        {
            if (State == GameState.MainMenu) BeginPlaying();
        }

        void OnRestartRequested(RestartRequested _) => Restart();
        void OnPauseToggleRequested(PauseToggleRequested _) => TogglePause();

        void OnQuitRequested(QuitRequested _)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }

        void TogglePause()
        {
            if (State == GameState.Playing) fsm.Transition(GameState.Paused);
            else if (State == GameState.Paused) fsm.Transition(GameState.Playing);
        }

        void BeginPlaying()
        {
            fsm.Transition(GameState.Playing);
            EventBus.Publish(new GameStarted());
        }

        IEnumerator RestartRoutine()
        {
            fsm.Transition(GameState.Loading);
            Time.timeScale = 1f;
            yield return LoadPlayScene(reload: true);
            BeginPlaying();
        }

        IEnumerator LoadPlayScene(bool reload)
        {
            if (config == null) { Debug.LogError("GameManager has no GameConfig assigned.", this); yield break; }

            var scene = SceneManager.GetSceneByName(config.PlayScene);
            if (reload && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
                scene = default;
            }

            if (!scene.isLoaded)
            {
                if (!Application.CanStreamedLevelBeLoaded(config.PlayScene))
                {
                    Debug.LogError($"Scene '{config.PlayScene}' is not in Build Settings.", this);
                    yield break;
                }
                yield return SceneManager.LoadSceneAsync(config.PlayScene, LoadSceneMode.Additive);
                scene = SceneManager.GetSceneByName(config.PlayScene);
            }

            // The play scene owns lighting, fog and skybox, so it must be the active scene.
            SceneManager.SetActiveScene(scene);
            SpawnPlayer(scene);
        }

        void SpawnPlayer(Scene scene)
        {
            if (config.PlayerPrefab == null) return;

            var pose = new Pose(Vector3.zero, Quaternion.identity);
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == config.SpawnPointName) { pose = new Pose(root.transform.position, root.transform.rotation); break; }

            // Instantiated into the active (play) scene, so unloading it on restart removes the player too.
            spawnPose = pose;
            player = Instantiate(config.PlayerPrefab, pose.position, pose.rotation);
            player.name = config.PlayerPrefab.name;
            EventBus.Publish(new PlayerSpawned(player.transform, player.CameraTarget));
        }
    }
}
