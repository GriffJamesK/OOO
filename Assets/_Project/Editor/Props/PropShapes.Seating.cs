using UnityEngine;
using M = Kit.Editor.OfficeKit.M;

namespace Kit.Editor
{
    /// <summary>
    /// Seating: the hero shell chair (inside_02) and its beam row, the office swivel chair
    /// (Office_01), the black folding chair (inside_01), the green buttoned bench (severance_01)
    /// and the dark leather sofa (control_01).
    /// </summary>
    public static partial class PropShapes
    {
        // Side profile of the shell (z, y), front lip to top of the back. Catmull-Rom through
        // these, resampled by arc length. The seat dips slightly, the back reclines about 14 deg.
        static readonly Vector2[] shellProfile =
        {
            new Vector2(0.262f, 0.408f), new Vector2(0.240f, 0.446f), new Vector2(0.170f, 0.458f),
            new Vector2(0.060f, 0.447f), new Vector2(-0.060f, 0.433f), new Vector2(-0.150f, 0.441f),
            new Vector2(-0.203f, 0.482f), new Vector2(-0.226f, 0.560f), new Vector2(-0.248f, 0.660f),
            new Vector2(-0.270f, 0.740f), new Vector2(-0.290f, 0.805f),
        };

        /// <summary>
        /// The moulded one-piece shell alone (no legs): a parametric surface with 8 mm thickness.
        /// Across the width it narrows at the waist where seat meets back, curls up at the sides
        /// (more on the back, so it hugs), and rounds its four corners; along the length it follows
        /// <see cref="shellProfile"/>. About 750 triangles. Bottom of the seat is at about 0.425 m.
        /// </summary>
        static void ShellSeat(MeshKit k)
        {
            const int nu = 11, nv = 17;
            const float thickness = 0.008f;

            // Dense arc-length table of the profile for even spacing along the shell.
            const int dense = 200;
            var pts = new Vector2[dense + 1];
            var arc = new float[dense + 1];
            for (int i = 0; i <= dense; i++)
            {
                pts[i] = CatmullRom(shellProfile, i / (float)dense);
                arc[i] = i == 0 ? 0f : arc[i - 1] + Vector2.Distance(pts[i], pts[i - 1]);
            }
            float total = arc[dense];
            Vector2 At(float s, out Vector2 normal)
            {
                float target = Mathf.Clamp01(s) * total;
                int i = 1;
                while (i < dense && arc[i] < target) i++;
                float t = Mathf.InverseLerp(arc[i - 1], arc[i], target);
                var tangent = (pts[i] - pts[i - 1]).normalized;
                normal = new Vector2(tangent.y, -tangent.x); // (z, y): the sitting side (up on the seat, forward on the back)
                return Vector2.Lerp(pts[i - 1], pts[i], t);
            }

            var grid = new Vector3[nu, nv];
            var hints = new Vector3[nu, nv];
            for (int i = 0; i < nu; i++)
            {
                float u = -1f + 2f * i / (nu - 1);
                float au = Mathf.Abs(u);
                // Rounded corners: the outer columns start a little later and end a little earlier.
                float s0 = 0.035f * au * au * au, s1 = 1f - 0.06f * au * au * au * au;
                for (int j = 0; j < nv; j++)
                {
                    float s = Mathf.Lerp(s0, s1, j / (float)(nv - 1));
                    var prof = At(s, out var n);
                    float half = Keys(s, new Vector2(0f, 0.214f), new Vector2(0.08f, 0.232f), new Vector2(0.36f, 0.235f),
                        new Vector2(0.52f, 0.198f), new Vector2(0.64f, 0.196f), new Vector2(0.82f, 0.216f), new Vector2(1f, 0.226f));
                    float curl = Keys(s, new Vector2(0f, 0.008f), new Vector2(0.15f, 0.02f), new Vector2(0.4f, 0.024f),
                        new Vector2(0.56f, 0.04f), new Vector2(0.8f, 0.05f), new Vector2(1f, 0.045f));
                    float lift = curl * u * u;
                    grid[i, j] = new Vector3(u * half, prof.y + n.y * lift, prof.x + n.x * lift);
                    hints[i, j] = new Vector3(0f, n.y, n.x);
                }
            }
            k.Use(M.ChairOrange);
            k.ThickSurface(grid, (i, j) => hints[i, j], thickness);
        }

