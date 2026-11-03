using System.Collections.Generic;
using Kit.Gameplay.Signage;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using A2M = Kit.Editor.OfficeKit.M;
using A2P = Kit.Editor.OfficeKit.PropIds;

namespace Kit.Editor
{
    /// <summary>
    /// A2, the Atrium hub and its lobby: the visual heart of OOO. A 40 x 60 m concrete hall,
    /// 34 m tall, with ring galleries at L1-L4 around a central void, a monumental white stair from
    /// the lobby to L2, a coffered roof with a long skylight band and two louvred square skylights,
    /// and a vast empty upper void with the cantilevered boardroom pod (reachable only by the
    /// launch out of threshold 6 high on the west wall). The lobby holds the glazed entrance
    /// (threshold 1, which loops back to the waiting room), the high directory letterboard (the
    /// tile source of "OUT OF OFFICE"), the cloakroom with its blank letterboard plate (where the
    /// player makes 000), sunken seating pits, a lone tree under a skylight and the front desk.
    /// The atrium owns its four outer walls and cuts every socket exactly as OfficeLayout gives it.
    /// </summary>
    public static partial class OfficeBuilder
    {
        // Inner faces of the outer walls, the outer faces, and the central void.
        const float A2_InX = 19.75f, A2_InZ = 29.75f, A2_OutZ = 30.25f;
        const float A2_VoidX = 15f, A2_VoidZ = 25f;
        const float A2_SlabT = 0.4f;          // gallery slab thickness
        const float A2_WallTop = 33.4f;       // walls stop under the roof slab
        const float A2_RoofY = 34f, A2_RoofT = 0.6f;
        const float A2_RibDepth = 1f;         // coffer ribs hang 1 m below the roof slab
        const float A2_GlazeHalf = 6f, A2_GlazeTop = 4.6f; // lobby entrance glazing in the south wall
        const float A2_VestibuleZ = -33.5f;   // outer glass line of the entrance vestibule
        const float A2_LaunchHoleHalf = 1.9f, A2_LaunchHoleBottom = 26.6f, A2_LaunchHoleTop = 32.6f;
        const float A2_DirectoryY = 6.64f;    // directory centre: tiles span y 5.88..7.40

        static readonly Color A2_Cool = new Color(0.78f, 0.86f, 1f);
        static readonly Color A2_Daylight = new Color(0.92f, 0.95f, 1f);
        static readonly Color A2_Warm = new Color(1f, 0.72f, 0.45f);
        static readonly Color A2_ShaftTint = new Color(0.8f, 0.86f, 0.95f, 0.22f);

        /// <summary>The scene sun's travel direction (OfficeBuilder.BuildScene: Euler(62, -35, 0)), for skylight pools and shafts.</summary>
        static Vector3 A2_SunDir => Quaternion.Euler(62f, -35f, 0f) * Vector3.forward;

        /// <summary>Builds the whole atrium under <paramref name="root"/> (world coordinates).</summary>
        static partial void BuildArea_A2(Transform root)
        {
            OfficeKit.Zone(root, "A2", OfficeLayout.A2);
            A2_Walls(root);
            A2_LobbyFloor(root);
            A2_Galleries(root);
            A2_Stairs(root);
            A2_Entrance(root);
            A2_Directory(root);
            A2_Cloakroom(root);
            A2_LobbyDressing(root);
            A2_GalleryDressing(root);
            A2_Pod(root);
            A2_Launch(root);
            A2_Roof(root);
            A2_Lights(root);
            A2_Signage(root);
            A2_Zones(root);
        }

        // ================================================================== helpers

        /// <summary>An empty child to group the hierarchy, so the lead can find things in the scene.</summary>
        static Transform A2_Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>
        /// Splits a wall rectangle (u along the wall, v up) into solid boxes around rectangular holes
        /// (x = u0, y = v0, z = u1, w = v1). Runs are merged along u, then stacked runs with the same
        /// span are merged up, so a wall with several openings stays a handful of boxes. Unlike
        /// OfficeKit.Wall, holes may share a span along the wall (A5_L0 sits under A5_L2).
        /// </summary>
        static List<Vector4> A2_Cut(float u0, float u1, float v0, float v1, List<Vector4> holes)
        {
            var us = new SortedSet<float> { u0, u1 };
            var vs = new SortedSet<float> { v0, v1 };
            foreach (var h in holes)
            {
                us.Add(Mathf.Clamp(h.x, u0, u1)); us.Add(Mathf.Clamp(h.z, u0, u1));
                vs.Add(Mathf.Clamp(h.y, v0, v1)); vs.Add(Mathf.Clamp(h.w, v0, v1));
            }
            var ul = new List<float>(us);
            var vl = new List<float>(vs);

            bool Solid(float cu, float cv)
            {
                foreach (var h in holes)
                    if (cu > h.x && cu < h.z && cv > h.y && cv < h.w) return false;
                return true;
            }

            var done = new List<Vector4>();
            var open = new List<Vector4>();
            for (int vi = 0; vi < vl.Count - 1; vi++)
            {
                float va = vl[vi], vb = vl[vi + 1], cv = (va + vb) * 0.5f;
                var next = new List<Vector4>();
                int ui = 0;
                while (ui < ul.Count - 1)
                {
                    if (!Solid((ul[ui] + ul[ui + 1]) * 0.5f, cv)) { ui++; continue; }
                    int start = ui;
                    while (ui < ul.Count - 1 && Solid((ul[ui] + ul[ui + 1]) * 0.5f, cv)) ui++;
                    float ra = ul[start], rb = ul[ui];
                    int k = open.FindIndex(o => o.x == ra && o.z == rb && Mathf.Abs(o.w - va) < 0.0001f);
                    if (k >= 0) { var o = open[k]; o.w = vb; next.Add(o); open.RemoveAt(k); }
                    else next.Add(new Vector4(ra, va, rb, vb));
                }
                done.AddRange(open);
                open = next;
            }
            done.AddRange(open);
            return done;
        }

        /// <summary>A wall on the plane x = <paramref name="x"/>, from z0 to z1 and y0 to y1, minus holes given as (z0, y0, z1, y1).</summary>
        static void A2_WallX(Transform parent, string name, string mat, float x, float z0, float z1, float y0, float y1, float t, List<Vector4> holes)
        {
            var g = A2_Group(parent, name);
            int i = 0;
            foreach (var r in A2_Cut(z0, z1, y0, y1, holes))
                OfficeKit.Box(g, $"{name}_{i++}", mat, new Vector3(x - t * 0.5f, r.y, r.x), new Vector3(x + t * 0.5f, r.w, r.z));
        }

        /// <summary>A wall on the plane z = <paramref name="z"/>, from x0 to x1 and y0 to y1, minus holes given as (x0, y0, x1, y1).</summary>
        static void A2_WallZ(Transform parent, string name, string mat, float z, float x0, float x1, float y0, float y1, float t, List<Vector4> holes)
        {
            var g = A2_Group(parent, name);
            int i = 0;
            foreach (var r in A2_Cut(x0, x1, y0, y1, holes))
                OfficeKit.Box(g, $"{name}_{i++}", mat, new Vector3(r.x, r.y, z - t * 0.5f), new Vector3(r.z, r.w, z + t * 0.5f));
        }

        /// <summary>A socket's opening as a hole in a wall on an x plane: (z0, y0, z1, y1).</summary>
        static Vector4 A2_HoleZY(OfficeLayout.Socket s) => new Vector4(s.Opening.min.z, s.Opening.min.y, s.Opening.max.z, s.Opening.max.y);

        /// <summary>A socket's opening as a hole in a wall on a z plane: (x0, y0, x1, y1).</summary>
        static Vector4 A2_HoleXY(OfficeLayout.Socket s) => new Vector4(s.Opening.min.x, s.Opening.min.y, s.Opening.max.x, s.Opening.max.y);

