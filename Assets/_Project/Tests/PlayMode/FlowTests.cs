using System.Collections;
using System.Collections.Generic;
using Kit.Core.Events;
using Kit.Gameplay;
using Kit.Gameplay.Flow;
using Kit.Gameplay.Thresholds;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// No fail state (GDD F8, F9): a fall puts you back on the last solid ground you stood on, and
    /// walking out of the 000 door ends the game with a title card but no game over.
    /// </summary>
    public class FlowTests
    {
        readonly List<Object> spawned = new List<Object>();

        PlayerController MakePlayer(Vector3 position)
        {
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            var go = new GameObject("TestPlayer");
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            var pc = go.AddComponent<PlayerController>();
            pc.Config = config;
            spawned.Add(go);
            spawned.Add(config);
            EventBus.Publish(new PlayerSpawned(go.transform, go.transform));
            return pc;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.Destroy(o);
            spawned.Clear();
        }

        [UnityTest]
        public IEnumerator FallingOffTheWorld_ReturnsToTheLastSolidGround()
        {
            var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.transform.position = new Vector3(0f, -0.5f, 0f);
            ledge.transform.localScale = new Vector3(4f, 1f, 4f);
            spawned.Add(ledge);
            var fall = new GameObject("FallZone");
            fall.transform.position = new Vector3(0f, -30f, 0f);
            fall.AddComponent<FallZone>().Setup(new Vector3(400f, 10f, 400f));
            spawned.Add(fall);
            Physics.SyncTransforms();

            var player = MakePlayer(new Vector3(0.5f, 0.05f, 0.5f));
            float until = Time.time + 2f;
            while (!player.IsGrounded && Time.time < until) yield return null;
            yield return new WaitForSeconds(0.6f); // longer than SafeGroundDwell
            var safe = player.transform.position;

            bool respawned = false;
            System.Action<PlayerRespawned> onRespawn = _ => respawned = true;
            EventBus.Subscribe(onRespawn);
            player.SetVelocity(new Vector3(8f, 0f, 0f)); // airborne off the ledge next frame
            yield return null;
            player.Respawn(new Pose(new Vector3(20f, -5f, 0f), Quaternion.identity)); // over the void
            until = Time.time + 5f;
            while (!respawned && Time.time < until) yield return null;
            EventBus.Unsubscribe(onRespawn);

            Assert.IsTrue(respawned, "Entering a FallZone should respawn the player. " + FlowDebug.Report());
            yield return new WaitForSeconds(0.1f);
            Assert.Less(Vector3.Distance(player.transform.position, safe), 0.6f, $"Back at the ledge ({safe}), not at {player.transform.position}");
            Assert.Less(player.HorizontalSpeed, 0.01f, "Momentum is cleared.");
        }

        [UnityTest]
        public IEnumerator WalkingOutOfThe000Door_CompletesTheGame()
        {
            var cfg = ConfigLookup.Get<FlowConfig>();
            float delay = cfg.EndingTitleDelay;
            cfg.EndingTitleDelay = 0.2f;
            try
            {
                Threshold Make(string name, Vector3 centre, Vector3 facing, bool exterior)
                {
                    var go = new GameObject(name);
                    go.SetActive(false);
                    go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(facing));
                    var t = go.AddComponent<Threshold>();
                    t.Setup(new Vector2(1.6f, 2.1f), exterior, null);
                    go.AddComponent<BrassPlate>().Setup("000", 0.1f);
                    go.SetActive(true);
                    spawned.Add(go);
                    return t;
                }
                Make("Cloakroom", new Vector3(0f, 1.05f, 6f), Vector3.back, false);
                Make("Exterior", new Vector3(100f, 1.05f, 0f), Vector3.right, true);

                bool completed = false;
                System.Action<GameCompleted> onDone = _ => completed = true;
                EventBus.Subscribe(onDone);
                var player = MakePlayer(new Vector3(0f, 0.15f, 2f));
                float until = Time.time + 3f;
                while (player.transform.position.x < 50f && Time.time < until)
                {
                    player.SetVelocity(new Vector3(0f, 0f, 6f));
                    yield return null;
                }
                until = Time.time + 2f;
                while (!completed && Time.time < until) yield return null;
                EventBus.Unsubscribe(onDone);
                Assert.IsTrue(completed, "Arriving through the exterior door should publish GameCompleted after the delay.");
            }
            finally
            {
                cfg.EndingTitleDelay = delay;
            }
        }
    }
}
