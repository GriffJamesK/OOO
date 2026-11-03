using Kit.Gameplay.Signage;
using UnityEngine;
using OKit = Kit.Editor.OfficeKit;
using OMat = Kit.Editor.OfficeKit.M;
using OProp = Kit.Editor.OfficeKit.PropIds;

namespace Kit.Editor
{
    /// <summary>
    /// A6 Executive Wing (L6, floor y 24): reachable only by the launch onto the atrium's
    /// boardroom pod. Warm wood, oxblood carpet and a dropped ceiling with very few lit panels, so
    /// the wing reads as pools of light in darkness (control_01, desaturated). Plan, west to east:
    /// a slate vestibule from the A6_Bridge socket; a lounge (planter, leather sofa, corkboard,
    /// vending machines, a clock); a cold dark hall to the north where a lit glass meeting room
    /// faces rows of empty folding chairs (inside_01); an office corridor to the south (brass doors
    /// 601-608, the director's door 600 at its end with an OUT OF OFFICE tag); a lit spine east to
    /// the boardroom, whose full-height east window looks down over the pale plain to the lit
    /// door 000 (K5). The whiteboard notes there are the game's strongest hint.
    /// </summary>
    public static partial class OfficeBuilder
    {
        // Heights (world y).
        const float A6_Floor = 24f;
        const float A6_Ceil = 28f;          // underside of the dropped ceiling
        const float A6_WallH = A6_Ceil - A6_Floor;
        const float A6_VestCeil = 27f;      // the bridge vestibule's lower soffit (socket height)

        // Light tints (desaturated warm, cool fluorescent).
        static readonly Color A6_Warm = new Color(1f, 0.82f, 0.62f);
        static readonly Color A6_Cool = new Color(0.86f, 0.91f, 1f);

        // The six south offices (601-606) share the corridor wall at z -14 in equal bays.
        const float A6_BayX0 = 26f;
        const float A6_BayW = 23f / 6f;

        static readonly string[] A6_OfficeNames =
        {
            "AGNES PELL  ·  RECORDS",
            "TOMAS VARGA  ·  CONTINUITY",
            "RUTH ABERNATHY  ·  ACCOUNTS",
            "HUGO LINDQVIST  ·  PERSONNEL",
            "MAE OKONJO  ·  CONTINUITY",
            "WALTER BRISK  ·  ASSISTANT DIRECTOR",
            "ARTHUR PENHALIGON  ·  DEPUTY DIRECTOR",
            "JUNE HALLORAN  ·  SECRETARY TO THE DIRECTOR",
        };

        /// <summary>Builds the whole Executive Wing under <paramref name="root"/> (world space).</summary>
        static partial void BuildArea_A6(Transform root)
        {
            OKit.Zone(root, "A6", OfficeLayout.A6);
            A6_Shell(root);
            A6_Vestibule(root);
            A6_Lounge(root);
            A6_Hall(root);
            A6_Corridor(root);
            A6_Spine(root);
            A6_Boardroom(root);
            A6_Director(root);
            A6_Archive(root);

            // Interior fog: dark and short, a touch warmer than the global office fog (Twin Peaks wood).
            OKit.FogZone(root, "A6_Interior", OfficeLayout.A6, new Color(0.13f, 0.115f, 0.11f), 6f, 55f, 2f, 10f);
        }

        // ------------------------------------------------------------------ shell

