using System.Collections.Generic;
using Kit.Gameplay.Look;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kit.Editor
{
    /// <summary>
    /// The Look asset group: the office material set (every OfficeKit.M name), the light shaft
    /// material, the Volume_Office look profile tuned to inside_02 (dense blue-grey fog, cool grade,
    /// lifted blacks, soft bloom, no vignette), and the renderer settings dozens of interior lights
    /// need (Forward+, a roomier additional-light shadow atlas). Everything is created or updated in
    /// place, so GUIDs (and every scene and prefab reference) survive a rebuild.
    /// </summary>
    public static partial class OfficeBuilder
    {
        const string Look_ProfilePath = OfficeKit.Root + "/Settings/Volume_Office.asset";
        const string Look_RendererPath = OfficeKit.Root + "/Settings/URP_Renderer.asset";
        const string Look_ShaftMaterialName = "Mat_LightShaft";

        /// <summary>How a surface takes light. Most of the office is matte; floors, metal and glass are the exceptions.</summary>
        enum Look_Finish
        {
            Matte,     // no highlights, no reflections: concrete, carpet, paper read as volume and colour
            Satin,     // soft highlights from nearby lamps: wood, plastic, leather, leaves
            Polished,  // floors that catch lights in long soft streaks (severance_01, control_02)
            Metal,     // metallic with reflections: steel, brass
        }

        /// <summary>One office material: name, sRGB colour, smoothness, metallic, finish.</summary>
        readonly struct Look_Spec
        {
            public readonly string Name;
            public readonly string Hex;
            public readonly float Smoothness;
            public readonly float Metallic;
            public readonly Look_Finish Finish;

            public Look_Spec(string name, string hex, float smoothness, Look_Finish finish, float metallic = 0f)
            {
                Name = name; Hex = hex; Smoothness = smoothness; Finish = finish; Metallic = metallic;
            }
        }

        // Palette from the references (GDD "References"). Concrete stays a cool grey with a hint of
        // green-blue; each warm colour (wood, orange chairs, oxblood) is meant as a single accent.
        static readonly Look_Spec[] Look_Specs =
        {
            new Look_Spec(OfficeKit.M.Concrete,      "#7E8584", 0.18f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.ConcreteDark,  "#4A504F", 0.15f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.ConcreteLight, "#A9AEAB", 0.20f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.ConcreteFloor, "#6D7271", 0.45f, Look_Finish.Polished),
            new Look_Spec(OfficeKit.M.Terrazzo,      "#9A968C", 0.42f, Look_Finish.Polished),
            new Look_Spec(OfficeKit.M.Slate,         "#2F3335", 0.45f, Look_Finish.Polished),
            new Look_Spec(OfficeKit.M.CarpetOxblood, "#5A1E1E", 0.10f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.CarpetGrey,    "#6E6A62", 0.10f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.WoodVeneer,    "#7A4A2A", 0.32f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.WoodDark,      "#3B2618", 0.35f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.Beige,         "#B9B09A", 0.22f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.ChairOrange,   "#B8632E", 0.38f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.BenchGreen,    "#24443A", 0.20f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.FloorInlay,    "#9DA45A", 0.42f, Look_Finish.Polished),
            new Look_Spec(OfficeKit.M.Plant,         "#2F5A34", 0.30f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.PlantDark,     "#1E3A24", 0.30f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.Soil,          "#2A2118", 0.10f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.Metal,         "#8E9294", 0.50f, Look_Finish.Metal, 0.6f),
            new Look_Spec(OfficeKit.M.MetalDark,     "#1A1C1E", 0.40f, Look_Finish.Satin, 0.3f),
            new Look_Spec(OfficeKit.M.Brass,         "#A88A55", 0.55f, Look_Finish.Metal, 0.7f),
            new Look_Spec(OfficeKit.M.Felt,          "#141414", 0.10f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.TileWhite,     "#E9E6DE", 0.35f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.Whiteboard,    "#ECEDEA", 0.50f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.Paper,         "#E8E2D0", 0.10f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.Cork,          "#9C7448", 0.10f, Look_Finish.Matte),
            new Look_Spec(OfficeKit.M.Frame,         "#2A2522", 0.30f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.Leather,       "#2B2220", 0.35f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.Plastic,       "#C9C2AE", 0.30f, Look_Finish.Satin),
            new Look_Spec(OfficeKit.M.Exterior,      "#C9CDD0", 0.15f, Look_Finish.Matte),
        };

        /// <summary>Builds every Look asset. Called from BuildAssets after BuildConfigs.</summary>
        static partial void BuildLook()
        {
            if (!AssetDatabase.IsValidFolder(OfficeKit.OfficeMaterialDir))
                AssetDatabase.CreateFolder(OfficeKit.Root + "/Materials", "Office");

            Look_BuildMaterials();
            Look_BuildShaftMaterial();
            Look_BuildVolumeProfile();
            Look_ConfigureRenderer();
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ materials

        static void Look_BuildMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var grid = Shader.Find("Kit/GridChecker");

            foreach (var spec in Look_Specs)
            {
                var m = Look_LitMaterial(spec.Name, lit, Look_Hex(spec.Hex), spec.Smoothness, spec.Metallic, spec.Finish);
                Look_Finalise(m, null, 1f);
            }

            // Acoustic ceiling: 0.6 m tiles in world space, so every ceiling reads the same scale
            // whatever its mesh. Ceilings get almost no direct light, so the main-light-only grid
            // shader costs nothing visible here.
            var ceilingColour = Look_Hex("#B8B5AC");
            var ceiling = grid != null ? Look_Material(OfficeKit.M.CeilingTile, grid) : null;
            if (ceiling != null)
            {
                ceiling.SetColor("_ColorA", ceilingColour);
                ceiling.SetColor("_ColorB", ceilingColour * 0.78f);
                ceiling.SetFloat("_CellSize", 0.6f);
                ceiling.SetFloat("_Mode", 1f);
                ceiling.SetFloat("_LineWidth", 0.018f);
                Look_Label(ceiling, "role-ceiling");
                EditorUtility.SetDirty(ceiling);
            }
            else Look_Finalise(Look_LitMaterial(OfficeKit.M.CeilingTile, lit, ceilingColour, 0.1f, 0f, Look_Finish.Matte), null, 1f);

            // Glass: barely there. Transparent, alpha blended, glossy so lamps glint on it.
            var glass = Look_LitMaterial(OfficeKit.M.Glass, lit, Look_Hex("#9FB3B8", 0.12f), 0.95f, 0f, Look_Finish.Metal);
            glass.SetFloat("_Surface", 1f); // Transparent
            glass.SetFloat("_Blend", 0f);   // Alpha
            glass.SetFloat("_BlendModePreserveSpecular", 1f);
            Look_Finalise(glass, null, 1f);
            // Window light must fall through glazing (brutal_02's bars of daylight), so glass casts no shadow.
            glass.SetShaderPassEnabled("ShadowCaster", false);
            glass.renderQueue = (int)RenderQueue.Transparent;
            Look_Label(glass, "role-glass");

            // Emissive: dark bases, glow above the bloom threshold (1.0) so they bloom softly.
            Look_Finalise(Look_LitMaterial(OfficeKit.M.Crt, lit, Look_Hex("#0F1712"), 0.6f, 0f, Look_Finish.Satin), Look_Hex("#39FF88"), 1.5f);
            Look_Finalise(Look_LitMaterial(OfficeKit.M.LightPanel, lit, Look_Hex("#D8DCE0"), 0.2f, 0f, Look_Finish.Matte), Look_Hex("#E4ECFF"), 3f);
            Look_Finalise(Look_LitMaterial(OfficeKit.M.LightWarm, lit, Look_Hex("#E0C8A0"), 0.2f, 0f, Look_Finish.Matte), Look_Hex("#FFB066"), 2.5f);
        }

        /// <summary>URP Lit with the base colour, smoothness, metallic and the finish's highlight and reflection toggles.</summary>
        static Material Look_LitMaterial(string name, Shader lit, Color colour, float smoothness, float metallic, Look_Finish finish)
        {
            var m = Look_Material(name, lit);
            m.SetFloat("_WorkflowMode", 1f); // metallic
            m.SetFloat("_Surface", 0f);
            m.SetColor("_BaseColor", colour);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            // No reflection probes are placed, so environment reflections would mirror the bright
            // sky indoors; only metal and glass keep them. Highlights stay for anything not matte.
            m.SetFloat("_SpecularHighlights", finish == Look_Finish.Matte ? 0f : 1f);
            m.SetFloat("_EnvironmentReflections", finish == Look_Finish.Metal ? 1f : 0f);
            Look_Label(m, "role-" + finish.ToString().ToLowerInvariant());
            return m;
        }

        /// <summary>
        /// Emission (or none), then the same keyword and render-state pass URP's material inspector
        /// runs, so surface type, blend, queue, ZWrite and the Lit toggles all agree.
        /// </summary>
        static void Look_Finalise(Material m, Color? emission, float emissionIntensity)
        {
            if (emission.HasValue)
            {
                m.SetColor("_EmissionColor", Look_Hdr(emission.Value, emissionIntensity));
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                Look_Label(m, "role-glow");
            }
            else
            {
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
            BaseShaderGUI.SetMaterialKeywords(m, global::UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
            if (emission.HasValue) m.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
        }

        /// <summary>The office material at Materials/Office/&lt;name&gt;.mat, created if missing; the shader is swapped in place so the GUID survives.</summary>
        static Material Look_Material(string name, Shader shader)
        {
            string path = $"{OfficeKit.OfficeMaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }

        /// <summary>Labels every office material "office" plus its role, like the kit's greybox set.</summary>
        static void Look_Label(Material m, string role)
        {
            var labels = new List<string>(AssetDatabase.GetLabels(m));
            labels.RemoveAll(l => l.StartsWith("role-") && role != "role-glow");
            if (!labels.Contains("office")) labels.Add("office");
            if (!labels.Contains(role)) labels.Add(role);
            AssetDatabase.SetLabels(m, labels.ToArray());
        }

        static Color Look_Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = alpha;
            return c;
        }

        /// <summary>
        /// An HDR colour whose linear value is <paramref name="c"/>'s linear value times
        /// <paramref name="intensity"/>. Material colours are given in sRGB and converted to linear
        /// on upload, so scaling the sRGB value directly would overshoot (x1.5 becomes about x2.4).
        /// </summary>
        static Color Look_Hdr(Color c, float intensity)
        {
            var hdr = (c.linear * intensity).gamma;
            hdr.a = 1f;
            return hdr;
        }

        /// <summary>
        /// Mat_LightShaft (Kit/LightShaft) at Materials/Office/, assigned to LookConfig. Material
        /// values are only defaults: LightShaft pushes the LookConfig numbers per renderer.
        /// </summary>
        static void Look_BuildShaftMaterial()
        {
            var shader = Shader.Find("Kit/LightShaft");
            var config = OfficeKit.Config<LookConfig>();
            if (shader == null)
            {
                Debug.LogWarning("[OOO] Kit/LightShaft shader not found; light shafts will not render.");
                return;
            }
            var m = Look_Material(Look_ShaftMaterialName, shader);
            m.SetColor("_Tint", new Color(0.8f, 0.86f, 0.95f, 0.25f));
            m.SetFloat("_Intensity", config.ShaftIntensity);
            m.SetFloat("_EdgeSoftness", config.ShaftEdgeSoftness);
            m.SetFloat("_BottomBrightness", config.ShaftBottomBrightness);
            m.SetFloat("_TopFade", config.ShaftTopFade);
            m.SetFloat("_BottomFade", config.ShaftBottomFade);
            m.SetFloat("_NearFadeStart", config.ShaftNearFadeStart);
            m.SetFloat("_NearFadeEnd", config.ShaftNearFadeEnd);
            m.SetFloat("_FogFade", config.ShaftFogFade);
            m.SetFloat("_ForwardScatter", config.ShaftForwardScatter);
            m.renderQueue = (int)RenderQueue.Transparent;
            Look_Label(m, "role-light");
            EditorUtility.SetDirty(m);

            // Only the reference is written: the config's numbers are the owner's to tune.
            if (config.LightShaftMaterial != m)
            {
                config.LightShaftMaterial = m;
                EditorUtility.SetDirty(config);
            }
        }

        // ------------------------------------------------------------------ volume profile

        /// <summary>
        /// Volume_Office, tuned to inside_02: dense blue-grey fog 8-70 m, ACES, soft wide bloom so
        /// shafts, CRTs and troffers glow, contrast up and saturation down, a slightly cool filter,
        /// blacks lifted toward blue (inside_02 never goes fully black) and a faint grain. No
        /// vignette (owner decision, DECISIONS.md).
        /// </summary>
        static void Look_BuildVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Look_ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, Look_ProfilePath);
            }

            var fog = Look_Component<KitFog>(profile);
            fog.enabled.Override(true);
            fog.start.Override(8f);
            fog.end.Override(70f);
            fog.color.Override(Look_Hex("#2A323C"));

            Look_Component<Tonemapping>(profile).mode.Override(TonemappingMode.ACES);

            var bloom = Look_Component<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.7f);

            var colour = Look_Component<ColorAdjustments>(profile);
            colour.contrast.Override(12f);
            colour.saturation.Override(-15f);
            colour.colorFilter.Override(new Color(0.93f, 0.97f, 1f));

            var lgg = Look_Component<LiftGammaGain>(profile);
            // URP lift = 0.15 x linear(colour) - its luminance + w, added before tonemapping: this
            // gives blacks of about (0.006, 0.013, 0.023) linear, a deep blue-grey floor, not a wash.
            lgg.lift.Override(new Vector4(1f, 1.02f, 1.05f, 0.012f));
            lgg.gamma.Override(new Vector4(1f, 1f, 1f, 0f));
            lgg.gain.Override(new Vector4(1f, 1f, 1f, 0f));

            var grain = Look_Component<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.12f);
            grain.response.Override(0.8f);

            Look_RemoveComponent<Vignette>(profile);
            EditorUtility.SetDirty(profile);
        }

        /// <summary>The profile's component of type T, added (as a sub-asset, so it is saved) if missing; every component is active.</summary>
        static T Look_Component<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(false);
                component.name = typeof(T).Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        /// <summary>Removes a component and its sub-asset, so the profile file doesn't keep an orphan.</summary>
        static void Look_RemoveComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T existing)) return;
            profile.Remove<T>();
            AssetDatabase.RemoveObjectFromAsset(existing);
            Object.DestroyImmediate(existing, true);
        }

        // ------------------------------------------------------------------ renderer

        /// <summary>
        /// Forward+ on the URP renderer (clustered lighting: no 8-lights-per-object limit, so a room
        /// of troffers lights every surface), plus per-pixel additional lights with shadows and a
        /// 4096 additional-light shadow atlas, enough for a few shadowed spots per room. Set through
        /// SerializedObject so OnValidate runs and URP rebuilds the renderer; renderer features are
        /// untouched. Applies to every URP asset in Graphics and Quality settings.
        /// </summary>
        static void Look_ConfigureRenderer()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(Look_RendererPath);
            if (rendererData != null)
            {
                var so = new SerializedObject(rendererData);
                var mode = so.FindProperty("m_RenderingMode");
                if (mode != null && mode.intValue != (int)RenderingMode.ForwardPlus)
                {
                    mode.intValue = (int)RenderingMode.ForwardPlus;
                    so.ApplyModifiedProperties();
                }
                else if (mode == null) Debug.LogWarning("[OOO] URP_Renderer has no m_RenderingMode; Forward+ not set.");
            }
            else Debug.LogWarning($"[OOO] {Look_RendererPath} not found; Forward+ not set.");

            var pipelines = new HashSet<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset main) pipelines.Add(main);
            for (int i = 0; i < QualitySettings.count; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset level) pipelines.Add(level);

            foreach (var pipeline in pipelines)
            {
                var so = new SerializedObject(pipeline);
                Look_SetInt(so, "m_AdditionalLightsRenderingMode", (int)LightRenderingMode.PerPixel, raiseOnly: false);
                Look_SetInt(so, "m_AdditionalLightShadowsSupported", 1, raiseOnly: true);
                Look_SetInt(so, "m_AdditionalLightsShadowmapResolution", 4096, raiseOnly: true);
                so.ApplyModifiedProperties();
            }
        }

        /// <summary>Sets an int/bool/enum property by name; with <paramref name="raiseOnly"/>, never lowers a value someone raised further.</summary>
        static void Look_SetInt(SerializedObject so, string property, int value, bool raiseOnly)
        {
            var p = so.FindProperty(property);
            if (p == null) { Debug.LogWarning($"[OOO] {so.targetObject.name} has no {property}."); return; }
            if (p.propertyType == SerializedPropertyType.Boolean)
            {
                if (!raiseOnly || !p.boolValue) p.boolValue = value != 0;
                return;
            }
            if (raiseOnly && p.intValue >= value) return;
            p.intValue = value;
        }
    }
}
