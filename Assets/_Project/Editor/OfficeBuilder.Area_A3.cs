using Kit.Gameplay.Signage;
using UnityEngine;

namespace Kit.Editor
{
    /// <summary>
    /// A3 Open Plan (L3, floor y 12): the endless beige cubicle field from Office_01, with the
    /// records passage that teaches K2. Layout, west to east: aisles 31-36, the enclosed records
    /// passage (the missing "aisle 37"), then aisles 38-42. Each aisle has one letterboard
    /// threshold hung over its south mouth (ordinary, a supply of digit tiles). The passage is
    /// closed at both ends by the "37" pair facing into it, so it is endless from the inside; the
    /// only way in is a side door off aisle 36, and the only way out (once a tile is pulled) is
    /// north into the Records Room, which holds brass door 312. Service hatch 412 is in the entry
    /// foyer. Everything is built in world space inside OfficeLayout.A3; every helper, constant
    /// and field here is prefixed A3_ because other areas share this partial class.
    /// </summary>
    public static partial class OfficeBuilder
    {
        // ------------------------------------------------------------------ A3 numbers

        const float A3_Floor = 12f;
        const float A3_Ceiling = 15.3f;           // underside of the acoustic tile ceiling
        const float A3_CeilingTop = 15.6f;
        const float A3_PassageX = 2.7f;           // passage centre line (runs north-south)
        const float A3_PassageSouth = 48f;        // south 37 threshold plane
        const float A3_PassageNorth = 78f;        // north 37 threshold plane
        const float A3_PassageVestibule = 46.6f;  // outer face of the sealed 1 m vestibule behind the south 37
        const float A3_SideDoorZ = 63f;           // the side door from aisle 36, midway along the passage
        const float A3_SignZ = 46.5f;             // aisle signs hang across the aisles' south mouths
        const float A3_FieldLightIntensity = 4f;
        const float A3_FieldLightRange = 11f;
        const int A3_WarmRow = 8;                 // index into A3_RowX: the row west of aisle 41 (opens onto aisle 40)

        static readonly Color A3_Cool = new Color(0.86f, 0.93f, 1f);
        static readonly Color A3_Warm = new Color(1f, 0.72f, 0.45f);

        /// <summary>Aisle sign numbers (37 is the records passage) and each aisle's centre line x.</summary>
        static readonly int[] A3_AisleNumbers = { 31, 32, 33, 34, 35, 36, 38, 39, 40, 41, 42 };
        static readonly float[] A3_AisleX = { -27.85f, -22.45f, -17.05f, -11.65f, -6.25f, -0.85f, 6.25f, 11.65f, 17.05f, 22.45f, 27.85f };

        /// <summary>Cubicle rows (centre x, 2 m deep). West rows open east, east rows open west.</summary>
        static readonly float[] A3_RowX = { -30.55f, -25.15f, -19.75f, -14.35f, -8.95f, -3.55f, 8.95f, 14.35f, 19.75f, 25.15f, 30.55f };

        /// <summary>Workstations per cluster (south, middle, north) for each row; gaps keep the field open.</summary>
        static readonly int[,] A3_RowClusters =
        {
            { 3, 3, 0 }, { 3, 2, 0 }, { 3, 3, 0 }, { 2, 3, 0 }, { 3, 3, 0 }, { 3, 3, 2 },
            { 3, 3, 2 }, { 3, 2, 0 }, { 3, 3, 0 }, { 2, 3, 2 }, { 3, 3, 0 },
        };

        static readonly float[] A3_ClusterStartZ = { 47.6f, 59.6f, 71f };

        /// <summary>Missing ceiling tiles (x0, z0, x1, z1): holes in the ceiling slab with dark plenum above.</summary>
        static readonly Vector4[] A3_MissingTiles =
        {
            new Vector4(-19.5f, 64.2f, -18.9f, 65.4f),
            new Vector4(14.1f, 54.6f, 14.7f, 55.8f),
            new Vector4(-9.9f, 73.8f, -9.3f, 75f),
            new Vector4(26.1f, 45f, 26.7f, 46.2f),
        };

        static readonly string[] A3_Staff =
        {
            "A. PELL", "T. VARGA", "R. ABERNATHY", "H. LINDQVIST", "M. OKONJO", "I. CASTELLANO", "L. SATO",
            "P. HUME", "O. MARSH", "K. IBARRA", "F. MORTIMER", "J. HALLORAN", "A. PENHALIGON", "B. COLE",
            "S. ODUYA", "N. LINDGREN", "E. STRAND",
        };

        // ------------------------------------------------------------------ entry point

        /// <summary>Builds the whole Open Plan under <paramref name="root"/> (world space).</summary>
        static partial void BuildArea_A3(Transform root)
        {
            A3_Shell(A3_Group(root, "Shell"));
            A3_Foyer(A3_Group(root, "Foyer"));
            A3_Field(A3_Group(root, "Field"));
            A3_Passage(A3_Group(root, "RecordsPassage"));
            A3_RecordsRoom(A3_Group(root, "RecordsRoom"));
            A3_GlassOffice(A3_Group(root, "GlassOffice"));
            A3_CoffeeCorner(A3_Group(root, "CoffeeCorner"));
            A3_CeilingLights(A3_Group(root, "CeilingLights"));
            OfficeKit.Zone(root, "A3", OfficeLayout.A3);
        }

        // ------------------------------------------------------------------ helpers

        static Transform A3_Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>A paper note or card: dark text on a paper backing.</summary>
        static void A3_Note(Transform parent, string text, Vector3 position, Quaternion rotation, float letter, Vector2 size)
        {
            OfficeKit.Sign(parent, text, position, rotation, letter, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, OfficeKit.M.Paper, size);
        }

        /// <summary>World point of <paramref name="local"/> in a frame at <paramref name="origin"/> turned <paramref name="yaw"/> degrees.</summary>
        static Vector3 A3_Local(Vector3 origin, float yaw, Vector3 local) => origin + Quaternion.Euler(0f, yaw, 0f) * local;

        /// <summary>
        /// A name card clipped to the top of a cubicle's side partition at its open end, facing
        /// the aisle. Assumes workstation props open toward their local +Z.
        /// </summary>
        static void A3_Nameplate(Transform parent, Vector3 cubicle, float yaw, string text)
        {
            A3_Note(parent, text, A3_Local(cubicle, yaw, new Vector3(0.8f, 1.66f, 0.96f)), Quaternion.Euler(0f, yaw, 0f), 0.022f, new Vector2(0.36f, 0.07f));
        }

        static bool A3_NoTroffer(float x, float z)
        {
            if (x > -5.5f && x < 5.5f && z < 37.5f) return true;                 // foyer has its own lights
            if (x > 0.4f && x < 5f && z > 46f && z < 78.3f) return true;          // records passage
            if (x > -3.8f && x < 9.2f && z > 77.5f) return true;                  // records room
            if (x > 17f && z > 77f) return true;                                  // dark hall around the glass office
            return false;
        }

        // ------------------------------------------------------------------ shell

