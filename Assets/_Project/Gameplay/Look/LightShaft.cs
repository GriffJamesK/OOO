using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// A fake volumetric shaft of light (skylight or window beam): a soft additive box from its
    /// transform (top centre) down along -up, Size = width, length, depth. Fades with distance and
    /// view angle so it reads as light in fog, as in inside_02's skylight. Built by OfficeKit.LightShaft.
    /// </summary>
    public sealed class LightShaft : MonoBehaviour
    {
        [SerializeField] Vector3 size = new Vector3(3f, 12f, 3f);
        [SerializeField] Color tint = new Color(0.8f, 0.86f, 0.95f, 0.25f);

        public Vector3 Size => size;
        public Color Tint => tint;

        /// <summary>Builder entry point (edit mode): sets size and tint and builds the shaft mesh.</summary>
        public void Setup(Vector3 shaftSize, Color shaftTint)
        {
            size = shaftSize;
            tint = shaftTint;
            // STUB: the Look worker builds the mesh and assigns LookConfig.LightShaftMaterial.
        }
    }
}