        /// <summary>Catmull-Rom spline through points, t in 0..1 over the whole chain.</summary>
        static Vector2 CatmullRom(Vector2[] p, float t)
        {
            int segs = p.Length - 1;
            float f = Mathf.Clamp01(t) * segs;
            int i = Mathf.Min(Mathf.FloorToInt(f), segs - 1);
            float u = f - i;
            Vector2 p0 = p[Mathf.Max(i - 1, 0)], p1 = p[i], p2 = p[i + 1], p3 = p[Mathf.Min(i + 2, segs)];
            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
        }

        /// <summary>
        /// The hero prop (inside_02): the burnt-orange shell on four thin splayed black legs joined
        /// by a small frame under the seat. Seat height about 0.45 m, overall about 0.8 m.
        /// </summary>
        static void ShellChair(MeshKit k)
        {
            ShellSeat(k);
            k.Use(M.MetalDark);
            const float topY = 0.418f;
            k.Box(new Vector3(0f, 0.414f, 0.01f), new Vector3(0.26f, 0.01f, 0.2f));
            for (int side = -1; side <= 1; side += 2)
            {
                var frontTop = new Vector3(side * 0.15f, topY, 0.12f);
                var backTop = new Vector3(side * 0.15f, topY, -0.10f);
                k.Beam(frontTop, backTop, 0.016f, 0.014f, Vector3.up);
                k.Cylinder(new Vector3(side * 0.226f, 0f, 0.235f), frontTop, 0.0085f, 0.0085f, 6, true, false);
                k.Cylinder(new Vector3(side * 0.216f, 0f, -0.215f), backTop, 0.0085f, 0.0085f, 6, true, false);
            }
            k.Collider(new Vector3(0f, 0.225f, 0.01f), new Vector3(0.48f, 0.45f, 0.5f));
            k.Collider(new Vector3(0f, 0.64f, -0.255f), new Vector3(0.44f, 0.32f, 0.07f));
        }