        /// <summary>Floor, ceiling (with missing tiles), outer walls, columns and wall posters.</summary>
        static void A3_Shell(Transform g)
        {
            OfficeKit.Box(g, "Floor", OfficeKit.M.CarpetGrey, new Vector3(-35f, A3_Floor - 0.4f, 30f), new Vector3(35f, A3_Floor, 90f));
            OfficeKit.Slab(g, "Ceiling", OfficeKit.M.CeilingTile, new Vector3(-35f, 0f, 30f), new Vector3(35f, 0f, 90f),
                A3_CeilingTop, A3_CeilingTop - A3_Ceiling, A3_MissingTiles);

            // Outer walls. The atrium owns the z = 30 wall between x -20 and 20 (with the entry cut in it).
            const string wall = OfficeKit.M.Concrete;
            OfficeKit.Box(g, "Wall_West", wall, new Vector3(-35f, A3_Floor, 30f), new Vector3(-34.6f, A3_CeilingTop, 90f));
            OfficeKit.Box(g, "Wall_East", wall, new Vector3(34.6f, A3_Floor, 30f), new Vector3(35f, A3_CeilingTop, 90f));
            OfficeKit.Box(g, "Wall_North", wall, new Vector3(-34.6f, A3_Floor, 89.6f), new Vector3(34.6f, A3_CeilingTop, 90f));
            OfficeKit.Box(g, "Wall_South_W", wall, new Vector3(-34.6f, A3_Floor, 30f), new Vector3(-19.75f, A3_CeilingTop, 30.25f));
            OfficeKit.Box(g, "Wall_South_E", wall, new Vector3(19.75f, A3_Floor, 30f), new Vector3(34.6f, A3_CeilingTop, 30.25f));

            // Missing tiles: a dark plenum above each hole; one tile hangs from its hole, one lies on the carpet.
            for (int i = 0; i < A3_MissingTiles.Length; i++)
            {
                var h = A3_MissingTiles[i];
                OfficeKit.Box(g, $"Plenum_{i}", OfficeKit.M.ConcreteDark, new Vector3(h.x - 0.3f, A3_CeilingTop, h.y - 0.3f),
                    new Vector3(h.z + 0.3f, A3_CeilingTop + 0.35f, h.w + 0.3f), false);
            }
            var hang = A3_MissingTiles[3];
            OfficeKit.BoxAt(g, "HangingTile", OfficeKit.M.CeilingTile,
                new Vector3((hang.x + hang.z) * 0.5f, A3_Ceiling - 0.6f * Mathf.Sin(60f * Mathf.Deg2Rad), hang.y + 0.6f * Mathf.Cos(60f * Mathf.Deg2Rad)),
                new Vector3(0.6f, 0.02f, 1.2f), Quaternion.Euler(60f, 0f, 0f), false);
            var hang2 = A3_MissingTiles[0];
            OfficeKit.BoxAt(g, "HangingTile_2", OfficeKit.M.CeilingTile,
                new Vector3((hang2.x + hang2.z) * 0.5f, A3_Ceiling - 0.6f * Mathf.Sin(35f * Mathf.Deg2Rad), hang2.w - 0.6f * Mathf.Cos(35f * Mathf.Deg2Rad)),
                new Vector3(0.6f, 0.02f, 1.2f), Quaternion.Euler(-35f, 0f, 0f), false);
            OfficeKit.BoxAt(g, "FallenTile", OfficeKit.M.CeilingTile, new Vector3(14.7f, A3_Floor + 0.02f, 55.1f),
                new Vector3(0.6f, 0.02f, 1.2f), Quaternion.Euler(0f, 23f, 3f), false);
            OfficeKit.BoxAt(g, "FallenTile_Shard", OfficeKit.M.CeilingTile, new Vector3(13.9f, A3_Floor + 0.012f, 56.2f),
                new Vector3(0.28f, 0.02f, 0.34f), Quaternion.Euler(0f, -40f, 0f), false);

            // Structural columns, standing in the row lines (in the gaps between clusters) and in the cross aisle.
            float[] columnX = { -30.55f, -19.75f, -8.95f, 14.35f, 25.15f };
            float[] columnZ = { 42f, 57f, 69f };
            foreach (float cx in columnX)
                foreach (float cz in columnZ)
                    OfficeKit.Box(g, $"Column_{cx:0}_{cz:0}", OfficeKit.M.Concrete, new Vector3(cx - 0.35f, A3_Floor, cz - 0.35f), new Vector3(cx + 0.35f, A3_Ceiling, cz + 0.35f));

            // Company posters on the long walls, kept flat so the walls stay runnable.
            var faceEast = OfficeKit.Facing(Vector3.right);
            var faceWest = OfficeKit.Facing(Vector3.left);
            A3_Note(g, "ACCURACY\nDISCRETION\nCONTINUITY", new Vector3(-34.58f, 13.9f, 55f), faceEast, 0.09f, new Vector2(1f, 1.3f));
            A3_Note(g, "EVERY FILE\nIN ITS PLACE", new Vector3(-34.58f, 13.9f, 72f), faceEast, 0.09f, new Vector2(1f, 1.3f));
            A3_Note(g, "A COMPANY\nIS ITS RECORDS\n\nHALE & ORMSBY\n1921", new Vector3(34.58f, 13.9f, 62f), faceWest, 0.08f, new Vector2(1.1f, 1.4f));
        }

        // ------------------------------------------------------------------ foyer and hatch 412

