using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// The emissive material slot of a renderer (a CRT's screen, a troffer's panel) and its
    /// authored emission colour, so FlickerLight and ScreenGlow can scale the glow per instance
    /// through a MaterialPropertyBlock without copying materials. Props often mix a plain body
    /// material with one emissive one, so the slot is the first material with _EMISSION on.
    /// </summary>
    struct EmissiveSlot
    {
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Renderer renderer;
        int index;
        Color baseEmission;
        MaterialPropertyBlock block;

        /// <summary>True when the renderer has an emissive material to drive.</summary>
        public bool Valid => renderer != null && index >= 0;

        /// <summary>Finds the emissive slot on <paramref name="target"/>; Valid is false when there is none.</summary>
        public static EmissiveSlot Find(Renderer target)
        {
            var slot = new EmissiveSlot { renderer = target, index = -1 };
            if (target == null) return slot;
            var materials = target.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var m = materials[i];
                if (m == null || !m.HasProperty(EmissionId) || !m.IsKeywordEnabled("_EMISSION")) continue;
                slot.index = i;
                slot.baseEmission = m.GetColor(EmissionId);
                slot.block = new MaterialPropertyBlock();
                break;
            }
            return slot;
        }

        /// <summary>Sets the slot's emission to its authored colour times <paramref name="level"/>.</summary>
        public void Set(float level)
        {
            if (!Valid) return;
            renderer.GetPropertyBlock(block, index);
            block.SetColor(EmissionId, baseEmission * level);
            renderer.SetPropertyBlock(block, index);
        }

        /// <summary>Drops the override, so the renderer goes back to its shared material (and SRP batching).</summary>
        public void Clear()
        {
            if (!Valid) return;
            renderer.SetPropertyBlock(null, index);
        }
    }
}
