using Kit.Gameplay.Signage;
using UnityEngine;
using UnityEngine.Rendering;
using A5M = Kit.Editor.OfficeKit.M;
using A5P = Kit.Editor.OfficeKit.PropIds;

namespace Kit.Editor
{
    /// <summary>
    /// A5 Stair Core ("Stair B"): a 38 m concrete shaft (brutal_01) beside the atrium's west wall.
    /// Open concrete flights wrap a square void (OfficeLayout.A5Void) from the basement (L-1, y -4)
    /// to the top landing (L6, y 24). At the bottom of the void sits brass floor hatch 6, the K3
    /// launch: dropping in from the top comes out of the atrium's west wall high above the lobby.
    ///
    /// Layout, walking upward:
    /// - L-1 basement: a concrete floor with the hatch (over a 2.5 m safety pit) under the void, a
    ///   straight flight between two walls up to L0 along the north wall.
    /// - Landings L0, L2, L4, L6 stack in the east band (x -28..-20). L0 and L2 meet the atrium
    ///   sockets A5_L0 / A5_L2. Between two landings the stair makes one full turn round the void:
    ///   north side (west, 14 risers), west side (south, 12), south side (east, 14) = 8 m.
    /// - L6: an L-shaped top landing over the east and north sides. One gap in the north railing,
    ///   lined up with the hatch, with a lone shell chair and a lamp facing the drop.
    /// - One great curved wall sweeps the north-west corner; a slot of sky in the roof throws a hard
    ///   shaft down the void and a bar of sun across the stair and the curve.
    /// Everything is built in world space under <c>root</c>. All helpers carry the A5_ prefix
    /// because every area's file is a partial of the same class.
    /// </summary>
    public static partial class OfficeBuilder
    {
        // ------------------------------------------------------------------ plan numbers

        const float A5_Strip = 2f;          // width of the stair ring round the void (flights and corner landings)
        const float A5_Rise = 0.2f;         // step rise (player step offset is 0.3)
        const float A5_Tread = 0.3f;
        const float A5_SlabT = 0.3f;        // landing slab thickness
        const float A5_StepDepth = 0.5f;    // each open-stair tread block hangs this far below its top
        const float A5_RailIn = 0.08f;      // railings sit this far inside the walking edge, never in the void
        const float A5_RailH = 1.05f;
        const float A5_WallT = 0.4f;        // north wall thickness (west and south walls are thickened to the ring)
        const int A5_LongFlight = 14;       // north and south sides: 2.8 m
        const int A5_ShortFlight = 12;      // west side: 2.4 m (14 + 12 + 14 risers = one 8 m turn)
        const int A5_BasementFlight = 20;   // L-1 to L0

        static float A5_VX0 => OfficeLayout.A5Void.min.x;                    // -36
        static float A5_VX1 => OfficeLayout.A5Void.max.x;                    // -28
        static float A5_VZ0 => OfficeLayout.A5Void.min.z;                    // -30
        static float A5_VZ1 => OfficeLayout.A5Void.max.z;                    // -22
        static float A5_RingX0 => A5_VX0 - A5_Strip;                         // -38: inner face of the thickened west wall
        static float A5_RingZ0 => A5_VZ0 - A5_Strip;                         // -32: inner face of the thickened south wall
        static float A5_RingZ1 => A5_VZ1 + A5_Strip;                         // -20: north edge of the stair ring
        static float A5_NorthFace => OfficeLayout.A5.max.z - A5_WallT;       // -14.4
        static float A5_FaceX => OfficeLayout.A5.max.x - OfficeLayout.AtriumWallThickness * 0.5f; // -20.25: atrium wall face, our side

        /// <summary>Basement flight: along the north wall, rising east from the well to L0.</summary>
        static float A5_StairX0 => A5_VX1 + 0.4f;                                            // -27.6
        static float A5_StairX1 => A5_StairX0 + A5_BasementFlight * A5_Tread;                 // -21.6
        static Vector4 A5_StairwellHole => new Vector4(A5_StairX0, A5_NorthFace - 1.8f, A5_StairX1, A5_NorthFace);

        /// <summary>The roof slot of sky: 1.5 m wide, the length of the void, centred over the hatch.</summary>
        static Vector4 A5_Slot => new Vector4(OfficeLayout.LaunchHatchCentre.x - 0.75f, A5_VZ0, OfficeLayout.LaunchHatchCentre.x + 0.75f, A5_VZ1);

        static readonly Color A5_Green = new Color(0.55f, 1f, 0.72f);    // emergency fittings: cool, so the one warm accent stays the lamp
        static readonly Color A5_Amber = new Color(1f, 0.7f, 0.42f);

        // ------------------------------------------------------------------ entry point

