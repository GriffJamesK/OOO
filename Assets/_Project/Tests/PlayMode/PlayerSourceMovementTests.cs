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
    /// Source-style movement on real colliders: instant stop on release, bunny hops keep speed,
    /// air strafing gains speed, and too-steep ramps surf instead of grounding.
    /// Gamepad-driven for the same reason as PlayerSlideTests.
    /// </summary>
    public class PlayerSourceMovementTests : InputTestFixture
    {
        GameObject ground;
        GameObject player;
        PlayerConfig config;
        Gamepad gamepad;
        PlayerController pc;

        public override void Setup()
        {
            base.Setup();
            KitInput.Reset();
            gamepad = InputSystem.AddDevice<Gamepad>();

            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(400f, 1f, 400f);
            Physics.SyncTransforms();

            config = ScriptableObject.CreateInstance<PlayerConfig>();
            player = new GameObject("TestPlayer");
            player.transform.position = new Vector3(0f, 0.05f, -150f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            pc = player.AddComponent<PlayerController>();
            pc.Config = config;
            EventBus.Publish(new GameStarted());
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Object.Destroy(player);
            Object.Destroy(ground);
            Object.Destroy(config);
            KitInput.Reset();
            base.TearDown();
        }

        IEnumerator SettleAndSprint()
        {
            float until = Time.realtimeSinceStartup + 2f;
            while (!pc.IsGrounded && Time.realtimeSinceStartup < until) yield return null;
            Set(gamepad.leftStick, new Vector2(0f, 1f));
            Press(gamepad.leftStickButton);
            yield return new WaitForSeconds(0.5f);
        }

        [UnityTest]
        public IEnumerator ReleasingInputOnGround_StopsImmediately()
        {
            yield return SettleAndSprint();
            Assert.Greater(pc.HorizontalSpeed, 8f, PlayerDebug.Report());

            Set(gamepad.leftStick, Vector2.zero);
            Release(gamepad.leftStickButton);
            yield return null;
            yield return null;
            Assert.IsTrue(pc.IsGrounded);
            Assert.Less(pc.HorizontalSpeed, 0.01f, "On the ground with nothing held, the player stops at once. " + PlayerDebug.Report());
        }

        [UnityTest]
        public IEnumerator BunnyHop_WithNoKeysHeld_KeepsSpeed()
        {
            yield return SettleAndSprint();
            float sprint = pc.HorizontalSpeed;
            int jumps = 0;
            var log = new System.Text.StringBuilder();
            System.Action<PlayerJumped> count = _ => { jumps++; log.Append($" jump#{jumps}@y{player.transform.position.y:0.00}"); };
            EventBus.Subscribe(count);

            Press(gamepad.buttonSouth);
            yield return null;
            // Let go of everything in the air. Changing several controls in one step can lose a
            // release, so do the stick first, then the buttons a frame later, and check.
            Set(gamepad.leftStick, Vector2.zero);
            Release(gamepad.leftStickButton);
            yield return null;
            Release(gamepad.buttonSouth);
            yield return null;
            Assert.IsFalse(gamepad.buttonSouth.isPressed, "Jump must be released before the buffered press.");

            // Press Jump just before touching down (jump buffer), so the landing frame is a hop.
            float until = Time.realtimeSinceStartup + 3f;
            while (!(jumps == 1 && pc.VerticalSpeed < 0f && player.transform.position.y < 0.3f) && Time.realtimeSinceStartup < until) yield return null;
            log.Append($" press@y{player.transform.position.y:0.00},vy{pc.VerticalSpeed:0.0},g{pc.IsGrounded}" +
                       $" southHeld={gamepad.buttonSouth.isPressed} jumpHeld={KitInput.Actions.Player.Jump.IsPressed()}" +
                       $" jumpEnabled={KitInput.Actions.Player.Jump.enabled} input={pc.InputEnabled}");
            // Queue the press so the next frame's normal input update processes it, as with a real
            // controller; an immediate manual update can make WasPressedThisFrame miss it.
            Press(gamepad.buttonSouth, queueEventOnly: true);
            until = Time.realtimeSinceStartup + 3f;
            int frames = 0;
            while (jumps < 2 && Time.realtimeSinceStartup < until)
            {
                if (frames++ < 4) log.Append($" [f{frames} y{player.transform.position.y:0.00} g{pc.IsGrounded} buf{pc.JumpBufferRemaining:0.000}" +
                                             $" south={gamepad.buttonSouth.isPressed} pressedFrame={KitInput.Actions.Player.Jump.WasPressedThisFrame()}]");
                yield return null;
            }
            Release(gamepad.buttonSouth);
            yield return null;
            EventBus.Unsubscribe(count);

            Assert.AreEqual(2, jumps, "The buffered jump should fire on landing. " + PlayerDebug.Report() + log);
            Assert.GreaterOrEqual(pc.HorizontalSpeed, sprint - 0.05f, "A hop on the landing frame keeps speed with no keys held. " + PlayerDebug.Report());
        }

        [UnityTest]
        public IEnumerator AirStrafing_WhileTurning_GainsSpeed()
        {
            var cam = new GameObject("TestCamera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            try
            {
                yield return SettleAndSprint();
                float start = pc.HorizontalSpeed;

                Time.captureDeltaTime = 1f / 120f; // deterministic air time
                Press(gamepad.buttonSouth);
                yield return null;
                Release(gamepad.buttonSouth);

                // Hold "right" and turn the view right while airborne.
                Set(gamepad.leftStick, new Vector2(1f, 0f));
                float yaw = 0f;
                for (int i = 0; i < 70 && !pc.IsGrounded; i++)
                {
                    yaw += 1.5f;
                    cam.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    yield return null;
                }
                Assert.Greater(pc.HorizontalSpeed, start + 0.3f, "Air strafing should gain speed. " + PlayerDebug.Report());
            }
            finally { Object.Destroy(cam); }
        }

        [UnityTest]
        public IEnumerator HoldingForwardInAir_SteersMomentum()
        {
            var cam = new GameObject("TestCamera") { tag = "MainCamera" };
            cam.AddComponent<Camera>();
            try
            {
                yield return SettleAndSprint(); // running +Z, camera facing +Z
                Press(gamepad.buttonSouth);
                yield return null;
                Release(gamepad.buttonSouth);
                cam.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // look +X, keep holding forward

                var last = player.transform.position;
                yield return new WaitForSeconds(0.4f);
                var moved = player.transform.position - last;
                moved.y = 0f;
                Assert.IsFalse(pc.IsGrounded, PlayerDebug.Report());
                Assert.Greater(Vector3.Dot(moved.normalized, Vector3.right), 0.3f,
                    "Holding forward in the air should steer momentum toward where you look. " + PlayerDebug.Report());
            }
            finally { Object.Destroy(cam); }
        }

        /// <summary>12 solid 0.25 m steps up +Z from z = 0 (3 m total), then a 6 m landing.</summary>
        static GameObject BuildStairs()
        {
            var root = new GameObject("TestStairs");
            for (int i = 0; i < 12; i++)
            {
                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.transform.SetParent(root.transform);
                float top = (i + 1) * 0.25f;
                step.transform.position = new Vector3(0f, top * 0.5f, i * 0.35f + 0.175f);
                step.transform.localScale = new Vector3(4f, top, 0.35f);
            }
            var landing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            landing.transform.SetParent(root.transform);
            landing.transform.position = new Vector3(0f, 1.5f, 12 * 0.35f + 3f);
            landing.transform.localScale = new Vector3(4f, 3f, 6f);
            Physics.SyncTransforms();
            return root;
        }

        [UnityTest]
        public IEnumerator Stairs_UpAndDown_StayGrounded_AndCameraGlides()
        {
            var stairs = BuildStairs();
            try
            {
                pc.Respawn(new Pose(new Vector3(0f, 0.05f, -6f), Quaternion.identity));
                float until = Time.realtimeSinceStartup + 2f;
                while (!pc.IsGrounded && Time.realtimeSinceStartup < until) yield return null;

                // Up: sprint +Z over the steps (z 0..4.2).
                Set(gamepad.leftStick, new Vector2(0f, 1f));
                Press(gamepad.leftStickButton);
                int frames = 0, groundedFrames = 0;
                float maxEyeStep = 0f, lastEye = pc.CameraTarget.position.y;
                until = Time.realtimeSinceStartup + 5f;
                while (player.transform.position.z < 6f && Time.realtimeSinceStartup < until)
                {
                    yield return null;
                    float z = player.transform.position.z;
                    float eye = pc.CameraTarget.position.y;
                    if (z > 0.3f && z < 4f)
                    {
                        frames++;
                        if (pc.IsGrounded) groundedFrames++;
                        maxEyeStep = Mathf.Max(maxEyeStep, Mathf.Abs(eye - lastEye));
                    }
                    lastEye = eye;
                }
                Assert.Greater(player.transform.position.y, 2.9f, "Should reach the top. " + PlayerDebug.Report());
                Assert.GreaterOrEqual(groundedFrames, frames * 0.9f, $"Going up stairs should stay grounded ({groundedFrames}/{frames}).");
                Assert.Less(maxEyeStep, 0.08f, "The camera should glide up steps, not pop a step height in one frame.");

                // Down: run back over the steps (-Z).
                Set(gamepad.leftStick, new Vector2(0f, -1f));
                frames = groundedFrames = 0;
                maxEyeStep = 0f;
                lastEye = pc.CameraTarget.position.y;
                until = Time.realtimeSinceStartup + 5f;
                while (player.transform.position.z > -2f && Time.realtimeSinceStartup < until)
                {
                    yield return null;
                    float z = player.transform.position.z;
                    float eye = pc.CameraTarget.position.y;
                    if (z > 0.3f && z < 4f)
                    {
                        frames++;
                        if (pc.IsGrounded) groundedFrames++;
                        maxEyeStep = Mathf.Max(maxEyeStep, Mathf.Abs(eye - lastEye));
                    }
                    lastEye = eye;
                }
                Assert.Less(player.transform.position.y, 0.2f, "Should reach the bottom. " + PlayerDebug.Report());
                Assert.GreaterOrEqual(groundedFrames, frames * 0.9f, $"Going down stairs should stay grounded, not hop step to step ({groundedFrames}/{frames}).");
                Assert.Less(maxEyeStep, 0.08f, "The camera should glide down steps.");
            }
            finally { Object.Destroy(stairs); }
        }

        [UnityTest]
        public IEnumerator SteepRamp_Surfs_AndKeepsSpeedAlongIt()
        {
            // A 60 deg ramp running along +Z, facing -X. Too steep to stand on (slope limit 45).
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(0f, 0f, 60f));
            ramp.transform.localScale = new Vector3(20f, 1f, 80f);
            Physics.SyncTransforms();
            try
            {
                var normal = ramp.transform.up;
                var onSurface = ramp.transform.position + normal * 0.5f;
                pc.Respawn(new Pose(onSurface + normal * 0.6f + Vector3.up * 0.5f + Vector3.back * 30f, Quaternion.identity));
                pc.SetVelocity(new Vector3(0f, 0f, 10f));

                bool surfed = false;
                float startZ = player.transform.position.z;
                float until = Time.time + 0.6f;
                while (Time.time < until)
                {
                    surfed |= pc.IsSurfing;
                    Assert.IsFalse(pc.IsGrounded, "A 60 deg ramp is not ground. " + PlayerDebug.Report());
                    yield return null;
                }
                float travelled = player.transform.position.z - startZ;
                Assert.IsTrue(surfed, "Touching a too-steep ramp should surf. " + PlayerDebug.Report());
                Assert.Greater(travelled, 5.4f, "No friction while surfing: ~10 m/s along the ramp for 0.6 s. " + PlayerDebug.Report());
            }
            finally { Object.Destroy(ramp); }
        }

        [UnityTest]
        public IEnumerator LeavingASurfRamp_GoesTheWayYouFace_NotDownTheRamp()
        {
            // A short floating 60 deg ramp along +Z (z -5..5), facing -X. Surfing it with no input
            // slides you down-slope (-X); once you fly off its north end you should head where you
            // face (+Z here: no camera), not keep drifting toward the ramp's slope direction.
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.transform.SetPositionAndRotation(new Vector3(0f, 20f, 0f), Quaternion.Euler(0f, 0f, 60f));
            ramp.transform.localScale = new Vector3(20f, 1f, 10f);
            Physics.SyncTransforms();
            try
            {
                var normal = ramp.transform.up;
                var onSurface = ramp.transform.position + normal * 0.5f;
                pc.Respawn(new Pose(onSurface + normal * 0.6f + Vector3.up * 0.5f + Vector3.back * 3f, Quaternion.identity));
                pc.SetVelocity(new Vector3(0f, 0f, 10f));

                bool surfed = false;
                float until = Time.time + 2f;
                while (Time.time < until && (pc.IsSurfing || player.transform.position.z < 6.5f))
                {
                    surfed |= pc.IsSurfing;
                    yield return null;
                }
                Assert.IsTrue(surfed, "Should surf the ramp first. " + PlayerDebug.Report());
                Assert.Greater(player.transform.position.z, 6.5f, "Should fly off the north end. " + PlayerDebug.Report());

                var last = player.transform.position;
                yield return new WaitForSeconds(0.1f);
                var moved = player.transform.position - last;
                moved.y = 0f;
                Assert.IsFalse(pc.IsSurfing, PlayerDebug.Report());
                Assert.Greater(Vector3.Dot(moved.normalized, Vector3.forward), 0.995f,
                    "Leaving the ramp should send you the way you face, not sideways down the slope. moved=" + moved + " " + PlayerDebug.Report());
            }
            finally { Object.Destroy(ramp); }
        }
    }
}
