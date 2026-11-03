using UnityEngine;
using M = Kit.Editor.OfficeKit.M;

namespace Kit.Editor
{
    /// <summary>
    /// Office furniture and desk clutter (Office_01, control_01): desks, the CRT and keyboard,
    /// cubicle partitions and the full workstation, filing cabinet, archive shelving, wall clock,
    /// corkboard, whiteboard, boardroom table, desk lamp, telephone and paper stacks.
    /// </summary>
    public static partial class PropShapes
    {
        /// <summary>Uniform random float in [a, b) from a seeded generator, so rebuilds are identical.</summary>
        static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>A plain work desk: wood veneer top on a black steel frame with a modesty panel and a beige drawer pedestal on the right.</summary>
        static void Desk(MeshKit k)
        {
            k.Use(M.WoodVeneer);
            k.Box(new Vector3(0f, 0.725f, 0f), new Vector3(1.4f, 0.03f, 0.7f));
            k.Use(M.MetalDark);
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                k.Box(new Vector3(sx * 0.665f, 0.355f, sz * 0.315f), new Vector3(0.035f, 0.71f, 0.035f), false);
            k.Box(new Vector3(0f, 0.685f, -0.315f), new Vector3(1.3f, 0.05f, 0.02f));
            k.Box(new Vector3(0f, 0.685f, 0.315f), new Vector3(1.3f, 0.05f, 0.02f));
            k.Box(new Vector3(-0.665f, 0.685f, 0f), new Vector3(0.02f, 0.05f, 0.6f));
            k.Box(new Vector3(0f, 0.47f, -0.305f), new Vector3(1.29f, 0.36f, 0.01f));
            Pedestal(k, new Vector3(0.43f, 0f, 0.0f), 0.4f, 0.66f, 0.6f, 3);
            k.Collider(new Vector3(0f, 0.37f, 0f), new Vector3(1.4f, 0.74f, 0.7f));
        }

        /// <summary>A drawer pedestal (body, proud drawer fronts, steel pulls) standing at <paramref name="foot"/>, fronts facing +Z.</summary>
        static void Pedestal(MeshKit k, Vector3 foot, float w, float h, float d, int drawers)
        {
            k.Use(M.Beige);
            k.Box(foot + new Vector3(0f, 0.04f + (h - 0.04f) * 0.5f, 0f), new Vector3(w, h - 0.04f, d), false);
            float dh = (h - 0.06f) / drawers;
            for (int i = 0; i < drawers; i++)
            {
                float y = 0.05f + dh * i + dh * 0.5f;
                k.Use(M.Beige);
                k.Box(foot + new Vector3(0f, y, d * 0.5f + 0.008f), new Vector3(w - 0.03f, dh - 0.012f, 0.016f));
                k.Use(M.Metal);
                k.Box(foot + new Vector3(0f, y + dh * 0.22f, d * 0.5f + 0.024f), new Vector3(0.1f, 0.014f, 0.016f));
            }
            k.Use(M.MetalDark);
            k.Box(foot + new Vector3(0f, 0.02f, -0.02f), new Vector3(w - 0.04f, 0.04f, d - 0.04f), false);
        }

        /// <summary>
        /// A deep beige CRT: rounded bezel, tapered rear housing, a recessed lip, and a slightly
        /// bulged emissive green screen (M.Crt) on a small swivel stand. No collider (desk clutter).
        /// </summary>
        static void CrtMonitor(MeshKit k)
        {
            k.Use(M.Plastic);
            k.Cylinder(new Vector3(0f, 0f, 0.02f), new Vector3(0f, 0.025f, 0.02f), 0.12f, 0.11f, 16, false, true);
            k.Box(new Vector3(0f, 0.045f, 0.02f), new Vector3(0.12f, 0.03f, 0.12f), false);
            k.RoundedBox(new Vector3(0f, 0.22f, 0.15f), new Vector3(0.40f, 0.32f, 0.12f), 0.02f, 1);
            k.TaperBox(new Vector3(0f, 0.22f, 0.095f), new Vector2(0.37f, 0.29f), new Vector3(0f, 0.235f, -0.205f), new Vector2(0.24f, 0.2f));
            const float sw = 0.30f, sh = 0.225f, sy = 0.228f, lip = 0.018f;
            k.Box(new Vector3(0f, sy + (sh + lip) * 0.5f, 0.216f), new Vector3(sw + lip * 2f, lip, 0.012f));
            k.Box(new Vector3(0f, sy - (sh + lip) * 0.5f, 0.216f), new Vector3(sw + lip * 2f, lip, 0.012f));
            k.Box(new Vector3(-(sw + lip) * 0.5f, sy, 0.216f), new Vector3(lip, sh, 0.012f));
            k.Box(new Vector3((sw + lip) * 0.5f, sy, 0.216f), new Vector3(lip, sh, 0.012f));
            k.Use(M.Beige);
            k.Box(new Vector3(0.15f, 0.085f, 0.212f), new Vector3(0.025f, 0.012f, 0.008f));

            k.Use(M.Crt);
            const int nx = 7, ny = 6;
            var p = new Vector3[nx, ny];
            for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
            {
                float u = -1f + 2f * i / (nx - 1), v = -1f + 2f * j / (ny - 1);
                p[i, j] = new Vector3(u * sw * 0.5f, sy + v * sh * 0.5f, 0.212f + 0.012f * (1f - u * u * 0.7f) * (1f - v * v * 0.7f));
            }
            k.Grid(p, (i, j) => Vector3.forward);
        }