        /// <summary>Builds the whole stair core under <paramref name="root"/>.</summary>
        static partial void BuildArea_A5(Transform root)
        {
            OfficeKit.Zone(root, "A5", OfficeLayout.A5);
            A5_Shell(root);
            A5_Basement(root);
            for (int k = 0; k < 3; k++) A5_Turn(root, k, OfficeLayout.L(2 * k));
            A5_Landing(root, 0);
            A5_Landing(root, 2);
            A5_Landing(root, 4);
            A5_Landing(root, 6);
            A5_TopLanding(root);
            A5_CurvedWall(root);
            A5_Sky(root);

            // Denser, darker haze in the shaft so the skylight shaft reads and the bottom stays a little out of reach.
            var fog = new Bounds();
            fog.SetMinMax(new Vector3(OfficeLayout.A5.min.x + 0.5f, OfficeLayout.A5.min.y - 3f, OfficeLayout.A5.min.z + 0.5f),
                          new Vector3(OfficeLayout.A5.max.x - 0.5f, OfficeLayout.A5.max.y - 0.5f, OfficeLayout.A5.max.z - 0.5f));
            OfficeKit.FogZone(root, "A5_StairCore", fog, new Color(0.13f, 0.155f, 0.185f), 3f, 48f, 2f, 10f);
        }

        // ------------------------------------------------------------------ shell

        /// <summary>
        /// Outer walls, basement floor (with the hatch hole) and roof (with the sky slot). The west
        /// and south walls are thickened to the stair ring, so the flights run against solid concrete
        /// as in brutal_01. The atrium owns the x -20 wall from y 0 up between z -30 and the north
        /// end; we close the rest of our east side (the strip south of the atrium, and the basement).
        /// </summary>
        static void A5_Shell(Transform root)
        {
            var g = A5_Group(root, "Shell");
            var e = OfficeLayout.A5;
            float x0 = e.min.x, x1 = e.max.x, z0 = e.min.z, z1 = e.max.z, y0 = e.min.y, y1 = e.max.y;
            float face = A5_FaceX;

            OfficeKit.Box(g, "Wall_West", A5M.Concrete, new Vector3(x0, y0, z0), new Vector3(A5_RingX0, y1, z1));
            OfficeKit.Box(g, "Wall_South", A5M.Concrete, new Vector3(A5_RingX0, y0, z0), new Vector3(face, y1, A5_RingZ0));
            OfficeKit.Box(g, "Wall_North", A5M.Concrete, new Vector3(A5_RingX0, y0, A5_NorthFace), new Vector3(face, y1, z1));
            OfficeKit.Box(g, "Wall_East_South", A5M.Concrete, new Vector3(face, y0, A5_RingZ0), new Vector3(x1, y1, OfficeLayout.A2.min.z));
            OfficeKit.Box(g, "Wall_East_Basement", A5M.Concrete, new Vector3(face, y0, OfficeLayout.A2.min.z), new Vector3(x1, OfficeLayout.L(0) - A5_SlabT, A5_NorthFace));

            var c = OfficeLayout.LaunchHatchCentre;
            var hs = OfficeLayout.HatchOpening * 0.5f;
            OfficeKit.Slab(g, "Floor_L-1", A5M.ConcreteFloor, new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z1), y0, 0.4f,
                new Vector4(c.x - hs.x, c.z - hs.y, c.x + hs.x, c.z + hs.y));

            OfficeKit.Slab(g, "Roof", A5M.ConcreteDark, new Vector3(x0, 0f, z0), new Vector3(face, 0f, z1), y1, 0.5f, A5_Slot);
        }

        // ------------------------------------------------------------------ basement

