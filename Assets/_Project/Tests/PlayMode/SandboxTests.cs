using System.Collections;
using Kit.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>Smoke test for the real boot path: Boot loads, its systems start, nothing logs an error.</summary>
    public class SandboxTests
    {
        [UnityTest]
        public IEnumerator SandboxBootsCleanly()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            yield return null;

            // Any Debug.LogError or exception fails the test; this also asserts nothing unexpected was logged.
            LogAssert.NoUnexpectedReceived();
            Assert.IsNotNull(Object.FindAnyObjectByType<GameManager>(), "Boot should contain a GameManager.");
        }

        [UnityTest]
        public IEnumerator BootDefaultsToFirstPerson_WithProfileFog()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            var gm = Object.FindAnyObjectByType<GameManager>();
            float timeout = Time.realtimeSinceStartup + 10f;
            while (gm.State == GameState.Loading && Time.realtimeSinceStartup < timeout) yield return null;
            yield return null; // let LookController apply fog in LateUpdate

            Assert.AreEqual(CameraRigMode.FirstPerson, Object.FindAnyObjectByType<CameraRig>().Mode);
            Assert.IsTrue(gm.Player.FirstPersonView, "FPS rig should switch the player to first-person view.");
            Assert.AreEqual(0f, GameObject.Find("UI_Canvas/HUD").GetComponent<CanvasGroup>().alpha, "HUD starts hidden.");

            // Fog comes from the play scene's Volume profile (KitFog), applied by LookController.
            var look = Object.FindAnyObjectByType<Kit.Gameplay.Look.LookController>();
            Assert.IsNotNull(look);
            var profile = look.GetComponent<UnityEngine.Rendering.Volume>().sharedProfile;
            Assert.IsTrue(profile.TryGet(out Kit.Gameplay.Look.KitFog fog), "The play scene's look profile carries KitFog.");
            Assert.AreEqual(fog.end.value, RenderSettings.fogEndDistance, 0.01f);
        }

        [UnityTest]
        public IEnumerator FallingOutOfBounds_RespawnsAtSpawn()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            var gm = Object.FindAnyObjectByType<GameManager>();
            float timeout = Time.realtimeSinceStartup + 10f;
            while (gm.State == GameState.Loading && Time.realtimeSinceStartup < timeout) yield return null;
            Kit.Core.Events.EventBus.Publish(new Kit.Core.Events.PlayRequested());
            yield return null;

            var spawn = gm.Player.transform.position;
            PlayerDebug.Teleport(new Vector3(30f, gm.Config.KillHeight - 5f, 30f));
            yield return null;
            yield return null;

            Assert.Less(Vector3.Distance(gm.Player.transform.position, spawn), 1f,
                "Falling below KillHeight should put the player back at the spawn point. " + PlayerDebug.Report());
            Assert.Less(gm.Player.HorizontalSpeed, 0.01f, "Respawn clears momentum.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Let the additive Sandbox load finish, then leave an empty scene for the next test.
            float timeout = Time.realtimeSinceStartup + 10f;
            var gm = Object.FindAnyObjectByType<GameManager>();
            while (gm != null && gm.State == GameState.Loading && Time.realtimeSinceStartup < timeout) yield return null;

            var empty = SceneManager.CreateScene("TestEmpty");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s != empty && s.isLoaded) yield return SceneManager.UnloadSceneAsync(s);
            }
        }
    }
}
