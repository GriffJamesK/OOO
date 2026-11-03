using System.Collections;
using System.Linq;
using Kit.Core.Events;
using Kit.Gameplay;
using Kit.Gameplay.Letterboards;
using Kit.Gameplay.Thresholds;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// The building's routes in the real Office scene (GDD F10, F11): every planned pair can be
    /// walked through from its front and arrives clear of walls in the right area, the launch
    /// lands on the boardroom pod, and the expert route (spawn -> door 1 -> letter O's -> the
    /// cloakroom set to OOO -> outside) completes the game. Movement is driven by setting the
    /// player's velocity (a small hop so ground friction doesn't stop it), not by input.
    /// </summary>
    public class OfficeRouteTests
    {
        GameManager gm;
        PlayerController player;

        IEnumerator BootAndPlay()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            gm = Object.FindAnyObjectByType<GameManager>();
            float until = Time.realtimeSinceStartup + 30f;
            while (gm.State == GameState.Loading && Time.realtimeSinceStartup < until) yield return null;
            EventBus.Publish(new PlayRequested());
            yield return null;
            player = gm.Player;
            Assert.IsNotNull(player, "Boot should spawn the player in the Office.");
        }

        static Threshold Find(string number, System.Func<Threshold, bool> extra = null) =>
            ThresholdRegistry.All.FirstOrDefault(t => t != null && t.Number == number && (extra == null || extra(t)));

        static bool Overlapping(PlayerController p)
        {
            var cc = p.GetComponent<CharacterController>();
            Vector3 a = p.transform.position + Vector3.up * (cc.radius + 0.05f);
            Vector3 b = p.transform.position + Vector3.up * (cc.height - cc.radius - 0.05f);
            foreach (var c in Physics.OverlapCapsule(a, b, cc.radius * 0.9f, ~0, QueryTriggerInteraction.Ignore))
                if (c != cc && !c.transform.IsChildOf(p.transform)) return true;
            return false;
        }

        /// <summary>Hops the player through <paramref name="entry"/> from 0.7 m in front of its centre; returns the threshold it came out of (or null).</summary>
        IEnumerator CrossFromFront(Threshold entry, System.Action<Threshold> result)
        {
            var cc = player.GetComponent<CharacterController>();
            Vector3 front = entry.transform.forward;
            Vector3 start = entry.transform.position + front * 0.7f;
            Vector3 feet = new Vector3(start.x, entry.transform.position.y - entry.Size.y * 0.5f + 0.05f, start.z);
            gm.RespawnPlayer(new Pose(feet, Quaternion.LookRotation(-new Vector3(front.x, 0f, front.z))));
            yield return null;

            Threshold arrived = null;
            System.Action<PlayerCrossedThreshold> onCross = e => arrived = e.To;
            EventBus.Subscribe(onCross);
            Vector3 dir = -new Vector3(front.x, 0f, front.z).normalized;
            float until = Time.time + 1.5f;
            while (arrived == null && Time.time < until)
            {
                player.SetVelocity(dir * 4f + Vector3.up * (player.IsGrounded ? 2.5f : player.VerticalSpeed));
                yield return null;
            }
            EventBus.Unsubscribe(onCross);
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsFalse(Overlapping(player), $"After crossing {entry.name} the player is inside geometry at {player.transform.position} (capsule {cc.height} m).");
            result(arrived);
        }

        [UnityTest]
        public IEnumerator EveryPlannedPair_CanBeWalkedThrough_FromItsFront()
        {
            yield return BootAndPlay();
            // 412 is a 1.25 m slide-through hatch: a standing hop can't fit, so it is checked by size below.
            foreach (var number in new[] { "104", "1", "37", "312" })
            {
                var pair = ThresholdRegistry.All.Where(t => t != null && t.Number == number).ToList();
                Assert.AreEqual(2, pair.Count, $"pair {number}");
                foreach (var entry in pair)
                {
                    Threshold arrived = null;
                    yield return CrossFromFront(entry, t => arrived = t);
                    Assert.AreSame(entry.Partner, arrived, $"Walking into {entry.name} ({number}) from its front should come out of {entry.Partner?.name}. Player at {player.transform.position}");
                }
            }

            var hatch = ThresholdRegistry.All.Where(t => t != null && t.Number == "412").ToList();
            Assert.AreEqual(2, hatch.Count, "pair 412");
            Assert.AreEqual(hatch[0].Size, hatch[1].Size, "412 openings match");
            Assert.Greater(hatch[0].Size.y, 1.05f, "a sliding capsule (1.0 m) fits through 412");
        }

        [UnityTest]
        public IEnumerator DroppingFromTheStairTop_LaunchesOntoThePod()
        {
            yield return BootAndPlay();
            var hatch = Find("6", t => t.transform.forward.y > 0.9f);
            Assert.IsNotNull(hatch, "the floor hatch 6");
            gm.RespawnPlayer(new Pose(new Vector3(hatch.transform.position.x, 24.05f, hatch.transform.position.z), Quaternion.identity));
            float until = Time.time + 6f;
            bool landed = false;
            yield return new WaitForSeconds(1.5f);
            while (Time.time < until)
            {
                if (player.IsGrounded) { landed = true; break; }
                yield return null;
            }
            Assert.IsTrue(landed, "The launch should land somewhere. " + PlayerDebug.Report());
            var pos = player.transform.position;
            Assert.AreEqual(24f, pos.y, 0.3f, $"Lands on the pod's top (y 24): {pos}");
            Assert.IsTrue(pos.x > 0f && pos.x < 20f && Mathf.Abs(pos.z) < 6f, $"Lands on the pod (x 0..20, z -6..6): {pos}");
        }

        [UnityTest]
        public IEnumerator ExpertRoute_DoorOne_LetterOs_Cloakroom_Out()
        {
            yield return BootAndPlay();

            // 1. Spawn -> reception door 1 -> the lobby.
            var doorOne = Find("1", t => t.transform.position.y > 10f);
            Threshold arrived = null;
            yield return CrossFromFront(doorOne, t => arrived = t);
            Assert.AreSame(doorOne.Partner, arrived, "Door 1 leads to the lobby.");
            Assert.AreEqual("A2", AreaZone.Current, "Now in the atrium lobby.");

            // 2. Three letter O's. They come from any board holding an O (the lobby directory is
            // the intended source); taking them by the carrier checks the slots really hold O tiles.
            var carrier = player.GetComponent<TileCarrier>();
            Assert.IsNotNull(carrier);
            carrier.Clear();
            var boards = Object.FindObjectsByType<Letterboard>().Where(b => b.GetComponentInParent<Threshold>() == null).ToList();
            foreach (var board in boards)
            {
                foreach (var slot in board.GetComponentsInChildren<LetterSlot>())
                {
                    if (carrier.Carried.Count == 3) break;
                    if (board.GlyphAt(slot.Index) != 'O') continue;
                    Vector3 eye = slot.transform.position + board.transform.forward * 1.2f;
                    carrier.InteractAlong(eye, -board.transform.forward);
                }
                if (carrier.Carried.Count == 3) break;
            }
            CollectionAssert.AreEqual(new[] { 'O', 'O', 'O' }, carrier.Carried, "Three letter O's are available on the signs.");

            // 3. Press them into the blank cloakroom plate, from where a player stands.
            var cloak = ThresholdRegistry.All.First(t => t != null && t.GetComponentInChildren<Letterboard>() != null && t.Number == "" && t.transform.position.y < 3f);
            var plate = cloak.GetComponentInChildren<Letterboard>();
            Physics.SyncTransforms();
            foreach (var slot in plate.GetComponentsInChildren<LetterSlot>())
            {
                Vector3 eye = cloak.transform.position + cloak.transform.forward * 1.0f + Vector3.up * (1.6f - cloak.Size.y * 0.5f);
                Assert.Less(Vector3.Distance(eye, slot.transform.position), ConfigLookup.Get<LetterboardConfig>().Reach, "The plate is within reach of a standing player.");
                Assert.IsTrue(carrier.InteractAlong(eye, slot.transform.position - eye), $"Place a tile in {slot.name}");
            }
            Assert.AreEqual("000", cloak.Number);
            Assert.IsNotNull(cloak.Partner);
            Assert.IsTrue(cloak.Partner.Exterior, "OOO pairs the cloakroom with the outside door.");

            // 4. Walk out.
            bool completed = false;
            System.Action<GameCompleted> onDone = _ => completed = true;
            EventBus.Subscribe(onDone);
            yield return CrossFromFront(cloak, t => arrived = t);
            Assert.IsTrue(arrived != null && arrived.Exterior, "Walking through the cloakroom door arrives outside.");
            float until = Time.time + ConfigLookup.Get<Kit.Gameplay.Flow.FlowConfig>().EndingTitleDelay + 2f;
            while (!completed && Time.time < until) yield return null;
            EventBus.Unsubscribe(onDone);
            Assert.IsTrue(completed, "The ending fires.");
            Assert.AreEqual(GameState.Playing, gm.State, "No game over: play continues outside.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var manager = Object.FindAnyObjectByType<GameManager>();
            float until = Time.realtimeSinceStartup + 30f;
            while (manager != null && manager.State == GameState.Loading && Time.realtimeSinceStartup < until) yield return null;
            Time.timeScale = 1f;
            var empty = SceneManager.CreateScene("TestEmpty");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s != empty && s.isLoaded) yield return SceneManager.UnloadSceneAsync(s);
            }
        }
    }
}
