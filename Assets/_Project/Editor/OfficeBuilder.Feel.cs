using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kit.Core.Config;
using Kit.Gameplay;
using Kit.Gameplay.Feel;
using UnityEditor;
using UnityEngine;

namespace Kit.Editor
{
    public static partial class OfficeBuilder
    {
        const string Feel_MaterialPath = OfficeKit.OfficeMaterialDir + "/Mat_Particle_Dust.mat";
        const string Feel_TexturePath = OfficeKit.Root + "/Generated/Tex_Particle_Dust.png";
        const int Feel_TextureSize = 64;

        /// <summary>
        /// Feel asset group: a soft dust texture (Generated/Tex_Particle_Dust.png), the shared
        /// particle material Materials/Office/Mat_Particle_Dust.mat on URP Particles/Unlit (alpha
        /// blended, soft particles, camera fade), assigned to FeelConfig.DustMaterial; and FeelConfig
        /// listed in GameConfig.Systems so ConfigLookup finds it at runtime. A material referenced from
        /// a config also keeps the particle shader in builds (Shader.Find alone would let it be stripped).
        /// Re-running updates in place and never touches FeelConfig's tuned numbers.
        /// </summary>
        static partial void BuildFeelAssets()
        {
            var shader = Shader.Find(FeelMaterials.ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[OOO] Shader '{FeelMaterials.ShaderName}' not found; Mat_Particle_Dust not built.");
                return;
            }

            var texture = Feel_DustTexture();
            var material = AssetDatabase.LoadAssetAtPath<Material>(Feel_MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(Feel_MaterialPath) };
                AssetDatabase.CreateAsset(material, Feel_MaterialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            FeelMaterials.ConfigureDust(material, texture);
            EditorUtility.SetDirty(material);

            var config = OfficeKit.Config<FeelConfig>();
            if (config.DustMaterial != material)
            {
                config.DustMaterial = material;
                EditorUtility.SetDirty(config);
            }

            Feel_RegisterConfig(config);
        }

        /// <summary>
        /// Writes the soft dot PNG once (its pixels are deterministic, so an existing file is already
        /// right) and makes sure it imports as an uncompressed, clamped, alpha-transparent texture:
        /// compression would band the faint gradients that make the dust look soft.
        /// </summary>
        static Texture2D Feel_DustTexture()
        {
            if (!File.Exists(Feel_TexturePath))
            {
                var tex = new Texture2D(Feel_TextureSize, Feel_TextureSize, TextureFormat.RGBA32, false);
                tex.SetPixels32(FeelMaterials.SoftDotPixels(Feel_TextureSize));
                tex.Apply(false);
                File.WriteAllBytes(Feel_TexturePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(Feel_TexturePath, ImportAssetOptions.ForceSynchronousImport);
            }

            if (AssetImporter.GetAtPath(Feel_TexturePath) is TextureImporter importer)
            {
                bool changed = importer.textureType != TextureImporterType.Default
                               || !importer.alphaIsTransparency
                               || importer.alphaSource != TextureImporterAlphaSource.FromInput
                               || importer.wrapMode != TextureWrapMode.Clamp
                               || importer.textureCompression != TextureImporterCompression.Uncompressed
                               || !importer.mipmapEnabled
                               || !importer.sRGBTexture;
                if (changed)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaSource = TextureImporterAlphaSource.FromInput;
                    importer.alphaIsTransparency = true;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = true;
                    importer.sRGBTexture = true;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Feel_TexturePath);
        }

        /// <summary>
        /// Adds FeelConfig to GameConfig.Systems if it is missing (the lead's BuildConfigs list stays
        /// untouched), in the same way BuildConfigs does: existing entries and order are kept.
        /// </summary>
        static void Feel_RegisterConfig(FeelConfig config)
        {
            var game = AssetDatabase.LoadAssetAtPath<GameConfig>(OfficeKit.ConfigDir + "/GameConfig.asset");
            if (game == null) return;
            var list = new List<ConfigAsset>(game.Systems ?? new ConfigAsset[0]);
            if (list.Any(c => c != null && c.GetType() == typeof(FeelConfig))) return;
            list.RemoveAll(c => c == null);
            list.Add(config);
            game.Systems = list.ToArray();
            EditorUtility.SetDirty(game);
        }
    }
}
