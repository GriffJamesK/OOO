using UnityEngine;

namespace Kit.Gameplay.Flow
{
    /// <summary>
    /// A box (axis-aligned, centred on this transform) that counts as "off the world": entering it
    /// returns the player to the last safe ground. Placed by builders under drops that lead nowhere.
    /// Enabled zones register with FlowSystem, which tests the player against them every frame
    /// (a plain bounds test, so no trigger collider or physics layer setup is needed).
    /// </summary>
    public sealed class FallZone : MonoBehaviour
    {
        [SerializeField] Vector3 size = new Vector3(10f, 2f, 10f);

        public Bounds Bounds => new Bounds(transform.position, size);

        /// <summary>Builder entry point (edit mode).</summary>
        public void Setup(Vector3 boxSize) => size = boxSize;

        void OnEnable() => FlowSystem.Register(this);
        void OnDisable() => FlowSystem.Unregister(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
