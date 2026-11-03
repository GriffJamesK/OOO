using System.Collections.Generic;
using Kit.Gameplay.Signage;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using M = Kit.Editor.OfficeKit.M;
using P = Kit.Editor.OfficeKit.PropIds;

namespace Kit.Editor
{
    /// <summary>
    /// A4 Cafeteria (L1): the inside_02 hall. A cavernous dark room 51 x 49 m and 13.6 m tall,
    /// round tables and orange shell chairs left as if lunch just ended, one hard skylight shaft
    /// through the fog onto a bright patch of floor, a hanging garden on the east wall and a high
    /// south gallery half lost in the haze.
    ///
    /// Movement: a stepped seating terrace on the north side (60 deg planter-faced risers, the
    /// great planter prism in its centre aisle as decoration) tops out at y 8.8. An aisle stair at
    /// its east end climbs beside a floor-to-roof concrete blade wall to a landing at 8.8. The
    /// north balcony (deck y 10.7, 1.9 m higher) starts 5.5 m beyond the landing's end, so no
    /// jump reaches it (apex +1.5). The line: sprint up the stair, jump off the landing's end
    /// angled toward the blade so the wall run catches near the top of the jump, and run the
    /// blade's west face onto the deck (or wall-jump from it). Door 312 on the balcony is brass
    /// and pairs with A3's Records Room. The serving line on the south side has the menu
    /// letterboard (the K4 "S0UP / C0FFEE" hint) and the kitchen roller shutter, brass 412
    /// (slide under; pairs with A3's service hatch).
    ///
    /// All numbers a level designer might retune for the balcony line are the A4_ constants below.
    /// </summary>
    public static partial class OfficeBuilder
    {
        // ------------------------------------------------------------------ A4 layout numbers

        const float A4_Floor = 4f;           // floor top (L1)
        const float A4_Roof = 17.6f;         // roof underside
        const float A4_RoofTop = 18f;        // envelope top
        const float A4_XMin = 20.25f;        // inner face of A2's east wall (A2 owns x = 20)
        const float A4_XMax = 71.6f;         // inner face of our east wall (x 71.6..72)
        const float A4_ZMin = -24.6f;        // inner face of our south wall
        const float A4_ZMax = 24.6f;         // inner face of our north wall

        // North balcony (door 312). Deck 1.9 m above the landing and 5.5 m beyond it: one jump
        // (apex +1.5, plus the controller's 0.3 m step offset) never reaches it.
        const float A4_BalconyY = 10.7f;     // deck top
        const float A4_BalconyEdgeZ = 19f;   // front edge of the deck
        const float A4_BalconyBackZ = 22.6f; // centre plane of the wall holding door 312
        const float A4_BalconyX0 = 32f;      // west end of the balcony
        const float A4_LandingGapX0 = 58f;   // railing left out from here to the blade (where the old stair arrived)
        const float A4_Door312X = 54f;

        // The blade wall and aisle stair (the wall-run line), at the east end of the terraces.
        const float A4_BladeX = 64.4f;       // west face of the blade (it is 0.4 m thick)
        const float A4_BladeZ0 = 3.5f;       // south end of the blade (it runs north to the north wall)
        const float A4_StairX0 = 62f;        // aisle stair and landing, x 62..64.4, against the blade
        const int A4_StairSteps = 16;
        const float A4_StairRise = 0.3f, A4_StairTread = 0.4f;
        const float A4_ThresholdWall = 0.2f; // walls holding thresholds are thinner than the frame depth, so plates stand proud

        // Great planter (decoration) in the terrace's centre aisle.
        const float A4_SlotX0 = 43f, A4_SlotX1 = 49f;
        const float A4_PlanterFootZ = 9.5f;  // south foot of the 60 deg face, on the floor
        const float A4_PlanterLipY = 10.4f;  // sharp lip (no flat top to stand on)

        // Seating terraces either side of the aisle: risers are 60 deg planter faces.
        const float A4_TerraceZ0 = 3.5f, A4_TerraceTread = 2.5f, A4_TerraceRise = 1.2f;
        const int A4_TerraceTiers = 4;
        const float A4_TerraceX0 = 30f, A4_TerraceX1 = 62f;

        // Serving line and kitchen (shutter 412).
        const float A4_KitchenWallZ = -21.15f;
        const float A4_KitchenX0 = 44f;
        const float A4_Shutter412X = 65f;
        const float A4_KitchenTop = 8.5f;     // above any wall-run + wall-jump from the floor (about 7.75)
        static readonly Vector2 A4_ShutterOpening = new Vector2(2f, 1.25f);

        // The skylight: where the sun patch lands; the roof hole is placed up-sun from it.
        static readonly Vector3 A4_SunPatch = new Vector3(60.5f, 4f, 1.5f);
        static readonly Vector2 A4_SkylightSize = new Vector2(5f, 7f);

        const float A4_Tan60 = 1.7320508f;
        static float A4_TerraceBackZ => A4_TerraceZ0 + A4_TerraceTiers * A4_TerraceTread;
        static float A4_PlanterLipZ => A4_PlanterFootZ + (A4_PlanterLipY - A4_Floor) / A4_Tan60;

        /// <summary>The scene sun's forward (OfficeBuilder.BuildScene: Euler 62, -35, 0), so the fake shaft agrees with real sunlight.</summary>
        static Vector3 A4_SunDir => (Quaternion.Euler(62f, -35f, 0f) * Vector3.forward).normalized;

        static readonly Color A4_Warm = new Color(1f, 0.8f, 0.58f);
        static readonly Color A4_Cool = new Color(0.86f, 0.91f, 1f);

        /// <summary>Builds the whole cafeteria under <paramref name="root"/> (world coordinates).</summary>
        static partial void BuildArea_A4(Transform root)
        {
            var rng = new System.Random(447); // fixed seed: the same "just left" mess every build

            OfficeKit.Zone(root, "A4", OfficeLayout.A4);
            Vector3 hole = A4_Shell(root);
            A4_Skylight(root, hole);
            A4_Terraces(root, rng);
            A4_Balcony(root, rng);
            A4_ServingLine(root);
            A4_Kitchen(root);
            A4_Tables(root, rng);
            A4_Garden(root, rng);
            A4_UpperGallery(root, rng);
            A4_Amenities(root, rng);
            A4_Lights(root);

            // Denser, bluer haze than the global volume, toward inside_02 (#2A323C).
            var b = OfficeLayout.A4;
            OfficeKit.FogZone(root, "A4_Cafeteria", new Bounds(b.center, b.size - Vector3.one * 0.6f),
                new Color(0.15f, 0.18f, 0.22f), 4f, 58f, 3f);

            // Safety net under the floor slab (nothing else lives below the cafeteria).
            OfficeKit.FallZone(root, "A4_FallZone_UnderFloor", new Bounds(new Vector3(46f, 1.5f, 0f), new Vector3(50f, 3f, 48f)));
        }

        // ------------------------------------------------------------------ shell

