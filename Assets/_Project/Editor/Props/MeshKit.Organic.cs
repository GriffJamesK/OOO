using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kit.Editor
{
    /// <summary>
    /// The organic half of MeshKit: parametric grids (single surfaces and shells with thickness),
    /// tubes along a path, and leaf cards. These carry the props that must read as curved up close:
    /// the moulded shell chair, tufted cushions, philodendron leaves, the tree and the furled flag.
    /// Normals come from the surface itself (central differences), so curvature shades smoothly.
    /// </summary>
    public sealed partial class MeshKit
    {
        /// <summary>
        /// Per-vertex normals of a point grid by central differences, flipped to agree with
        /// <paramref name="hint"/> (the side the caller calls "front"). Degenerate spots (a pinched
        /// tip, a pole) fall back to one-sided differences and finally to the hint.
        /// </summary>
        public static Vector3[,] GridNormals(Vector3[,] p, Func<int, int, Vector3> hint, bool wrapU = false)
        {
            int nu = p.GetLength(0), nv = p.GetLength(1);
            var n = new Vector3[nu, nv];
            for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
            {
                // With wrapU the last column repeats the first (a UV seam), so neighbours wrap past it.
                int i0 = wrapU ? (i == 0 ? nu - 2 : i - 1) : Mathf.Max(i - 1, 0);
                int i1 = wrapU ? (i == nu - 1 ? 1 : i + 1) : Mathf.Min(i + 1, nu - 1);
                int j0 = Mathf.Max(j - 1, 0), j1 = Mathf.Min(j + 1, nv - 1);
                var du = p[i1, j] - p[i0, j];
                var dv = p[i, j1] - p[i, j0];
                var c = Vector3.Cross(du, dv);
                if (c.sqrMagnitude < 1e-14f)
                {
                    // Pinched row: borrow the tangent from the neighbouring row.
                    int jj = j == nv - 1 ? j - 1 : j + 1;
                    du = p[i1, jj] - p[i0, jj];
                    c = Vector3.Cross(du, dv);
                }
                var h = hint(i, j);
                if (c.sqrMagnitude < 1e-14f) c = h;
                c.Normalize();
                if (Vector3.Dot(c, h) < 0f) c = -c;
                n[i, j] = c;
            }
            return n;
        }

        /// <summary>
        /// A single-sided smooth surface through a point grid (front = the <paramref name="hint"/>
        /// side). Set <paramref name="doubleSided"/> for thin cards seen from both sides (leaves,
        /// cloth); the back copy shares positions with flipped normals. Returns the front indices.
        /// </summary>
        public int[,] Grid(Vector3[,] p, Func<int, int, Vector3> hint, bool doubleSided = false, bool wrapU = false)
        {
            var n = GridNormals(p, hint, wrapU);
            var idx = GridPass(p, n, 1f);
            if (doubleSided) GridPass(p, n, -1f);
            return idx;
        }

        /// <summary>Emits one side of a grid with normals scaled by <paramref name="sign"/>.</summary>
        int[,] GridPass(Vector3[,] p, Vector3[,] n, float sign)
        {
            int nu = p.GetLength(0), nv = p.GetLength(1);
            var idx = new int[nu, nv];
            float uLen = 0f, vLen = 0f;
            for (int i = 1; i < nu; i++) uLen += Vector3.Distance(p[i, nv / 2], p[i - 1, nv / 2]);
            for (int j = 1; j < nv; j++) vLen += Vector3.Distance(p[nu / 2, j], p[nu / 2, j - 1]);
            for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
                idx[i, j] = Vertex(p[i, j], n[i, j] * sign, new Vector2(i / (float)(nu - 1) * uLen, j / (float)(nv - 1) * vLen));
            for (int i = 0; i < nu - 1; i++)
            for (int j = 0; j < nv - 1; j++)
                QuadIdx(idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i + 1, j]);
            return idx;
        }

        /// <summary>
        /// A surface with real thickness (a moulded shell): the front grid, a back grid offset by
        /// <paramref name="thickness"/> against the front normals, and a rim joining them around all
        /// four edges, each rim facing outward along the surface. Smooth throughout.
        /// </summary>
        public void ThickSurface(Vector3[,] p, Func<int, int, Vector3> hint, float thickness)
        {
            int nu = p.GetLength(0), nv = p.GetLength(1);
            var n = GridNormals(p, hint);
            var back = new Vector3[nu, nv];
            for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
                back[i, j] = p[i, j] - n[i, j] * thickness;
            GridPass(p, n, 1f);
            GridPass(back, n, -1f);

            // Rim: walk each boundary; the outward direction is "away from the grid interior",
            // projected into the surface's tangent plane.
            void Rim(int count, Func<int, (int i, int j, int ii, int jj)> at)
            {
                var f = new int[count];
                var b = new int[count];
                for (int k = 0; k < count; k++)
                {
                    var (i, j, ii, jj) = at(k);
                    var o = p[i, j] - p[ii, jj];
                    o -= n[i, j] * Vector3.Dot(o, n[i, j]);
                    o = o.sqrMagnitude > 1e-12f ? o.normalized : n[i, j];
                    f[k] = Vertex(p[i, j], o, new Vector2(k * 0.03f, 0f));
                    b[k] = Vertex(back[i, j], o, new Vector2(k * 0.03f, thickness));
                }
                for (int k = 0; k < count - 1; k++) QuadIdx(f[k], f[k + 1], b[k + 1], b[k]);
            }
            Rim(nu, k => (k, 0, k, 1));
            Rim(nu, k => (k, nv - 1, k, nv - 2));
            Rim(nv, k => (0, k, 1, k));
            Rim(nv, k => (nu - 1, k, nu - 2, k));
        }

        /// <summary>
        /// A smooth tube along a path with per-point radii (tree trunks and branches, bent chrome
        /// frames, coat-rack hooks). Frames are parallel-transported so the tube never twists.
        /// </summary>
        public void Tube(IList<Vector3> path, IList<float> radii, int seg, bool capStart = false, bool capEnd = true)
        {
            int n = path.Count;
            if (n < 2) return;
            var tangents = new Vector3[n];
            for (int k = 0; k < n; k++)
            {
                var t = path[Mathf.Min(k + 1, n - 1)] - path[Mathf.Max(k - 1, 0)];
                tangents[k] = t.sqrMagnitude > 1e-12f ? t.normalized : Vector3.up;
            }
            Basis(tangents[0], out var e1, out _);
            var rings = new int[n, seg + 1];
            float v = 0f;
            for (int k = 0; k < n; k++)
            {
                if (k > 0)
                {
                    v += Vector3.Distance(path[k], path[k - 1]);
                    e1 = Vector3.ProjectOnPlane(e1, tangents[k]);
                    if (e1.sqrMagnitude < 1e-10f) Basis(tangents[k], out e1, out _);
                    e1.Normalize();
                }
                var e2 = Vector3.Cross(tangents[k], e1);
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2f;
                    var radial = e1 * Mathf.Cos(a) + e2 * Mathf.Sin(a);
                    rings[k, i] = Vertex(path[k] + radial * radii[k], radial, new Vector2(i / (float)seg * 0.3f, v));
                }
            }
            for (int k = 0; k < n - 1; k++)
            for (int i = 0; i < seg; i++)
                QuadIdx(rings[k, i], rings[k, i + 1], rings[k + 1, i + 1], rings[k + 1, i]);
            if (capStart && radii[0] > 1e-5f) { Basis(tangents[0], out var a1, out var a2); Disc(path[0], -tangents[0], a1, a2, radii[0], seg); }
            if (capEnd && radii[n - 1] > 1e-5f) { Basis(tangents[n - 1], out var b1, out var b2); Disc(path[n - 1], tangents[n - 1], b1, b2, radii[n - 1], seg); }
        }

        /// <summary>Points on a quadratic Bezier from a through control c to b (count + 1 points), for stems and arching tubes.</summary>
        public static List<Vector3> Bezier(Vector3 a, Vector3 c, Vector3 b, int count)
        {
            var pts = new List<Vector3>();
            for (int i = 0; i <= count; i++)
            {
                float t = i / (float)count;
                pts.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b);
            }
            return pts;
        }

        // Heart-shaped philodendron outline: rows along the midrib (t), half-width at each row, and
        // how far the edge sits behind the row (the two lobes reach back past the stem notch).
        static readonly float[] heartT = { 0f, 0.16f, 0.42f, 0.72f, 1f };
        static readonly float[] heartW = { 0.62f, 0.98f, 0.88f, 0.48f, 0f };
        static readonly float[] heartBack = { 0.13f, 0.05f, 0f, 0f, 0f };
        // Plain pointed oval (tree leaves): base, widest point, tip.
        static readonly float[] ovalT = { 0f, 0.38f, 1f };
        static readonly float[] ovalW = { 0f, 1f, 0f };
        static readonly float[] ovalBack = { 0f, 0f, 0f };

        /// <summary>
        /// A double-sided leaf card attached at <paramref name="attach"/>, pointing along
        /// <paramref name="dir"/> with its face towards <paramref name="faceNormal"/>. The blade
        /// folds up along the midrib (<paramref name="fold"/>, 0-1) and droops toward local down
        /// along its length (<paramref name="droop"/>, metres at the tip), so it catches light
        /// like a real leaf instead of a flat card. Heart leaves cost 30 triangles, ovals 8.
        /// </summary>
        public void Leaf(Vector3 attach, Vector3 dir, Vector3 faceNormal, float length, float width, float droop, float fold, bool heart = true)
        {
            var f = dir.normalized;
            var s = Vector3.Cross(faceNormal, f);
            if (s.sqrMagnitude < 1e-8f) Basis(f, out s, out _);
            s.Normalize();
            var up = Vector3.Cross(f, s).normalized;
            if (Vector3.Dot(up, faceNormal) < 0f) up = -up;
            var T = heart ? heartT : ovalT;
            var W = heart ? heartW : ovalW;
            var B = heart ? heartBack : ovalBack;
            var p = new Vector3[3, T.Length];
            for (int r = 0; r < T.Length; r++)
            for (int c = 0; c < 3; c++)
            {
                float side = c - 1;
                float along = T[r] - (side != 0 ? B[r] : 0f);
                float half = W[r] * width * 0.5f;
                var pos = attach + f * (along * length) + s * (side * half) + up * (Mathf.Abs(side) * half * fold * 0.6f);
                pos += Vector3.down * (droop * along * along);
                p[c, r] = pos;
            }
            Grid(p, (i, j) => up, true);
        }
    }
}