        /// <summary>A glass box that lets light through (no shadow), so the sun and the entrance spot fall through glazing and skylights.</summary>
        static GameObject A2_Glass(Transform parent, string name, Vector3 min, Vector3 max, bool collider = true)
        {
            var go = OfficeKit.Box(parent, name, A2M.Glass, min, max, collider);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        /// <summary>A rotated glass panel that casts no shadow (revolving door wings and drum).</summary>
        static void A2_GlassAt(Transform parent, string name, Vector3 centre, Vector3 size, Quaternion rot)
        {
            var go = OfficeKit.BoxAt(parent, name, A2M.Glass, centre, size, rot, false);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>A decorative cylinder (no collider): revolving-door canopy, pendant shades.</summary>
        static void A2_Cylinder(Transform parent, string name, string mat, Vector3 centre, float diameter, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = OfficeKit.Mat(mat);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>Rotation for a LightShaft: its up axis points back toward the sun, its width runs along world X.</summary>
        static Quaternion A2_ShaftRotation()
        {
            var up = -A2_SunDir;
            return Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.forward, up).normalized, up);
        }

        // ================================================================== shell

        /// <summary>
        /// The four outer walls, 0.5 m thick on the planes x = +/-20 and z = +/-30, from the floor to
        /// the roof slab. East and west walls run the full length (corners included); north and south
        /// fit between them. The west wall's south part starts at y -4 because the A5 basement is
        /// behind it. Every socket is cut exactly as OfficeLayout gives it.
        /// </summary>
        static void A2_Walls(Transform root)
        {
            var g = A2_Group(root, "OuterWalls");
            float t = OfficeLayout.AtriumWallThickness;

            var west = new List<Vector4>
            {
                A2_HoleZY(OfficeLayout.A5_L0),
                A2_HoleZY(OfficeLayout.A5_L2),
                A2_HoleZY(OfficeLayout.A1_Shutter),
                A2_HoleZY(OfficeLayout.A1_Transom),
                new Vector4(-A2_LaunchHoleHalf, A2_LaunchHoleBottom, A2_LaunchHoleHalf, A2_LaunchHoleTop), // launch exit 6 recess
                new Vector4(-14f, -4f, A2_OutZ, 0f), // below the floor only where A5's basement is behind
            };
            A2_WallX(g, "Wall_West", A2M.Concrete, -20f, -A2_OutZ, A2_OutZ, -4f, A2_WallTop, t, west);

            var east = new List<Vector4> { A2_HoleZY(OfficeLayout.A4_Entry), A2_HoleZY(OfficeLayout.A6_Bridge) };
            A2_WallX(g, "Wall_East", A2M.Concrete, 20f, -A2_OutZ, A2_OutZ, 0f, A2_WallTop, t, east);

            A2_WallZ(g, "Wall_North", A2M.Concrete, 30f, -A2_InX, A2_InX, 0f, A2_WallTop, t,
                new List<Vector4> { A2_HoleXY(OfficeLayout.A3_Entry) });

            A2_WallZ(g, "Wall_South", A2M.Concrete, -30f, -A2_InX, A2_InX, 0f, A2_WallTop, t,
                new List<Vector4> { new Vector4(-A2_GlazeHalf, 0f, A2_GlazeHalf, A2_GlazeTop) });

            // Faint horizontal pour lines on the upper void walls (flush, no collider, so they
            // never make a ledge anyone could wall-run or stand on near the pod).
            for (int i = 0; i < 4; i++)
            {
                float y = 19.5f + i * 3.5f;
                OfficeKit.Box(g, $"PourLine_N_{i}", A2M.ConcreteDark, new Vector3(-A2_InX, y, A2_InZ - 0.02f), new Vector3(A2_InX, y + 0.06f, A2_InZ), false);
                OfficeKit.Box(g, $"PourLine_S_{i}", A2M.ConcreteDark, new Vector3(-A2_InX, y, -A2_InZ), new Vector3(A2_InX, y + 0.06f, -A2_InZ + 0.02f), false);
            }
        }

        /// <summary>
        /// L0: slate at the entrance, terrazzo everywhere else, two sunken seating pits with green
        /// benches (severance_01), a yellow-green inlay runner and border, and the tree planter.
        /// </summary>
        static void A2_LobbyFloor(Transform root)
        {
            var g = A2_Group(root, "L0_Floor");
            OfficeKit.Slab(g, "Floor_Slate", A2M.Slate, new Vector3(-A2_InX, 0f, -A2_InZ), new Vector3(A2_InX, 0f, -24f), 0f, 0.5f);
            OfficeKit.Slab(g, "Floor_Terrazzo", A2M.Terrazzo, new Vector3(-A2_InX, 0f, -24f), new Vector3(A2_InX, 0f, A2_InZ), 0f, 0.5f,
                new Vector4(-12f, -18f, -6f, -10f), new Vector4(6f, -18f, 12f, -10f));

            for (int side = -1; side <= 1; side += 2) A2_SeatingPit(g, side);

            // Inlay: a border band around the seating area and a runner from the doors to the stair.
            var inlay = A2_Group(g, "Inlay");
            void Band(string n, float x0, float z0, float x1, float z1, float h) =>
                OfficeKit.Box(inlay, n, A2M.FloorInlay, new Vector3(x0, 0f, z0), new Vector3(x1, h, z1), false);
            Band("Border_S", -13.5f, -19.5f, 13.5f, -19.1f, 0.01f);
            Band("Border_N", -13.5f, -8.9f, 13.5f, -8.5f, 0.01f);
            Band("Border_W", -13.5f, -19.1f, -13.1f, -8.9f, 0.01f);
            Band("Border_E", 13.1f, -19.1f, 13.5f, -8.9f, 0.01f);
            Band("Runner_S", -1.25f, -29.6f, 1.25f, -8.6f, 0.012f);
            Band("Runner_N", -1.25f, -3.4f, 1.25f, 5.9f, 0.012f);

            // The tree planter: low concrete rim, soil, one lone tree (control_02), lit by skylight S1.
            var p = A2_Group(g, "TreePlanter");
            OfficeKit.Box(p, "Rim_S", A2M.ConcreteLight, new Vector3(-2.5f, 0f, -8.5f), new Vector3(2.5f, 0.6f, -8.2f));
            OfficeKit.Box(p, "Rim_N", A2M.ConcreteLight, new Vector3(-2.5f, 0f, -3.8f), new Vector3(2.5f, 0.6f, -3.5f));
            OfficeKit.Box(p, "Rim_W", A2M.ConcreteLight, new Vector3(-2.5f, 0f, -8.2f), new Vector3(-2.2f, 0.6f, -3.8f));
            OfficeKit.Box(p, "Rim_E", A2M.ConcreteLight, new Vector3(2.2f, 0f, -8.2f), new Vector3(2.5f, 0.6f, -3.8f));
            OfficeKit.Box(p, "Fill", A2M.ConcreteDark, new Vector3(-2.2f, 0f, -8.2f), new Vector3(2.2f, 0.45f, -3.8f));
            OfficeKit.Box(p, "Soil", A2M.Soil, new Vector3(-2.2f, 0.45f, -8.2f), new Vector3(2.2f, 0.5f, -3.8f));
            OfficeKit.Prop(p, A2P.Tree, new Vector3(0f, 0.5f, -6f), 20f);
        }

        /// <summary>
        /// A sunken seating pit 0.4 m deep (x 6..12 mirrored by <paramref name="side"/>, z -18..-10):
        /// a step down on the side facing the axis, low parapets on the other three, benches facing
        /// a low table. The benches face each other as if a meeting just broke up.
        /// </summary>
        static void A2_SeatingPit(Transform parent, int side)
        {
            float s = side;
            var g = A2_Group(parent, side < 0 ? "SeatingPit_W" : "SeatingPit_E");
            OfficeKit.Box(g, "PitFloor", A2M.Slate, new Vector3(s * 6f, -0.9f, -18f), new Vector3(s * 12f, -0.4f, -10f));
            OfficeKit.Box(g, "Step", A2M.Terrazzo, new Vector3(s * 6f, -0.9f, -18f), new Vector3(s * 6.6f, -0.2f, -10f));
            OfficeKit.Box(g, "Parapet_N", A2M.ConcreteLight, new Vector3(s * 6f, 0f, -10f), new Vector3(s * 12.25f, 0.45f, -9.75f));
            OfficeKit.Box(g, "Parapet_S", A2M.ConcreteLight, new Vector3(s * 6f, 0f, -18.25f), new Vector3(s * 12.25f, 0.45f, -18f));
            OfficeKit.Box(g, "Parapet_Out", A2M.ConcreteLight, new Vector3(s * 12f, 0f, -18.25f), new Vector3(s * 12.25f, 0.45f, -9.75f));
            OfficeKit.Box(g, "Table", A2M.ConcreteDark, new Vector3(s * 8.4f, -0.4f, -15.4f), new Vector3(s * 10f, -0.05f, -12.6f));

            float outYaw = side < 0 ? 90f : -90f;
            foreach (float z in new[] { -16.2f, -14f, -11.8f })
                OfficeKit.Prop(g, A2P.Bench, new Vector3(s * 11.6f, -0.4f, z), outYaw);
            OfficeKit.Prop(g, A2P.Bench, new Vector3(s * 9.3f, -0.4f, -10.4f), 180f);
            OfficeKit.Prop(g, A2P.Bench, new Vector3(s * 9.3f, -0.4f, -17.6f), 0f);
        }

        // ================================================================== galleries

        /// <summary>Stairwell holes in each gallery slab (index = level): where a corner flight arrives from below.</summary>
        static readonly Vector4[] A2_StairHoles =
        {
            Vector4.zero,
            new Vector4(17.75f, 19.5f, 19.75f, 25.5f),    // L1: flight G->1, east wall, rising north
            new Vector4(17.75f, -25.5f, 19.75f, -19.5f),  // L2: flight 1->2, east wall, rising south
            new Vector4(-19.75f, 19.5f, -17.75f, 25.5f),  // L3: flight 2->3, west wall, rising north
            new Vector4(-19.75f, -18f, -17.75f, -12f),    // L4: flight 3->4, west wall, rising south
        };

        /// <summary>
        /// Ring galleries 4.75 m deep at L1-L4 around the void (x -15..15, z -25..25), with deep
        /// fascias and glass railings (severance_01). L1 has no south side, so the lobby is double
        /// height under the directory. Nothing above L4: the upper void stays empty to the roof.
        /// </summary>
        static void A2_Galleries(Transform root)
        {
            var g = A2_Group(root, "Galleries");
            for (int n = 1; n <= 4; n++)
            {
                float y = OfficeLayout.L(n);
                var lv = A2_Group(g, $"Gallery_L{n}");
                var voidHole = n == 1 ? new Vector4(-A2_VoidX, -A2_InZ, A2_VoidX, A2_VoidZ) : new Vector4(-A2_VoidX, -A2_VoidZ, A2_VoidX, A2_VoidZ);
                OfficeKit.Slab(lv, "Slab", A2M.ConcreteFloor, new Vector3(-A2_InX, 0f, -A2_InZ), new Vector3(A2_InX, 0f, A2_InZ), y, A2_SlabT,
                    voidHole, A2_StairHoles[n]);

                // Fascias: deep concrete bands under the slab edge.
                float f0 = y - 1.1f, f1 = y - A2_SlabT;
                float sideZ0 = n == 1 ? -A2_InZ : -A2_VoidZ;
                OfficeKit.Box(lv, "Fascia_W", A2M.ConcreteLight, new Vector3(-A2_VoidX - 0.35f, f0, sideZ0), new Vector3(-A2_VoidX, f1, A2_VoidZ));
                OfficeKit.Box(lv, "Fascia_E", A2M.ConcreteLight, new Vector3(A2_VoidX, f0, sideZ0), new Vector3(A2_VoidX + 0.35f, f1, A2_VoidZ));
                if (n <= 2)
                {
                    // The monumental stair's top meets the north gallery at x -4..4.
                    OfficeKit.Box(lv, "Fascia_N_W", A2M.ConcreteLight, new Vector3(-A2_VoidX - 0.35f, f0, A2_VoidZ), new Vector3(-4.3f, f1, A2_VoidZ + 0.35f));
                    OfficeKit.Box(lv, "Fascia_N_E", A2M.ConcreteLight, new Vector3(4.3f, f0, A2_VoidZ), new Vector3(A2_VoidX + 0.35f, f1, A2_VoidZ + 0.35f));
                }
                else
                {
                    OfficeKit.Box(lv, "Fascia_N", A2M.ConcreteLight, new Vector3(-A2_VoidX - 0.35f, f0, A2_VoidZ), new Vector3(A2_VoidX + 0.35f, f1, A2_VoidZ + 0.35f));
                }
                if (n >= 2)
                    OfficeKit.Box(lv, "Fascia_S", A2M.ConcreteLight, new Vector3(-A2_VoidX - 0.35f, f0, -A2_VoidZ - 0.35f), new Vector3(A2_VoidX + 0.35f, f1, -A2_VoidZ));

                // Glass railings on the void edge.
                const float e = A2_VoidX + 0.15f, ez = A2_VoidZ + 0.15f;
                float railZ0 = n == 1 ? -A2_InZ : -ez;
                OfficeKit.Railing(lv, "Rail_W", new Vector3(-e, y, railZ0), new Vector3(-e, y, ez));
                OfficeKit.Railing(lv, "Rail_E", new Vector3(e, y, railZ0), new Vector3(e, y, ez));
                if (n == 1)
                {
                    OfficeKit.Railing(lv, "Rail_N_W", new Vector3(-e, y, ez), new Vector3(-4f, y, ez));
                    OfficeKit.Railing(lv, "Rail_N_E", new Vector3(4f, y, ez), new Vector3(e, y, ez));
                }
                else if (n == 2)
                {
                    OfficeKit.Railing(lv, "Rail_N_W", new Vector3(-e, y, ez), new Vector3(-4.3f, y, ez));
                    OfficeKit.Railing(lv, "Rail_N_E", new Vector3(4.3f, y, ez), new Vector3(e, y, ez));
                }
                else
                {
                    OfficeKit.Railing(lv, "Rail_N", new Vector3(-e, y, ez), new Vector3(e, y, ez));
                }
                if (n >= 2) OfficeKit.Railing(lv, "Rail_S", new Vector3(-e, y, -ez), new Vector3(e, y, -ez));
            }
        }

        // ================================================================== stairs

        /// <summary>
        /// The monumental stair (control_02's white stair) from the lobby to the L2 north gallery,
        /// 8 m wide, two flights of 20 with a landing at 4 m, built as one solid mass with sloped
        /// parapets. Then four 2 m corner flights along the side walls (G-1, 1-2, 2-3, 3-4), each
        /// rising into a stairwell hole in the gallery above with railings round the hole.
        /// </summary>
        static void A2_Stairs(Transform root)
        {
            var g = A2_Group(root, "Stairs");
            var m = A2_Group(g, "MonumentalStair");
            OfficeKit.Stairs(m, "Flight_1", A2M.ConcreteLight, new Vector3(0f, 0f, 6f), Vector3.forward, 8f, 20);
            OfficeKit.Box(m, "Landing", A2M.ConcreteLight, new Vector3(-4f, 0f, 12f), new Vector3(4f, 4f, 16f));
            OfficeKit.Box(m, "Plinth", A2M.ConcreteLight, new Vector3(-4f, 0f, 16f), new Vector3(4f, 4f, 22f));
            OfficeKit.Stairs(m, "Flight_2", A2M.ConcreteLight, new Vector3(0f, 4f, 16f), Vector3.forward, 8f, 20);
            OfficeKit.Box(m, "TopLanding", A2M.ConcreteLight, new Vector3(-4f, 0f, 22f), new Vector3(4f, 8f, 25f));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 4.15f;
                string sn = side < 0 ? "W" : "E";
                A2_SlopedParapet(m, "Parapet_F1_" + sn, new Vector3(x, 0.1f, 6f), new Vector3(x, 4.1f, 12f));
                A2_SlopedParapet(m, "Parapet_F2_" + sn, new Vector3(x, 4.1f, 16f), new Vector3(x, 8.1f, 22f));
                OfficeKit.Box(m, "Parapet_Landing_" + sn, A2M.ConcreteLight, new Vector3(side * 4f, 4f, 12f), new Vector3(side * 4.3f, 5.2f, 16f));
                OfficeKit.Box(m, "Parapet_Top_" + sn, A2M.ConcreteLight, new Vector3(side * 4f, 8f, 22f), new Vector3(side * 4.3f, 9.1f, 25f));
            }
            OfficeKit.Prop(m, A2P.PottedPlant, new Vector3(3.2f, 4f, 14.8f), 0f);
            OfficeKit.Prop(m, A2P.PottedPlant, new Vector3(-3.3f, 8f, 24.2f), 40f);

            var c = A2_Group(g, "CornerFlights");
            A2_CornerFlight(c, "Flight_G_1", new Vector3(18.75f, 0f, 19.5f), 1f, 17.8f);
            A2_CornerFlight(c, "Flight_1_2", new Vector3(18.75f, 4f, -19.5f), -1f, 17.8f);
            A2_CornerFlight(c, "Flight_2_3", new Vector3(-18.75f, 8f, 19.5f), 1f, -17.8f);
            A2_CornerFlight(c, "Flight_3_4", new Vector3(-18.75f, 12f, -12f), -1f, -17.8f);

            // Railings round each stairwell hole at the upper level (the arrival end stays open).
            OfficeKit.Railing(c, "Well_L1_Side", new Vector3(17.65f, 4f, 19.4f), new Vector3(17.65f, 4f, 25.5f));
            OfficeKit.Railing(c, "Well_L1_End", new Vector3(17.65f, 4f, 19.4f), new Vector3(A2_InX, 4f, 19.4f));
            OfficeKit.Railing(c, "Well_L2_Side", new Vector3(17.65f, 8f, -19.4f), new Vector3(17.65f, 8f, -25.5f));
            OfficeKit.Railing(c, "Well_L2_End", new Vector3(17.65f, 8f, -19.4f), new Vector3(A2_InX, 8f, -19.4f));
            OfficeKit.Railing(c, "Well_L3_Side", new Vector3(-17.65f, 12f, 19.4f), new Vector3(-17.65f, 12f, 25.5f));
            OfficeKit.Railing(c, "Well_L3_End", new Vector3(-A2_InX, 12f, 19.4f), new Vector3(-17.65f, 12f, 19.4f));
            OfficeKit.Railing(c, "Well_L4_Side", new Vector3(-17.65f, 16f, -11.9f), new Vector3(-17.65f, 16f, -18f));
            OfficeKit.Railing(c, "Well_L4_End", new Vector3(-A2_InX, 16f, -11.9f), new Vector3(-17.65f, 16f, -11.9f));
        }

