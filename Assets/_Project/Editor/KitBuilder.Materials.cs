using Kit.Gameplay.Look;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kit.Editor
{
    public static partial class KitBuilder
    {
        // Sky and fog share one tint so the horizon blends into the fog instead of showing a seam.
        static readonly Color SkyTint = new Color(0.50f, 0.60f, 0.75f);
        static readonly Color FogColor = new Color(0.66f, 0.73f, 0.82f);

        /// <summary>
        /// Greybox palette: one role per colour. The role is stored as an asset label
        /// ("role-floor"...) and listed in CLAUDE.md, so agents pick by purpose, not by taste.
        /// </summary>
        static readonly (string name, Color color, string role)[] Greybox =
        {
            ("Mat_Grey_Floor",    new Color(0.78f, 0.76f, 0.72f), "floor"),       // walkable
            ("Mat_Grey_Wall",     new Color(0.72f, 0.74f, 0.78f), "wall"),        // static
            ("Mat_Pastel_Blue",   new Color(0.60f, 0.71f, 0.86f), "interactive"),
            ("Mat_Pastel_Rose",   new Color(0.88f, 0.62f, 0.68f), "hazard"),
            ("Mat_Pastel_Green",  new Color(0.62f, 0.80f, 0.64f), "goal"),        // goal / exit
            ("Mat_Pastel_Butter", new Color(0.93f, 0.87f, 0.60f), "spawn"),
            ("Mat_Pastel_Peach",  new Color(0.92f, 0.73f, 0.62f), "pickup"),      // kit extension
            ("Mat_Pastel_Lilac",  new Color(0.74f, 0.67f, 0.86f), "character"),   // kit extension
        };

        const string DevLight = "Mat_Dev_Grey_Light";
        const string DevMid = "Mat_Dev_Grey_Mid";
        const string DevDark = "Mat_Dev_Grey_Dark";
        const string DevOrange = "Mat_Dev_Orange";

        static readonly (string name, Color baseColor, Color lineColor, string role)[] DevMaterials =
        {
            (DevLight,  new Color(0.70f, 0.70f, 0.69f), new Color(0.60f, 0.60f, 0.59f), "role-dev-structure"),
            (DevMid,    new Color(0.52f, 0.52f, 0.51f), new Color(0.44f, 0.44f, 0.43f), "role-dev-structure"),
            (DevDark,   new Color(0.34f, 0.34f, 0.34f), new Color(0.27f, 0.27f, 0.27f), "role-dev-structure"),
            (DevOrange, new Color(0.86f, 0.46f, 0.13f), new Color(0.72f, 0.36f, 0.08f), "role-dev-feature"),
        };

        static void BuildMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var grid = Shader.Find("Kit/GridChecker");

            // The Mat_Flat_* set is replaced by the greybox palette.
            foreach (var guid in AssetDatabase.FindAssets("Mat_Flat_ t:Material", new[] { MaterialDir }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

            foreach (var (name, color, role) in Greybox)
            {
                var m = Matte(name, lit, color);
                AssetDatabase.SetLabels(m, new[] { "greybox", "role-" + role });
            }

            Lit("Mat_Lit_Warm", lit, new Color(0.60f, 0.50f, 0.42f));
            Lit("Mat_Lit_Cool", lit, new Color(0.42f, 0.50f, 0.60f));

            Emissive("Mat_Emissive_Cyan", lit, new Color(0f, 1f, 1f));
            Emissive("Mat_Emissive_Orange", lit, new Color(1f, 0.45f, 0f));

            var glass = Lit("Mat_Transparent_Glass", lit, new Color(0.75f, 0.88f, 1f, 0.3f));
            glass.SetFloat("_Surface", 1f); // Transparent
            glass.SetFloat("_Blend", 0f);   // Alpha
            glass.SetFloat("_Smoothness", 0.9f);
            BaseShaderGUI.SetMaterialKeywords(glass);
            EditorUtility.SetDirty(glass);

            var checker = GridMaterial("Mat_Grid_Checker", grid, mode: 0,
                new Color(0.80f, 0.78f, 0.75f), new Color(0.71f, 0.69f, 0.66f));
            AssetDatabase.SetLabels(checker, new[] { "greybox", "role-floor", "scale-reference" });
            var wallGrid = GridMaterial("Mat_Grid_Wall", grid, mode: 1,
                new Color(0.74f, 0.76f, 0.80f), new Color(0.64f, 0.66f, 0.71f));
            AssetDatabase.SetLabels(wallGrid, new[] { "greybox", "role-wall", "scale-reference" });

            // Dev-texture set for test and blockout courses: neutral greys at three values for plain
            // geometry, one orange for movement features. All carry the 1 m grid for scale.
            foreach (var (name, baseColor, lineColor, role) in DevMaterials)
            {
                var m = GridMaterial(name, grid, mode: 1, baseColor, lineColor);
                m.SetFloat("_LineWidth", 0.035f);
                AssetDatabase.SetLabels(m, new[] { "dev", role, "scale-reference" });
            }

            var sky = LoadOrCreateMaterial("Mat_Skybox_Default", Shader.Find("Skybox/Procedural"));
            sky.SetColor("_SkyTint", SkyTint);
            sky.SetColor("_GroundColor", new Color(0.37f, 0.35f, 0.34f));
            sky.SetFloat("_AtmosphereThickness", 0.8f);
            sky.SetFloat("_Exposure", 1.2f);
            EditorUtility.SetDirty(sky);

            // Every material states its role, so agents browsing the folder know which to use.
            Label("Mat_Lit_Warm", "polish", "role-tinted-surface");
            Label("Mat_Lit_Cool", "polish", "role-tinted-surface");
            Label("Mat_Emissive_Cyan", "polish", "role-glow");
            Label("Mat_Emissive_Orange", "polish", "role-glow");
            Label("Mat_Transparent_Glass", "polish", "role-glass");
            Label("Mat_Skybox_Default", "environment", "role-sky");
        }

        static void Label(string material, params string[] labels) => AssetDatabase.SetLabels(Mat(material), labels);

        /// <summary>URP Lit, roughness 0.9, specular highlights off: blockouts read as volume, not material.</summary>
        static Material Matte(string name, Shader shader, Color color)
        {
            var m = LoadOrCreateMaterial(name, shader);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            m.SetFloat("_SpecularHighlights", 0f);
            m.SetFloat("_EnvironmentReflections", 0f);
            BaseShaderGUI.SetMaterialKeywords(m);
            // Lit-specific toggles are not covered by the generic keyword setup.
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material GridMaterial(string name, Shader shader, int mode, Color a, Color b)
        {
            var m = LoadOrCreateMaterial(name, shader);
            m.SetColor("_ColorA", a);
            m.SetColor("_ColorB", b);
            m.SetFloat("_CellSize", 1f);
            m.SetFloat("_Mode", mode);
            m.SetFloat("_LineWidth", 0.025f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Lit(string name, Shader shader, Color color)
        {
            var m = LoadOrCreateMaterial(name, shader);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            BaseShaderGUI.SetMaterialKeywords(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Emission at 2x colour: above the bloom threshold of 1.0, so it visibly glows.</summary>
        static void Emissive(string name, Shader shader, Color color)
        {
            var m = Lit(name, shader, color * 0.2f);
            m.SetColor("_EmissionColor", color * 2f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }

        static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }

        /// <summary>
        /// The one look profile: bloom, ACES, a light colour grade and linear fog 20-120 m.
        /// No vignette (owner request). The Lo-Fi preset was removed (owner request).
        /// </summary>
        static void BuildVolumeProfiles()
        {
            var clean = LoadOrCreateProfile(SettingsDir + "/Volume_Default.asset");
            var bloom = Component<Bloom>(clean);
            bloom.intensity.Override(0.4f);
            bloom.threshold.Override(1f);
            Component<Tonemapping>(clean).mode.Override(TonemappingMode.ACES);
            RemoveComponent<Vignette>(clean);
            var color = Component<ColorAdjustments>(clean);
            color.contrast.Override(10f);
            color.saturation.Override(5f);
            var cleanFog = Component<KitFog>(clean);
            cleanFog.enabled.Override(true);
            cleanFog.start.Override(20f);
            cleanFog.end.Override(120f);
            cleanFog.color.Override(FogColor);
            EditorUtility.SetDirty(clean);
        }

        /// <summary>Removes a component and its sub-asset, so the profile file doesn't keep an orphan.</summary>
        static void RemoveComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T existing)) return;
            profile.Remove<T>();
            AssetDatabase.RemoveObjectFromAsset(existing);
            Object.DestroyImmediate(existing, true);
        }

        static VolumeProfile LoadOrCreateProfile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        static T Component<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing)) { EditorUtility.SetDirty(existing); return existing; }
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }
    }
}