        /// <summary>A beige wedge keyboard with raised key rows and a short space bar.</summary>
        static void Keyboard(MeshKit k)
        {
            k.Use(M.Plastic);
            k.TaperBox(new Vector3(0f, 0.009f, 0.075f), new Vector2(0.45f, 0.018f), new Vector3(0f, 0.014f, -0.075f), new Vector2(0.45f, 0.028f));
            k.Use(M.Beige);
            for (int r = 0; r < 5; r++)
            {
                float z = 0.052f - r * 0.025f;
                float top = Mathf.Lerp(0.018f, 0.028f, (0.075f - z) / 0.15f);
                float w = r == 0 ? 0.2f : 0.40f;
                float x = r == 0 ? -0.04f : -0.01f;
                k.Box(new Vector3(x, top + 0.003f, z), new Vector3(w, 0.008f, 0.02f), false);
            }
            k.Box(new Vector3(0.185f, 0.024f, 0.0f), new Vector3(0.05f, 0.008f, 0.1f), false);
        }

        /// <summary>A free-standing 2 m cubicle partition: beige panel, steel top and end caps, black feet. Lies in the XY plane, faces +Z.</summary>
        static void CubiclePartition(MeshKit k)
        {
            k.Use(M.MetalDark);
            k.Box(new Vector3(-0.88f, 0.015f, 0f), new Vector3(0.05f, 0.03f, 0.3f), false);
            k.Box(new Vector3(0.88f, 0.015f, 0f), new Vector3(0.05f, 0.03f, 0.3f), false);
            k.Box(new Vector3(0f, 0.065f, 0f), new Vector3(1.96f, 0.07f, 0.058f), false);
            k.Use(M.Beige);
            k.Box(new Vector3(0f, 0.1f + 1.47f * 0.5f, 0f), new Vector3(1.96f, 1.47f, 0.056f), false, false);
            k.Use(M.Metal);
            k.Box(new Vector3(0f, 1.585f, 0f), new Vector3(2f, 0.03f, 0.064f));
            k.Box(new Vector3(-0.99f, 0.82f, 0f), new Vector3(0.02f, 1.54f, 0.062f), false, false);
            k.Box(new Vector3(0.99f, 0.82f, 0f), new Vector3(0.02f, 1.54f, 0.062f), false, false);
            k.Collider(new Vector3(0f, 0.8f, 0f), new Vector3(2f, 1.6f, 0.07f));
        }

