using Kit.Gameplay.Letterboards;
using Kit.Gameplay.Thresholds;
using UnityEngine;

namespace Kit.Gameplay
{
    // Events for the OOO prototype's systems. Kept apart from GameplayEvents.cs (the kit's own)
    // so the kit file stays untouched and the prototype's contracts read in one place.

    /// <summary>
    /// The player was moved through a threshold. Map is the full rigid transform applied (position
    /// and rotation), Delta the change in position. The camera re-aims with Map's rotation.
    /// </summary>
    public readonly struct PlayerTeleported
    {
        public readonly Matrix4x4 Map;
        public readonly Vector3 Delta;
        public PlayerTeleported(Matrix4x4 map, Vector3 delta) { Map = map; Delta = delta; }
    }

    /// <summary>The player went through From and came out of To at Speed m/s. Hook for audio and the ending.</summary>
    public readonly struct PlayerCrossedThreshold
    {
        public readonly Threshold From;
        public readonly Threshold To;
        public readonly float Speed;
        public PlayerCrossedThreshold(Threshold from, Threshold to, float speed) { From = from; To = to; Speed = speed; }
    }

    /// <summary>Pairs were recomputed (a number changed, or a threshold appeared or vanished).</summary>
    public readonly struct ThresholdsRepaired
    {
        public readonly int PairCount;
        public ThresholdsRepaired(int pairCount) { PairCount = pairCount; }
    }

    /// <summary>A tile was taken off a letterboard.</summary>
    public readonly struct TilePulled
    {
        public readonly Letterboards.Letterboard Board;
        public readonly int Slot;
        public readonly char Glyph;
        public TilePulled(Letterboards.Letterboard board, int slot, char glyph) { Board = board; Slot = slot; Glyph = glyph; }
    }

    /// <summary>A carried tile was pressed into an empty letterboard slot.</summary>
    public readonly struct TilePlaced
    {
        public readonly Letterboards.Letterboard Board;
        public readonly int Slot;
        public readonly char Glyph;
        public TilePlaced(Letterboards.Letterboard board, int slot, char glyph) { Board = board; Slot = slot; Glyph = glyph; }
    }

    /// <summary>The player touched walkable ground after being airborne. FallSpeed is the downward speed at impact (m/s). Hook for landing dust, shake and thumps.</summary>
    public readonly struct PlayerLanded
    {
        public readonly Vector3 Position;
        public readonly float FallSpeed;
        public PlayerLanded(Vector3 position, float fallSpeed) { Position = position; FallSpeed = fallSpeed; }
    }

    /// <summary>The player fell below GameConfig.KillHeight (published once per fall). The Flow system respawns them.</summary>
    public readonly struct PlayerFellOut { }

    /// <summary>The player was put back on solid ground after a fall.</summary>
    public readonly struct PlayerRespawned
    {
        public readonly Vector3 Position;
        public PlayerRespawned(Vector3 position) { Position = position; }
    }

    /// <summary>The player walked into a different area (AreaZone id, e.g. "A1"). Hook for ambience.</summary>
    public readonly struct PlayerEnteredArea
    {
        public readonly string AreaId;
        public PlayerEnteredArea(string areaId) { AreaId = areaId; }
    }
}