        /// <summary>A straight 20-step flight (4 m rise over 6 m) against a side wall, with a sloped rail on its open side.</summary>
        static void A2_CornerFlight(Transform parent, string name, Vector3 start, float dirZ, float railX)
        {
            var dir = new Vector3(0f, 0f, dirZ);
            var top = OfficeKit.Stairs(parent, name, A2M.ConcreteLight, start, dir, 2f, 20);
            OfficeKit.Railing(parent, name + "_Rail",
                new Vector3(railX, start.y + 0.2f, start.z + dirZ * 0.15f),
                new Vector3(railX, top.y, top.z - dirZ * 0.15f));
        }

        /// <summary>A solid parapet following a flight's pitch line from <paramref name="a"/> to <paramref name="b"/> (about 1 m above the nosings).</summary>
        static void A2_SlopedParapet(Transform parent, string name, Vector3 a, Vector3 b)
        {
            var dir = (b - a).normalized;
            OfficeKit.BoxAt(parent, name, A2M.ConcreteLight, (a + b) * 0.5f, new Vector3(0.3f, 2f, Vector3.Distance(a, b)), Quaternion.LookRotation(dir, Vector3.up));
        }

        // ================================================================== entrance

        /// <summary>
        /// The glazed double-height entrance (brutal_02): dark mullions flush with the wall's inner
        /// face (so the south wall stays one clean wall-run surface), brass threshold 1 in the glass
        /// (front facing north into the lobby: stepping out arrives in the A1 waiting room), and a
        /// 3 m glass vestibule outside with a decorative revolving door, beyond which there is only
        /// a pale lit ground fading into fog.
        /// </summary>
        static void A2_Entrance(Transform root)
        {
            var g = A2_Group(root, "Entrance");
            const float zi0 = -29.8f, zi1 = -29.76f; // inner glass, flush with the wall face (-29.75)
            const float doorX = 2.5f, jamb = 0.12f;
            float dl = doorX - OfficeLayout.Door.x * 0.5f - jamb, dr = doorX + OfficeLayout.Door.x * 0.5f + jamb, dh = OfficeLayout.Door.y + jamb;

            A2_Glass(g, "Glass_Inner_W", new Vector3(-A2_GlazeHalf, 0f, zi0), new Vector3(dl, A2_GlazeTop, zi1));
            A2_Glass(g, "Glass_Inner_E", new Vector3(dr, 0f, zi0), new Vector3(A2_GlazeHalf, A2_GlazeTop, zi1));
            A2_Glass(g, "Glass_Inner_OverDoor", new Vector3(dl, dh, zi0), new Vector3(dr, A2_GlazeTop, zi1));
            foreach (float x in new[] { -5.96f, -3f, 0f, 4.5f, 5.96f })
                OfficeKit.Box(g, $"Mullion_{x:0.0}", A2M.MetalDark, new Vector3(x - 0.04f, 0f, -30.25f), new Vector3(x + 0.04f, A2_GlazeTop, -29.75f));
            OfficeKit.Box(g, "Transom", A2M.MetalDark, new Vector3(-A2_GlazeHalf, 2.6f, -30.25f), new Vector3(A2_GlazeHalf, 2.68f, -29.75f));
            OfficeKit.Box(g, "Head", A2M.MetalDark, new Vector3(-A2_GlazeHalf, A2_GlazeTop - 0.08f, -30.25f), new Vector3(A2_GlazeHalf, A2_GlazeTop, -29.75f));

            // Threshold 1 (number plan: A2 lobby doors, brass, front north). The opening is the gap
            // left in the glass; the vestibule behind gives 3 m clear.
            OfficeKit.Threshold(g, "Threshold_1_LobbyDoors", new Vector3(doorX, OfficeLayout.Door.y * 0.5f, -30f),
                OfficeKit.Facing(Vector3.forward), OfficeLayout.Door, "1");

            // Vestibule: slate floor, concrete canopy, glass sides and outer line.
            var v = A2_Group(g, "Vestibule");
            OfficeKit.Box(v, "Floor", A2M.Slate, new Vector3(-A2_GlazeHalf, -0.3f, A2_VestibuleZ), new Vector3(A2_GlazeHalf, 0f, -29.75f));
            OfficeKit.Box(v, "Canopy", A2M.ConcreteDark, new Vector3(-6.3f, A2_GlazeTop, -33.8f), new Vector3(6.3f, 5f, -30.25f));
            OfficeKit.Box(v, "Mat", A2M.CarpetGrey, new Vector3(1.6f, 0f, -31.6f), new Vector3(3.4f, 0.01f, -30.5f), false);
            for (int side = -1; side <= 1; side += 2)
            {
                A2_Glass(v, side < 0 ? "Glass_Side_W" : "Glass_Side_E", new Vector3(side * 5.98f, 0f, A2_VestibuleZ), new Vector3(side * 6.02f, A2_GlazeTop, -30.25f));
                OfficeKit.Box(v, side < 0 ? "Post_W" : "Post_E", A2M.MetalDark, new Vector3(side * 5.95f, 0f, A2_VestibuleZ - 0.05f), new Vector3(side * 6.05f, A2_GlazeTop, A2_VestibuleZ + 0.05f));
            }
            const float drumX = -2.5f, drumR = 1.35f;
            A2_Glass(v, "Glass_Outer_W", new Vector3(-A2_GlazeHalf, 0f, A2_VestibuleZ - 0.02f), new Vector3(drumX - drumR - 0.05f, A2_GlazeTop, A2_VestibuleZ + 0.02f));
            A2_Glass(v, "Glass_Outer_E", new Vector3(drumX + drumR + 0.05f, 0f, A2_VestibuleZ - 0.02f), new Vector3(A2_GlazeHalf, A2_GlazeTop, A2_VestibuleZ + 0.02f));
            A2_Glass(v, "Glass_Outer_OverDrum", new Vector3(drumX - drumR - 0.05f, 2.7f, A2_VestibuleZ - 0.02f), new Vector3(drumX + drumR + 0.05f, A2_GlazeTop, A2_VestibuleZ + 0.02f));
            foreach (float x in new[] { drumX - drumR - 0.05f, drumX + drumR + 0.05f, 1.5f, 4f })
                OfficeKit.Box(v, $"OuterMullion_{x:0.0}", A2M.MetalDark, new Vector3(x - 0.04f, 0f, A2_VestibuleZ - 0.06f), new Vector3(x + 0.04f, A2_GlazeTop, A2_VestibuleZ + 0.06f));
            A2_RevolvingDoor(v, new Vector3(drumX, 0f, A2_VestibuleZ), drumR);

            // Outside: a pale ground and backdrop that the fog swallows. Never reachable.
            var o = A2_Group(g, "OutsideGlimpse");
            OfficeKit.Box(o, "Ground", A2M.Exterior, new Vector3(-14f, -0.3f, -48f), new Vector3(16f, -0.02f, A2_VestibuleZ));
            OfficeKit.Box(o, "Backdrop", A2M.Exterior, new Vector3(-30f, -1f, -50f), new Vector3(30f, 30f, -49.5f), false);
        }

