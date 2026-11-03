using Kit.Gameplay.Signage;
using Kit.Gameplay.Thresholds;
using UnityEngine;
using UnityEngine.Rendering;
using OKit = Kit.Editor.OfficeKit;
using OMat = Kit.Editor.OfficeKit.M;
using OProp = Kit.Editor.OfficeKit.PropIds;

namespace Kit.Editor
{
    /// <summary>
    /// A7 Outside, the ending (playdead_01, staged like Kentucky Route Zero): a vast pale plain in
    /// white-grey haze. A single free-standing door frame stands alone on it under a warm spot,
    /// brass threshold 000 on its east face (the exterior threshold: arriving through it ends the
    /// game) and big brass "000" on its back, readable from the A6 boardroom window high up to the
    /// west. Eastward, a line of dark lamp posts (one barely lit), a bench, a lone shell chair and a
    /// far low wall fade into fog; a pale sun disc emerges only for those who keep walking. Looking
    /// back west, the building is a dark brutalist mass, kept low behind the door so the window's
    /// sightline stays clear.
    /// </summary>
    public static partial class OfficeBuilder
    {
        static readonly Color A7_Warm = new Color(1f, 0.8f, 0.58f);
        static readonly Color A7_FogColour = new Color(0.788f, 0.804f, 0.816f); // #C9CDD0

        // The stone stoop under the door: flush with the plain, cut into the ground slab.
        static readonly Vector4 A7_StoopRect = new Vector4(110.4f, -1.4f, 113.6f, 1.4f);

        /// <summary>Builds the outside under <paramref name="root"/> (world space).</summary>
        static partial void BuildArea_A7(Transform root)
        {
            OKit.Zone(root, "A7", OfficeLayout.A7);
            A7_Ground(root);
            A7_Door(root);
            A7_GroundNumerals(root);
            A7_Building(root);
            A7_Silhouettes(root);
            A7_SunDisc(root);

            // Pale haze over the whole outside, down into the fall void too so a fall stays pale.
            var fog = new Bounds();
            fog.SetMinMax(new Vector3(72f, -26f, -220f), new Vector3(440f, 60f, 220f));
            OKit.FogZone(root, "A7_Outside", fog, A7_FogColour, 4f, 110f, 3f, 10f);
        }

        /// <summary>
        /// The pale ground (top at y 0) with the door's stoop cut flush into it, and a fall zone under
        /// everything so walking off any edge returns the player to the last safe ground.
        /// </summary>
        static void A7_Ground(Transform root)
        {
            OKit.Slab(root, "A7_Ground", OMat.Exterior, new Vector3(72f, 0f, -200f), new Vector3(420f, 0f, 200f), 0f, 1f, A7_StoopRect);
            OKit.Box(root, "A7_Stoop", OMat.ConcreteDark, new Vector3(A7_StoopRect.x, -1f, A7_StoopRect.y), new Vector3(A7_StoopRect.z, 0f, A7_StoopRect.w));
            // Brass nosings at the stoop's front and back edges (5 mm proud, walk-over).
            OKit.Box(root, "A7_Stoop_NosingW", OMat.Brass, new Vector3(110.4f, 0f, -1.4f), new Vector3(110.46f, 0.005f, 1.4f), false);
            OKit.Box(root, "A7_Stoop_NosingE", OMat.Brass, new Vector3(113.54f, 0f, -1.4f), new Vector3(113.6f, 0.005f, 1.4f), false);

            var fall = new Bounds();
            fall.SetMinMax(new Vector3(40f, -25f, -260f), new Vector3(520f, -15f, 260f));
            OKit.FallZone(root, "A7_FallZone", fall);
        }

