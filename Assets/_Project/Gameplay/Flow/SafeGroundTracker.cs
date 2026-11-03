using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// Remembers the last place the player stood on solid ground, so a fall puts them back there
    /// instead of at the start (GDD F8: no fail state). A spot only counts after standing on it for
    /// SafeGroundDwell, which filters out a frame of contact on a ledge lip mid-jump. Added to the
    /// player by FlowSystem on PlayerSpawned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeGroundTracker : MonoBehaviour
    {
        PlayerController player;
        float dwell;

        /// <summary>True once any safe pose was recorded for this player.</summary>
        public bool HasSafeGround { get; private set; }

        /// <summary>Feet position and yaw-only rotation of the last safe spot.</summary>
        public Pose LastSafePose { get; private set; }

        /// <summary>Seconds grounded on the current spot (for FlowDebug).</summary>
        public float Dwell => dwell;

        void Awake() => player = GetComponent<PlayerController>();

        void OnEnable() => EventBus.Subscribe<PlayerTeleported>(OnTeleported);
        void OnDisable() => EventBus.Unsubscribe<PlayerTeleported>(OnTeleported);

        /// <summary>Starts the dwell over, so the next safe pose needs fresh time standing still on ground.</summary>
        public void ResetDwell() => dwell = 0f;

        // A threshold moves the player (possibly mid-air); never let the pre-teleport dwell
        // carry over and record a spot the player never stood on.
        void OnTeleported(PlayerTeleported _) => dwell = 0f;

        void Update()
        {
            if (player == null) return;

            // While a respawn is covering the screen the player is being placed; record nothing.
            if (FlowSystem.IsRespawning) { dwell = 0f; return; }

            if (!player.IsGrounded || player.IsSliding || FlowSystem.IsInNoSafeGround(player))
            {
                dwell = 0f;
                return;
            }

            dwell += Time.deltaTime;
            if (dwell < ConfigLookup.Get<FlowConfig>().SafeGroundDwell) return;

            var t = player.transform;
            LastSafePose = new Pose(t.position, Quaternion.Euler(0f, t.eulerAngles.y, 0f));
            HasSafeGround = true;
        }
    }
}
