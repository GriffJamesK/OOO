using System.Collections.Generic;
using Kit.Core;
using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// Self-installing: gives every spawned player a SafeGroundTracker, answers PlayerFellOut and
    /// FallZones by putting the player back on the last safe ground (no fail state), and runs the
    /// ending (PlayerCrossedThreshold into an Exterior threshold -> wait -> GameCompleted).
    /// <para>
    /// A respawn places the player immediately and cuts the screen to black in the same frame, then
    /// holds black for FadeOut and fades in over FadeIn, and only then publishes PlayerRespawned.
    /// Placing first (rather than fading out while the player keeps falling) means a fall never
    /// hitches or lets the player drop further, the camera snap is always hidden behind black, and
    /// GameManager's "back at the spawn within a frame" contract (SandboxTests) holds.
    /// </para>
    /// </summary>
    public static class FlowSystem
    {
        static readonly List<FallZone> fallZones = new List<FallZone>();
        static readonly List<NoSafeGroundZone> noSafeZones = new List<NoSafeGroundZone>();

        static bool installed;
        static PlayerController player;
        static SafeGroundTracker tracker;
        static Pose spawnFallback;

        static bool respawning;
        static bool fadingIn;
        static float respawnClock;
        static int placedFrame = -1;

        static bool endingFired;
        static TimerHandle endingTimer;

        /// <summary>The player FlowSystem currently looks after (the last one spawned).</summary>
        public static PlayerController Player => player;

        /// <summary>The tracked player's SafeGroundTracker, or null.</summary>
        public static SafeGroundTracker Tracker => tracker;

        /// <summary>True from a respawn until its fade-in finishes. Safe ground is not recorded meanwhile.</summary>
        public static bool IsRespawning => respawning;

        /// <summary>True once the ending was triggered this run (GameCompleted fires after EndingTitleDelay).</summary>
        public static bool EndingFired => endingFired;

        /// <summary>Domain reload is off: forget the last session's player, zones and timers.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            fallZones.Clear();
            noSafeZones.Clear();
            installed = false;
            player = null;
            tracker = null;
            spawnFallback = default;
            respawning = false;
            fadingIn = false;
            respawnClock = 0f;
            placedFrame = -1;
            endingFired = false;
            endingTimer = null;
        }

        /// <summary>
        /// Subscribes once per play session. Unsubscribing first makes a second call harmless even
        /// if the bus was not cleared (EventBus.Clear normally runs at SubsystemRegistration too).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (installed) return;
            installed = true;

            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Unsubscribe<PlayerFellOut>(OnPlayerFellOut);
            EventBus.Unsubscribe<PlayerCrossedThreshold>(OnPlayerCrossedThreshold);
            EventBus.Unsubscribe<GameStarted>(OnGameStarted);

            EventBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Subscribe<PlayerFellOut>(OnPlayerFellOut);
            EventBus.Subscribe<PlayerCrossedThreshold>(OnPlayerCrossedThreshold);
            EventBus.Subscribe<GameStarted>(OnGameStarted);

            KitHost.Ensure<FlowRunner>();
        }

        internal static void Register(FallZone zone) { if (!fallZones.Contains(zone)) fallZones.Add(zone); }
        internal static void Unregister(FallZone zone) => fallZones.Remove(zone);
        internal static void Register(NoSafeGroundZone zone) { if (!noSafeZones.Contains(zone)) noSafeZones.Add(zone); }
        internal static void Unregister(NoSafeGroundZone zone) => noSafeZones.Remove(zone);

        /// <summary>True if the player's feet or capsule centre is inside an enabled NoSafeGroundZone.</summary>
        public static bool IsInNoSafeGround(PlayerController p)
        {
            if (p == null) return false;
            Vector3 feet = p.transform.position;
            Vector3 centre = CapsuleCentre(p);
            for (int i = 0; i < noSafeZones.Count; i++)
            {
                var z = noSafeZones[i];
                if (z == null) continue;
                var b = z.Bounds;
                if (b.Contains(feet) || b.Contains(centre)) return true;
            }
            return false;
        }

        /// <summary>Called every frame by FlowRunner: fall zones and the respawn fade sequence.</summary>
        internal static void Tick()
        {
            CheckFallZones();
            TickRespawn();
        }

        static Vector3 CapsuleCentre(PlayerController p)
        {
            var cc = p.GetComponent<CharacterController>();
            return cc != null ? p.transform.TransformPoint(cc.center) : p.transform.position + Vector3.up * 0.9f;
        }

        static void CheckFallZones()
        {
            if (player == null || fallZones.Count == 0) return;
            Vector3 centre = CapsuleCentre(player);
            for (int i = 0; i < fallZones.Count; i++)
            {
                var z = fallZones[i];
                if (z != null && z.Bounds.Contains(centre)) { Respawn(); return; }
            }
        }

        /// <summary>Holds black for FadeOut, fades in over FadeIn (unscaled), then announces the respawn.</summary>
        static void TickRespawn()
        {
            if (!respawning) return;
            var cfg = ConfigLookup.Get<FlowConfig>();
            respawnClock += Time.unscaledDeltaTime;
            if (!fadingIn && respawnClock >= cfg.FadeOut)
            {
                fadingIn = true;
                ScreenFade.FadeTo(0f, cfg.FadeIn);
            }
            if (fadingIn && respawnClock >= cfg.FadeOut + cfg.FadeIn)
            {
                respawning = false;
                fadingIn = false;
                if (player != null) EventBus.Publish(new PlayerRespawned(player.transform.position));
            }
        }

        /// <summary>
        /// Puts the tracked player on the last safe ground (or the spawn when none is known yet)
        /// right now, and starts the black cover. A second trigger in the same frame (a FallZone
        /// and PlayerFellOut together) is ignored. A trigger during a running fade simply places
        /// again and restarts the cover, so a fall is never left unanswered.
        /// </summary>
        public static void Respawn()
        {
            if (player == null) return;
            if (placedFrame == Time.frameCount) return;
            placedFrame = Time.frameCount;

            var gm = Object.FindAnyObjectByType<GameManager>();
            bool ownedByGm = gm != null && gm.Player == player;
            Pose pose;
            if (tracker != null && tracker.HasSafeGround) pose = tracker.LastSafePose;
            else pose = ownedByGm ? gm.SpawnPose : spawnFallback;

            // GameManager clears momentum and republishes PlayerSpawned so the camera snaps; isolated
            // tests (no GameManager) move the controller directly.
            if (ownedByGm) gm.RespawnPlayer(pose);
            else player.Respawn(pose);
            if (tracker != null) tracker.ResetDwell();

            respawning = true;
            fadingIn = false;
            respawnClock = 0f;
            ScreenFade.FadeTo(1f, 0f);
        }

        static void OnPlayerSpawned(PlayerSpawned e)
        {
            var pc = e.Root != null ? e.Root.GetComponent<PlayerController>() : null;
            if (pc == null) return;

            // GameManager.RespawnPlayer republishes PlayerSpawned for the same player; only a new
            // instance (first spawn, restart, a test's player) is a new run.
            if (pc != player)
            {
                player = pc;
                spawnFallback = new Pose(pc.transform.position, Quaternion.Euler(0f, pc.transform.eulerAngles.y, 0f));
                ResetEnding();
            }

            tracker = pc.GetComponent<SafeGroundTracker>();
            if (tracker == null) tracker = pc.gameObject.AddComponent<SafeGroundTracker>();
        }

        static void OnPlayerFellOut(PlayerFellOut _) => Respawn();

        /// <summary>Arriving through an exterior threshold ends the game once per run, after a beat.</summary>
        static void OnPlayerCrossedThreshold(PlayerCrossedThreshold e)
        {
            if (endingFired || e.To == null || !e.To.Exterior) return;
            endingFired = true;
            endingTimer = Timer.After(ConfigLookup.Get<FlowConfig>().EndingTitleDelay, () =>
            {
                endingTimer = null;
                EventBus.Publish(new GameCompleted());
            });
        }

        static void OnGameStarted(GameStarted _) => ResetEnding();

        static void ResetEnding()
        {
            endingFired = false;
            endingTimer?.Cancel();
            endingTimer = null;
        }
    }
}
