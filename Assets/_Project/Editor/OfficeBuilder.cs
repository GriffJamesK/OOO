using System.Collections.Generic;
using System.Linq;
using Kit.Core.Config;
using Kit.Gameplay;
using Kit.Gameplay.Audio;
using Kit.Gameplay.Flow;
using Kit.Gameplay.Letterboards;
using Kit.Gameplay.Look;
using Kit.Gameplay.Portals;
using Kit.Gameplay.Signage;
using Kit.Gameplay.Thresholds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Editor
{
    /// <summary>
    /// Builds the OOO world: system configs, the generated assets each system owns (materials,
    /// props, shaders' materials), then Scenes/Office.unity from the area builders. Each area and
    /// asset group is a partial method implemented in its own file by the worker who owns it
    /// (OfficeBuilder.Area_A1.cs ...), so areas are built in parallel without sharing a file; a
    /// partial method nobody has implemented yet compiles to nothing. Re-running is safe.
    /// Menu: Kit > Build Office. MCP: <c>Kit.Editor.OfficeBuilder.BuildOffice();</c>
    /// </summary>
    public static partial class OfficeBuilder
    {
        public const string OfficeScenePath = OfficeKit.Root + "/Scenes/Office.unity";
        const string BootPath = OfficeKit.Root + "/Scenes/Boot.unity";
        const string SandboxPath = OfficeKit.Root + "/Scenes/Sandbox.unity";

        // Asset groups, each implemented by the system's worker in its own file.
        static partial void BuildLook();              // OfficeBuilder.Look.cs: office materials, Volume_Office, renderer, LookConfig
        static partial void BuildSignageAssets();     // OfficeBuilder.Signage.cs: WorldText materials, WorldTextConfig
        static partial void BuildLetterboardAssets(); // OfficeBuilder.Letterboard.cs: felt/tile materials, LetterboardConfig
        static partial void BuildPortalAssets();      // OfficeBuilder.Portals.cs: Mat_Portal, PortalConfig.SurfaceMaterial
        static partial void BuildProps();             // OfficeBuilder.Props.cs: every OfficeKit.PropIds prefab

        // Areas, each implemented in OfficeBuilder.Area_<id>.cs. Build under root, in world space.
        static partial void BuildArea_A1(Transform root); // Waiting Room (spawn)
        static partial void BuildArea_A2(Transform root); // Atrium hub and lobby
        static partial void BuildArea_A3(Transform root); // Open Plan
        static partial void BuildArea_A4(Transform root); // Cafeteria
        static partial void BuildArea_A5(Transform root); // Stair Core
        static partial void BuildArea_A6(Transform root); // Executive Wing
        static partial void BuildArea_A7(Transform root); // Outside

        /// <summary>Builds every OOO asset and the Office scene, then reopens Boot.</summary>
        [MenuItem("Kit/Build Office")]
        public static void BuildOffice()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("[OOO] Exit Play Mode before building the office.");

            OfficeKit.BeginBuild();
            BuildAssets();
            BuildScene();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);

            string missing = OfficeKit.MissingReport();
            Debug.Log("[OOO] Built the office." + (missing.Length > 0 ? " Stand-ins used: " + missing : ""));
        }

        /// <summary>Configs and every generated asset group, without touching scenes.</summary>
        public static void BuildAssets()
        {
            BuildConfigs();
            BuildLook();
            BuildSignageAssets();
            BuildLetterboardAssets();
            BuildPortalAssets();
            BuildProps();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Creates missing system configs (never overwrites tuned values), lists them in
        /// GameConfig.Systems so ConfigLookup finds them at runtime, and points the game at the
        /// Office scene.
        /// </summary>
        static void BuildConfigs()
        {
            var systems = new ConfigAsset[]
            {
                OfficeKit.Config<ThresholdConfig>(),
                OfficeKit.Config<PortalConfig>(),
                OfficeKit.Config<LetterboardConfig>(),
                OfficeKit.Config<WorldTextConfig>(),
                OfficeKit.Config<FlowConfig>(),
                OfficeKit.Config<AudioConfig>(),
                OfficeKit.Config<LookConfig>(),
            };

            // Lead-owned asset wiring for systems that have no builder of their own.
            var thresholds = OfficeKit.Config<ThresholdConfig>();
            if (thresholds.PlateMaterial == null)
            {
                thresholds.PlateMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{OfficeKit.OfficeMaterialDir}/{OfficeKit.M.MetalDark}.mat");
                EditorUtility.SetDirty(thresholds);
            }

            var game = AssetDatabase.LoadAssetAtPath<GameConfig>(OfficeKit.ConfigDir + "/GameConfig.asset");
            if (game == null) return;
            var list = new List<ConfigAsset>(game.Systems ?? new ConfigAsset[0]);
            foreach (var s in systems)
                if (!list.Any(c => c != null && c.GetType() == s.GetType())) list.Add(s);
            list.RemoveAll(c => c == null);
            game.Systems = list.ToArray();
            game.PlayScene = "Office"; // OOO plays in the office; Sandbox stays in the build as the movement test course
            EditorUtility.SetDirty(game);
        }

        static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironment();

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.86f, 0.9f, 1f);
            sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(62f, -35f, 0f); // steep, so skylights throw shafts onto floors
            RenderSettings.sun = sun;

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(OfficeKit.Root + "/Settings/Volume_Office.asset")
                                   ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>(OfficeKit.Root + "/Settings/Volume_Default.asset");
            volume.gameObject.AddComponent<LookController>();

            BuildArea_A1(AreaRoot("A1_WaitingRoom"));
            BuildArea_A2(AreaRoot("A2_Atrium"));
            BuildArea_A3(AreaRoot("A3_OpenPlan"));
            BuildArea_A4(AreaRoot("A4_Cafeteria"));
            BuildArea_A5(AreaRoot("A5_StairCore"));
            BuildArea_A6(AreaRoot("A6_ExecutiveWing"));
            BuildArea_A7(AreaRoot("A7_Outside"));

            // One knob for the whole building's light level (LookConfig.LightIntensityScale).
            float lightScale = OfficeKit.Config<LookConfig>().LightIntensityScale;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (light.type != LightType.Directional) light.intensity *= lightScale;

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetPositionAndRotation(OfficeLayout.SpawnPosition, OfficeLayout.SpawnRotation);

            EditorSceneManager.SaveScene(scene, OfficeScenePath);
        }

        static Transform AreaRoot(string name) => new GameObject(name).transform;

        /// <summary>
        /// Dark trilight ambient (the office is lit by its own fixtures and skylights, no bake) and
        /// the interior fog from the GDD; LookController keeps fog in sync with the volume stack.
        /// </summary>
        static void ApplyEnvironment()
        {
            RenderSettings.skybox = OfficeSkybox();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.33f, 0.38f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.21f, 0.23f);
            RenderSettings.ambientGroundColor = new Color(0.07f, 0.07f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 8f;
            RenderSettings.fogEndDistance = 70f;
            RenderSettings.fogColor = new Color(0.165f, 0.196f, 0.235f); // #2A323C
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
        }

        /// <summary>
        /// A pale, hazy procedural sky (playdead_01: white-grey haze, a faint sun): the skylights
        /// glow white instead of blue, and the outside reads as fog rather than a clear day.
        /// Created or updated in place at Materials/Office/Mat_Skybox_Office.mat.
        /// </summary>
        static Material OfficeSkybox()
        {
            string path = OfficeKit.OfficeMaterialDir + "/Mat_Skybox_Office.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetFloat("_SunDisk", 1f);
            mat.SetFloat("_SunSize", 0.025f);
            mat.SetFloat("_SunSizeConvergence", 4f);
            mat.SetFloat("_AtmosphereThickness", 2.6f);
            mat.SetColor("_SkyTint", new Color(0.62f, 0.64f, 0.66f));
            mat.SetColor("_GroundColor", new Color(0.72f, 0.74f, 0.76f));
            mat.SetFloat("_Exposure", 1.05f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Boot first, then the Office (the game), then the kit's Sandbox course.</summary>
        public static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(BootPath, true) };
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OfficeScenePath) != null) scenes.Add(new EditorBuildSettingsScene(OfficeScenePath, true));
            scenes.Add(new EditorBuildSettingsScene(SandboxPath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
