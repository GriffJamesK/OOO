using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Letterboards
{
    /// <summary>Self-installing: gives every spawned player a TileCarrier (kit rule: plumbing installs itself).</summary>
    public static class LetterboardSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // EventBus clears itself at this point too; unsubscribing anyway keeps this safe if
            // that ever changes, since domain reload is off and the handler is a static method.
            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // Unsubscribe first so a second Install in one session can never double-subscribe.
            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
            EventBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
        }

        static void OnPlayerSpawned(PlayerSpawned e)
        {
            if (e.Root == null) return;
            if (e.Root.GetComponent<TileCarrier>() == null) e.Root.gameObject.AddComponent<TileCarrier>();
        }
    }
}
