using UnityEngine;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// Finds the LookConfig for look components. At runtime that is ConfigLookup (GameConfig.Systems).
    /// Builders call LightShaft.Setup in edit mode, where nothing is registered yet, so in the editor
    /// this falls back to the real asset at Config/LookConfig.asset; without it a freshly built shaft
    /// would get no material.
    /// </summary>
    static class LookAssets
    {
        const string ConfigPath = "Assets/_Project/Config/LookConfig.asset";

        /// <summary>The LookConfig in use: the registered one, the project asset (editor), or defaults.</summary>
        public static LookConfig Config
        {
            get
            {
                var config = ConfigLookup.Get<LookConfig>();
#if UNITY_EDITOR
                if (config.LightShaftMaterial == null)
                {
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<LookConfig>(ConfigPath);
                    if (asset != null) return asset;
                }
#endif
                return config;
            }
        }
    }
}
