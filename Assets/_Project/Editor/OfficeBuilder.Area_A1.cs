using Kit.Gameplay.Signage;
using UnityEngine;

namespace Kit.Editor
{
    /// <summary>
    /// A1 Waiting Room (spawn, L4, y 16). The player wakes in a wood-panelled waiting room: rows of
    /// orange shell chairs facing a shuttered reception hatch under a NOW SERVING 104 display, one
    /// lit ceiling panel pooling light in fog, a warm lamp at the hatch. Beside the hatch, brass door
    /// "1" pairs with the lobby's revolving doors. A short passage leads east into a cold concrete
    /// corridor along the atrium wall whose two end doors are both "104": walking either way comes
    /// out of the other end (K1). The corridor's exits are A2's sockets: a transom window over a low
    /// bench, and a jammed, half-lowered roller shutter to slide under.
    /// Layout (world): waiting room x -50..-27.9, z -10..10, ceiling 20; service strip (reception
    /// office, passage) x -27.7..-24.3; corridor x -24..-20.25, z -10.4..10.4, ceiling 20.5, with
    /// 1.1 m dead-end vestibules behind the 104 doors.
    /// </summary>
    public static partial class OfficeBuilder
    {
        const float A1_Floor = 16f;
        const float A1_RoomCeiling = 20f;      // waiting room: low, slightly oppressive (4 m)
        const float A1_CorridorCeiling = 20.5f; // 4.5 m so the transom (top 19.6) fits

        // Waiting room interior and the walls around it.
        const float A1_RoomWestX = -50f, A1_RoomEastX = -27.9f;   // east wall x -27.9..-27.7 (0.2, so frames show)
        const float A1_RoomSouthZ = -10f, A1_RoomNorthZ = 10f;
        const float A1_ServiceEastX = -24.3f;                     // corridor west wall x -24.3..-24.0
        const float A1_CorridorWestX = -24f, A1_CorridorEastX = -20.25f;
        const float A1_CorridorMidX = -22.125f;
        const float A1_EndDoorZ = 10.5f;                          // 104 end walls centred on z +-10.5 (0.2 thick)
        const float A1_DoorOneZ = -4f;                            // reception door "1" centre z
        const float A1_PassageZ0 = 3f, A1_PassageZ1 = 5f;         // waiting room <-> corridor opening

        static readonly Color A1_Cool = new Color(0.82f, 0.9f, 1f);
        static readonly Color A1_Warm = new Color(1f, 0.7f, 0.42f);

        static partial void BuildArea_A1(Transform root)
        {
            A1_Shell(root);
            A1_WaitingRoom(root);
            A1_Reception(root);
            A1_Corridor(root);
            A1_Sockets(root);
            A1_Lighting(root);
            OfficeKit.Zone(root, "A1", OfficeLayout.A1);
        }

        // ------------------------------------------------------------------ structure

