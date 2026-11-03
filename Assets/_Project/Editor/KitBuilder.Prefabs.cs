using Kit.Gameplay;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Kit.Editor
{
    public static partial class KitBuilder
    {
        const float PlayerHeight = 1.8f;
        const float PlayerRadius = 0.35f;
        const float EyeHeight = 1.6f;

        static void BuildPrefabs()
        {
            var playerBase = BuildPlayerBase();
            var capsule = BuildPlayerCapsule(playerBase);

            var game = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigDir + "/GameConfig.asset");
            game.PlayerPrefab = capsule.GetComponent<PlayerController>();
            EditorUtility.SetDirty(game);

            var rigBase = BuildCameraRigBase();
            BuildRigVariant(rigBase, "CameraRig_ThirdPerson", CameraRigMode.ThirdPerson, "CameraConfig_ThirdPerson",
                cm => { cm.AddComponent<CinemachineOrbitalFollow>(); cm.AddComponent<CinemachineRotationComposer>(); });
            BuildRigVariant(rigBase, "CameraRig_FPS", CameraRigMode.FirstPerson, "CameraConfig_FPS",
                cm => { cm.AddComponent<CinemachineHardLockToTarget>(); cm.AddComponent<CinemachinePanTilt>(); });
            BuildRigVariant(rigBase, "CameraRig_TopDown", CameraRigMode.TopDown, "CameraConfig_TopDown",
                cm => { cm.AddComponent<CinemachineFollow>(); cm.AddComponent<CinemachineRotationComposer>(); });

            BuildUICanvas();
            BuildGreyboxPrefabs();
        }

        /// <summary>Logic-only player: CharacterController sized to 1.8 m with its pivot at the feet, no visuals.</summary>
        static GameObject BuildPlayerBase()
        {
            var root = new GameObject("Player_Base") { tag = "Player" };
            var cc = root.AddComponent<CharacterController>();
            cc.height = PlayerHeight;
            cc.radius = PlayerRadius;
            cc.center = new Vector3(0f, PlayerHeight / 2f, 0f);
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.05f;
            cc.minMoveDistance = 0f; // see PlayerController.Awake

            var target = new GameObject("CameraTarget").transform;
            target.SetParent(root.transform, false);
            target.localPosition = new Vector3(0f, EyeHeight, 0f);

            var pc = root.AddComponent<PlayerController>();
            Set(pc, "config", AssetDatabase.LoadAssetAtPath<PlayerConfig>(ConfigDir + "/PlayerConfig.asset"));
            Set(pc, "cameraTarget", target);

            return SavePrefab(root, PrefabDir + "/Player/Player_Base.prefab");
        }

        /// <summary>Variant of Player_Base that adds a capsule body and a "nose" so facing is readable.</summary>
        static GameObject BuildPlayerCapsule(GameObject playerBase)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(playerBase);
            root.name = "Player_Capsule";

            var body = Primitive(PrimitiveType.Capsule, "Body", root.transform, Mat("Mat_Pastel_Lilac"));
            body.transform.localPosition = new Vector3(0f, PlayerHeight / 2f, 0f);
            body.transform.localScale = new Vector3(PlayerRadius * 2f, PlayerHeight / 2f, PlayerRadius * 2f);

            var nose = Primitive(PrimitiveType.Cube, "Nose", root.transform, Mat("Mat_Lit_Warm"));
            nose.transform.localPosition = new Vector3(0f, 1.35f, PlayerRadius);
            nose.transform.localScale = new Vector3(0.25f, 0.12f, 0.15f);

            // First-person rigs switch these to shadows-only so the view is not blocked by the body.
            SetArray(root.GetComponent<PlayerController>(), "bodyRenderers",
                body.GetComponent<MeshRenderer>(), nose.GetComponent<MeshRenderer>());

            return SavePrefab(root, PrefabDir + "/Player/Player_Capsule.prefab");
        }

        /// <summary>Shared rig: the real Camera (brain, post-processing) plus an empty CinemachineCamera the variants fill in.</summary>
        static GameObject BuildCameraRigBase()
        {
            var root = new GameObject("CameraRig_Base");

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineBrain>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            var cmGo = new GameObject("CM Camera");
            cmGo.transform.SetParent(root.transform, false);
            var vcam = cmGo.AddComponent<CinemachineCamera>();

            var rig = root.AddComponent<CameraRig>();
            Set(rig, "virtualCamera", vcam);

            return SavePrefab(root, PrefabDir + "/Camera/CameraRig_Base.prefab");
        }

        static void BuildRigVariant(GameObject rigBase, string name, CameraRigMode mode, string configName,
            System.Action<GameObject> addComponents)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(rigBase);
            root.name = name;
            var rig = root.GetComponent<CameraRig>();
            SetEnum(rig, "mode", (int)mode);
            Set(rig, "config", AssetDatabase.LoadAssetAtPath<CameraConfig>($"{ConfigDir}/{configName}.asset"));
            addComponents(root.GetComponentInChildren<CinemachineCamera>().gameObject);
            rig.ApplyConfig();
            SavePrefab(root, $"{PrefabDir}/Camera/{name}.prefab");
        }

        static GameObject SolidPrimitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>Saves (or overwrites in place, keeping the GUID) and removes the temporary scene instance.</summary>
        static GameObject SavePrefab(GameObject instance, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }
    }
}
