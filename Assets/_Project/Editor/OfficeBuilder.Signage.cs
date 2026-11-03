using Kit.Gameplay.Signage;
using UnityEditor;
using UnityEngine;

namespace Kit.Editor
{
    public static partial class OfficeBuilder
    {
        const string Signage_ShaderName = "Kit/WorldText";

        /// <summary>
        /// One material per WorldTextStyle, in enum order. Colours from the art direction: near-black
        /// print on light boards, warm off-white on dark plates, brass plate numbers with a soft
        /// highlight, and pale CRT green that emits about twice its colour so it blooms a little.
        /// </summary>
        static readonly (string name, Color color, float specular, float smoothness, float emission)[] Signage_Styles =
        {
            ("Mat_WorldText_DarkOnLight", Signage_Hex(0x1C1C1E), 0.05f, 0.3f, 0f),
            ("Mat_WorldText_LightOnDark", Signage_Hex(0xE6E1D6), 0.05f, 0.3f, 0f),
            ("Mat_WorldText_Brass",       Signage_Hex(0xB9975B), 0.6f,  0.65f, 0f),
            ("Mat_WorldText_Glow",        Signage_Hex(0x8CFFB0), 0f,    0.3f, 2f),
        };

        /// <summary>
        /// Creates or updates the WorldText style materials (Materials/Office/Mat_WorldText_*) and
        /// wires them, plus the LegacyRuntime font, into WorldTextConfig. Only the materials and
        /// those two fields are written, so the owner's tuned sizing values survive a rebuild.
        /// </summary>
        static partial void BuildSignageAssets()
        {
            var shader = Shader.Find(Signage_ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[OOO] Shader '{Signage_ShaderName}' not found; WorldText materials were not built.");
                return;
            }
            Signage_EnsureFolder(OfficeKit.OfficeMaterialDir);

            var cfg = OfficeKit.Config<WorldTextConfig>();
            var mats = new Material[Signage_Styles.Length];
            for (int i = 0; i < Signage_Styles.Length; i++)
            {
                var s = Signage_Styles[i];
                mats[i] = Signage_Material(s.name, shader, s.color, s.specular, s.smoothness, s.emission);
            }

            cfg.StyleMaterials = mats;
            cfg.Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cfg.Validate();
            EditorUtility.SetDirty(cfg);
        }

        /// <summary>Loads or creates one style material and writes its colour, highlight and emission.</summary>
        static Material Signage_Material(string name, Shader shader, Color color, float specular, float smoothness, float emission)
        {
            string path = $"{OfficeKit.OfficeMaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_Color", color);
            m.SetFloat("_Specular", specular);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Cutoff", 0.5f);
            // Scale in linear space, then hand Unity the gamma value it expects for colour properties.
            m.SetColor("_EmissionColor", emission > 0f ? (color.linear * emission).gamma : Color.black);
            m.renderQueue = -1; // use the shader's AlphaTest queue
            m.enableInstancing = false;
            AssetDatabase.SetLabels(m, new[] { "worldtext" });
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Creates every missing folder along an Assets/... path.</summary>
        static void Signage_EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            Signage_EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        /// <summary>sRGB colour from 0xRRGGBB, so the art-direction hex values read as written.</summary>
        static Color Signage_Hex(int rgb)
            => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
