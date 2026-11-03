using System;
using Kit.Gameplay.Signage;
using UnityEngine;

namespace Kit.Gameplay.Thresholds
{
    /// <summary>
    /// A fixed, screwed-on number plate (brass). It never changes at runtime, so its Changed event
    /// never fires; the number is shown with WorldText in the brass style on a thin dark backing.
    /// Built by OfficeKit.Threshold.
    /// </summary>
    public sealed class BrassPlate : MonoBehaviour, INumberSource
    {
        const string BackingName = "Backing";
        const string NumberName = "Number";

        [SerializeField] string number = "";

        public string Raw => number;

        // A brass plate never changes; the event exists only to satisfy INumberSource.
        public event Action Changed { add { } remove { } }

        /// <summary>
        /// Builder entry point (edit mode, also used by tests in play mode): sets the number and
        /// builds the plate visual facing this transform's forward. The backing sits just behind the
        /// transform (local z from -depth to 0) and the number just in front, so the builder can put
        /// the transform on the frame's face. Calling it again rebuilds the visual.
        /// </summary>
        public void Setup(string plateNumber, float letterHeight)
        {
            number = plateNumber ?? "";
            var cfg = ConfigLookup.Get<ThresholdConfig>();
            ClearVisual();

            float h = Mathf.Max(0.005f, letterHeight);
            float pad = h * cfg.PlatePadding;
            int glyphs = Mathf.Max(1, number.Length);
            var plateSize = new Vector3(glyphs * h * cfg.PlateGlyphWidth + pad * 2f, h + pad * 2f, cfg.PlateDepth);

            // The backing is a primitive cube without its collider: the plate often sits inside an
            // opening's frame, and it must never block the player or a tile raycast.
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(backing.GetComponent<Collider>());
            backing.name = BackingName;
            backing.transform.SetParent(transform, false);
            backing.transform.localPosition = new Vector3(0f, 0f, -plateSize.z * 0.5f);
            backing.transform.localScale = plateSize;
            if (cfg.PlateMaterial != null) backing.GetComponent<MeshRenderer>().sharedMaterial = cfg.PlateMaterial;

            // A WorldText reads from its -Z side (TextMesh convention); a half turn makes it read
            // from the plate's front. A hair in front of the backing so it never z-fights.
            WorldText.Create(transform, NumberName, number, new Vector3(0f, 0f, 0.002f), Quaternion.Euler(0f, 180f, 0f),
                h, WorldTextStyle.Brass);
        }

        /// <summary>
        /// Removes a visual built by an earlier Setup. Edit mode has no end of frame, so builders
        /// need DestroyImmediate; play mode uses Destroy (detaching first so the old parts are gone
        /// from the hierarchy at once).
        /// </summary>
        void ClearVisual()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != BackingName && child.name != NumberName) continue;
                if (Application.isPlaying)
                {
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
                else DestroyImmediate(child.gameObject);
            }
        }
    }
}