        /// <summary>
        /// The door 000: the exterior threshold (OfficeLayout's centre, rotation and Door size, front
        /// facing +X), a dark wood frame inside a dark concrete surround about 3.2 m tall, big brass
        /// "000" on the lintel's back for the boardroom window, a shadowed warm spot from above and a
        /// faint beam, plus a small spot washing the back lettering.
        /// </summary>
        static void A7_Door(Transform root)
        {
            var t = new GameObject("A7_Door000").transform;
            t.SetParent(root, false);

            OKit.Threshold(t, "A7_Threshold_000", OfficeLayout.ExteriorDoorCentre, OfficeLayout.ExteriorDoorRotation, OfficeLayout.Door, "000",
                null, true, false, OMat.WoodDark);

            // Surround: pillars beside the frame, a deep lintel above it, a thin overhanging cap. Its
            // front face stops just behind the threshold's brass plate (x 112.135).
            const float x0 = 111.6f, x1 = 112.12f;
            float fw = OKit.Config<ThresholdConfig>().FrameWidth;
            float jamb = OfficeLayout.Door.x * 0.5f + fw; // the frame's jamb outer edge
            float head = OfficeLayout.Door.y + fw;        // top of the frame's head
            OKit.Box(t, "A7_Surround_PillarN", OMat.ConcreteDark, new Vector3(x0, 0f, jamb), new Vector3(x1, 3.2f, 1f));
            OKit.Box(t, "A7_Surround_PillarS", OMat.ConcreteDark, new Vector3(x0, 0f, -1f), new Vector3(x1, 3.2f, -jamb));
            OKit.Box(t, "A7_Surround_Lintel", OMat.ConcreteDark, new Vector3(x0, head, -jamb), new Vector3(x1, 3.2f, jamb));
            OKit.Box(t, "A7_Surround_Cap", OMat.ConcreteDark, new Vector3(111.5f, 3.2f, -1.1f), new Vector3(112.22f, 3.36f, 1.1f));

            // Back lettering, facing the building (-X): 0.72 m digits across the lintel and pillars.
            OKit.Sign(t, "000", new Vector3(x0 - 0.015f, 2.71f, 0f), OKit.Facing(Vector3.left), 0.72f, WorldTextStyle.Brass);

            // The KRZ spot: high, slightly in front, aimed at the doorway; shadows on.
            var spotPos = new Vector3(113.4f, 9.5f, 0f);
            var aim = new Vector3(112f, 0.8f, 0f) - spotPos;
            OKit.SpotLight(t, "A7_Door_Spot", spotPos, OKit.Facing(aim), A7_Warm, 60f, 16f, 30f, true);
            OKit.LightShaft(t, "A7_Door_Beam", spotPos, Quaternion.FromToRotation(Vector3.up, -aim.normalized), new Vector3(2.4f, 9.6f, 2.4f),
                new Color(1f, 0.85f, 0.65f, 0.22f));

            var washPos = new Vector3(108.6f, 4.6f, 0f);
            OKit.SpotLight(t, "A7_Sign_Wash", washPos, OKit.Facing(new Vector3(x0, 2.71f, 0f) - washPos), A7_Warm, 4f, 7f, 34f, false);
        }

        // Ground numerals "000": glyph size and placement (world metres). The glyph's "up" is +X.
        const float A7_GlyphCentreX = 106.8f;  // x 104.6..109: A4's roof edge (y 18 at x 72) hides the ground nearer than about x 103.5 from the window
        const float A7_GlyphLength = 4.4f;     // along X (the glyph's height), long to offset foreshortening from 25 m up
        const float A7_GlyphWidth = 2.3f;      // along Z
        const float A7_GlyphStroke = 0.55f;
        const float A7_GlyphPitch = 3.1f;      // centre-to-centre along Z
        const int A7_GlyphSegments = 32;

        /// <summary>
        /// Three zeros set flush into the pale ground in dark slate, between the building and the
        /// door, like an old site marking. They're the readable form of the door's number from the
        /// A6 boardroom window (the brass plate is only a few pixels from there). Each zero is a
        /// rounded superellipse ring of slate segments, glyph "up" toward +X so it reads correctly
        /// looking east and down. No colliders: the ground slab collides and the tops sit 3 mm
        /// proud, so nothing trips.
        /// </summary>
        static void A7_GroundNumerals(Transform root)
        {
            var t = new GameObject("A7_GroundNumerals").transform;
            t.SetParent(root, false);
            for (int g = -1; g <= 1; g++)
                A7_ZeroGlyph(t, $"A7_Zero_{g + 1}", new Vector3(A7_GlyphCentreX, 0f, g * A7_GlyphPitch));
        }