        /// <summary>A decorative revolving door (drum, canopy, four glass wings) in the vestibule's outer glass line. Purely visual.</summary>
        static void A2_RevolvingDoor(Transform parent, Vector3 c, float r)
        {
            var d = A2_Group(parent, "RevolvingDoor");
            A2_Cylinder(d, "Canopy", A2M.MetalDark, c + Vector3.up * 2.6f, r * 2f + 0.2f, 0.2f);
            A2_Cylinder(d, "Base", A2M.Metal, c + Vector3.up * 0.01f, r * 2f + 0.1f, 0.02f);
            A2_Cylinder(d, "Post", A2M.Metal, c + Vector3.up * 1.25f, 0.08f, 2.5f);
            for (int i = 0; i < 4; i++)
            {
                var rot = Quaternion.Euler(0f, 45f + 90f * i, 0f);
                var dir = rot * Vector3.forward;
                A2_GlassAt(d, $"Wing_{i}", c + dir * (r * 0.5f) + Vector3.up * 1.25f, new Vector3(0.02f, 2.3f, r - 0.1f), rot);
                OfficeKit.BoxAt(d, $"WingEdge_{i}", A2M.MetalDark, c + dir * (r - 0.06f) + Vector3.up * 1.25f, new Vector3(0.05f, 2.35f, 0.05f), rot, false);
            }
            float chord = 2f * r * Mathf.Sin(10f * Mathf.Deg2Rad) + 0.02f;
            for (int side = 0; side < 2; side++)
                for (int k = 0; k < 5; k++)
                {
                    float rad = ((side == 0 ? -40f : 140f) + 20f * k) * Mathf.Deg2Rad;
                    var radial = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                    A2_GlassAt(d, $"Drum_{side}_{k}", c + radial * r + Vector3.up * 1.25f, new Vector3(chord, 2.5f, 0.02f), Quaternion.LookRotation(radial));
                }
        }

