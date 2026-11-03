using UnityEditor;
using UnityEngine;

namespace Kit.Editor
{
    // The Sandbox movement course: a 160 x 160 m walled arena. The east half is a flowing loop that
    // runs like an obstacle course; the west half is a test bay of measurement stations.
    // All numbers are metres; boxes are built from their min/max corners so this reads like a plan.
    //
    // Loop (start at spawn, south edge, facing +Z), each section leads into the next:
    //   C01 Start straight   two hurdles one jump apart (hop chain), slide bar
    //   C02 Kicker           20 deg ramp up to a 2 m deck
    //   C03 Deck + gap       4 m jump gap, slide-under bar on the deck
    //   C04 Wall-run gap     9 m gap: wall-run the orange wall, jump off onto the next deck
    //   C05 Turn east        deck with a hop box, then a 15 deg ramp down
    //   C06 Slalom           staggered pillars
    //   C07 Curve            curved wall turning south (wall-runnable)
    //   C08 Corridor         6 m wall-to-wall corridor heading south
    //   C09 Return lane      vault boxes, stair hump, slide tunnel, step-ups, back into the start straight
    // Test bay (west): stair and ramp limits, tiers and goal tower, jump-gap ladder, curb and vault
    // rows, a raised 60 deg surf prism with a start platform,
    // a free wall-run wall, and the greybox prefab sample.
    //
    // Colours follow Valve-style dev textures: plain geometry is a deterministic mix of three greys;
    // movement features (things to jump, slide, vault or wall-run) are orange.
    public static partial class KitBuilder
    {
        const float CourseHalfSize = 80f;
        static readonly Vector3 SpawnPoint = new Vector3(0f, 0.05f, -72f);

        static void BuildCourse()
        {
            BuildBoundary();
            BuildStartStraight();
            BuildUpperRun();
            BuildEastLoop();
            BuildReturnLane();
            BuildTestBay();
            BuildSurfRamp();
        }

        /// <summary>
        /// Surf test: one solid triangular prism (a single mesh, so the two faces meet cleanly at the
        /// ridge) floating 3 m off the ground, 9 m tall, 45 m long, with 60 deg faces (well past the
        /// 45 deg walkable limit, so they always surf). Climb the stairs to the start platform 1 m
        /// above the ridge, run off the north edge onto either face, and strafe into it to stay on.
        /// </summary>
        static void BuildSurfRamp()
        {
            var s = Section("TestBay_SurfRamp");
            const float ridgeX = -18f, baseY = 3f, height = 9f, angle = 60f, zStart = -15f, length = 45f;
            float halfWidth = height / Mathf.Tan(angle * Mathf.Deg2Rad);

            var ramp = new GameObject("SurfRamp");
            ramp.transform.SetParent(s, false);
            ramp.transform.position = new Vector3(ridgeX, baseY, zStart);
            var mesh = PrismMesh("Mesh_SurfRamp", halfWidth, height, length);
            ramp.AddComponent<MeshFilter>().sharedMesh = mesh;
            ramp.AddComponent<MeshRenderer>().sharedMaterial = Mat(DevOrange);
            ramp.AddComponent<MeshCollider>().sharedMesh = mesh;

            // Start platform 1 m above the ridge and as wide as the ramp, so you pick how high on a face
            // to drop in; stairs up from the south.
            float top = baseY + height + 1f;
            Block(s, "SurfStartPlatform", Dev("SurfStart"), new Vector3(ridgeX - halfWidth, 0f, zStart - 7f), new Vector3(ridgeX + halfWidth, top, zStart));
            const float rise = 0.25f, tread = 0.35f;
            float run = Mathf.CeilToInt(top / rise - 0.001f) * tread;
            Stairs(s, "SurfStairs", Dev("SurfStairs"), new Vector3(ridgeX, 0f, zStart - 7f - run), Vector3.forward, 4f, rise, tread, top);
        }

