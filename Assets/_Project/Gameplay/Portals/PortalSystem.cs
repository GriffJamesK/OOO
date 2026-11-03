using System.Collections.Generic;
using Kit.Core;
using Kit.Gameplay.Thresholds;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Kit.Gameplay.Portals
{
    /// <summary>
    /// Self-installing: keeps one PortalView per paired threshold (rebuilt on ThresholdsRepaired)
    /// and, before each frame renders, places and renders the views the main camera can see.
    ///
    /// Rendering choice: portal cameras stay ENABLED (with a target texture and a depth below the
    /// main camera) while they are selected, so URP draws them in its own camera loop just before
    /// the main camera. Selection runs in LateUpdate (a camera enabled during a render callback
    /// would miss this frame's camera list); placement runs in beginContextRendering, after
    /// Cinemachine has moved the main camera and before any camera is culled. SubmitRenderRequest
    /// was rejected because it can't be called from inside a render callback (recursive rendering)
    /// and from LateUpdate it would race Cinemachine's own LateUpdate.
    /// </summary>
    public static class PortalSystem
    {
        static readonly List<PortalView> views = new List<PortalView>();
        static readonly List<PortalView> candidates = new List<PortalView>();
        static readonly HashSet<Threshold> covered = new HashSet<Threshold>();
        static readonly List<RenderTexture> freeTargets = new List<RenderTexture>();
        static readonly Plane[] frustum = new Plane[6];

        static int activeCount;
        static bool dirty = true;
        static bool hostAlive;
        static Camera mainCamera;
        static Material fallbackMaterial;
        static Mesh quadMesh;
        static Mesh boxMesh;
        static int targetWidth, targetHeight;

        /// <summary>Domain reload is off: forget everything from the last play session.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            views.Clear();
            candidates.Clear();
            covered.Clear();
            freeTargets.Clear();
            activeCount = 0;
            dirty = true;
            hostAlive = false;
            mainCamera = null;
            fallbackMaterial = null;
            quadMesh = null;
            boxMesh = null;
            targetWidth = targetHeight = 0;
        }

        /// <summary>Puts the runner on the shared Kit host; it forwards LateUpdate and the render hook here.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            KitHost.Ensure<PortalRunner>();
        }

        /// <summary>Number of portal views currently rendering (for tests and PortalDebug).</summary>
        public static int ActiveViewCount => activeCount;

        /// <summary>Every live view (one per paired threshold), for PortalDebug.</summary>
        public static IReadOnlyList<PortalView> Views => views;

        /// <summary>The camera views are placed against this frame (Camera.main at selection time).</summary>
        public static Camera MainCamera => mainCamera;

        // ---- Runner hooks -------------------------------------------------------------------

        internal static void OnHostEnabled() { hostAlive = true; dirty = true; }

        /// <summary>A pair changed somewhere: rescan the registry at the next LateUpdate (never mid-teardown).</summary>
        internal static void MarkDirty() => dirty = true;

        /// <summary>
        /// After a teleport the camera arrives right beside the exit opening; each view must take
        /// its side from where the camera is now, not from where it was before the jump.
        /// </summary>
        internal static void ForgetApproachSides()
        {
            foreach (var v in views) if (v != null) v.ForgetApproachSide();
        }

        /// <summary>
        /// Once per frame, late: matches views to the current pairs, then picks which views render
        /// (paired, near, in the main camera's frustum, nearest first, at most MaxActive) and
        /// enables only their cameras, so URP includes exactly those in this frame's camera list.
        /// </summary>
        internal static void Tick()
        {
            if (dirty) Rescan();
            else DropStale();

            var config = ConfigLookup.Get<PortalConfig>();
            mainCamera = Camera.main;
            candidates.Clear();

            foreach (var v in views) v.Distance = float.PositiveInfinity;
            if (mainCamera != null && mainCamera.isActiveAndEnabled && config.MaxActive > 0)
            {
                ResizeTargets(mainCamera, config.ResolutionScale);
                Vector3 eye = mainCamera.transform.position;
                float maxSqr = config.MaxDistance * config.MaxDistance;
                GeometryUtility.CalculateFrustumPlanes(mainCamera, frustum);

                foreach (var v in views)
                {
                    var bounds = v.SurfaceBounds;
                    float sqr = bounds.SqrDistance(eye);
                    v.Distance = Mathf.Sqrt(sqr);
                    if (sqr > maxSqr) continue;
                    if (!GeometryUtility.TestPlanesAABB(frustum, bounds)) continue;
                    candidates.Add(v);
                }
                candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                if (candidates.Count > config.MaxActive) candidates.RemoveRange(config.MaxActive, candidates.Count - config.MaxActive);
            }

            activeCount = 0;
            foreach (var v in views)
            {
                bool on = candidates.Contains(v);
                v.SetRendering(on, mainCamera);
                if (on) activeCount++;
            }
        }

        /// <summary>
        /// Called at the start of each frame's rendering: by now Cinemachine has placed the main
        /// camera, so the virtual cameras and the near-plane boxes follow it exactly this frame.
        /// </summary>
        internal static void PlaceViews()
        {
            var cam = mainCamera;
            if (cam == null) return;
            var config = ConfigLookup.Get<PortalConfig>();
            foreach (var v in views)
            {
                if (v == null) continue;
                v.UpdateSurface(cam.transform.position, config);
                if (v.Rendering) v.Place(cam);
            }
        }

        /// <summary>Play mode is ending (or the host went away): free every view, texture and shared asset now.</summary>
        internal static void OnHostDestroyed()
        {
            hostAlive = false;
            foreach (var v in views) if (v != null) v.Shutdown();
            views.Clear();
            candidates.Clear();
            foreach (var rt in freeTargets) DestroyTarget(rt);
            freeTargets.Clear();
            DestroyAsset(fallbackMaterial);
            DestroyAsset(quadMesh);
            DestroyAsset(boxMesh);
            fallbackMaterial = null;
            quadMesh = boxMesh = null;
            activeCount = 0;
            mainCamera = null;
        }

        // ---- Views ----------------------------------------------------------------------------

        static bool WantsView(Threshold t) => t != null && t.isActiveAndEnabled && t.IsPaired && t.Partner != null;

        /// <summary>Full match against the registry: drop views that lost their pair, add views for new pairs.</summary>
        static void Rescan()
        {
            dirty = false;
            DropStale();
            covered.Clear();
            foreach (var v in views) covered.Add(v.Threshold);
            foreach (var t in ThresholdRegistry.All)
            {
                if (!WantsView(t) || covered.Contains(t)) continue;
                views.Add(PortalView.Create(t));
                covered.Add(t);
            }
        }

        /// <summary>
        /// Cheap per-frame pass. Views die with their threshold when a scene unloads (they are its
        /// children), and an unpaired threshold must lose its surface even if no event reached us.
        /// </summary>
        static void DropStale()
        {
            for (int i = views.Count - 1; i >= 0; i--)
            {
                var v = views[i];
                if (v != null && WantsView(v.Threshold)) continue;
                if (v != null) v.Dispose();
                views.RemoveAt(i);
            }
        }

        // ---- Shared resources -------------------------------------------------------------------

        /// <summary>The configured surface material, or one made from Kit/Portal when no asset is assigned (tests).</summary>
        internal static Material SurfaceMaterial
        {
            get
            {
                var configured = ConfigLookup.Get<PortalConfig>().SurfaceMaterial;
                if (configured != null) return configured;
                if (fallbackMaterial == null)
                {
                    var shader = Shader.Find("Kit/Portal");
                    if (shader != null) fallbackMaterial = new Material(shader) { name = "Mat_Portal (runtime)", hideFlags = HideFlags.DontSave };
                }
                return fallbackMaterial;
            }
        }

        /// <summary>Unit quad in XY centred on the origin (z = 0): the surface seen from a distance.</summary>
        internal static Mesh QuadMesh
        {
            get
            {
                if (quadMesh != null) return quadMesh;
                quadMesh = new Mesh { name = "PortalQuad", hideFlags = HideFlags.DontSave };
                quadMesh.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
                };
                quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                quadMesh.RecalculateNormals();
                quadMesh.RecalculateBounds();
                return quadMesh;
            }
        }

        /// <summary>
        /// Unit box spanning x, y in [-0.5, 0.5] and z in [0, 1]: the surface while the camera is
        /// within NearExtrude of the plane. Scaled along z (negative = behind the plane).
        /// </summary>
        internal static Mesh BoxMesh
        {
            get
            {
                if (boxMesh != null) return boxMesh;
                boxMesh = new Mesh { name = "PortalBox", hideFlags = HideFlags.DontSave };
                boxMesh.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(-0.5f, -0.5f, 1f), new Vector3(0.5f, -0.5f, 1f), new Vector3(0.5f, 0.5f, 1f), new Vector3(-0.5f, 0.5f, 1f)
                };
                // The shader draws both sides (Cull Off), so winding only needs to be consistent.
                boxMesh.triangles = new[]
                {
                    0, 2, 1, 0, 3, 2, // z = 0 (the opening plane)
                    4, 5, 6, 4, 6, 7, // z = 1 (far end)
                    0, 1, 5, 0, 5, 4, // bottom
                    3, 6, 2, 3, 7, 6, // top
                    0, 4, 7, 0, 7, 3, // left
                    1, 2, 6, 1, 6, 5  // right
                };
                boxMesh.RecalculateNormals();
                boxMesh.RecalculateBounds();
                return boxMesh;
            }
        }

        /// <summary>
        /// Render textures follow the main camera's pixel size (times ResolutionScale) so the
        /// surface samples them 1:1 in screen space. A size change drops the spare textures; views
        /// holding an old size swap theirs when they next render.
        /// </summary>
        static void ResizeTargets(Camera cam, float scale)
        {
            int w = Mathf.Max(16, Mathf.RoundToInt(cam.pixelWidth * scale));
            int h = Mathf.Max(16, Mathf.RoundToInt(cam.pixelHeight * scale));
            if (w == targetWidth && h == targetHeight) return;
            targetWidth = w;
            targetHeight = h;
            foreach (var rt in freeTargets) DestroyTarget(rt);
            freeTargets.Clear();
        }

        /// <summary>True if <paramref name="rt"/> matches the current screen-derived size.</summary>
        internal static bool TargetFits(RenderTexture rt) => rt != null && rt.width == targetWidth && rt.height == targetHeight;

        /// <summary>
        /// A render texture for a view that starts rendering. Only rendering views hold one (at
        /// most MaxActive), so dozens of paired doors never cost dozens of full-screen HDR buffers.
        /// </summary>
        internal static RenderTexture AcquireTarget()
        {
            for (int i = freeTargets.Count - 1; i >= 0; i--)
            {
                var rt = freeTargets[i];
                freeTargets.RemoveAt(i);
                if (TargetFits(rt)) return rt;
                DestroyTarget(rt);
            }

            var desc = new RenderTextureDescriptor(targetWidth, targetHeight, GraphicsFormat.R16G16B16A16_SFloat,
                SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil))
            {
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            var created = new RenderTexture(desc)
            {
                name = "PortalView RT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            created.Create();
            return created;
        }

        /// <summary>Returns a texture to the spare list, or frees it when it is surplus, stale, or play mode is ending.</summary>
        internal static void ReleaseTarget(RenderTexture rt)
        {
            if (rt == null) return;
            int keep = ConfigLookup.Get<PortalConfig>().MaxActive;
            if (hostAlive && TargetFits(rt) && freeTargets.Count < keep && !freeTargets.Contains(rt)) freeTargets.Add(rt);
            else DestroyTarget(rt);
        }

        static void DestroyTarget(RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            DestroyAsset(rt);
        }

        static void DestroyAsset(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }
    }
}