        /// <summary>
        /// The lobby directory: a 7 x 20 letterboard high on the south wall above the entrance. Its
        /// bottom tiles sit at 5.88 m: out of reach of a standing jump (eye peaks at 3.1 m, reach
        /// 2.6 m) but inside reach from a wall run along the south wall (eye about 3.5-4 m). The
        /// bottom row reads OUT OF OFFICE, the game's three O tiles.
        /// </summary>
        static void A2_Directory(Transform root)
        {
            var g = A2_Group(root, "Directory");
            string content = string.Join("\n",
                A2_DirRow("HALE & ORMSBY", true),
                A2_DirRow("L1 CAFETERIA", false),
                A2_DirRow("L2 STAIR B", false),
                A2_DirRow("L3 RECORDS", false),
                A2_DirRow("L4 RECEPTION", false),
                A2_DirRow("L6 EXECUTIVE", false),
                A2_DirRow("OUT OF OFFICE", true));
            OfficeKit.LetterboardSign(g, "Directory_Letterboard", new Vector3(0f, A2_DirectoryY, -29.7f), OfficeKit.Facing(Vector3.forward), 7, 20, content);

            // Dark wood frame (no colliders: nothing here may become a ledge).
            const float zf0 = -29.75f, zf1 = -29.66f, hx = 1.94f, ix = 1.86f;
            float yb = A2_DirectoryY - 0.91f, yt = A2_DirectoryY + 0.91f;
            OfficeKit.Box(g, "Frame_Top", A2M.WoodDark, new Vector3(-hx, yt - 0.08f, zf0), new Vector3(hx, yt, zf1), false);
            OfficeKit.Box(g, "Frame_Bottom", A2M.WoodDark, new Vector3(-hx, yb, zf0), new Vector3(hx, yb + 0.08f, zf1), false);
            OfficeKit.Box(g, "Frame_L", A2M.WoodDark, new Vector3(-hx, yb, zf0), new Vector3(-ix, yt, zf1), false);
            OfficeKit.Box(g, "Frame_R", A2M.WoodDark, new Vector3(ix, yb, zf0), new Vector3(hx, yt, zf1), false);

            // A picture light under the L2 soffit, warm: the lobby's accent when looking south.
            OfficeKit.Box(g, "PictureLight_Housing", A2M.MetalDark, new Vector3(-0.4f, 7.48f, -27.4f), new Vector3(0.4f, 7.6f, -27.2f), false);
            OfficeKit.SpotLight(g, "PictureLight", new Vector3(0f, 7.42f, -27.3f),
                Quaternion.LookRotation(new Vector3(0f, A2_DirectoryY - 7.42f, -29.75f + 27.3f).normalized, Vector3.up), A2_Warm, 4f, 7f, 70f, false);
        }

        /// <summary>One 20-slot directory row: spaces become empty slots ('_'), centred or indented by two.</summary>
        static string A2_DirRow(string text, bool centre)
        {
            const int cols = 20;
            int pad = centre ? (cols - text.Length) / 2 : 2;
            string row = new string('_', pad) + text.Replace(' ', '_');
            return row.Length >= cols ? row.Substring(0, cols) : row + new string('_', cols - row.Length);
        }

        // ================================================================== cloakroom

        /// <summary>
        /// The cloakroom: a small wood-panelled room tucked under the L1 west gallery
        /// (x -19.75..-16.25, z -12..-8). Its door carries a blank three-slot letterboard plate
        /// (front facing +X, into the lobby). Blank, it is just a closet with coats and a counter;
        /// set to 000 it pairs with the door outside. Lit warm: the lobby's lit doorway (control_02).
        /// </summary>
        static void A2_Cloakroom(Transform root)
        {
            var g = A2_Group(root, "Cloakroom");
            const float wx = -16.25f, h = 3.6f, t = 0.2f;
            A2_WallX(g, "Wall_Front", A2M.WoodVeneer, wx, -12.1f, -7.9f, 0f, h, t,
                new List<Vector4> { new Vector4(-10f - OfficeLayout.Door.x * 0.5f, 0f, -10f + OfficeLayout.Door.x * 0.5f, OfficeLayout.Door.y) });
            A2_WallZ(g, "Wall_N", A2M.WoodVeneer, -8f, -A2_InX, wx - t * 0.5f, 0f, h, t, new List<Vector4>());
            A2_WallZ(g, "Wall_S", A2M.WoodVeneer, -12f, -A2_InX, wx - t * 0.5f, 0f, h, t, new List<Vector4>());
            OfficeKit.Box(g, "Carpet", A2M.CarpetOxblood, new Vector3(-A2_InX, 0f, -11.9f), new Vector3(wx - t * 0.5f, 0.01f, -8.1f), false);

            OfficeKit.Threshold(g, "Threshold_Cloakroom", new Vector3(wx, OfficeLayout.Door.y * 0.5f, -10f),
                OfficeKit.Facing(Vector3.right), OfficeLayout.Door, null, "___");

            // Counter across the room with a gap at the north end; coats and a forgotten umbrella.
            OfficeKit.Box(g, "Counter", A2M.WoodDark, new Vector3(-18.45f, 0f, -11.9f), new Vector3(-17.95f, 1f, -9f));
            OfficeKit.Box(g, "CounterTop", A2M.Frame, new Vector3(-18.5f, 1f, -11.9f), new Vector3(-17.9f, 1.04f, -9f));
            OfficeKit.Prop(g, A2P.CoatRack, new Vector3(-19.2f, 0f, -11.3f), 0f);
            OfficeKit.Prop(g, A2P.CoatRack, new Vector3(-19.2f, 0f, -9.5f), 30f);
            OfficeKit.Box(g, "Coat", A2M.Leather, new Vector3(-19.45f, 0.75f, -11.42f), new Vector3(-18.95f, 1.7f, -11.18f), false);
            OfficeKit.Box(g, "HookRail", A2M.Brass, new Vector3(-A2_InX, 1.72f, -11.6f), new Vector3(-19.7f, 1.76f, -8.4f), false);

            var umb = Quaternion.Euler(0f, 0f, -12f);
            OfficeKit.BoxAt(g, "Umbrella_Shaft", A2M.MetalDark, new Vector3(-17.78f, 0.47f, -11.45f), new Vector3(0.025f, 0.94f, 0.025f), umb, false);
            OfficeKit.BoxAt(g, "Umbrella_Canopy", A2M.Leather, new Vector3(-17.8f, 0.42f, -11.45f), new Vector3(0.1f, 0.6f, 0.1f), umb, false);
            OfficeKit.BoxAt(g, "Umbrella_Handle", A2M.WoodDark, new Vector3(-17.68f, 0.93f, -11.45f), new Vector3(0.1f, 0.03f, 0.03f), umb, false);

            OfficeKit.CeilingLight(g, "Cloakroom_Light", new Vector3(-18.2f, h - 0.05f, -10f), new Vector2(0.5f, 0.5f), A2_Warm, 4f, 5f,
                false, false, A2M.LightWarm);
        }

        // ================================================================== dressing

        /// <summary>
        /// Lobby furniture: the front desk with its sign-out sheet, a ticket dispenser, a turnstile
        /// line (severance_01), philodendrons, a stopped clock, the elevator bank (closed doors whose
        /// floor numbers are not openings), and pendants over the seating pits.
        /// </summary>
        static void A2_LobbyDressing(Transform root)
        {
            var g = A2_Group(root, "LobbyDressing");

            OfficeKit.Prop(g, A2P.ReceptionDesk, new Vector3(10f, 0f, -22f), -90f);
            OfficeKit.Prop(g, A2P.SwivelChair, new Vector3(11.2f, 0f, -22.3f), -110f);
            OfficeKit.Prop(g, A2P.Telephone, new Vector3(10.1f, 1.1f, -21.0f), -100f);
            OfficeKit.Prop(g, A2P.DeskLamp, new Vector3(10.1f, 1.1f, -23.1f), -60f);
            OfficeKit.Prop(g, A2P.TicketDispenser, new Vector3(8.8f, 0f, -25.2f), -90f);
            OfficeKit.Prop(g, A2P.Bin, new Vector3(11f, 0f, -20.2f), 0f);
            OfficeKit.Prop(g, A2P.PottedPlant, new Vector3(12.6f, 0f, -25f), 15f);
            OfficeKit.Prop(g, A2P.PottedPlant, new Vector3(-15.7f, 0f, -12.7f), 70f);
            OfficeKit.Prop(g, A2P.Planter, new Vector3(18.9f, 0f, -12f), -90f);
            OfficeKit.Prop(g, A2P.WallClock, new Vector3(19.72f, 2.65f, -21f), -90f);
            OfficeKit.Prop(g, A2P.Corkboard, new Vector3(-19.73f, 1f, -13.8f), 90f);
            OfficeKit.Prop(g, A2P.Flag, new Vector3(-17.5f, 0f, -28.8f), 0f);

            foreach (float x in new[] { -5.4f, -3.6f, -1.8f, 1.8f, 3.6f, 5.4f })
                OfficeKit.Prop(g, A2P.Turnstile, new Vector3(x, 0f, -21f), 0f);

            for (int n = 0; n <= 4; n++) A2_Elevators(g, n);

            // Pendants over the seating pits: long cables from the roof, cool drum shades.
            for (int side = -1; side <= 1; side += 2)
            {
                var p = new Vector3(side * 9f, 0f, -14f);
                string sn = side < 0 ? "W" : "E";
                OfficeKit.Box(g, "Pendant_Cable_" + sn, A2M.MetalDark, new Vector3(p.x - 0.01f, 6.85f, p.z - 0.01f), new Vector3(p.x + 0.01f, A2_RoofY - A2_RoofT, p.z + 0.01f), false);
                A2_Cylinder(g, "Pendant_Shade_" + sn, A2M.MetalDark, new Vector3(p.x, 6.72f, p.z), 1.4f, 0.26f);
                A2_Cylinder(g, "Pendant_Diffuser_" + sn, A2M.LightPanel, new Vector3(p.x, 6.58f, p.z), 1.3f, 0.02f);
                OfficeKit.SpotLight(g, "Pendant_Light_" + sn, new Vector3(p.x, 6.5f, p.z), Quaternion.LookRotation(Vector3.down, Vector3.forward), A2_Cool, 20f, 9f, 110f, false);
            }
        }

