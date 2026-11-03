using Kit.Core.Config;
using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>Game-flow settings: which scene to play and which player to spawn into it.</summary>
    [CreateAssetMenu(menuName = "Kit/Config/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ConfigAsset
    {
        [Tooltip("Scene loaded additively by Boot. Must be in Build Settings.")]
        public string PlayScene = "Sandbox";

        [Tooltip("Name of the empty in the play scene where the player spawns.")]
        public string SpawnPointName = "PlayerSpawn";

        [Tooltip("Player prefab instantiated at the spawn point on every (re)start.")]
        public PlayerController PlayerPrefab;

        [Tooltip("Falling below this height (m) respawns the player at the spawn point with momentum cleared.")]
        public float KillHeight = -25f;

        protected override void Sanitize()
        {
            if (string.IsNullOrWhiteSpace(PlayScene)) PlayScene = "Sandbox";
            if (string.IsNullOrWhiteSpace(SpawnPointName)) SpawnPointName = "PlayerSpawn";
        }
    }
}