        /// <summary>
        /// L-1: the launch hatch (brass threshold 6) over a closed 2.5 m pit with climb-out blocks,
        /// the flight up to L0 between the north wall and a tall concrete wall, overflow archive
        /// shelving under L0, and two dim lights (one warm bulb, one green fitting).
        /// </summary>
        static void A5_Basement(Transform root)
        {
            var g = A5_Group(root, "Basement_L-1");
            float y = OfficeLayout.L(-1);
            var c = OfficeLayout.LaunchHatchCentre;
            var hs = OfficeLayout.HatchOpening * 0.5f;

            // Threshold 6: front faces up, so a fall from above crosses front-to-back.
            OfficeKit.Threshold(g, "Threshold_6_LaunchHatch", c, OfficeLayout.LaunchHatchRotation, OfficeLayout.HatchOpening, "6",
                null, false, true, A5M.Brass);

            // The pit: only reached if hatch 6 ever loses its partner. Walls stop at the slab's underside.
            float px0 = c.x - hs.x, px1 = c.x + hs.x, pz0 = c.z - hs.y, pz1 = c.z + hs.y;
            float slabBottom = y - 0.4f, pitFloor = y - 2.5f, t = 0.4f;
            OfficeKit.Box(g, "Pit_Floor", A5M.ConcreteDark, new Vector3(px0 - t, pitFloor - t, pz0 - t), new Vector3(px1 + t, pitFloor, pz1 + t));
            OfficeKit.Box(g, "Pit_West", A5M.ConcreteDark, new Vector3(px0 - t, pitFloor - t, pz0 - t), new Vector3(px0, slabBottom, pz1 + t));
            OfficeKit.Box(g, "Pit_East", A5M.ConcreteDark, new Vector3(px1, pitFloor - t, pz0 - t), new Vector3(px1 + t, slabBottom, pz1 + t));
            OfficeKit.Box(g, "Pit_South", A5M.ConcreteDark, new Vector3(px0, pitFloor - t, pz0 - t), new Vector3(px1, slabBottom, pz0));
            OfficeKit.Box(g, "Pit_North", A5M.ConcreteDark, new Vector3(px0, pitFloor - t, pz1), new Vector3(px1, slabBottom, pz1 + t));
            // Climb-out blocks along the south side, each a short hop. The highest top stays 1 m under
            // the hatch plane, so a player stepping off the rim crosses the plane before touching one.
            float[] tops = { pitFloor + 0.3f, pitFloor + 0.7f, pitFloor + 1.1f, pitFloor + 1.5f };
            for (int i = 0; i < tops.Length; i++)
            {
                float bx0 = px0 + i * (px1 - px0) / tops.Length, bx1 = px0 + (i + 1) * (px1 - px0) / tops.Length;
                OfficeKit.Box(g, $"Pit_Block_{i + 1}", A5M.Concrete, new Vector3(bx0, pitFloor, pz0), new Vector3(bx1, tops[i], pz0 + 0.9f));
            }
            // A maintenance tag lying by the rim, read from the south (the side the stair arrives from).
            OfficeKit.Sign(g, "INSPECTED\n4/77   W.B.", new Vector3(c.x + 1.05f, y + 0.015f, pz0 - 0.32f),
                Quaternion.LookRotation(Vector3.up, Vector3.forward), 0.018f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter,
                A5M.Paper, new Vector2(0.17f, 0.085f));

            // Flight L-1 -> L0: solid steps along the north wall, a tall wall on the other side.
            OfficeKit.Stairs(g, "Stair_L-1_L0", A5M.ConcreteFloor, new Vector3(A5_StairX0, y, A5_NorthFace - 0.9f), Vector3.right, 1.8f, A5_BasementFlight);
            OfficeKit.Box(g, "Stair_L-1_Wall", A5M.Concrete, new Vector3(A5_StairX0, y, A5_NorthFace - 2.2f),
                new Vector3(A5_StairX1, OfficeLayout.L(0) + 1.1f, A5_NorthFace - 1.8f));

            // Level markers: one met at the foot of the flight, one on the far wall.
            A5_Marker(g, "-1", new Vector3(A5_VX1 - 1.6f, y + 2.1f, A5_NorthFace), Vector3.back);
            A5_Marker(g, "-1", new Vector3(-24f, y + 1.9f, A5_RingZ0), Vector3.forward);

            // Records overflow under L0: lived in, kept to the edges so the floor stays empty.
            OfficeKit.Prop(g, A5P.ArchiveShelf, new Vector3(A5_FaceX - 0.26f, y, -28.4f), -90f);
            OfficeKit.Prop(g, A5P.ArchiveShelf, new Vector3(A5_FaceX - 0.26f, y, -26.2f), -90f);
            OfficeKit.Prop(g, A5P.FilingCabinet, new Vector3(-26.4f, y, A5_RingZ0 + 0.33f), 0f);
            OfficeKit.Prop(g, A5P.FilingCabinet, new Vector3(-25.8f, y, A5_RingZ0 + 0.33f), 0f);
            OfficeKit.Prop(g, A5P.PaperStack, new Vector3(-21.4f, y, -23.6f), 12f);
            OfficeKit.Prop(g, A5P.Bin, new Vector3(-23.4f, y, A5_RingZ0 + 0.4f), 0f);

            A5_Fitting(g, "Bulb_Warm", new Vector3(-24.5f, y + 2.6f, A5_NorthFace - 2.2f), Vector3.back, true);
            A5_Fitting(g, "Emergency_West", new Vector3(A5_RingX0, y + 2.4f, c.z), Vector3.right, false);

            // Safety net under the pit only (every other drop here lands on a floor).
            var under = new Bounds();
            under.SetMinMax(new Vector3(OfficeLayout.A5.min.x, y - 8.5f, OfficeLayout.A5.min.z), new Vector3(OfficeLayout.A5.max.x, pitFloor - t - 0.6f, OfficeLayout.A5.max.z));
            OfficeKit.FallZone(g, "FallZone_UnderPit", under);
        }

        // ------------------------------------------------------------------ one turn of the stair