        /// <summary>A pair of closed elevator doors on the east wall at level <paramref name="n"/>, with a floor indicator. Visual only: no opening, no threshold.</summary>
        static void A2_Elevators(Transform parent, int n)
        {
            float y = OfficeLayout.L(n);
            var g = A2_Group(parent, $"Elevators_{(n == 0 ? "G" : n.ToString())}");
            foreach (float zc in new[] { 11f, 13f })
            {
                OfficeKit.Box(g, $"Door_{zc}", A2M.MetalDark, new Vector3(19.66f, y, zc - 0.55f), new Vector3(A2_InX, y + 2.2f, zc + 0.55f), false);
                OfficeKit.Box(g, $"Seam_{zc}", A2M.Metal, new Vector3(19.65f, y, zc - 0.01f), new Vector3(19.66f, y + 2.2f, zc + 0.01f), false);
                OfficeKit.Box(g, $"Surround_{zc}", A2M.Frame, new Vector3(19.69f, y + 2.2f, zc - 0.65f), new Vector3(A2_InX, y + 2.32f, zc + 0.65f), false);
            }
            OfficeKit.Box(g, "CallPanel", A2M.Metal, new Vector3(19.7f, y + 0.95f, 11.92f), new Vector3(A2_InX, y + 1.35f, 12.08f), false);
            string label = n == 0 ? "G" : n.ToString();
            OfficeKit.Sign(g, label, new Vector3(19.72f, y + 2.6f, 12f), OfficeKit.Facing(Vector3.left), 0.14f, WorldTextStyle.Brass);
        }

        /// <summary>Gallery furniture: shell chair rows near area entrances (the warm accent per gallery view), planters, a water cooler, a chair left in the aisle.</summary>
        static void A2_GalleryDressing(Transform root)
        {
            var g = A2_Group(root, "GalleryDressing");
            OfficeKit.Prop(g, A2P.ShellChairRow, new Vector3(19.25f, 4f, -8f), -90f);
            OfficeKit.Prop(g, A2P.ShellChairRow, new Vector3(19.25f, 4f, 8f), -90f);
            OfficeKit.Prop(g, A2P.Planter, new Vector3(-19.3f, 8f, 5f), 90f);
            OfficeKit.Prop(g, A2P.Planter, new Vector3(-19.3f, 8f, -6f), 90f);
            OfficeKit.Prop(g, A2P.ShellChairRow, new Vector3(8f, 12f, 29.3f), 180f);
            OfficeKit.Prop(g, A2P.WaterCooler, new Vector3(-8f, 12f, 29.4f), 180f);
            OfficeKit.Prop(g, A2P.SwivelChair, new Vector3(-5.6f, 12f, 27.1f), 145f);
            OfficeKit.Prop(g, A2P.Planter, new Vector3(19.3f, 12f, -8f), -90f);
            OfficeKit.Prop(g, A2P.ShellChair, new Vector3(-17.1f, 16f, 8.5f), 75f);
            OfficeKit.Prop(g, A2P.Bin, new Vector3(-19.3f, 16f, -8.2f), 0f);
            OfficeKit.Prop(g, A2P.PottedPlant, new Vector3(-19.2f, 16f, 6.5f), 0f);
        }

        // ================================================================== pod and launch

        /// <summary>
        /// The boardroom pod: a cantilevered slab at y 24 (x 0..20, z -6..6) jutting from the east
        /// wall in front of the A6 bridge, with a stepped soffit, solid parapets north and south and
        /// a low curb at the open west end where the launch lands (about x +6). Nothing walkable lies
        /// within 6 m below it (the L4 gallery is 8 m down) or 8 m beside it, and the upper walls
        /// carry no ledges.
        /// </summary>
        static void A2_Pod(Transform root)
        {
            var g = A2_Group(root, "BoardroomPod");
            var pod = OfficeLayout.BoardroomPod;
            OfficeKit.Box(g, "Slab", A2M.ConcreteLight, new Vector3(pod.min.x, pod.min.y, pod.min.z), new Vector3(A2_InX, pod.max.y, pod.max.z));
            OfficeKit.Box(g, "Soffit_1", A2M.ConcreteDark, new Vector3(5f, 22.8f, -5f), new Vector3(A2_InX, pod.min.y, 5f));
            OfficeKit.Box(g, "Soffit_2", A2M.ConcreteDark, new Vector3(11f, 22f, -4f), new Vector3(A2_InX, 22.8f, 4f));
            OfficeKit.Box(g, "Parapet_N", A2M.ConcreteLight, new Vector3(0f, 24f, 5.7f), new Vector3(A2_InX, 25.05f, 6f));
            OfficeKit.Box(g, "Parapet_S", A2M.ConcreteLight, new Vector3(0f, 24f, -6f), new Vector3(A2_InX, 25.05f, -5.7f));
            OfficeKit.Box(g, "Curb_W", A2M.ConcreteLight, new Vector3(0f, 24f, -5.7f), new Vector3(0.3f, 24.25f, 5.7f));
            OfficeKit.Prop(g, A2P.ShellChair, new Vector3(16f, 24f, 4.4f), -130f);
            OfficeKit.Prop(g, A2P.PottedPlant, new Vector3(17.5f, 24f, -4.6f), 0f);
        }

        /// <summary>
        /// Launch exit 6 high on the west wall: a brass full-frame threshold tilted 30 deg up, set
        /// in a deep recess with a projecting concrete surround (a chute mouth). The surround is
        /// kept clear of the flight path (exit at 26 m/s, apex 31.8 m near x -10, landing on the pod
        /// near x +6). Behind it, a short dark chute closes the wall: seen only if the pair breaks.
        /// </summary>
        static void A2_Launch(Transform root)
        {
            var g = A2_Group(root, "LaunchExit");
            OfficeKit.Threshold(g, "Threshold_6_LaunchExit", OfficeLayout.LaunchExitCentre, OfficeLayout.LaunchExitRotation,
                OfficeLayout.LaunchOpening, "6", null, false, true);

            const float fx0 = -A2_InX, fx1 = -18.9f, yb = 25.8f, yt = 33.4f;
            OfficeKit.Box(g, "Surround_JambS", A2M.Concrete, new Vector3(fx0, yb, -3f), new Vector3(fx1, yt, -A2_LaunchHoleHalf));
            OfficeKit.Box(g, "Surround_JambN", A2M.Concrete, new Vector3(fx0, yb, A2_LaunchHoleHalf), new Vector3(fx1, yt, 3f));
            OfficeKit.Box(g, "Surround_Hood", A2M.Concrete, new Vector3(fx0, A2_LaunchHoleTop, -A2_LaunchHoleHalf), new Vector3(fx1, yt, A2_LaunchHoleHalf));
            OfficeKit.Box(g, "Surround_Sill", A2M.Concrete, new Vector3(fx0, yb, -A2_LaunchHoleHalf), new Vector3(fx1, A2_LaunchHoleBottom, A2_LaunchHoleHalf));

            // The dark chute behind the opening (outside the wall's west face, closed on every side).
            const float cx0 = -23.5f, cx1 = -20.25f, cz = 2.3f;
            OfficeKit.Box(g, "Chute_Floor", A2M.ConcreteDark, new Vector3(cx0, yb, -cz), new Vector3(cx1, A2_LaunchHoleBottom, cz));
            OfficeKit.Box(g, "Chute_Roof", A2M.ConcreteDark, new Vector3(cx0, A2_LaunchHoleTop, -cz), new Vector3(cx1, yt, cz));
            OfficeKit.Box(g, "Chute_Back", A2M.ConcreteDark, new Vector3(cx0 - 0.4f, yb, -cz), new Vector3(cx0, yt, cz));
            OfficeKit.Box(g, "Chute_S", A2M.ConcreteDark, new Vector3(cx0, A2_LaunchHoleBottom, -cz), new Vector3(cx1, A2_LaunchHoleTop, -A2_LaunchHoleHalf));
            OfficeKit.Box(g, "Chute_N", A2M.ConcreteDark, new Vector3(cx0, A2_LaunchHoleBottom, A2_LaunchHoleHalf), new Vector3(cx1, A2_LaunchHoleTop, cz));
        }