        /// <summary>
        /// A triangular prism running +Z from the origin: base 2 x <paramref name="halfWidth"/> wide on
        /// y = 0, ridge at <paramref name="height"/>. Flat-shaded (each face has its own vertices) and
        /// saved under Generated/ so the scene can reference it; re-running the builder updates it in place.
        /// </summary>
        static Mesh PrismMesh(string name, float halfWidth, float height, float length)
        {
            Vector3 w0 = new Vector3(-halfWidth, 0f, 0f), e0 = new Vector3(halfWidth, 0f, 0f), r0 = new Vector3(0f, height, 0f);
            var l = Vector3.forward * length;
            Vector3 w1 = w0 + l, e1 = e0 + l, r1 = r0 + l;
            float slope = Vector3.Distance(w0, r0);
            var quads = new[]
            {
                // corners clockwise seen from outside (Unity's front face); uv size per face in metres
                (new[] { w0, w1, r1, r0 }, new Vector2(length, slope)),         // west face
                (new[] { e0, r0, r1, e1 }, new Vector2(slope, length)),         // east face
                (new[] { w0, e0, e1, w1 }, new Vector2(2f * halfWidth, length)), // underside
            };
            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();
            foreach (var (c, size) in quads)
            {
                int i = vertices.Count;
                vertices.AddRange(c);
                uvs.AddRange(new[] { Vector2.zero, new Vector2(size.x, 0f), size, new Vector2(0f, size.y) });
                triangles.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            foreach (var (a, b, c) in new[] { (w0, r0, e0), (e1, r1, w1) }) // south and north ends
            {
                int i = vertices.Count;
                vertices.AddRange(new[] { a, b, c });
                uvs.AddRange(new[] { Vector2.zero, new Vector2(halfWidth, height), new Vector2(2f * halfWidth, 0f) });
                triangles.AddRange(new[] { i, i + 1, i + 2 });
            }

            string path = $"{Root}/Generated/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created) mesh = new Mesh { name = name };
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // ---------- loop ----------

        static void BuildBoundary()
        {
            var s = Section("Boundary_Walls");
            const float h = 10f, t = 1f, e = CourseHalfSize;
            Block(s, "Wall_South", DevDark, new Vector3(-e - t, 0f, -e - t), new Vector3(e + t, h, -e));
            Block(s, "Wall_North", DevDark, new Vector3(-e - t, 0f, e), new Vector3(e + t, h, e + t));
            Block(s, "Wall_West", DevDark, new Vector3(-e - t, 0f, -e), new Vector3(-e, h, e));
            Block(s, "Wall_East", DevDark, new Vector3(e, 0f, -e), new Vector3(e + t, h, e));
        }

        static void BuildStartStraight()
        {
            var s = Section("C01_StartStraight");
            for (int z = -70; z <= -40; z += 10) // flush distance markers
                Block(s, $"Marker_{z}", DevMid, new Vector3(-5f, 0f, z - 0.15f), new Vector3(5f, 0.02f, z + 0.15f));
            // Lane curbs; the right one has a gap where the return lane rejoins (z -72..-62).
            Block(s, "Curb_L", DevLight, new Vector3(-5.3f, 0f, -77f), new Vector3(-5f, 0.3f, -34f));
            Block(s, "Curb_R_South", DevLight, new Vector3(5f, 0f, -77f), new Vector3(5.3f, 0.3f, -72f));
            Block(s, "Curb_R_North", DevLight, new Vector3(5f, 0f, -62f), new Vector3(5.3f, 0.3f, -34f));

            Block(s, "Hurdle_1", DevOrange, new Vector3(-5f, 0f, -56.15f), new Vector3(5f, 0.6f, -55.85f));
            Block(s, "Hurdle_2", DevOrange, new Vector3(-5f, 0f, -50.15f), new Vector3(5f, 0.6f, -49.85f));
            SlideBar(s, "SlideBar", -36f, groundY: 0f); // ~8 m after the hurdle-2 landing: room to start a slide

            var c02 = Section("C02_Kicker");
            Ramp(c02, "Kicker_20deg", DevOrange, new Vector3(0f, 0f, -32f), Vector3.forward, 10f, 20f, 2f);
        }

        static void BuildUpperRun()
        {
            // Kicker top lands at z = -32 + 2 / tan(20) = -26.5.
            var s = Section("C03_DeckAndGap");
            Block(s, "Deck_A", Dev("Deck_A"), new Vector3(-5f, 0f, -26.5f), new Vector3(5f, 2f, -14f));
            // 4 m gap (z -14..-10), then Deck B with a slide-under bar on it.
            Block(s, "Deck_B", Dev("Deck_B"), new Vector3(-5f, 0f, -10f), new Vector3(5f, 2f, 2f));
            SlideBar(s, "DeckSlideBar", -4f, groundY: 2f);

            var w = Section("C04_WallRunGap");
            // 9 m gap (z 2..11): run the wall on the right and jump off it to cross. The wall-run arc
            // stays above deck height for ~1 s, so a pure run falls just short and a wall jump lands it
            // even at plain sprint speed (the kick angle costs forward speed). Flush with the deck edge (x 5) so
            // running along the edge puts the wall within catch range.
            Block(w, "WallRunWall", DevOrange, new Vector3(5f, 0f, 0f), new Vector3(5.5f, 7f, 11f));
            Block(w, "GapFloor_Marker", DevDark, new Vector3(-5f, 0f, 2f), new Vector3(5f, 0.02f, 11f));
        }

        static void BuildEastLoop()
        {
            var s = Section("C05_TurnEast");
            // L-shaped deck: land at x -5..5, turn right and run east along z 11..30.
            Block(s, "Deck_C", Dev("Deck_C"), new Vector3(-5f, 0f, 11f), new Vector3(30f, 2f, 30f));
            Block(s, "HopBox", DevOrange, new Vector3(14f, 2f, 18f), new Vector3(16f, 3f, 30f));
            Ramp(s, "DropRamp_15deg", Dev("DropRamp"), new Vector3(30f + 2f / Mathf.Tan(15f * Mathf.Deg2Rad), 0f, 24f), Vector3.left, 12f, 15f, 2f);

            var slalom = Section("C06_Slalom");
            for (int col = 0; col < 5; col++)
            for (int row = 0; row < 3; row++)
            {
                float x = 42f + col * 4f + (row % 2) * 2f;
                float z = 20f + row * 4f;
                string name = $"Pillar_{col}{row}";
                Block(slalom, name, (col + row) % 4 == 0 ? DevOrange : Dev(name),
                    new Vector3(x - 0.4f, 0f, z - 0.4f), new Vector3(x + 0.4f, 4f, z + 0.4f));
            }

            // Curved wall: quarter circle (radius 15 around 62, 12) turning the route from +X to -Z.
            var curve = Section("C07_CurveTurn");
            var center = new Vector3(62f, 0f, 12f);
            const float radius = 15f, segment = 2f, height = 6f;
            int count = Mathf.CeilToInt(radius * Mathf.PI * 0.5f / segment);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(90f, 0f, (i + 0.5f) / count) * Mathf.Deg2Rad;
                var p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                var tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                var go = SolidPrimitive(PrimitiveType.Cube, $"Curve_{i + 1:00}", curve, Mat(DevOrange));
                go.transform.SetPositionAndRotation(p + Vector3.up * height * 0.5f, Quaternion.LookRotation(tangent));
                go.transform.localScale = new Vector3(0.5f, height, segment + 0.15f);
            }

            // Corridor south: 6 m between walls (x 71 and 77), 10 m panels alternating grey/orange.
            var corridor = Section("C08_Corridor");
            for (int i = 0; i < 4; i++)
            {
                float zTop = 12f - i * 10f;
                Block(corridor, $"East_{i + 1}", i % 2 == 0 ? Dev($"East_{i}") : DevOrange,
                    new Vector3(77f, 0f, zTop - 10f), new Vector3(77.5f, 6f, zTop));
                Block(corridor, $"West_{i + 1}", i % 2 == 1 ? Dev($"West_{i}") : DevOrange,
                    new Vector3(70.5f, 0f, zTop - 10f), new Vector3(71f, 6f, zTop));
            }
        }

        static void BuildReturnLane()
        {
            // Heads west along z -72..-62 from the corridor exit, rejoining the start straight before the hurdles.
            var s = Section("C09_ReturnLane");
            const float zMin = -72f, zMax = -62f;
            Block(s, "Rail_South", DevMid, new Vector3(8f, 0f, zMin - 0.3f), new Vector3(78f, 1.2f, zMin));
            Block(s, "Rail_North", DevMid, new Vector3(8f, 0f, zMax), new Vector3(70.5f, 1.2f, zMax + 0.3f));

            Block(s, "Vault_0.8m", DevOrange, new Vector3(64f, 0f, zMin), new Vector3(65f, 0.8f, zMax));
            Block(s, "Vault_1.2m", DevOrange, new Vector3(58f, 0f, zMin), new Vector3(59f, 1.2f, zMax));

            // Stair hump heading west: up 1.5 m, a 3 m top, down again. Six 0.25 m steps = 2.1 m run each side.
            const float humpStart = 52f, humpRun = 6f * 0.35f, humpTop = 3f;
            float zMid = (zMin + zMax) * 0.5f;
            Stairs(s, "Hump_Up", Dev("Hump"), new Vector3(humpStart, 0f, zMid), Vector3.left, 12f, 0.25f, 0.35f, 1.5f);
            float topEast = humpStart - humpRun, topWest = topEast - humpTop;
            Block(s, "Hump_Top", Dev("HumpTop"), new Vector3(topWest, 0f, zMin), new Vector3(topEast, 1.5f, zMax));
            Stairs(s, "Hump_Down", Dev("Hump"), new Vector3(topWest - humpRun, 0f, zMid), Vector3.right, 12f, 0.25f, 0.35f, 1.5f);

            // Slide tunnel: 8 m long, 1.25 m clearance.
            Block(s, "SlideTunnel", DevOrange, new Vector3(28f, 1.25f, zMin), new Vector3(36f, 3.5f, zMax));
            Block(s, "SlideTunnel_Top", DevDark, new Vector3(28f, 3.5f, zMin), new Vector3(36f, 3.6f, zMax));

            // Step-ups: 0.5 / 1.0 / 1.5 m blocks, then drop into the start straight.
            float[] steps = { 0.5f, 1f, 1.5f };
            for (int i = 0; i < steps.Length; i++)
            {
                float x = 22f - i * 3f;
                Block(s, $"StepUp_{steps[i]:0.0}m", i == 2 ? DevOrange : Dev($"StepUp{i}"),
                    new Vector3(x - 3f, 0f, zMin), new Vector3(x, steps[i], zMax));
            }
        }

        // ---------- test bay (west) ----------

        static void BuildTestBay()
        {
            var s = Section("TestBay_StairsAndRamps");
            // Tier 1 (3 m) reached by stairs of three rises and ramps of four angles, all ending at z = 10.
            Block(s, "Tier1_3m", Dev("Tier1"), new Vector3(-72f, 0f, 10f), new Vector3(-34f, 3f, 23f));
            StairsEndingAt(s, "Stairs_Rise15cm", new Vector3(-37f, 0f, 10f), 0.15f);
            StairsEndingAt(s, "Stairs_Rise25cm", new Vector3(-42f, 0f, 10f), 0.25f);
            StairsEndingAt(s, "Stairs_Rise30cm_Limit", new Vector3(-47f, 0f, 10f), 0.30f); // = step offset
            RampEndingAt(s, "Ramp_15deg", new Vector3(-52.5f, 0f, 10f), 15f);
            RampEndingAt(s, "Ramp_30deg", new Vector3(-57.5f, 0f, 10f), 30f);
            RampEndingAt(s, "Ramp_44deg", new Vector3(-62.5f, 0f, 10f), 44f);
            RampEndingAt(s, "Ramp_50deg_TooSteep", new Vector3(-67.5f, 0f, 10f), 50f); // past the 45 deg slope limit

            var t = Section("TestBay_TiersAndTower");
            Stairs(t, "Stairs_Tier1to2", Dev("StairsT2"), new Vector3(-49f, 3f, 23f), Vector3.forward, 4f, 0.25f, 0.35f, 3f);
            Block(t, "Tier2_6m", Dev("Tier2"), new Vector3(-72f, 0f, 27f), new Vector3(-46f, 6f, 39f));
            Stairs(t, "Stairs_Tier2toBridge", Dev("StairsBridge"), new Vector3(-68f, 6f, 27.5f), Vector3.forward, 4f, 0.25f, 0.35f, 4f);
            Block(t, "Bridge_10m", Dev("Bridge"), new Vector3(-70f, 9.7f, 33.1f), new Vector3(-66f, 10f, 40f));
            Block(t, "Tower_10m", DevDark, new Vector3(-72f, 0f, 40f), new Vector3(-64f, 10f, 48f));
            Block(t, "TowerTop_Goal", DevOrange, new Vector3(-72f, 10f, 40f), new Vector3(-64f, 10.2f, 48f));
            var orb = SolidPrimitive(PrimitiveType.Sphere, "GoalOrb", t, Mat("Mat_Emissive_Orange"));
            orb.transform.position = new Vector3(-68f, 11.2f, 44f);
            orb.transform.localScale = Vector3.one * 2f;

            var g = Section("TestBay_JumpGaps");
            // Start platform at 2 m, then 3 m platforms after gaps of 2, 3, 4, 5 m heading +X.
            Stairs(g, "GapStairs", Dev("GapStairs"), new Vector3(-72f, 0f, -30.6f), Vector3.forward, 4f, 0.25f, 0.35f, 2f);
            Block(g, "GapStart", Dev("GapStart"), new Vector3(-76f, 0f, -27.8f), new Vector3(-68f, 2f, -21f));
            float edge = -68f;
            foreach (float gap in new[] { 2f, 3f, 4f, 5f })
            {
                float left = edge + gap;
                Block(g, $"JumpGap_{gap:0}m", gap >= 4f ? DevOrange : Dev($"Gap{gap}"),
                    new Vector3(left, 1f, -27.8f), new Vector3(left + 3f, 2f, -21f));
                edge = left + 3f;
            }

            var c = Section("TestBay_CurbsAndVaults");
            float[] curbs = { 0.1f, 0.2f, 0.3f, 0.35f, 0.5f, 0.75f };
            for (int i = 0; i < curbs.Length; i++)
            {
                float x = -72f + i * 3.5f;
                Block(c, $"Curb_{curbs[i] * 100f:0}cm", Dev($"Curb{i}"), new Vector3(x, 0f, -45f), new Vector3(x + 2.5f, curbs[i], -42f));
            }
            float[] vaults = { 0.5f, 1f, 1.5f, 2f, 2.5f };
            for (int i = 0; i < vaults.Length; i++)
            {
                float x = -72f + i * 4f;
                Block(c, $"Vault_{vaults[i]:0.0}m", i % 2 == 0 ? DevOrange : Dev($"Vault{i}"),
                    new Vector3(x, 0f, -56f), new Vector3(x + 2f, vaults[i], -53f));
            }

            var w = Section("TestBay_WallRunWall");
            Block(w, "FreeWallRunWall", DevOrange, new Vector3(-30.5f, 0f, -60f), new Vector3(-30f, 6f, -20f));

            var sample = Section("TestBay_GreyboxSample");
            Place(sample, "Greybox_Wall_4m", new Vector3(-28.25f, 0f, 34f));
            Place(sample, "Greybox_DoorFrame_2m", new Vector3(-25f, 0f, 34f));
            Place(sample, "Greybox_Wall_4m", new Vector3(-21.75f, 0f, 34f));
            for (int i = 0; i < 4; i++)
            {
                var stepGo = Place(sample, "Greybox_Step_1m", new Vector3(-17f, 0f, 37f + i * 0.3f));
                stepGo.transform.localScale = new Vector3(1f, 1f + i, 1f);
            }
            Place(sample, "Greybox_Block_1m", new Vector3(-29f, 0f, 38f));
            Place(sample, "Greybox_ScaleRef_3m", new Vector3(-22.5f, 0f, 38f));
        }

        // ---------- helpers ----------

        /// <summary>A 10 m-wide orange slide-under bar (1.25 m clearance above <paramref name="groundY"/>) on grey posts.</summary>
        static void SlideBar(Transform parent, string name, float z, float groundY)
        {
            Block(parent, name, DevOrange, new Vector3(-5f, groundY + 1.25f, z - 0.5f), new Vector3(5f, groundY + 2.25f, z + 0.5f));
            // Posts stand on the arena floor (y 0) so bars on raised decks don't float.
            Block(parent, name + "_PostL", DevMid, new Vector3(-5.6f, 0f, z - 0.5f), new Vector3(-5f, groundY + 2.25f, z + 0.5f));
            Block(parent, name + "_PostR", DevMid, new Vector3(5f, 0f, z - 0.5f), new Vector3(5.6f, groundY + 2.25f, z + 0.5f));
        }

        /// <summary>
        /// Picks one of the three dev greys from the name, stable across rebuilds (FNV-1a hash, not
        /// string.GetHashCode, which can change between runtimes).
        /// </summary>
        static string Dev(string name)
        {
            uint hash = 2166136261;
            foreach (char ch in name) { hash ^= ch; hash *= 16777619; }
            switch (hash % 3)
            {
                case 0: return DevLight;
                case 1: return DevMid;
                default: return DevDark;
            }
        }

        static Transform Section(string name) => new GameObject(name).transform;

        /// <summary>An axis-aligned solid box between two corners, with a collider.</summary>
        static GameObject Block(Transform parent, string name, string material, Vector3 min, Vector3 max)
        {
            var go = SolidPrimitive(PrimitiveType.Cube, name, parent, Mat(material));
            go.transform.position = (min + max) * 0.5f;
            go.transform.localScale = max - min;
            return go;
        }

        /// <summary>
        /// Solid stairs climbing <paramref name="height"/> along <paramref name="direction"/> from
        /// <paramref name="start"/> (bottom-centre of the first step). Each step is a full-height
        /// block, so there are no gaps to fall through.
        /// </summary>
        static void Stairs(Transform parent, string name, string material, Vector3 start, Vector3 direction,
            float width, float rise, float tread, float height)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(start, Quaternion.LookRotation(direction));
            int steps = Mathf.CeilToInt(height / rise - 0.001f);
            for (int i = 0; i < steps; i++)
            {
                float top = Mathf.Min(height, (i + 1) * rise);
                var step = SolidPrimitive(PrimitiveType.Cube, $"Step_{i + 1:00}", root, Mat(material));
                step.transform.localPosition = new Vector3(0f, top * 0.5f, i * tread + tread * 0.5f);
                step.transform.localScale = new Vector3(width, top, tread);
            }
        }

