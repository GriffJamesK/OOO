using System.Collections.Generic;
using Kit.Core.Events;
using UnityEngine;

namespace Kit.Gameplay
{
    /// <summary>
    /// A named box of the world (an area such as "A1", or a sub-space such as "A7_Outside").
    /// Each frame the zone system checks which zone holds the player and publishes
    /// PlayerEnteredArea when that changes. A bounds test instead of trigger physics keeps it
    /// deterministic and testable, and works with the CharacterController without a Rigidbody.
    /// When zones overlap, the smallest one wins, so a room inside an area can carry its own id.
    /// </summary>
    public sealed class AreaZone : MonoBehaviour
    {
        [SerializeField] string areaId = "A0";
        [Tooltip("Box size in metres, centred on this transform (axis-aligned; rotation is ignored).")]
        [SerializeField] Vector3 size = Vector3.one * 10f;

        static readonly List<AreaZone> zones = new List<AreaZone>();
        static string current = "";
        static Transform tracked;

        /// <summary>Id of the zone the player is in, or "" when outside every zone.</summary>
        public static string Current => current;

        public string AreaId => areaId;
        public Bounds Bounds => new Bounds(transform.position, size);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            zones.Clear();
            current = "";
            tracked = null;
        }

        /// <summary>Builder entry point: sets id and size in one call.</summary>
        public void Setup(string id, Vector3 boxSize)
        {
            areaId = id;
            size = boxSize;
        }

        void OnEnable()
        {
            zones.Add(this);
            EventBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
        }

        void OnDisable()
        {
            zones.Remove(this);
            EventBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
        }

        static void OnPlayerSpawned(PlayerSpawned e) => tracked = e.Root;

        /// <summary>Id of the smallest zone containing a world point, or "".</summary>
        public static string IdAt(Vector3 point)
        {
            AreaZone best = null;
            float bestVolume = float.MaxValue;
            foreach (var z in zones)
            {
                var b = z.Bounds;
                if (!b.Contains(point)) continue;
                float v = b.size.x * b.size.y * b.size.z;
                if (v < bestVolume) { best = z; bestVolume = v; }
            }
            return best != null ? best.areaId : "";
        }

        void LateUpdate()
        {
            // Every zone runs this, but only the first one each frame does the work.
            if (zones.Count == 0 || zones[0] != this || tracked == null) return;
            string id = IdAt(tracked.position + Vector3.up * 0.9f);
            if (id == current) return;
            current = id;
            EventBus.Publish(new PlayerEnteredArea(id));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
