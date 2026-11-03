using System.Collections.Generic;
using Kit.Gameplay.Signage;
using NUnit.Framework;
using UnityEngine;

namespace Kit.Tests.EditMode
{
    /// <summary>
    /// WorldText in edit mode, the way OfficeKit builders use it: Create/Set build a TextMesh with
    /// the configured font and style material, style changes swap the material, and the size
    /// formula hits the requested capital height. Works whether or not the Signage builder has
    /// run yet: with no style material the font's own material is the expected fallback.
    /// </summary>
    public class SignageTests
    {
        readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created) if (go != null) Object.DestroyImmediate(go);
            created.Clear();
        }

        WorldText Make(string content, float height, WorldTextStyle style)
        {
            var root = new GameObject("SignageTestRoot");
            created.Add(root);
            return WorldText.Create(root.transform, "Text", content, Vector3.zero, Quaternion.identity, height, style);
        }

        static Font ExpectedFont(WorldTextConfig cfg)
            => cfg != null && cfg.Font != null ? cfg.Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>The style material when the config has one, otherwise the font's default material.</summary>
        static Material ExpectedMaterial(WorldTextConfig cfg, WorldTextStyle style)
        {
            int i = (int)style;
            if (cfg != null && cfg.StyleMaterials != null && i < cfg.StyleMaterials.Length && cfg.StyleMaterials[i] != null)
                return cfg.StyleMaterials[i];
            return ExpectedFont(cfg).material;
        }

        [Test]
        public void Create_InEditMode_BuildsTextMeshWithFontAndStyleMaterial()
        {
            var cfg = WorldText.ResolveConfig();
            var wt = Make("RECEPTION\nFLOOR 2", 0.08f, WorldTextStyle.DarkOnLight);

            var tm = wt.GetComponent<TextMesh>();
            var mr = wt.GetComponent<MeshRenderer>();
            Assert.IsNotNull(tm, "TextMesh");
            Assert.IsNotNull(mr, "MeshRenderer");
            Assert.AreEqual("RECEPTION\nFLOOR 2", tm.text);
            Assert.AreEqual(ExpectedFont(cfg), tm.font);
            Assert.AreEqual(TextAlignment.Center, tm.alignment);
            Assert.AreEqual(TextAnchor.MiddleCenter, tm.anchor);
            Assert.IsFalse(tm.richText);
            Assert.AreEqual(ExpectedMaterial(cfg, WorldTextStyle.DarkOnLight), mr.sharedMaterial);
            Assert.AreEqual(UnityEngine.Rendering.ShadowCastingMode.Off, mr.shadowCastingMode);
        }

        [Test]
        public void StyleMaterial_GetsFontAtlasThroughPropertyBlock_NotTheSharedMaterial()
        {
            var cfg = WorldText.ResolveConfig();
            var wt = Make("4B", 0.1f, WorldTextStyle.Brass);
            var mr = wt.GetComponent<MeshRenderer>();
            var font = wt.GetComponent<TextMesh>().font;

            var block = new MaterialPropertyBlock();
            mr.GetPropertyBlock(block);
            if (mr.sharedMaterial == font.material)
            {
                Assert.IsTrue(block.isEmpty, "fallback font material needs no property block");
                return;
            }
            Assert.AreEqual(cfg.StyleMaterials[(int)WorldTextStyle.Brass], mr.sharedMaterial);
            Assert.AreEqual(font.material.mainTexture, block.GetTexture("_MainTex"));
        }

        [Test]
        public void ChangingStyle_SwapsMaterial()
        {
            var cfg = WorldText.ResolveConfig();
            var wt = Make("EXIT", 0.12f, WorldTextStyle.DarkOnLight);
            var mr = wt.GetComponent<MeshRenderer>();

            foreach (WorldTextStyle style in System.Enum.GetValues(typeof(WorldTextStyle)))
            {
                wt.Set("EXIT", 0.12f, style);
                Assert.AreEqual(style, wt.Style);
                Assert.AreEqual(ExpectedMaterial(cfg, style), mr.sharedMaterial, style.ToString());
            }

            var dark = ExpectedMaterial(cfg, WorldTextStyle.DarkOnLight);
            var glow = ExpectedMaterial(cfg, WorldTextStyle.Glow);
            if (dark != glow)
            {
                wt.Set("EXIT", 0.12f, WorldTextStyle.DarkOnLight);
                Assert.AreEqual(dark, mr.sharedMaterial);
                wt.Set("EXIT", 0.12f, WorldTextStyle.Glow);
                Assert.AreEqual(glow, mr.sharedMaterial);
            }
        }

        [TestCase(0.01f)]
        [TestCase(0.05f)]
        [TestCase(0.1f)]
        [TestCase(0.3f)]
        [TestCase(1.5f)]
        public void Sizing_FollowsFormula_AndHitsCapHeight(float letterHeight)
        {
            var cfg = WorldText.ResolveConfig();
            var wt = Make("H", letterHeight, WorldTextStyle.LightOnDark);
            var tm = wt.GetComponent<TextMesh>();

            int expectedSize = Mathf.Clamp(Mathf.RoundToInt(letterHeight * cfg.PixelsPerMetre), cfg.MinFontSize, cfg.MaxFontSize);
            Assert.AreEqual(expectedSize, tm.fontSize, "fontSize");
            Assert.That(tm.fontSize, Is.InRange(cfg.MinFontSize, cfg.MaxFontSize));

            float cap = WorldText.CapHeightFor(cfg, tm.fontSize, tm.characterSize);
            Assert.That(cap, Is.EqualTo(letterHeight).Within(letterHeight * 0.15f), "cap height");
        }

        [Test]
        public void ComputeSizing_DefaultConfig_ClampsFontSize()
        {
            var cfg = ScriptableObject.CreateInstance<WorldTextConfig>();
            try
            {
                cfg.Validate();
                WorldText.ComputeSizing(cfg, 0.001f, out int small, out float smallChar);
                WorldText.ComputeSizing(cfg, 10f, out int big, out float bigChar);
                Assert.AreEqual(16, small);
                Assert.AreEqual(160, big);
                Assert.That(WorldText.CapHeightFor(cfg, small, smallChar), Is.EqualTo(0.001f).Within(0.0001f));
                Assert.That(WorldText.CapHeightFor(cfg, big, bigChar), Is.EqualTo(10f).Within(0.01f));
                Assert.AreEqual(4, cfg.StyleMaterials.Length);
            }
            finally
            {
                Object.DestroyImmediate(cfg);
            }
        }

        [Test]
        public void Set_ClampsTinyHeight_AndKeepsAnchor()
        {
            var wt = Make("x", 0.1f, WorldTextStyle.DarkOnLight);
            wt.Set(null, -1f, WorldTextStyle.DarkOnLight, TextAnchor.UpperLeft);
            Assert.AreEqual("", wt.GetComponent<TextMesh>().text);
            Assert.Greater(wt.LetterHeight, 0f);
            Assert.AreEqual(TextAnchor.UpperLeft, wt.GetComponent<TextMesh>().anchor);
        }
    }
}
