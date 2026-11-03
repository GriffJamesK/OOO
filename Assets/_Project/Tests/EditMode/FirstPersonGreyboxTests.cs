using Kit.Gameplay;
using Kit.Gameplay.Look;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Tests.EditMode
{
    /// <summary>
    /// Guards the first-person and greybox defaults: movement maths, the colour-role mapping
    /// agents rely on, and the Lo-Fi preset values. Asset tests read the real project assets.
    /// </summary>
    public class FirstPersonGreyboxTests
    {
        static readonly AnimationCurve Linear = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Test]
        public void SpeedRamp_ReachesFullSpeedAfterAccelerationTime_AndStopsAfterDeceleration()
        {
            var ramp = new SpeedRamp();
            float speed = 0f;
            for (int i = 0; i < 3; i++) speed = ramp.Step(true, 0.125f, 0.0625f, Linear, Linear, 0.03125f);
            Assert.Less(speed, 1f);
            speed = ramp.Step(true, 0.125f, 0.0625f, Linear, Linear, 0.03125f);
            Assert.AreEqual(1f, speed, 1e-5f);

            ramp.Step(false, 0.125f, 0.0625f, Linear, Linear, 0.03125f);
            speed = ramp.Step(false, 0.125f, 0.0625f, Linear, Linear, 0.03125f);
            Assert.AreEqual(0f, speed, 1e-5f);
        }

        [Test]
        public void CoyoteTimer_AllowsJumpBrieflyAfterLeavingGround_Once()
        {
            var coyote = new CoyoteTimer();
            coyote.Tick(grounded: true, coyoteTime: 0.1f, dt: 0.016f);
            coyote.Tick(grounded: false, coyoteTime: 0.1f, dt: 0.0625f);
            Assert.IsTrue(coyote.CanJump, "Still inside the 0.1 s grace period.");

            coyote.Consume();
            Assert.IsFalse(coyote.CanJump, "One ledge gives one jump.");

            coyote.Tick(grounded: true, coyoteTime: 0.1f, dt: 0.016f);
            coyote.Tick(grounded: false, coyoteTime: 0.1f, dt: 0.125f);
            Assert.IsFalse(coyote.CanJump, "Grace period expired.");
        }

        [Test]
        public void PlayerConfig_DefaultsMatchFirstPersonSpec()
        {
            var config = ScriptableObject.CreateInstance<PlayerConfig>();
            try
            {
                Assert.AreEqual(95f, config.FieldOfView);
                Assert.AreEqual(0.1f, config.CoyoteTime);
                Assert.AreEqual(-30f, config.Gravity);
                Assert.Greater(config.SpeedFovKick, 0f);
                Assert.Greater(config.HeadBobAmplitude, 0f);
                Assert.LessOrEqual(config.HeadBobAmplitude, 0.025f, "Walking bob must stay subtle (owner feedback).");
                Assert.Greater(config.LandingDipMax, 0f);
                Assert.Greater(config.MaxSpeed, config.MoveSpeed * config.SprintMultiplier);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void ChainBoost_IsFullAtWalkSpeed_ShrinksWithSpeed_AndNeverPassesMax()
        {
            Assert.AreEqual(3f, Momentum.ChainBoost(6f, 3f, walkSpeed: 6f, maxSpeed: 18f), 1e-4f);
            Assert.AreEqual(1.5f, Momentum.ChainBoost(12f, 3f, 6f, 18f), 1e-4f);
            Assert.AreEqual(0f, Momentum.ChainBoost(18f, 3f, 6f, 18f), 1e-4f);
            Assert.LessOrEqual(17.5f + Momentum.ChainBoost(17.5f, 3f, 6f, 18f), 18f);
        }

        [Test]
        public void ChainedHops_BuildSpeedTowardMax()
        {
            // Simulates the speed rule of a hop chain: every chained hop adds a shrinking boost.
            float speed = 9f; // sprinting
            for (int hop = 0; hop < 30; hop++) speed += Momentum.ChainBoost(speed, 1f, 6f, 18f);
            Assert.Greater(speed, 15f, "Thirty chained hops should carry a sprint well past sprint speed.");
            Assert.LessOrEqual(speed, 18f);
        }

        [Test]
        public void ChainStreak_GrowsBoostEachChain_UpToCap_AndResetsOnBreak()
        {
            var streak = new ChainStreak();
            Assert.AreEqual(1f, streak.Multiplier(0.25f, 8), 1e-5f);
            streak.Extend();
            streak.Extend();
            Assert.AreEqual(1.5f, streak.Multiplier(0.25f, 8), 1e-5f);
            for (int i = 0; i < 20; i++) streak.Extend();
            Assert.AreEqual(3f, streak.Multiplier(0.25f, 8), 1e-5f, "Multiplier stops growing at maxChains.");
            streak.Break();
            Assert.AreEqual(1f, streak.Multiplier(0.25f, 8), 1e-5f);
        }

        [Test]
        public void SlideJumpChain_WithStreak_PicksUpMoreSpeedThanWithout()
        {
            // Same slide+hop cycle as the controller (default PlayerConfig numbers), with and without the streak.
            float Run(float perChain)
            {
                var streak = new ChainStreak();
                float speed = 9f; // sprint
                for (int cycle = 0; cycle < 5; cycle++)
                {
                    float m = streak.Multiplier(perChain, 8);
                    speed += Momentum.ChainBoost(speed, 3f * m, 6f, 26f); // slide
                    speed += Momentum.ChainBoost(speed, 1f * m, 6f, 26f); // slide-jump
                    streak.Extend();
                }
                return speed;
            }

            float flat = Run(0f);
            float escalating = Run(0.25f);
            Assert.Greater(escalating, flat + 1.5f, "The streak should make a long chain clearly faster.");
            Assert.Greater(escalating, 20f);
            Assert.LessOrEqual(escalating, 26f);
        }

        [Test]
        public void WallTangent_FollowsTheWall_OnTheSideYouAreMoving()
        {
            var normal = Vector3.left; // wall on the +X side, facing -X
            Assert.AreEqual(Vector3.forward, Momentum.WallTangent(new Vector3(0.3f, 0f, 1f), normal));
            Assert.AreEqual(Vector3.back, Momentum.WallTangent(new Vector3(0.3f, 0f, -1f), normal));
        }

        [Test]
        public void WallJump_GainsExactlyTheBoost_AndLeavesTheWall()
        {
            var normal = Vector3.left;               // wall on the right of a +Z run
            var run = Vector3.forward * 10f;
            var v = Momentum.WallJump(run, normal, push: 6f, boost: 1.5f, aim: Vector3.forward, maxAimTurn: 60f);
            Assert.AreEqual(11.5f, v.magnitude, 1e-3f, "Speed gain is exactly the boost.");
            Assert.Less(v.x, -1f, "Launch points away from the wall.");
            Assert.Greater(v.z, 0f, "Launch keeps going forward.");
        }

        [Test]
        public void WallJump_TurnsTowardAim_ButNeverIntoTheWall()
        {
            var normal = Vector3.left;
            var run = Vector3.forward * 10f;
            var straight = Momentum.WallJump(run, normal, 6f, 0f, Vector3.forward, 60f);
            var aimedAway = Momentum.WallJump(run, normal, 6f, 0f, new Vector3(-1f, 0f, 1f), 60f);
            var aimedIntoWall = Momentum.WallJump(run, normal, 6f, 0f, Vector3.right, 60f);
            Assert.Less(aimedAway.x, straight.x, "Looking away from the wall launches further away.");
            Assert.AreEqual(straight.x, aimedIntoWall.x, 1e-3f, "Looking into the wall falls back to the plain launch.");
        }

        [Test]
        public void WallJump_LaunchesExactlyWhereYouLook_WhenThatClearsTheWall()
        {
            var normal = Vector3.left;               // wall on the right of a +Z run
            var run = Vector3.forward * 10f;
            var aim = new Vector3(-1f, 0f, 1f).normalized;
            var v = Momentum.WallJump(run, normal, push: 6f, boost: 1f, aim: aim, maxAimTurn: 90f);
            Assert.Greater(Vector3.Dot(v.normalized, aim), 0.9999f, "No extra push on top of the aim: you leave the way you look.");
            var away = Momentum.WallJump(run, normal, 6f, 0f, Vector3.left, 90f);
            Assert.Greater(Vector3.Dot(away.normalized, Vector3.left), 0.9999f, "Looking straight off the wall launches straight off it.");
        }

        [Test]
        public void AirAccelerate_HoldingForwardAtSpeed_AddsNothing()
        {
            var v = new Vector3(0f, 0f, 9f);
            var after = Momentum.AirAccelerate(v, Vector3.forward, wishSpeed: 9f, accelerate: 12f, cap: 0.6f, dt: 1f / 60f);
            Assert.AreEqual(v, after, "Source air control: no speed from holding forward once faster than the cap.");
        }

        [Test]
        public void AirAccelerate_Sideways_AddsAtMostTheCap()
        {
            var v = new Vector3(0f, 0f, 9f);
            var after = Momentum.AirAccelerate(v, Vector3.right, 9f, 12f, 0.6f, 1f / 60f);
            Assert.AreEqual(0.6f, after.x, 1e-4f);
            Assert.AreEqual(9f, after.z, 1e-4f);
        }

        [Test]
        public void AirStrafing_TurningWhileStrafing_GainsSpeed()
        {
            // Hold "right" and turn the view right 2 deg per frame for 2 s: classic air strafe.
            var v = new Vector3(0f, 0f, 9f);
            float yaw = 0f;
            for (int i = 0; i < 120; i++)
            {
                yaw += 2f;
                var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                v = Momentum.AirAccelerate(v, right, 9f, 12f, 0.6f, 1f / 60f);
            }
            Assert.Greater(v.magnitude, 10f, "Air strafing should build speed.");
        }

        [Test]
        public void ClipVelocity_RemovesOnlyTheIntoSurfacePart()
        {
            // Wall: keep the along-wall part.
            var wall = Momentum.ClipVelocity(new Vector3(3f, 0f, 4f), Vector3.left);
            Assert.AreEqual(new Vector3(0f, 0f, 4f), wall);
            // Ceiling: upward motion stops.
            var ceiling = Momentum.ClipVelocity(new Vector3(0f, 5f, 2f), Vector3.down);
            Assert.AreEqual(new Vector3(0f, 0f, 2f), ceiling);
            // Surf ramp (60 deg): a pure fall turns into a slide down and away from the ramp.
            var rampNormal = new Vector3(-Mathf.Sin(60f * Mathf.Deg2Rad), Mathf.Cos(60f * Mathf.Deg2Rad), 0f);
            var surf = Momentum.ClipVelocity(new Vector3(0f, -10f, 8f), rampNormal);
            Assert.AreEqual(0f, Vector3.Dot(surf, rampNormal), 1e-4f, "No motion into the ramp.");
            Assert.Less(surf.x, 0f, "Slides down the slope.");
            Assert.AreEqual(8f, surf.z, 1e-4f, "Speed along the ramp is kept.");
        }

        [Test]
        public void Steer_TurnsAtMostMaxDegrees_AndStaysFlat()
        {
            var turned = Momentum.Steer(Vector3.forward, Vector3.right, 30f);
            Assert.AreEqual(30f, Vector3.Angle(Vector3.forward, turned), 0.01f);
            Assert.AreEqual(0f, turned.y, 1e-5f);
            Assert.AreEqual(1f, turned.magnitude, 1e-4f);
        }

        [Test]
        public void SpeedFovKick_IsZeroAtWalk_FullAtMax_AndRisesWithSpeed()
        {
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            Assert.AreEqual(0f, Momentum.SpeedFovKick(3f, 6f, 18f, 20f, curve), 1e-4f);
            Assert.AreEqual(0f, Momentum.SpeedFovKick(6f, 6f, 18f, 20f, curve), 1e-4f);
            Assert.AreEqual(10f, Momentum.SpeedFovKick(12f, 6f, 18f, 20f, curve), 1e-4f);
            Assert.AreEqual(20f, Momentum.SpeedFovKick(25f, 6f, 18f, 20f, curve), 1e-4f);
        }

        [Test]
        public void SpeedRamp_SnapToFraction_ResumesAtThatSpeed()
        {
            var ramp = new SpeedRamp();
            var curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            ramp.SnapToFraction(0.4f, curve);
            Assert.AreEqual(0.4f, curve.Evaluate(ramp.Progress), 1e-3f);
        }

        [TestCase("Mat_Grey_Floor", "role-floor")]
        [TestCase("Mat_Grey_Wall", "role-wall")]
        [TestCase("Mat_Pastel_Blue", "role-interactive")]
        [TestCase("Mat_Pastel_Rose", "role-hazard")]
        [TestCase("Mat_Pastel_Green", "role-goal")]
        [TestCase("Mat_Pastel_Butter", "role-spawn")]
        [TestCase("Mat_Pastel_Peach", "role-pickup")]
        [TestCase("Mat_Pastel_Lilac", "role-character")]
        [TestCase("Mat_Grid_Checker", "role-floor")]
        [TestCase("Mat_Grid_Wall", "role-wall")]
        public void GreyboxMaterial_ExistsWithRoleLabel(string material, string role)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Materials/{material}.mat");
            Assert.IsNotNull(mat, material + " is missing; run Kit > Rebuild Kit Assets.");
            CollectionAssert.Contains(AssetDatabase.GetLabels(mat), role);
        }

        [TestCase("Mat_Dev_Grey_Light", "role-dev-structure")]
        [TestCase("Mat_Dev_Grey_Mid", "role-dev-structure")]
        [TestCase("Mat_Dev_Grey_Dark", "role-dev-structure")]
        [TestCase("Mat_Dev_Orange", "role-dev-feature")]
        public void DevMaterial_ExistsWithRoleLabel(string material, string role)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Materials/{material}.mat");
            Assert.IsNotNull(mat, material + " is missing; run Kit > Rebuild Kit Assets.");
            CollectionAssert.Contains(AssetDatabase.GetLabels(mat), role);
        }

        [Test]
        public void FlatMaterialSet_IsGone()
        {
            Assert.IsEmpty(AssetDatabase.FindAssets("Mat_Flat_ t:Material", new[] { "Assets/_Project/Materials" }));
        }

        [Test]
        public void DefaultProfile_HasNoVignette_AndCarriesFog()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volume_Default.asset");
            Assert.IsNotNull(profile);
            Assert.IsFalse(profile.Has<UnityEngine.Rendering.Universal.Vignette>(), "No vignette (owner request).");
            Assert.IsTrue(profile.TryGet(out KitFog fog) && fog.IsActive());
            Assert.AreEqual(20f, fog.start.value);
            Assert.AreEqual(120f, fog.end.value);
        }

        [Test]
        public void LoFiPreset_IsRemoved()
        {
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/Volume_LoFi.asset"));
            var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/_Project/Settings/URP_Renderer.asset");
            foreach (var feature in renderer.rendererFeatures)
                Assert.IsNotNull(feature, "No missing (deleted) renderer features left on the renderer.");
        }
    }
}