        /// <summary>
        /// The entry foyer (x -5..5, z 30.25..37): wood walls, an unattended reception desk, the
        /// Facilities corkboard, and service hatch 412 low in the west wall with a stand-up duct
        /// behind it. The north wall frames the first view down aisle 36.
        /// </summary>
        static void A3_Foyer(Transform g)
        {
            const string wood = OfficeKit.M.WoodVeneer;
            float top = A3_Ceiling;

            OfficeKit.Box(g, "FloorSlate", OfficeKit.M.Slate, new Vector3(-4.9f, A3_Floor, 30.25f), new Vector3(4.9f, A3_Floor + 0.01f, 36.9f), false);

            // West wall with the hatch: 2 x 1.25 m opening at floor level, centred z 34.
            OfficeKit.Wall(g, "Wall_W", wood, new Vector3(-5f, 0f, 30.25f), new Vector3(-5f, 0f, 37f), A3_Floor, top - A3_Floor, 0.2f,
                new OfficeKit.Opening(34f - 1f - 30.25f, 2f, 0f, 1.25f));
            OfficeKit.Box(g, "Wall_E", wood, new Vector3(4.9f, A3_Floor, 30.25f), new Vector3(5.1f, top, 37f));
            OfficeKit.Wall(g, "Wall_N", wood, new Vector3(-5.1f, 0f, 37f), new Vector3(5.1f, 0f, 37f), A3_Floor, top - A3_Floor, 0.2f,
                new OfficeKit.Opening(3.1f, 4f, 0f, 3f));

            OfficeKit.Threshold(g, "Hatch_412", new Vector3(-5f, A3_Floor + 0.625f, 34f), OfficeKit.Facing(Vector3.right), new Vector2(2f, 1.25f),
                "412", null, false, true, OfficeKit.M.Metal);

            // Service duct behind the hatch: 2.1 m deep, tall enough to stand up and slide back out.
            var duct = A3_Group(g, "ServiceDuct");
            const string metal = OfficeKit.M.MetalDark;
            OfficeKit.Box(duct, "Back", metal, new Vector3(-7.4f, A3_Floor, 32.6f), new Vector3(-7.2f, 14.6f, 35.4f));
            OfficeKit.Box(duct, "South", metal, new Vector3(-7.4f, A3_Floor, 32.6f), new Vector3(-5.1f, 14.6f, 32.8f));
            OfficeKit.Box(duct, "North", metal, new Vector3(-7.4f, A3_Floor, 35.2f), new Vector3(-5.1f, 14.6f, 35.4f));
            OfficeKit.Box(duct, "Roof", metal, new Vector3(-7.4f, 14.4f, 32.6f), new Vector3(-5.1f, 14.6f, 35.4f));
            OfficeKit.Box(duct, "FloorPlate", OfficeKit.M.Metal, new Vector3(-7.2f, A3_Floor, 32.8f), new Vector3(-5.1f, A3_Floor + 0.01f, 35.2f), false);
            OfficeKit.Box(duct, "Pipe_A", OfficeKit.M.Metal, new Vector3(-7.2f, 14.1f, 33f), new Vector3(-5.2f, 14.22f, 33.12f), false);
            OfficeKit.Box(duct, "Pipe_B", OfficeKit.M.Metal, new Vector3(-7.2f, 13.9f, 33.3f), new Vector3(-5.2f, 14.0f, 33.4f), false);

            // Reception: unattended, against the east wall, clear of the run-up to the hatch.
            OfficeKit.Prop(g, OfficeKit.PropIds.ReceptionDesk, new Vector3(3.6f, A3_Floor, 31.8f), 270f);
            OfficeKit.Prop(g, OfficeKit.PropIds.SwivelChair, new Vector3(4.45f, A3_Floor, 31.5f), 250f);
            OfficeKit.Prop(g, OfficeKit.PropIds.Telephone, new Vector3(3.55f, A3_Floor + 1.1f, 31.0f), 260f);
            OfficeKit.BoxAt(g, "FlipClock", OfficeKit.M.MetalDark, new Vector3(3.45f, A3_Floor + 1.16f, 32.7f), new Vector3(0.08f, 0.12f, 0.22f), Quaternion.identity, false);
            OfficeKit.Sign(g, "4:47", new Vector3(3.405f, A3_Floor + 1.16f, 32.7f), OfficeKit.Facing(Vector3.left), 0.05f, WorldTextStyle.Glow);
            A3_Note(g, "RECEPTION UNATTENDED\n4:45 - 5:00", new Vector3(3.42f, A3_Floor + 1.17f, 31.6f), OfficeKit.Facing(Vector3.left) * Quaternion.Euler(-12f, 0f, 0f),
                0.018f, new Vector2(0.3f, 0.1f));
            OfficeKit.Prop(g, OfficeKit.PropIds.TicketDispenser, new Vector3(-2.7f, A3_Floor, 31.2f), 90f);
            OfficeKit.Prop(g, OfficeKit.PropIds.Planter, new Vector3(-4.45f, A3_Floor, 31.5f), 90f);
            OfficeKit.Prop(g, OfficeKit.PropIds.PottedPlant, new Vector3(4.4f, A3_Floor, 36.4f));
            OfficeKit.Prop(g, OfficeKit.PropIds.PottedPlant, new Vector3(-4.4f, A3_Floor, 36.4f));

            // Company lettering beside the opening, facing whoever comes in from the atrium.
            var faceSouth = OfficeKit.Facing(Vector3.back);
            OfficeKit.BoxAt(g, "NamePanel", OfficeKit.M.WoodDark, new Vector3(-3.55f, 13.95f, 36.88f), new Vector3(2.6f, 0.9f, 0.02f), Quaternion.identity, false);
            OfficeKit.Sign(g, "HALE & ORMSBY", new Vector3(-3.55f, 14.1f, 36.865f), faceSouth, 0.13f, WorldTextStyle.Brass);
            OfficeKit.Sign(g, "RECORDS & CONTINUITY  .  THREE", new Vector3(-3.55f, 13.75f, 36.865f), faceSouth, 0.045f, WorldTextStyle.Brass);

            // Facilities corkboard on the east wall: the letter-O memo (K4 hint) and the coffee rota.
            var faceWest = OfficeKit.Facing(Vector3.left);
            OfficeKit.Prop(g, OfficeKit.PropIds.Corkboard, new Vector3(4.88f, 13.0f, 35.4f), 270f);
            A3_Note(g, "FACILITIES - SIGN LETTERS\n\nThe supplier has again sent capital O's\nin place of zeros. We have checked, and\nas far as the building is concerned\nthey are the same thing. They will be\nused as zeros until further notice.\n\nW. Brisk, Facilities",
                new Vector3(4.85f, 13.45f, 35.62f), faceWest, 0.016f, new Vector2(0.5f, 0.42f));
            A3_Note(g, "COFFEE ROTA - THREE\n\nMon   A. Pell\nTue   T. Varga\nWed   R. Abernathy\nThu   H. Lindqvist\nFri   ____________",
                new Vector3(4.85f, 13.42f, 35.0f), faceWest, 0.017f, new Vector2(0.3f, 0.36f));

            OfficeKit.CeilingLight(g, "FoyerLight_S", new Vector3(0f, A3_Ceiling - 0.02f, 32.4f), new Vector2(0.6f, 1.2f), A3_Cool, 3f, 8f);
            OfficeKit.CeilingLight(g, "FoyerLight_N", new Vector3(0f, A3_Ceiling - 0.02f, 35.6f), new Vector2(0.6f, 1.2f), A3_Cool, 3f, 8f);
        }

        // ------------------------------------------------------------------ the cubicle field