        /// <summary>Floor, east/north/south walls, roof with the skylight hole, deep roof beams. Returns the skylight centre (roof underside).</summary>
        static Vector3 A4_Shell(Transform root)
        {
            var shell = A4_Group(root, "A4_Shell");
            OfficeKit.Slab(shell, "Floor", M.ConcreteFloor, new Vector3(A4_XMin, 0f, -25f), new Vector3(72f, 0f, 25f), A4_Floor, 0.3f);
            OfficeKit.Box(shell, "Wall_North", M.Concrete, new Vector3(A4_XMin, A4_Floor, A4_ZMax), new Vector3(72f, A4_RoofTop, 25f));
            OfficeKit.Box(shell, "Wall_South", M.Concrete, new Vector3(A4_XMin, A4_Floor, -25f), new Vector3(72f, A4_RoofTop, A4_ZMin));
            OfficeKit.Box(shell, "Wall_East", M.Concrete, new Vector3(A4_XMax, A4_Floor, A4_ZMin), new Vector3(72f, A4_RoofTop, A4_ZMax));

            // Skylight hole: up-sun from the patch, so the scene's real sun comes through it too.
            var sun = A4_SunDir;
            float t = (A4_Roof - A4_SunPatch.y) / -sun.y;
            var hole = A4_SunPatch - sun * t;
            float hx0 = hole.x - A4_SkylightSize.x * 0.5f, hx1 = hole.x + A4_SkylightSize.x * 0.5f;
            float hz0 = hole.z - A4_SkylightSize.y * 0.5f, hz1 = hole.z + A4_SkylightSize.y * 0.5f;
            OfficeKit.Slab(shell, "Roof", M.ConcreteDark, new Vector3(A4_XMin, 0f, A4_ZMin), new Vector3(A4_XMax, 0f, A4_ZMax), A4_RoofTop, A4_RoofTop - A4_Roof,
                new Vector4(hx0, hz0, hx1, hz1));

            // Deep brutalist beams under the roof, skipping any that would cross the skylight.
            foreach (float x in new[] { 28f, 36f, 44f, 52f, 60f, 68f })
            {
                if (x + 0.3f > hx0 && x - 0.3f < hx1) continue;
                OfficeKit.Box(shell, $"Beam_{x:0}", M.ConcreteDark, new Vector3(x - 0.3f, A4_Roof - 1.2f, A4_ZMin), new Vector3(x + 0.3f, A4_Roof, A4_ZMax), false);
            }
            return hole;
        }

        /// <summary>
        /// The one skylight: glass, louvre fins tilted along the sun (striped light as in inside_02),
        /// a fake volumetric shaft down to the patch, and a shadowed cool key spot that makes the
        /// patch read even when the real sun is blocked by the floors above.
        /// </summary>
        static void A4_Skylight(Transform root, Vector3 hole)
        {
            var g = A4_Group(root, "A4_Skylight");
            var sun = A4_SunDir;

            var glass = OfficeKit.BoxAt(g, "Glass", M.Glass, new Vector3(hole.x, A4_RoofTop - 0.03f, hole.z),
                new Vector3(A4_SkylightSize.x, 0.02f, A4_SkylightSize.y), Quaternion.identity, false);
            glass.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Fins run east-west, tilted so their planes contain the sun's direction.
            float tilt = Mathf.Atan2(sun.z, -sun.y) * Mathf.Rad2Deg; // degrees off vertical toward +Z, as the light travels
            var finRot = Quaternion.Euler(-tilt, 0f, 0f);
            int fins = Mathf.FloorToInt(A4_SkylightSize.y / 0.6f);
            for (int i = 0; i <= fins; i++)
            {
                float z = hole.z - A4_SkylightSize.y * 0.5f + i * (A4_SkylightSize.y / fins);
                OfficeKit.BoxAt(g, $"Fin_{i:00}", M.MetalDark, new Vector3(hole.x, A4_Roof + 0.2f, z), new Vector3(A4_SkylightSize.x, 0.4f, 0.05f), finRot, false);
            }

            float length = (A4_Roof - A4_Floor) / -sun.y;
            OfficeKit.LightShaft(g, "Shaft", new Vector3(hole.x, A4_Roof, hole.z), Quaternion.FromToRotation(Vector3.up, -sun),
                new Vector3(A4_SkylightSize.x * 0.9f, length, A4_SkylightSize.y * 0.9f), new Color(0.78f, 0.85f, 0.97f, 0.32f));

            // Key "sun" spot just under the roof, aimed along the sun at the patch (shadow light 1 of 4).
            OfficeKit.SpotLight(g, "SunPatchKey", A4_SunPatch - sun * (length - 0.6f), Quaternion.LookRotation(sun, Vector3.up),
                A4_Cool, 70f, length + 6f, 32f, true);
        }

        // ------------------------------------------------------------------ terraces and the great planter

        /// <summary>
        /// Two seating terrace blocks (x 30..43 and 49..62, z 3.5..13.5) stepping up north in four
        /// 1.2 m tiers whose risers are 60 deg planter faces, and the great planter prism in the
        /// 6 m aisle between them. Each block is one extruded mesh so faces meet cleanly.
        /// </summary>
        static void A4_Terraces(Transform root, System.Random rng)
        {
            var g = A4_Group(root, "A4_Terraces");
            var profile = A4_TerraceProfile();
            A4_ProfileSolid(g, "A4_TerraceWest", M.Concrete, A4_TerraceX0, A4_SlotX0, A4_Floor, profile);
            A4_ProfileSolid(g, "A4_TerraceEast", M.Concrete, A4_SlotX1, A4_TerraceX1, A4_Floor, profile);

            // The great planter: a sharp 60 deg prism, decoration: foot at z 9.5, lip 6.4 m up.
            float lipZ = A4_PlanterLipZ;
            float northFootZ = lipZ + (A4_PlanterLipY - A4_Floor) / A4_Tan60;
            A4_ProfileSolid(g, "A4_GreatPlanter", M.ConcreteLight, A4_SlotX0, A4_SlotX1, A4_Floor,
                new[] { new Vector2(A4_PlanterFootZ, A4_Floor), new Vector2(lipZ, A4_PlanterLipY), new Vector2(northFootZ, A4_Floor) });

            // Shell-chair rows on the treads, facing south over the hall; a few left out.
            float[] xs = { 31.6f, 34.4f, 37.2f, 40.0f, 52.0f, 54.8f, 57.6f, 60.4f };
            for (int tier = 1; tier <= A4_TerraceTiers; tier++)
            {
                float y = A4_Floor + tier * A4_TerraceRise;
                float back = A4_TerraceZ0 + tier * A4_TerraceTread;
                float skip = tier == A4_TerraceTiers ? 0.6f : 0.3f;
                foreach (float x in xs)
                {
                    if (rng.NextDouble() < skip) continue;
                    OfficeKit.Prop(g, P.ShellChairRow, new Vector3(x + A4_Range(rng, -0.15f, 0.15f), y, back - 0.45f), 180f + A4_Range(rng, -3f, 3f));
                }
                if (tier == 3)
                {
                    // Philodendrons at the corners of the third tier (well away from the balcony).
                    foreach (float x in new[] { A4_TerraceX0 + 0.5f, A4_SlotX0 - 0.5f, A4_SlotX1 + 0.5f, A4_TerraceX1 - 0.5f })
                        OfficeKit.Prop(g, P.PottedPlant, new Vector3(x, y, back - 0.4f), A4_Range(rng, 0f, 360f));
                }
                if (tier == 1)
                {
                    // A forgotten cardigan on one seat, a cup on a tread.
                    OfficeKit.Box(g, "Cardigan", M.PlantDark, new Vector3(56.1f, y, back - 1.25f), new Vector3(56.5f, y + 0.05f, back - 0.95f), false);
                    OfficeKit.Box(g, "Cup", M.Paper, new Vector3(36f, y, back - 1.2f), new Vector3(36.08f, y + 0.1f, back - 1.12f), false);
                }
            }

            // Philodendrons flanking the planter's foot, and the plaque on the aisle wall.
            OfficeKit.Prop(g, P.PottedPlant, new Vector3(A4_SlotX0 + 0.4f, A4_Floor, A4_PlanterFootZ - 0.5f), 20f);
            OfficeKit.Prop(g, P.PottedPlant, new Vector3(A4_SlotX1 - 0.4f, A4_Floor, A4_PlanterFootZ - 0.5f), 200f);
            OfficeKit.Sign(g, "PLANTING SCHEME, PHASE II\nApproved by T. Vane,\nDirector of Continuity\nMarch 1977",
                new Vector3(A4_SlotX0 + 0.012f, A4_Floor + 0.6f, 5.1f), OfficeKit.Facing(Vector3.right), 0.03f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Brass, new Vector2(0.7f, 0.3f));
        }

