using System.Collections;
using System.Collections.Generic;
using Kit.Gameplay.Portals;
using Kit.Gameplay.Thresholds;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Kit.Tests.PlayMode
{
    /// <summary>
    /// See-through thresholds (GDD F3): a paired threshold in view renders its partner's side; an
    /// unpaired one shows no portal surface, so the real room behind it is what you see.
    /// </summary>
    public class PortalViewTests
    {
        readonly List<Object> spawned = new List<Object>();

        Threshold MakeThreshold(string name, string number, Vector3 centre, Vector3 facing)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(facing));
            var surface = new GameObject("PortalSurface").transform;
            surface.SetParent(go.transform, false);
            var t = go.AddComponent<Threshold>();
            t.Setup(new Vector2(1f, 2.1f), false, surface);
            go.AddComponent<BrassPlate>().Setup(number, 0.1f);
            go.SetActive(true);
            spawned.Add(go);
            return t;
        }

        static bool SurfaceVisible(Threshold t)
        {
            foreach (var r in t.PortalSurfaceRoot.GetComponentsInChildren<Renderer>())
                if (r.enabled && r.gameObject.activeInHierarchy) return true;
            return false;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in spawned) if (o != null) Object.Destroy(o);
            spawned.Clear();
        }

        [UnityTest]
        public IEnumerator PairedThresholdInView_RendersItsPartner_UnpairedShowsNothing()
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 1.6f, -4f), Quaternion.identity);
            spawned.Add(camGo);

            var a = MakeThreshold("A", "12", new Vector3(0f, 1.05f, 0f), Vector3.back);
            MakeThreshold("B", "12", new Vector3(40f, 1.05f, 0f), Vector3.right);
            yield return null;
            yield return null;

            Assert.IsTrue(a.IsPaired);
            Assert.IsTrue(SurfaceVisible(a), "A paired threshold gets a portal surface. " + PortalDebug.Report());
            Assert.Greater(PortalSystem.ActiveViewCount, 0, "A paired threshold in view renders. " + PortalDebug.Report());

            // A third 12 cancels the pair: no surface, no rendering.
            MakeThreshold("C", "12", new Vector3(-40f, 1.05f, 0f), Vector3.left);
            yield return null;
            yield return null;
            Assert.IsFalse(a.IsPaired);
            Assert.IsFalse(SurfaceVisible(a), "Unpaired thresholds show the real space behind them.");
            Assert.AreEqual(0, PortalSystem.ActiveViewCount);
        }
    }
}