        /// <summary>
        /// One full turn round the void, from the east landing at <paramref name="h"/> to the next one
        /// 8 m up: north side heading west, north-west corner, west side heading south, south-west
        /// corner, south side heading east, arriving at the south-east corner of the next landing.
        /// Each side is flat - flight - flat, with steel railings on the void side (and on the well
        /// side of the north flights).
        /// </summary>
        static void A5_Turn(Transform root, int k, float h)
        {
            var g = A5_Group(root, $"Turn_{k}_y{h:0}");
            float h1 = h + A5_LongFlight * A5_Rise, h2 = h1 + A5_ShortFlight * A5_Rise, h3 = h2 + A5_LongFlight * A5_Rise;
            float side = A5_VX1 - A5_VX0; // 8
            float longRun = A5_LongFlight * A5_Tread, shortRun = A5_ShortFlight * A5_Tread;

            // North side, heading west.
            float nA = A5_VX1 - (side - longRun) * 0.5f, nB = nA - longRun;   // -29.9, -34.1
            A5_Flat(g, "N_Flat", nA, A5_VZ1, A5_VX1, A5_RingZ1, h);
            A5_Flight(g, "N_Flight", new Vector3(nA, h, A5_VZ1 + A5_Strip * 0.5f), Vector3.left, A5_Strip, A5_LongFlight);
            A5_Flat(g, "NW_Landing", A5_RingX0, A5_VZ1, nB, A5_RingZ1, h1);

            // West side, heading south.
            float wA = A5_VZ1 - (side - shortRun) * 0.5f, wB = wA - shortRun; // -24.2, -27.8
            A5_Flat(g, "W_Flat", A5_RingX0, wA, A5_VX0, A5_VZ1, h1);
            A5_Flight(g, "W_Flight", new Vector3(A5_VX0 - A5_Strip * 0.5f, h1, wA), Vector3.back, A5_Strip, A5_ShortFlight);
            A5_Flat(g, "W_Flat2", A5_RingX0, A5_VZ0, A5_VX0, wB, h2);

            // South side, heading east.
            float sA = A5_VX0 + (side - longRun) * 0.5f, sB = sA + longRun;   // -34.1, -29.9
            A5_Flat(g, "SW_Landing", A5_RingX0, A5_RingZ0, sA, A5_VZ0, h2);
            A5_Flight(g, "S_Flight", new Vector3(sA, h2, A5_VZ0 - A5_Strip * 0.5f), Vector3.right, A5_Strip, A5_LongFlight);
            A5_Flat(g, "S_Flat", sB, A5_RingZ0, A5_VX1, A5_VZ0, h3);

            // Railings, inset from the void edge so nothing enters the drop line.
            float rN = A5_VZ1 + A5_RailIn, rW = A5_VX0 - A5_RailIn, rS = A5_VZ0 - A5_RailIn, rWell = A5_RingZ1 - A5_RailIn, rE = A5_VX1 + A5_RailIn;
            A5_Rail(g, "Rail_N_1", new Vector3(rE, h, rN), new Vector3(nA, h, rN));
            A5_Rail(g, "Rail_N_2", new Vector3(nA, h, rN), new Vector3(nB, h1, rN));
            A5_Rail(g, "Rail_N_3", new Vector3(nB, h1, rN), new Vector3(rW, h1, rN));
            A5_Rail(g, "Rail_NWell_1", new Vector3(rE, h, rWell), new Vector3(nA, h, rWell));
            A5_Rail(g, "Rail_NWell_2", new Vector3(nA, h, rWell), new Vector3(nB, h1, rWell));
            A5_Rail(g, "Rail_NWell_3", new Vector3(nB, h1, rWell), new Vector3(A5_RingX0, h1, rWell));
            A5_Rail(g, "Rail_W_1", new Vector3(rW, h1, rN), new Vector3(rW, h1, wA));
            A5_Rail(g, "Rail_W_2", new Vector3(rW, h1, wA), new Vector3(rW, h2, wB));
            A5_Rail(g, "Rail_W_3", new Vector3(rW, h2, wB), new Vector3(rW, h2, rS));
            A5_Rail(g, "Rail_S_1", new Vector3(rW, h2, rS), new Vector3(sA, h2, rS));
            A5_Rail(g, "Rail_S_2", new Vector3(sA, h2, rS), new Vector3(sB, h3, rS));
            A5_Rail(g, "Rail_S_3", new Vector3(sB, h3, rS), new Vector3(rE, h3, rS));

            // Dim emergency fittings on the two corner landings.
            A5_Fitting(g, "Emergency_NW", new Vector3(A5_RingX0, h1 + 2.3f, A5_VZ1 + 1.2f), Vector3.right, false);
            A5_Fitting(g, "Emergency_SW", new Vector3(A5_RingX0 + 1f, h2 + 2.3f, A5_RingZ0), Vector3.forward, false);
        }

        // ------------------------------------------------------------------ east landings

        /// <summary>
        /// A landing in the east band at level <paramref name="level"/> (0, 2, 4 or 6): the slab runs
        /// from the void's east edge to the atrium wall (meeting the A5_L0 / A5_L2 openings exactly),
        /// with railings on the void and the north well, an emergency fitting, a painted level marker
        /// and the dressing for that level.
        /// </summary>
        static void A5_Landing(Transform root, int level)
        {
            float h = OfficeLayout.L(level);
            var g = A5_Group(root, $"Landing_L{level}");
            var holes = level == 0 ? new[] { A5_StairwellHole } : new Vector4[0];
            OfficeKit.Slab(g, "Slab", A5M.ConcreteFloor, new Vector3(A5_VX1, 0f, A5_RingZ0), new Vector3(OfficeLayout.A5.max.x, 0f, A5_NorthFace), h, A5_SlabT, holes);

            float rx = A5_VX1 + A5_RailIn;
            float zFrom = level == 0 ? A5_RingZ0 : A5_VZ0 - A5_RailIn; // at L0 nothing arrives from the south side yet
            A5_Rail(g, "Rail_Void", new Vector3(rx, h, zFrom), new Vector3(rx, h, A5_VZ1 + A5_RailIn));
            if (level < 6) A5_Rail(g, "Rail_Well", new Vector3(rx, h, A5_RingZ1 - A5_RailIn), new Vector3(rx, h, A5_NorthFace));

            A5_Fitting(g, "Emergency", new Vector3(A5_FaceX, h + 2.7f, -28.7f), Vector3.left, false);

            // Painted level marker on the short wall south of the atrium, facing anyone arriving up the stair.
            var markerAt = new Vector3(A5_FaceX, h + 1.9f, (A5_RingZ0 + OfficeLayout.A2.min.z) * 0.5f);
            if (level != 4) A5_Marker(g, level.ToString(), markerAt, Vector3.left);

            switch (level)
            {
                case 0: A5_DoorDressing(g, OfficeLayout.A5_L0.Opening); A5_DressL0(g, h); break;
                case 2: A5_DoorDressing(g, OfficeLayout.A5_L2.Opening); A5_DressL2(g, h); break;
                case 4: A5_DressL4(g, h, markerAt); break;
            }
        }