        /// <summary>
        /// Aisle-sign thresholds, cubicle rows, the warm desk, planters, stray chairs, and the
        /// field's notices (seating chart, water cooler, the poster at the end of aisle 36).
        /// </summary>
        static void A3_Field(Transform g)
        {
            // Aisle signs: letterboard thresholds hung on light gantries across each aisle mouth, front facing south.
            var signs = A3_Group(g, "AisleSigns");
            for (int i = 0; i < A3_AisleNumbers.Length; i++)
            {
                float x = A3_AisleX[i];
                string n = A3_AisleNumbers[i].ToString();
                OfficeKit.Threshold(signs, "AisleSign_" + n, new Vector3(x, A3_Floor + 1.2f, A3_SignZ), OfficeKit.Facing(Vector3.back), new Vector2(2f, 2.4f),
                    null, n, false, false, OfficeKit.M.MetalDark);
                for (int side = -1; side <= 1; side += 2)
                    OfficeKit.Box(signs, $"Hanger_{n}_{(side < 0 ? "L" : "R")}", OfficeKit.M.MetalDark,
                        new Vector3(x + side * 1.06f - 0.02f, A3_Floor + 2.5f, A3_SignZ - 0.02f), new Vector3(x + side * 1.06f + 0.02f, A3_Ceiling, A3_SignZ + 0.02f), false);
            }

            // Cubicle rows, with name cards on every fourth desk. Desk 3-114 is the warm desk.
            var rows = A3_Group(g, "Cubicles");
            int counter = 0, nameIndex = 0;
            for (int r = 0; r < A3_RowX.Length; r++)
            {
                float x = A3_RowX[r];
                float yaw = x < A3_PassageX ? 90f : 270f;
                for (int c = 0; c < 3; c++)
                {
                    for (int k = 0; k < A3_RowClusters[r, c]; k++)
                    {
                        var pos = new Vector3(x, A3_Floor, A3_ClusterStartZ[c] + 1f + 2f * k);
                        if (r == A3_WarmRow && c == 1 && k == 1)
                        {
                            A3_WarmDesk(g, pos, yaw);
                            continue;
                        }
                        OfficeKit.Prop(rows, OfficeKit.PropIds.CubicleWorkstation, pos, yaw);
                        if (counter % 4 == 0)
                        {
                            int desk = 101 + counter;
                            if (desk >= 114) desk++;
                            A3_Nameplate(rows, pos, yaw, $"DESK 3-{desk}: {A3_Staff[nameIndex++ % A3_Staff.Length]}");
                        }
                        counter++;
                    }
                }
                // Philodendron troughs at the south end of each row, except beside the passage.
                if (r != 5 && r != 6)
                    OfficeKit.Prop(g, OfficeKit.PropIds.Planter, new Vector3(x, A3_Floor, 46.4f));
            }

            // The one grey swivel chair left in the aisle (Office_01), and another drifted into aisle 33.
            OfficeKit.Prop(g, OfficeKit.PropIds.SwivelChair, new Vector3(-0.6f, A3_Floor, 55.3f), 200f);
            OfficeKit.Prop(g, OfficeKit.PropIds.SwivelChair, new Vector3(-16.7f, A3_Floor, 67.4f), 75f);

            // Water cooler (the blue accent) and the seating chart, either side of the foyer opening.
            OfficeKit.Prop(g, OfficeKit.PropIds.WaterCooler, new Vector3(3.6f, A3_Floor, 37.45f));
            OfficeKit.Prop(g, OfficeKit.PropIds.Bin, new Vector3(4.2f, A3_Floor, 37.4f));
            OfficeKit.Prop(g, OfficeKit.PropIds.Whiteboard, new Vector3(-3.5f, 12.9f, 37.13f));
            OfficeKit.Sign(g, "THREE - SEATING (rev. 14)\n\n31  Pell . Varga . Sato\n32  Abernathy . Hume\n33  Lindqvist . Marsh\n34  Okonjo . Ibarra\n35  Castellano . Cole\n36  Mortimer . Halloran\n37  RECORDS (T. Vane)\n38  Penhaligon . Oduya\n39  Lindgren . Strand\n40  3-114  ____________\n41  (vacant)\n42  (vacant)",
                new Vector3(-3.5f, 13.5f, 37.17f), OfficeKit.Facing(Vector3.forward), 0.042f);

            // South band: filing cabinets along the south wall, a retirement card, a broken copier, a coat rack.
            for (int k = 0; k < 13; k++)
                OfficeKit.Prop(g, OfficeKit.PropIds.FilingCabinet, new Vector3(-33f + 0.55f * k, A3_Floor, 30.58f));
            A3_Note(g, "RUTH\n25 YEARS!\nfrom everyone on Three", new Vector3(-29.7f, A3_Floor + 1.38f, 30.6f),
                OfficeKit.Facing(Vector3.forward) * Quaternion.Euler(-10f, 0f, 0f), 0.024f, new Vector2(0.24f, 0.16f));
            OfficeKit.Box(g, "Copier", OfficeKit.M.Plastic, new Vector3(7.5f, A3_Floor, 30.4f), new Vector3(8.6f, A3_Floor + 1.1f, 31.2f));
            A3_Note(g, "OUT OF ORDER\n(again)  G.F.", new Vector3(8.05f, A3_Floor + 0.95f, 31.212f), OfficeKit.Facing(Vector3.forward), 0.02f, new Vector2(0.26f, 0.12f));
            OfficeKit.Prop(g, OfficeKit.PropIds.CoatRack, new Vector3(-6.1f, A3_Floor, 38f));

            // The end of the view down aisle 36 from the entry: a poster on the records room's south wall.
            var faceSouth = OfficeKit.Facing(Vector3.back);
            OfficeKit.Sign(g, "CONTINUITY\nIS EVERYONE'S\nRESPONSIBILITY", new Vector3(-0.85f, 14.0f, 77.88f), faceSouth, 0.11f,
                WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter, OfficeKit.M.ChairOrange, new Vector2(1.5f, 1.9f));
            OfficeKit.Sign(g, "HALE & ORMSBY", new Vector3(-0.85f, 13.25f, 77.875f), faceSouth, 0.045f, WorldTextStyle.LightOnDark);
        }

