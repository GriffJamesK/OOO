using System.Collections;
using System.Collections.Generic;
using Kit.Core.Events;
using Kit.Gameplay;
using Kit.Gameplay.Letterboards;
using Kit.Gameplay.Thresholds;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// Moving letterboard tiles rewires the building (GDD F5): pull the 7 off one plate, press it
    /// into a blank plate, and the blank one now pairs with the other 7, on the same frame.
    /// </summary>
    public class LetterboardTests
    {
        readonly List<Object> spawned = new List<Object>();

        Threshold MakeThreshold(string name, Vector3 centre, string brass, string letterboard)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(Vector3.back));
            var t = go.AddComponent<Threshold>();
            t.Setup(new Vector2(1f, 2.1f), false, null);
            var plate = new GameObject("Plate");
            plate.transform.SetParent(go.transform, false);
            plate.transform.localPosition = new Vector3(0f, 1.3f, 0.1f);
            if (letterboard != null) plate.AddComponent<Letterboard>().Setup(1, letterboard.Length, letterboard);
            else plate.AddComponent<BrassPlate>().Setup(brass, 0.1f);
            go.SetActive(true);
            spawned.Add(go);
            return t;
        }

        static LetterSlot FirstSlot(Threshold t) => t.GetComponentInChildren<LetterSlot>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.Destroy(o);
            spawned.Clear();
        }

        [Test]
        public void Letterboard_PullAndPlace()
        {
            var go = new GameObject("Board");
            spawned.Add(go);
            var board = go.AddComponent<Letterboard>();
            board.Setup(2, 3, "AB_\nC");
            Assert.AreEqual(6, board.SlotCount);
            Assert.AreEqual('A', board.GlyphAt(0));
            Assert.AreEqual(' ', board.GlyphAt(2), "'_' is an empty slot");
            Assert.AreEqual('C', board.GlyphAt(3));
            Assert.AreEqual(' ', board.GlyphAt(5), "short rows are padded with empty slots");

            int changes = 0;
            board.Changed += () => changes++;
            Assert.IsTrue(board.TryPull(0, out char g));
            Assert.AreEqual('A', g);
            Assert.IsFalse(board.TryPull(0, out _), "an empty slot has nothing to pull");
            Assert.IsFalse(board.TryPlace(1, 'Z'), "a full slot takes nothing");
            Assert.IsTrue(board.TryPlace(2, 'A'));
            Assert.AreEqual('A', board.GlyphAt(2));
            Assert.AreEqual(2, changes);
            Assert.AreEqual(6, board.GetComponentsInChildren<LetterSlot>().Length, "one interaction collider per slot");
        }

        [UnityTest]
        public IEnumerator MovingATile_RewiresThePairs()
        {
            var brass = MakeThreshold("Brass7", new Vector3(0f, 1.05f, 0f), "7", null);
            var source = MakeThreshold("Board7", new Vector3(10f, 1.05f, 0f), null, "7");
            var blank = MakeThreshold("Blank", new Vector3(20f, 1.05f, 0f), null, "_");
            yield return null;
            Assert.AreSame(source, brass.Partner, "brass 7 starts paired with the letterboard 7. " + ThresholdDebug.Report());

            var carrierGo = new GameObject("Carrier");
            spawned.Add(carrierGo);
            var carrier = carrierGo.AddComponent<TileCarrier>();
            int repairs = 0;
            System.Action<ThresholdsRepaired> onRepair = _ => repairs++;
            EventBus.Subscribe(onRepair);

            var slotA = FirstSlot(source);
            Assert.IsNotNull(slotA, "letterboard plates need LetterSlot colliders");
            Physics.SyncTransforms();
            Assert.IsTrue(carrier.InteractAlong(slotA.transform.position + Vector3.back * 1.2f, Vector3.forward), "pull the 7");
            CollectionAssert.AreEqual(new[] { '7' }, carrier.Carried);
            Assert.IsNull(brass.Partner, "with the tile gone, brass 7 is alone");

            var slotB = FirstSlot(blank);
            Assert.IsTrue(carrier.InteractAlong(slotB.transform.position + Vector3.back * 1.2f, Vector3.forward), "place the 7");
            Assert.AreEqual(0, carrier.Carried.Count);
            Assert.AreSame(blank, brass.Partner, "the blank plate now reads 7 and pairs with brass 7");
            Assert.GreaterOrEqual(repairs, 2);
            EventBus.Unsubscribe(onRepair);
        }

        [Test]
        public void Carrier_HoldsAtMostMaxCarried()
        {
            var go = new GameObject("Carrier");
            spawned.Add(go);
            var carrier = go.AddComponent<TileCarrier>();
            int max = ConfigLookup.Get<LetterboardConfig>().MaxCarried;
            for (int i = 0; i < max; i++) Assert.IsTrue(carrier.Give('O'));
            Assert.IsFalse(carrier.Give('O'), "hand is full");
            Assert.AreEqual(max, carrier.Carried.Count);
        }
    }
}
