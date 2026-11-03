using Kit.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Editor
{
    public static partial class KitBuilder
    {
        const string BootPath = SceneDir + "/Boot.unity";
        const string SandboxPath = SceneDir + "/Sandbox.unity";

        static void BuildScenes()
        {
            BuildSandbox();
            BuildBoot();
            EditorSceneManager.OpenScene(BootPath, OpenSceneMode.Single);
        }

        /// <summary>
        /// Boot holds only what the owner edits by hand: GameManager, UI_Canvas and the active
        /// camera rig. Runners (timers, tweens, debug overlay) self-install and are not placed.
        /// </summary>
        static void BuildBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironment(fog: false);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            Set(gm, "config", AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigDir + "/GameConfig.asset"));

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/UI/UI_Canvas.prefab"));
            // First person is the kit default; swap this instance for another CameraRig_* variant if the GDD says so.
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Camera/CameraRig_FPS.prefab"));

            EditorSceneManager.SaveScene(scene, BootPath);
        }

        /// <summary>
        /// 160x160 m checker ground, soft-shadow sun, global Volume on Volume_Default, the movement
        /// test course (KitBuilder.Course.cs), and the spawn point.
        /// </summary>
        static void BuildSandbox()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ApplyEnvironment(fog: true); // same 20-120 m fog as the profile's KitFog, so edit mode matches play

            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = sun;

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SettingsDir + "/Volume_Default.asset");
            volume.gameObject.AddComponent<Kit.Gameplay.Look.LookController>(); // keeps scene fog in sync with KitFog

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(CourseHalfSize / 5f, 1f, CourseHalfSize / 5f); // Plane is 10 m: 160 m square
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            groundRenderer.sharedMaterial = Mat("Mat_Grid_Checker");
            groundRenderer.shadowCastingMode = ShadowCastingMode.Off;

            BuildCourse(); // the movement test zones: KitBuilder.Course.cs

            var spawnPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spawnPad.name = "SpawnPad";
            Object.DestroyImmediate(spawnPad.GetComponent<Collider>());
            spawnPad.transform.position = new Vector3(SpawnPoint.x, 0.01f, SpawnPoint.z);
            spawnPad.transform.localScale = new Vector3(1.2f, 0.01f, 1.2f);
            spawnPad.GetComponent<MeshRenderer>().sharedMaterial = Mat("Mat_Pastel_Butter");

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.position = SpawnPoint; // south edge, facing +Z down the start straight

            EditorSceneManager.SaveScene(scene, SandboxPath);
        }

        static GameObject Place(Transform parent, string prefabName, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/Greybox/{prefabName}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = position;
            return go;
        }

        /// <summary>
        /// Skybox, trilight ambient (no lighting bake needed) and linear fog 20-120 m tinted to match the sky.
        /// </summary>
        static void ApplyEnvironment(bool fog)
        {
            RenderSettings.skybox = Mat("Mat_Skybox_Default");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.52f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.26f);
            RenderSettings.fog = fog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 120f;
            RenderSettings.fogColor = FogColor;
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
        }

        static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene(SandboxPath, true)
            };
        }
    }
}
