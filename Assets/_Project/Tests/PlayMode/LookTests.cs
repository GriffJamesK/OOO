using System.Collections;
using System.Collections.Generic;
using Kit.Gameplay.Look;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// Per-area fog: LookController reads the blended volume stack at the main camera, so a local
    /// box Volume with a KitFog override (what OfficeKit.FogZone builds) takes over the scene fog
    /// while the camera is inside it, and the global profile's fog returns outside it. Also checks
    /// the light shaft builds a bounded mesh and that a flickering light always comes back.
    /// </summary>
    public class LookTests
    {
        // Far from any course or office geometry, so leftover scene volumes can't interfere.
        static readonly Vector3 ZoneCentre = new Vector3(900f, 400f, 900f);
        static readonly Color GlobalColour = new Color(0.165f, 0.196f, 0.235f);
        static readonly Color LocalColour = new Color(0.80f, 0.82f, 0.84f);

        readonly List<Object> spawned = new List<Object>();
        readonly List<Camera> silencedCameras = new List<Camera>();
        bool savedFog;
        FogMode savedMode;
        float savedStart, savedEnd;
        Color savedColour;

        [SetUp]
        public void SetUp()
        {
            savedFog = RenderSettings.fog;
            savedMode = RenderSettings.fogMode;
            savedStart = RenderSettings.fogStartDistance;
            savedEnd = RenderSettings.fogEndDistance;
            savedColour = RenderSettings.fogColor;

            // Camera.main must be our test camera: switch off any other enabled main camera.
            foreach (var cam in Object.FindObjectsByType<Camera>())
                if (cam.enabled && cam.CompareTag("MainCamera")) { cam.enabled = false; silencedCameras.Add(cam); }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.Destroy(o);
            spawned.Clear();
            foreach (var cam in silencedCameras) if (cam != null) cam.enabled = true;
            silencedCameras.Clear();

            RenderSettings.fog = savedFog;
            RenderSettings.fogMode = savedMode;
            RenderSettings.fogStartDistance = savedStart;
            RenderSettings.fogEndDistance = savedEnd;
            RenderSettings.fogColor = savedColour;
        }

        VolumeProfile FogProfile(float start, float end, Color colour)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var fog = profile.Add<KitFog>(true);
            fog.enabled.value = true;
            fog.start.value = start;
            fog.end.value = end;
            fog.color.value = colour;
            spawned.Add(fog);
            spawned.Add(profile);
            return profile;
        }

        [UnityTest]
        public IEnumerator LocalFogVolume_TakesOverInside_AndGlobalFogReturnsOutside()
        {
            // Global look volume with the office fog, and the LookController that applies it.
            var global = new GameObject("TestGlobalVolume");
            spawned.Add(global);
            var globalVolume = global.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 100f; // above any leftover scene volume
            globalVolume.sharedProfile = FogProfile(8f, 70f, GlobalColour);
            global.AddComponent<LookController>();

            // Local box volume (a fog zone): trigger collider first so the Volume caches it.
            var local = new GameObject("TestFogZone");
            spawned.Add(local);
            local.transform.position = ZoneCentre;
            var box = local.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(10f, 10f, 10f);
            var localVolume = local.AddComponent<Volume>();
            localVolume.isGlobal = false;
            localVolume.blendDistance = 0f;
            localVolume.priority = 200f;
            localVolume.sharedProfile = FogProfile(20f, 200f, LocalColour);
            localVolume.UpdateColliders();

            var camGo = new GameObject("TestMainCamera");
            spawned.Add(camGo);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = ZoneCentre;

            yield return null;
            yield return null;
            Assert.AreEqual(cam, Camera.main, "The test camera should be the main camera.");
            Assert.IsTrue(RenderSettings.fog);
            Assert.AreEqual(FogMode.Linear, RenderSettings.fogMode);
            Assert.AreEqual(20f, RenderSettings.fogStartDistance, 0.01f, "Inside the zone the local fog wins. " + LookDebug.Report());
            Assert.AreEqual(200f, RenderSettings.fogEndDistance, 0.01f, LookDebug.Report());
            AssertColour(LocalColour, RenderSettings.fogColor);
            Assert.AreEqual(1, LookDebug.LocalFogVolumesAtCamera());

            camGo.transform.position = ZoneCentre + new Vector3(0f, 0f, 30f); // well outside the box
            yield return null;
            yield return null;
            Assert.AreEqual(8f, RenderSettings.fogStartDistance, 0.01f, "Outside the zone the global fog returns. " + LookDebug.Report());
            Assert.AreEqual(70f, RenderSettings.fogEndDistance, 0.01f, LookDebug.Report());
            AssertColour(GlobalColour, RenderSettings.fogColor);
            Assert.AreEqual(0, LookDebug.LocalFogVolumesAtCamera());

            // Back in: the switch works both ways, every frame.
            camGo.transform.position = ZoneCentre + new Vector3(2f, -1f, 3f);
            yield return null;
            yield return null;
            Assert.AreEqual(200f, RenderSettings.fogEndDistance, 0.01f, LookDebug.Report());
        }

        [UnityTest]
        public IEnumerator FogZone_BlendsAcrossItsBlendDistance()
        {
            var global = new GameObject("TestGlobalVolume");
            spawned.Add(global);
            var globalVolume = global.AddComponent<Volume>();
            globalVolume.isGlobal = true;
            globalVolume.priority = 100f;
            globalVolume.sharedProfile = FogProfile(8f, 70f, GlobalColour);
            global.AddComponent<LookController>();

            var local = new GameObject("TestFogZone");
            spawned.Add(local);
            local.transform.position = ZoneCentre;
            var box = local.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(10f, 10f, 10f);
            var localVolume = local.AddComponent<Volume>();
            localVolume.isGlobal = false;
            localVolume.blendDistance = 4f;
            localVolume.priority = 200f;
            localVolume.sharedProfile = FogProfile(20f, 200f, LocalColour);
            localVolume.UpdateColliders();

            var camGo = new GameObject("TestMainCamera");
            spawned.Add(camGo);
            camGo.tag = "MainCamera";
            camGo.AddComponent<Camera>();
            // 2 m outside the face of the box: influence = 1 - (2/4)^2 = 0.75.
            camGo.transform.position = ZoneCentre + new Vector3(7f, 0f, 0f);

            yield return null;
            yield return null;
            Assert.AreEqual(Mathf.Lerp(70f, 200f, 0.75f), RenderSettings.fogEndDistance, 0.5f,
                "Fog end should be part-way between the global and local values near the zone. " + LookDebug.Report());
        }

        [UnityTest]
        public IEnumerator LightShaft_BuildsABoundedFrustum_WithNoCollider()
        {
            var go = new GameObject("TestShaft");
            spawned.Add(go);
            go.transform.position = ZoneCentre;
            var shaft = go.AddComponent<LightShaft>();
            shaft.Setup(new Vector3(3f, 12f, 2f), new Color(0.8f, 0.86f, 0.95f, 0.25f));
            yield return null;

            var filter = go.GetComponent<MeshFilter>();
            Assert.IsNotNull(filter, "The shaft builds its own MeshFilter.");
            Assert.IsNotNull(filter.sharedMesh);
            spawned.Add(filter.sharedMesh);
            var bounds = filter.sharedMesh.bounds;
            Assert.AreEqual(0f, bounds.max.y, 0.001f, "The shaft hangs from its transform.");
            Assert.AreEqual(-12f, bounds.min.y, 0.001f, "The shaft is size.y long.");
            Assert.GreaterOrEqual(bounds.size.x, 3f - 0.001f, "At least size.x wide (wider at the bottom).");
            Assert.GreaterOrEqual(bounds.size.z, 2f - 0.001f);
            Assert.IsNull(go.GetComponent<Collider>(), "Light is not solid.");
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, go.GetComponent<MeshRenderer>().shadowCastingMode);
        }

        [UnityTest]
        public IEnumerator FlickerLight_DimsBriefly_ThenReturnsToFullIntensity()
        {
            var go = new GameObject("TestFlicker");
            spawned.Add(go);
            var light = go.AddComponent<Light>();
            light.intensity = 2f;
            var flicker = go.AddComponent<FlickerLight>();
            yield return null;

            flicker.FlickerNow();
            float lowest = light.intensity;
            float until = Time.time + 2f; // a burst lasts at most FlickerMaxDuration (0.6 s by default)
            while (flicker.Flickering && Time.time < until)
            {
                yield return null;
                lowest = Mathf.Min(lowest, light.intensity);
                Assert.LessOrEqual(light.intensity, 2f + 1e-4f, "A flicker only ever dims.");
            }
            Assert.IsFalse(flicker.Flickering, "A flicker burst is brief.");
            Assert.Less(lowest, 2f, "The light visibly dipped during the flicker.");
            Assert.AreEqual(2f, light.intensity, 1e-4f, "After a flicker the light returns to full intensity.");
        }

        static void AssertColour(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f, $"fog colour r: expected {expected}, got {actual}");
            Assert.AreEqual(expected.g, actual.g, 0.01f, $"fog colour g: expected {expected}, got {actual}");
            Assert.AreEqual(expected.b, actual.b, 0.01f, $"fog colour b: expected {expected}, got {actual}");
        }
    }
}