        /// <summary>
        /// Floor slab at y 24 (carpet, with slate in the vestibule and polished concrete in the dark
        /// hall), the dropped ceiling and roof, the exterior walls, and every interior partition.
        /// Walls are listed in one place so openings line up with the rooms that use them.
        /// </summary>
        static void A6_Shell(Transform root)
        {
            var shell = new GameObject("A6_Shell").transform;
            shell.SetParent(root, false);

            var vestHole = new Vector4(20.25f, -2.15f, 26f, 2.15f);
            var hallHole = new Vector4(26f, 2.15f, 49f, 19.7f);
            OKit.Slab(shell, "A6_Floor_Carpet", OMat.CarpetOxblood, new Vector3(20.25f, 0f, -20f), new Vector3(60f, 0f, 20f), A6_Floor, 0.5f, vestHole, hallHole);
            OKit.Slab(shell, "A6_Floor_Vestibule", OMat.Slate, new Vector3(20.25f, 0f, -2.15f), new Vector3(26f, 0f, 2.15f), A6_Floor, 0.5f);
            OKit.Slab(shell, "A6_Floor_Hall", OMat.ConcreteFloor, new Vector3(26f, 0f, 2.15f), new Vector3(49f, 0f, 19.7f), A6_Floor, 0.5f);

            // Dropped acoustic ceiling (tiles), raw dark soffit over the hall, and a concrete roof.
            OKit.Slab(shell, "A6_Ceiling_Tiles", OMat.CeilingTile, new Vector3(20.25f, 0f, -20f), new Vector3(60f, 0f, 20f), A6_Ceil + 0.3f, 0.3f, hallHole);
            OKit.Slab(shell, "A6_Ceiling_Hall", OMat.ConcreteDark, new Vector3(26f, 0f, 2.15f), new Vector3(49f, 0f, 19.7f), A6_Ceil + 0.3f, 0.3f);
            OKit.Slab(shell, "A6_Roof", OMat.ConcreteDark, new Vector3(20.25f, 0f, -20f), new Vector3(60f, 0f, 20f), 29f, 0.7f);
            OKit.Box(shell, "A6_Vestibule_Soffit", OMat.CeilingTile, new Vector3(20.25f, A6_VestCeil, -2.15f), new Vector3(26f, A6_Ceil, 2.15f));

            // Exterior: a lining against the atrium's east wall (keeps the wing sealed whatever A2
            // builds), the south and north walls, and the east wall with the boardroom window.
            var bridge = new OKit.Opening(17.7f, 4f, 0f, A6_VestCeil - A6_Floor);
            OKit.Wall(shell, "A6_Wall_WestLining", OMat.ConcreteDark, new Vector3(20.35f, 0f, -19.7f), new Vector3(20.35f, 0f, 19.7f), A6_Floor, A6_WallH, 0.2f, bridge);
            OKit.Wall(shell, "A6_Wall_South", OMat.WoodVeneer, new Vector3(20.25f, 0f, -19.85f), new Vector3(60f, 0f, -19.85f), A6_Floor, A6_WallH, 0.3f);
            OKit.Wall(shell, "A6_Wall_North", OMat.Concrete, new Vector3(20.25f, 0f, 19.85f), new Vector3(60f, 0f, 19.85f), A6_Floor, A6_WallH, 0.3f);
            OKit.Wall(shell, "A6_Wall_East", OMat.Concrete, new Vector3(59.85f, 0f, -19.7f), new Vector3(59.85f, 0f, 19.7f), A6_Floor, A6_WallH, 0.3f,
                new OKit.Opening(10.7f, 18f, 0f, A6_WallH));
            // A dark edge beam under the east face, so the cantilever reads as one mass from outside.
            OKit.Box(shell, "A6_EdgeBeam_East", OMat.ConcreteDark, new Vector3(59.3f, 23.1f, -20f), new Vector3(60f, A6_Floor - 0.5f, 20f));

            // Vestibule sides and the west wall of the wing proper.
            OKit.Wall(shell, "A6_Wall_VestN", OMat.WoodVeneer, new Vector3(20.45f, 0f, 2.15f), new Vector3(26f, 0f, 2.15f), A6_Floor, A6_WallH, 0.2f);
            OKit.Wall(shell, "A6_Wall_VestS", OMat.WoodVeneer, new Vector3(20.45f, 0f, -2.15f), new Vector3(26f, 0f, -2.15f), A6_Floor, A6_WallH, 0.2f);
            OKit.Wall(shell, "A6_Wall_X26_Lounge", OMat.WoodVeneer, new Vector3(26f, 0f, -19.7f), new Vector3(26f, 0f, 2.25f), A6_Floor, A6_WallH, 0.2f,
                new OKit.Opening(17.7f, 4f, 0f, A6_VestCeil - A6_Floor));
            OKit.Wall(shell, "A6_Wall_X26_Hall", OMat.Concrete, new Vector3(26f, 0f, 2.25f), new Vector3(26f, 0f, 19.7f), A6_Floor, A6_WallH, 0.2f);

            // Lounge / spine north wall (onto the hall), with the wide opening into the hall.
            OKit.Wall(shell, "A6_Wall_Z2", OMat.WoodVeneer, new Vector3(26.1f, 0f, 2.15f), new Vector3(48.9f, 0f, 2.15f), A6_Floor, A6_WallH, 0.2f,
                new OKit.Opening(2.9f, 6f, 0f, 3.2f));
            // Spine south wall (offices 607-608 behind it), lounge east wall, office partition.
            OKit.Wall(shell, "A6_Wall_Zm2", OMat.WoodVeneer, new Vector3(38f, 0f, -2.15f), new Vector3(48.9f, 0f, -2.15f), A6_Floor, A6_WallH, 0.2f);
            OKit.Wall(shell, "A6_Wall_X38", OMat.WoodVeneer, new Vector3(38f, 0f, -9.9f), new Vector3(38f, 0f, -2.05f), A6_Floor, A6_WallH, 0.2f);
            OKit.Wall(shell, "A6_Wall_X43", OMat.WoodVeneer, new Vector3(43.5f, 0f, -9.9f), new Vector3(43.5f, 0f, -2.25f), A6_Floor, A6_WallH, 0.2f);

            // Corridor north wall: the lounge opening, doors 607 and 608, and their glass sidelights.
            OKit.Wall(shell, "A6_Wall_Zm10", OMat.WoodVeneer, new Vector3(26.1f, 0f, -10f), new Vector3(48.9f, 0f, -10f), A6_Floor, A6_WallH, 0.2f,
                new OKit.Opening(0.9f, 3f, 0f, 3f),
                OKit.Opening.Door(40.2f - 26.1f), new OKit.Opening(41.8f - 26.1f, 1.2f, 0.9f, 1.3f),
                OKit.Opening.Door(45.7f - 26.1f), new OKit.Opening(47.3f - 26.1f, 1.2f, 0.9f, 1.3f));

            // Corridor south wall: six office fronts, each a door and a glass sidelight.
            var fronts = new OKit.Opening[12];
            for (int i = 0; i < 6; i++)
            {
                float x0 = A6_BayX0 + i * A6_BayW;
                fronts[i * 2] = OKit.Opening.Door(x0 + 0.7f - 26.1f);
                fronts[i * 2 + 1] = new OKit.Opening(x0 + 2.2f - 26.1f, 1.2f, 0.9f, 1.3f);
            }
            OKit.Wall(shell, "A6_Wall_Zm14", OMat.WoodVeneer, new Vector3(26.1f, 0f, -14f), new Vector3(48.9f, 0f, -14f), A6_Floor, A6_WallH, 0.2f, fronts);
            for (int i = 1; i < 6; i++)
            {
                float x = A6_BayX0 + i * A6_BayW;
                OKit.Wall(shell, $"A6_Wall_Office{600 + i}_{601 + i}", OMat.WoodVeneer, new Vector3(x, 0f, -19.7f), new Vector3(x, 0f, -14.1f), A6_Floor, A6_WallH, 0.2f);
            }

            // x 49: director's door 600, the boardroom double doors, the archive door 609.
            OKit.Wall(shell, "A6_Wall_X49", OMat.WoodVeneer, new Vector3(49f, 0f, -19.7f), new Vector3(49f, 0f, 19.7f), A6_Floor, A6_WallH, 0.2f,
                new OKit.Opening(7.1f, OfficeLayout.ExecutiveDoor.x, 0f, OfficeLayout.ExecutiveDoor.y),
                new OKit.Opening(18.5f, 2.4f, 0f, 2.7f),
                OKit.Opening.Door(30.2f));
            OKit.Wall(shell, "A6_Wall_Z9", OMat.WoodVeneer, new Vector3(49.1f, 0f, 9f), new Vector3(59.7f, 0f, 9f), A6_Floor, A6_WallH, 0.2f);
            OKit.Wall(shell, "A6_Wall_Zm9", OMat.WoodVeneer, new Vector3(49.1f, 0f, -9f), new Vector3(59.7f, 0f, -9f), A6_Floor, A6_WallH, 0.2f);

            // Glass sidelights in the office fronts (solid), so lit offices show through.
            for (int i = 0; i < 6; i++)
            {
                float x0 = A6_BayX0 + i * A6_BayW;
                A6_GlassPane(shell, $"A6_Sidelight_{601 + i}", new Vector3(x0 + 2.2f, A6_Floor + 0.9f, -14.02f), new Vector3(x0 + 3.4f, A6_Floor + 2.2f, -13.98f));
            }
            A6_GlassPane(shell, "A6_Sidelight_607", new Vector3(41.8f, A6_Floor + 0.9f, -10.02f), new Vector3(43f, A6_Floor + 2.2f, -9.98f));
            A6_GlassPane(shell, "A6_Sidelight_608", new Vector3(47.3f, A6_Floor + 0.9f, -10.02f), new Vector3(48.5f, A6_Floor + 2.2f, -9.98f));
        }

