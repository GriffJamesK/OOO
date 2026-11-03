using System.Collections.Generic;
using Kit.Core;
using Kit.Core.Config;
using Kit.Core.Debugging;
using Kit.Core.Events;
using Kit.Core.Feel;
using Kit.Core.FSM;
using Kit.Core.Pooling;
using Kit.Core.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace Kit.Tests.EditMode
{
    /// <summary>One test per Kit.Core class. Copy a test here as the pattern for new edit-mode tests.</summary>
    public class CoreTests
    {
        struct Ping { public int Value; }

        enum Signal { Red, Green }

        sealed class TestConfig : ConfigAsset
        {
            public float Speed = -5f;
            protected override void Sanitize() => Speed = Mathf.Max(0f, Speed);
        }

        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        [Test]
        public void EventBus_PublishReachesSubscriber_UntilUnsubscribed()
        {
            int received = 0;
            System.Action<Ping> handler = p => received += p.Value;

            EventBus.Subscribe(handler);
            EventBus.Publish(new Ping { Value = 2 });
            EventBus.Unsubscribe(handler);
            EventBus.Publish(new Ping { Value = 5 });

            Assert.AreEqual(2, received);
            Assert.AreEqual(0, EventBus.SubscriberCount<Ping>());
        }

        [Test]
        public void ObjectPool_GetAndRelease_TracksCounts()
        {
            int created = 0;
            var pool = new ObjectPool<object>(() => { created++; return new object(); }, prewarm: 3);
            Assert.AreEqual(3, pool.CountInactive);

            var a = pool.Get();
            var b = pool.Get();
            Assert.AreEqual(2, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);

            pool.Release(a);
            pool.Release(b);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(3, pool.CountInactive);
            Assert.AreEqual(3, created, "Released instances must be reused, not recreated.");
        }

        [Test]
        public void Tween_ReachesTargetAtDuration([Values] Ease ease)
        {
            var state = new TweenState(2f, 10f, 0.5f, ease);
            // 0.125 is exact in binary, so the test does not depend on float rounding.
            for (int i = 0; i < 3; i++) state.Step(0.125f);
            Assert.IsFalse(state.IsDone);

            state.Step(0.125f);
            Assert.IsTrue(state.IsDone);
            Assert.AreEqual(10f, state.Value);
        }

        [Test]
        public void StateMachine_FiresEnterExitAndPublishesTransition()
        {
            var log = new List<string>();
            StateChanged<Signal> lastEvent = default;
            System.Action<StateChanged<Signal>> onChanged = e => lastEvent = e;
            EventBus.Subscribe(onChanged);

            using (var fsm = new StateMachine<Signal>("Traffic"))
            {
                fsm.Configure(Signal.Red, enter: () => log.Add("enter red"), exit: () => log.Add("exit red"))
                   .Configure(Signal.Green, enter: () => log.Add("enter green"));
                fsm.Transition(Signal.Red);
                fsm.Transition(Signal.Green);

                CollectionAssert.AreEqual(new[] { "enter red", "exit red", "enter green" }, log);
                Assert.AreEqual(Signal.Green, fsm.Current);
                Assert.AreEqual(Signal.Red, lastEvent.Previous);
                Assert.AreEqual(Signal.Green, lastEvent.Current);
                Assert.AreEqual("Traffic", lastEvent.Machine);
            }
        }

        [Test]
        public void Timer_FiresAfterSeconds_AndEveryRepeats_WithManualTick()
        {
            var scheduler = new TimerScheduler();
            int once = 0, repeated = 0;
            scheduler.After(1f, () => once++);
            var every = scheduler.Every(0.25f, () => repeated++);

            scheduler.Tick(0.9f);
            Assert.AreEqual(0, once);
            scheduler.Tick(0.2f);
            Assert.AreEqual(1, once);
            Assert.AreEqual(4, repeated, "0.25 s repeat over 1.1 s fires 4 times.");

            every.Cancel();
            scheduler.Tick(1f);
            Assert.AreEqual(4, repeated);
            Assert.AreEqual(0, scheduler.Count);
        }

        [Test]
        public void Juice_PunchCurve_StartsAndEndsAtRest()
        {
            Assert.AreEqual(0f, Juice.PunchCurve(0f), 1e-5f);
            Assert.AreEqual(0f, Juice.PunchCurve(1f), 1e-5f);
            Assert.Greater(Mathf.Abs(Juice.PunchCurve(0.15f)), 0.3f);
        }

        [Test]
        public void ConfigAsset_ValidateRunsSanitize()
        {
            var config = ScriptableObject.CreateInstance<TestConfig>();
            try
            {
                config.Validate();
                Assert.AreEqual(0f, config.Speed);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void GameEvents_CarryTheirPayload()
        {
            GameOver received = default;
            System.Action<GameOver> handler = e => received = e;
            EventBus.Subscribe(handler);
            EventBus.Publish(new GameOver(won: true));

            Assert.IsTrue(received.Won);
            Assert.AreEqual(7, new ScoreChanged(7).Score);
            Assert.IsTrue(new GamePaused(true).Paused);
            Assert.AreEqual(3f, new PlayerDamaged(3f).Amount);
        }

        [Test]
        public void DebugOverlay_LogStoresLatestValue()
        {
            DebugOverlay.Log("test.speed", 3);
            DebugOverlay.Log("test.speed", 4.5f);
            Assert.IsTrue(DebugOverlay.TryGetValue("test.speed", out var value));
            Assert.AreEqual(4.5f.ToString(), value);
        }
    }
}
