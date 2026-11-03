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
    /// Wall run on a real wall: sprinting alongside a wall and jumping attaches to it (gentle arc,
    /// slower fall), and jumping off it kicks away from the wall with a small speed gain.
    /// Gamepad-driven for the same reason as PlayerSlideTests.
    /// </summary>
    public class PlayerWallRunTests : InputTestFixture
    {
        GameObject ground;
        GameObject wall;
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

            // Wall along +Z, its face 0.7 m to the right of the run line (x = 0).
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0.95f, 3f, -30f);
            wall.transform.localScale = new Vector3(0.5f, 6f, 40f);
            Physics.SyncTransforms();

            config = ScriptableObject.CreateInstance<PlayerConfig>();
            player = new GameObject("TestPlayer");
            player.transform.position = new Vector3(0f, 0.05f, -62f);
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
            Object.Destroy(wall);
            Object.Destroy(ground);
            Object.Destroy(config);
            KitInput.Reset();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator SprintJumpBesideWall_WallRuns_ThenWallJumpKicksAwayFaster()
        {
            var pc = player.GetComponent<PlayerController>();
            float settleUntil = Time.realtimeSinceStartup + 2f;
            while (!pc.IsGrounded && Time.realtimeSinceStartup < settleUntil) yield return null;

            // Sprint up the run line until the wall is beside us, then jump.
            Set(gamepad.leftStick, new Vector2(0f, 1f));
            Press(gamepad.leftStickButton);
            float until = Time.realtimeSinceStartup + 5f;
            while (player.transform.position.z < -46f && Time.realtimeSinceStartup < until) yield return null;
            // Timeline for the failure message: what happened, frame by frame.
            var timeline = new System.Text.StringBuilder();
            float t0 = Time.time;
            System.Action<PlayerJumped> onJump = e => timeline.Append($" jump@{Time.time - t0:0.000}");
            System.Action<PlayerWallRan> onWall = e => timeline.Append($" wall@{Time.time - t0:0.000}");
            EventBus.Subscribe(onJump);
            EventBus.Subscribe(onWall);

            // Records state changes (not every frame) so a failure message shows what happened.
            bool lastG = pc.IsGrounded, lastW = pc.IsWallRunning;
            int frames = 0;
            float nextSample = 0f;
            IEnumerator Watch(float seconds)
            {
                float end = Time.time + seconds;
                while (Time.time < end)
                {
                    frames++;
                    if (Time.time - t0 >= nextSample)
                    {
                        nextSample += 0.05f;
                        timeline.Append($" ({Time.time - t0:0.00} y={player.transform.position.y:0.00} vy={pc.VerticalSpeed:0.0})");
                    }
                    if (pc.IsGrounded != lastG || pc.IsWallRunning != lastW)
                    {
                        lastG = pc.IsGrounded; lastW = pc.IsWallRunning;
                        var p = player.transform.position;
                        timeline.Append($" [{Time.time - t0:0.000} g={lastG} w={lastW} y={p.y:0.00} x={p.x:0.00} spd={pc.HorizontalSpeed:0.0}]");
                    }
                    yield return null;
                }
            }
            string Info() => $"{PlayerDebug.Report()} frames={frames} dt={Time.deltaTime:0.0000} timeline:{timeline}";

            Press(gamepad.buttonSouth);
            yield return null;
            Release(gamepad.buttonSouth);
            yield return Watch(0.15f);

            Assert.IsTrue(pc.IsWallRunning, "Jumping at sprint speed beside a wall should start a wall run. " + Info());
            Assert.AreEqual(1f, pc.WallSide, "The wall is on the right.");
            Assert.Greater(Mathf.Abs(pc.CurrentCameraRoll), 1f, "The camera leans away from the wall.");

            // A normal jump would be back on the ground after ~0.63 s; the wall-run arc keeps us up.
            yield return Watch(0.55f);
            EventBus.Unsubscribe(onJump);
            EventBus.Unsubscribe(onWall);
            Assert.IsTrue(pc.IsWallRunning, "Still wall running. " + Info());
            Assert.Greater(player.transform.position.y, 0.5f, "Still airborne on the wall. " + Info());

            // Wall jump: speed goes up a little and we move away from the wall (-X).
            float speedBefore = pc.HorizontalSpeed;
            float xBefore = player.transform.position.x;
            Press(gamepad.buttonSouth);
            yield return null;
            Release(gamepad.buttonSouth);
            yield return new WaitForSeconds(0.1f);
            Assert.IsFalse(pc.IsWallRunning);
            Assert.Greater(pc.HorizontalSpeed, speedBefore, "A wall jump adds a little speed. " + PlayerDebug.Report());
            Assert.Less(player.transform.position.x, xBefore - 0.2f, "A wall jump kicks away from the wall.");
        }

        [UnityTest]
        public IEnumerator SlowJumpBesideWall_DoesNotWallRun()
        {
            var pc = player.GetComponent<PlayerController>();
            float settleUntil = Time.realtimeSinceStartup + 2f;
            while (!pc.IsGrounded && Time.realtimeSinceStartup < settleUntil) yield return null;
            player.GetComponent<PlayerController>().Respawn(new Pose(new Vector3(0f, 0.05f, -40f), Quaternion.identity));

            Set(gamepad.leftStick, new Vector2(0f, 0.4f)); // ~2.4 m/s, under WallRunMinSpeed
            yield return new WaitForSeconds(0.3f);
            Press(gamepad.buttonSouth);
            yield return null;
            Release(gamepad.buttonSouth);
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(pc.IsWallRunning, "Wall runs need WallRunMinSpeed.");
        }
    }
}