        /// <summary>
        /// Desk 3-114, built from parts so its contents sit exactly: lamp on, cardigan over the
        /// pushed-back chair, a mug, a CRT showing a cursor, Friday's calendar and a nameplate
        /// with no name. Opens toward local +Z like the workstation props.
        /// </summary>
        static void A3_WarmDesk(Transform parent, Vector3 o, float yaw)
        {
            var g = A3_Group(parent, "WarmDesk_3-114");
            var rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 L(float x, float y, float z) => o + rot * new Vector3(x, y, z);

            OfficeKit.Prop(g, OfficeKit.PropIds.CubiclePartition, L(0f, 0f, -0.97f), yaw);
            OfficeKit.Prop(g, OfficeKit.PropIds.CubiclePartition, L(-0.97f, 0f, 0f), yaw + 90f);
            OfficeKit.Prop(g, OfficeKit.PropIds.CubiclePartition, L(0.97f, 0f, 0f), yaw + 90f);
            OfficeKit.Prop(g, OfficeKit.PropIds.Desk, L(0f, 0f, -0.6f), yaw);
            OfficeKit.Prop(g, OfficeKit.PropIds.CrtMonitor, L(0f, 0.74f, -0.72f), yaw);
            OfficeKit.Prop(g, OfficeKit.PropIds.Keyboard, L(0f, 0.74f, -0.36f), yaw + 4f);
            OfficeKit.Prop(g, OfficeKit.PropIds.DeskLamp, L(0.5f, 0.74f, -0.75f), yaw - 25f);
            OfficeKit.Prop(g, OfficeKit.PropIds.PaperStack, L(-0.25f, 0.74f, -0.8f), yaw + 8f);
            OfficeKit.BoxAt(g, "Mug", OfficeKit.M.TileWhite, L(-0.48f, 0.79f, -0.42f), new Vector3(0.08f, 0.1f, 0.08f), rot, false);
            OfficeKit.BoxAt(g, "Mug_Coffee", OfficeKit.M.WoodDark, L(-0.48f, 0.835f, -0.42f), new Vector3(0.065f, 0.005f, 0.065f), rot, false);

            // The chair is pushed back and turned, as if someone just stood up; the cardigan hangs on it.
            var chairRot = rot * Quaternion.Euler(0f, 160f, 0f);
            var chairPos = L(0.1f, 0f, 0.05f);
            OfficeKit.Prop(g, OfficeKit.PropIds.SwivelChair, chairPos, yaw + 160f);
            OfficeKit.BoxAt(g, "Cardigan", OfficeKit.M.ChairOrange, chairPos + chairRot * new Vector3(0f, 0.8f, -0.28f), new Vector3(0.44f, 0.42f, 0.05f),
                chairRot * Quaternion.Euler(-10f, 0f, 0f), false);
            OfficeKit.BoxAt(g, "Cardigan_Sleeve", OfficeKit.M.ChairOrange, chairPos + chairRot * new Vector3(0.25f, 0.55f, -0.24f), new Vector3(0.09f, 0.42f, 0.08f),
                chairRot * Quaternion.Euler(0f, 0f, 8f), false);

            OfficeKit.Sign(g, "_", L(-0.1f, 1.0f, -0.505f), rot, 0.05f, WorldTextStyle.Glow);
            A3_Note(g, "Fri.\nfinish the 300s\nbefore they\nlock up", L(0.62f, 1.28f, -0.935f), rot, 0.013f, new Vector2(0.13f, 0.12f));
            A3_Note(g, "FRI\n13\nMAY", L(-0.62f, 0.83f, -0.82f), rot * Quaternion.Euler(-15f, 0f, 0f), 0.028f, new Vector2(0.13f, 0.16f));
            A3_Nameplate(g, o, yaw, "DESK 3-114:");

            OfficeKit.SpotLight(g, "LampLight", L(0.42f, 1.15f, -0.62f), Quaternion.LookRotation(rot * new Vector3(-0.2f, -1f, 0.35f)), A3_Warm, 2.5f, 5f, 110f, true);
            OfficeKit.PointLight(g, "LampGlow", L(0.3f, 1.45f, -0.3f), A3_Warm, 0.6f, 6f);
        }

        // ------------------------------------------------------------------ the records passage (37 / 37)

        /// <summary>
        /// The enclosed records passage (interior x 1.5..3.9 between shelf faces, z 48..78, 3.15 m
        /// high). Both ends are letterboard "37" thresholds facing into it, so from inside it runs
        /// on forever. The south threshold's back opens only into a sealed 1 m vestibule. The way
        /// in is the side door off aisle 36 at z 63.
        /// </summary>
        static void A3_Passage(Transform g)
        {
            float px = A3_PassageX, s = A3_PassageSouth, n = A3_PassageNorth;
            float inW = px - 1.2f, inE = px + 1.2f;       // shelf faces: 1.5 .. 3.9
            float outW = inW - 0.65f, outE = inE + 0.65f; // outer wall faces: 0.85 .. 4.55
            float doorS = A3_SideDoorZ - 0.6f, doorN = A3_SideDoorZ + 0.6f;
            float height = A3_Ceiling - A3_Floor;
            const float innerCeiling = 15.15f;           // above the 37 plates (which top out near 15.02)

            OfficeKit.Wall(g, "Wall_W", OfficeKit.M.Concrete, new Vector3(outW + 0.075f, 0f, A3_PassageVestibule), new Vector3(outW + 0.075f, 0f, n - 0.1f),
                A3_Floor, height, 0.15f, new OfficeKit.Opening(doorS - A3_PassageVestibule, doorN - doorS, 0f, 2.2f));
            OfficeKit.Box(g, "Wall_E", OfficeKit.M.Concrete, new Vector3(outE - 0.15f, A3_Floor, A3_PassageVestibule), new Vector3(outE, A3_Ceiling, n - 0.1f));
            OfficeKit.Box(g, "Vestibule_S", OfficeKit.M.Concrete, new Vector3(outW, A3_Floor, A3_PassageVestibule), new Vector3(outE, A3_Ceiling, A3_PassageVestibule + 0.2f));
            OfficeKit.Wall(g, "EndWall_S", OfficeKit.M.ConcreteDark, new Vector3(outW, 0f, s), new Vector3(outE, 0f, s), A3_Floor, height, 0.2f,
                new OfficeKit.Opening(inW - outW, 2.4f, 0f, 2.6f));
            OfficeKit.Wall(g, "EndWall_N", OfficeKit.M.ConcreteDark, new Vector3(outW, 0f, n), new Vector3(outE, 0f, n), A3_Floor, height, 0.2f,
                new OfficeKit.Opening(inW - outW, 2.4f, 0f, 2.6f));
            OfficeKit.Box(g, "InnerCeiling", OfficeKit.M.ConcreteDark, new Vector3(outW + 0.15f, innerCeiling, A3_PassageVestibule + 0.2f), new Vector3(outE - 0.15f, A3_Ceiling, n - 0.1f));
            OfficeKit.Box(g, "FloorConcrete", OfficeKit.M.ConcreteFloor, new Vector3(outW + 0.15f, A3_Floor, A3_PassageVestibule + 0.2f), new Vector3(outE - 0.15f, A3_Floor + 0.01f, n - 0.1f), false);

            // The pair: same size and sill, fronts facing into the passage.
            var size = new Vector2(2.4f, 2.6f);
            OfficeKit.Threshold(g, "Passage37_South", new Vector3(px, A3_Floor + 1.3f, s), OfficeKit.Facing(Vector3.forward), size, null, "37", false, false, OfficeKit.M.WoodDark);
            OfficeKit.Threshold(g, "Passage37_North", new Vector3(px, A3_Floor + 1.3f, n), OfficeKit.Facing(Vector3.back), size, null, "37", false, false, OfficeKit.M.WoodDark);

            // Floor-to-ceiling records shelving down both sides; a gap on the west side for the door.
            const int shelfCount = 15;
            float pitch = (n - s - 0.3f) / shelfCount;
            float first = s + 0.15f + pitch * 0.5f;
            float gapS = s, gapN = n;
            for (int k = 0; k < shelfCount; k++)
            {
                float z = first + pitch * k;
                OfficeKit.Prop(g, OfficeKit.PropIds.ArchiveShelf, new Vector3(inE + 0.25f, A3_Floor, z), 270f);
                bool atDoor = z + pitch * 0.5f > doorS && z - pitch * 0.5f < doorN;
                if (atDoor) continue;
                OfficeKit.Prop(g, OfficeKit.PropIds.ArchiveShelf, new Vector3(inW - 0.25f, A3_Floor, z), 90f);
                if (z < A3_SideDoorZ) gapS = Mathf.Max(gapS, z + pitch * 0.5f);
                else gapN = Mathf.Min(gapN, z - pitch * 0.5f);
            }

            // A deep wooden reveal where the side door cuts through the shelving.
            OfficeKit.Box(g, "DoorReveal_S", OfficeKit.M.WoodDark, new Vector3(outW + 0.15f, A3_Floor, gapS), new Vector3(inW, innerCeiling, doorS));
            OfficeKit.Box(g, "DoorReveal_N", OfficeKit.M.WoodDark, new Vector3(outW + 0.15f, A3_Floor, doorN), new Vector3(inW, innerCeiling, gapN));
            OfficeKit.Box(g, "DoorReveal_Head", OfficeKit.M.WoodDark, new Vector3(outW + 0.15f, A3_Floor + 2.2f, doorS), new Vector3(inW, innerCeiling, doorN));

            // Year labels on the west shelves: walking north they run 1968 to 1977, then 1968 again.
            string[] years = { "1968", "1971", "1974", "1977" };
            float[] yearZ = { s + 2f, s + 8f, s + 20f, s + 26f };
            for (int i = 0; i < years.Length; i++)
                A3_Note(g, years[i], new Vector3(inW + 0.01f, 13.9f, yearZ[i]), OfficeKit.Facing(Vector3.right), 0.05f, new Vector2(0.24f, 0.09f));

            // Dim cool lights every 10 m, so the rhythm survives the loop seam.
            for (int i = 0; i < 3; i++)
                OfficeKit.CeilingLight(g, $"PassageLight_{i}", new Vector3(px, innerCeiling - 0.02f, s + 5f + 10f * i), new Vector2(0.3f, 1.2f), A3_Cool, 1.6f, 6f);
            var fogBounds = new Bounds(new Vector3(px, (A3_Floor + innerCeiling) * 0.5f, (A3_PassageVestibule + n) * 0.5f),
                new Vector3(outE - outW - 0.3f, innerCeiling - A3_Floor, n - A3_PassageVestibule));
            OfficeKit.FogZone(g, "A3_RecordsPassage", fogBounds, new Color(0.12f, 0.14f, 0.16f), 1.5f, 26f, 1f);

            // Outside: the staff-only sign over the side door, and a typed card on the blind south end.
            OfficeKit.Sign(g, "RECORDS: STAFF ONLY", new Vector3(outW - 0.01f, 14.55f, A3_SideDoorZ), OfficeKit.Facing(Vector3.left), 0.06f,
                WorldTextStyle.LightOnDark, TextAnchor.MiddleCenter, OfficeKit.M.MetalDark, new Vector2(1.05f, 0.17f));
            A3_Note(g, "RECORDS STORE\nentrance halfway along aisle 36", new Vector3(px, 13.6f, A3_PassageVestibule - 0.02f), OfficeKit.Facing(Vector3.back),
                0.04f, new Vector2(0.95f, 0.22f));
        }