        /// <summary>
        /// Office_01's cubicle: partitions on the back and both sides (open towards +Z), a
        /// cantilevered work surface with a drawer pedestal, a CRT and keyboard, a telephone, a
        /// paper stack, and a swivel chair pushed back from the desk. One mesh, one draw per material.
        /// </summary>
        static void CubicleWorkstation(MeshKit k)
        {
            k.Push(new Vector3(0f, 0f, -0.97f), Quaternion.identity); CubiclePartition(k); k.Pop();
            k.Push(new Vector3(-0.97f, 0f, 0.02f), Quaternion.Euler(0f, 90f, 0f)); CubiclePartition(k); k.Pop();
            k.Push(new Vector3(0.97f, 0f, 0.02f), Quaternion.Euler(0f, -90f, 0f)); CubiclePartition(k); k.Pop();

            k.Use(M.Plastic);
            k.Box(new Vector3(0f, 0.725f, -0.59f), new Vector3(1.88f, 0.03f, 0.7f));
            k.Use(M.MetalDark);
            k.Box(new Vector3(-0.55f, 0.66f, -0.68f), new Vector3(0.03f, 0.1f, 0.5f), false);
            Pedestal(k, new Vector3(0.68f, 0f, -0.63f), 0.4f, 0.70f, 0.56f, 3);
            k.Collider(new Vector3(0f, 0.37f, -0.59f), new Vector3(1.88f, 0.74f, 0.7f));

            k.Push(new Vector3(-0.28f, 0.74f, -0.72f), Quaternion.Euler(0f, 8f, 0f)); CrtMonitor(k); k.Pop();
            k.Push(new Vector3(-0.24f, 0.74f, -0.40f), Quaternion.Euler(0f, 4f, 0f)); Keyboard(k); k.Pop();
            k.Push(new Vector3(0.55f, 0.74f, -0.74f), Quaternion.Euler(0f, -22f, 0f)); Telephone(k); k.Pop();
            k.Push(new Vector3(0.22f, 0.74f, -0.56f), Quaternion.Euler(0f, 12f, 0f)); PaperStack(k); k.Pop();
            k.Push(new Vector3(-0.12f, 0f, -0.02f), Quaternion.Euler(0f, 168f, 0f)); SwivelChair(k); k.Pop();
        }

        /// <summary>A four-drawer beige steel filing cabinet with pulls and label holders.</summary>
        static void FilingCabinet(MeshKit k)
        {
            k.Use(M.Beige);
            k.Box(new Vector3(0f, 0.65f, -0.01f), new Vector3(0.5f, 1.3f, 0.63f), false);
            for (int i = 0; i < 4; i++)
            {
                float y0 = 0.04f + i * 0.315f;
                k.Use(M.Beige);
                k.Box(new Vector3(0f, y0 + 0.15f, 0.315f), new Vector3(0.46f, 0.30f, 0.02f));
                k.Use(M.Metal);
                k.Box(new Vector3(0f, y0 + 0.19f, 0.336f), new Vector3(0.14f, 0.018f, 0.024f));
                k.Box(new Vector3(-0.055f, y0 + 0.19f, 0.329f), new Vector3(0.012f, 0.018f, 0.014f), false);
                k.Box(new Vector3(0.055f, y0 + 0.19f, 0.329f), new Vector3(0.012f, 0.018f, 0.014f), false);
                k.Box(new Vector3(0f, y0 + 0.245f, 0.327f), new Vector3(0.08f, 0.035f, 0.005f));
            }
            k.Collider(new Vector3(0f, 0.65f, 0f), new Vector3(0.5f, 1.3f, 0.65f));
        }

        /// <summary>
        /// Floor-to-ceiling (3 m) steel records shelving: uprights, seven shelves, back cross
        /// bracing, and runs of box files in kraft, card and beige with gaps and the odd lean.
        /// </summary>
        static void ArchiveShelf(MeshKit k)
        {
            var rng = new System.Random(4471);
            k.Use(M.Metal);
            foreach (float x in new[] { -0.98f, 0f, 0.98f })
            {
                k.Box(new Vector3(x, 1.5f, 0.23f), new Vector3(0.035f, 3f, 0.035f));
                k.Box(new Vector3(x, 1.5f, -0.23f), new Vector3(0.035f, 3f, 0.035f));
            }
            for (int lvl = 0; lvl < 7; lvl++)
                k.Box(new Vector3(0f, 0.08f + lvl * 0.47f, 0f), new Vector3(1.99f, 0.025f, 0.48f));
            k.Beam(new Vector3(-0.97f, 0.1f, -0.235f), new Vector3(0f, 2.9f, -0.235f), 0.02f, 0.005f, Vector3.forward);
            k.Beam(new Vector3(0f, 0.1f, -0.235f), new Vector3(0.97f, 2.9f, -0.235f), 0.02f, 0.005f, Vector3.forward);

            var fileMats = new[] { M.Cork, M.Paper, M.Beige, M.Cork };
            for (int lvl = 0; lvl < 6; lvl++)
            {
                float y = 0.0925f + lvl * 0.47f;
                foreach (var (x0, x1) in new[] { (-0.955f, -0.025f), (0.025f, 0.955f) })
                {
                    float x = x0;
                    while (x < x1 - 0.07f)
                    {
                        if (rng.NextDouble() < 0.08) { x += R(rng, 0.1f, 0.3f); continue; }
                        float w = R(rng, 0.07f, 0.1f);
                        if (x + w > x1) break;
                        float h = R(rng, 0.29f, 0.34f), d = R(rng, 0.3f, 0.36f);
                        k.Use(fileMats[rng.Next(fileMats.Length)]);
                        k.Box(new Vector3(x + w * 0.5f, y + h * 0.5f, 0.2f - d * 0.5f), new Vector3(w - 0.004f, h, d), false);
                        x += w;
                    }
                }
            }
            k.Collider(new Vector3(0f, 1.5f, 0f), new Vector3(2f, 3f, 0.5f));
        }