        /// <summary>Floors, walls and ceilings for every A1 space, with the openings cut for door 1, the hatch, the passage and both 104 doors.</summary>
        static void A1_Shell(Transform root)
        {
            var shell = new GameObject("Shell").transform;
            shell.SetParent(root, false);
            float f = A1_Floor;

            // One structural floor under the whole area; carpets are thin overlays.
            OfficeKit.Box(shell, "Floor", OfficeKit.M.ConcreteFloor, new Vector3(-50.3f, f - 0.3f, -12f), new Vector3(A1_CorridorEastX, f, 12f));
            OfficeKit.Box(shell, "Carpet_WaitingRoom", OfficeKit.M.CarpetOxblood, new Vector3(A1_RoomWestX, f, A1_RoomSouthZ), new Vector3(A1_RoomEastX, f + 0.01f, A1_RoomNorthZ), false);

            // Waiting room: warm wood on all sides (control_01), acoustic tile ceiling at 4 m.
            float roomH = A1_RoomCeiling + 0.3f - f;
            OfficeKit.Wall(shell, "Wall_West", OfficeKit.M.WoodVeneer, new Vector3(-50.15f, 0f, -10.3f), new Vector3(-50.15f, 0f, 10.3f), f, roomH, 0.3f);
            OfficeKit.Wall(shell, "Wall_North", OfficeKit.M.WoodVeneer, new Vector3(-50.3f, 0f, 10.15f), new Vector3(A1_ServiceEastX, 0f, 10.15f), f, roomH, 0.3f);
            OfficeKit.Wall(shell, "Wall_South", OfficeKit.M.WoodVeneer, new Vector3(-50.3f, 0f, -10.15f), new Vector3(A1_ServiceEastX, 0f, -10.15f), f, roomH, 0.3f);
            // East wall (z -10 -> 10): door 1 (z -4.5..-3.5), the reception hatch (z -1.2..1.2, sill 0.95), the passage (z 3..5).
            OfficeKit.Wall(shell, "Wall_East_Reception", OfficeKit.M.WoodVeneer, new Vector3(-27.8f, 0f, -10f), new Vector3(-27.8f, 0f, 10f), f, roomH, 0.2f,
                OfficeKit.Opening.Door(A1_DoorOneZ - OfficeLayout.Door.x * 0.5f + 10f),
                new OfficeKit.Opening(8.8f, 2.4f, 0.95f, 1.1f),
                new OfficeKit.Opening(A1_PassageZ0 + 10f, A1_PassageZ1 - A1_PassageZ0, 0f, 2.4f));
            OfficeKit.Box(shell, "Ceiling_WaitingRoom", OfficeKit.M.CeilingTile, new Vector3(-50.3f, A1_RoomCeiling, -10.3f), new Vector3(-27.7f, A1_RoomCeiling + 0.3f, 10.3f));

            // Service strip between the waiting room and the corridor: passage, reception office, two sealed voids.
            OfficeKit.Box(shell, "Passage_WallSouth", OfficeKit.M.Concrete, new Vector3(-27.7f, f, 2.85f), new Vector3(A1_ServiceEastX, A1_RoomCeiling + 0.3f, A1_PassageZ0));
            OfficeKit.Box(shell, "Passage_WallNorth", OfficeKit.M.Concrete, new Vector3(-27.7f, f, A1_PassageZ1), new Vector3(A1_ServiceEastX, A1_RoomCeiling + 0.3f, 5.15f));
            OfficeKit.Box(shell, "Passage_Ceiling", OfficeKit.M.ConcreteDark, new Vector3(-27.7f, f + 2.4f, A1_PassageZ0), new Vector3(A1_ServiceEastX, f + 2.6f, A1_PassageZ1));
            OfficeKit.Box(shell, "Reception_WallSouth", OfficeKit.M.WoodVeneer, new Vector3(-27.7f, f, -6.65f), new Vector3(A1_ServiceEastX, A1_RoomCeiling + 0.3f, -6.5f));
            OfficeKit.Box(shell, "Reception_Ceiling", OfficeKit.M.CeilingTile, new Vector3(-27.7f, 18.8f, -6.5f), new Vector3(A1_ServiceEastX, 18.95f, 2.85f));
            OfficeKit.Box(shell, "Reception_Carpet", OfficeKit.M.CarpetGrey, new Vector3(-27.7f, f, -6.5f), new Vector3(A1_ServiceEastX, f + 0.01f, 2.85f), false);
            OfficeKit.Box(shell, "Service_Lid", OfficeKit.M.ConcreteDark, new Vector3(-27.7f, A1_RoomCeiling + 0.3f, -10.3f), new Vector3(A1_CorridorWestX, A1_RoomCeiling + 0.6f, 10.3f));

            // Corridor: cold board-marked concrete, 4.5 m ceiling.
            float corrH = A1_CorridorCeiling + 0.3f - f;
            OfficeKit.Wall(shell, "Corridor_WallWest", OfficeKit.M.Concrete, new Vector3(-24.15f, 0f, -12f), new Vector3(-24.15f, 0f, 12f), f, corrH, 0.3f,
                new OfficeKit.Opening(A1_PassageZ0 + 12f, A1_PassageZ1 - A1_PassageZ0, 0f, 2.4f));
            OfficeKit.Box(shell, "Corridor_Ceiling", OfficeKit.M.ConcreteDark, new Vector3(A1_CorridorWestX, A1_CorridorCeiling, -12f), new Vector3(A1_CorridorEastX, A1_CorridorCeiling + 0.3f, 12f));

            // End walls with the 104 openings (1.2 x 2.4), then 1.1 m dead-end vestibules and a back wall.
            float doorStart = A1_CorridorMidX - OfficeLayout.ExecutiveDoor.x * 0.5f - A1_CorridorWestX;
            var door104 = new OfficeKit.Opening(doorStart, OfficeLayout.ExecutiveDoor.x, 0f, OfficeLayout.ExecutiveDoor.y);
            float endH = A1_CorridorCeiling - f;
            OfficeKit.Wall(shell, "Corridor_EndNorth", OfficeKit.M.Concrete, new Vector3(A1_CorridorWestX, 0f, A1_EndDoorZ), new Vector3(A1_CorridorEastX, 0f, A1_EndDoorZ), f, endH, 0.2f, door104);
            OfficeKit.Wall(shell, "Corridor_EndSouth", OfficeKit.M.Concrete, new Vector3(A1_CorridorWestX, 0f, -A1_EndDoorZ), new Vector3(A1_CorridorEastX, 0f, -A1_EndDoorZ), f, endH, 0.2f, door104);
            OfficeKit.Box(shell, "Vestibule_BackNorth", OfficeKit.M.ConcreteDark, new Vector3(A1_CorridorWestX, f, 11.7f), new Vector3(A1_CorridorEastX, A1_CorridorCeiling, 12f));
            OfficeKit.Box(shell, "Vestibule_BackSouth", OfficeKit.M.ConcreteDark, new Vector3(A1_CorridorWestX, f, -12f), new Vector3(A1_CorridorEastX, A1_CorridorCeiling, -11.7f));
        }

        // ------------------------------------------------------------------ waiting room