        /// <summary>
        /// One flush "0": boxes laid along the centreline of a superellipse (exponent 2.6, so it has
        /// a typeface digit's straight-ish sides and round ends), each overlapping its neighbours
        /// so the ring has no gaps.
        /// </summary>
        static void A7_ZeroGlyph(Transform parent, string name, Vector3 centre)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            float a = (A7_GlyphLength - A7_GlyphStroke) * 0.5f; // centreline half-extent along X
            float b = (A7_GlyphWidth - A7_GlyphStroke) * 0.5f;  // centreline half-extent along Z
            const float e = 2f / 2.6f;
            const float top = 0.003f, depth = 0.05f;

            Vector3 Point(int i)
            {
                float th = i * Mathf.PI * 2f / A7_GlyphSegments;
                float c = Mathf.Cos(th), s = Mathf.Sin(th);
                return centre + new Vector3(a * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e), 0f, b * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e));
            }

            for (int i = 0; i < A7_GlyphSegments; i++)
            {
                Vector3 p0 = Point(i), p1 = Point(i + 1);
                Vector3 dir = p1 - p0;
                float len = dir.magnitude + A7_GlyphStroke * 0.35f; // overlap fills the outer corner at each joint
                var mid = (p0 + p1) * 0.5f + Vector3.up * (top - depth * 0.5f);
                OKit.BoxAt(t, $"Seg_{i:00}", OMat.Slate, mid, new Vector3(A7_GlyphStroke, depth, len), Quaternion.LookRotation(dir.normalized, Vector3.up), false);
            }
        }

        /// <summary>
        /// The building seen from the plain: dark concrete towers and wings at x 72..80, cantilevered
        /// slabs and vertical fins. Behind the door (z -12..12) only a 9 m plinth, so the line from the
        /// A6 window (x 60, y 24..28) down to the door (x 112) passes well above it.
        /// </summary>
        static void A7_Building(Transform root)
        {
            var t = new GameObject("A7_Building").transform;
            t.SetParent(root, false);

            OKit.Box(t, "A7_Mass_Plinth", OMat.Concrete, new Vector3(72f, 0f, -12f), new Vector3(80f, 9f, 12f));
            OKit.Box(t, "A7_Mass_TowerN", OMat.ConcreteDark, new Vector3(72f, 0f, 12f), new Vector3(80f, 34f, 28f));
            OKit.Box(t, "A7_Mass_TowerS", OMat.ConcreteDark, new Vector3(72f, 0f, -28f), new Vector3(80f, 34f, -12f));
            OKit.Box(t, "A7_Mass_BlockN", OMat.ConcreteDark, new Vector3(72f, 0f, 28f), new Vector3(78f, 27f, 60f));
            OKit.Box(t, "A7_Mass_BlockS", OMat.ConcreteDark, new Vector3(72f, 0f, -60f), new Vector3(78f, 23f, -28f));
            OKit.Box(t, "A7_Mass_SlabN", OMat.ConcreteDark, new Vector3(72f, 30f, 12f), new Vector3(84f, 31.6f, 46f));
            OKit.Box(t, "A7_Mass_SlabS", OMat.ConcreteDark, new Vector3(72f, 25f, -50f), new Vector3(83f, 26.4f, -12f));
            // Long low wings closing the west edge of the plain beyond the main block.
            OKit.Box(t, "A7_Mass_WingN", OMat.ConcreteDark, new Vector3(72f, 0f, 60f), new Vector3(76f, 14f, 200f));
            OKit.Box(t, "A7_Mass_WingS", OMat.ConcreteDark, new Vector3(72f, 0f, -200f), new Vector3(76f, 12f, -60f));

            // Vertical fins on the towers' east faces.
            for (int i = 0; i < 8; i++)
            {
                float z = 13f + i * 2f;
                OKit.Box(t, $"A7_FinN_{i}", OMat.Concrete, new Vector3(80f, 2f, z - 0.2f), new Vector3(80.6f, 32f, z + 0.2f), false);
                OKit.Box(t, $"A7_FinS_{i}", OMat.Concrete, new Vector3(80f, 2f, -z - 0.2f), new Vector3(80.6f, 32f, -z + 0.2f), false);
            }
        }

        /// <summary>
        /// Sparse silhouettes heading east into the haze: six dark lamp posts in a line (one barely
        /// lit, a shell chair under it), a bench facing east, a lone tree, and a far low wall with a
        /// gap on the lamp line and a small brass plaque.
        /// </summary>
        static void A7_Silhouettes(Transform root)
        {
            var t = new GameObject("A7_Silhouettes").transform;
            t.SetParent(root, false);

            for (int i = 0; i < 6; i++)
            {
                float x = 134f + i * 22f;
                A7_LampPost(t, $"A7_LampPost_{i}", new Vector3(x, 0f, 4.5f), i == 2);
            }
            OKit.Prop(t, OProp.ShellChair, new Vector3(179.2f, 0f, 2.6f), 205f);
            OKit.Prop(t, OProp.Bench, new Vector3(190f, 0f, -3.5f), 90f);
            OKit.Prop(t, OProp.Tree, new Vector3(232f, 0f, -24f), 0f, 1.3f);

            OKit.Box(t, "A7_LowWall_S", OMat.Concrete, new Vector3(279f, 0f, -70f), new Vector3(280.2f, 1.1f, -3f));
            OKit.Box(t, "A7_LowWall_N", OMat.Concrete, new Vector3(279f, 0f, 3f), new Vector3(280.2f, 1.1f, 70f));
            OKit.Sign(t, "HALE & ORMSBY\nPROPERTY LINE", new Vector3(278.985f, 0.72f, 5.2f), OKit.Facing(Vector3.left), 0.05f,
                WorldTextStyle.Brass, TextAnchor.MiddleCenter, OMat.ConcreteDark, new Vector2(0.52f, 0.18f));
        }

        /// <summary>A dark lamp post (base, pole, arm, head over the path); <paramref name="lit"/> gives it a faint warm glow.</summary>
        static void A7_LampPost(Transform parent, string name, Vector3 b, bool lit)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            OKit.Box(t, "Base", OMat.MetalDark, new Vector3(b.x - 0.16f, 0f, b.z - 0.16f), new Vector3(b.x + 0.16f, 0.5f, b.z + 0.16f));
            OKit.Box(t, "Pole", OMat.MetalDark, new Vector3(b.x - 0.07f, 0f, b.z - 0.07f), new Vector3(b.x + 0.07f, 4.6f, b.z + 0.07f));
            OKit.Box(t, "Arm", OMat.MetalDark, new Vector3(b.x - 0.04f, 4.5f, b.z - 0.9f), new Vector3(b.x + 0.04f, 4.58f, b.z), false);
            OKit.Box(t, "Head", lit ? OMat.LightWarm : OMat.MetalDark, new Vector3(b.x - 0.18f, 4.32f, b.z - 1.05f), new Vector3(b.x + 0.18f, 4.5f, b.z - 0.7f), false);
            if (lit) OKit.PointLight(t, "Glow", new Vector3(b.x, 4.1f, b.z - 0.88f), A7_Warm, 3f, 7f);
        }

        /// <summary>
        /// A large faint emissive disc far east, facing the door, as the pale sun. Linear fog hides it
        /// completely from the door (it sits past the fog's end), so it only emerges, slowly, for a
        /// player who keeps walking east.
        /// </summary>
        static void A7_SunDisc(Transform root)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "A7_SunDisc";
            disc.transform.SetParent(root, false);
            disc.transform.SetPositionAndRotation(new Vector3(412f, 40f, 0f), Quaternion.Euler(0f, 0f, 90f));
            disc.transform.localScale = new Vector3(36f, 0.02f, 36f);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = OKit.Mat(OMat.LightPanel);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