        /// <summary>
        /// Our side of an atrium doorway: a steel frame, both fire-door leaves swung right back
        /// against the wall (each signed KEEP SHUT), and the STAIR B plate over the head.
        /// </summary>
        static void A5_DoorDressing(Transform g, Bounds opening)
        {
            float f = A5_FaceX, h = opening.min.y, top = opening.max.y, zs = opening.min.z, zn = opening.max.z;
            OfficeKit.Box(g, "DoorFrame_S", A5M.MetalDark, new Vector3(f - 0.08f, h, zs - 0.1f), new Vector3(f, top + 0.1f, zs));
            OfficeKit.Box(g, "DoorFrame_N", A5M.MetalDark, new Vector3(f - 0.08f, h, zn), new Vector3(f, top + 0.1f, zn + 0.1f));
            OfficeKit.Box(g, "DoorFrame_Head", A5M.MetalDark, new Vector3(f - 0.08f, top, zs), new Vector3(f, top + 0.1f, zn));

            float leaf = (zn - zs) * 0.5f;
            OfficeKit.Box(g, "FireDoor_S", A5M.Frame, new Vector3(f - 0.05f, h, zs - 0.1f - leaf), new Vector3(f, top - 0.05f, zs - 0.1f));
            OfficeKit.Box(g, "FireDoor_N", A5M.Frame, new Vector3(f - 0.05f, h, zn + 0.1f), new Vector3(f, top - 0.05f, zn + 0.1f + leaf));
            foreach (float z in new[] { zs - 0.1f - leaf * 0.5f, zn + 0.1f + leaf * 0.5f })
                OfficeKit.Sign(g, "FIRE DOOR\nKEEP SHUT", new Vector3(f - 0.06f, h + 1.5f, z), OfficeKit.Facing(Vector3.left), 0.07f,
                    WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter, A5M.MetalDark, new Vector2(0.5f, 0.25f));

            A5_WallSign(g, "STAIR B", new Vector3(f, top + 0.4f, (zs + zn) * 0.5f), Vector3.left, 0.2f, WorldTextStyle.LightOnDark, A5M.MetalDark, new Vector2(1.1f, 0.34f));
        }

        /// <summary>L0: corkboard with the Facilities numbering notice and a lost-numeral note, and a philodendron.</summary>
        static void A5_DressL0(Transform g, float h)
        {
            float f = A5_FaceX;
            OfficeKit.Prop(g, A5P.Corkboard, new Vector3(f - 0.015f, h + 1.0f, -18.5f), -90f);
            A5_WallSign(g, "NOTICE\n\nStair B levels are numbered\nfor the convenience of staff\nand do not necessarily\ncorrespond to floors.\n\nFacilities Dept.   3/76",
                new Vector3(f - 0.03f, h + 1.47f, -18.2f), Vector3.left, 0.02f, WorldTextStyle.DarkOnLight, A5M.Paper, new Vector2(0.46f, 0.34f));
            A5_WallSign(g, "LOST\nBrass door numeral '9'.\nIf found, return to\nW. Brisk, Facilities\next. 47",
                new Vector3(f - 0.03f, h + 1.5f, -18.85f), Vector3.left, 0.017f, WorldTextStyle.DarkOnLight, A5M.Paper, new Vector2(0.3f, 0.2f));
            OfficeKit.Prop(g, A5P.PottedPlant, new Vector3(f - 0.65f, h, -20.6f), 30f);
        }

        /// <summary>L2: a wall clock (stopped at 4:47 like every clock here) with a complaint under it, a laminated evacuation plan, a ticket dispenser.</summary>
        static void A5_DressL2(Transform g, float h)
        {
            float f = A5_FaceX;
            OfficeKit.Prop(g, A5P.WallClock, new Vector3(f - 0.025f, h + 2.22f, -19.0f), -90f);
            A5_WallSign(g, "Reported to Facilities\n11/76,  2/77,  5/77.\nM. Okonjo", new Vector3(f, h + 1.85f, -19.0f), Vector3.left, 0.02f,
                WorldTextStyle.DarkOnLight, A5M.Paper, new Vector2(0.3f, 0.12f));
            A5_EvacPlan(g, new Vector3(f, h + 1.55f, -16.6f), Vector3.left);
            OfficeKit.Prop(g, A5P.TicketDispenser, new Vector3(f - 0.5f, h, -15.1f), -90f);
        }

