using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Gameplay.Look
{
    /// <summary>
    /// A fake volumetric shaft of light (skylight or window beam): a soft additive volume from its
    /// transform (top centre) down along -up, Size = width, length, depth, widening slightly toward
    /// the bottom (LookConfig.ShaftBottomWiden). Fades with distance, fog and view angle so it reads
    /// as light in fog, as in inside_02's skylight. Built by OfficeKit.LightShaft.
    ///
    /// The mesh is only a bounding frustum; the Kit/LightShaft shader marches the view ray through
    /// the frustum and clips it against the depth buffer, so the shaft looks the same from every
    /// side, from inside, and where it meets floors and walls. Shape, tint and the LookConfig
    /// numbers go in through a MaterialPropertyBlock: one shared material, no copies.
    /// </summary>
    public sealed class LightShaft : MonoBehaviour
    {
        [SerializeField] Vector3 size = new Vector3(3f, 12f, 3f);
        [SerializeField] Color tint = new Color(0.8f, 0.86f, 0.95f, 0.25f);

        public Vector3 Size => size;
        public Color Tint => tint;

        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int ShapeId = Shader.PropertyToID("_ShaftShape");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        static readonly int BottomBrightnessId = Shader.PropertyToID("_BottomBrightness");
        static readonly int TopFadeId = Shader.PropertyToID("_TopFade");
        static readonly int BottomFadeId = Shader.PropertyToID("_BottomFade");
        static readonly int NearFadeStartId = Shader.PropertyToID("_NearFadeStart");
        static readonly int NearFadeEndId = Shader.PropertyToID("_NearFadeEnd");
        static readonly int FogFadeId = Shader.PropertyToID("_FogFade");
        static readonly int ForwardScatterId = Shader.PropertyToID("_ForwardScatter");

        const string MeshName = "LightShaft";

        MaterialPropertyBlock block;

        /// <summary>Builder entry point (edit mode): sets size and tint and builds the shaft mesh.</summary>
        public void Setup(Vector3 shaftSize, Color shaftTint)
        {
            size = new Vector3(Mathf.Max(0.05f, shaftSize.x), Mathf.Max(0.05f, shaftSize.y), Mathf.Max(0.05f, shaftSize.z));
            tint = shaftTint;
            Refresh(rebuildMesh: true);
        }

        void OnEnable() => Refresh(rebuildMesh: false);

        /// <summary>
        /// Makes sure the mesh, renderer and material exist and pushes shape, tint and the current
        /// LookConfig values to the renderer. Call after tuning LookConfig in Play Mode.
        /// </summary>
        public void Refresh(bool rebuildMesh = false)
        {
            var config = LookAssets.Config;
            var filter = GetComponent<MeshFilter>();
            if (filter == null) filter = gameObject.AddComponent<MeshFilter>();
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();

            if (rebuildMesh || filter.sharedMesh == null)
            {
                // Reuse our own mesh when rebuilding, so re-running Setup never leaks one.
                var mesh = filter.sharedMesh;
                if (mesh == null || mesh.name != MeshName) mesh = new Mesh { name = MeshName };
                FillMesh(mesh, size, config.ShaftBottomWiden);
                filter.sharedMesh = mesh;
            }

            // Light, not a surface: it never casts, receives or reflects anything.
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.allowOcclusionWhenDynamic = false;
            if (config.LightShaftMaterial != null) renderer.sharedMaterial = config.LightShaftMaterial;
            renderer.enabled = renderer.sharedMaterial != null; // never draw a pink error box

            block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(TintId, tint);
            block.SetVector(ShapeId, new Vector4(size.x * 0.5f, size.y, size.z * 0.5f, config.ShaftBottomWiden));
            block.SetFloat(IntensityId, config.ShaftIntensity);
            block.SetFloat(EdgeSoftnessId, config.ShaftEdgeSoftness);
            block.SetFloat(BottomBrightnessId, config.ShaftBottomBrightness);
            block.SetFloat(TopFadeId, config.ShaftTopFade);
            block.SetFloat(BottomFadeId, config.ShaftBottomFade);
            block.SetFloat(NearFadeStartId, config.ShaftNearFadeStart);
            block.SetFloat(NearFadeEndId, config.ShaftNearFadeEnd);
            block.SetFloat(FogFadeId, config.ShaftFogFade);
            block.SetFloat(ForwardScatterId, config.ShaftForwardScatter);
            renderer.SetPropertyBlock(block);
        }

        /// <summary>
        /// A closed frustum: top rectangle (size.x by size.z) at y = 0, bottom rectangle widened by
        /// <paramref name="widen"/> at y = -size.y. Triangles face outward, because the shader draws
        /// back faces only: that covers every pixel of the shaft exactly once, whether the camera is
        /// outside or inside it.
        /// </summary>
        static void FillMesh(Mesh mesh, Vector3 size, float widen)
        {
            float hx = size.x * 0.5f, hz = size.z * 0.5f, len = size.y;
            float bx = hx * widen, bz = hz * widen;
            var t0 = new Vector3(-hx, 0f, -hz); var t1 = new Vector3(hx, 0f, -hz);
            var t2 = new Vector3(hx, 0f, hz); var t3 = new Vector3(-hx, 0f, hz);
            var b0 = new Vector3(-bx, -len, -bz); var b1 = new Vector3(bx, -len, -bz);
            var b2 = new Vector3(bx, -len, bz); var b3 = new Vector3(-bx, -len, bz);

            var vertices = new Vector3[24];
            var triangles = new int[36];
            int face = 0;
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                // a-b-c-d go round the face; flip if needed so the winding faces away from the axis.
                var centre = (a + b + c + d) * 0.25f;
                var outward = centre - new Vector3(0f, -len * 0.5f, 0f);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0f) (b, d) = (d, b);
                int v = face * 4, i = face * 6;
                vertices[v] = a; vertices[v + 1] = b; vertices[v + 2] = c; vertices[v + 3] = d;
                triangles[i] = v; triangles[i + 1] = v + 1; triangles[i + 2] = v + 2;
                triangles[i + 3] = v; triangles[i + 4] = v + 2; triangles[i + 5] = v + 3;
                face++;
            }
            Quad(t0, t1, t2, t3); // top
            Quad(b0, b1, b2, b3); // bottom
            Quad(t0, t1, b1, b0); // -z
            Quad(t1, t2, b2, b1); // +x
            Quad(t2, t3, b3, b2); // +z
            Quad(t3, t0, b0, b3); // -x

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