        /// <summary>The terrace section as (z, y) points: 60 deg riser faces and flat treads, rising north, vertical back.</summary>
        static List<Vector2> A4_TerraceProfile()
        {
            float run = A4_TerraceRise / A4_Tan60;
            var p = new List<Vector2> { new Vector2(A4_TerraceZ0, A4_Floor) };
            for (int i = 1; i <= A4_TerraceTiers; i++)
            {
                float z = A4_TerraceZ0 + (i - 1) * A4_TerraceTread;
                float y0 = A4_Floor + (i - 1) * A4_TerraceRise;
                if (i > 1) p.Add(new Vector2(z, y0));
                p.Add(new Vector2(z + run, y0 + A4_TerraceRise));
            }
            p.Add(new Vector2(A4_TerraceBackZ, A4_Floor + A4_TerraceTiers * A4_TerraceRise));
            return p;
        }

        // ------------------------------------------------------------------ balcony and door 312

        /// <summary>
        /// The north balcony (deck y 10.7, x 32..64.4, z 19..22.5) and its access line: an aisle
        /// stair at the terraces' east end climbing beside a floor-to-roof blade wall to a landing
        /// at y 8.8 that ends 5.5 m short of the deck. Door 312 sits in the back wall with a closet
        /// behind it. The railing is left out at the east end, where the old stair used to land.
        /// </summary>
        static void A4_Balcony(Transform root, System.Random rng)
        {
            var g = A4_Group(root, "A4_Balcony");
            float y = A4_BalconyY;
            float landingY = A4_Floor + A4_StairSteps * A4_StairRise; // 8.8, level with the top terrace tread
            float landingEnd = A4_TerraceBackZ;                        // 13.5

            // The blade: one solid concrete wall, floor to roof, from the terrace front to the north
            // wall. Its west face is the wall-run surface; it also closes the balcony's east end.
            OfficeKit.Box(g, "Blade", M.Concrete, new Vector3(A4_BladeX, A4_Floor, A4_BladeZ0), new Vector3(A4_BladeX + 0.4f, A4_Roof, A4_ZMax));

            // Aisle stair and landing between the east terrace block and the blade.
            float stairW = A4_BladeX - A4_StairX0;
            var top = OfficeKit.Stairs(g, "AisleStair", M.Concrete, new Vector3(A4_StairX0 + stairW * 0.5f, A4_Floor, A4_BladeZ0), Vector3.forward, stairW,
                A4_StairSteps, A4_StairRise, A4_StairTread);
            OfficeKit.Box(g, "Landing", M.Concrete, new Vector3(A4_StairX0, A4_Floor, top.z), new Vector3(A4_BladeX, landingY, landingEnd));

            OfficeKit.Slab(g, "Deck", M.Concrete, new Vector3(A4_BalconyX0, 0f, A4_BalconyEdgeZ), new Vector3(A4_BladeX, 0f, A4_ZMax), y, 0.4f);
            OfficeKit.Wall(g, "BackWall", M.Concrete, new Vector3(A4_BalconyX0, 0f, A4_BalconyBackZ), new Vector3(A4_BladeX, 0f, A4_BalconyBackZ),
                y, A4_Roof - y, A4_ThresholdWall, OfficeKit.Opening.Door(A4_Door312X - OfficeLayout.Door.x * 0.5f - A4_BalconyX0));
            // Seal the service void behind the back wall at its west end (the blade closes the east end).
            float wallBack = A4_BalconyBackZ + A4_ThresholdWall * 0.5f;
            OfficeKit.Box(g, "VoidEnd_West", M.Concrete, new Vector3(A4_BalconyX0, y, A4_BalconyBackZ - A4_ThresholdWall * 0.5f), new Vector3(A4_BalconyX0 + 0.2f, A4_Roof, A4_ZMax));

            // Door 312: brass, front facing out onto the balcony (south). Pairs with A3's Records Room.
            OfficeKit.Threshold(g, "A4_Threshold_312", new Vector3(A4_Door312X, y + OfficeLayout.Door.y * 0.5f, A4_BalconyBackZ),
                OfficeKit.Facing(Vector3.back), OfficeLayout.Door, "312");

            // The closet behind it (1.9 m deep): what an unpaired 312 shows.
            float cx0 = A4_Door312X - 1.2f, cx1 = A4_Door312X + 1.2f, ceil = y + 2.6f;
            OfficeKit.Box(g, "Closet_WallW", M.Concrete, new Vector3(cx0, y, wallBack), new Vector3(cx0 + 0.2f, ceil, A4_ZMax));
            OfficeKit.Box(g, "Closet_WallE", M.Concrete, new Vector3(cx1 - 0.2f, y, wallBack), new Vector3(cx1, ceil, A4_ZMax));
            OfficeKit.Box(g, "Closet_Ceiling", M.ConcreteDark, new Vector3(cx0, ceil, wallBack), new Vector3(cx1, ceil + 0.2f, A4_ZMax));
            OfficeKit.Prop(g, P.CoatRack, new Vector3(cx1 - 0.5f, y, A4_ZMax - 0.4f), 200f);
            OfficeKit.Box(g, "Closet_MopBucket", M.MetalDark, new Vector3(cx0 + 0.35f, y, A4_ZMax - 0.6f), new Vector3(cx0 + 0.7f, y + 0.35f, A4_ZMax - 0.25f), false);
            OfficeKit.PointLight(g, "Closet_Light", new Vector3(A4_Door312X, ceil - 0.2f, A4_ZMax - 0.9f), new Color(0.9f, 0.85f, 0.75f), 0.8f, 3.5f);

            // Railing: west end and front edge, left out from x 58 to the blade, where the stair used to land.
            float rz = A4_BalconyEdgeZ + 0.05f, front = A4_BalconyBackZ - A4_ThresholdWall * 0.5f - 0.05f;
            OfficeKit.Railing(g, "Railing_West", new Vector3(A4_BalconyX0 + 0.05f, y, rz), new Vector3(A4_BalconyX0 + 0.05f, y, front));
            OfficeKit.Railing(g, "Railing_Front", new Vector3(A4_BalconyX0 + 0.05f, y, rz), new Vector3(A4_LandingGapX0, y, rz));

            // Slim columns under the deck edge.
            foreach (float x in new[] { 35f, 44f, 53f, 61f })
                OfficeKit.Box(g, $"Column_{x:0}", M.Concrete, new Vector3(x - 0.3f, A4_Floor, A4_BalconyEdgeZ + 0.1f), new Vector3(x + 0.3f, y - 0.4f, A4_BalconyEdgeZ + 0.7f));

            // Dressing: a lone table at the quiet west end, a philodendron and a bin by the door.
            OfficeKit.Prop(g, P.RoundTable, new Vector3(38f, y, 20.8f), 15f);
            OfficeKit.Prop(g, P.ShellChair, new Vector3(37.1f, y, 21f), 100f);
            OfficeKit.Prop(g, P.ShellChair, new Vector3(39.1f, y, 20.2f), 290f + A4_Range(rng, -10f, 10f));
            OfficeKit.Prop(g, P.PottedPlant, new Vector3(A4_Door312X + 1.4f, y, front - 0.4f), 40f);
            OfficeKit.Prop(g, P.Bin, new Vector3(A4_Door312X - 1.3f, y, front - 0.3f), 0f);

            // Door lamp: the warm accent high on the north side, seen from the floor.
            OfficeKit.Box(g, "DoorLamp", M.LightWarm, new Vector3(A4_Door312X - 0.15f, y + 2.55f, front - 0.08f), new Vector3(A4_Door312X + 0.15f, y + 2.7f, front), false);
            OfficeKit.PointLight(g, "DoorLamp_Light", new Vector3(A4_Door312X, y + 2.4f, front - 0.5f), A4_Warm, 2.5f, 6f);

            // Text: the Facilities memo (by the railing gap), the overflow plaque, the birthday banner.
            OfficeKit.Sign(g, "FACILITIES NOTICE\n\nThe balcony stair has been taken out.\nThe balcony is closed until further notice\nand the railing will be made good.\n\n14 March 1977",
                new Vector3(60.5f, y + 1.55f, front - 0.01f), OfficeKit.Facing(Vector3.back), 0.03f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.95f, 0.55f));
            OfficeKit.Sign(g, "RECORDS\nOVERFLOW", new Vector3(A4_Door312X + 0.95f, y + 1.6f, front - 0.01f), OfficeKit.Facing(Vector3.back), 0.045f,
                WorldTextStyle.Brass, TextAnchor.MiddleCenter, M.MetalDark, new Vector2(0.42f, 0.22f));
            OfficeKit.Sign(g, "HAPPY BIRTHDAY AGNES", new Vector3(44f, y - 0.85f, A4_BalconyEdgeZ - 0.03f), OfficeKit.Facing(Vector3.back), 0.36f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(8.4f, 0.66f));
        }

