using Kit.Gameplay.Portals;
using UnityEditor;
using UnityEngine;

namespace Kit.Editor
{
    public static partial class OfficeBuilder
    {
        const string Portals_MaterialPath = OfficeKit.Root + "/Materials/Mat_Portal.mat";

        /// <summary>
        /// Portals asset group: creates or updates Materials/Mat_Portal.mat on the Kit/Portal shader
        /// and assigns it to PortalConfig.SurfaceMaterial, so every portal surface shares one
        /// material and the shader is referenced by a build (Shader.Find alone would strip it).
        /// Each view's texture goes in through a MaterialPropertyBlock, never into this asset.
        /// </summary>
        static partial void BuildPortalAssets()
        {
            var shader = Shader.Find("Kit/Portal");
            if (shader == null)
            {
                Debug.LogError("[OOO] Kit/Portal shader not found; Mat_Portal not built. Is Shaders/Portal.shader imported without errors?");
                return;
            }

            var material = Portals_LoadOrCreateMaterial(Portals_MaterialPath, shader);
            material.SetColor("_FillColor", new Color(0.165f, 0.196f, 0.235f)); // office fog #2A323C, shown by idle views
            material.SetFloat("_FillAmount", 0f);
            material.enableInstancing = false; // views differ by property block; instancing would gain nothing
            EditorUtility.SetDirty(material);

            var config = OfficeKit.Config<PortalConfig>();
            if (config.SurfaceMaterial != material)
            {
                config.SurfaceMaterial = material;
                EditorUtility.SetDirty(config);
            }
        }

        /// <summary>Loads the material at <paramref name="path"/>, creating it (or fixing its shader) as needed.</summary>
        static Material Portals_LoadOrCreateMaterial(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }
            return material;
        }
    }
}
