using System.Collections.Generic;
using System.Linq;
using Kit.Gameplay.Signage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Kit.Editor
{
    /// <summary>
    /// Props: turns every PropShapes recipe into a mesh asset (Generated/Props/Mesh_&lt;Name&gt;.asset)
    /// and a prefab (OfficeKit.PropPrefabDir/&lt;Id&gt;.prefab) with one renderer, one sub-mesh per
    /// material, and simple box colliders. Meshes and prefabs are overwritten in place, so GUIDs
    /// and scene references survive rebuilds. Also builds Scenes/PropGallery.unity, a lineup of
    /// every prop for screenshots and review. Menu: Kit > Build Props, Kit > Build Prop Gallery.
    /// </summary>
    public static partial class OfficeBuilder
    {
        const string PropMeshDir = OfficeKit.Root + "/Generated/Props";
        const string PropGalleryPath = OfficeKit.Root + "/Scenes/PropGallery.unity";

        /// <summary>
        /// Builds every OfficeKit.PropIds prefab. Called from BuildAssets after the office
        /// materials exist. Objects are assembled in a preview scene so the open scene is never
        /// touched or dirtied. Logs one line with each prop's triangle count (the budget check).
        /// </summary>
        static partial void BuildProps()
        {
            Props_EnsureFolder(PropMeshDir);
            Props_EnsureFolder(OfficeKit.PropPrefabDir);
            var tints = Props_BuildTints();
            var report = new List<string>();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var id in OfficeKit.PropIds.All)
                {
                    if (!PropShapes.Recipes.TryGetValue(id, out var recipe))
                    {
                        Debug.LogError($"[Props] No recipe for {id}; add one to PropShapes.Recipes.");
                        continue;
                    }
                    var kit = new MeshKit();
                    recipe(kit);
                    string name = Props_Short(id);
                    var mesh = kit.Save($"{PropMeshDir}/Mesh_{name}.asset", "Mesh_" + name);

                    var go = new GameObject(id);
                    SceneManager.MoveGameObjectToScene(go, preview);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterials = kit.Materials.Select(m => Props_Material(m, tints)).ToArray();
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    Props_AddColliders(go, kit.Colliders);
                    foreach (var t in go.GetComponentsInChildren<Transform>())
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);

                    PrefabUtility.SaveAsPrefabAsset(go, $"{OfficeKit.PropPrefabDir}/{id}.prefab");
                    Object.DestroyImmediate(go);
                    report.Add($"{name} {kit.TriangleCount}");
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Props] Built {report.Count} props (triangles): {string.Join(", ", report)}");
        }

        /// <summary>Rebuilds just the props (and their tinted materials) without rebuilding the office.</summary>
        [MenuItem("Kit/Build Props")]
        public static void RebuildProps()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("[OOO] Exit Play Mode before building props.");
            OfficeKit.BeginBuild();
            BuildProps();
        }

        /// <summary>
        /// Rebuilds the props, then saves Scenes/PropGallery.unity: every prop on a grey floor in
        /// rows sorted by height (small things in front), each with a name label, under a soft
        /// directional light, with an overview camera plus a disabled close-up camera per row for
        /// screenshots. Not added to build settings. The gallery stays open so it can be shot right away.
        /// </summary>
        [MenuItem("Kit/Build Prop Gallery")]
        public static void BuildPropGallery()
        {
            RebuildProps();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(OfficeKit.Root + "/Materials/Mat_Skybox_Default.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.39f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.19f);
            RenderSettings.fog = false;

            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, 150f, 0f); // from behind the camera, so prop fronts (+Z) are lit
            RenderSettings.sun = sun;

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(OfficeKit.Root + "/Settings/Volume_Office.asset")
                                   ?? AssetDatabase.LoadAssetAtPath<VolumeProfile>(OfficeKit.Root + "/Settings/Volume_Default.asset");

            // Layout: rows along X, each row further back (-Z) than the last.
            var entries = new List<(string id, GameObject prefab, Bounds bounds)>();
            foreach (var id in OfficeKit.PropIds.All)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OfficeKit.PropPrefabDir}/{id}.prefab");
                var mf = prefab != null ? prefab.GetComponent<MeshFilter>() : null;
                if (mf == null || mf.sharedMesh == null) continue;
                entries.Add((id, prefab, mf.sharedMesh.bounds));
            }
            entries.Sort((a, b) => a.bounds.size.y.CompareTo(b.bounds.size.y));

            var root = new GameObject("Props").transform;
            const float maxRowWidth = 13f, gap = 0.9f, labelSpace = 0.9f;
            var rows = new List<List<(string id, GameObject prefab, Bounds bounds)>> { new List<(string, GameObject, Bounds)>() };
            float width = 0f;
            foreach (var e in entries)
            {
                float w = Mathf.Max(e.bounds.size.x, 0.3f);
                if (rows[rows.Count - 1].Count > 0 && width + w > maxRowWidth)
                {
                    rows.Add(new List<(string, GameObject, Bounds)>());
                    width = 0f;
                }
                rows[rows.Count - 1].Add(e);
                width += w + gap;
            }

            float z = 0f, maxWidth = 0f;
            var rowCentres = new List<(Vector3 centre, float width, float height)>();
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                float rowWidth = row.Sum(e => Mathf.Max(e.bounds.size.x, 0.3f)) + gap * (row.Count - 1);
                float rowDepth = row.Max(e => e.bounds.size.z);
                float rowHeight = row.Max(e => e.bounds.size.y);
                float x = -rowWidth * 0.5f;
                float rowZ = z - rowDepth * 0.5f;
                foreach (var (id, prefab, b) in row)
                {
                    float w = Mathf.Max(b.size.x, 0.3f);
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
                    inst.transform.position = new Vector3(x + w * 0.5f - b.center.x, 0f, rowZ - b.center.z);
                    string label = Props_Short(id);
                    OfficeKit.Sign(root, label, new Vector3(x + w * 0.5f, 0.12f, rowZ + rowDepth * 0.5f + 0.35f), Quaternion.LookRotation(new Vector3(0f, 0.7f, 1f)),
                        0.07f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(label.Length * 0.055f + 0.12f, 0.14f));
                    x += w + gap;
                }
                rowCentres.Add((new Vector3(0f, 0f, rowZ), rowWidth, rowHeight));
                maxWidth = Mathf.Max(maxWidth, rowWidth);
                z -= rowDepth + labelSpace + 0.6f;
            }

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            float floorDepth = -z + 8f;
            floor.transform.position = new Vector3(0f, 0f, z * 0.5f);
            floor.transform.localScale = new Vector3((maxWidth + 8f) / 10f, 1f, floorDepth / 10f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = OfficeKit.Mat("Mat_Grey_Floor");

            // Overview camera frames the whole lineup from the front, above.
            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            var target = new Vector3(0f, 0.6f, z * 0.5f);
            float dist = Mathf.Max(maxWidth, -z) * 0.95f + 3f;
            cam.transform.position = target + new Vector3(0f, dist * 0.55f, dist * 0.85f);
            cam.transform.LookAt(target);

            // One disabled close-up per row: enable one (or point manage_camera at it) to inspect a row.
            for (int r = 0; r < rowCentres.Count; r++)
            {
                var (centre, rowWidth, rowHeight) = rowCentres[r];
                var rowCam = new GameObject($"RowCam_{r}").AddComponent<Camera>();
                rowCam.fieldOfView = 45f;
                rowCam.nearClipPlane = 0.02f;
                rowCam.enabled = false;
                float d = Mathf.Max(rowWidth * 0.62f, rowHeight * 1.6f) + 0.8f;
                var look = centre + Vector3.up * (rowHeight * 0.45f);
                rowCam.transform.position = look + new Vector3(0f, d * 0.35f + rowHeight * 0.3f, d);
                rowCam.transform.LookAt(look);
            }

            EditorSceneManager.SaveScene(scene, PropGalleryPath);
            Debug.Log($"[Props] Saved the prop gallery ({entries.Count} props in {rows.Count} rows) to {PropGalleryPath}.");
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Prop id without the "Prop_" prefix, for mesh names and labels.</summary>
        static string Props_Short(string id) => id.StartsWith("Prop_") ? id.Substring(5) : id;

        /// <summary>Resolves a MeshKit material name: generated tints first, then the office palette (with its stand-in fallback).</summary>
        static Material Props_Material(string name, Dictionary<string, Material> tints) =>
            tints.TryGetValue(name, out var m) ? m : OfficeKit.Mat(name);

        /// <summary>
        /// Creates or refreshes the few tinted materials the office palette lacks (PropShapes.Tints):
        /// each is a copy of an office material with a new colour, updated in place so references
        /// survive. Until the base material exists, the copy inherits its stand-in.
        /// </summary>
        static Dictionary<string, Material> Props_BuildTints()
        {
            var result = new Dictionary<string, Material>();
            foreach (var t in PropShapes.Tints)
            {
                string path = $"{PropMeshDir}/{t.Name}.mat";
                var src = OfficeKit.Mat(t.Base);
                if (src == null) src = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool created = mat == null;
                if (created) mat = new Material(src);
                else
                {
                    mat.shader = src.shader;
                    mat.CopyPropertiesFromMaterial(src);
                }
                mat.name = t.Name;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", t.Color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", t.Color);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", t.Smoothness);
                if (t.ClearMap)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", null);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", null);
                }
                if (created) AssetDatabase.CreateAsset(mat, path);
                else EditorUtility.SetDirty(mat);
                result[t.Name] = mat;
            }
            return result;
        }

        /// <summary>
        /// Adds the recorded box colliders: axis-aligned ones (or ones turned by a multiple of
        /// 90 degrees about Y) go straight on the root; any other rotation gets a child object.
        /// </summary>
        static void Props_AddColliders(GameObject go, IReadOnlyList<MeshKit.BoxShape> shapes)
        {
            int n = 0;
            foreach (var s in shapes)
            {
                var e = s.Rotation.eulerAngles;
                bool upright = Mathf.Abs(Mathf.DeltaAngle(e.x, 0f)) < 0.5f && Mathf.Abs(Mathf.DeltaAngle(e.z, 0f)) < 0.5f;
                float quarters = e.y / 90f;
                if (upright && Mathf.Abs(quarters - Mathf.Round(quarters)) < 0.01f)
                {
                    var size = s.Size;
                    if ((Mathf.RoundToInt(quarters) & 1) == 1) size = new Vector3(size.z, size.y, size.x);
                    var box = go.AddComponent<BoxCollider>();
                    box.center = s.Center;
                    box.size = size;
                }
                else
                {
                    var child = new GameObject($"Collider_{n++}");
                    child.transform.SetParent(go.transform, false);
                    child.transform.localPosition = s.Center;
                    child.transform.localRotation = s.Rotation;
                    child.AddComponent<BoxCollider>().size = s.Size;
                }
            }
        }

        /// <summary>Creates an asset folder and any missing parents.</summary>
        static void Props_EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            Props_EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