        // ------------------------------------------------------------------ serving line, shutter 412, menu board

        /// <summary>
        /// The kitchen front wall along the south side (z -21.15) with the 412 roller shutter at
        /// floor level, the serving counters, trays, the ticket dispenser, the menu letterboard
        /// (K4 hint), the stopped clock and the servery notices.
        /// </summary>
        static void A4_ServingLine(Transform root)
        {
            var g = A4_Group(root, "A4_ServingLine");
            float face = A4_KitchenWallZ + A4_ThresholdWall * 0.5f; // north face of the kitchen wall
            var north = OfficeKit.Facing(Vector3.forward);

            OfficeKit.Wall(g, "KitchenFrontWall", M.Concrete, new Vector3(A4_KitchenX0, 0f, A4_KitchenWallZ), new Vector3(A4_XMax, 0f, A4_KitchenWallZ),
                A4_Floor, A4_KitchenTop - A4_Floor, A4_ThresholdWall,
                new OfficeKit.Opening(A4_Shutter412X - A4_ShutterOpening.x * 0.5f - A4_KitchenX0, A4_ShutterOpening.x, 0f, A4_ShutterOpening.y));

            // Shutter 412: brass, a 2 x 1.25 floor-level opening, front facing into the cafeteria.
            OfficeKit.Threshold(g, "A4_Threshold_412", new Vector3(A4_Shutter412X, A4_Floor + A4_ShutterOpening.y * 0.5f, A4_KitchenWallZ),
                north, A4_ShutterOpening, "412");

            // The half-raised roller shutter: slats from the frame head up into a housing box, guide rails each side.
            float sx0 = A4_Shutter412X - 1.1f, sx1 = A4_Shutter412X + 1.1f;
            float slatBottom = A4_Floor + A4_ShutterOpening.y + 0.12f, housing = A4_Floor + 2.9f;
            int slats = Mathf.FloorToInt((housing - slatBottom) / 0.1f);
            for (int i = 0; i < slats; i++)
            {
                float sy = slatBottom + i * 0.1f;
                OfficeKit.Box(g, $"Slat_{i:00}", i % 2 == 0 ? M.Metal : M.MetalDark, new Vector3(sx0, sy, face), new Vector3(sx1, sy + 0.09f, face + 0.012f), false);
            }
            OfficeKit.Box(g, "Shutter_Housing", M.MetalDark, new Vector3(sx0 - 0.1f, housing, face), new Vector3(sx1 + 0.1f, housing + 0.4f, face + 0.35f), false);
            OfficeKit.Box(g, "Shutter_RailW", M.MetalDark, new Vector3(sx0 - 0.12f, A4_Floor, face), new Vector3(sx0 - 0.07f, housing, face + 0.08f), false);
            OfficeKit.Box(g, "Shutter_RailE", M.MetalDark, new Vector3(sx1 + 0.07f, A4_Floor, face), new Vector3(sx1 + 0.12f, housing, face + 0.08f), false);
            OfficeKit.Sign(g, "KITCHEN - STAFF ONLY", new Vector3(A4_Shutter412X, housing + 0.2f, face + 0.352f), north, 0.09f, WorldTextStyle.LightOnDark);

            // Counters along the wall (they face the hall), trays, the ticket dispenser.
            foreach (float x in new[] { 51.5f, 54.5f, 57.5f, 60.5f })
                OfficeKit.Prop(g, P.ServingCounter, new Vector3(x, A4_Floor, face + 0.4f), 0f);
            float counterTop = A4_Floor + P.SizeOf(P.ServingCounter).y;
            OfficeKit.Box(g, "TrayStand", M.Metal, new Vector3(49.3f, A4_Floor, face + 0.05f), new Vector3(49.9f, counterTop, face + 0.55f));
            OfficeKit.Prop(g, P.TrayStack, new Vector3(49.6f, counterTop, face + 0.3f), 0f);
            OfficeKit.Prop(g, P.TrayStack, new Vector3(50.4f, counterTop, face + 0.35f), 4f);
            OfficeKit.Sign(g, "TRAYS", new Vector3(49.9f, A4_Floor + 1.75f, face + 0.012f), north, 0.09f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.5f, 0.17f));
            OfficeKit.Prop(g, P.TicketDispenser, new Vector3(49.0f, A4_Floor, face + 1.1f), 180f);
            OfficeKit.Sign(g, "NOW SERVING  47", new Vector3(55.5f, A4_Floor + 2.55f, face + 0.03f), north, 0.12f,
                WorldTextStyle.Glow, TextAnchor.MiddleCenter, M.MetalDark, new Vector2(1.5f, 0.3f));