        // ------------------------------------------------------------------ the records room and door 312

        /// <summary>
        /// The Records Room (interior x -3.2..8.6, z 78.1..87.5), reachable only out of the
        /// passage's north end once the 37 pair is broken. Dim, one warm desk lamp, filing
        /// cabinets and the strongest notes. Door 312 is in its north wall with a closet behind.
        /// </summary>
        static void A3_RecordsRoom(Transform g)
        {
            const float w = -3.3f, e = 8.7f, s = 78f, north = 87.6f;
            float px = A3_PassageX;
            const string wall = OfficeKit.M.Concrete, wood = OfficeKit.M.WoodVeneer;

            OfficeKit.Box(g, "Wall_W", wall, new Vector3(w - 0.1f, A3_Floor, s + 0.1f), new Vector3(w + 0.1f, A3_Ceiling, 89.6f));
            OfficeKit.Box(g, "Wall_E", wall, new Vector3(e - 0.1f, A3_Floor, s + 0.1f), new Vector3(e + 0.1f, A3_Ceiling, 89.6f));
            OfficeKit.Box(g, "Wall_S_W", wall, new Vector3(w - 0.1f, A3_Floor, s - 0.1f), new Vector3(px - 1.85f, A3_Ceiling, s + 0.1f));
            OfficeKit.Box(g, "Wall_S_E", wall, new Vector3(px + 1.85f, A3_Floor, s - 0.1f), new Vector3(e + 0.1f, A3_Ceiling, s + 0.1f));
            float doorLeft = px - OfficeLayout.Door.x * 0.5f;
            OfficeKit.Wall(g, "Wall_N", wall, new Vector3(w + 0.1f, 0f, north), new Vector3(e - 0.1f, 0f, north), A3_Floor, A3_Ceiling - A3_Floor, 0.2f,
                OfficeKit.Opening.Door(doorLeft - (w + 0.1f)));

            // Wood lining inside (the outside stays concrete, like the passage).
            OfficeKit.Box(g, "Lining_W", wood, new Vector3(w + 0.1f, A3_Floor, s + 0.1f), new Vector3(w + 0.12f, A3_Ceiling, north - 0.1f), false);
            OfficeKit.Box(g, "Lining_E", wood, new Vector3(e - 0.12f, A3_Floor, s + 0.1f), new Vector3(e - 0.1f, A3_Ceiling, north - 0.1f), false);
            OfficeKit.Box(g, "Lining_S_W", wood, new Vector3(w + 0.1f, A3_Floor, s + 0.1f), new Vector3(px - 1.85f, A3_Ceiling, s + 0.12f), false);
            OfficeKit.Box(g, "Lining_S_E", wood, new Vector3(px + 1.85f, A3_Floor, s + 0.1f), new Vector3(e - 0.1f, A3_Ceiling, s + 0.12f), false);
            OfficeKit.Box(g, "Lining_N_W", wood, new Vector3(w + 0.1f, A3_Floor, north - 0.12f), new Vector3(doorLeft - 0.12f, A3_Ceiling, north - 0.1f), false);
            OfficeKit.Box(g, "Lining_N_E", wood, new Vector3(doorLeft + OfficeLayout.Door.x + 0.12f, A3_Floor, north - 0.12f), new Vector3(e - 0.1f, A3_Ceiling, north - 0.1f), false);
            OfficeKit.Box(g, "FloorCarpet", OfficeKit.M.CarpetOxblood, new Vector3(w + 0.1f, A3_Floor, s + 0.1f), new Vector3(e - 0.1f, A3_Floor + 0.01f, north - 0.1f), false);

            // Door 312 (brass), front facing into the room, and the closet behind it (1.9 m deep).
            OfficeKit.Threshold(g, "Door_312", new Vector3(px, A3_Floor + OfficeLayout.Door.y * 0.5f, north), OfficeKit.Facing(Vector3.back), OfficeLayout.Door,
                "312", null, false, false, OfficeKit.M.WoodDark);
            OfficeKit.Box(g, "Closet_W", wall, new Vector3(px - 1.4f, A3_Floor, north + 0.1f), new Vector3(px - 1.2f, A3_Ceiling, 89.6f));
            OfficeKit.Box(g, "Closet_E", wall, new Vector3(px + 1.2f, A3_Floor, north + 0.1f), new Vector3(px + 1.4f, A3_Ceiling, 89.6f));
            OfficeKit.Prop(g, OfficeKit.PropIds.Bin, new Vector3(px + 0.8f, A3_Floor, 89.3f));
            OfficeKit.Prop(g, OfficeKit.PropIds.PaperStack, new Vector3(px - 0.8f, A3_Floor, 89.3f), 15f);

            // Filing cabinets along the west wall and the south-east wall; shelving in the east half.
            for (int k = 0; k < 14; k++)
                OfficeKit.Prop(g, OfficeKit.PropIds.FilingCabinet, new Vector3(w + 0.425f, A3_Floor, 78.9f + 0.55f * k), 90f);
            for (int k = 0; k < 6; k++)
                OfficeKit.Prop(g, OfficeKit.PropIds.FilingCabinet, new Vector3(5f + 0.55f * k, A3_Floor, s + 0.45f));
            for (int k = 0; k < 2; k++)
            {
                OfficeKit.Prop(g, OfficeKit.PropIds.ArchiveShelf, new Vector3(5.55f + 2f * k, A3_Floor, 81.3f), 180f);
                OfficeKit.Prop(g, OfficeKit.PropIds.ArchiveShelf, new Vector3(5.55f + 2f * k, A3_Floor, 84.6f));
            }

            // The reading desk, its lamp the only warm light here.
            OfficeKit.Prop(g, OfficeKit.PropIds.Desk, new Vector3(-0.6f, A3_Floor, 83.8f), 90f);
            OfficeKit.Prop(g, OfficeKit.PropIds.SwivelChair, new Vector3(0.2f, A3_Floor, 83.7f), 255f);
            OfficeKit.Prop(g, OfficeKit.PropIds.DeskLamp, new Vector3(-0.75f, A3_Floor + 0.74f, 84.3f), 120f);
            OfficeKit.Prop(g, OfficeKit.PropIds.PaperStack, new Vector3(-0.7f, A3_Floor + 0.74f, 83.25f), -10f);
            var onDesk = OfficeKit.Facing(Vector3.right) * Quaternion.Euler(-75f, 0f, 0f);
            A3_Note(g, "THREE - HEADCOUNT\nFriday 13 May\n\nBadges issued ......... 62\nBadges returned ...... 61\nOutstanding ............... 1\n\n(3-114)",
                new Vector3(-0.55f, A3_Floor + 0.76f, 83.8f), onDesk, 0.014f, new Vector2(0.22f, 0.29f));
            OfficeKit.SpotLight(g, "DeskLamp", new Vector3(-0.6f, 13.35f, 84.05f), Quaternion.LookRotation(new Vector3(0.3f, -1f, -0.1f)), A3_Warm, 4f, 6.5f, 80f, true);
            OfficeKit.PointLight(g, "Fill", new Vector3(px, 14.9f, 82.5f), A3_Warm, 0.35f, 10f);

            // The card catalogue against the north wall, with a loose card on top.
            OfficeKit.Prop(g, OfficeKit.PropIds.FilingCabinet, new Vector3(-0.8f, A3_Floor, 87.17f), 180f);
            OfficeKit.Prop(g, OfficeKit.PropIds.FilingCabinet, new Vector3(-1.4f, A3_Floor, 87.17f), 180f);
            var faceSouth = OfficeKit.Facing(Vector3.back);
            A3_Note(g, "300 - 399", new Vector3(-0.8f, 13.0f, 86.835f), faceSouth, 0.022f, new Vector2(0.17f, 0.05f));
            A3_Note(g, "000 - 299", new Vector3(-1.4f, 13.0f, 86.835f), faceSouth, 0.022f, new Vector2(0.17f, 0.05f));
            A3_Note(g, "000\nNo such room.\nNot to be numbered.\nNot to be filed.", new Vector3(-0.85f, A3_Floor + 1.315f, 87.1f),
                faceSouth * Quaternion.Euler(-70f, 0f, 0f), 0.012f, new Vector2(0.13f, 0.09f));
            OfficeKit.Prop(g, OfficeKit.PropIds.PottedPlant, new Vector3(0.9f, A3_Floor, 87.05f));

            // The director's memo above the catalogue.
            OfficeKit.Prop(g, OfficeKit.PropIds.Corkboard, new Vector3(-1.0f, 13.55f, north - 0.14f), 180f);
            A3_Note(g, "OFFICE OF THE DIRECTOR OF CONTINUITY\nRe: Desk 3-114\n\nThree has not reconciled since Friday.\nUntil it does, Facilities will go on numbering.\nEvery door is to remain a door.\nNothing is to be closed.\n\nT. Vane",
                new Vector3(-1.0f, 14.0f, north - 0.17f), faceSouth, 0.015f, new Vector2(0.46f, 0.36f));

            // A time card in the rack beside door 312.
            OfficeKit.Box(g, "TimeCardRack", OfficeKit.M.MetalDark, new Vector3(4.25f, 12.95f, north - 0.14f), new Vector3(4.55f, 13.6f, north - 0.1f), false);
            A3_Note(g, "TIME CARD\nweek ending 13 May\n\nName  ____________\nDesk  3-114\nIN     8:58\nOUT", new Vector3(4.4f, 13.3f, north - 0.16f), faceSouth,
                0.013f, new Vector2(0.17f, 0.25f));
        }