        // ================================================================== roof

        /// <summary>
        /// The roof at y 34: a concrete slab with coffer ribs on a 4 m grid (control_02), a long
        /// skylight band along X right over the launch path (x -18..14, z -3..3, so the flight passes
        /// under open sky with no ribs in the way), and two louvred square skylights (inside_02):
        /// S1 throws the sun onto the tree, S2 onto the L4 west gallery where the player first
        /// arrives. Glass casts no shadow; mullions and louvres do, so the pools are barred.
        /// </summary>
        static void A2_Roof(Transform root)
        {
            var g = A2_Group(root, "Roof");
            float under = A2_RoofY - A2_RoofT, ribBottom = under - A2_RibDepth;
            var band = new Vector4(-18f, -3f, 14f, 3f);
            var s1 = new Vector4(8f, -23f, 12f, -19f);
            var s2 = new Vector4(-12f, -11f, -8f, -7f);
            OfficeKit.Slab(g, "RoofSlab", A2M.ConcreteDark, new Vector3(-20.25f, 0f, -A2_OutZ), new Vector3(20.25f, 0f, A2_OutZ), A2_RoofY, A2_RoofT, band, s1, s2);

            var ribs = A2_Group(g, "CofferRibs");
            for (float x = -16f; x <= 16f; x += 4f)
            {
                bool crossesBand = x > band.x && x < band.z;
                if (crossesBand)
                {
                    OfficeKit.Box(ribs, $"RibX_{x}_S", A2M.Concrete, new Vector3(x - 0.2f, ribBottom, -A2_InZ), new Vector3(x + 0.2f, under, band.y));
                    OfficeKit.Box(ribs, $"RibX_{x}_N", A2M.Concrete, new Vector3(x - 0.2f, ribBottom, band.w), new Vector3(x + 0.2f, under, A2_InZ));
                }
                else
                {
                    OfficeKit.Box(ribs, $"RibX_{x}", A2M.Concrete, new Vector3(x - 0.2f, ribBottom, -A2_InZ), new Vector3(x + 0.2f, under, A2_InZ));
                }
            }
            for (float z = 3f; z <= 27f; z += 4f)
            {
                OfficeKit.Box(ribs, $"RibZ_{z}", A2M.Concrete, new Vector3(-A2_InX, ribBottom, z - 0.2f), new Vector3(A2_InX, under, z + 0.2f));
                OfficeKit.Box(ribs, $"RibZ_{-z}", A2M.Concrete, new Vector3(-A2_InX, ribBottom, -z - 0.2f), new Vector3(A2_InX, under, -z + 0.2f));
            }

            // Glazing just under the roof top, with mullions (band) and louvres (squares) below it.
            var sky = A2_Group(g, "Skylights");
            foreach (var h in new[] { band, s1, s2 })
                A2_Glass(sky, $"Glass_{h.x}_{h.y}", new Vector3(h.x, A2_RoofY - 0.08f, h.y), new Vector3(h.z, A2_RoofY - 0.04f, h.w), false);
            for (float x = band.x + 2f; x < band.z; x += 2f)
                OfficeKit.Box(sky, $"BandMullion_{x}", A2M.MetalDark, new Vector3(x - 0.05f, A2_RoofY - 0.18f, band.y), new Vector3(x + 0.05f, A2_RoofY - 0.08f, band.w), false);
            OfficeKit.Box(sky, "BandSpine", A2M.MetalDark, new Vector3(band.x, A2_RoofY - 0.18f, -0.05f), new Vector3(band.z, A2_RoofY - 0.08f, 0.05f), false);

            // Louvres: plates along X, tilted to the sun's elevation so light slips between them in stripes.
            var tilt = Quaternion.Euler(-23.5f, 0f, 0f);
            foreach (var h in new[] { s1, s2 })
                for (float z = h.y + 0.25f; z < h.w; z += 0.5f)
                    OfficeKit.BoxAt(sky, $"Louvre_{h.x}_{z:0.00}", A2M.MetalDark, new Vector3((h.x + h.z) * 0.5f, under + 0.2f, z), new Vector3(h.z - h.x, 0.4f, 0.03f), tilt, false);

            // Fake volumetric shafts along the sun direction, down through the fog (inside_02).
            var shafts = A2_Group(g, "LightShafts");
            var rot = A2_ShaftRotation();
            OfficeKit.LightShaft(shafts, "Shaft_S1_Tree", new Vector3(10f, under, -21f), rot, new Vector3(3.6f, 38f, 3.6f), A2_ShaftTint);
            OfficeKit.LightShaft(shafts, "Shaft_S2_Gallery", new Vector3(-10f, under, -9f), rot, new Vector3(3.6f, 21f, 3.6f), A2_ShaftTint);
            foreach (float x in new[] { -14f, -6f, 2f, 10f })
                OfficeKit.LightShaft(shafts, $"Shaft_Band_{x}", new Vector3(x, under, 0f), rot, new Vector3(7.6f, 26f, 5.6f), A2_ShaftTint);
        }

        // ================================================================== light

        /// <summary>
        /// Realtime lights only: small cool pools under each gallery soffit with darkness between,
        /// sconces on L4, and five shadow-casting spots (tree, stair landing, L4 arrival, pod,
        /// window bars at the entrance). The sun itself also falls through the skylights.
        /// </summary>
        static void A2_Lights(Transform root)
        {
            var g = A2_Group(root, "Lights");

            for (int n = 1; n <= 4; n++)
            {
                float y = OfficeLayout.L(n) - A2_SlabT - 0.05f;
                foreach (float z in new[] { -16f, 2f, 20f })
                {
                    OfficeKit.CeilingLight(g, $"Soffit_L{n}_W_{z}", new Vector3(-16.6f, y, z), new Vector2(0.6f, 1.2f), A2_Cool, 5f, 6f);
                    OfficeKit.CeilingLight(g, $"Soffit_L{n}_E_{z}", new Vector3(16.6f, y, z), new Vector2(0.6f, 1.2f), A2_Cool, 5f, 6f);
                }
                foreach (float x in new[] { -8f, 8f })
                {
                    OfficeKit.CeilingLight(g, $"Soffit_L{n}_N_{x}", new Vector3(x, y, 27.4f), new Vector2(1.2f, 0.6f), A2_Cool, 5f, 6f, n == 3 && x > 0f);
                    if (n >= 2)
                        OfficeKit.CeilingLight(g, $"Soffit_L{n}_S_{x}", new Vector3(x, y, -27.4f), new Vector2(1.2f, 0.6f), A2_Cool, 5f, 6f);
                }
            }

            // L4 has no ceiling: wall sconces.
            var sconces = new[]
            {
                (new Vector3(-19.6f, 18.2f, -10f), Vector3.right), (new Vector3(-19.6f, 18.2f, 12f), Vector3.right),
                (new Vector3(19.6f, 18.2f, -12f), Vector3.left), (new Vector3(19.6f, 18.2f, 12f), Vector3.left),
                (new Vector3(0f, 18.2f, 29.6f), Vector3.back), (new Vector3(0f, 18.2f, -29.6f), Vector3.forward),
            };
            int i = 0;
            foreach (var (pos, face) in sconces)
            {
                OfficeKit.BoxAt(g, $"Sconce_{i}", A2M.LightPanel, pos, new Vector3(0.6f, 0.15f, 0.1f), OfficeKit.Facing(face), false);
                OfficeKit.PointLight(g, $"Sconce_Light_{i}", pos + face * 0.4f, A2_Cool, 2f, 6f);
                i++;
            }

            // Shadow casters (5 of the 6 allowed).
            var sun = Quaternion.LookRotation(A2_SunDir, Vector3.up);
            OfficeKit.SpotLight(g, "Skylight_S1_Tree", new Vector3(10f, 33.2f, -21f), sun, A2_Daylight, 900f, 48f, 9f, true);
            OfficeKit.SpotLight(g, "Skylight_Band_Stair", new Vector3(8f, 33.2f, 0f), sun, A2_Daylight, 700f, 42f, 16f, true);
            OfficeKit.SpotLight(g, "Skylight_S2_L4", new Vector3(-10f, 33.2f, -9f), sun, A2_Daylight, 300f, 26f, 12f, true);
            var podAim = (new Vector3(10f, 24f, 0f) - new Vector3(4f, 33f, 0f)).normalized;
            OfficeKit.SpotLight(g, "Pod_Light", new Vector3(4f, 33f, 0f), Quaternion.LookRotation(podAim, Vector3.up), A2_Cool, 120f, 18f, 55f, true);
            var barsFrom = new Vector3(2f, 3.6f, -46f);
            OfficeKit.SpotLight(g, "Entrance_WindowBars", barsFrom, Quaternion.LookRotation((new Vector3(0.5f, 0f, -22f) - barsFrom).normalized, Vector3.up),
                A2_Daylight, 400f, 40f, 32f, true);

            // The pale glow outside the vestibule.
            OfficeKit.PointLight(g, "Outside_Glow", new Vector3(0f, 6f, -44f), new Color(0.85f, 0.9f, 0.95f), 25f, 16f);
        }

