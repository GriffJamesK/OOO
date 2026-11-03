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

            // The atrium roof is 34 m up and the outside plain is wide: with the default 50 m shadow
            // distance the sun would leak onto floors the roof should cover.
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline is UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset urp && urp.shadowDistance < 110f)
            {
                urp.shadowDistance = 110f;
                EditorUtility.SetDirty(urp);
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
        /// A pale haze sky (playdead_01: white-grey haze, no blue, no sunset): a small generated
        /// cubemap with a vertical gradient from a pale horizon to a slightly darker grey zenith,
        /// on Skybox/Cubemap. The procedural skybox can't do this (thick atmosphere turns orange),
        /// and skyboxes aren't fogged, so the sky itself has to be the fog's colour. The skylights
        /// glow white, and the outside reads as one soft haze. Assets: Generated/Sky_Office.cubemap
        /// and Materials/Office/Mat_Skybox_Office.mat, updated in place.
        /// </summary>
        static Material OfficeSkybox()
        {
            const int size = 32;
            string cubePath = OfficeKit.Root + "/Generated/Sky_Office.cubemap";
            var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(cubePath);
            if (cube == null)
            {
                cube = new Cubemap(size, TextureFormat.RGBA32, false);
                AssetDatabase.CreateAsset(cube, cubePath);
            }
            var zenith = new Color(0.66f, 0.69f, 0.72f);
            var horizon = new Color(0.82f, 0.83f, 0.84f);
            var below = new Color(0.74f, 0.75f, 0.76f);
            var pixels = new Color[size * size];
            foreach (CubemapFace face in new[] { CubemapFace.PositiveX, CubemapFace.NegativeX, CubemapFace.PositiveY, CubemapFace.NegativeY, CubemapFace.PositiveZ, CubemapFace.NegativeZ })
            {
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Direction through this texel; only its height matters for the gradient.
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    Vector3 d = face switch
                    {
                        CubemapFace.PositiveX => new Vector3(1f, -v, -u),
                        CubemapFace.NegativeX => new Vector3(-1f, -v, u),
                        CubemapFace.PositiveY => new Vector3(u, 1f, v),
                        CubemapFace.NegativeY => new Vector3(u, -1f, -v),
                        CubemapFace.PositiveZ => new Vector3(u, -v, 1f),
                        _ => new Vector3(-u, -v, -1f),
                    };
                    float h = d.normalized.y;
                    pixels[y * size + x] = h >= 0f ? Color.Lerp(horizon, zenith, Mathf.Pow(h, 0.6f)) : Color.Lerp(horizon, below, Mathf.Clamp01(-h * 3f));
                }
                cube.SetPixels(pixels, face);
            }
            cube.Apply();
            EditorUtility.SetDirty(cube);

            string path = OfficeKit.OfficeMaterialDir + "/Mat_Skybox_Office.mat";
            var shader = Shader.Find("Skybox/Cubemap");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetTexture("_Tex", cube);
            mat.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f));
            mat.SetFloat("_Exposure", 1f);
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