        // ------------------------------------------------------------------ glass office and coffee corner

        /// <summary>The floor manager's glass box (x 21..29, z 80.5..87.5): lit inside, dark hall around it (inside_01).</summary>
        static void A3_GlassOffice(Transform g)
        {
            const float x0 = 21f, x1 = 29f, z0 = 80.5f, z1 = 87.5f, h = 2.8f;
            const string glass = OfficeKit.M.Glass, metal = OfficeKit.M.MetalDark;
            OfficeKit.Wall(g, "Glass_S", glass, new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0), A3_Floor, h, 0.06f, OfficeKit.Opening.Door(3.5f));
            OfficeKit.Wall(g, "Glass_N", glass, new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), A3_Floor, h, 0.06f);
            OfficeKit.Wall(g, "Glass_W", glass, new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), A3_Floor, h, 0.06f);
            OfficeKit.Wall(g, "Glass_E", glass, new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1), A3_Floor, h, 0.06f);
            OfficeKit.Box(g, "Roof", OfficeKit.M.ConcreteDark, new Vector3(x0 - 0.04f, A3_Floor + h, z0 - 0.04f), new Vector3(x1 + 0.04f, A3_Floor + h + 0.15f, z1 + 0.04f));

            // Mullions: corners, door posts, and every 2 m.
            float[] postX = { x0, 23f, 24.5f, 25.5f, 27f, x1 };
            foreach (float px in postX)
            {
                OfficeKit.Box(g, $"Post_S_{px}", metal, new Vector3(px - 0.04f, A3_Floor, z0 - 0.04f), new Vector3(px + 0.04f, A3_Floor + h, z0 + 0.04f), false);
                if (px != 24.5f && px != 25.5f)
                    OfficeKit.Box(g, $"Post_N_{px}", metal, new Vector3(px - 0.04f, A3_Floor, z1 - 0.04f), new Vector3(px + 0.04f, A3_Floor + h, z1 + 0.04f), false);
            }
            float[] postZ = { 82.25f, 84f, 85.75f };
            foreach (float pz in postZ)
            {
                OfficeKit.Box(g, $"Post_W_{pz}", metal, new Vector3(x0 - 0.04f, A3_Floor, pz - 0.04f), new Vector3(x0 + 0.04f, A3_Floor + h, pz + 0.04f), false);
                OfficeKit.Box(g, $"Post_E_{pz}", metal, new Vector3(x1 - 0.04f, A3_Floor, pz - 0.04f), new Vector3(x1 + 0.04f, A3_Floor + h, pz + 0.04f), false);
            }
            OfficeKit.Box(g, "DoorHead", metal, new Vector3(24.5f, A3_Floor + 2.1f, z0 - 0.04f), new Vector3(25.5f, A3_Floor + 2.18f, z0 + 0.04f), false);

            OfficeKit.Prop(g, OfficeKit.PropIds.Desk, new Vector3(25f, A3_Floor, 85.9f));
            OfficeKit.Prop(g, OfficeKit.PropIds.SwivelChair, new Vector3(25f, A3_Floor, 86.75f), 180f);
            OfficeKit.Prop(g, OfficeKit.PropIds.FoldingChair, new Vector3(24.3f, A3_Floor, 84.4f), 10f);
            OfficeKit.Prop(g, OfficeKit.PropIds.FoldingChair, new Vector3(25.8f, A3_Floor, 84.3f), -15f);
            OfficeKit.Prop(g, OfficeKit.PropIds.DeskLamp, new Vector3(24.4f, A3_Floor + 0.74f, 86.05f), 200f);
            OfficeKit.Prop(g, OfficeKit.PropIds.Telephone, new Vector3(25.45f, A3_Floor + 0.74f, 85.95f), 190f);
            OfficeKit.Prop(g, OfficeKit.PropIds.FilingCabinet, new Vector3(21.5f, A3_Floor, 87.1f), 180f);
            OfficeKit.Prop(g, OfficeKit.PropIds.CoatRack, new Vector3(28.4f, A3_Floor, 87f));
            A3_Note(g, "TELEPHONE MESSAGE\nFor: Mr Fenwick\nFrom: Records\nRe: 3-114. The count is\nstill one over.\nWill call back.   4:47",
                new Vector3(24.85f, A3_Floor + 0.76f, 85.75f), OfficeKit.Facing(Vector3.back) * Quaternion.Euler(-75f, 0f, 0f), 0.012f, new Vector2(0.18f, 0.13f));
            A3_Note(g, "G. FENWICK\nFLOOR MANAGER", new Vector3(23.75f, 13.9f, z0 - 0.04f), OfficeKit.Facing(Vector3.back), 0.035f, new Vector2(0.5f, 0.16f));

            OfficeKit.CeilingLight(g, "OfficeLight", new Vector3(25f, A3_Floor + h - 0.02f, 84.6f), new Vector2(0.6f, 1.2f), A3_Cool, 3.5f, 7f, false, true);
        }

        /// <summary>The coffee corner in the north-west: vending machine, trolley, a table of shell chairs, the Facilities corkboard.</summary>
        static void A3_CoffeeCorner(Transform g)
        {
            OfficeKit.Prop(g, OfficeKit.PropIds.VendingMachine, new Vector3(-32.6f, A3_Floor, 89.15f), 180f);
            OfficeKit.Prop(g, OfficeKit.PropIds.CoffeeTrolley, new Vector3(-30.8f, A3_Floor, 89.3f), 180f);
            OfficeKit.Prop(g, OfficeKit.PropIds.Planter, new Vector3(-22.5f, A3_Floor, 89.15f), 180f);
            OfficeKit.Prop(g, OfficeKit.PropIds.RoundTable, new Vector3(-27f, A3_Floor, 85.2f));
            OfficeKit.Prop(g, OfficeKit.PropIds.ShellChair, new Vector3(-27f, A3_Floor, 84.2f), 0f);
            OfficeKit.Prop(g, OfficeKit.PropIds.ShellChair, new Vector3(-28.0f, A3_Floor, 85.8f), 125f);
            OfficeKit.Prop(g, OfficeKit.PropIds.ShellChair, new Vector3(-25.7f, A3_Floor, 86.1f), 250f);
            OfficeKit.Prop(g, OfficeKit.PropIds.WallClock, new Vector3(-29.2f, 14.2f, 89.57f), 180f);

            OfficeKit.Prop(g, OfficeKit.PropIds.Corkboard, new Vector3(-27.2f, 12.95f, 89.57f), 180f);
            var faceSouth = OfficeKit.Facing(Vector3.back);
            A3_Note(g, "FACILITIES\n\nThe aisle letters are company property\nand are not for birthday banners.\nThe 4 from aisle 34 turned up in Accounts.\n\nW. Brisk",
                new Vector3(-27.45f, 13.42f, 89.54f), faceSouth, 0.016f, new Vector2(0.48f, 0.3f));
            A3_Note(g, "LOST: orange cardigan,\nleft on a chair on Three.\nReception has not seen it.", new Vector3(-26.85f, 13.25f, 89.54f), faceSouth, 0.015f, new Vector2(0.26f, 0.15f));
        }

        // ------------------------------------------------------------------ ceiling troffers

        /// <summary>
        /// The troffer grid (4.8 m): every panel glows, every fourth carries a real downlight (no
        /// shadows), a few are dead, two flicker. The passage, records room and the hall around the
        /// glass office stay dark.
        /// </summary>
        static void A3_CeilingLights(Transform g)
        {
            for (int i = 0; i < 14; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    float x = -31.2f + 4.8f * i, z = 33.6f + 4.8f * j;
                    if (A3_NoTroffer(x, z)) continue;
                    bool lit = i % 2 == 0 && j % 2 == 1;
                    if (lit)
                    {
                        bool flicker = (i == 4 && j == 5) || (i == 10 && j == 3);
                        OfficeKit.CeilingLight(g, $"Troffer_{i}_{j}", new Vector3(x, A3_Ceiling - 0.02f, z), new Vector2(0.6f, 1.2f), A3_Cool,
                            A3_FieldLightIntensity, A3_FieldLightRange, flicker);
                        continue;
                    }
                    bool dead = (i * 7 + j * 3) % 11 == 0;
                    OfficeKit.Box(g, $"Troffer_{i}_{j}", dead ? OfficeKit.M.Plastic : OfficeKit.M.LightPanel,
                        new Vector3(x - 0.3f, A3_Ceiling - 0.02f, z - 0.6f), new Vector3(x + 0.3f, A3_Ceiling + 0.02f, z + 0.6f), false);
                }
            }
        }
    }
}