        // ================================================================== text

        /// <summary>
        /// Diegetic text (24+ props): wayfinding at every stair and area entrance, the plaque by the
        /// tree, the front desk, the cloakroom's lost property and tickets, a Facilities memo about
        /// numbering, an empty Employee of the Month. None of it addresses the player or explains a
        /// mechanic; it is an office that was lived in, abandoned at 4:47.
        /// </summary>
        static void A2_Signage(Transform root)
        {
            var g = A2_Group(root, "Signage");
            var toN = OfficeKit.Facing(Vector3.forward);  // on the south wall
            var toS = OfficeKit.Facing(Vector3.back);     // on the north wall
            var toE = OfficeKit.Facing(Vector3.right);    // on the west wall
            var toW = OfficeKit.Facing(Vector3.left);     // on the east wall

            // Entrance.
            OfficeKit.Sign(g, "RECORDS & CONTINUITY", new Vector3(0f, 5.25f, -29.73f), toN, 0.16f, WorldTextStyle.Brass);
            OfficeKit.Sign(g, "PULL", new Vector3(1.62f, 1.05f, -29.74f), toN, 0.045f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A2M.Brass, new Vector2(0.2f, 0.08f));

            // Tree plaque on the planter rim.
            OfficeKit.Sign(g, "THIS FICUS WAS PLANTED\nAT THE OPENING OF THE ATRIUM\n4 MARCH 1974\nBY THEODORA VANE\nDIRECTOR OF CONTINUITY\n\nWATERED THURSDAYS  - G.F.",
                new Vector3(0f, 0.32f, -8.52f), OfficeKit.Facing(Vector3.back), 0.022f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A2M.Brass, new Vector2(0.62f, 0.3f));

            // Front desk.
            OfficeKit.Sign(g, "WALTER BRISK\nFRONT DESK", new Vector3(9.58f, 0.9f, -22f), toW, 0.04f, WorldTextStyle.Brass, TextAnchor.MiddleCenter, A2M.WoodDark, new Vector2(0.42f, 0.14f));
            OfficeKit.Sign(g, "STAFF SIGN-OUT\n\nA. Pell ................ 4:47\nT. Varga .............. 4:47\nR. Abernathy ........ 4:47\nH. Lindqvist .......... 4:47\nM. Okonjo ............ 4:47\nI. Castellano ......... 4:47\n____________ ...... ____",
                new Vector3(9.85f, 1.115f, -21.8f), Quaternion.LookRotation(Vector3.up, Vector3.right), 0.014f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A2M.Paper, new Vector2(0.21f, 0.29f));
            OfficeKit.Sign(g, "TAKE A NUMBER", new Vector3(8.63f, 1.4f, -25.2f), toW, 0.03f, WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter, A2M.ConcreteDark, new Vector2(0.26f, 0.06f));
            OfficeKit.Sign(g, "EMPLOYEE\nOF THE MONTH\n\nNOVEMBER 1979", new Vector3(19.72f, 1.95f, -17f), toW, 0.035f, WorldTextStyle.Brass, TextAnchor.MiddleCenter, A2M.WoodDark, new Vector2(0.55f, 0.75f));
            OfficeKit.Box(g, "EmployeeOfMonth_EmptyPhoto", A2M.Paper, new Vector3(19.7f, 1.32f, -17.15f), new Vector3(19.71f, 1.6f, -16.85f), false);
            OfficeKit.Sign(g, "______________", new Vector3(19.7f, 1.27f, -17f), toW, 0.03f, WorldTextStyle.Brass);

            // Lift out of service (on the ground-floor lift doors).
            OfficeKit.Sign(g, "LIFT 2\nOUT OF SERVICE\n\nEngineer booked\nfor the 14th.\n- Facilities", new Vector3(19.64f, 1.45f, 13f), toW, 0.022f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A2M.Paper, new Vector2(0.24f, 0.3f));

            // Cloakroom.
            OfficeKit.Sign(g, "CLOAKROOM", new Vector3(-16.13f, 1.75f, -11.3f), toE, 0.06f, WorldTextStyle.Brass);
            OfficeKit.Sign(g, "41    42    43    44    45    46          48", new Vector3(-19.72f, 1.62f, -10f), toE, 0.026f, WorldTextStyle.Brass);
            OfficeKit.Sign(g, "HALE & ORMSBY\nCLOAKROOM\n\nNo. 47", new Vector3(-18.2f, 1.045f, -10.6f), Quaternion.LookRotation(Vector3.up, Vector3.left), 0.012f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A2M.Paper, new Vector2(0.07f, 0.11f));
            OfficeKit.Sign(g, "LOST PROPERTY\n\nUmbrella, black, bamboo handle\n(lobby, 14 Feb)\nOne glove, tan, left\nReading glasses, green case\nKey on a blue tag marked B\nCardigan, oatmeal (L3)\n\nHeld at the cloakroom 30 days,\nthen to Facilities.\n- W. Brisk, Front Desk",
                new Vector3(-19.7f, 1.45f, -14.1f), toE, 0.017f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A2M.Paper, new Vector2(0.42f, 0.55f));
            OfficeKit.Sign(g, "FACILITIES NOTICE\n\nRoom numbers in this building\nare under review.\n\nTwo doors on L3 have been found\ncarrying the same number. Until\nthis is resolved, staff are asked\nnot to re-letter doors themselves.\nLoose tiles to the cloakroom.\n\nG. Fenwick, Facilities\n2 November 1979",
                new Vector3(-19.7f, 1.45f, -13.45f), toE, 0.017f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A2M.Paper, new Vector2(0.42f, 0.55f));

            // Wayfinding at the area entrances.
            OfficeKit.Sign(g, "STAIR B", new Vector3(-19.72f, 3.3f, -24f), toE, 0.16f, WorldTextStyle.LightOnDark);
            OfficeKit.Sign(g, "STAIR B", new Vector3(-19.72f, 11.3f, -24f), toE, 0.16f, WorldTextStyle.LightOnDark);
            OfficeKit.Sign(g, "CAFETERIA", new Vector3(19.72f, 7.3f, 0f), toW, 0.16f, WorldTextStyle.LightOnDark);
            OfficeKit.Sign(g, "RECORDS", new Vector3(0f, 15.3f, 29.72f), toS, 0.18f, WorldTextStyle.LightOnDark);
            OfficeKit.Sign(g, "RECEPTION", new Vector3(-19.72f, 17.7f, -1f), toE, 0.14f, WorldTextStyle.LightOnDark);
            OfficeKit.Sign(g, "BOARDROOM", new Vector3(19.72f, 27.4f, 0f), toW, 0.14f, WorldTextStyle.Brass);

            // Level markers at the top of each stair.
            A2_LevelMarker(g, "1", "CAFETERIA", new Vector3(16.5f, 4f, 29.72f), toS);
            A2_LevelMarker(g, "2", "ARCHIVE  /  STAIR B", new Vector3(0f, 8f, 29.72f), toS);
            A2_LevelMarker(g, "2", "STAIR B", new Vector3(16.5f, 8f, -29.72f), toN);
            A2_LevelMarker(g, "3", "RECORDS", new Vector3(-16.5f, 12f, 29.72f), toS);
            A2_LevelMarker(g, "4", "RECEPTION", new Vector3(-16.5f, 16f, -29.72f), toN);
        }

        /// <summary>A big floor numeral with a caption under it, painted on a wall facing the top of a stair.</summary>
        static void A2_LevelMarker(Transform parent, string numeral, string caption, Vector3 wallPointAtFloor, Quaternion facing)
        {
            OfficeKit.Sign(parent, numeral, wallPointAtFloor + Vector3.up * 2.4f, facing, 0.55f, WorldTextStyle.LightOnDark);
            OfficeKit.Sign(parent, caption, wallPointAtFloor + Vector3.up * 1.75f, facing, 0.12f, WorldTextStyle.LightOnDark);
        }

        // ================================================================== zones

        /// <summary>A fall zone under the floor (anything clipping through respawns) and no safe ground on the launch surround.</summary>
        static void A2_Zones(Transform root)
        {
            var below = new Bounds();
            below.SetMinMax(new Vector3(-A2_InX, -8f, -A2_InZ), new Vector3(A2_InX, -4f, A2_InZ));
            OfficeKit.FallZone(root, "A2_FallBelowFloor", below);

            var launch = new Bounds();
            launch.SetMinMax(new Vector3(-24f, 25.5f, -3.2f), new Vector3(-18.5f, A2_RoofY, 3.2f));
            OfficeKit.NoSafeGround(root, "A2_NoSafe_LaunchSurround", launch);
        }
    }
}
