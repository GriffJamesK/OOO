using System.Collections;
using System.Collections.Generic;
using Kit.Core.Events;
using Kit.Gameplay;
using Kit.Gameplay.Thresholds;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// Going through paired thresholds on a real player (GDD F2, F6): speed and heading carry
    /// through the pair's rotation, and a fall into a floor hatch leaves a tilted wall opening as
    /// a launch. Momentum is set directly, so no input device is needed.
    /// </summary>
    public class ThresholdTraversalTests
    {
        readonly List<Object> spawned = new List<Object>();
        PlayerController player;

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

        Threshold MakeThreshold(string name, string number, Vector3 centre, Quaternion rotation, Vector2 size)
        {
            var go = new GameObject(name);
            go.SetActive(false); // add the plate before OnEnable registers the threshold
            go.transform.SetPositionAndRotation(centre, rotation);
            var t = go.AddComponent<Threshold>();
            t.Setup(size, false, null);
            go.AddComponent<BrassPlate>().Setup(number, 0.1f);
            go.SetActive(true);
            spawned.Add(go);
            return t;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.Destroy(o);
            spawned.Clear();
        }

        [UnityTest]
        public IEnumerator WalkingThroughAPair_KeepsSpeed_AndTurnsWithTheFrames()
        {
            // No ground: on walkable ground with no input the player stops instantly (Source rule),
            // so the run is airborne with vertical speed held at zero each frame.
            var a = MakeThreshold("A", "12", new Vector3(0f, 1.05f, 10f), Quaternion.LookRotation(Vector3.back), new Vector2(1.6f, 2.1f));
            var b = MakeThreshold("B", "12", new Vector3(50f, 1.05f, -30f), Quaternion.LookRotation(Vector3.right), new Vector2(1.6f, 2.1f));
            yield return null;
            Assert.AreSame(b, a.Partner, "Two plates reading 12 should pair. " + ThresholdDebug.Report());

            player = MakePlayer(new Vector3(0f, 0.15f, 4f)); // capsule centre level with the opening's centre
            int crossings = 0;
            System.Action<PlayerCrossedThreshold> onCross = _ => crossings++;
            EventBus.Subscribe(onCross);
            yield return null;

            float until = Time.time + 3f;
            while (crossings == 0 && Time.time < until)
            {
                player.SetVelocity(new Vector3(0f, 0f, 10f));
                yield return null;
            }
            EventBus.Unsubscribe(onCross);

            Assert.AreEqual(1, crossings, "Running into A's front should cross exactly once.");
            Assert.Greater(player.transform.position.x, 49.5f, $"Should come out in front of B, at {player.transform.position}");
            Assert.AreEqual(10f, player.HorizontalSpeed, 0.15f, "Speed carries through.");
            var flat = new Vector3(player.Velocity.x, 0f, player.Velocity.z);
            Assert.Less(Vector3.Angle(flat, Vector3.right), 1f, "Heading turns with the frames: out along B's front.");
        }

        [UnityTest]
        public IEnumerator FallingIntoAFloorHatch_LaunchesOutOfATiltedOpening()
        {
            var hatch = MakeThreshold("Hatch", "6", new Vector3(0f, 0f, 0f), Quaternion.LookRotation(Vector3.up, Vector3.forward), new Vector2(3f, 3f));
            var exitForward = new Vector3(Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad), 0f);
            MakeThreshold("Exit", "6", new Vector3(200f, 40f, 0f), Quaternion.LookRotation(exitForward, Vector3.up), new Vector2(2.5f, 2.5f));
            yield return null;
            Assert.IsTrue(hatch.IsPaired, ThresholdDebug.Report());

            player = MakePlayer(new Vector3(0f, 20f, 0f));
            bool crossed = false;
            System.Action<PlayerCrossedThreshold> onCross = _ => crossed = true;
            EventBus.Subscribe(onCross);
            float until = Time.time + 4f;
            while (!crossed && Time.time < until) yield return null;
            EventBus.Unsubscribe(onCross);

            Assert.IsTrue(crossed, "Falling 20 m should pass through the hatch.");
            float maxExit = ConfigLookup.Get<ThresholdConfig>().MaxExitSpeed;
            Assert.Less(Vector3.Angle(player.Velocity, exitForward), 2f, $"Leaves along the exit's front; velocity {player.Velocity}");
            Assert.AreEqual(maxExit, player.Velocity.magnitude, 0.5f, "A 20 m fall is faster than MaxExitSpeed, so it is clamped to it.");
            Assert.Greater(player.transform.position.x, 199f);
        }
    }
}