        /// <summary>Chairs, plants, wall dressing and the paper trail of the waiting room. The spawn aisle (z -1..1) stays clear.</summary>
        static void A1_WaitingRoom(Transform root)
        {
            var room = new GameObject("WaitingRoom").transform;
            room.SetParent(root, false);
            float f = A1_Floor;

            A1_WoodPanelling(room);

            // Rows of shell chairs facing east toward the hatch (inside_02). Two benches each side of the aisle.
            float[] rowsX = { -45f, -42.5f, -37.5f, -35f, -32.5f };
            float[] benchZ = { 2.15f, 4.5f, -2.15f, -4.5f };
            foreach (float x in rowsX)
                foreach (float z in benchZ)
                {
                    // Back row, north side: one bench gone, a single chair left at an angle.
                    if (Mathf.Approximately(x, -45f) && z > 0f) continue;
                    float yaw = 90f;
                    if (Mathf.Approximately(x, -32.5f) && Mathf.Approximately(z, 4.5f)) yaw = 84f; // nudged out of line
                    OfficeKit.Prop(room, OfficeKit.PropIds.ShellChairRow, new Vector3(x, f, z), yaw);
                }
            OfficeKit.Prop(room, OfficeKit.PropIds.ShellChair, new Vector3(-45.3f, f, 2.9f), 62f);
            OfficeKit.Prop(room, OfficeKit.PropIds.ShellChair, new Vector3(-44.6f, f, 4.8f), 101f);

            // The ticket somebody was holding: on the aisle seat, second row from the hatch.
            A1_Flat(room, "104", new Vector3(-35f, f + 0.48f, -1.3f), 0.03f, new Vector2(0.07f, 0.045f));

            // West wall: leather bench under the framed print, side tables, a coat rack.
            OfficeKit.Prop(room, OfficeKit.PropIds.LeatherSofa, new Vector3(-49.55f, f, 0f), 90f);
            OfficeKit.Prop(room, OfficeKit.PropIds.SideTable, new Vector3(-49.6f, f, 1.5f), 90f);
            OfficeKit.Prop(room, OfficeKit.PropIds.SideTable, new Vector3(-49.6f, f, -1.5f), 90f);
            A1_Flat(room, "OFFICE MONTHLY\nApril 1977\n\nThe Paperless\nOffice: 1990?", new Vector3(-49.6f, f + 0.565f, 1.45f), 0.014f, new Vector2(0.2f, 0.27f));
            OfficeKit.Prop(room, OfficeKit.PropIds.CoatRack, new Vector3(-49.3f, f, -8.9f));
            A1_FramedPrint(room);

            // Philodendrons: long concrete planters on the side walls, pots in the corners.
            OfficeKit.Prop(room, OfficeKit.PropIds.Planter, new Vector3(-44f, f, 9.5f));
            OfficeKit.Prop(room, OfficeKit.PropIds.Planter, new Vector3(-38f, f, -9.5f), 180f);
            OfficeKit.Prop(room, OfficeKit.PropIds.Planter, new Vector3(-31.5f, f, -9.5f), 180f);
            OfficeKit.Prop(room, OfficeKit.PropIds.PottedPlant, new Vector3(-49.2f, f, 9.1f));
            OfficeKit.Prop(room, OfficeKit.PropIds.PottedPlant, new Vector3(-28.5f, f, 8.6f));
            OfficeKit.Prop(room, OfficeKit.PropIds.PottedPlant, new Vector3(-28.5f, f, -8.6f));
            OfficeKit.Sign(room, "WATERED THURSDAYS  - H.L.", new Vector3(-44f, f + 0.55f, 9.08f), OfficeKit.Facing(Vector3.back), 0.018f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.42f, 0.05f));

            // North wall: corkboard of memos and the water cooler.
            A1_Corkboard(room);
            OfficeKit.Prop(room, OfficeKit.PropIds.WaterCooler, new Vector3(-32.6f, f, 9.6f), 180f);
            OfficeKit.Sign(room, "SPRING WATER\ndelivered Thursdays\ncups at Reception", new Vector3(-32.6f, f + 1.55f, 9.985f), OfficeKit.Facing(Vector3.back), 0.02f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.34f, 0.15f));