        /// <summary>A solid glass pane between two corners, with a dark wood frame line top and bottom.</summary>
        static void A6_GlassPane(Transform parent, string name, Vector3 min, Vector3 max)
        {
            OKit.Box(parent, name, OMat.Glass, min, max);
            // Sill and head straddle the wall's own sill/lintel faces (no coplanar faces to flicker).
            bool alongX = (max.x - min.x) > (max.z - min.z);
            var pad = alongX ? new Vector3(0f, 0f, 0.12f) : new Vector3(0.12f, 0f, 0f);
            OKit.Box(parent, name + "_Sill", OMat.WoodDark, new Vector3(min.x, min.y - 0.02f, min.z) - pad, new Vector3(max.x, min.y + 0.02f, max.z) + pad, false);
            OKit.Box(parent, name + "_Head", OMat.WoodDark, new Vector3(min.x, max.y - 0.02f, min.z) - pad, new Vector3(max.x, max.y + 0.02f, max.z) + pad, false);
        }

        // ------------------------------------------------------------------ vestibule

        /// <summary>
        /// The short bridge-like vestibule from the A6_Bridge socket (x 20, z -2..2): slate floor
        /// edged in brass, a low soffit with one warm downlight, and "EXECUTIVE" in brass letters
        /// over the far opening, readable from the pod as the player lands.
        /// </summary>
        static void A6_Vestibule(Transform root)
        {
            var t = new GameObject("A6_Vestibule").transform;
            t.SetParent(root, false);

            OKit.Box(t, "A6_Vest_BrassEdgeN", OMat.Brass, new Vector3(20.45f, A6_Floor, 1.9f), new Vector3(26f, A6_Floor + 0.006f, 1.95f), false);
            OKit.Box(t, "A6_Vest_BrassEdgeS", OMat.Brass, new Vector3(20.45f, A6_Floor, -1.95f), new Vector3(26f, A6_Floor + 0.006f, -1.9f), false);
            OKit.CeilingLight(t, "A6_Vest_Light", new Vector3(23.2f, A6_VestCeil - 0.03f, 0f), new Vector2(0.6f, 0.6f), A6_Warm, 4f, 5f, false, false, OMat.LightWarm);

            OKit.Prop(t, OProp.CoatRack, new Vector3(25.4f, A6_Floor, 1.55f));
            OKit.Prop(t, OProp.TicketDispenser, new Vector3(25.5f, A6_Floor, -1.6f), -90f);
            // Brass lettering on the north wall, seen as the player steps in off the pod.
            OKit.Sign(t, "HALE & ORMSBY\nEXECUTIVE OFFICES", new Vector3(22.6f, 25.75f, 2.045f), OKit.Facing(Vector3.back), 0.09f, WorldTextStyle.Brass);
        }

        // ------------------------------------------------------------------ lounge

