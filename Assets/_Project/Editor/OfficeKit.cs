using System.Collections.Generic;
using Kit.Core.Config;
using Kit.Gameplay;
using Kit.Gameplay.Flow;
using Kit.Gameplay.Letterboards;
using Kit.Gameplay.Look;
using Kit.Gameplay.Signage;
using Kit.Gameplay.Thresholds;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Editor
{
    /// <summary>
    /// The building blocks every OOO area builder uses: boxes, walls with openings, floor slabs
    /// with holes, stairs, ramps, railings, numbered thresholds, letterboards, signs, props,
    /// lights, light shafts and zones. All positions are world space (metres) unless a parameter
    /// says otherwise. Area builders call only these (plus plain Unity calls for anything exotic),
    /// so geometry, materials and component setup stay consistent across areas built in parallel.
    /// Missing materials and props fall back to greybox stand-ins and are listed in the build log.
    /// </summary>
    public static class OfficeKit
    {
        public const string Root = "Assets/_Project";
        public const string ConfigDir = Root + "/Config";
        public const string OfficeMaterialDir = Root + "/Materials/Office";
        public const string PropPrefabDir = Root + "/Prefabs/Office/Props";
        public const string FogProfileDir = Root + "/Settings/FogZones";

        /// <summary>
        /// Office material names (assets at Materials/Office/&lt;name&gt;.mat, built by the Look
        /// worker in OfficeBuilder.Look.cs). Use these constants, never literal names.
        /// </summary>
        public static class M
        {
            public const string Concrete = "Mat_Office_Concrete";            // board-marked cool grey panels (walls)
            public const string ConcreteDark = "Mat_Office_ConcreteDark";    // soffits, shadowed structure
            public const string ConcreteLight = "Mat_Office_ConcreteLight";  // sunlit slabs, parapets
            public const string ConcreteFloor = "Mat_Office_ConcreteFloor";  // polished concrete floor
            public const string Terrazzo = "Mat_Office_Terrazzo";            // lobby floor
            public const string Slate = "Mat_Office_Slate";                  // dark stone floor (brutal_02)
            public const string CarpetOxblood = "Mat_Office_CarpetOxblood";  // control_01 red carpet
            public const string CarpetGrey = "Mat_Office_CarpetGrey";        // open-plan carpet (Office_01)
            public const string WoodVeneer = "Mat_Office_WoodVeneer";        // warm wall panelling
            public const string WoodDark = "Mat_Office_WoodDark";            // boardroom table, frames
            public const string CeilingTile = "Mat_Office_CeilingTile";      // acoustic tile grid
            public const string Beige = "Mat_Office_Beige";                  // cubicle partitions, old computers
            public const string ChairOrange = "Mat_Office_ChairOrange";      // shell chairs (#B8632E)
            public const string BenchGreen = "Mat_Office_BenchGreen";        // severance_01 buttoned benches
            public const string FloorInlay = "Mat_Office_FloorInlay";        // yellow-green lobby inlay
            public const string Plant = "Mat_Office_Plant";                  // philodendron leaves
            public const string PlantDark = "Mat_Office_PlantDark";
            public const string Soil = "Mat_Office_Soil";
            public const string Metal = "Mat_Office_Metal";                  // brushed steel
            public const string MetalDark = "Mat_Office_MetalDark";          // black chair legs, mullions, rails
            public const string Brass = "Mat_Office_Brass";
            public const string Felt = "Mat_Office_Felt";                    // letterboard backing
            public const string TileWhite = "Mat_Office_TileWhite";          // letterboard tiles, bathroom tile
            public const string Glass = "Mat_Office_Glass";                  // transparent glazing
            public const string Crt = "Mat_Office_CRT";                      // emissive green screen
            public const string LightPanel = "Mat_Office_LightPanel";        // emissive cool troffer
            public const string LightWarm = "Mat_Office_LightWarm";          // emissive warm lamp / lit doorway
            public const string Whiteboard = "Mat_Office_Whiteboard";
            public const string Paper = "Mat_Office_Paper";
            public const string Cork = "Mat_Office_Cork";
            public const string Frame = "Mat_Office_Frame";                  // door and window frames
            public const string Leather = "Mat_Office_Leather";              // dark leather sofas
            public const string Plastic = "Mat_Office_Plastic";              // beige plastic housings
            public const string Exterior = "Mat_Office_Exterior";            // the pale ground outside
        }

        /// <summary>
        /// Prop ids (prefabs at Prefabs/Office/Props/&lt;id&gt;.prefab, built by the Props worker).
        /// Size is the nominal bounding box (m) used for placeholders until the prefab exists.
        /// Pivots are bottom-centre and props face +Z.
        /// </summary>
        public static class PropIds
        {
            public const string ShellChair = "Prop_ShellChair";
            public const string ShellChairRow = "Prop_ShellChairRow";          // four shell chairs on a beam
            public const string RoundTable = "Prop_RoundTable";
            public const string FoldingChair = "Prop_FoldingChair";
            public const string SwivelChair = "Prop_SwivelChair";
            public const string Desk = "Prop_Desk";
            public const string CrtMonitor = "Prop_CrtMonitor";
            public const string Keyboard = "Prop_Keyboard";
            public const string CubicleWorkstation = "Prop_CubicleWorkstation"; // partitions on three sides, desk, CRT, chair
            public const string CubiclePartition = "Prop_CubiclePartition";
            public const string FilingCabinet = "Prop_FilingCabinet";
            public const string ArchiveShelf = "Prop_ArchiveShelf";             // floor-to-ceiling records shelving
            public const string Planter = "Prop_Planter";                       // long concrete trough with philodendrons
            public const string PottedPlant = "Prop_PottedPlant";
            public const string Tree = "Prop_Tree";                             // lone atrium tree
            public const string Bench = "Prop_Bench";                           // green buttoned bench
            public const string LeatherSofa = "Prop_LeatherSofa";
            public const string SideTable = "Prop_SideTable";
            public const string VendingMachine = "Prop_VendingMachine";
            public const string WaterCooler = "Prop_WaterCooler";
            public const string CoffeeTrolley = "Prop_CoffeeTrolley";
            public const string WallClock = "Prop_WallClock";
            public const string Corkboard = "Prop_Corkboard";
            public const string Whiteboard = "Prop_Whiteboard";
            public const string BoardroomTable = "Prop_BoardroomTable";
            public const string ReceptionDesk = "Prop_ReceptionDesk";
            public const string ServingCounter = "Prop_ServingCounter";
            public const string TrayStack = "Prop_TrayStack";
            public const string Bin = "Prop_Bin";
            public const string PaperStack = "Prop_PaperStack";
            public const string DeskLamp = "Prop_DeskLamp";
            public const string Telephone = "Prop_Telephone";
            public const string CoatRack = "Prop_CoatRack";
            public const string TicketDispenser = "Prop_TicketDispenser";       // "take a number"
            public const string Turnstile = "Prop_Turnstile";
            public const string Flag = "Prop_Flag";                             // flag on a pole, folded

            static readonly Dictionary<string, Vector3> sizes = new Dictionary<string, Vector3>
            {
                { ShellChair, new Vector3(0.5f, 0.8f, 0.55f) }, { ShellChairRow, new Vector3(2.3f, 0.8f, 0.6f) },
                { RoundTable, new Vector3(1.2f, 0.74f, 1.2f) }, { FoldingChair, new Vector3(0.45f, 0.8f, 0.5f) },
                { SwivelChair, new Vector3(0.6f, 1.0f, 0.6f) }, { Desk, new Vector3(1.4f, 0.74f, 0.7f) },
                { CrtMonitor, new Vector3(0.4f, 0.38f, 0.42f) }, { Keyboard, new Vector3(0.45f, 0.03f, 0.15f) },
                { CubicleWorkstation, new Vector3(2f, 1.6f, 2f) }, { CubiclePartition, new Vector3(2f, 1.6f, 0.06f) },
                { FilingCabinet, new Vector3(0.5f, 1.3f, 0.65f) }, { ArchiveShelf, new Vector3(2f, 3f, 0.5f) },
                { Planter, new Vector3(2.4f, 1.4f, 0.8f) }, { PottedPlant, new Vector3(0.5f, 1.2f, 0.5f) },
                { Tree, new Vector3(3f, 4.5f, 3f) }, { Bench, new Vector3(2f, 0.8f, 0.7f) },
                { LeatherSofa, new Vector3(2f, 0.8f, 0.8f) }, { SideTable, new Vector3(0.5f, 0.55f, 0.5f) },
                { VendingMachine, new Vector3(0.9f, 1.9f, 0.8f) }, { WaterCooler, new Vector3(0.35f, 1.2f, 0.35f) },
                { CoffeeTrolley, new Vector3(0.8f, 0.9f, 0.5f) }, { WallClock, new Vector3(0.35f, 0.35f, 0.05f) },
                { Corkboard, new Vector3(1.2f, 0.9f, 0.03f) }, { Whiteboard, new Vector3(1.8f, 1.2f, 0.05f) },
                { BoardroomTable, new Vector3(6f, 0.75f, 1.6f) }, { ReceptionDesk, new Vector3(3f, 1.1f, 0.8f) },
                { ServingCounter, new Vector3(3f, 0.95f, 0.8f) }, { TrayStack, new Vector3(0.45f, 0.3f, 0.35f) },
                { Bin, new Vector3(0.35f, 0.6f, 0.35f) }, { PaperStack, new Vector3(0.3f, 0.1f, 0.22f) },
                { DeskLamp, new Vector3(0.2f, 0.45f, 0.3f) }, { Telephone, new Vector3(0.22f, 0.1f, 0.2f) },
                { CoatRack, new Vector3(0.5f, 1.8f, 0.5f) }, { TicketDispenser, new Vector3(0.3f, 1.3f, 0.3f) },
                { Turnstile, new Vector3(0.3f, 1f, 0.9f) }, { Flag, new Vector3(0.4f, 2.2f, 0.4f) },
            };

            /// <summary>All prop ids, for the props builder and tests.</summary>
            public static IEnumerable<string> All => sizes.Keys;

            /// <summary>Nominal size of a prop (placeholder box); 1 m cube for unknown ids.</summary>
            public static Vector3 SizeOf(string id) => sizes.TryGetValue(id, out var s) ? s : Vector3.one;
        }

        /// <summary>A rectangular opening in a Wall: Start = distance along the wall from its first point, Bottom = height above the wall's base.</summary>
        public readonly struct Opening
        {
            public readonly float Start, Width, Bottom, Height;
            public Opening(float start, float width, float bottom, float height) { Start = start; Width = width; Bottom = bottom; Height = height; }

            /// <summary>A door-sized opening (from OfficeLayout.Door) starting at <paramref name="start"/>.</summary>
            public static Opening Door(float start) => new Opening(start, OfficeLayout.Door.x, 0f, OfficeLayout.Door.y);
        }

        static readonly HashSet<string> missingMaterials = new HashSet<string>();
        static readonly HashSet<string> missingProps = new HashSet<string>();
        static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        static readonly Dictionary<string, GameObject> propCache = new Dictionary<string, GameObject>();

        /// <summary>Clears per-build caches and the missing-asset lists. Called at the start of OfficeBuilder.BuildOffice.</summary>
        public static void BeginBuild()
        {
            missingMaterials.Clear();
            missingProps.Clear();
            materialCache.Clear();
            propCache.Clear();
        }

        /// <summary>One-line report of stand-ins used in this build (empty when nothing was missing).</summary>
        public static string MissingReport()
        {
            if (missingMaterials.Count == 0 && missingProps.Count == 0) return "";
            return $"materials missing: [{string.Join(", ", missingMaterials)}]; props missing: [{string.Join(", ", missingProps)}]";
        }

        // ------------------------------------------------------------------ assets

        /// <summary>Office material by name (see M); Mat_Grey_Wall stand-in when it doesn't exist yet.</summary>
        public static Material Mat(string name)
        {
            if (materialCache.TryGetValue(name, out var cached) && cached != null) return cached;
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{OfficeMaterialDir}/{name}.mat")
                    ?? AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/{name}.mat");
            if (m == null)
            {
                missingMaterials.Add(name);
                m = AssetDatabase.LoadAssetAtPath<Material>($"{Root}/Materials/Mat_Grey_Wall.mat");
            }
            materialCache[name] = m;
            return m;
        }

        /// <summary>The config asset of type T at Config/&lt;TypeName&gt;.asset, created with class defaults if missing.</summary>
        public static T Config<T>() where T : ConfigAsset
        {
            string path = $"{ConfigDir}/{typeof(T).Name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void MarkStatic(GameObject go) => GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

        static GameObject Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        // ------------------------------------------------------------------ geometry

        /// <summary>A static solid box between two world corners, with a BoxCollider unless <paramref name="collider"/> is false.</summary>
        public static GameObject Box(Transform parent, string name, string material, Vector3 min, Vector3 max, bool collider = true)
        {
            var lo = Vector3.Min(min, max);
            var hi = Vector3.Max(min, max);
            return BoxAt(parent, name, material, (lo + hi) * 0.5f, hi - lo, Quaternion.identity, collider);
        }

        /// <summary>A static solid box by centre, size and rotation (world).</summary>
        public static GameObject BoxAt(Transform parent, string name, string material, Vector3 centre, Vector3 size, Quaternion rotation, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(centre, rotation);
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            MarkStatic(go);
            return go;
        }

        /// <summary>
        /// A straight wall from <paramref name="a"/> to <paramref name="b"/> (their y is ignored),
        /// centred on that line, from <paramref name="baseY"/> up <paramref name="height"/> m, with
        /// rectangular openings cut out (doors, windows, transoms). Openings must not overlap.
        /// </summary>
        public static Transform Wall(Transform parent, string name, string material, Vector3 a, Vector3 b, float baseY, float height,
            float thickness, params Opening[] openings)
        {
            var root = Child(parent, name).transform;
            var flatA = new Vector3(a.x, baseY, a.z);
            var flatB = new Vector3(b.x, baseY, b.z);
            float length = Vector3.Distance(flatA, flatB);
            if (length < 0.001f) return root;
            root.SetPositionAndRotation(flatA, Quaternion.LookRotation((flatB - flatA).normalized, Vector3.up));

            var sorted = new List<Opening>(openings ?? new Opening[0]);
            sorted.Sort((x, y) => x.Start.CompareTo(y.Start));

            // Local frame: +Z runs along the wall, +X is its thickness, +Y up.
            void Piece(string pieceName, float z0, float z1, float y0, float y1)
            {
                if (z1 - z0 < 0.001f || y1 - y0 < 0.001f) return;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = pieceName;
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(0f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f);
                go.transform.localScale = new Vector3(thickness, y1 - y0, z1 - z0);
                go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
                MarkStatic(go);
            }

            float cursor = 0f;
            int i = 0;
            foreach (var o in sorted)
            {
                float s = Mathf.Clamp(o.Start, 0f, length), e = Mathf.Clamp(o.Start + o.Width, 0f, length);
                Piece($"Solid_{i}", cursor, s, 0f, height);
                Piece($"Sill_{i}", s, e, 0f, Mathf.Min(o.Bottom, height));
                Piece($"Lintel_{i}", s, e, Mathf.Min(o.Bottom + o.Height, height), height);
                cursor = Mathf.Max(cursor, e);
                i++;
            }
            Piece($"Solid_{i}", cursor, length, 0f, height);
            return root;
        }

        /// <summary>
        /// A horizontal slab whose top is at <paramref name="topY"/>, covering the XZ rectangle
        /// from <paramref name="min"/> to <paramref name="max"/> (their y ignored), minus rectangular
        /// holes (each given as min/max XZ corners in a Vector4: x0, z0, x1, z1). Used for floors,
        /// ceilings and galleries; holes make voids, hatches and stairwells.
        /// </summary>
        public static Transform Slab(Transform parent, string name, string material, Vector3 min, Vector3 max, float topY, float thickness,
            params Vector4[] holes)
        {
            var root = Child(parent, name).transform;
            float x0 = Mathf.Min(min.x, max.x), x1 = Mathf.Max(min.x, max.x), z0 = Mathf.Min(min.z, max.z), z1 = Mathf.Max(min.z, max.z);
            var xs = new SortedSet<float> { x0, x1 };
            var zs = new SortedSet<float> { z0, z1 };
            foreach (var h in holes ?? new Vector4[0])
            {
                xs.Add(Mathf.Clamp(Mathf.Min(h.x, h.z), x0, x1)); xs.Add(Mathf.Clamp(Mathf.Max(h.x, h.z), x0, x1));
                zs.Add(Mathf.Clamp(Mathf.Min(h.y, h.w), z0, z1)); zs.Add(Mathf.Clamp(Mathf.Max(h.y, h.w), z0, z1));
            }
            var xl = new List<float>(xs);
            var zl = new List<float>(zs);

            bool InHole(float cx, float cz)
            {
                foreach (var h in holes ?? new Vector4[0])
                    if (cx > Mathf.Min(h.x, h.z) && cx < Mathf.Max(h.x, h.z) && cz > Mathf.Min(h.y, h.w) && cz < Mathf.Max(h.y, h.w)) return true;
                return false;
            }

            // Grid cells around the holes, merged along X into strips to keep the box count low.
            int n = 0;
            for (int zi = 0; zi < zl.Count - 1; zi++)
            {
                float cz = (zl[zi] + zl[zi + 1]) * 0.5f;
                int xi = 0;
                while (xi < xl.Count - 1)
                {
                    if (InHole((xl[xi] + xl[xi + 1]) * 0.5f, cz)) { xi++; continue; }
                    int start = xi;
                    while (xi < xl.Count - 1 && !InHole((xl[xi] + xl[xi + 1]) * 0.5f, cz)) xi++;
                    Box(root, $"Cell_{n++}", material, new Vector3(xl[start], topY - thickness, zl[zi]), new Vector3(xl[xi], topY, zl[zi + 1]));
                }
            }
            return root;
        }

        /// <summary>
        /// Solid stairs: <paramref name="steps"/> steps of <paramref name="rise"/> by <paramref name="tread"/>,
        /// climbing along <paramref name="direction"/> from <paramref name="start"/> (bottom-front-centre
        /// of the first step). Each step is a full-height block, so there are no gaps underneath.
        /// Returns the top-back-centre point (where the landing begins).
        /// </summary>
        public static Vector3 Stairs(Transform parent, string name, string material, Vector3 start, Vector3 direction, float width,
            int steps, float rise = 0.2f, float tread = 0.3f)
        {
            var root = Child(parent, name).transform;
            var dir = new Vector3(direction.x, 0f, direction.z).normalized;
            root.SetPositionAndRotation(start, Quaternion.LookRotation(dir, Vector3.up));
            for (int i = 0; i < steps; i++)
            {
                float top = (i + 1) * rise;
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Step_{i + 1:00}";
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(0f, top * 0.5f, i * tread + tread * 0.5f);
                go.transform.localScale = new Vector3(width, top, tread);
                go.GetComponent<MeshRenderer>().sharedMaterial = Mat(material);
                MarkStatic(go);
            }
            return start + dir * (steps * tread) + Vector3.up * (steps * rise);
        }

        /// <summary>
        /// A slab ramp rising at <paramref name="angleDeg"/> along <paramref name="direction"/> from
        /// <paramref name="start"/> (bottom edge centre) for a horizontal run of <paramref name="run"/>.
        /// Steeper than 45 deg is a surf face. Returns the top edge centre.
        /// </summary>
        public static Vector3 Ramp(Transform parent, string name, string material, Vector3 start, Vector3 direction, float width,
            float run, float angleDeg, float thickness = 0.4f)
        {
            var dir = new Vector3(direction.x, 0f, direction.z).normalized;
            float height = run * Mathf.Tan(angleDeg * Mathf.Deg2Rad);
            float slope = Mathf.Sqrt(run * run + height * height);
            var rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-angleDeg, 0f, 0f);
            var topCentre = start + dir * (run * 0.5f) + Vector3.up * (height * 0.5f);
            BoxAt(parent, name, material, topCentre - rot * Vector3.up * (thickness * 0.5f), new Vector3(width, thickness, slope), rot);
            return start + dir * run + Vector3.up * height;
        }

        /// <summary>A railing from <paramref name="a"/> to <paramref name="b"/> (at their y): posts every 1.5 m, a top rail, and an optional glass infill panel.</summary>
        public static Transform Railing(Transform parent, string name, Vector3 a, Vector3 b, float height = 1.05f, bool glass = true)
        {
            var root = Child(parent, name).transform;
            float length = Vector3.Distance(a, b);
            if (length < 0.01f) return root;
            var dir = (b - a) / length;
            var rot = Quaternion.LookRotation(dir, Vector3.up);
            int posts = Mathf.Max(1, Mathf.CeilToInt(length / 1.5f));
            for (int i = 0; i <= posts; i++)
                BoxAt(root, $"Post_{i}", M.MetalDark, a + dir * (length * i / posts) + Vector3.up * (height * 0.5f), new Vector3(0.05f, height, 0.05f), rot, false);
            BoxAt(root, "Rail", M.MetalDark, (a + b) * 0.5f + Vector3.up * height, new Vector3(0.06f, 0.06f, length), rot);
            if (glass)
                BoxAt(root, "Glass", M.Glass, (a + b) * 0.5f + Vector3.up * (height * 0.5f), new Vector3(0.02f, height - 0.1f, length), rot);
            else
                BoxAt(root, "Blocker", M.MetalDark, (a + b) * 0.5f + Vector3.up * (height * 0.5f), new Vector3(0.02f, height, length), rot).GetComponent<MeshRenderer>().enabled = false;
            return root;
        }

        // ------------------------------------------------------------------ thresholds and lettering

        /// <summary>
        /// A numbered opening. <paramref name="centre"/> is the middle of the opening; the rotation's
        /// forward is the threshold's FRONT (the side players approach from; see ARCHITECTURE.md).
        /// Builds the frame (jambs and head; all four sides when <paramref name="fullFrame"/>, for
        /// windows and hatches), a Threshold component, and its plate on the front above the head:
        /// a BrassPlate when <paramref name="letterboardSlots"/> is null, otherwise a one-row
        /// Letterboard with those glyphs ('_' = empty slot). The wall opening itself must be cut by
        /// the caller (Wall openings or Slab holes), with 0.6 m clear on both sides.
        /// Only numbers from the ARCHITECTURE.md number plan may be used.
        /// </summary>
        public static Threshold Threshold(Transform parent, string name, Vector3 centre, Quaternion rotation, Vector2 size,
            string brassNumber, string letterboardSlots = null, bool exterior = false, bool fullFrame = false, string frameMaterial = M.Frame)
        {
            var cfg = Config<ThresholdConfig>();
            var go = Child(parent, name);
            go.transform.SetPositionAndRotation(centre, rotation);

            float fw = cfg.FrameWidth, fd = cfg.FrameDepth, w = size.x, h = size.y;
            var frame = Child(go.transform, "Frame").transform;
            void FramePiece(string pieceName, Vector3 localCentre, Vector3 localSize)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = pieceName;
                piece.transform.SetParent(frame, false);
                piece.transform.localPosition = localCentre;
                piece.transform.localScale = localSize;
                piece.GetComponent<MeshRenderer>().sharedMaterial = Mat(frameMaterial);
                MarkStatic(piece);
            }
            float bottom = fullFrame ? -h * 0.5f - fw : -h * 0.5f;
            float jambHeight = h * 0.5f + fw - bottom;
            FramePiece("JambL", new Vector3(-(w + fw) * 0.5f, bottom + jambHeight * 0.5f, 0f), new Vector3(fw, jambHeight, fd));
            FramePiece("JambR", new Vector3((w + fw) * 0.5f, bottom + jambHeight * 0.5f, 0f), new Vector3(fw, jambHeight, fd));
            FramePiece("Head", new Vector3(0f, (h + fw) * 0.5f, 0f), new Vector3(w + fw * 2f, fw, fd));
            if (fullFrame) FramePiece("Sill", new Vector3(0f, -(h + fw) * 0.5f, 0f), new Vector3(w + fw * 2f, fw, fd));

            var surface = Child(go.transform, "PortalSurface").transform;
            var threshold = go.AddComponent<Threshold>();
            threshold.Setup(size, exterior, surface);

            var plate = Child(go.transform, "Plate");
            if (letterboardSlots != null)
            {
                var lb = Config<LetterboardConfig>();
                float plateH = lb.SlotSize.y + lb.TileGap * 2f;
                plate.transform.localPosition = new Vector3(0f, h * 0.5f + fw + plateH * 0.5f + 0.06f, fd * 0.5f + 0.01f);
                plate.AddComponent<Letterboard>().Setup(1, letterboardSlots.Length, letterboardSlots);
            }
            else
            {
                float letter = exterior ? 0.32f : 0.09f;
                plate.transform.localPosition = new Vector3(0f, h * 0.5f + fw + letter * 0.9f + 0.05f, fd * 0.5f + 0.01f);
                plate.AddComponent<BrassPlate>().Setup(brassNumber ?? "", letter);
            }
            return threshold;
        }

        /// <summary>
        /// A free-standing letterboard sign (directory, menu, notice) centred at <paramref name="centre"/>
        /// facing the rotation's forward. <paramref name="content"/> rows are separated by '\n'
        /// ('_' = empty slot). Its tiles can be pulled by the player.
        /// </summary>
        public static Letterboard LetterboardSign(Transform parent, string name, Vector3 centre, Quaternion rotation, int rows, int columns, string content)
        {
            var go = Child(parent, name);
            go.transform.SetPositionAndRotation(centre, rotation);
            var board = go.AddComponent<Letterboard>();
            board.Setup(rows, columns, content);
            return board;
        }

        /// <summary>
        /// Diegetic text (signage, nameplates, memos) at a world position, facing the rotation's
        /// forward. With <paramref name="backing"/> set, a thin panel of that material and size sits
        /// just behind the text. Letter height is the capital height in metres.
        /// </summary>
        public static WorldText Sign(Transform parent, string text, Vector3 position, Quaternion rotation, float letterHeight,
            WorldTextStyle style = WorldTextStyle.DarkOnLight, TextAnchor anchor = TextAnchor.MiddleCenter, string backing = null, Vector2 backingSize = default)
        {
            var root = Child(parent, "Sign_" + Abbrev(text));
            root.transform.SetPositionAndRotation(position, rotation);
            if (backing != null && backingSize != Vector2.zero)
            {
                var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = "Backing";
                panel.transform.SetParent(root.transform, false);
                panel.transform.localPosition = new Vector3(0f, 0f, -0.006f);
                panel.transform.localScale = new Vector3(backingSize.x, backingSize.y, 0.01f);
                panel.GetComponent<MeshRenderer>().sharedMaterial = Mat(backing);
                Object.DestroyImmediate(panel.GetComponent<Collider>());
                MarkStatic(panel);
            }
            // TextMesh faces -Z of its transform, so turn it around to face the sign's forward.
            return WorldText.Create(root.transform, "Text", text, new Vector3(0f, 0f, 0.002f), Quaternion.Euler(0f, 180f, 0f), letterHeight, style, anchor);
        }

        static string Abbrev(string text)
        {
            var s = (text ?? "").Replace('\n', ' ').Trim();
            return s.Length <= 18 ? s : s.Substring(0, 18);
        }

        // ------------------------------------------------------------------ props

        /// <summary>
        /// Places a prop prefab (see PropIds) with its bottom-centre at <paramref name="position"/>,
        /// turned <paramref name="yaw"/> degrees. Until the Props worker's prefab exists, a greybox
        /// stand-in of the nominal size is placed instead.
        /// </summary>
        public static GameObject Prop(Transform parent, string propId, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            if (!propCache.TryGetValue(propId, out var prefab))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropPrefabDir}/{propId}.prefab");
                propCache[propId] = prefab;
            }
            GameObject go;
            if (prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }
            else
            {
                missingProps.Add(propId);
                var size = PropIds.SizeOf(propId);
                go = new GameObject(propId + "_Placeholder");
                go.transform.SetParent(parent, false);
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
                body.transform.localScale = size;
                body.GetComponent<MeshRenderer>().sharedMaterial = Mat("Mat_Grey_Wall");
                MarkStatic(body);
            }
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        // ------------------------------------------------------------------ light

        /// <summary>A realtime point light.</summary>
        public static Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range, bool shadows = false)
        {
            var light = Child(parent, name).AddComponent<Light>();
            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        /// <summary>A realtime spot light at <paramref name="position"/> pointing along the rotation's forward (Quaternion.LookRotation(Vector3.down) points down).</summary>
        public static Light SpotLight(Transform parent, string name, Vector3 position, Quaternion rotation, Color color, float intensity,
            float range, float angle, bool shadows = true)
        {
            var light = Child(parent, name).AddComponent<Light>();
            light.transform.SetPositionAndRotation(position, rotation);
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.55f;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        /// <summary>
        /// A ceiling light fixture: an emissive panel of <paramref name="size"/> (x by z) whose
        /// underside is at <paramref name="position"/>, plus a downward spot light. With
        /// <paramref name="flicker"/>, it gets a FlickerLight (rare, subtle).
        /// </summary>
        public static Light CeilingLight(Transform parent, string name, Vector3 position, Vector2 size, Color color, float intensity,
            float range, bool flicker = false, bool shadows = false, string panelMaterial = M.LightPanel)
        {
            var root = Child(parent, name).transform;
            root.position = position;
            var panel = BoxAt(root, "Panel", panelMaterial, position + Vector3.up * 0.02f, new Vector3(size.x, 0.04f, size.y), Quaternion.identity, false);
            var light = SpotLight(root, "Light", position - Vector3.up * 0.05f, Quaternion.LookRotation(Vector3.down, Vector3.forward), color, intensity, range, 150f, shadows);
            if (flicker) light.gameObject.AddComponent<FlickerLight>().Setup(panel.GetComponent<MeshRenderer>());
            return light;
        }

        /// <summary>A fake volumetric light shaft from <paramref name="top"/> downward (rotation's up is the shaft axis reversed); size = width, length, depth.</summary>
        public static LightShaft LightShaft(Transform parent, string name, Vector3 top, Quaternion rotation, Vector3 size, Color tint)
        {
            var go = Child(parent, name);
            go.transform.SetPositionAndRotation(top, rotation);
            var shaft = go.AddComponent<LightShaft>();
            shaft.Setup(size, tint);
            return shaft;
        }

        // ------------------------------------------------------------------ zones

        /// <summary>An AreaZone: ambience and analytics know the player is in <paramref name="id"/> while inside these bounds.</summary>
        public static AreaZone Zone(Transform parent, string id, Bounds bounds)
        {
            var go = Child(parent, "Zone_" + id);
            go.transform.position = bounds.center;
            var zone = go.AddComponent<AreaZone>();
            zone.Setup(id, bounds.size);
            return zone;
        }

        /// <summary>Entering these bounds counts as falling off the world (respawn at the last safe ground).</summary>
        public static FallZone FallZone(Transform parent, string name, Bounds bounds)
        {
            var go = Child(parent, name);
            go.transform.position = bounds.center;
            var zone = go.AddComponent<FallZone>();
            zone.Setup(bounds.size);
            return zone;
        }

        /// <summary>Standing inside these bounds never counts as safe ground.</summary>
        public static NoSafeGroundZone NoSafeGround(Transform parent, string name, Bounds bounds)
        {
            var go = Child(parent, name);
            go.transform.position = bounds.center;
            var zone = go.AddComponent<NoSafeGroundZone>();
            zone.Setup(bounds.size);
            return zone;
        }

        /// <summary>
        /// A local fog volume: inside <paramref name="bounds"/> (blending over <paramref name="blend"/> m)
        /// the fog becomes this colour and range. Needs LookController to read the blended volume stack.
        /// The profile is saved under Settings/FogZones/ (regenerated every build).
        /// </summary>
        public static Volume FogZone(Transform parent, string name, Bounds bounds, Color color, float start, float end, float blend = 3f, float priority = 10f)
        {
            if (!AssetDatabase.IsValidFolder(FogProfileDir)) AssetDatabase.CreateFolder(Root + "/Settings", "FogZones");
            string path = $"{FogProfileDir}/Fog_{name}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            if (!profile.TryGet(out KitFog fog))
            {
                // Volume components are sub-assets: without AddObjectToAsset the override lives only
                // in memory and vanishes on the next reimport or Editor restart.
                fog = profile.Add<KitFog>(true);
                fog.name = nameof(KitFog);
                AssetDatabase.AddObjectToAsset(fog, profile);
            }
            fog.active = true;
            fog.enabled.Override(true);
            fog.start.Override(start);
            fog.end.Override(end);
            fog.color.Override(color);
            EditorUtility.SetDirty(profile);

            var go = Child(parent, name);
            go.transform.position = bounds.center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = bounds.size;
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.blendDistance = blend;
            volume.priority = priority;
            volume.sharedProfile = profile;
            return volume;
        }

        /// <summary>Rotation facing <paramref name="direction"/> with world up (shorthand for builders).</summary>
        public static Quaternion Facing(Vector3 direction) => Quaternion.LookRotation(direction, Vector3.up);
    }
}