        /// <summary>
        /// L4: a landing with no door. The doorway in the atrium wall is filled in, the painted level
        /// number has been painted over, and the dead end has become the smokers' landing.
        /// </summary>
        static void A5_DressL4(Transform g, float h, Vector3 markerAt)
        {
            float f = A5_FaceX;
            var door = OfficeLayout.A5_L0.Opening; // same plan position as the doorways below
            OfficeKit.Box(g, "SealedDoorway", A5M.ConcreteDark, new Vector3(f - 0.03f, h, door.min.z), new Vector3(f, h + door.size.y, door.max.z), false);
            A5_WallSign(g, "DOOR SEALED BY ORDER\nOF FACILITIES   2/78", new Vector3(f - 0.03f, h + 1.6f, door.center.z), Vector3.left, 0.07f, WorldTextStyle.LightOnDark);

            OfficeKit.Box(g, "MarkerPaintedOver", A5M.ConcreteLight, new Vector3(f - 0.015f, markerAt.y - 1.1f, markerAt.z - 0.8f),
                new Vector3(f, markerAt.y + 1.1f, markerAt.z + 0.8f), false);
            A5_WallSign(g, "LEVEL NUMBER\nUNDER REVIEW", new Vector3(f, markerAt.y - 1.45f, markerAt.z), Vector3.left, 0.06f, WorldTextStyle.LightOnDark);

            A5_WallSign(g, "SMOKES\nR.A.   IIII IIII IIII I\nT.V.   IIII IIII II\nW.B.   IIII IIII IIII IIII III\nG.F.   I",
                new Vector3(f, h + 1.45f, -18.8f), Vector3.left, 0.05f, WorldTextStyle.LightOnDark);
            OfficeKit.Prop(g, A5P.Bin, new Vector3(f - 0.4f, h, -17.9f), 0f);
        }

        // ------------------------------------------------------------------ top landing

        /// <summary>
        /// L6: the north part of the top landing (the east part is the L6 landing slab), the railing
        /// with its one gap lined up with the hatch 28 m below, and the tableau: a lone shell chair
        /// facing the drop, a lamp on a side table (the warm accent), a note taped to the railing and
        /// a Facilities memo about the chair.
        /// </summary>
        static void A5_TopLanding(Transform root)
        {
            float h = OfficeLayout.L(6);
            var g = A5_Group(root, "TopLanding_L6");
            OfficeKit.Box(g, "Slab_North", A5M.ConcreteFloor, new Vector3(A5_RingX0, h - A5_SlabT, A5_VZ1), new Vector3(A5_VX1, h, A5_NorthFace));

            float rz = A5_VZ1 + A5_RailIn;
            float cx = OfficeLayout.LaunchHatchCentre.x, gap = OfficeLayout.HatchOpening.x * 0.5f;
            A5_Rail(g, "Rail_Void_W", new Vector3(A5_RingX0, h, rz), new Vector3(cx - gap, h, rz));
            A5_Rail(g, "Rail_Void_E", new Vector3(cx + gap, h, rz), new Vector3(A5_VX1 + A5_RailIn, h, rz));

            OfficeKit.Prop(g, A5P.ShellChair, new Vector3(cx, h, A5_VZ1 + 2.4f), 180f);
            OfficeKit.Prop(g, A5P.SideTable, new Vector3(cx + 1.25f, h, A5_VZ1 + 2.6f), 0f);
            OfficeKit.Prop(g, A5P.DeskLamp, new Vector3(cx + 1.25f, h + A5P.SizeOf(A5P.SideTable).y, A5_VZ1 + 2.6f), 200f);
            OfficeKit.PointLight(g, "Lamp_Warm", new Vector3(cx + 1.25f, h + 1.0f, A5_VZ1 + 2.55f), A5_Amber, 2.5f, 7f);

            // A note taped under the top rail, beside the gap, facing the landing.
            var noteAt = new Vector3(cx - gap - 0.32f, h + 0.92f, rz + 0.03f);
            OfficeKit.Sign(g, "Whoever keeps moving\nthe chair up here,\nplease stop.\n— G.F.", noteAt, OfficeKit.Facing(Vector3.forward), 0.018f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, A5M.Paper, new Vector2(0.26f, 0.15f));
            OfficeKit.Box(g, "Note_Tape", A5M.TileWhite, noteAt + new Vector3(-0.04f, 0.075f, 0.002f), noteAt + new Vector3(0.04f, 0.1f, 0.006f), false);

            A5_WallSign(g, "FACILITIES DEPARTMENT  ·  MEMO\n\nRe: furniture on Stair B landings\n\nAn orange shell chair has been returned\nto the Waiting Room four times this\nquarter. Each following morning it has\nbeen found here on Level 6, facing the\nstairwell. Landings are to be kept clear.\n\nF. Mortimer, Facilities",
                new Vector3(-27.2f, h + 1.55f, A5_NorthFace), Vector3.back, 0.02f, WorldTextStyle.DarkOnLight, A5M.Paper, new Vector2(0.56f, 0.42f));
            A5_Marker(g, "6", new Vector3(-24f, h + 2f, A5_NorthFace), Vector3.back);
        }

        // ------------------------------------------------------------------ the curve

