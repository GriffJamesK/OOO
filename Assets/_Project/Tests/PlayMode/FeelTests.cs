using System.Collections;
using Kit.Core.Events;
using Kit.Gameplay;
using Kit.Gameplay.Feel;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// Impact feel (polish checklist): hard landings jolt the camera and kick up pooled dust, soft
    /// landings do nothing, and a flurry of landings reuses pooled bursts instead of growing the
    /// pool. Events are published directly, so no player or scene is needed.
    /// </summary>
    public class FeelTests
    {
        static readonly Vector3 Somewhere = new Vector3(0f, -500f, 0f); // far below anything a scene might hold

        [SetUp]
        public void SetUp()
        {
            FeelSystem.EnsureInstalled();
            FeelSystem.ClearEffects();
        }

        [TearDown]
        public void TearDown() => FeelSystem.ClearEffects();

        [UnityTest]
        public IEnumerator HardLanding_SpawnsDust_AndRequestsAShake()
        {
            yield return null;
            var cfg = ConfigLookup.Get<FeelConfig>();
            int active = FeelSystem.PoolActive;
            int shakes = FeelSystem.ShakeCount;
            int played = FeelSystem.EffectsPlayed;

            EventBus.Publish(new PlayerLanded(Somewhere, 15f));

            Assert.AreEqual(active + 1, FeelSystem.PoolActive, "A 15 m/s landing plays one dust burst. " + FeelDebug.Report());
            Assert.AreEqual(played + 1, FeelSystem.EffectsPlayed, FeelDebug.Report());
            Assert.AreEqual(shakes + 1, FeelSystem.ShakeCount, "A 15 m/s landing requests one camera jolt. " + FeelDebug.Report());
            Assert.Greater(FeelSystem.LastShakeAmplitude, 0f);
            Assert.LessOrEqual(FeelSystem.LastShakeAmplitude, cfg.LandShakeCap + 1e-5f, "The jolt is capped.");
            Assert.Greater(FeelSystem.LastShakeDuration, 0f);

            yield return null; // a frame of simulation: the burst must still be alive (not despawned instantly)
            Assert.AreEqual(active + 1, FeelSystem.PoolActive, "The burst lives for its particles' lifetime. " + FeelDebug.Report());
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator SoftLanding_DoesNothing()
        {
            yield return null;
            int active = FeelSystem.PoolActive;
            int shakes = FeelSystem.ShakeCount;
            int played = FeelSystem.EffectsPlayed;

            EventBus.Publish(new PlayerLanded(Somewhere, 4f));
            yield return null;

            Assert.AreEqual(active, FeelSystem.PoolActive, "A normal landing makes no dust. " + FeelDebug.Report());
            Assert.AreEqual(played, FeelSystem.EffectsPlayed, FeelDebug.Report());
            Assert.AreEqual(shakes, FeelSystem.ShakeCount, "A normal landing makes no jolt. " + FeelDebug.Report());
        }

        [UnityTest]
        public IEnumerator RepeatedLandings_ReusePooledBursts()
        {
            var cfg = ConfigLookup.Get<FeelConfig>();
            int cap = cfg.MaxActiveEffects;

            // A flurry, far more than the cap, a few per frame: the oldest bursts are recycled.
            for (int i = 0; i < cap * 3; i++)
            {
                EventBus.Publish(new PlayerLanded(Somewhere + Vector3.right * i, 15f));
                if (i % 4 == 3) yield return null;
            }
            Assert.LessOrEqual(FeelSystem.PoolTotal, cap, "The pool never grows past MaxActiveEffects. " + FeelDebug.Report());
            Assert.LessOrEqual(FeelSystem.PoolActive, cap, FeelDebug.Report());

            // Bursts return to the pool by themselves once their particles are gone.
            float timeout = Time.time + cfg.LandDustLifetime.y * cfg.BigFallScale + 3f;
            while (FeelSystem.PoolActive > 0 && Time.time < timeout) yield return null;
            Assert.AreEqual(0, FeelSystem.PoolActive, "Finished bursts despawn on their own. " + FeelDebug.Report());

            // Fewer landings than are parked: every one is a reuse, so the pool does not grow.
            int total = FeelSystem.PoolTotal;
            int reuse = Mathf.Min(5, total);
            for (int i = 0; i < reuse; i++) EventBus.Publish(new PlayerLanded(Somewhere, 15f));
            Assert.AreEqual(reuse, FeelSystem.PoolActive, FeelDebug.Report());
            Assert.AreEqual(total, FeelSystem.PoolTotal, "Landings reuse parked bursts. " + FeelDebug.Report());
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TileEventWithoutABoard_IsIgnoredQuietly()
        {
            yield return null;
            int played = FeelSystem.EffectsPlayed;
            EventBus.Publish(new TilePulled(null, 0, 'A'));
            EventBus.Publish(new TilePlaced(null, 0, 'A'));
            yield return null;
            Assert.AreEqual(played, FeelSystem.EffectsPlayed, "No board, no puff. " + FeelDebug.Report());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void InstallingTwice_SubscribesOnce()
        {
            int before = EventBus.SubscriberCount<PlayerLanded>();
            FeelSystem.EnsureInstalled();
            FeelSystem.EnsureInstalled();
            Assert.AreEqual(before, EventBus.SubscriberCount<PlayerLanded>(), "EnsureInstalled never subscribes a second time.");
        }
    }
}
