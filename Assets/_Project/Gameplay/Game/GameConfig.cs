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

        [Tooltip("Falling below this height (m) respawns the player with momentum cleared.")]
        public float KillHeight = -25f;

        [Tooltip("On: a fall publishes PlayerFellOut and the Flow system returns the player to the last solid ground they stood on. Off: the kit's respawn at the spawn point.")]
        public bool RespawnAtSafeGround = true;

        [Tooltip("Configs for self-installing systems (thresholds, portals, audio...). Read at runtime through ConfigLookup.Get<T>().")]
        public ConfigAsset[] Systems = new ConfigAsset[0];

        protected override void Sanitize()
        {
            if (string.IsNullOrWhiteSpace(PlayScene)) PlayScene = "Sandbox";
            if (string.IsNullOrWhiteSpace(SpawnPointName)) SpawnPointName = "PlayerSpawn";
            if (Systems == null) Systems = new ConfigAsset[0];
        }
    }
}
