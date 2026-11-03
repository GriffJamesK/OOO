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
    /// Slide and chain mechanics on a real CharacterController: a slide adds speed and lowers the
    /// capsule, and a hop out of a slide keeps that speed and adds more. Driven with a gamepad:
    /// the Input System resets keyboards when the Editor is unfocused (or in batch mode), which
    /// would make keyboard-driven play-mode tests flaky.
    /// </summary>
    public class PlayerSlideTests : InputTestFixture
    {
        GameObject ground;
        GameObject player;
        PlayerConfig config;
        Gamepad gamepad;

        public override void Setup()
        {
            base.Setup();
            KitInput.Reset();
            gamepad = InputSystem.AddDevice<Gamepad>();

            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(400f, 1f, 400f);
            Physics.SyncTransforms(); // physics only sees the new ground after a sync; batch-mode first frames can be long

            config = ScriptableObject.CreateInstance<PlayerConfig>();
            player = new GameObject("TestPlayer");
            player.transform.position = new Vector3(0f, 0.05f, -150f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerController>().Config = config;
            EventBus.Publish(new GameStarted());
        }

        public override void TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(ground);
            Object.Destroy(config);
            KitInput.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator SlideBoostsSpeed_AndSlideHopKeepsAndAddsSpeed()
        {
            var pc = player.GetComponent<PlayerController>();
            var cc = player.GetComponent<CharacterController>();

            // Settle on the ground first; batch-mode frame timing is uneven.
            float settleUntil = Time.realtimeSinceStartup + 2f;
            while (!pc.IsGrounded && Time.realtimeSinceStartup < settleUntil) yield return null;

            // Sprint forward to full speed (game time, not frames: movement eases in).
            Set(gamepad.leftStick, new Vector2(0f, 1f));
            Press(gamepad.leftStickButton);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(pc.IsGrounded, "Player should be standing on the test ground.");
            float sprintSpeed = pc.HorizontalSpeed;
            Assert.AreEqual(config.MoveSpeed * config.SprintMultiplier, sprintSpeed, 0.3f, PlayerDebug.Report());

            // Slide: speed jumps above sprint speed and the capsule drops.
            Press(gamepad.buttonEast);
            yield return null;
            yield return null;
            Assert.IsTrue(pc.IsSliding, PlayerDebug.Report());
            Assert.AreEqual(config.SlideHeight, cc.height, 0.01f);
            float slideSpeed = pc.HorizontalSpeed;
            Assert.Greater(slideSpeed, sprintSpeed + 1f, "A slide from a sprint should add speed.");

            // Slide-hop: leaving the slide with Jump keeps the momentum and adds the hop boost.
            Press(gamepad.buttonSouth);
            yield return null;
            yield return null;
            Release(gamepad.buttonSouth);
            Assert.IsFalse(pc.IsGrounded, "Jump should leave the ground.");
            Assert.Greater(pc.HorizontalSpeed, slideSpeed, "A slide-hop keeps slide speed and adds the hop boost.");
            Assert.Greater(pc.CurrentFieldOfView, config.FieldOfView, "FOV widens with speed.");
        }

        [UnityTest]
        public IEnumerator SlideJump_SnapsMomentumTowardFacing()
        {
            var pc = player.GetComponent<PlayerController>();
            float settleUntil = Time.realtimeSinceStartup + 2f;
            while (!pc.IsGrounded && Time.realtimeSinceStartup < settleUntil) yield return null;

            // Sprint and slide along +Z (no camera yet: movement is world-relative).
            Set(gamepad.leftStick, new Vector2(0f, 1f));
            Press(gamepad.leftStickButton);
            yield return new WaitForSeconds(0.5f);
            Press(gamepad.buttonEast);
            yield return null;
            yield return null;
            Assert.IsTrue(pc.IsSliding, PlayerDebug.Report());
            Set(gamepad.leftStick, Vector2.zero); // no steering input: any turn must come from the slide-jump

            // Look 90 degrees right (+X), then slide-jump.
            var cam = new GameObject("TestCamera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            cam.transform.rotation = Quaternion.LookRotation(Vector3.right);
            try
            {
                Press(gamepad.buttonSouth);
                yield return null;
                Release(gamepad.buttonSouth);
                var before = player.transform.position;
                yield return null;
                yield return null;
                var moved = player.transform.position - before;
                moved.y = 0f;
                Assert.Greater(Vector3.Dot(moved.normalized, Vector3.right), 0.95f,
                    $"Slide-jump should redirect momentum toward facing (+X). moved={moved} {PlayerDebug.Report()}");
            }
            finally { Object.Destroy(cam); }
        }

        [UnityTest]
        public IEnumerator AirMomentum_IsKept_AtVeryHighFrameRate()
        {
            // Regressions at thousands of fps: collision absorption once compared against
            // controller.velocity, whose float noise ratcheted carried momentum down to zero; and
            // CharacterController.minMoveDistance dropped slow vertical steps.
            var pc = player.GetComponent<PlayerController>();
            float settleUntil = Time.realtimeSinceStartup + 2f;
            while (!pc.IsGrounded && Time.realtimeSinceStartup < settleUntil) yield return null;

            Set(gamepad.leftStick, new Vector2(0f, 1f));
            Press(gamepad.leftStickButton);
            yield return new WaitForSeconds(0.5f);
            float sprint = pc.HorizontalSpeed;

            Time.captureDeltaTime = 0.0002f; // 5000 fps, like batch mode
            try
            {
                float startY = player.transform.position.y;
                Press(gamepad.buttonSouth);
                yield return null;
                Release(gamepad.buttonSouth);
                // Rise to the apex: ~0.32 s of game time = ~1600 frames.
                float apex = startY;
                for (int i = 0; i < 3000 && pc.VerticalSpeed > 0f; i++)
                {
                    apex = Mathf.Max(apex, player.transform.position.y);
                    yield return null;
                }
                Assert.IsFalse(pc.IsGrounded, PlayerDebug.Report());
                Assert.Greater(pc.HorizontalSpeed, sprint - 0.1f, "Airborne momentum must not decay with frame rate. " + PlayerDebug.Report());
                // Regression: CharacterController.minMoveDistance (default 1 mm) dropped slow vertical steps,
                // so at high frame rates jumps stalled well short of JumpHeight.
                Assert.Greater(apex - startY, config.JumpHeight - 0.1f, "A jump must reach its full height at any frame rate. " + PlayerDebug.Report());
            }
            finally { Time.captureDeltaTime = 0f; }
        }

        [UnityTest]
        public IEnumerator SlideNeedsSpeed_StandingSlideDoesNothing()
        {
            var pc = player.GetComponent<PlayerController>();
            yield return new WaitForSeconds(0.2f);
            Press(gamepad.buttonEast);
            yield return null;
            yield return null;
            Assert.IsFalse(pc.IsSliding, "Slide requires SlideMinStartSpeed.");
        }
    }
}
