using UnityEngine;

namespace Kit.Gameplay
{
    // Events shared between Gameplay systems. Game-flow events that UI also needs live in
    // Kit.Core.Events.GameEvents instead, because UI cannot reference Gameplay.

    /// <summary>A player instance now exists. Camera rigs retarget from this instead of searching the scene.</summary>
    public readonly struct PlayerSpawned
    {
        public readonly Transform Root;
        public readonly Transform CameraTarget;

        public PlayerSpawned(Transform root, Transform cameraTarget)
        {
            Root = root; CameraTarget = cameraTarget;
        }
    }

    /// <summary>The player left the ground from a jump. Chained = a slide-hop or a hop right on landing (speed boost).</summary>
    public readonly struct PlayerJumped
    {
        public readonly Vector3 Position;
        public readonly bool Chained;
        public PlayerJumped(Vector3 position, bool chained = false) { Position = position; Chained = chained; }
    }

    /// <summary>A wall run started. Side is +1 for a wall on the right, -1 on the left. Hook for wall-run audio/sparks.</summary>
    public readonly struct PlayerWallRan
    {
        public readonly Vector3 Position;
        public readonly float Side;
        public readonly float Speed;
        public PlayerWallRan(Vector3 position, float side, float speed) { Position = position; Side = side; Speed = speed; }
    }

    /// <summary>A slide started. Boosted = this slide added speed (not inside the boost cooldown). Hook for slide audio/dust.</summary>
    public readonly struct PlayerSlid
    {
        public readonly Vector3 Position;
        public readonly float Speed;
        public readonly bool Boosted;
        public PlayerSlid(Vector3 position, float speed, bool boosted) { Position = position; Speed = speed; Boosted = boosted; }
    }

    /// <summary>The player pressed Interact. Interactables near Position decide whether they respond.</summary>
    public readonly struct PlayerInteracted
    {
        public readonly Vector3 Position;
        public readonly Vector3 Forward;
        public PlayerInteracted(Vector3 position, Vector3 forward) { Position = position; Forward = forward; }
    }
}
