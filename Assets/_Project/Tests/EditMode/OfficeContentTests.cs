using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kit.Gameplay;
using Kit.Gameplay.Letterboards;
using Kit.Gameplay.Signage;
using Kit.Gameplay.Thresholds;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kit.Tests.EditMode
{
    /// <summary>
    /// Checks the built Office scene against the plan (GDD F13, ARCHITECTURE number plan and
    /// sockets): only planned threshold numbers, the planned pairs, enough lived-in text per area,
    /// text that never speaks to the player, and every socket where areas meet left open.
    /// Run after Kit > Build Office.
    /// </summary>
    public class OfficeContentTests
    {
        const string OfficePath = "Assets/_Project/Scenes/Office.unity";
        Scene previous;
        string previousPath;

        [OneTimeSetUp]
        public void OpenOffice()
        {
            previous = SceneManager.GetActiveScene();
            previousPath = previous.path;
            EditorSceneManager.OpenScene(OfficePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        [OneTimeTearDown]
        public void Restore()
        {
            if (!string.IsNullOrEmpty(previousPath)) EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }

        static List<Threshold> Thresholds() => Object.FindObjectsByType<Threshold>(FindObjectsInactive.Exclude).ToList();

        static string NumberOf(Threshold t)
        {
            var source = t.GetComponentInChildren<INumberSource>(true);
            return ThresholdNumber.Normalize(source != null ? source.Raw : "");
        }

        [Test]
        public void ThresholdNumbers_FollowThePlan()
        {
            var counts = new Dictionary<string, int>();
            foreach (var t in Thresholds())
            {
                string n = NumberOf(t);
                counts[n] = counts.TryGetValue(n, out var c) ? c + 1 : 1;
            }
            string all = string.Join(", ", counts.Select(kv => $"{(kv.Key == "" ? "(blank)" : kv.Key)}x{kv.Value}"));

            foreach (var pair in new[] { "104", "1", "37", "312", "412", "6" })
                Assert.AreEqual(2, counts.TryGetValue(pair, out var c) ? c : 0, $"{pair} must appear exactly twice (a pair). Found: {all}");
            Assert.AreEqual(1, counts.TryGetValue("000", out var z) ? z : 0, $"Only the exterior door reads 000 at the start. Found: {all}");
            Assert.GreaterOrEqual(counts.TryGetValue("", out var b) ? b : 0, 1, "The cloakroom starts blank.");

            var planned = new HashSet<string> { "104", "1", "37", "312", "412", "6", "000", "" };
            foreach (var kv in counts)
            {
                if (planned.Contains(kv.Key)) continue;
                Assert.AreEqual(1, kv.Value, $"Unplanned number {kv.Key} must be unique (it would pair). Found: {all}");
                bool aisle = int.TryParse(kv.Key, out int v) && v >= 31 && v <= 42;
                bool ranged = int.TryParse(kv.Key, out v) && v >= 101 && v <= 699;
                Assert.IsTrue(aisle || ranged, $"{kv.Key} is outside every area's range. Found: {all}");
            }

            var exterior = Thresholds().Where(t => t.Exterior).ToList();
            Assert.AreEqual(1, exterior.Count, "Exactly one exterior door.");
            Assert.AreEqual("000", NumberOf(exterior[0]));
        }

        [Test]
        public void EveryArea_HasLivedInText()
        {
            var texts = Object.FindObjectsByType<WorldText>(FindObjectsInactive.Exclude)
                .Where(w => w.GetComponentInParent<Threshold>() == null && w.GetComponentInParent<Letterboard>() == null)
                .ToList();
            foreach (var zone in Object.FindObjectsByType<AreaZone>(FindObjectsInactive.Exclude))
            {
                if (!Regex.IsMatch(zone.AreaId, "^A[1-6]$")) continue;
                int n = texts.Count(w => zone.Bounds.Contains(w.transform.position));
                Assert.GreaterOrEqual(n, 8, $"{zone.AreaId} needs at least 8 diegetic text props (has {n}).");
            }
        }

        [Test]
        public void Text_NeverSpeaksToThePlayer()
        {
            var banned = new Regex(@"\b(press|click|wasd|controller|teleport|portal|tutorial|jump|wall ?run|slide under|interact)\b", RegexOptions.IgnoreCase);
            foreach (var w in Object.FindObjectsByType<WorldText>(FindObjectsInactive.Exclude))
                Assert.IsFalse(banned.IsMatch(w.Text ?? ""), $"'{w.Text}' ({w.name}) reads like instructions to the player.");
        }

        [Test]
        public void Sockets_AreOpen()
        {
            var sockets = new (string name, Vector3 centre, Vector3 dir)[]
            {
                ("A1_Transom", new Vector3(-20f, 18.6f, 2f), Vector3.left),
                ("A1_Shutter", new Vector3(-20f, 16.6f, -4.5f), Vector3.left),
                ("A3_Entry", new Vector3(0f, 13.5f, 30f), Vector3.forward),
                ("A4_Entry", new Vector3(20f, 5.5f, 0f), Vector3.right),
                ("A5_L0", new Vector3(-20f, 1.5f, -24f), Vector3.left),
                ("A5_L2", new Vector3(-20f, 9.5f, -24f), Vector3.left),
                ("A6_Bridge", new Vector3(20f, 25.5f, 0f), Vector3.right),
            };
            foreach (var (name, centre, dir) in sockets)
            {
                var start = centre - dir * 1.2f;
                bool blocked = Physics.Raycast(start, dir, out var hit, 2.4f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsFalse(blocked, $"Socket {name} is blocked by {(blocked ? hit.collider.name : "")} at {(blocked ? hit.point.ToString("F2") : "")}");
            }
        }
    }
}