        /// <summary>
        /// The one great curve (brutal_01: right angles meeting a single sweep): a full-height quarter
        /// cylinder of board-marked concrete closing the north-west corner, from the west wall at the
        /// stair ring to the north wall. Segmented boxes; the sun slot lands on it in the afternoon.
        /// </summary>
        static void A5_CurvedWall(Transform root)
        {
            var g = A5_Group(root, "CurvedWall");
            float r = A5_NorthFace - A5_RingZ1; // 5.6
            var centre = new Vector3(A5_RingX0 + r, 0f, A5_RingZ1);
            const int segments = 14;
            const float t = 0.8f;
            float y0 = OfficeLayout.A5.min.y, y1 = OfficeLayout.A5.max.y;
            float step = 90f / segments;
            float length = 2f * (r + t) * Mathf.Tan(step * 0.5f * Mathf.Deg2Rad) + 0.02f; // long enough to close the outer face too
            for (int i = 0; i < segments; i++)
            {
                float a = (90f + step * (i + 0.5f)) * Mathf.Deg2Rad;
                var radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                var c = centre + radial * (r + t * 0.5f) + Vector3.up * ((y0 + y1) * 0.5f);
                OfficeKit.BoxAt(g, $"Segment_{i:00}", A5M.Concrete, c, new Vector3(t, y1 - y0, length), Quaternion.LookRotation(tangent, Vector3.up));
            }
        }

        // ------------------------------------------------------------------ light from above

        /// <summary>
        /// The slot of sky: glazing in the roof slot (no shadow, so the sun comes through), a strong
        /// shadowed spot pooling cold light on the hatch, and two fake shafts: the sun's slant toward
        /// the curve, and a faint vertical one down the drop. The spot is the area's only shadowed light.
        /// </summary>
        static void A5_Sky(Transform root)
        {
            var g = A5_Group(root, "Sky");
            float top = OfficeLayout.A5.max.y;
            var s = A5_Slot;
            var glass = OfficeKit.Box(g, "Skylight_Glass", A5M.Glass, new Vector3(s.x, top - 0.3f, s.y), new Vector3(s.z, top - 0.25f, s.w));
            glass.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            var c = new Vector3((s.x + s.z) * 0.5f, top - 0.6f, (s.y + s.w) * 0.5f);
            OfficeKit.SpotLight(g, "Spot_Shaft", c, Quaternion.LookRotation(Vector3.down, Vector3.forward), new Color(0.9f, 0.94f, 1f), 900f, 50f, 24f, true);

            var sunDir = Quaternion.Euler(62f, -35f, 0f) * Vector3.forward; // matches the scene's sun
            OfficeKit.LightShaft(g, "Shaft_Sun", new Vector3(c.x, top - 0.5f, c.z), Quaternion.FromToRotation(Vector3.up, -sunDir),
                new Vector3(s.z - s.x, 11f, s.w - s.y), new Color(0.95f, 0.93f, 0.85f, 0.14f));
            OfficeKit.LightShaft(g, "Shaft_Down", c, Quaternion.identity, new Vector3(2.6f, top - OfficeLayout.A5.min.y - 1f, 2.6f), new Color(0.8f, 0.86f, 0.95f, 0.05f));
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>An empty child to group a part of the area in the hierarchy.</summary>
        static Transform A5_Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>A flat stair-ring landing between two XZ corners with its top at <paramref name="top"/>.</summary>
        static void A5_Flat(Transform g, string name, float x0, float z0, float x1, float z1, float top) =>
            OfficeKit.Box(g, name, A5M.ConcreteFloor, new Vector3(x0, top - A5_SlabT, z0), new Vector3(x1, top, z1));

        /// <summary>A steel railing (posts, top rail, invisible blocker) at the standard height.</summary>
        static void A5_Rail(Transform g, string name, Vector3 a, Vector3 b) => OfficeKit.Railing(g, name, a, b, A5_RailH, false);

        /// <summary>
        /// An open concrete flight (brutal_01): separate tread blocks over an inclined soffit slab,
        /// so the underside reads as a cantilevered stair rather than a solid mass. Starts at the
        /// bottom-front-centre <paramref name="start"/> and climbs along an axis-aligned
        /// <paramref name="dir"/>; the last tread is flush with the upper landing (same convention as
        /// OfficeKit.Stairs). The soffit has no collider: the treads carry the player.
        /// </summary>
        static void A5_Flight(Transform parent, string name, Vector3 start, Vector3 dir, float width, int steps)
        {
            var g = A5_Group(parent, name);
            var right = Vector3.Cross(Vector3.up, dir).normalized;
            for (int i = 0; i < steps; i++)
            {
                float top = start.y + (i + 1) * A5_Rise;
                var a = start + dir * (i * A5_Tread) - right * (width * 0.5f);
                var b = start + dir * ((i + 1) * A5_Tread) + right * (width * 0.5f);
                a.y = top - A5_StepDepth;
                b.y = top;
                OfficeKit.Box(g, $"Tread_{i + 1:00}", A5M.ConcreteFloor, a, b);
            }

            float run = steps * A5_Tread, rise = steps * A5_Rise;
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            var rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-angle, 0f, 0f);
            // The soffit's top plane runs from the lower landing's underside to the upper landing's underside.
            var midTop = start + dir * (run * 0.5f) + Vector3.up * (rise * 0.5f - A5_SlabT);
            const float soffit = 0.25f;
            OfficeKit.BoxAt(g, "Soffit", A5M.ConcreteDark, midTop - rot * Vector3.up * (soffit * 0.5f),
                new Vector3(width, soffit, Mathf.Sqrt(run * run + rise * rise)), rot, false);
        }

