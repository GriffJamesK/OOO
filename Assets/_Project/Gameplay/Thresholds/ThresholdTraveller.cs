using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// Lives on the player (added by ThresholdSystem on PlayerSpawned). On PlayerController.Moved it
    /// tests the segment from the previous to the current capsule centre against every paired
    /// threshold nearby; a crossing calls PlayerController.TeleportThrough and publishes
    /// PlayerCrossedThreshold. At most one teleport per frame, so a pair can't ping-pong.
    /// </summary>
    public sealed class ThresholdTraveller : MonoBehaviour
    {
        PlayerController player;

        /// <summary>The player this traveller is bound to.</summary>
        public PlayerController Player => player;

        /// <summary>Attaches to a player. Called by ThresholdSystem.</summary>
        public void Bind(PlayerController target)
        {
            // STUB: implemented by the Thresholds worker (subscribe to target.Moved).
            player = target;
        }

        void OnDestroy()
        {
            // STUB: unsubscribe from player.Moved.
            player = null;
        }
    }
}