        /// <summary>Waiting-room beam seating: four shells on one black beam with two T-feet.</summary>
        static void ShellChairRow(MeshKit k)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 0.56f;
                k.Push(new Vector3(x, 0f, 0f), Quaternion.identity);
                ShellSeat(k);
                k.Use(M.MetalDark);
                k.Box(new Vector3(0f, 0.399f, -0.01f), new Vector3(0.04f, 0.03f, 0.04f));
                k.Box(new Vector3(0f, 0.418f, 0.01f), new Vector3(0.24f, 0.01f, 0.18f));
                k.Pop();
            }
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.36f, -0.01f), new Vector3(2.24f, 0.05f, 0.08f));
            for (int side = -1; side <= 1; side += 2)
            {
                k.Box(new Vector3(side * 0.84f, 0.17f, -0.01f), new Vector3(0.05f, 0.34f, 0.05f));
                k.Box(new Vector3(side * 0.84f, 0.0125f, -0.01f), new Vector3(0.06f, 0.025f, 0.56f));
            }
            k.Collider(new Vector3(0f, 0.225f, 0.01f), new Vector3(2.2f, 0.45f, 0.5f));
            k.Collider(new Vector3(0f, 0.64f, -0.255f), new Vector3(2.15f, 0.32f, 0.07f));
        }

        /// <summary>
        /// Office_01's task chair: grey fabric seat and back cushions, a black back bar, a gas
        /// column and a five-star base on casters. Faces +Z.
        /// </summary>
        static void SwivelChair(MeshKit k)
        {
            k.Use(M.MetalDark);
            for (int a = 0; a < 5; a++)
            {
                float ang = (a * 72f + 36f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
                var tip = dir * 0.30f + Vector3.up * 0.075f;
                k.Beam(dir * 0.04f + Vector3.up * 0.10f, tip, 0.045f, 0.03f, Vector3.up);
                k.Box(tip + Vector3.down * 0.02f, new Vector3(0.025f, 0.03f, 0.025f));
                var axle = Vector3.Cross(dir, Vector3.up) * 0.018f;
                var wheel = tip - Vector3.up * 0.047f;
                k.Cylinder(wheel - axle, wheel + axle, 0.028f, 0.028f, 8);
            }
            k.Cylinder(new Vector3(0f, 0.06f, 0f), new Vector3(0f, 0.12f, 0f), 0.05f, 0.045f, 10);
            k.Cylinder(new Vector3(0f, 0.12f, 0f), new Vector3(0f, 0.30f, 0f), 0.032f, 0.03f, 10, false, true);
            k.Use(M.Metal);
            k.Cylinder(new Vector3(0f, 0.30f, 0f), new Vector3(0f, 0.41f, 0f), 0.02f, 0.02f, 8, false, false);
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.425f, 0f), new Vector3(0.2f, 0.04f, 0.22f));
            k.Beam(new Vector3(0f, 0.43f, -0.08f), new Vector3(0f, 0.45f, -0.27f), 0.06f, 0.02f, Vector3.up);
            k.Beam(new Vector3(0f, 0.44f, -0.27f), new Vector3(0f, 0.64f, -0.30f), 0.06f, 0.02f, Vector3.forward);

            k.Use(PropShapes.Fabric);
            k.RoundedBox(new Vector3(0f, 0.49f, 0.01f), new Vector3(0.48f, 0.09f, 0.47f), 0.035f, 2);
            k.Push(new Vector3(0f, 0.77f, -0.29f), Quaternion.Euler(-8f, 0f, 0f));
            k.RoundedBox(Vector3.zero, new Vector3(0.44f, 0.46f, 0.08f), 0.035f, 2);
            k.Pop();

            k.Collider(new Vector3(0f, 0.265f, 0f), new Vector3(0.5f, 0.53f, 0.5f));
            k.Collider(new Vector3(0f, 0.77f, -0.29f), new Vector3(0.44f, 0.46f, 0.1f));
        }

        /// <summary>inside_01's black metal folding chair: tube frame, flat seat pan, curved-edge back plate.</summary>
        static void FoldingChair(MeshKit k)
        {
            k.Use(M.MetalDark);
            const float r = 0.0105f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.195f;
                k.Cylinder(new Vector3(x, 0f, -0.17f), new Vector3(x, 0.80f, -0.215f), r, r, 6, true, true);       // back upright
                k.Cylinder(new Vector3(x + side * 0.008f, 0f, 0.215f), new Vector3(x + side * 0.008f, 0.43f, 0.15f), r, r, 6, true, false); // front leg
                k.Cylinder(new Vector3(x, 0.43f, 0.17f), new Vector3(x, 0.43f, -0.19f), r * 0.9f, r * 0.9f, 6, true, true); // seat rail
                k.Cylinder(new Vector3(x + side * 0.008f, 0.07f, 0.2f), new Vector3(x, 0.40f, -0.18f), r * 0.8f, r * 0.8f, 6, false, false); // folding brace
            }
            k.Cylinder(new Vector3(-0.195f, 0.06f, -0.173f), new Vector3(0.195f, 0.06f, -0.173f), r * 0.8f, r * 0.8f, 6, false, false);
            k.Cylinder(new Vector3(-0.203f, 0.10f, 0.205f), new Vector3(0.203f, 0.10f, 0.205f), r * 0.8f, r * 0.8f, 6, false, false);
            k.RoundedBox(new Vector3(0f, 0.448f, -0.005f), new Vector3(0.40f, 0.022f, 0.40f), 0.009f, 1);
            k.Push(new Vector3(0f, 0.69f, -0.207f), Quaternion.Euler(-6f, 0f, 0f));
            k.RoundedBox(Vector3.zero, new Vector3(0.39f, 0.17f, 0.016f), 0.007f, 1);
            k.Pop();
            k.Collider(new Vector3(0f, 0.23f, 0.01f), new Vector3(0.44f, 0.46f, 0.44f));
            k.Collider(new Vector3(0f, 0.65f, -0.2f), new Vector3(0.42f, 0.3f, 0.05f));
        }

        /// <summary>
        /// severance_01's deep-green bench: three modules, each a buttoned seat cushion and a low
        /// buttoned back, on a green body over a recessed black plinth. Low (seat 0.42 m).
        /// </summary>
        static void Bench(MeshKit k)
        {
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.04f, -0.01f), new Vector3(1.92f, 0.08f, 0.6f), false);
            k.Use(M.BenchGreen);
            k.Box(new Vector3(0f, 0.17f, 0f), new Vector3(2f, 0.18f, 0.7f), false);
            for (int m = 0; m < 3; m++)
            {
                float x = (m - 1) * 0.667f;
                Tufted(k, new Vector3(x, 0.34f, 0.08f), 0.66f, 0.52f, 0.16f, 3, 2);
                k.Push(new Vector3(x, 0.52f, -0.27f), Quaternion.Euler(-6f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                Tufted(k, Vector3.zero, 0.66f, 0.5f, 0.15f, 3, 2);
                k.Pop();
            }
            k.Collider(new Vector3(0f, 0.21f, 0f), new Vector3(2f, 0.42f, 0.7f));
            k.Collider(new Vector3(0f, 0.6f, -0.27f), new Vector3(2f, 0.38f, 0.16f));
        }

        /// <summary>control_01's dark leather sofa: three buttoned seat and back cushions on a chrome tube frame whose arms run down to the floor.</summary>
        static void LeatherSofa(MeshKit k)
        {
            k.Use(M.Metal);
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.96f;
                var path = new[]
                {
                    new Vector3(x, 0f, 0.36f), new Vector3(x, 0.55f, 0.36f), new Vector3(x, 0.59f, 0.345f), new Vector3(x, 0.6f, 0.31f),
                    new Vector3(x, 0.6f, -0.31f), new Vector3(x, 0.59f, -0.345f), new Vector3(x, 0.55f, -0.36f), new Vector3(x, 0f, -0.36f),
                };
                k.Tube(path, new[] { 0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f }, 8, true, true);
                k.Cylinder(new Vector3(x, 0.24f, 0.33f), new Vector3(x - side * 0.04f, 0.24f, 0.33f), 0.012f, 0.012f, 6, false, false);
                k.Cylinder(new Vector3(x, 0.24f, -0.33f), new Vector3(x - side * 0.04f, 0.24f, -0.33f), 0.012f, 0.012f, 6, false, false);
            }
            k.Use(M.Leather);
            k.Box(new Vector3(0f, 0.26f, 0f), new Vector3(1.84f, 0.12f, 0.72f));
            k.Box(new Vector3(0f, 0.5f, -0.33f), new Vector3(1.84f, 0.36f, 0.06f));
            for (int m = 0; m < 3; m++)
            {
                float x = (m - 1) * 0.614f;
                Tufted(k, new Vector3(x, 0.39f, 0.04f), 0.61f, 0.62f, 0.14f, 2, 2, 0.015f, 0.03f);
                k.Push(new Vector3(x, 0.63f, -0.24f), Quaternion.Euler(-10f, 0f, 0f) * Quaternion.Euler(90f, 0f, 0f));
                Tufted(k, Vector3.zero, 0.61f, 0.38f, 0.13f, 2, 2, 0.015f, 0.03f);
                k.Pop();
            }
            k.Collider(new Vector3(0f, 0.23f, 0f), new Vector3(2f, 0.46f, 0.8f));
            k.Collider(new Vector3(0f, 0.64f, -0.3f), new Vector3(1.9f, 0.36f, 0.18f));
        }
    }
}
