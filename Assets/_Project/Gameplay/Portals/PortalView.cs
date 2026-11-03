using Kit.Gameplay.Thresholds;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kit.Gameplay.Portals
{
    /// <summary>
    /// The see-through half of a paired threshold (GDD F3). Lives under the threshold's
    /// PortalSurfaceRoot and owns:
    /// - a surface on layer Portal, sized to the opening, drawn with Kit/Portal, which samples
    ///   this view's render texture in screen space;
    /// - a camera placed at PairMap * main camera, rendering the partner's side into that texture.
    /// Portal cameras leave out the Portal and ViewModel layers, so recursion stops at depth 1:
    /// a portal seen through a portal shows the real space behind it.
    /// PortalSystem creates, selects and places views; a view never decides on its own to render.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class PortalView : MonoBehaviour
    {
        static readonly int PortalTexId = Shader.PropertyToID("_PortalTex");
        static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");

        Threshold threshold;
        Transform surface;
        MeshFilter surfaceFilter;
        MeshRenderer surfaceRenderer;
        Camera viewCamera;
        RenderTexture target;
        MaterialPropertyBlock block;
        int approachSide; // +1 the main camera came at the opening from its front, -1 from behind, 0 not known yet
        bool extruded;

        /// <summary>The threshold this view belongs to.</summary>
        public Threshold Threshold => threshold;

        /// <summary>The virtual camera (rendering only while this view is selected).</summary>
        public Camera ViewCamera => viewCamera;

        /// <summary>True while this view's camera is in this frame's render list.</summary>
        public bool Rendering => viewCamera != null && viewCamera.enabled;

        /// <summary>True while the surface is the near-plane box instead of the flat quad.</summary>
        public bool Extruded => extruded;

        /// <summary>Which side the main camera is treated as being on (+1 front, -1 back, 0 unknown).</summary>
        public int ApproachSide => approachSide;

        /// <summary>Distance (m) from the main camera to the surface at the last selection (infinity if not measured).</summary>
        public float Distance { get; internal set; } = float.PositiveInfinity;

        /// <summary>World bounds of the surface, used for the distance and frustum tests.</summary>
        public Bounds SurfaceBounds => surfaceRenderer != null ? surfaceRenderer.bounds : new Bounds(transform.position, Vector3.zero);

        /// <summary>The surface renderer (tests and debug).</summary>
        public Renderer SurfaceRenderer => surfaceRenderer;

        /// <summary>
        /// Builds a view for <paramref name="t"/>: a root at the opening's pose under its
        /// PortalSurfaceRoot (so it dies with the threshold), the surface and the camera.
        /// </summary>
        internal static PortalView Create(Threshold t)
        {
            int layer = LayerMask.NameToLayer("Portal");
            if (layer < 0) layer = 0;

            var go = new GameObject("PortalView") { layer = layer };
            go.transform.SetParent(t.PortalSurfaceRoot, false);
            go.transform.SetPositionAndRotation(t.transform.position, t.transform.rotation);
            var view = go.AddComponent<PortalView>();
            view.Build(t, layer);
            return view;
        }

        /// <summary>Creates the surface (quad at local z = 0) and the disabled portal camera.</summary>
        void Build(Threshold t, int layer)
        {
            threshold = t;
            block = new MaterialPropertyBlock();

            var surfaceGo = new GameObject("Surface") { layer = layer };
            surface = surfaceGo.transform;
            surface.SetParent(transform, false);
            surfaceFilter = surfaceGo.AddComponent<MeshFilter>();
            surfaceFilter.sharedMesh = PortalSystem.QuadMesh;
            surfaceRenderer = surfaceGo.AddComponent<MeshRenderer>();
            surfaceRenderer.sharedMaterial = PortalSystem.SurfaceMaterial;
            surfaceRenderer.shadowCastingMode = ShadowCastingMode.Off;
            surfaceRenderer.receiveShadows = false;
            surfaceRenderer.lightProbeUsage = LightProbeUsage.Off;
            surfaceRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            ApplySurfaceShape(ConfigLookup.Get<PortalConfig>());
            ApplyBlock(null);

            // The camera is a child so it is destroyed with the threshold, but it never inherits a
            // pose: it is placed in world space every frame. DontSave keeps it out of any save.
            var camGo = new GameObject("PortalCamera") { hideFlags = HideFlags.DontSave };
            camGo.transform.SetParent(transform, false);
            viewCamera = camGo.AddComponent<Camera>();
            viewCamera.enabled = false;
            viewCamera.useOcclusionCulling = false; // occlusion data is baked from real viewpoints; the virtual camera often sits inside walls
            viewCamera.allowHDR = true;
            viewCamera.allowMSAA = false;
            viewCamera.allowDynamicResolution = false;

            var data = viewCamera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.renderPostProcessing = false; // the main camera grades the whole frame once, portal content included
            data.antialiasing = AntialiasingMode.None;
            data.renderShadows = true;
        }

        /// <summary>
        /// Starts or stops this view's rendering for the frame. A rendering view holds a render
        /// texture from the shared pool; a stopped one gives it back and fills its surface with the
        /// fog colour (it is far, beyond MaxActive, or off screen), so a stale frame never shows.
        /// </summary>
        internal void SetRendering(bool on, Camera main)
        {
            if (viewCamera == null) return;
            if (on && main != null)
            {
                if (!PortalSystem.TargetFits(target))
                {
                    DetachTarget();
                    target = PortalSystem.AcquireTarget();
                }
                viewCamera.targetTexture = target;
                viewCamera.depth = main.depth - 1f; // URP sorts by depth: the texture is ready before the main camera samples it
                if (!viewCamera.enabled)
                {
                    Place(main); // a sane pose now; beginContextRendering refines it after Cinemachine
                    viewCamera.enabled = true;
                }
                ApplyBlock(target);
            }
            else
            {
                viewCamera.enabled = false;
                DetachTarget();
                ApplyBlock(null);
            }
        }

        /// <summary>
        /// Puts the virtual camera where the main camera would be if the partner's side lay behind
        /// this opening (PairMap * main), with the same lens, and clips everything between it and
        /// the partner's opening with an oblique near plane.
        /// </summary>
        internal void Place(Camera main)
        {
            var partner = threshold != null ? threshold.Partner : null;
            if (viewCamera == null || partner == null || main == null) return;

            var map = threshold.PairMap;
            var eye = main.transform;
            Vector3 position = map.MultiplyPoint3x4(eye.position);
            viewCamera.transform.SetPositionAndRotation(position, map.rotation * eye.rotation);

            // FOV changes every frame with speed, so the lens is copied every frame.
            viewCamera.fieldOfView = main.fieldOfView;
            viewCamera.aspect = main.aspect;
            viewCamera.nearClipPlane = main.nearClipPlane;
            viewCamera.farClipPlane = main.farClipPlane;
            viewCamera.clearFlags = main.clearFlags;
            viewCamera.backgroundColor = main.backgroundColor;
            viewCamera.cullingMask = main.cullingMask & ~ExcludedLayers;

            viewCamera.ResetProjectionMatrix();
            viewCamera.ResetCullingMatrix();
            Matrix4x4 plain = viewCamera.projectionMatrix;

            // Keep what lies beyond the opening as seen from the side the main camera came from,
            // mapped to the partner. Using the remembered side (not the live one) keeps the clip
            // right during a crossing, before the teleport has happened.
            int side = approachSide != 0 ? approachSide : SideOf(eye.position);
            Vector3 normal = map.MultiplyVector(-side * threshold.transform.forward).normalized;
            Vector3 planePoint = partner.transform.position;

            // Only clip when the plane is clearly in front of the virtual camera. Closer than the
            // near plane, an oblique matrix degenerates; the normal near clip already hides that sliver.
            if (Vector3.Dot(normal, planePoint - position) > viewCamera.nearClipPlane)
            {
                Matrix4x4 worldToCamera = viewCamera.worldToCameraMatrix;
                Vector3 n = worldToCamera.MultiplyVector(normal);
                Vector3 p = worldToCamera.MultiplyPoint3x4(planePoint);
                viewCamera.projectionMatrix = viewCamera.CalculateObliqueMatrix(new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, p)));
                // An oblique matrix bends the far plane; cull with the plain frustum so nothing far vanishes.
                viewCamera.cullingMatrix = plain * worldToCamera;
            }
        }

        /// <summary>
        /// Near-plane fix: within NearExtrude of the plane (and over the opening), the flat quad
        /// becomes a box reaching NearExtrude behind the plane, on the far side from where the camera
        /// came, so the 0.1 m near clip never shows the real room for a frame during a crossing.
        /// </summary>
        internal void UpdateSurface(Vector3 eye, PortalConfig config)
        {
            if (threshold == null) return;
            Vector3 local = Quaternion.Inverse(threshold.transform.rotation) * (eye - threshold.transform.position);
            Vector2 half = threshold.Size * 0.5f;
            float band = config.NearExtrude;
            bool near = band > 0f
                        && Mathf.Abs(local.z) < band
                        && Mathf.Abs(local.x) < half.x + band
                        && Mathf.Abs(local.y) < half.y + band;

            // Outside the band the side is simply where the camera is. Inside it, keep the side the
            // camera entered from, so stepping through the plane doesn't flip the box onto the camera.
            if (!near || approachSide == 0) approachSide = local.z >= 0f ? 1 : -1;

            if (near != extruded)
            {
                extruded = near;
                surfaceFilter.sharedMesh = extruded ? PortalSystem.BoxMesh : PortalSystem.QuadMesh;
            }
            ApplySurfaceShape(config);
        }

        /// <summary>After a teleport the side is re-read from the camera's new position.</summary>
        internal void ForgetApproachSide() => approachSide = 0;

        /// <summary>Stops rendering and frees the render texture (the GameObject may live on briefly).</summary>
        internal void Shutdown()
        {
            if (viewCamera != null) viewCamera.enabled = false;
            DetachTarget();
        }

        /// <summary>The pair is gone: hide the surface at once and destroy the view at the end of the frame.</summary>
        internal void Dispose()
        {
            Shutdown();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            // Covers scene unloads and play-mode exit, where the threshold takes this view with it.
            DetachTarget();
            if (viewCamera != null) Destroy(viewCamera.gameObject);
        }

        static int ExcludedLayers
        {
            get
            {
                int mask = 0;
                int portal = LayerMask.NameToLayer("Portal");
                int viewModel = LayerMask.NameToLayer("ViewModel");
                if (portal >= 0) mask |= 1 << portal;
                if (viewModel >= 0) mask |= 1 << viewModel;
                return mask;
            }
        }

        int SideOf(Vector3 point) =>
            Vector3.Dot(point - threshold.transform.position, threshold.transform.forward) >= 0f ? 1 : -1;

        /// <summary>Sizes the surface to the opening (plus margin) in world metres, whatever the parents' scale.</summary>
        void ApplySurfaceShape(PortalConfig config)
        {
            if (threshold == null || surface == null) return;
            float margin = config.SurfaceMargin * 2f;
            var world = new Vector3(threshold.Size.x + margin, threshold.Size.y + margin, 1f);
            // The box mesh spans z 0..1; a negative depth puts it behind the plane as seen from the camera's side.
            if (extruded) world.z = -approachSide * Mathf.Max(0.01f, config.NearExtrude);

            Vector3 parent = transform.lossyScale;
            var local = new Vector3(
                world.x / NonZero(parent.x),
                world.y / NonZero(parent.y),
                world.z / NonZero(parent.z));
            if (surface.localScale != local) surface.localScale = local;
        }

        static float NonZero(float v) => Mathf.Abs(v) < 1e-5f ? 1f : v;

        /// <summary>Gives the render texture back to the pool, unhooking it from the camera first.</summary>
        void DetachTarget()
        {
            if (viewCamera != null) viewCamera.targetTexture = null;
            if (target == null) return;
            PortalSystem.ReleaseTarget(target);
            target = null;
        }

        /// <summary>Points the surface at <paramref name="texture"/>, or at a flat fog-coloured fill when there is none.</summary>
        void ApplyBlock(RenderTexture texture)
        {
            if (surfaceRenderer == null) return;
            surfaceRenderer.GetPropertyBlock(block);
            block.SetTexture(PortalTexId, texture != null ? texture : Texture2D.blackTexture);
            block.SetColor(FillColorId, RenderSettings.fogColor);
            block.SetFloat(FillAmountId, texture != null ? 0f : 1f);
            surfaceRenderer.SetPropertyBlock(block);
        }
    }
}