        /// <summary>
        /// control_01's room: one lit panel pooling over a leather sofa, a long concrete planter of
        /// philodendrons, a corkboard of notices, two vending machines, a coffee trolley, a wall
        /// clock and a framed photograph. Openings lead north into the dark hall, south into the
        /// office corridor and east down the lit spine to the boardroom.
        /// </summary>
        static void A6_Lounge(Transform root)
        {
            var t = new GameObject("A6_Lounge").transform;
            t.SetParent(root, false);

            OKit.CeilingLight(t, "A6_Lounge_Panel", new Vector3(34f, A6_Ceil - 0.03f, -8.2f), new Vector2(1.2f, 0.6f), A6_Cool, 12f, 7f, false, true);

            OKit.Prop(t, OProp.LeatherSofa, new Vector3(34f, A6_Floor, -9.45f));
            OKit.Prop(t, OProp.SideTable, new Vector3(35.6f, A6_Floor, -9.5f));
            OKit.Prop(t, OProp.Planter, new Vector3(32.5f, A6_Floor, -5.2f));
            OKit.Prop(t, OProp.Planter, new Vector3(34.9f, A6_Floor, -5.2f));
            OKit.Prop(t, OProp.VendingMachine, new Vector3(37.45f, A6_Floor, -8.6f), -90f);
            OKit.Prop(t, OProp.VendingMachine, new Vector3(37.45f, A6_Floor, -7.6f), -90f);
            OKit.Prop(t, OProp.CoffeeTrolley, new Vector3(37.2f, A6_Floor, -5.4f), -90f);
            OKit.Prop(t, OProp.Bin, new Vector3(36.4f, A6_Floor, -9.6f));
            OKit.Prop(t, OProp.PottedPlant, new Vector3(26.6f, A6_Floor, 1.5f));
            OKit.Prop(t, OProp.PottedPlant, new Vector3(37.4f, A6_Floor, 1.5f));
            OKit.Prop(t, OProp.WallClock, new Vector3(34f, 26.5f, -9.87f));
            OKit.Prop(t, OProp.Flag, new Vector3(26.45f, A6_Floor, -7.7f));

            // Corkboard on the west wall, facing east, with pinned notices.
            OKit.Prop(t, OProp.Corkboard, new Vector3(26.115f, 25f, -6.5f), 90f);
            var east = OKit.Facing(Vector3.right);
            OKit.Sign(t, "FACILITIES\nRoom numbers remain\nthe property of\nthe building.", new Vector3(26.145f, 25.62f, -6.78f), east, 0.018f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.27f, 0.2f));
            OKit.Sign(t, "Retirement tea\nW. Brisk\nFri. 4:30, lounge\n(postponed)", new Vector3(26.145f, 25.3f, -6.2f), east, 0.017f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.22f, 0.19f));
            OKit.Sign(t, "FOUND: reading glasses,\nboardroom.\nSee J. Halloran.", new Vector3(26.145f, 25.7f, -6.22f), east, 0.015f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.24f, 0.13f));
            OKit.Sign(t, "TAKE A NUMBER\nThe 4th floor dispenser\nis out of tickets.", new Vector3(26.145f, 25.25f, -6.8f), east, 0.015f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.25f, 0.14f));

            // Framed photograph and its caption, beside the vestibule opening.
            OKit.Box(t, "A6_Photo_Frame", OMat.WoodDark, new Vector3(26.1f, 25.65f, -4.0f), new Vector3(26.14f, 26.15f, -3.3f), false);
            OKit.Box(t, "A6_Photo_Print", OMat.Leather, new Vector3(26.14f, 25.7f, -3.95f), new Vector3(26.145f, 26.1f, -3.35f), false);
            OKit.Sign(t, "CONTINUITY DEPARTMENT  ·  FOUNDERS' DAY, 1975", new Vector3(26.15f, 25.55f, -3.65f), east, 0.016f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.62f, 0.05f));

            // Wayfinding over the corridor opening.
            OKit.Sign(t, "OFFICES  600 — 608", new Vector3(28.5f, 27.35f, -9.88f), OKit.Facing(Vector3.forward), 0.09f,
                WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter, OMat.MetalDark, new Vector2(1.5f, 0.24f));
        }

        // ------------------------------------------------------------------ dark hall

        /// <summary>
        /// inside_01 staged: a cold, dark hall with a raw soffit and polished concrete floor. At its
        /// centre a lit glass meeting room (desk, lamp, drip stands, one chair facing the desk);
        /// outside, rows of empty folding chairs face it in shadow. A second, smaller glass room
        /// glows warmly in the far corner.
        /// </summary>
        static void A6_Hall(Transform root)
        {
            var t = new GameObject("A6_Hall").transform;
            t.SetParent(root, false);

            // Meeting Room C: x 33..41, z 10..16, glass 3 m high under a dark timber roof.
            A6_GlassRoom(t, "A6_RoomC", new Vector3(33f, 0f, 10f), new Vector3(41f, 0f, 16f), 'E', 14.2f);
            OKit.Box(t, "A6_RoomC_Floor", OMat.ConcreteLight, new Vector3(33.05f, A6_Floor, 10.05f), new Vector3(40.95f, A6_Floor + 0.02f, 15.95f));
            OKit.CeilingLight(t, "A6_RoomC_Light", new Vector3(37f, 26.97f, 13f), new Vector2(1.4f, 0.6f), A6_Cool, 10f, 7f, false, true);
            OKit.Prop(t, OProp.Desk, new Vector3(37f, A6_Floor, 13.6f), 180f);
            OKit.Prop(t, OProp.SwivelChair, new Vector3(37f, A6_Floor, 14.5f), 180f);
            OKit.Prop(t, OProp.DeskLamp, new Vector3(36.4f, A6_Floor + 0.74f, 13.7f), 160f);
            OKit.Prop(t, OProp.FoldingChair, new Vector3(37.1f, A6_Floor, 12.25f), 4f);
            OKit.Prop(t, OProp.FilingCabinet, new Vector3(34f, A6_Floor, 15.4f), 180f);
            A6_DripStand(t, "A6_DripStand_1", new Vector3(34.3f, A6_Floor, 11.2f));
            A6_DripStand(t, "A6_DripStand_2", new Vector3(35.2f, A6_Floor, 10.8f));
            OKit.Sign(t, "MEETING ROOM C", new Vector3(37f, 26.6f, 9.97f), OKit.Facing(Vector3.back), 0.06f);
            OKit.Sign(t, "MINUTES\n4:47 p.m.\nNo quorum.\nAdjourned.", new Vector3(37.3f, A6_Floor + 0.758f, 13.5f), Quaternion.LookRotation(Vector3.up, Vector3.back), 0.016f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.21f, 0.28f));

            // Empty folding chairs facing the lit room, a few out of line.
            float[] rowZ = { 5.6f, 6.8f, 8.0f };
            for (int r = 0; r < rowZ.Length; r++)
                for (int c = 0; c < 7; c++)
                {
                    if ((r == 1 && c == 5) || (r == 2 && c == 1)) continue; // gaps where chairs were moved
                    float jitter = ((r * 7 + c) * 37 % 11 - 5) * 0.8f;
                    OKit.Prop(t, OProp.FoldingChair, new Vector3(34f + c, A6_Floor, rowZ[r] + ((c + r) % 3) * 0.04f), jitter);
                }
            OKit.Prop(t, OProp.FoldingChair, new Vector3(31.2f, A6_Floor, 8.9f), 38f);   // the moved ones, off to the side
            OKit.Prop(t, OProp.FoldingChair, new Vector3(42.4f, A6_Floor, 7.1f), -70f);
            OKit.Prop(t, OProp.FoldingChair, new Vector3(30.6f, A6_Floor, 12.6f), 90f);  // a short row on the west side
            OKit.Prop(t, OProp.FoldingChair, new Vector3(30.6f, A6_Floor, 13.6f), 92f);
            OKit.Prop(t, OProp.FoldingChair, new Vector3(30.6f, A6_Floor, 14.6f), 87f);

            // Room D: smaller, warm, in the north-east corner of the hall.
            A6_GlassRoom(t, "A6_RoomD", new Vector3(43.5f, 0f, 14f), new Vector3(47f, 0f, 18.5f), 'S', 45.25f);
            OKit.Prop(t, OProp.RoundTable, new Vector3(45.3f, A6_Floor, 16.4f));
            OKit.Prop(t, OProp.ShellChair, new Vector3(44.6f, A6_Floor, 16.9f), 120f);
            OKit.Prop(t, OProp.ShellChair, new Vector3(46.1f, A6_Floor, 15.6f), -60f);
            OKit.Prop(t, OProp.Telephone, new Vector3(45.5f, A6_Floor + 0.74f, 16.5f), 20f);
            OKit.Prop(t, OProp.DeskLamp, new Vector3(45.0f, A6_Floor + 0.74f, 16.2f), 200f);
            OKit.PointLight(t, "A6_RoomD_Lamp", new Vector3(45.0f, 25.15f, 16.2f), A6_Warm, 1.6f, 3.5f);
            OKit.Sign(t, "ROOM D", new Vector3(44.2f, 26.6f, 13.97f), OKit.Facing(Vector3.back), 0.05f);
            OKit.Prop(t, OProp.FoldingChair, new Vector3(44.3f, A6_Floor, 12.3f), 2f);
            OKit.Prop(t, OProp.FoldingChair, new Vector3(46.4f, A6_Floor, 12.2f), -6f);

            // A faint cool spill at the far west so the hall's edges aren't pure black.
            OKit.PointLight(t, "A6_Hall_Spill", new Vector3(28f, 27.4f, 17.5f), A6_Cool, 1.2f, 9f);
            OKit.Prop(t, OProp.PottedPlant, new Vector3(26.7f, A6_Floor, 19.0f));
            OKit.Prop(t, OProp.Bin, new Vector3(48.3f, A6_Floor, 3.0f));
        }

        /// <summary>
        /// A free-standing glass room (x/z rectangle from <paramref name="min"/> to <paramref name="max"/>):
        /// solid glass walls 3 m high, dark steel mullions and a dark timber roof, with a 1 m doorway
        /// on one side (<paramref name="doorSide"/> 'N', 'S', 'E' or 'W', centred at <paramref name="doorAt"/>
        /// along that side).
        /// </summary>
        static void A6_GlassRoom(Transform parent, string name, Vector3 min, Vector3 max, char doorSide, float doorAt)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            const float h = 3f, g = 0.03f;
            float y0 = A6_Floor, y1 = A6_Floor + h;

            void Side(string side, Vector3 a, Vector3 b, bool hasDoor)
            {
                bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
                float s0 = alongX ? a.x : a.z, s1 = alongX ? b.x : b.z;
                Vector3 P0(float s, float y) => alongX ? new Vector3(s, y, a.z - g) : new Vector3(a.x - g, y, s);
                Vector3 P1(float s, float y) => alongX ? new Vector3(s, y, a.z + g) : new Vector3(a.x + g, y, s);
                if (hasDoor)
                {
                    float d0 = doorAt - 0.5f, d1 = doorAt + 0.5f;
                    OKit.Box(t, $"{side}_GlassA", OMat.Glass, P0(s0, y0), P1(d0, y1));
                    OKit.Box(t, $"{side}_GlassB", OMat.Glass, P0(d1, y0), P1(s1, y1));
                    OKit.Box(t, $"{side}_DoorHead", OMat.MetalDark, P0(d0, y0 + 2.2f), P1(d1, y0 + 2.26f));
                    OKit.Box(t, $"{side}_GlassAbove", OMat.Glass, P0(d0, y0 + 2.26f), P1(d1, y1));
                    foreach (float d in new[] { d0, d1 })
                        OKit.Box(t, $"{side}_DoorPost_{d:0.0}", OMat.MetalDark, P0(d - 0.04f, y0), P1(d + 0.04f, y1));
                }
                else
                {
                    OKit.Box(t, $"{side}_Glass", OMat.Glass, P0(s0, y0), P1(s1, y1));
                }
                int n = Mathf.Max(1, Mathf.RoundToInt((s1 - s0) / 2f));
                for (int i = 1; i < n; i++)
                {
                    float s = s0 + (s1 - s0) * i / n;
                    if (hasDoor && Mathf.Abs(s - doorAt) < 0.7f) continue;
                    OKit.Box(t, $"{side}_Mullion_{i}", OMat.MetalDark, P0(s - 0.04f, y0), P1(s + 0.04f, y1), false);
                }
            }

            Side("S", new Vector3(min.x, 0f, min.z), new Vector3(max.x, 0f, min.z), doorSide == 'S');
            Side("N", new Vector3(min.x, 0f, max.z), new Vector3(max.x, 0f, max.z), doorSide == 'N');
            Side("W", new Vector3(min.x, 0f, min.z), new Vector3(min.x, 0f, max.z), doorSide == 'W');
            Side("E", new Vector3(max.x, 0f, min.z), new Vector3(max.x, 0f, max.z), doorSide == 'E');
            foreach (var c in new[] { new Vector3(min.x, 0f, min.z), new Vector3(max.x, 0f, min.z), new Vector3(min.x, 0f, max.z), new Vector3(max.x, 0f, max.z) })
                OKit.Box(t, $"Corner_{c.x:0}_{c.z:0}", OMat.MetalDark, new Vector3(c.x - 0.06f, y0, c.z - 0.06f), new Vector3(c.x + 0.06f, y1, c.z + 0.06f));
            OKit.Box(t, "Roof", OMat.WoodDark, new Vector3(min.x - 0.15f, y1, min.z - 0.15f), new Vector3(max.x + 0.15f, y1 + 0.25f, max.z + 0.15f));
        }

        /// <summary>A hospital drip stand (pole, splayed foot, hook bar, one bag): thin, no collider.</summary>
        static void A6_DripStand(Transform parent, string name, Vector3 foot)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            OKit.BoxAt(t, "Pole", OMat.Metal, foot + new Vector3(0f, 0.95f, 0f), new Vector3(0.03f, 1.9f, 0.03f), Quaternion.identity, false);
            OKit.BoxAt(t, "FootA", OMat.MetalDark, foot + new Vector3(0f, 0.03f, 0f), new Vector3(0.55f, 0.03f, 0.05f), Quaternion.Euler(0f, 30f, 0f), false);
            OKit.BoxAt(t, "FootB", OMat.MetalDark, foot + new Vector3(0f, 0.03f, 0f), new Vector3(0.55f, 0.03f, 0.05f), Quaternion.Euler(0f, 120f, 0f), false);
            OKit.BoxAt(t, "HookBar", OMat.Metal, foot + new Vector3(0f, 1.88f, 0f), new Vector3(0.32f, 0.02f, 0.02f), Quaternion.identity, false);
            OKit.BoxAt(t, "Bag", OMat.Glass, foot + new Vector3(0.13f, 1.72f, 0f), new Vector3(0.1f, 0.2f, 0.04f), Quaternion.identity, false);
        }

        // ------------------------------------------------------------------ office corridor

        /// <summary>
        /// The office corridor (x 26..49, z -14..-10): wood-panelled fronts with brass doors 601-606
        /// to the south and 607-608 to the north, nameplates over each sidelight, a few offices lit
        /// by a desk lamp so they show through the glass, and at the far east end the director's
        /// door 600 under its own spot, with a nameplate and an OUT OF OFFICE tag.
        /// </summary>
        static void A6_Corridor(Transform root)
        {
            var t = new GameObject("A6_Corridor").transform;
            t.SetParent(root, false);

            OKit.CeilingLight(t, "A6_Corridor_Panel", new Vector3(31.5f, A6_Ceil - 0.03f, -12f), new Vector2(1.2f, 0.6f), A6_Cool, 5f, 6f, true);
            OKit.Prop(t, OProp.WaterCooler, new Vector3(26.5f, A6_Floor, -12.9f), 90f);
            OKit.Prop(t, OProp.PottedPlant, new Vector3(26.5f, A6_Floor, -11.0f));

            var north = OKit.Facing(Vector3.forward);
            var south = OKit.Facing(Vector3.back);
            for (int i = 0; i < 6; i++)
            {
                float x0 = A6_BayX0 + i * A6_BayW;
                int number = 601 + i;
                OKit.Threshold(t, $"A6_Door_{number}", new Vector3(x0 + 1.2f, A6_Floor + OfficeLayout.Door.y * 0.5f, -14f), north, OfficeLayout.Door, number.ToString());
                OKit.Sign(t, A6_OfficeNames[i], new Vector3(x0 + 2.8f, 26.5f, -13.885f), north, 0.035f, WorldTextStyle.Brass, TextAnchor.MiddleCenter,
                    OMat.WoodDark, new Vector2(1.15f, 0.1f));
                bool lit = number == 602 || number == 605;
                A6_SmallOffice(t, number, new Vector3(x0 + A6_BayW * 0.5f, A6_Floor, -17.9f), 0f, lit, number == 604);
            }

            // 607 and 608, north side of the corridor (front faces south, into the corridor).
            OKit.Threshold(t, "A6_Door_607", new Vector3(40.7f, A6_Floor + OfficeLayout.Door.y * 0.5f, -10f), south, OfficeLayout.Door, "607");
            OKit.Threshold(t, "A6_Door_608", new Vector3(46.2f, A6_Floor + OfficeLayout.Door.y * 0.5f, -10f), south, OfficeLayout.Door, "608");
            OKit.Sign(t, A6_OfficeNames[6], new Vector3(42.4f, 26.5f, -10.115f), south, 0.032f, WorldTextStyle.Brass, TextAnchor.MiddleCenter,
                OMat.WoodDark, new Vector2(1.3f, 0.1f));
            OKit.Sign(t, A6_OfficeNames[7], new Vector3(47.75f, 26.5f, -10.115f), south, 0.026f, WorldTextStyle.Brass, TextAnchor.MiddleCenter,
                OMat.WoodDark, new Vector2(1.3f, 0.1f));
            A6_SmallOffice(t, 607, new Vector3(40.75f, A6_Floor, -5.0f), 180f, true, false);
            A6_SmallOffice(t, 608, new Vector3(46.25f, A6_Floor, -5.0f), 180f, false, true);

            // The director's door at the end of the corridor (front faces west, toward the walker).
            var west = OKit.Facing(Vector3.left);
            var door = OfficeLayout.ExecutiveDoor;
            OKit.Threshold(t, "A6_Door_600", new Vector3(49f, A6_Floor + door.y * 0.5f, -12f), west, door, "600", null, false, false, OMat.WoodDark);
            OKit.Sign(t, "T. VANE: DIRECTOR OF CONTINUITY", new Vector3(48.885f, 25.55f, -10.85f), west, 0.026f, WorldTextStyle.Brass, TextAnchor.MiddleCenter,
                OMat.WoodDark, new Vector2(0.74f, 0.08f));
            // OUT OF OFFICE card hanging from the handle-side jamb on a short cord.
            OKit.Box(t, "A6_Tag_Cord", OMat.MetalDark, new Vector3(48.858f, 25.12f, -11.345f), new Vector3(48.862f, 25.3f, -11.335f), false);
            OKit.Sign(t, "OUT OF\nOFFICE", new Vector3(48.85f, 25.02f, -11.34f), west, 0.022f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter,
                OMat.Paper, new Vector2(0.13f, 0.17f));
            OKit.SpotLight(t, "A6_Door600_Spot", new Vector3(47.6f, A6_Ceil - 0.1f, -12f), OKit.Facing(new Vector3(1.4f, -2.6f, 0f)), A6_Warm, 6f, 6f, 42f, true);
            OKit.Prop(t, OProp.PottedPlant, new Vector3(48.4f, A6_Floor, -13.5f));
        }

        /// <summary>
        /// A small office behind one of the corridor doors: desk (facing the door), chair, lamp,
        /// filing cabinet. <paramref name="lit"/> leaves the desk lamp on (visible through the
        /// sidelight); <paramref name="busy"/> adds paper stacks and a coat still on its rack.
        /// </summary>
        static void A6_SmallOffice(Transform parent, int number, Vector3 deskPos, float yaw, bool lit, bool busy)
        {
            var t = new GameObject($"A6_Office_{number}").transform;
            t.SetParent(parent, false);
            var rot = Quaternion.Euler(0f, yaw, 0f);
            OKit.Prop(t, OProp.Desk, deskPos, yaw);
            OKit.Prop(t, OProp.SwivelChair, deskPos + rot * new Vector3(0f, 0f, -0.8f), yaw);
            OKit.Prop(t, OProp.FilingCabinet, deskPos + rot * new Vector3(1.4f, 0f, -1.4f), yaw);
            OKit.Prop(t, OProp.DeskLamp, deskPos + rot * new Vector3(-0.5f, 0.74f, 0.05f), yaw + 20f);
            OKit.Prop(t, OProp.Telephone, deskPos + rot * new Vector3(0.45f, 0.74f, 0.1f), yaw - 10f);
            if (lit) OKit.PointLight(t, $"A6_Office_{number}_Lamp", deskPos + rot * new Vector3(-0.5f, 1.15f, 0.1f), A6_Warm, 1.6f, 3.6f);
            if (busy)
            {
                OKit.Prop(t, OProp.PaperStack, deskPos + rot * new Vector3(0.1f, 0.74f, 0f), yaw + 8f);
                OKit.Prop(t, OProp.PaperStack, deskPos + rot * new Vector3(1.4f, 1.3f, -1.4f), yaw - 5f);
                OKit.Prop(t, OProp.CoatRack, deskPos + rot * new Vector3(-1.4f, 0f, -1.3f), yaw);
            }
            else
            {
                OKit.Prop(t, OProp.PottedPlant, deskPos + rot * new Vector3(-1.45f, 0f, -1.4f), yaw);
            }
        }

        // ------------------------------------------------------------------ spine

        /// <summary>
        /// The lit spine (x 38..49, z -2..2): three warm panels in a row, seen from the dark lounge
        /// like control_01's lit corridor, ending at the boardroom's double doors where the pale
        /// window light shows through.
        /// </summary>
        static void A6_Spine(Transform root)
        {
            var t = new GameObject("A6_Spine").transform;
            t.SetParent(root, false);
            for (int i = 0; i < 3; i++)
                OKit.CeilingLight(t, $"A6_Spine_Light_{i}", new Vector3(40.5f + i * 3.4f, A6_Ceil - 0.03f, 0f), new Vector2(0.6f, 0.6f), A6_Warm, 6f, 5f,
                    false, false, OMat.LightWarm);
            OKit.Sign(t, "BOARDROOM", new Vector3(38.3f, 27.35f, 0f), OKit.Facing(Vector3.left), 0.1f, WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter,
                OMat.MetalDark, new Vector2(1.3f, 0.26f));

            // Double-door surround and brass lettering on the spine side of the boardroom wall.
            OKit.Box(t, "A6_BoardDoor_JambN", OMat.WoodDark, new Vector3(48.82f, A6_Floor, 1.2f), new Vector3(49.18f, 26.85f, 1.35f));
            OKit.Box(t, "A6_BoardDoor_JambS", OMat.WoodDark, new Vector3(48.82f, A6_Floor, -1.35f), new Vector3(49.18f, 26.85f, -1.2f));
            OKit.Box(t, "A6_BoardDoor_Head", OMat.WoodDark, new Vector3(48.82f, 26.7f, -1.35f), new Vector3(49.18f, 26.85f, 1.35f));
            OKit.Sign(t, "BOARDROOM", new Vector3(48.81f, 27.25f, 0f), OKit.Facing(Vector3.left), 0.12f, WorldTextStyle.Brass);
            OKit.Prop(t, OProp.Bench, new Vector3(43f, A6_Floor, 1.6f), 180f);
        }

        // ------------------------------------------------------------------ boardroom

        /// <summary>
        /// The boardroom (x 49..60, z -9..9): a long table running toward the full-height east
        /// window (solid glass), chairs down both sides with one turned to the view, the agenda on
        /// the table, and the whiteboard of matched numbers ending in "000 — ?". A pale fog zone
        /// lets the plain and the lit door 000 read through the window from here.
        /// </summary>
        static void A6_Boardroom(Transform root)
        {
            var t = new GameObject("A6_Boardroom").transform;
            t.SetParent(root, false);

            // Window: one solid glass plane, steel mullions every 3 m, a low dark sill strip.
            OKit.Box(t, "A6_Window_Glass", OMat.Glass, new Vector3(59.8f, A6_Floor, -9f), new Vector3(59.9f, A6_Ceil, 9f));
            for (int i = 0; i <= 6; i++)
            {
                float z = -9f + i * 3f;
                OKit.Box(t, $"A6_Window_Mullion_{i}", OMat.MetalDark, new Vector3(59.72f, A6_Floor, z - 0.05f), new Vector3(59.98f, A6_Ceil, z + 0.05f));
            }
            OKit.Box(t, "A6_Window_Sill", OMat.MetalDark, new Vector3(59.7f, A6_Floor, -9f), new Vector3(59.98f, A6_Floor + 0.06f, 9f));
            OKit.Box(t, "A6_Window_Head", OMat.MetalDark, new Vector3(59.7f, A6_Ceil - 0.12f, -9f), new Vector3(59.98f, A6_Ceil, 9f), false);

            OKit.FogZone(t, "A6_BoardroomView", new Bounds(new Vector3(54.6f, 26f, 0f), new Vector3(10f, 4f, 17.6f)),
                new Color(0.604f, 0.639f, 0.659f), 30f, 240f, 2.5f, 20f);
            OKit.CeilingLight(t, "A6_Boardroom_Panel", new Vector3(53.5f, A6_Ceil - 0.03f, 0f), new Vector2(1.2f, 0.6f), A6_Warm, 4f, 6f, false, false, OMat.LightWarm);

            // Table along x 51..57; chairs either side, the window end left empty.
            OKit.Prop(t, OProp.BoardroomTable, new Vector3(54f, A6_Floor, 0f));
            for (int i = 0; i < 5; i++)
            {
                float x = 51.8f + i * 1.1f;
                OKit.Prop(t, OProp.SwivelChair, new Vector3(x, A6_Floor, 1.25f), 180f + (i % 2 == 0 ? 4f : -3f));
                if (i == 3) continue; // this one stands at the window instead
                OKit.Prop(t, OProp.SwivelChair, new Vector3(x, A6_Floor, -1.25f), i % 2 == 0 ? -5f : 3f);
            }
            OKit.Prop(t, OProp.SwivelChair, new Vector3(58.3f, A6_Floor, -2.4f), 100f); // turned to face out of the window
            OKit.Prop(t, OProp.SwivelChair, new Vector3(50.7f, A6_Floor, -3.3f), 55f);  // pushed back from the table, clear of the doors

            // The agenda, flat on the table near the door end, readable walking in (facing +X).
            OKit.Sign(t, "AGENDA\n\n1. Re-numbering.\n2. The exterior.\n3. AOB", new Vector3(51.5f, A6_Floor + 0.762f, -0.25f),
                Quaternion.LookRotation(Vector3.up, Vector3.right), 0.026f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.3f, 0.4f));
            OKit.Prop(t, OProp.PaperStack, new Vector3(52.6f, A6_Floor + 0.75f, 0.4f), 12f);
            OKit.Prop(t, OProp.Telephone, new Vector3(56.6f, A6_Floor + 0.75f, 0.5f), -15f);
            OKit.Prop(t, OProp.PottedPlant, new Vector3(50f, A6_Floor, 8.3f));
            OKit.Prop(t, OProp.PottedPlant, new Vector3(50f, A6_Floor, -8.3f));
            OKit.Prop(t, OProp.PottedPlant, new Vector3(59f, A6_Floor, 8.3f));
            OKit.Prop(t, OProp.Flag, new Vector3(59.1f, A6_Floor, -8.4f));

            A6_Whiteboard(t);
        }

        /// <summary>
        /// The whiteboard on the boardroom's north wall: someone's working notes on the
        /// re-numbering, every pair of matched numbers written as a line, and the last one
        /// unanswered. Not addressed to anyone; the hint is in the pattern.
        /// </summary>
        static void A6_Whiteboard(Transform t)
        {
            const float scale = 1.5f;
            OKit.Prop(t, OProp.Whiteboard, new Vector3(54f, 24.8f, 8.86f), 180f, scale);
            var face = OKit.Facing(Vector3.back);
            float z = 8.86f - OProp.SizeOf(OProp.Whiteboard).z * scale * 0.5f - 0.02f;
            const float lh = 0.075f;

            OKit.Sign(t, "RE-NUMBERING — draft 3", new Vector3(53.35f, 26.35f, z), face, 0.06f);
            OKit.Sign(t, "(T.V., Thurs.)", new Vector3(54.95f, 26.35f, z), face, 0.04f);

            OKit.Sign(t, "104 — 104", new Vector3(53.3f, 26.05f, z), face, lh);
            OKit.Sign(t, "1 — 1", new Vector3(53.3f, 25.8f, z), face, lh);
            OKit.Sign(t, "37 — 37", new Vector3(53.3f, 25.55f, z), face, lh);
            OKit.Sign(t, "6 — 6", new Vector3(53.3f, 25.3f, z), face, lh);
            OKit.Sign(t, "312 — 312", new Vector3(54.8f, 26.05f, z), face, lh);
            OKit.Sign(t, "412 — 412", new Vector3(54.8f, 25.8f, z), face, lh);
            OKit.Sign(t, "000 — ?", new Vector3(54.8f, 25.35f, z), face, 0.11f);

            OKit.Sign(t, "4:47", new Vector3(52.95f, 24.98f, z), face, 0.035f);
        }

        // ------------------------------------------------------------------ director

        /// <summary>
        /// Theodora Vane's office behind door 600 (x 49..60, z -20..-9, about 10 m deep): her desk
        /// facing the door under a lamp, the open ledger, a letter left for her, a flip clock
        /// stopped at 4:47, a calendar never turned past the 17th, a sofa and a folded flag.
        /// </summary>
        static void A6_Director(Transform root)
        {
            var t = new GameObject("A6_Director").transform;
            t.SetParent(root, false);
            const float top = A6_Floor + 0.74f;

            // Veneer lining over the concrete east wall.
            OKit.Box(t, "A6_Dir_Lining", OMat.WoodVeneer, new Vector3(59.66f, A6_Floor, -19.7f), new Vector3(59.7f, A6_Ceil, -9.1f));

            OKit.Prop(t, OProp.Desk, new Vector3(55f, A6_Floor, -14.5f), -90f);
            OKit.Prop(t, OProp.SwivelChair, new Vector3(55.9f, A6_Floor, -14.5f), -90f);
            OKit.Prop(t, OProp.DeskLamp, new Vector3(55.05f, top, -15.0f), -110f);
            OKit.Prop(t, OProp.Telephone, new Vector3(55.1f, top, -13.95f), -80f);
            OKit.PointLight(t, "A6_Dir_Lamp", new Vector3(54.9f, 25.15f, -15.0f), A6_Warm, 2f, 5f, true);

            var up = Quaternion.LookRotation(Vector3.up, Vector3.right); // flat, readable from the door side
            // The ledger: a leather cover with the open page on top.
            OKit.Box(t, "A6_Dir_LedgerCover", OMat.Leather, new Vector3(54.64f, top, -14.9f), new Vector3(54.96f, top + 0.012f, -14.5f), false);
            OKit.Sign(t, "17 NOV.\nSIGNED IN  214\nSIGNED OUT  213", new Vector3(54.8f, top + 0.024f, -14.7f), up, 0.02f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.36f, 0.26f));
            OKit.Sign(t, "Theodora —\nThe evening count\nwas one over again.\nFacilities says the\nbuilding is only doing\nas it was asked.\nI have stopped asking.\n— A.",
                new Vector3(54.75f, top + 0.014f, -14.05f), Quaternion.LookRotation(Vector3.up, new Vector3(1f, 0f, 0.15f)), 0.014f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.24f, 0.32f));

            // Flip clock on the desk's corner, facing the visitor.
            OKit.Box(t, "A6_Dir_FlipClock", OMat.Plastic, new Vector3(54.78f, top, -15.45f), new Vector3(54.9f, top + 0.09f, -15.25f), false);
            OKit.Sign(t, "4:47", new Vector3(54.775f, top + 0.045f, -15.35f), OKit.Facing(Vector3.left), 0.04f, WorldTextStyle.Glow);

            // Calendar on the north wall (inner face z -9.1).
            OKit.Sign(t, "NOVEMBER 1977\n\n17   board, 4:47\n18\n19\n20", new Vector3(51.6f, 25.7f, -9.112f), OKit.Facing(Vector3.back), 0.03f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.42f, 0.5f));

            OKit.Prop(t, OProp.LeatherSofa, new Vector3(54.5f, A6_Floor, -19.25f));
            OKit.Prop(t, OProp.SideTable, new Vector3(56.2f, A6_Floor, -19.3f));
            OKit.Prop(t, OProp.CoatRack, new Vector3(50f, A6_Floor, -18.9f));
            OKit.Prop(t, OProp.Flag, new Vector3(59.2f, A6_Floor, -9.6f));
            OKit.Prop(t, OProp.PottedPlant, new Vector3(59.1f, A6_Floor, -19.1f));
            OKit.Prop(t, OProp.FilingCabinet, new Vector3(58f, A6_Floor, -9.5f), 180f);
            OKit.Prop(t, OProp.FilingCabinet, new Vector3(58.55f, A6_Floor, -9.5f), 180f);
        }

        // ------------------------------------------------------------------ archive

        /// <summary>
        /// A dim records room behind brass door 609 off the dark hall: two rows of archive shelving
        /// under a failing fluorescent panel, labelled with the years of clocking cards.
        /// </summary>
        static void A6_Archive(Transform root)
        {
            var t = new GameObject("A6_Archive").transform;
            t.SetParent(root, false);
            OKit.Threshold(t, "A6_Door_609", new Vector3(49f, A6_Floor + OfficeLayout.Door.y * 0.5f, 11f), OKit.Facing(Vector3.left), OfficeLayout.Door, "609");
            OKit.CeilingLight(t, "A6_Archive_Panel", new Vector3(54.5f, A6_Ceil - 0.03f, 13.5f), new Vector2(1.2f, 0.3f), A6_Cool, 3f, 6f, true);
            foreach (float z in new[] { 14f, 18f })
                for (int i = 0; i < 4; i++)
                    OKit.Prop(t, OProp.ArchiveShelf, new Vector3(52f + i * 2f, A6_Floor, z));
            OKit.Sign(t, "CLOCKING CARDS\n1974 — 79", new Vector3(50.95f, 26.3f, 14f), OKit.Facing(Vector3.left), 0.04f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OMat.Paper, new Vector2(0.4f, 0.16f));
            OKit.Prop(t, OProp.PaperStack, new Vector3(51.2f, A6_Floor, 10.2f), 30f);
            OKit.Prop(t, OProp.FilingCabinet, new Vector3(59.2f, A6_Floor, 10f), -90f);
        }
    }
}
