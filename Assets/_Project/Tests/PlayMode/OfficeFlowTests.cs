using System.Collections;
using Kit.Core.Events;
using Kit.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// OOO's game flow through the real Boot path (GDD F12, F14): the title menu, then play with
    /// nothing on screen, pause and resume, restart, and an ending that never becomes a game over.
    /// </summary>
    public class OfficeFlowTests
    {
        static CanvasGroup Panel(string name) => GameObject.Find("UI_Canvas/" + name)?.GetComponent<CanvasGroup>();

        static IEnumerator BootToMenu()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            var gm = Object.FindAnyObjectByType<GameManager>();
            float until = Time.realtimeSinceStartup + 20f;
            while (gm.State == GameState.Loading && Time.realtimeSinceStartup < until) yield return null;
        }

        static IEnumerator Wait(float seconds)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        [UnityTest]
        public IEnumerator Boot_PlaysTheOffice_WithNoHud_AndNoGameOver()
        {
            yield return BootToMenu();
            var gm = Object.FindAnyObjectByType<GameManager>();
            Assert.AreEqual(GameState.MainMenu, gm.State);
            Assert.AreEqual("Office", gm.Config.PlayScene);
            Assert.IsTrue(SceneManager.GetSceneByName("Office").isLoaded, "Boot loads the Office scene.");
            Assert.AreEqual("OOO", GameObject.Find("UI_Canvas/MainMenu/Title").GetComponent<UnityEngine.UI.Text>().text);

            EventBus.Publish(new PlayRequested());
            yield return Wait(0.5f);
            Assert.AreEqual(GameState.Playing, gm.State);
            foreach (var name in new[] { "MainMenu", "PauseMenu", "GameOver", "HUD", "Ending" })
                Assert.AreEqual(0f, Panel(name).alpha, 0.01f, $"{name} must be hidden during play (no HUD, nothing explained).");

            EventBus.Publish(new PauseToggleRequested());
            yield return Wait(0.4f);
            Assert.AreEqual(GameState.Paused, gm.State);
            Assert.AreEqual(1f, Panel("PauseMenu").alpha, 0.01f);
            EventBus.Publish(new PauseToggleRequested());
            yield return Wait(0.4f);
            Assert.AreEqual(GameState.Playing, gm.State);
            Assert.AreEqual(0f, Panel("HUD").alpha, 0.01f, "Resuming must not bring a HUD back.");

            EventBus.Publish(new GameCompleted());
            yield return Wait(3.5f);
            Assert.AreEqual(GameState.Playing, gm.State, "The ending is not a game over: play continues.");
            Assert.IsTrue(gm.Completed);
            Assert.Greater(Panel("Ending").alpha, 0.9f, "The OOO card fades in.");
            Assert.AreEqual(0f, Panel("GameOver").alpha, 0.01f);

            gm.Restart();
            float until = Time.realtimeSinceStartup + 20f;
            while (gm.State != GameState.Playing && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(GameState.Playing, gm.State, "Restart starts a new game.");
            Assert.IsFalse(gm.Completed);
            Assert.IsNotNull(gm.Player);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var gm = Object.FindAnyObjectByType<GameManager>();
            float until = Time.realtimeSinceStartup + 20f;
            while (gm != null && gm.State == GameState.Loading && Time.realtimeSinceStartup < until) yield return null;
            Time.timeScale = 1f;
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