        /// <summary>4 m-wide stairs up to 3 m, ending exactly at <paramref name="end"/> (top edge, centre).</summary>
        static void StairsEndingAt(Transform parent, string name, Vector3 end, float rise)
        {
            int steps = Mathf.CeilToInt(3f / rise - 0.001f);
            const float tread = 0.35f;
            Stairs(parent, name, Dev(name), end - Vector3.forward * (steps * tread), Vector3.forward, 4f, rise, tread, 3f);
        }

        /// <summary>
        /// A slab ramp rising <paramref name="height"/> at <paramref name="angle"/> degrees along
        /// <paramref name="direction"/>, starting at ground level at <paramref name="start"/>.
        /// Returns its horizontal run.
        /// </summary>
        static float Ramp(Transform parent, string name, string material, Vector3 start, Vector3 direction,
            float width, float angle, float height)
        {
            const float thickness = 0.4f;
            float rad = angle * Mathf.Deg2Rad;
            float slope = height / Mathf.Sin(rad);
            float run = height / Mathf.Tan(rad);

            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(start, Quaternion.LookRotation(direction));
            var slab = SolidPrimitive(PrimitiveType.Cube, "Slab", root, Mat(material));
            var tilt = Quaternion.Euler(-angle, 0f, 0f);
            var topCentre = new Vector3(0f, height * 0.5f, run * 0.5f);
            slab.transform.localRotation = tilt;
            slab.transform.localPosition = topCentre - tilt * Vector3.up * (thickness * 0.5f);
            slab.transform.localScale = new Vector3(width, thickness, slope);
            return run;
        }

        /// <summary>4 m-wide ramp up to 3 m, ending exactly at <paramref name="end"/> (top edge, centre).</summary>
        static void RampEndingAt(Transform parent, string name, Vector3 end, float angle)
        {
            float run = 3f / Mathf.Tan(angle * Mathf.Deg2Rad);
            Ramp(parent, name, Dev(name), end - Vector3.forward * run, Vector3.forward, 4f, angle, 3f);
        }
    }
}