        /// <summary>
        /// The office wall clock, hands at 4:47 (hour hand between 4 and 5, minute hand on 47).
        /// Mounted on its back: the back sits on z = 0, the face looks along +Z, and the pivot is
        /// at the bottom of the face, so a builder places it at the wall with its base at the
        /// desired bottom height. No collider.
        /// </summary>
        static void WallClock(MeshKit k)
        {
            const float rad = 0.175f, depth = 0.05f, faceZ = 0.035f;
            var c = new Vector3(0f, rad, 0f);
            k.Push(c, Quaternion.Euler(90f, 0f, 0f));
            k.Use(M.MetalDark);
            k.Lathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(rad, 0f), new Vector2(rad, 0f), new Vector2(rad, depth), new Vector2(rad, depth),
                new Vector2(rad - 0.017f, depth), new Vector2(rad - 0.017f, depth), new Vector2(rad - 0.023f, faceZ),
            }, 32, Vector3.zero);
            k.Use(M.Paper);
            k.Lathe(new[] { new Vector2(rad - 0.023f, faceZ), new Vector2(0f, faceZ) }, 32, Vector3.zero);
            k.Pop();

            // Clockwise as seen by the viewer (who looks along -Z): angle a from 12 points at (-sin a, cos a).
            Vector3 Dir(float deg) => new Vector3(-Mathf.Sin(deg * Mathf.Deg2Rad), Mathf.Cos(deg * Mathf.Deg2Rad), 0f);
            k.Use(M.MetalDark);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                float len = i % 3 == 0 ? 0.028f : 0.016f;
                k.Box(c + Dir(a) * (rad - 0.04f - len * 0.5f) + Vector3.forward * (faceZ + 0.001f), new Vector3(i % 3 == 0 ? 0.008f : 0.005f, len, 0.002f), new Vector3(0f, 0f, a));
            }
            void Hand(float deg, float len, float width, float z)
            {
                k.Box(c + Dir(deg) * (len * 0.5f - 0.015f) + Vector3.forward * z, new Vector3(width, len, 0.002f), new Vector3(0f, 0f, deg));
            }
            Hand((4f + 47f / 60f) * 30f, 0.085f, 0.011f, faceZ + 0.004f);
            Hand(47f / 60f * 360f, 0.13f, 0.007f, faceZ + 0.007f);
            k.Use(SignalRed);
            Hand(60f, 0.14f, 0.0025f, faceZ + 0.010f);
            k.Use(M.MetalDark);
            k.Cylinder(c + Vector3.forward * faceZ, c + Vector3.forward * (faceZ + 0.012f), 0.007f, 0.007f, 8, false, true);
        }

        /// <summary>A framed corkboard with pinned notices (blank paper; signs and text are placed separately). Wall-mounted: back on z = 0, pivot at the bottom.</summary>
        static void Corkboard(MeshKit k)
        {
            const float w = 1.2f, h = 0.9f, fw = 0.035f;
            k.Use(M.Frame);
            k.Box(new Vector3(0f, h - fw * 0.5f, 0.015f), new Vector3(w, fw, 0.03f));
            k.Box(new Vector3(0f, fw * 0.5f, 0.015f), new Vector3(w, fw, 0.03f));
            k.Box(new Vector3(-(w - fw) * 0.5f, h * 0.5f, 0.015f), new Vector3(fw, h - fw * 2f, 0.03f));
            k.Box(new Vector3((w - fw) * 0.5f, h * 0.5f, 0.015f), new Vector3(fw, h - fw * 2f, 0.03f));
            k.Use(M.Cork);
            k.Box(new Vector3(0f, h * 0.5f, 0.01f), new Vector3(w - fw * 2f, h - fw * 2f, 0.02f));

            var notes = new[]
            {
                (new Vector2(-0.38f, 0.62f), new Vector2(0.21f, 0.297f), -3f, M.Paper),
                (new Vector2(-0.12f, 0.66f), new Vector2(0.148f, 0.21f), 2f, M.Paper),
                (new Vector2(0.12f, 0.63f), new Vector2(0.21f, 0.297f), 1f, M.Paper),
                (new Vector2(0.38f, 0.66f), new Vector2(0.18f, 0.13f), -4f, M.ChairOrange),
                (new Vector2(-0.4f, 0.25f), new Vector2(0.148f, 0.21f), 4f, M.Beige),
                (new Vector2(-0.16f, 0.27f), new Vector2(0.21f, 0.297f), -1f, M.Paper),
                (new Vector2(0.1f, 0.26f), new Vector2(0.127f, 0.076f), 6f, M.Paper),
                (new Vector2(0.36f, 0.3f), new Vector2(0.21f, 0.297f), -2f, M.Paper),
                (new Vector2(0.1f, 0.4f), new Vector2(0.1f, 0.15f), -5f, M.Beige),
            };
            float z = 0.0205f;
            foreach (var (pos, size, rot, mat) in notes)
            {
                z += 0.0004f;
                k.Use(mat);
                k.Box(new Vector3(pos.x, pos.y, z), new Vector3(size.x, size.y, 0.0008f), new Vector3(0f, 0f, rot));
                k.Use(SignalRed);
                k.Box(new Vector3(pos.x, pos.y + size.y * 0.5f - 0.015f, z + 0.004f), new Vector3(0.01f, 0.01f, 0.008f));
            }
        }

        /// <summary>A wall-mounted whiteboard: aluminium frame, white surface, marker tray with two markers. Back on z = 0, pivot at the bottom.</summary>
        static void Whiteboard(MeshKit k)
        {
            const float w = 1.8f, h = 1.16f, y0 = 0.04f, fw = 0.02f;
            k.Use(M.Whiteboard);
            k.Box(new Vector3(0f, y0 + h * 0.5f, 0.01f), new Vector3(w - fw * 2f, h - fw * 2f, 0.02f));
            k.Use(M.Metal);
            k.Box(new Vector3(0f, y0 + h - fw * 0.5f, 0.0125f), new Vector3(w, fw, 0.025f));
            k.Box(new Vector3(0f, y0 + fw * 0.5f, 0.0125f), new Vector3(w, fw, 0.025f));
            k.Box(new Vector3(-(w - fw) * 0.5f, y0 + h * 0.5f, 0.0125f), new Vector3(fw, h - fw * 2f, 0.025f));
            k.Box(new Vector3((w - fw) * 0.5f, y0 + h * 0.5f, 0.0125f), new Vector3(fw, h - fw * 2f, 0.025f));
            k.Box(new Vector3(0f, 0.03f, 0.04f), new Vector3(1.2f, 0.012f, 0.07f));
            k.Box(new Vector3(0f, 0.045f, 0.072f), new Vector3(1.2f, 0.02f, 0.006f));
            k.Use(M.MetalDark);
            k.Cylinder(new Vector3(-0.3f, 0.048f, 0.045f), new Vector3(-0.17f, 0.048f, 0.045f), 0.009f, 0.009f, 6);
            k.Use(SignalRed);
            k.Cylinder(new Vector3(-0.12f, 0.048f, 0.04f), new Vector3(0.01f, 0.048f, 0.045f), 0.009f, 0.009f, 6);
        }

        /// <summary>The 6 m boardroom table: a dark wood racetrack top on two wooden slab pedestals with black plinths and a steel stretcher.</summary>
        static void BoardroomTable(MeshKit k)
        {
            k.Use(M.WoodDark);
            k.ExtrudeY(MeshKit.RoundedRect(6f, 1.6f, 0.8f, 10), 0.705f, 0.75f);
            for (int s = -1; s <= 1; s += 2)
            {
                k.Use(M.WoodDark);
                k.Box(new Vector3(s * 1.7f, 0.37f, 0f), new Vector3(0.12f, 0.66f, 0.9f), false);
                k.Use(M.MetalDark);
                k.Box(new Vector3(s * 1.7f, 0.02f, 0f), new Vector3(0.22f, 0.04f, 1.1f), false);
                k.Box(new Vector3(s * 1.7f, 0.69f, 0f), new Vector3(0.3f, 0.03f, 1.0f), false);
            }
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.6f, 0f), new Vector3(3.3f, 0.06f, 0.06f));
            k.Collider(new Vector3(0f, 0.375f, 0f), new Vector3(4.4f, 0.75f, 1.6f));
            k.Collider(new Vector3(0f, 0.375f, 0f), new Vector3(5.8f, 0.75f, 1.0f));
        }

        /// <summary>An angled task lamp: weighted base, two arm segments, a conical shade aimed down and forward (+Z) with a warm bulb inside.</summary>
        static void DeskLamp(MeshKit k)
        {
            k.Use(M.MetalDark);
            k.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.085f, 0f), new Vector2(0.085f, 0f), new Vector2(0.085f, 0.015f), new Vector2(0.07f, 0.026f), new Vector2(0f, 0.03f) }, 16, new Vector3(0f, 0f, -0.07f));
            var j0 = new Vector3(0f, 0.03f, -0.07f);
            var j1 = new Vector3(0f, 0.30f, -0.03f);
            var j2 = new Vector3(0f, 0.37f, 0.09f);
            k.Cylinder(j0, j1, 0.007f, 0.007f, 6, false, false);
            k.Cylinder(j1, j2, 0.006f, 0.006f, 6, false, false);
            k.Cylinder(j1 + Vector3.left * 0.015f, j1 + Vector3.right * 0.015f, 0.012f, 0.012f, 8);
            k.Push(j2 + new Vector3(0f, 0f, 0.02f), Quaternion.Euler(-25f, 0f, 0f));
            k.Lathe(new[]
            {
                new Vector2(0f, -0.004f), new Vector2(0.028f, -0.005f), new Vector2(0.028f, -0.005f), new Vector2(0.062f, -0.1f), new Vector2(0.062f, -0.1f),
                new Vector2(0.066f, -0.1f), new Vector2(0.066f, -0.1f), new Vector2(0.032f, 0f), new Vector2(0.032f, 0f), new Vector2(0f, 0.012f),
            }, 16, Vector3.zero);
            k.Use(M.LightWarm);
            var bulb = new Vector2[7];
            for (int i = 0; i < bulb.Length; i++)
            {
                float t = i / (float)(bulb.Length - 1) * Mathf.PI;
                bulb[i] = new Vector2(Mathf.Sin(t) * 0.02f, -0.045f - Mathf.Cos(t) * 0.02f);
            }
            k.Lathe(bulb, 8, Vector3.zero);
            k.Pop();
        }

        /// <summary>A 1970s desk telephone: sloped beige body, keypad, and a handset resting across the cradle.</summary>
        static void Telephone(MeshKit k)
        {
            k.Use(M.Plastic);
            k.TaperBox(new Vector3(0f, 0.03f, 0.1f), new Vector2(0.2f, 0.06f), new Vector3(0f, 0.05f, -0.09f), new Vector2(0.2f, 0.1f));
            k.Push(new Vector3(0f, 0.074f, 0.04f), Quaternion.Euler(11.9f, 0f, 0f));
            k.Use(M.Beige);
            k.Box(new Vector3(0f, 0.003f, 0f), new Vector3(0.09f, 0.008f, 0.075f), false);
            k.Use(M.MetalDark);
            for (int r = 0; r < 4; r++)
                k.Box(new Vector3(0f, 0.0085f, -0.026f + r * 0.017f), new Vector3(0.07f, 0.004f, 0.009f), false);
            k.Pop();
            k.Use(M.Beige);
            k.RoundedBox(new Vector3(0f, 0.112f, -0.05f), new Vector3(0.2f, 0.028f, 0.045f), 0.012f, 1);
            k.RoundedBox(new Vector3(-0.085f, 0.1f, -0.05f), new Vector3(0.055f, 0.034f, 0.06f), 0.014f, 1);
            k.RoundedBox(new Vector3(0.085f, 0.1f, -0.05f), new Vector3(0.055f, 0.034f, 0.06f), 0.014f, 1);
        }

        /// <summary>A loose stack of paper and a manila folder, each layer slightly askew.</summary>
        static void PaperStack(MeshKit k)
        {
            var rng = new System.Random(733);
            float y = 0f;
            for (int i = 0; i < 7; i++)
            {
                float t = R(rng, 0.01f, 0.018f);
                k.Use(i == 3 ? M.Cork : M.Paper);
                k.Box(new Vector3(R(rng, -0.006f, 0.006f), y + t * 0.5f, R(rng, -0.006f, 0.006f)),
                    i == 3 ? new Vector3(0.31f, t, 0.225f) : new Vector3(0.297f, t, 0.21f), new Vector3(0f, R(rng, -4f, 4f), 0f));
                y += t;
            }
        }
    }
}
