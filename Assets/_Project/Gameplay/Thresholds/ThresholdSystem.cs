using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// Self-installing: on every PlayerSpawned, makes sure the player has a ThresholdTraveller.
    /// Nothing is placed in scenes for it (kit rule: plumbing installs itself).
    /// </summary>
    public static class ThresholdSystem
    {
        /// <summary>
        /// Domain reload is off: drop last session's subscription. EventBus clears itself too, but
        /// the order of SubsystemRegistration calls isn't defined, so this doesn't rely on it.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
        }

        /// <summary>
        /// Listens for players. Unsubscribes first so a second call in one session can't double up,
        /// and also equips a player that already exists (one placed in a scene, spawned before this ran).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
            var existing = Object.FindAnyObjectByType<PlayerController>();
            if (existing != null) Equip(existing.transform);
        }

        static void OnPlayerSpawned(PlayerSpawned e)
        {
            if (e.Root != null) Equip(e.Root);
        }

        /// <summary>Adds a ThresholdTraveller to the player root if it has none, then binds it.</summary>
        static void Equip(Transform root)
        {
            var player = root.GetComponent<PlayerController>();
            if (player == null) player = root.GetComponentInChildren<PlayerController>(true);
            if (player == null) return;
            var traveller = root.GetComponent<ThresholdTraveller>();
            if (traveller == null) traveller = root.gameObject.AddComponent<ThresholdTraveller>();
            traveller.Bind(player);
        }
    }
}