        /// <summary>A small wall fitting with a dim point light: green-ish emergency light, or one warm bulb.</summary>
        static void A5_Fitting(Transform g, string name, Vector3 wallPoint, Vector3 normal, bool warm)
        {
            OfficeKit.BoxAt(g, name + "_Fitting", warm ? A5M.LightWarm : A5M.Crt, wallPoint + normal * 0.05f, new Vector3(0.32f, 0.12f, 0.1f),
                Quaternion.LookRotation(normal, Vector3.up), false);
            OfficeKit.PointLight(g, name, wallPoint + normal * 0.4f, warm ? A5_Amber : A5_Green, warm ? 2f : 1.4f, warm ? 7f : 6f);
        }

        /// <summary>
        /// Text on a wall face: <paramref name="facePoint"/> lies on the face, <paramref name="normal"/>
        /// points out of it. Offsets the sign just clear of the face (more with a backing panel).
        /// </summary>
        static void A5_WallSign(Transform g, string text, Vector3 facePoint, Vector3 normal, float letter, WorldTextStyle style,
            string backing = null, Vector2 backingSize = default)
        {
            float off = backing != null ? 0.012f : 0.005f;
            OfficeKit.Sign(g, text, facePoint + normal * off, OfficeKit.Facing(normal), letter, style, TextAnchor.MiddleCenter, backing, backingSize);
        }

        /// <summary>A big painted level numeral: the stair's design language (-1, 0, 2, 6).</summary>
        static void A5_Marker(Transform g, string text, Vector3 facePoint, Vector3 normal) =>
            A5_WallSign(g, text, facePoint, normal, 1.4f, WorldTextStyle.LightOnDark);

        /// <summary>
        /// The laminated evacuation plan on L2: a printed sheet with a little plan of the core and
        /// its void, and "this level" renumbered by hand twice (struck-out 3 and 5, then 2).
        /// </summary>
        static void A5_EvacPlan(Transform parent, Vector3 centre, Vector3 facing)
        {
            var g = A5_Group(parent, "EvacuationPlan");
            var rot = OfficeKit.Facing(facing);
            var right = Vector3.Cross(Vector3.up, -facing).normalized; // reading direction for a viewer facing the sheet
            Vector3 At(float r, float u, float o) => centre + right * r + Vector3.up * u + facing * o;

            OfficeKit.BoxAt(g, "Sheet", A5M.Paper, At(0f, 0f, 0.003f), new Vector3(0.62f, 0.46f, 0.006f), rot, false);
            OfficeKit.BoxAt(g, "Laminate", A5M.Glass, At(0f, 0f, 0.014f), new Vector3(0.64f, 0.48f, 0.004f), rot, false);

            OfficeKit.Sign(g, "HALE & ORMSBY   RECORDS & CONTINUITY", At(0f, 0.195f, 0.008f), rot, 0.013f);
            OfficeKit.Sign(g, "EVACUATION PLAN: STAIR B", At(0f, 0.163f, 0.008f), rot, 0.022f);

            // The core and its void, drawn as two squares, with the hatch as a small dot.
            void Square(float r, float u, float half, float line)
            {
                OfficeKit.BoxAt(g, "Line", A5M.MetalDark, At(r, u + half, 0.007f), new Vector3(half * 2f + line, line, 0.002f), rot, false);
                OfficeKit.BoxAt(g, "Line", A5M.MetalDark, At(r, u - half, 0.007f), new Vector3(half * 2f + line, line, 0.002f), rot, false);
                OfficeKit.BoxAt(g, "Line", A5M.MetalDark, At(r - half, u, 0.007f), new Vector3(line, half * 2f, 0.002f), rot, false);
                OfficeKit.BoxAt(g, "Line", A5M.MetalDark, At(r + half, u, 0.007f), new Vector3(line, half * 2f, 0.002f), rot, false);
            }
            Square(-0.12f, -0.01f, 0.11f, 0.006f);
            Square(-0.12f, -0.01f, 0.05f, 0.003f);
            OfficeKit.BoxAt(g, "Dot", A5M.MetalDark, At(-0.12f, -0.01f, 0.007f), new Vector3(0.016f, 0.016f, 0.002f), rot, false);

            OfficeKit.Sign(g, "THIS LEVEL", At(0.15f, 0.07f, 0.008f), rot, 0.018f);
            float[] xs = { 0.08f, 0.15f, 0.22f };
            string[] nums = { "3", "5", "2" };
            for (int i = 0; i < nums.Length; i++)
            {
                OfficeKit.Sign(g, nums[i], At(xs[i], 0.015f, 0.008f), rot, 0.04f);
                if (i < 2) // struck out by hand
                    OfficeKit.BoxAt(g, "Strike", A5M.MetalDark, At(xs[i], 0.015f, 0.009f), new Vector3(0.05f, 0.004f, 0.002f), rot * Quaternion.Euler(0f, 0f, 25f), false);
            }
            OfficeKit.Sign(g, "ASSEMBLY POINT: FORECOURT", At(0f, -0.175f, 0.008f), rot, 0.016f);
            OfficeKit.Sign(g, "Revised 1/78", At(0.22f, -0.205f, 0.008f), rot, 0.011f);
        }
    }
}