            // South wall: the fire notice.
            OfficeKit.Sign(room,
                "FIRE PROCEDURE  -  FLOOR 4\n\n" +
                "On the alarm, this floor is cleared\n" +
                "by the nearest stair. Lifts do not\n" +
                "run during an alarm.\n" +
                "Assembly point: the front forecourt.\n\n" +
                "Warden    G. Fenwick    ext. 214\n" +
                "Deputy     O. Marsh       ext. 260\n\n" +
                "The roll is called at the forecourt.\n" +
                "Last drill: all present.",
                new Vector3(-46.4f, f + 1.6f, -9.985f), OfficeKit.Facing(Vector3.forward), 0.014f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.5f, 0.42f));

            // East wall, north of the passage: the stopped clock and its plate.
            OfficeKit.Prop(room, OfficeKit.PropIds.WallClock, new Vector3(-27.93f, f + 2.6f, 7.2f), -90f);
            OfficeKit.Sign(room, "MASTER CLOCK SYSTEM\nFLOOR 4  -  SYNCHRONISED", new Vector3(-27.915f, f + 2.45f, 7.2f), OfficeKit.Facing(Vector3.left), 0.016f,
                WorldTextStyle.Brass, TextAnchor.MiddleCenter, OfficeKit.M.WoodDark, new Vector2(0.42f, 0.07f));

            // Over the passage, on both sides.
            OfficeKit.Sign(room, "CORRIDOR 104", new Vector3(-27.915f, f + 2.75f, 4f), OfficeKit.Facing(Vector3.left), 0.07f,
                WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter, OfficeKit.M.MetalDark, new Vector2(0.95f, 0.17f));
        }

        /// <summary>Plywood panel seams and a dark skirting on the three plain walls, so the veneer reads as panelling (control_01).</summary>
        static void A1_WoodPanelling(Transform room)
        {
            float f = A1_Floor, top = A1_RoomCeiling;
            var p = new GameObject("Panelling").transform;
            p.SetParent(room, false);
            for (float x = -47.6f; x < A1_RoomEastX - 0.5f; x += 2.4f)
            {
                OfficeKit.Box(p, "Seam_N", OfficeKit.M.WoodDark, new Vector3(x - 0.015f, f, 9.975f), new Vector3(x + 0.015f, top, 10f), false);
                OfficeKit.Box(p, "Seam_S", OfficeKit.M.WoodDark, new Vector3(x - 0.015f, f, -10f), new Vector3(x + 0.015f, top, -9.975f), false);
            }
            for (float z = -7.2f; z < 7.3f; z += 2.4f)
                OfficeKit.Box(p, "Seam_W", OfficeKit.M.WoodDark, new Vector3(-50f, f, z - 0.015f), new Vector3(-49.975f, top, z + 0.015f), false);
            OfficeKit.Box(p, "Skirting_N", OfficeKit.M.WoodDark, new Vector3(A1_RoomWestX, f, 9.97f), new Vector3(A1_RoomEastX, f + 0.1f, 10f), false);
            OfficeKit.Box(p, "Skirting_S", OfficeKit.M.WoodDark, new Vector3(A1_RoomWestX, f, -10f), new Vector3(A1_RoomEastX, f + 0.1f, -9.97f), false);
            OfficeKit.Box(p, "Skirting_W", OfficeKit.M.WoodDark, new Vector3(-50f, f, A1_RoomSouthZ), new Vector3(-49.97f, f + 0.1f, A1_RoomNorthZ), false);

            // Dead troffers in the dropped ceiling: only one panel in the room is lit.
            Vector2[] dead = { new Vector2(-43f, -5f), new Vector2(-43f, 5f), new Vector2(-29.5f, -6f), new Vector2(-29.5f, 6.5f), new Vector2(-48f, 0f) };
            foreach (var d in dead)
                OfficeKit.Box(p, "Troffer_Off", OfficeKit.M.TileWhite, new Vector3(d.x - 0.6f, top - 0.02f, d.y - 0.6f), new Vector3(d.x + 0.6f, top, d.y + 0.6f), false);
        }

        /// <summary>The corkboard on the north wall with four memos: Facilities on numbering, birthdays, lost property, the carpool.</summary>
        static void A1_Corkboard(Transform room)
        {
            float f = A1_Floor;
            const float cx = -37f, z = 9.985f, scale = 1.25f;
            OfficeKit.Prop(room, OfficeKit.PropIds.Corkboard, new Vector3(cx, f + 1.0f, z), 180f, scale);
            float y = f + 1.0f + 0.9f * scale * 0.5f;
            var back = OfficeKit.Facing(Vector3.back);
            var sheet = new Vector2(0.3f, 0.4f);

            // Facing -Z, the viewer's left is +X, so the first memo sits at the highest x.
            A1_Memo(room,
                "FACILITIES  -  FLOOR 4\n" +
                "Re: room numbering\n\n" +
                "Rooms on this floor will\n" +
                "be renumbered again from\n" +
                "Monday. Where two doors\n" +
                "carry the same number,\n" +
                "report it to Facilities.\n" +
                "Do not wedge either door.\n\n" +
                "The corridor by Reception\n" +
                "has been measured twice.\n" +
                "Both figures are on file.\n\n" +
                "W. Brisk, Facilities",
                new Vector3(cx + 0.54f, y + 0.02f, z - 0.03f), back, sheet);
            A1_Memo(room,
                "NOVEMBER BIRTHDAYS\n\n" +
                " 3   Mae Okonjo\n" +
                "11   Hugo Lindqvist\n" +
                "19   Lillian Sato\n" +
                "27   Felix Mortimer\n\n" +
                "Card is in Ruth's\n" +
                "second drawer.\n" +
                "Cake Friday, 4:45.",
                new Vector3(cx + 0.18f, y - 0.03f, z - 0.03f), back, sheet);
            A1_Memo(room,
                "FOUND AT RECEPTION\n\n" +
                "glove, brown, left\n" +
                "reading glasses,\n" +
                "   tortoiseshell\n" +
                "umbrella, black\n" +
                "key ring, fob no. 104\n\n" +
                "Unclaimed items go\n" +
                "to Records on the 30th.\n" +
                "A. Pell",
                new Vector3(cx - 0.18f, y + 0.03f, z - 0.03f), back, sheet);
            A1_Memo(room,
                "CARPOOL  -  ROUTE 9\n" +
                "Mon-Fri, leaving 5:00\n\n" +
                "T. Varga (driver)\n" +
                "I. Castellano\n" +
                "K. Ibarra\n" +
                "______________\n\n" +
                "One seat.\n" +
                "Still one seat.",
                new Vector3(cx - 0.54f, y - 0.02f, z - 0.03f), back, sheet);
        }

        /// <summary>A framed print of the building's model above the leather bench, with its caption.</summary>
        static void A1_FramedPrint(Transform room)
        {
            float f = A1_Floor;
            var right = OfficeKit.Facing(Vector3.right);
            var frame = new GameObject("FramedPrint").transform;
            frame.SetParent(room, false);
            OfficeKit.BoxAt(frame, "Frame", OfficeKit.M.WoodDark, new Vector3(-49.975f, f + 2.0f, 0f), new Vector3(0.05f, 1.0f, 1.4f), Quaternion.identity, false);
            OfficeKit.BoxAt(frame, "Print", OfficeKit.M.ConcreteLight, new Vector3(-49.945f, f + 2.0f, 0f), new Vector3(0.02f, 0.84f, 1.24f), Quaternion.identity, false);
            // Stepped massing of the model, in relief.
            OfficeKit.BoxAt(frame, "Mass_A", OfficeKit.M.ConcreteDark, new Vector3(-49.93f, f + 1.75f, -0.3f), new Vector3(0.02f, 0.22f, 0.5f), Quaternion.identity, false);
            OfficeKit.BoxAt(frame, "Mass_B", OfficeKit.M.ConcreteDark, new Vector3(-49.93f, f + 1.86f, 0.12f), new Vector3(0.02f, 0.44f, 0.34f), Quaternion.identity, false);
            OfficeKit.BoxAt(frame, "Mass_C", OfficeKit.M.ConcreteDark, new Vector3(-49.93f, f + 1.8f, 0.4f), new Vector3(0.02f, 0.32f, 0.22f), Quaternion.identity, false);
            OfficeKit.Sign(room, "HALE & ORMSBY HOUSE\narchitects' model, 1971\nsix floors, as drawn", new Vector3(-49.965f, f + 1.36f, 0f), right, 0.018f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.5f, 0.13f));
        }

        // ------------------------------------------------------------------ reception

        /// <summary>
        /// The reception front (shuttered hatch, NOW SERVING display, nameplate, ticket dispenser),
        /// brass door "1" (pairs with the lobby's revolving doors), and the small office behind it.
        /// </summary>
        static void A1_Reception(Transform root)
        {
            var rec = new GameObject("Reception").transform;
            rec.SetParent(root, false);
            float f = A1_Floor;
            var west = OfficeKit.Facing(Vector3.left);

            // Door 1: front faces west, into the waiting room. Pairs with the lobby's door 1.
            OfficeKit.Threshold(rec, "Threshold_1_ReceptionDoor", new Vector3(-27.8f, f + OfficeLayout.Door.y * 0.5f, A1_DoorOneZ), west, OfficeLayout.Door, "1");

            // The hatch: a counter ledge and a closed roller shutter in the opening (z -1.2..1.2, y 16.95..18.05).
            OfficeKit.Box(rec, "Hatch_Counter", OfficeKit.M.WoodDark, new Vector3(-28.2f, f + 0.9f, -1.3f), new Vector3(-27.7f, f + 0.95f, 1.3f));
            OfficeKit.Box(rec, "Hatch_Shutter", OfficeKit.M.Metal, new Vector3(-27.82f, f + 0.95f, -1.2f), new Vector3(-27.74f, f + 2.05f, 1.2f));
            for (int i = 1; i < 6; i++)
            {
                float y = f + 0.95f + i * 0.18f;
                OfficeKit.Box(rec, "Hatch_Slat", OfficeKit.M.MetalDark, new Vector3(-27.835f, y - 0.008f, -1.2f), new Vector3(-27.82f, y + 0.008f, 1.2f), false);
            }
            OfficeKit.Sign(rec, "CLOSED\nback at 4:50", new Vector3(-27.85f, f + 1.5f, 0.35f), west, 0.035f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.34f, 0.2f));

            // NOW SERVING display above the hatch (a sign, not a threshold).
            OfficeKit.BoxAt(rec, "NowServing_Box", OfficeKit.M.MetalDark, new Vector3(-27.97f, f + 2.75f, 0f), new Vector3(0.14f, 0.62f, 1.7f), Quaternion.identity, false);
            OfficeKit.Sign(rec, "NOW SERVING", new Vector3(-28.045f, f + 2.94f, 0f), west, 0.07f, WorldTextStyle.Glow);
            OfficeKit.Sign(rec, "104", new Vector3(-28.045f, f + 2.66f, 0f), west, 0.22f, WorldTextStyle.Glow);

            // Nameplate between the hatch and door 1, and the warm lamp on the counter.
            OfficeKit.Sign(rec, "RECEPTION\nA. PELL", new Vector3(-27.915f, f + 1.65f, -2.3f), west, 0.04f,
                WorldTextStyle.Brass, TextAnchor.MiddleCenter, OfficeKit.M.WoodDark, new Vector2(0.5f, 0.17f));
            OfficeKit.Prop(rec, OfficeKit.PropIds.DeskLamp, new Vector3(-28.05f, f + 0.95f, 0.9f), -120f);

            // Take a number.
            OfficeKit.Prop(rec, OfficeKit.PropIds.TicketDispenser, new Vector3(-28.6f, f, -2.3f), -90f);
            OfficeKit.Sign(rec, "PLEASE TAKE\nA NUMBER", new Vector3(-28.6f, f + 1.48f, -2.3f), west, 0.03f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.3f, 0.12f));
            A1_Flat(rec, "101", new Vector3(-29.1f, f + 0.012f, -1.9f), 0.02f, new Vector2(0.06f, 0.04f));
            A1_Flat(rec, "098", new Vector3(-29.5f, f + 0.012f, -2.7f), 0.02f, new Vector2(0.06f, 0.04f));

            // The office behind (x -27.7..-24.3, z -6.5..2.85): never seen while door 1 is paired, but it exists.
            OfficeKit.Prop(rec, OfficeKit.PropIds.Desk, new Vector3(-25.9f, f, -0.2f), -90f);
            OfficeKit.Prop(rec, OfficeKit.PropIds.SwivelChair, new Vector3(-25.0f, f, -0.2f), -90f);
            OfficeKit.Prop(rec, OfficeKit.PropIds.Telephone, new Vector3(-25.9f, f + 0.74f, 0.3f), -90f);
            OfficeKit.Prop(rec, OfficeKit.PropIds.PaperStack, new Vector3(-25.9f, f + 0.74f, -0.45f), -90f);
            A1_Flat(rec, "VISITORS\nNOVEMBER", new Vector3(-25.9f, f + 0.85f, -0.45f), 0.02f, new Vector2(0.2f, 0.12f));
            OfficeKit.Sign(rec, "AGNES PELL", new Vector3(-26.25f, f + 0.8f, -0.2f), west, 0.03f,
                WorldTextStyle.Brass, TextAnchor.MiddleCenter, OfficeKit.M.WoodDark, new Vector2(0.32f, 0.06f));
            OfficeKit.Prop(rec, OfficeKit.PropIds.FilingCabinet, new Vector3(-24.7f, f, -5.6f), -90f);
            OfficeKit.Prop(rec, OfficeKit.PropIds.CoatRack, new Vector3(-24.8f, f, 2.2f));
            OfficeKit.Sign(rec, "NOVEMBER 1977", new Vector3(-24.315f, f + 1.7f, -3f), west, 0.04f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.5f, 0.6f));
        }

        // ------------------------------------------------------------------ corridor

        /// <summary>
        /// The 104 corridor: two brass "104" thresholds facing each other across it (an endless
        /// corridor), a floor stripe and pipes that continue through the repeat, and a few slightly
        /// wrong details (a chair facing the wall, one philodendron, the same memo) that make the
        /// repeat noticeable.
        /// </summary>
        static void A1_Corridor(Transform root)
        {
            var cor = new GameObject("Corridor104").transform;
            cor.SetParent(root, false);
            float f = A1_Floor;
            float doorY = f + OfficeLayout.ExecutiveDoor.y * 0.5f;

            // North end: front faces south, into the corridor. South end: front faces north. Front in, front out.
            OfficeKit.Threshold(cor, "Threshold_104_North", new Vector3(A1_CorridorMidX, doorY, A1_EndDoorZ), OfficeKit.Facing(Vector3.back), OfficeLayout.ExecutiveDoor, "104");
            OfficeKit.Threshold(cor, "Threshold_104_South", new Vector3(A1_CorridorMidX, doorY, -A1_EndDoorZ), OfficeKit.Facing(Vector3.forward), OfficeLayout.ExecutiveDoor, "104");

            // Things that run the length of the corridor and line up through the doors.
            OfficeKit.Box(cor, "FloorStripe", OfficeKit.M.FloorInlay, new Vector3(A1_CorridorMidX - 0.06f, f, -10.4f), new Vector3(A1_CorridorMidX + 0.06f, f + 0.006f, 10.4f), false);
            OfficeKit.Box(cor, "Pipe_A", OfficeKit.M.MetalDark, new Vector3(-23.78f, A1_CorridorCeiling - 0.32f, -10.4f), new Vector3(-23.62f, A1_CorridorCeiling - 0.16f, 10.4f), false);
            OfficeKit.Box(cor, "Pipe_B", OfficeKit.M.Metal, new Vector3(-23.5f, A1_CorridorCeiling - 0.24f, -10.4f), new Vector3(-23.4f, A1_CorridorCeiling - 0.14f, 10.4f), false);

            // Slightly wrong: one chair turned to the wall, one philodendron, a bin by the passage.
            OfficeKit.Prop(cor, OfficeKit.PropIds.ShellChair, new Vector3(-23.6f, f, -7.8f), -90f);
            OfficeKit.Prop(cor, OfficeKit.PropIds.PottedPlant, new Vector3(-23.6f, f, 8.9f));
            OfficeKit.Prop(cor, OfficeKit.PropIds.Bin, new Vector3(-23.75f, f, 5.6f));

            var east = OfficeKit.Facing(Vector3.right);
            A1_Memo(cor,
                "To whoever keeps turning\n" +
                "the chair in 104 to face\n" +
                "the wall: it belongs at\n" +
                "Reception. It is not funny.\n" +
                "It was not funny on\n" +
                "Tuesday either.\n\n" +
                "A.P.",
                new Vector3(-23.985f, f + 1.5f, -1.6f), east, new Vector2(0.3f, 0.26f));
            OfficeKit.Sign(cor, "CORRIDOR 104", new Vector3(-23.985f, f + 2.75f, 4f), east, 0.07f,
                WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter, OfficeKit.M.MetalDark, new Vector2(0.95f, 0.17f));
        }

        /// <summary>
        /// Our side of A2's two wall sockets: the transom window (z 1.2..2.8, sill y 17.6) with a
        /// frame, a shallow sill ledge and a low bench under it for the hop; and the jammed roller
        /// shutter body over the 1.25 m slide gap (z -6..-3). Everything stays at x &lt;= -20.25.
        /// </summary>
        static void A1_Sockets(Transform root)
        {
            var s = new GameObject("Sockets").transform;
            s.SetParent(root, false);
            float f = A1_Floor;
            var t = OfficeLayout.A1_Transom.Opening;
            var sh = OfficeLayout.A1_Shutter.Opening;
            float face = A1_CorridorEastX;

            // Transom: a sill ledge flush with the opening's bottom, a frame around it, a bench and radiator below.
            float sill = t.min.y, head = t.max.y, z0 = t.min.z, z1 = t.max.z;
            OfficeKit.Box(s, "Transom_Ledge", OfficeKit.M.ConcreteLight, new Vector3(face - 0.25f, sill - 0.1f, z0 - 0.15f), new Vector3(face, sill, z1 + 0.15f));
            OfficeKit.Box(s, "Transom_JambS", OfficeKit.M.Frame, new Vector3(face - 0.08f, sill, z0 - 0.12f), new Vector3(face, head + 0.12f, z0));
            OfficeKit.Box(s, "Transom_JambN", OfficeKit.M.Frame, new Vector3(face - 0.08f, sill, z1), new Vector3(face, head + 0.12f, z1 + 0.12f));
            OfficeKit.Box(s, "Transom_Head", OfficeKit.M.Frame, new Vector3(face - 0.08f, head, z0 - 0.12f), new Vector3(face, head + 0.12f, z1 + 0.12f));
            OfficeKit.Box(s, "Bench_Base", OfficeKit.M.Concrete, new Vector3(face - 1.25f, f, z0 - 0.7f), new Vector3(face, f + 0.42f, z1 + 0.7f));
            OfficeKit.Box(s, "Bench_Seat", OfficeKit.M.WoodDark, new Vector3(face - 1.25f, f + 0.42f, z0 - 0.7f), new Vector3(face, f + 0.5f, z1 + 0.7f));
            OfficeKit.Box(s, "Radiator", OfficeKit.M.MetalDark, new Vector3(face - 0.12f, f + 0.6f, z0 - 0.3f), new Vector3(face, f + 1.3f, z1 + 0.3f));

            // Shutter: guide rails outside the opening, the curtain above the 1.25 m gap, a bottom bar, the roll housing.
            float gapTop = sh.max.y, sz0 = sh.min.z, sz1 = sh.max.z, top = A1_CorridorCeiling;
            OfficeKit.Box(s, "Shutter_RailS", OfficeKit.M.MetalDark, new Vector3(face - 0.2f, f, sz0 - 0.12f), new Vector3(face, top, sz0));
            OfficeKit.Box(s, "Shutter_RailN", OfficeKit.M.MetalDark, new Vector3(face - 0.2f, f, sz1), new Vector3(face, top, sz1 + 0.12f));
            OfficeKit.Box(s, "Shutter_BottomBar", OfficeKit.M.MetalDark, new Vector3(face - 0.25f, gapTop, sz0), new Vector3(face, gapTop + 0.12f, sz1));
            OfficeKit.Box(s, "Shutter_Curtain", OfficeKit.M.Metal, new Vector3(face - 0.17f, gapTop + 0.12f, sz0), new Vector3(face - 0.02f, top, sz1));
            for (float y = gapTop + 0.45f; y < top - 0.6f; y += 0.4f)
                OfficeKit.Box(s, "Shutter_Slat", OfficeKit.M.MetalDark, new Vector3(face - 0.18f, y - 0.01f, sz0), new Vector3(face - 0.17f, y + 0.01f, sz1), false);
            OfficeKit.Box(s, "Shutter_Housing", OfficeKit.M.MetalDark, new Vector3(face - 0.45f, top - 0.5f, sz0 - 0.12f), new Vector3(face, top, sz1 + 0.12f));
            OfficeKit.Sign(s,
                "SHUTTER 4E  -  JAMMED\n" +
                "reported  3 Nov   W.B.\n" +
                "reported 10 Nov   W.B.\n" +
                "reported 17 Nov   W.B.\n" +
                "reported 24 Nov   W.B.",
                new Vector3(face - 0.185f, gapTop + 0.55f, (sz0 + sz1) * 0.5f), OfficeKit.Facing(Vector3.left), 0.018f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, new Vector2(0.34f, 0.17f));
        }

        // ------------------------------------------------------------------ light

        /// <summary>
        /// Pools of light with darkness between: one lit ceiling panel over the chairs (with a shaft
        /// in the fog), a warm spot and lamp at the hatch, a faint fill at the back, cold
        /// fluorescents in the corridor (one flickers). Shadow casters: panel, hatch spot, one corridor tube.
        /// </summary>
        static void A1_Lighting(Transform root)
        {
            var l = new GameObject("Lighting").transform;
            l.SetParent(root, false);
            float f = A1_Floor;

            // The one lit panel, just ahead of the spawn so the first view has its pool and shaft.
            OfficeKit.CeilingLight(l, "Panel_Chairs", new Vector3(-35f, A1_RoomCeiling - 0.01f, 0f), new Vector2(1.2f, 1.2f), new Color(0.9f, 0.93f, 1f), 18f, 9f, false, true);
            OfficeKit.LightShaft(l, "Shaft_Chairs", new Vector3(-35f, A1_RoomCeiling - 0.03f, 0f), Quaternion.identity, new Vector3(1.3f, 3.9f, 1.3f), new Color(0.85f, 0.9f, 1f, 0.12f));

            // Warm accent: a spot on the hatch and door 1, and the counter lamp.
            var toHatch = Quaternion.LookRotation(new Vector3(1.7f, -2.7f, 0f).normalized, Vector3.up);
            OfficeKit.SpotLight(l, "Spot_Hatch", new Vector3(-29.6f, A1_RoomCeiling - 0.1f, -1.6f), toHatch, A1_Warm, 12f, 7f, 75f, true);
            OfficeKit.PointLight(l, "Lamp_Counter", new Vector3(-28.15f, f + 1.3f, 0.9f), A1_Warm, 1.5f, 3.5f);

            // The back of the room only just visible.
            OfficeKit.PointLight(l, "Fill_Back", new Vector3(-45f, A1_RoomCeiling - 0.8f, 0f), new Color(0.55f, 0.62f, 0.75f), 1.2f, 9f);

            // Reception office lamp (seen only if door 1 is ever unpaired).
            OfficeKit.PointLight(l, "Lamp_Office", new Vector3(-25.9f, f + 1.2f, -0.6f), A1_Warm, 1f, 3f);

            // Corridor fluorescents along its centre line.
            float[] tubes = { -7.5f, -2.5f, 2.5f, 7.5f };
            foreach (float z in tubes)
                OfficeKit.CeilingLight(l, $"Tube_{z:0.0}", new Vector3(A1_CorridorMidX, A1_CorridorCeiling - 0.01f, z), new Vector2(0.3f, 1.4f), A1_Cool, 9f, 6.5f,
                    flicker: Mathf.Approximately(z, 7.5f), shadows: Mathf.Approximately(z, -2.5f));

            // A little denser, warmer haze in the waiting room so the panel's light hangs in the air.
            OfficeKit.FogZone(l, "A1_WaitingRoomFog",
                new Bounds(new Vector3(-39f, f + 2f, 0f), new Vector3(22f, 4f, 20f)),
                new Color(0.19f, 0.18f, 0.18f), 3f, 40f, 1.5f);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A sheet of paper with text, pinned or taped to a wall.</summary>
        static void A1_Memo(Transform parent, string text, Vector3 position, Quaternion rotation, Vector2 sheet)
        {
            OfficeKit.Sign(parent, text, position, rotation, 0.011f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, sheet);
        }

        /// <summary>A small paper lying flat, face up (ticket stubs, a magazine, a ledger label). Text reads upright to someone facing east.</summary>
        static void A1_Flat(Transform parent, string text, Vector3 position, float letterHeight, Vector2 paper)
        {
            OfficeKit.Sign(parent, text, position, Quaternion.LookRotation(Vector3.up, Vector3.right), letterHeight,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, paper);
        }
    }
}
