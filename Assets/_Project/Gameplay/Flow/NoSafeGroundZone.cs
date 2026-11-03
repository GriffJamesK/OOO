using UnityEngine;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// A box where standing does not count as safe ground (narrow ledges, the edge of a launch),
    /// so a respawn never drops the player back somewhere that leads straight into another fall.
    /// Enabled zones register with FlowSystem; SafeGroundTracker asks it before recording a pose.
    /// </summary>
    public sealed class NoSafeGroundZone : MonoBehaviour
    {
        [SerializeField] Vector3 size = new Vector3(4f, 3f, 4f);

        public Bounds Bounds => new Bounds(transform.position, size);

        /// <summary>Builder entry point (edit mode).</summary>
        public void Setup(Vector3 boxSize) => size = boxSize;

        void OnEnable() => FlowSystem.Register(this);
        void OnDisable() => FlowSystem.Unregister(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
