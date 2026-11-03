using Kit.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Kit.Editor
{
    /// <summary>
    /// Rebuilds every kit-generated asset (configs, materials, volume profile, prefabs, scenes,
    /// build settings) from code. Generated assets are reproducible and reviewable as code, and
    /// an agent can repair a broken scene by re-running one menu item instead of hand-editing YAML.
    /// Re-running is safe: existing assets are updated in place, so GUIDs and references survive.
    /// </summary>
    public static partial class KitBuilder
    {
        const string Root = "Assets/_Project";
        const string ConfigDir = Root + "/Config";
        const string MaterialDir = Root + "/Materials";
        const string SettingsDir = Root + "/Settings";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";

        /// <summary>Menu entry and MCP entry point: <c>Kit.Editor.KitBuilder.BuildAll()</c>.</summary>
        [MenuItem("Kit/Rebuild Kit Assets")]
        public static void BuildAll()
        {
            // Scenes can only be rebuilt in edit mode; refuse up front instead of half-building.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("[Kit] Exit Play Mode before rebuilding kit assets.");
            BuildConfigs();
            BuildMaterials();
            BuildVolumeProfiles();
            BuildPrefabs();
            BuildScenes();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Kit] Rebuilt materials, volume profiles, prefabs and scenes (existing configs kept).");
        }

        /// <summary>
        /// Configs are owner-tuned, so they are only created when missing, never overwritten.
        /// A new config gets the defaults written in its class.
        /// </summary>
        static void BuildConfigs()
        {
            LoadOrCreate<GameConfig>(ConfigDir + "/GameConfig.asset");
            LoadOrCreate<PlayerConfig>(ConfigDir + "/PlayerConfig.asset");

            CameraPreset(ConfigDir + "/CameraConfig_ThirdPerson.asset", distance: 6f, damping: 0.3f, fov: 60f, pitchMin: -10f, pitchMax: 60f, start: 15f);
            CameraPreset(ConfigDir + "/CameraConfig_FPS.asset", distance: 0f, damping: 0f, fov: 95f, pitchMin: -80f, pitchMax: 80f, start: 0f);
            CameraPreset(ConfigDir + "/CameraConfig_TopDown.asset", distance: 18f, damping: 0.5f, fov: 45f, pitchMin: 60f, pitchMax: 60f, start: 60f);
        }

        static void CameraPreset(string path, float distance, float damping, float fov, float pitchMin, float pitchMax, float start)
        {
            if (AssetDatabase.LoadAssetAtPath<CameraConfig>(path) != null) return;
            var c = LoadOrCreate<CameraConfig>(path);
            c.Distance = distance;
            c.Damping = damping;
            c.FieldOfView = fov;
            c.PitchMin = pitchMin;
            c.PitchMax = pitchMax;
            c.StartPitch = start;
            EditorUtility.SetDirty(c);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        /// <summary>Sets a serialized (possibly private) field by name. Throws on typos so the builder never fails silently.</summary>
        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no field '{field}'");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no field '{field}'");
            prop.enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, params Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no field '{field}'");
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{name}.mat");
    }
}
