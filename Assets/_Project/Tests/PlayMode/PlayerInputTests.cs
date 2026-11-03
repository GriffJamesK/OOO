using System.Collections;
using Kit.Core.Events;
using Kit.Gameplay;
using Kit.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// Input pattern to copy: InputTestFixture swaps in virtual devices, then Set() drives them.
    /// KitInput is reset after the fixture's SetUp so the shared actions bind to the test devices.
    /// </summary>
    public class PlayerInputTests : InputTestFixture
    {
        GameObject player;
        GameObject ground;
        PlayerConfig config;

        public override void Setup()
        {
            base.Setup();
            KitInput.Reset();
        }

        public override void TearDown()
        {
            if (player != null) Object.Destroy(player);
            if (ground != null) Object.Destroy(ground);
            if (config != null) Object.Destroy(config);
            KitInput.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PlayerMovesOnInput()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            config = ScriptableObject.CreateInstance<PlayerConfig>();

            // Stand on real ground: in the air only limited air control applies, which is not what this tests.
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(200f, 1f, 200f);
            Physics.SyncTransforms(); // physics only sees the new ground after a sync; batch-mode first frames can be long

            player = new GameObject("TestPlayer");
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            var pc = player.AddComponent<PlayerController>();
            pc.Config = config;
            EventBus.Publish(new GameStarted()); // input is ignored until the game starts

            // Wait for the controller to settle on the ground; batch-mode frame timing is uneven.
            float settleUntil = Time.realtimeSinceStartup + 2f;
            while (!pc.IsGrounded && Time.realtimeSinceStartup < settleUntil) yield return null;
            Assert.IsTrue(pc.IsGrounded, "Player should be standing on the test ground. " + PlayerDebug.Report());
            var start = player.transform.position;

            // Hold for game time, not frames: movement eases in over PlayerConfig.AccelerationTime,
            // and test-runner frames can be only a few milliseconds long.
            Set(gamepad.leftStick, new Vector2(0f, 1f));
            yield return new WaitForSeconds(0.3f);

            var moved = player.transform.position - start;
            moved.y = 0f;
            Assert.Greater(moved.magnitude, 0.5f, "Capsule should move horizontally while Move is held. " + PlayerDebug.Report());
        }
    }
}