            // The menu letterboard (K4): digit zeros stand in for the letter O, as if the tiles ran out. Tiles can be taken.
            var menuCentre = new Vector3(47f, A4_Floor + 2f, face + 0.05f);
            OfficeKit.Box(g, "MenuFrame", M.WoodDark, new Vector3(45.6f, A4_Floor + 1.22f, face), new Vector3(48.4f, A4_Floor + 2.78f, face + 0.03f), false);
            OfficeKit.LetterboardSign(g, "A4_MenuBoard", menuCentre, north, 6, 14,
                "____TODAY_____\n" +
                "S0UP________35\n" +
                "P0T_R0AST___95\n" +
                "C0FFEE______20\n" +
                "TEA_________15\n" +
                "TOAST_______10");

            OfficeKit.Sign(g, "ALLERGEN NOTICE\n\nThe soup of the day may contain\ncelery, milk and nuts.\nServery staff hold the full list.\n\nM. Okonjo, Kitchen Supervisor",
                new Vector3(52.8f, A4_Floor + 1.75f, face + 0.012f), north, 0.026f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.7f, 0.5f));

            // The clock, stopped at 4:47 like every clock in the building.
            A4_StoppedClock(g, new Vector3(60.5f, A4_Floor + 2.35f, face), 2f);

            // Dish return: a dark steel hatch with a shelf (not an opening), east of the shutter.
            OfficeKit.Box(g, "DishReturn_Panel", M.MetalDark, new Vector3(67.6f, A4_Floor + 0.85f, face), new Vector3(69.8f, A4_Floor + 1.9f, face + 0.02f), false);
            OfficeKit.Box(g, "DishReturn_Shelf", M.Metal, new Vector3(67.5f, A4_Floor + 0.82f, face), new Vector3(69.9f, A4_Floor + 0.87f, face + 0.4f));
            OfficeKit.Prop(g, P.TrayStack, new Vector3(68.2f, A4_Floor + 0.87f, face + 0.22f), 12f);
            OfficeKit.Sign(g, "DISH RETURN\nTRAYS   CUPS   CUTLERY", new Vector3(68.7f, A4_Floor + 2.15f, face + 0.012f), north, 0.06f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(1.3f, 0.3f));
        }

        /// <summary>A wall clock (prop, scaled) with hands built in front of its face at 4:47.</summary>
        static void A4_StoppedClock(Transform parent, Vector3 wallPoint, float scale)
        {
            var size = P.SizeOf(P.WallClock) * scale;
            OfficeKit.Prop(parent, P.WallClock, wallPoint + Vector3.forward * (size.z * 0.5f), 0f, scale);
            var centre = wallPoint + new Vector3(0f, size.y * 0.5f, size.z + 0.01f);
            // Seen from the hall (looking -Z), clockwise runs toward -X; a Z rotation of theta turns "up" to (-sin, cos).
            void Hand(string name, float degrees, float length, float width)
            {
                var rot = Quaternion.Euler(0f, 0f, degrees);
                OfficeKit.BoxAt(parent, name, M.MetalDark, centre + rot * Vector3.up * (length * 0.5f), new Vector3(width, length, 0.01f), rot, false);
            }
            Hand("Clock_HourHand", (4f + 47f / 60f) * 30f, size.y * 0.25f, 0.03f);
            Hand("Clock_MinuteHand", 47f * 6f, size.y * 0.38f, 0.02f);
        }

        /// <summary>The small dark steel kitchen behind shutter 412 (x 44..71.6, z -24.6..-21.25), only seen when 412 is unpaired.</summary>
        static void A4_Kitchen(Transform root)
        {
            var g = A4_Group(root, "A4_Kitchen");
            float back = A4_KitchenWallZ - A4_ThresholdWall * 0.5f;
            OfficeKit.Box(g, "WestWall", M.Concrete, new Vector3(A4_KitchenX0 - 0.2f, A4_Floor, A4_ZMin), new Vector3(A4_KitchenX0, A4_KitchenTop, back + A4_ThresholdWall));
            OfficeKit.Box(g, "Roof", M.ConcreteDark, new Vector3(A4_KitchenX0 - 0.2f, A4_KitchenTop - 0.3f, A4_ZMin), new Vector3(A4_XMax, A4_KitchenTop, back));

            OfficeKit.Box(g, "BackWorktop", M.Metal, new Vector3(A4_KitchenX0 + 1f, A4_Floor, A4_ZMin), new Vector3(70.5f, A4_Floor + 0.9f, A4_ZMin + 0.7f));
            OfficeKit.Box(g, "Hood", M.Metal, new Vector3(51f, A4_Floor + 2.6f, A4_ZMin), new Vector3(59f, A4_Floor + 3.2f, A4_ZMin + 1f), false);
            OfficeKit.Box(g, "Shelf_1", M.Metal, new Vector3(61f, A4_Floor + 2f, A4_ZMin), new Vector3(70f, A4_Floor + 2.04f, A4_ZMin + 0.4f), false);
            OfficeKit.Box(g, "Shelf_2", M.Metal, new Vector3(61f, A4_Floor + 2.6f, A4_ZMin), new Vector3(70f, A4_Floor + 2.64f, A4_ZMin + 0.4f), false);
            OfficeKit.Box(g, "Island", M.Metal, new Vector3(49f, A4_Floor, -23.1f), new Vector3(58f, A4_Floor + 0.9f, -22.4f));
            OfficeKit.Box(g, "StockPot", M.MetalDark, new Vector3(53.2f, A4_Floor + 0.9f, -22.95f), new Vector3(53.6f, A4_Floor + 1.25f, -22.55f), false);
            OfficeKit.Box(g, "Ladle", M.Metal, new Vector3(53.35f, A4_Floor + 1.25f, -22.8f), new Vector3(53.4f, A4_Floor + 1.6f, -22.75f), false);
            OfficeKit.Prop(g, P.Bin, new Vector3(68.5f, A4_Floor, -22.4f), 0f);

            OfficeKit.PointLight(g, "Kitchen_Light_W", new Vector3(54f, A4_KitchenTop - 0.6f, -23f), new Color(0.7f, 0.8f, 0.9f), 1.2f, 8f);
            OfficeKit.PointLight(g, "Kitchen_Light_E", new Vector3(66f, A4_KitchenTop - 0.6f, -23f), new Color(0.7f, 0.8f, 0.9f), 1.2f, 8f);
        }

        // ------------------------------------------------------------------ tables

        /// <summary>
        /// About 34 round tables in two blocks either side of the clear 10 m aisle that runs from
        /// the menu board north to the great planter. Four shell chairs each, some
        /// pushed back, turned or missing; cups, table numbers, one birthday card in the sun patch.
        /// </summary>
        static void A4_Tables(Transform root, System.Random rng)
        {
            var g = A4_Group(root, "A4_Tables");
            float tableTop = A4_Floor + P.SizeOf(P.RoundTable).y;
            float[] rows = { -17f, -12.5f, -8f, -3.5f, 1f };
            float[] west = { 26.5f, 31f, 35.5f, 40f };
            float[] east = { 52.5f, 57f, 61.5f, 66f };
            var spots = new List<Vector3>();
            foreach (float z in rows)
            {
                foreach (float x in west)
                    if (!(x < 28f && z > -5f)) spots.Add(new Vector3(x, A4_Floor, z)); // keep the entry walk clear
                foreach (float x in east) spots.Add(new Vector3(x, A4_Floor, z));
            }

            int number = 1;
            var patchTable = Vector3.positiveInfinity;
            for (int i = 0; i < spots.Count; i++)
            {
                if (i == 9 || i == 22 || i == 30) continue; // a few gaps where tables were dragged away
                var c = spots[i] + new Vector3(A4_Range(rng, -0.4f, 0.4f), 0f, A4_Range(rng, -0.4f, 0.4f));
                var table = OfficeKit.Prop(g, P.RoundTable, c, A4_Range(rng, 0f, 360f));
                table.name = $"Table_{i:00}";
                if ((c - A4_SunPatch).sqrMagnitude < (patchTable - A4_SunPatch).sqrMagnitude) patchTable = c;

                float baseAngle = A4_Range(rng, 0f, 90f);
                for (int k = 0; k < 4; k++)
                {
                    double roll = rng.NextDouble();
                    if (roll < 0.12) continue; // chair taken to another table
                    bool pushed = roll > 0.75;
                    float a = baseAngle + k * 90f + A4_Range(rng, -14f, 14f);
                    float r = 0.85f + (pushed ? A4_Range(rng, 0.3f, 0.75f) : A4_Range(rng, -0.05f, 0.08f));
                    var dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                    float yaw = a + 180f + (pushed ? A4_Range(rng, -45f, 45f) : A4_Range(rng, -10f, 10f));
                    OfficeKit.Prop(g, P.ShellChair, c + dir * r, yaw);
                }

                if (rng.NextDouble() < 0.4)
                {
                    int cups = 1 + rng.Next(3);
                    for (int k = 0; k < cups; k++)
                    {
                        var p = c + Quaternion.Euler(0f, A4_Range(rng, 0f, 360f), 0f) * Vector3.forward * A4_Range(rng, 0.1f, 0.42f);
                        OfficeKit.Box(g, "Cup", M.Paper, new Vector3(p.x - 0.04f, tableTop, p.z - 0.04f), new Vector3(p.x + 0.04f, tableTop + 0.1f, p.z + 0.04f), false);
                    }
                }

                if (i % 3 == 0)
                {
                    string label = i == 12 ? "RESERVED\nT. VANE" : $"TABLE {number++}";
                    var p = c + new Vector3(0.25f, 0.065f, 0.1f);
                    p.y = tableTop + 0.065f;
                    OfficeKit.Sign(g, label, p, Quaternion.Euler(0f, A4_Range(rng, 0f, 360f), 0f), 0.022f,
                        WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.15f, 0.1f));
                }
            }

            // One chair knocked onto its side, near the aisle.
            var fallen = OfficeKit.Prop(g, P.ShellChair, new Vector3(42.2f, A4_Floor + 0.25f, -10.4f), 0f);
            fallen.transform.rotation = Quaternion.Euler(0f, 64f, 90f);

            // On the table in the sun patch: a cake box and a card left for Agnes.
            var patch = new Vector3(patchTable.x - 0.15f, tableTop, patchTable.z - 0.1f);
            OfficeKit.Box(g, "CakeBox", M.Paper, patch + new Vector3(-0.15f, 0f, -0.15f), patch + new Vector3(0.15f, 0.12f, 0.15f), false);
            OfficeKit.Sign(g, "For Agnes\nwith love from\nall of Records", patch + new Vector3(0.3f, 0.06f, 0.05f), Quaternion.Euler(0f, 160f, 0f), 0.017f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.16f, 0.11f));
        }

        // ------------------------------------------------------------------ hanging garden and upper gallery

        /// <summary>The hanging garden: a planting shelf 6 m up the east wall (y 10), planters and pots, vines trailing down the concrete.</summary>
        static void A4_Garden(Transform root, System.Random rng)
        {
            var g = A4_Group(root, "A4_Garden");
            const float shelfY = 10f;
            OfficeKit.Box(g, "Shelf", M.ConcreteDark, new Vector3(A4_XMax - 1f, shelfY - 0.3f, -18f), new Vector3(A4_XMax, shelfY, 14f));
            for (float z = -16f; z <= 12.1f; z += 4f)
                OfficeKit.Prop(g, P.Planter, new Vector3(A4_XMax - 0.5f, shelfY, z), 90f);
            for (float z = -14f; z <= 10.1f; z += 8f)
                OfficeKit.Prop(g, P.PottedPlant, new Vector3(A4_XMax - 0.45f, shelfY, z), A4_Range(rng, 0f, 360f));
            for (float z = -17.6f; z < 13.8f; z += A4_Range(rng, 0.35f, 0.8f))
                A4_Vine(g, new Vector3(A4_XMax - 1.02f, shelfY - 0.3f, z), Vector3.left, A4_Range(rng, 1.2f, 3.8f), rng);

            OfficeKit.PointLight(g, "Garden_Glow_S", new Vector3(A4_XMax - 2.5f, shelfY + 2f, -8f), new Color(0.6f, 0.75f, 0.7f), 1.4f, 9f);
            OfficeKit.PointLight(g, "Garden_Glow_N", new Vector3(A4_XMax - 2.5f, shelfY + 2f, 6f), new Color(0.6f, 0.75f, 0.7f), 1.2f, 9f);
        }

        /// <summary>The galleries high on the south wall (decor, unreachable): a slab at y 14.5, railing, planters spilling vines, dark doorways and one lit one.</summary>
        static void A4_UpperGallery(Transform root, System.Random rng)
        {
            var g = A4_Group(root, "A4_UpperGallery");
            const float y = 14.5f, edge = -22.2f, x0 = 26f, x1 = 58f;
            OfficeKit.Slab(g, "Deck", M.Concrete, new Vector3(x0, 0f, A4_ZMin), new Vector3(x1, 0f, edge), y, 0.35f);
            OfficeKit.Railing(g, "Railing", new Vector3(x0, y, edge + 0.05f), new Vector3(x1, y, edge + 0.05f));
            for (float x = 28f; x <= 56f; x += 5f)
                OfficeKit.Prop(g, P.Planter, new Vector3(x, y, edge - 0.6f), 0f);
            for (float x = x0 + 0.4f; x < x1 - 0.3f; x += A4_Range(rng, 0.4f, 0.9f))
                A4_Vine(g, new Vector3(x, y - 0.35f, edge + 0.08f), Vector3.forward, A4_Range(rng, 1f, 3.6f), rng);

            foreach (float x in new[] { 30f, 38f, 46f, 54f })
            {
                bool lit = x == 46f;
                OfficeKit.Box(g, $"Doorway_{x:0}", lit ? M.LightPanel : M.MetalDark, new Vector3(x - 0.55f, y, A4_ZMin), new Vector3(x + 0.55f, y + 2.2f, A4_ZMin + 0.04f), false);
            }
            OfficeKit.PointLight(g, "Doorway_Glow", new Vector3(46f, y + 1.4f, A4_ZMin + 0.8f), A4_Cool, 1.6f, 5f);
        }

        /// <summary>One trailing philodendron vine: a thin dark-green strip hanging from <paramref name="top"/>, leaning slightly off the wall.</summary>
        static void A4_Vine(Transform parent, Vector3 top, Vector3 outward, float length, System.Random rng)
        {
            var centre = top + Vector3.down * (length * 0.5f) + outward * 0.04f;
            var rot = Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(A4_Range(rng, -3f, 3f), 0f, A4_Range(rng, -4f, 4f));
            OfficeKit.BoxAt(parent, "Vine", rng.NextDouble() < 0.5 ? M.Plant : M.PlantDark, centre,
                new Vector3(A4_Range(rng, 0.12f, 0.28f), length, 0.04f), rot, false);
        }

        // ------------------------------------------------------------------ amenities and notices

        /// <summary>Vending machines, the water cooler, corkboards (rota, lost property, lunch club), the lunch-hours notice, the entry sign, and the clutter under the balcony.</summary>
        static void A4_Amenities(Transform root, System.Random rng)
        {
            var g = A4_Group(root, "A4_Amenities");
            var north = OfficeKit.Facing(Vector3.forward);
            float wall = A4_ZMin;

            // South-west corner: vending machines and the water cooler.
            float vd = P.SizeOf(P.VendingMachine).z;
            foreach (float x in new[] { 23.6f, 24.55f, 25.5f })
                OfficeKit.Prop(g, P.VendingMachine, new Vector3(x, A4_Floor, wall + vd * 0.5f + 0.02f), 0f);
            OfficeKit.Sign(g, "OUT OF ORDER\ncoin return jammed\nFacilities notified 2/11", new Vector3(24.55f, A4_Floor + 1.35f, wall + vd + 0.035f), north, 0.026f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.32f, 0.18f));
            OfficeKit.Prop(g, P.WaterCooler, new Vector3(27f, A4_Floor, wall + 0.25f), 0f);
            OfficeKit.Prop(g, P.Bin, new Vector3(27.6f, A4_Floor, wall + 0.25f), 0f);
            OfficeKit.PointLight(g, "Vending_Glow", new Vector3(24.6f, A4_Floor + 1.6f, wall + 1.6f), new Color(0.75f, 0.85f, 1f), 0.9f, 4f);

            // Corkboards: the servery rota, lost property and the lunch club.
            float cork = P.SizeOf(P.Corkboard).z;
            OfficeKit.Prop(g, P.Corkboard, new Vector3(30.5f, A4_Floor + 1.15f, wall + cork * 0.5f), 0f);
            OfficeKit.Prop(g, P.Corkboard, new Vector3(32f, A4_Floor + 1.15f, wall + cork * 0.5f), 0f);
            float pin = wall + cork + 0.012f;
            OfficeKit.Sign(g, "SERVERY ROTA  -  WEEK 44\n\nMON   W. Brisk / L. Sato\nTUE   K. Ibarra / J. Halloran\nWED   W. Brisk / N. Lindgren\nTHU   S. Oduya / L. Sato\nFRI   ----\n\nSwaps through M. Okonjo only.",
                new Vector3(30.5f, A4_Floor + 1.6f, pin), north, 0.024f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.62f, 0.5f));
            OfficeKit.Sign(g, "LOST PROPERTY\n\nUmbrella, black, bamboo handle\nCardigan, green (L.S.?)\nReading glasses, tortoiseshell\nThermos, tartan\nOne key on a brass fob, no number\n\nUnclaimed items pass to\nFacilities after 30 days.",
                new Vector3(31.85f, A4_Floor + 1.62f, pin), Quaternion.Euler(0f, 0f, 2f), 0.022f, WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.58f, 0.56f));
            OfficeKit.Sign(g, "LUNCH CLUB\nThursdays, table 9\nH. Lindqvist  P. Hume  O. Marsh", new Vector3(32.35f, A4_Floor + 1.3f, pin + 0.004f), Quaternion.Euler(0f, 0f, -3f), 0.02f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.4f, 0.14f));

            // Lunch hours, framed on the south wall.
            OfficeKit.Box(g, "LunchNotice_Frame", M.WoodDark, new Vector3(35f - 0.36f, A4_Floor + 1.32f, wall), new Vector3(35f + 0.36f, A4_Floor + 2.08f, wall + 0.02f), false);
            OfficeKit.Sign(g, "LUNCH IS SERVED\n12.00 - 1.30\n\nSecond sitting suspended\nuntil further notice.\n\nFACILITIES", new Vector3(35f, A4_Floor + 1.7f, wall + 0.032f), north, 0.032f,
                WorldTextStyle.DarkOnLight, TextAnchor.MiddleCenter, M.Paper, new Vector2(0.62f, 0.66f));

            // The entry: brass letters over the opening on our side of A2's wall, philodendrons either side.
            OfficeKit.Sign(g, "STAFF CAFETERIA", new Vector3(A4_XMin + 0.02f, A4_Floor + 3.55f, 0f), OfficeKit.Facing(Vector3.right), 0.2f, WorldTextStyle.Brass);
            OfficeKit.Prop(g, P.PottedPlant, new Vector3(A4_XMin + 0.5f, A4_Floor, -4f), 30f);
            OfficeKit.Prop(g, P.PottedPlant, new Vector3(A4_XMin + 0.5f, A4_Floor, 4f), 250f);
            OfficeKit.Prop(g, P.CoatRack, new Vector3(A4_XMin + 0.5f, A4_Floor, 5.2f), 0f);

            // Under the balcony: stacked chairs, the coffee trolley, bins.
            foreach (float x in new[] { 66.4f, 67.2f, 69.8f })
            {
                int n = 4 + rng.Next(4);
                for (int k = 0; k < n; k++)
                    OfficeKit.Prop(g, P.ShellChair, new Vector3(x, A4_Floor + k * 0.09f, A4_ZMax - 0.5f), 180f + A4_Range(rng, -4f, 4f));
            }
            OfficeKit.Prop(g, P.CoffeeTrolley, new Vector3(57.5f, A4_Floor, A4_ZMax - 0.6f), 186f);
            OfficeKit.Prop(g, P.Bin, new Vector3(55.6f, A4_Floor, A4_ZMax - 0.3f), 0f);
            OfficeKit.Prop(g, P.Bin, new Vector3(36.4f, A4_Floor, A4_ZMax - 0.3f), 0f);
        }

        // ------------------------------------------------------------------ lights

        /// <summary>Dim warm pendants over a few tables (away from the sun patch) and a low cool graze on the blade wall so the balcony line reads.</summary>
        static void A4_Lights(Transform root)
        {
            var g = A4_Group(root, "A4_Lights");
            var pendants = new[]
            {
                new Vector3(31f, 0f, -12f), new Vector3(35.5f, 0f, -3f), new Vector3(26.5f, 0f, -16.5f), new Vector3(40f, 0f, -16.5f),
                new Vector3(52.5f, 0f, -7.5f), new Vector3(57f, 0f, -12f), new Vector3(66f, 0f, -16.5f),
            };
            for (int i = 0; i < pendants.Length; i++)
                A4_Pendant(g, $"Pendant_{i}", new Vector3(pendants[i].x, 8.6f, pendants[i].z));

            OfficeKit.SpotLight(g, "BladeGraze", new Vector3(A4_BladeX - 3.5f, A4_Roof - 1f, 11f),
                Quaternion.LookRotation(new Vector3(0.45f, -0.7f, 0.55f), Vector3.up), new Color(0.7f, 0.78f, 0.9f), 9f, 18f, 45f, false);
        }

        /// <summary>A cafeteria pendant: a long cord from the roof, a dark shade with a warm glowing underside, and a dim point light.</summary>
        static void A4_Pendant(Transform parent, string name, Vector3 bottom)
        {
            var root = A4_Group(parent, name);
            OfficeKit.Box(root, "Cord", M.MetalDark, new Vector3(bottom.x - 0.01f, bottom.y + 0.3f, bottom.z - 0.01f), new Vector3(bottom.x + 0.01f, A4_Roof, bottom.z + 0.01f), false);
            OfficeKit.Box(root, "Shade", M.MetalDark, new Vector3(bottom.x - 0.3f, bottom.y + 0.05f, bottom.z - 0.3f), new Vector3(bottom.x + 0.3f, bottom.y + 0.3f, bottom.z + 0.3f), false);
            OfficeKit.Box(root, "Glow", M.LightWarm, new Vector3(bottom.x - 0.24f, bottom.y + 0.03f, bottom.z - 0.24f), new Vector3(bottom.x + 0.24f, bottom.y + 0.05f, bottom.z + 0.24f), false);
            OfficeKit.PointLight(root, "Light", bottom + Vector3.down * 0.3f, A4_Warm, 2.2f, 9f);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>An empty grouping transform at the origin, so children keep world coordinates.</summary>
        static Transform A4_Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>Uniform random float in [a, b) from the area's seeded generator.</summary>
        static float A4_Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>
        /// A solid extruded along X from <paramref name="x0"/> to <paramref name="x1"/>: its section
        /// is the area between <paramref name="baseY"/> and a polyline of (z, y) points with z never
        /// decreasing (treads, 60 deg faces, vertical risers). One flat-shaded mesh with a matching
        /// MeshCollider, so sloped faces meet treads and ridges with no seams or slivers (boxes leave
        /// those at a sharp ridge). The mesh is saved under Generated/A4/ and updated in place.
        /// </summary>
        static GameObject A4_ProfileSolid(Transform parent, string name, string material, float x0, float x1, float baseY, IList<Vector2> profile)
        {
            var origin = new Vector3(x0, baseY, profile[0].x);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-8f && Vector3.Cross(c - a, d - a).sqrMagnitude < 1e-8f) return;
                if (Vector3.Dot(Vector3.Cross(b - a, c - a) + Vector3.Cross(c - a, d - a), outward) < 0f) { var tmp = b; b = d; d = tmp; }
                int i = vertices.Count;
                foreach (var v in new[] { a, b, c, d })
                {
                    vertices.Add(v - origin);
                    var n = new Vector3(Mathf.Abs(outward.x), Mathf.Abs(outward.y), Mathf.Abs(outward.z));
                    uvs.Add(n.y >= n.x && n.y >= n.z ? new Vector2(v.x, v.z) : n.x >= n.z ? new Vector2(v.z, v.y) : new Vector2(v.x, v.y));
                }
                triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            for (int s = 0; s < profile.Count - 1; s++)
            {
                Vector2 p = profile[s], q = profile[s + 1];
                float dz = q.x - p.x, dy = q.y - p.y;
                // Top surface of this segment; outward is the upper side of the polyline.
                Quad(new Vector3(x0, p.y, p.x), new Vector3(x1, p.y, p.x), new Vector3(x1, q.y, q.x), new Vector3(x0, q.y, q.x), new Vector3(0f, dz, -dy));
                // Side caps, a strip from the base up to the segment.
                if (dz > 1e-4f)
                {
                    Quad(new Vector3(x0, baseY, p.x), new Vector3(x0, p.y, p.x), new Vector3(x0, q.y, q.x), new Vector3(x0, baseY, q.x), Vector3.left);
                    Quad(new Vector3(x1, baseY, p.x), new Vector3(x1, p.y, p.x), new Vector3(x1, q.y, q.x), new Vector3(x1, baseY, q.x), Vector3.right);
                }
            }
            Vector2 first = profile[0], last = profile[profile.Count - 1];
            if (first.y > baseY + 1e-4f)
                Quad(new Vector3(x0, baseY, first.x), new Vector3(x1, baseY, first.x), new Vector3(x1, first.y, first.x), new Vector3(x0, first.y, first.x), Vector3.back);
            if (last.y > baseY + 1e-4f)
                Quad(new Vector3(x0, baseY, last.x), new Vector3(x1, baseY, last.x), new Vector3(x1, last.y, last.x), new Vector3(x0, last.y, last.x), Vector3.forward);

            string generated = OfficeKit.Root + "/Generated";
            string dir = generated + "/A4";
            if (!AssetDatabase.IsValidFolder(generated)) AssetDatabase.CreateFolder(OfficeKit.Root, "Generated");
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(generated, "A4");
            string path = $"{dir}/Mesh_{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created) mesh = new Mesh { name = "Mesh_" + name };
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = OfficeKit.Mat(material);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }
    }
}
