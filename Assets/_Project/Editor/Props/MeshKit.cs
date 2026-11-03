using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kit.Editor
{
    /// <summary>
    /// A small procedural mesh builder for the office props. Geometry is added in a local frame
    /// that can be moved with <see cref="Push(Vector3, Quaternion)"/> / <see cref="Pop"/>, so a
    /// composite prop (a workstation with a desk, a CRT and a chair) can reuse each part's builder
    /// and still come out as ONE mesh with one sub-mesh per material, which keeps draw calls low
    /// when hundreds of props are placed. Every vertex gets an explicit normal (analytic for
    /// curved parts, so smooth surfaces stay smooth), and every triangle is wound to agree with its
    /// normals, so builders only have to get normals right, never the winding order.
    /// UVs are in metres (1 UV unit = 1 m), matching the office materials' world-scale tiling.
    /// Box colliders are recorded through the same frame stack, so composites inherit them.
    /// Organic shapes (surfaces, tubes, leaves) live in MeshKit.Organic.cs.
    /// </summary>
    public sealed partial class MeshKit
    {
        /// <summary>A box collider recorded in prop space (centre, size, rotation).</summary>
        public readonly struct BoxShape
        {
            public readonly Vector3 Center, Size;
            public readonly Quaternion Rotation;
            public BoxShape(Vector3 center, Vector3 size, Quaternion rotation) { Center = center; Size = size; Rotation = rotation; }
        }

        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Vector3> norms = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<string> materials = new List<string>();
        readonly List<List<int>> tris = new List<List<int>>();
        readonly List<BoxShape> colliders = new List<BoxShape>();
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        Matrix4x4 xf = Matrix4x4.identity;
        Matrix4x4 normalXf = Matrix4x4.identity;
        int sub = -1;

        /// <summary>Material names in sub-mesh order (resolve them to Materials when building the renderer).</summary>
        public IReadOnlyList<string> Materials => materials;

        /// <summary>Box colliders recorded with <see cref="Collider"/>, in prop space.</summary>
        public IReadOnlyList<BoxShape> Colliders => colliders;

        /// <summary>Total triangle count, for the budget report.</summary>
        public int TriangleCount
        {
            get { int n = 0; foreach (var t in tris) n += t.Count; return n / 3; }
        }

        // ------------------------------------------------------------------ state

        /// <summary>Selects the material (sub-mesh) that following geometry goes into, creating it on first use.</summary>
        public MeshKit Use(string material)
        {
            int i = materials.IndexOf(material);
            if (i < 0)
            {
                materials.Add(material);
                tris.Add(new List<int>());
                i = materials.Count - 1;
            }
            sub = i;
            return this;
        }

        /// <summary>The material currently selected, so a part builder can restore it.</summary>
        public string Current => sub < 0 ? null : materials[sub];

        /// <summary>Moves the local frame (relative to the current one) until the matching Pop.</summary>
        public void Push(Vector3 position, Quaternion rotation) => Push(position, rotation, Vector3.one);

        /// <summary>Moves and scales the local frame (relative to the current one) until the matching Pop.</summary>
        public void Push(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            stack.Push(xf);
            xf *= Matrix4x4.TRS(position, rotation, scale);
            normalXf = xf.inverse.transpose;
        }

        /// <summary>Restores the frame saved by the last Push.</summary>
        public void Pop()
        {
            xf = stack.Pop();
            normalXf = xf.inverse.transpose;
        }

        /// <summary>Records a box collider (local centre and size) in the current frame.</summary>
        public void Collider(Vector3 center, Vector3 size)
        {
            var s = xf.lossyScale;
            colliders.Add(new BoxShape(xf.MultiplyPoint3x4(center), Vector3.Scale(size, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z))), xf.rotation));
        }

        // ------------------------------------------------------------------ low level

        /// <summary>Adds one vertex (local position and normal) and returns its index.</summary>
        public int Vertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            verts.Add(xf.MultiplyPoint3x4(position));
            var n = normalXf.MultiplyVector(normal);
            norms.Add(n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up);
            uvs.Add(uv);
            return verts.Count - 1;
        }

        /// <summary>
        /// Adds a triangle, flipping its winding when needed so its front face agrees with the
        /// vertex normals (Unity front faces are clockwise). Degenerate triangles are dropped, so
        /// shapes that pinch to a point (leaf tips, lathe poles) cost nothing extra.
        /// </summary>
        public void Tri(int a, int b, int c)
        {
            if (sub < 0) throw new System.InvalidOperationException("MeshKit: call Use(material) before adding geometry.");
            var pa = verts[a];
            var face = Vector3.Cross(verts[b] - pa, verts[c] - pa);
            if (face.sqrMagnitude < 1e-14f) return;
            var list = tris[sub];
            if (Vector3.Dot(face, norms[a] + norms[b] + norms[c]) < 0f) { list.Add(a); list.Add(c); list.Add(b); }
            else { list.Add(a); list.Add(b); list.Add(c); }
        }

        /// <summary>Two triangles over a quad a-b-c-d (any consistent order around the edge).</summary>
        public void QuadIdx(int a, int b, int c, int d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        /// <summary>
        /// A flat quad through four local corners (in order around the edge). Its normal points
        /// away from <paramref name="inside"/>, so closed shapes can list corners in any order.
        /// </summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-14f) n = Vector3.Cross(c - a, d - a);
            n.Normalize();
            if (Vector3.Dot(n, (a + b + c + d) * 0.25f - inside) < 0f) n = -n;
            FlatQuad(a, b, c, d, n);
        }

        /// <summary>A flat quad with a given normal; UVs are its edge lengths in metres.</summary>
        public void FlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
        {
            float w = Vector3.Distance(a, d), h = Vector3.Distance(a, b);
            int i0 = Vertex(a, n, Vector2.zero), i1 = Vertex(b, n, new Vector2(0f, h)),
                i2 = Vertex(c, n, new Vector2(w, h)), i3 = Vertex(d, n, new Vector2(w, 0f));
            QuadIdx(i0, i1, i2, i3);
        }

        // ------------------------------------------------------------------ boxes

        /// <summary>An axis-aligned box (in the current frame) by centre and size; flat shaded. Leave out the bottom face when it sits on something.</summary>
        public void Box(Vector3 center, Vector3 size, bool bottom = true, bool top = true)
        {
            var h = size * 0.5f;
            for (int f = 0; f < 6; f++)
            {
                if (f == 3 && !bottom) continue;
                if (f == 2 && !top) continue;
                Axes(f, out var n, out var u, out var v);
                var c0 = center + Vector3.Scale(n, h);
                var U = u * Mathf.Abs(Vector3.Dot(u, h));
                var V = v * Mathf.Abs(Vector3.Dot(v, h));
                FlatQuad(c0 - U - V, c0 - U + V, c0 + U + V, c0 + U - V, n);
            }
        }

        /// <summary>A box rotated by Euler angles (degrees) about its own centre.</summary>
        public void Box(Vector3 center, Vector3 size, Vector3 euler)
        {
            Push(center, Quaternion.Euler(euler));
            Box(Vector3.zero, size);
            Pop();
        }

        /// <summary>A box between two corners (in the current frame).</summary>
        public void BoxMinMax(Vector3 min, Vector3 max, bool bottom = true)
        {
            var lo = Vector3.Min(min, max);
            var hi = Vector3.Max(min, max);
            Box((lo + hi) * 0.5f, hi - lo, bottom);
        }

        /// <summary>
        /// A box between two points along its length (a strut, leg or rail) with a square section
        /// of <paramref name="width"/> by <paramref name="depth"/>; <paramref name="up"/> orients the section.
        /// </summary>
        public void Beam(Vector3 a, Vector3 b, float width, float depth, Vector3 up)
        {
            var dir = b - a;
            float len = dir.magnitude;
            if (len < 1e-5f) return;
            Push((a + b) * 0.5f, Quaternion.LookRotation(dir / len, up));
            Box(Vector3.zero, new Vector3(width, depth, len));
            Pop();
        }

        /// <summary>
        /// A box tapering along local Z from a front rectangle (centre c0, size s0) to a back
        /// rectangle (centre c1, size s1). Used for CRT housings, wedge keyboards and phone bodies.
        /// </summary>
        public void TaperBox(Vector3 c0, Vector2 s0, Vector3 c1, Vector2 s1)
        {
            Vector3 P(Vector3 c, Vector2 s, int sx, int sy) => c + new Vector3(sx * s.x * 0.5f, sy * s.y * 0.5f, 0f);
            var inside = (c0 + c1) * 0.5f;
            Vector3 f00 = P(c0, s0, -1, -1), f10 = P(c0, s0, 1, -1), f11 = P(c0, s0, 1, 1), f01 = P(c0, s0, -1, 1);
            Vector3 b00 = P(c1, s1, -1, -1), b10 = P(c1, s1, 1, -1), b11 = P(c1, s1, 1, 1), b01 = P(c1, s1, -1, 1);
            Quad(f00, f10, f11, f01, inside); // front
            Quad(b00, b01, b11, b10, inside); // back
            Quad(f01, f11, b11, b01, inside); // top
            Quad(f00, b00, b10, f10, inside); // bottom
            Quad(f00, f01, b01, b00, inside); // left
            Quad(f10, b10, b11, f11, inside); // right
        }

        /// <summary>
        /// A box with rounded edges and corners (cushions, CRT bezels, drums). Each face is a grid
        /// whose corner rows are projected onto quarter circles, with smooth analytic normals.
        /// <paramref name="seg"/> = arc segments per 45 degrees (1 = cheap, 2 = smooth up close).
        /// </summary>
        public void RoundedBox(Vector3 center, Vector3 size, float radius, int seg = 2)
        {
            var h = size * 0.5f;
            float r = Mathf.Clamp(radius, 0.0005f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.999f);
            var inner = h - Vector3.one * r;
            for (int f = 0; f < 6; f++)
            {
                Axes(f, out var n, out var u, out var v);
                float hn = Mathf.Abs(Vector3.Dot(n, h));
                var su = RoundSamples(Mathf.Abs(Vector3.Dot(u, inner)), r, seg);
                var sv = RoundSamples(Mathf.Abs(Vector3.Dot(v, inner)), r, seg);
                var idx = new int[su.Count, sv.Count];
                for (int i = 0; i < su.Count; i++)
                for (int j = 0; j < sv.Count; j++)
                {
                    var p = n * hn + u * su[i] + v * sv[j];
                    var q = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    var d = (p - q).normalized;
                    idx[i, j] = Vertex(center + q + d * r, d, new Vector2(su[i], sv[j]));
                }
                for (int i = 0; i < su.Count - 1; i++)
                for (int j = 0; j < sv.Count - 1; j++)
                    QuadIdx(idx[i, j], idx[i, j + 1], idx[i + 1, j + 1], idx[i + 1, j]);
            }
        }

        /// <summary>Face coordinates across one axis of a rounded box: the flat middle plus tan-spaced corner samples (even angles on the arc).</summary>
        static List<float> RoundSamples(float innerHalf, float r, int seg)
        {
            var s = new List<float>();
            for (int k = seg; k >= 1; k--) s.Add(-(innerHalf + r * Mathf.Tan(k / (float)seg * Mathf.PI * 0.25f)));
            if (innerHalf > 1e-4f) { s.Add(-innerHalf); s.Add(innerHalf); }
            else s.Add(0f);
            for (int k = 1; k <= seg; k++) s.Add(innerHalf + r * Mathf.Tan(k / (float)seg * Mathf.PI * 0.25f));
            return s;
        }

        /// <summary>The normal and two tangents of box face f (0..5: +X, -X, +Y, -Y, +Z, -Z).</summary>
        static void Axes(int f, out Vector3 n, out Vector3 u, out Vector3 v)
        {
            switch (f)
            {
                case 0: n = Vector3.right; u = Vector3.forward; v = Vector3.up; break;
                case 1: n = Vector3.left; u = Vector3.back; v = Vector3.up; break;
                case 2: n = Vector3.up; u = Vector3.right; v = Vector3.forward; break;
                case 3: n = Vector3.down; u = Vector3.left; v = Vector3.forward; break;
                case 4: n = Vector3.forward; u = Vector3.left; v = Vector3.up; break;
                default: n = Vector3.back; u = Vector3.right; v = Vector3.up; break;
            }
        }

        // ------------------------------------------------------------------ round shapes

        /// <summary>
        /// A cylinder or cone frustum from <paramref name="a"/> (radius r0) to <paramref name="b"/>
        /// (radius r1) with smooth sides and optional flat caps. Legs, poles, columns, casters.
        /// </summary>
        public void Cylinder(Vector3 a, Vector3 b, float r0, float r1, int seg, bool capA = true, bool capB = true)
        {
            var axis = b - a;
            float len = axis.magnitude;
            if (len < 1e-5f) return;
            axis /= len;
            Basis(axis, out var e1, out var e2);
            float slope = (r0 - r1) / len;
            var ringA = new int[seg + 1];
            var ringB = new int[seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float t = i / (float)seg * Mathf.PI * 2f;
                var radial = e1 * Mathf.Cos(t) + e2 * Mathf.Sin(t);
                var n = (radial + axis * slope).normalized;
                float u = i / (float)seg * Mathf.PI * (r0 + r1);
                ringA[i] = Vertex(a + radial * r0, n, new Vector2(u, 0f));
                ringB[i] = Vertex(b + radial * r1, n, new Vector2(u, len));
            }
            for (int i = 0; i < seg; i++) QuadIdx(ringA[i], ringA[i + 1], ringB[i + 1], ringB[i]);
            if (capA && r0 > 1e-5f) Disc(a, -axis, e1, e2, r0, seg);
            if (capB && r1 > 1e-5f) Disc(b, axis, e1, e2, r1, seg);
        }

        /// <summary>A flat disc (fan) facing <paramref name="n"/>, used for cylinder caps.</summary>
        void Disc(Vector3 c, Vector3 n, Vector3 e1, Vector3 e2, float r, int seg)
        {
            int centre = Vertex(c, n, Vector2.zero);
            int first = -1, prev = -1;
            for (int i = 0; i < seg; i++)
            {
                float t = i / (float)seg * Mathf.PI * 2f;
                var off = (e1 * Mathf.Cos(t) + e2 * Mathf.Sin(t)) * r;
                int cur = Vertex(c + off, n, new Vector2(Vector3.Dot(off, e1), Vector3.Dot(off, e2)));
                if (prev >= 0) Tri(centre, prev, cur);
                else first = cur;
                prev = cur;
            }
            Tri(centre, prev, first);
        }

        /// <summary>Two unit vectors perpendicular to <paramref name="axis"/> and to each other.</summary>
        public static void Basis(Vector3 axis, out Vector3 e1, out Vector3 e2)
        {
            var refUp = Mathf.Abs(axis.y) < 0.95f ? Vector3.up : Vector3.right;
            e1 = Vector3.Cross(axis, refUp).normalized;
            e2 = Vector3.Cross(axis, e1).normalized;
        }

        /// <summary>
        /// Revolves a profile around the local Y axis through <paramref name="center"/>. Profile
        /// points are (radius, height). Walk the profile so the outside of the surface is on the
        /// right of the direction of travel (outer wall bottom-to-top, inner wall top-to-bottom,
        /// a top face inward, a bottom face outward); normals follow from that. Repeat a point to
        /// make a hard crease there (rims, base edges); everywhere else normals are smooth.
        /// </summary>
        public void Lathe(IList<Vector2> profile, int seg, Vector3 center)
        {
            int n = profile.Count;
            if (n < 2) return;
            var pn = new Vector2[n];
            var vlen = new float[n];
            for (int k = 0; k < n; k++)
            {
                bool dupPrev = k > 0 && (profile[k] - profile[k - 1]).sqrMagnitude < 1e-12f;
                bool dupNext = k < n - 1 && (profile[k + 1] - profile[k]).sqrMagnitude < 1e-12f;
                Vector2 t;
                if (k == 0 || dupPrev) t = k < n - 1 ? profile[k + 1] - profile[k] : Vector2.up;
                else if (k == n - 1 || dupNext) t = profile[k] - profile[k - 1];
                else t = (profile[k + 1] - profile[k]).normalized + (profile[k] - profile[k - 1]).normalized;
                pn[k] = new Vector2(t.y, -t.x).normalized;
                vlen[k] = k == 0 ? 0f : vlen[k - 1] + Vector2.Distance(profile[k], profile[k - 1]);
            }
            float maxR = 0f;
            foreach (var p in profile) maxR = Mathf.Max(maxR, p.x);
            var rings = new int[n, seg + 1];
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var radial = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                for (int k = 0; k < n; k++)
                {
                    var p = profile[k];
                    var nrm = radial * pn[k].x + Vector3.up * pn[k].y;
                    rings[k, i] = Vertex(center + radial * p.x + Vector3.up * p.y, nrm, new Vector2(i / (float)seg * Mathf.PI * 2f * maxR, vlen[k]));
                }
            }
            for (int k = 0; k < n - 1; k++)
            {
                if ((profile[k + 1] - profile[k]).sqrMagnitude < 1e-12f) continue;
                for (int i = 0; i < seg; i++) QuadIdx(rings[k, i], rings[k, i + 1], rings[k + 1, i + 1], rings[k + 1, i]);
            }
        }

        /// <summary>
        /// Extrudes a convex polygon (XZ points, any winding) vertically from y0 to y1, with flat
        /// caps. Side normals are smoothed across corners gentler than <paramref name="creaseDeg"/>,
        /// so a racetrack table top reads as one rounded edge while rectangle corners stay sharp.
        /// </summary>
        public void ExtrudeY(IList<Vector2> poly, float y0, float y1, float creaseDeg = 35f, bool bottom = true)
        {
            int n = poly.Count;
            if (n < 3) return;
            var centroid = Vector2.zero;
            foreach (var p in poly) centroid += p;
            centroid /= n;
            var edgeN = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                var d = poly[(i + 1) % n] - poly[i];
                var en = new Vector2(d.y, -d.x).normalized;
                if (Vector2.Dot(en, (poly[i] + poly[(i + 1) % n]) * 0.5f - centroid) < 0f) en = -en;
                edgeN[i] = en;
            }
            float cosCrease = Mathf.Cos(creaseDeg * Mathf.Deg2Rad);
            float u = 0f;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 nPrev = edgeN[(i - 1 + n) % n], nNext = edgeN[j];
                var nA = Vector2.Dot(nPrev, edgeN[i]) > cosCrease ? (nPrev + edgeN[i]).normalized : edgeN[i];
                var nB = Vector2.Dot(nNext, edgeN[i]) > cosCrease ? (nNext + edgeN[i]).normalized : edgeN[i];
                float len = Vector2.Distance(poly[i], poly[j]);
                var na3 = new Vector3(nA.x, 0f, nA.y);
                var nb3 = new Vector3(nB.x, 0f, nB.y);
                int a0 = Vertex(new Vector3(poly[i].x, y0, poly[i].y), na3, new Vector2(u, y0));
                int a1 = Vertex(new Vector3(poly[i].x, y1, poly[i].y), na3, new Vector2(u, y1));
                int b1 = Vertex(new Vector3(poly[j].x, y1, poly[j].y), nb3, new Vector2(u + len, y1));
                int b0 = Vertex(new Vector3(poly[j].x, y0, poly[j].y), nb3, new Vector2(u + len, y0));
                QuadIdx(a0, a1, b1, b0);
                u += len;
            }
            Cap(poly, centroid, y1, Vector3.up);
            if (bottom) Cap(poly, centroid, y0, Vector3.down);
        }

        /// <summary>A flat convex cap (fan around the centroid) at height y.</summary>
        void Cap(IList<Vector2> poly, Vector2 centroid, float y, Vector3 n)
        {
            int c = Vertex(new Vector3(centroid.x, y, centroid.y), n, centroid);
            var idx = new int[poly.Count];
            for (int i = 0; i < poly.Count; i++) idx[i] = Vertex(new Vector3(poly[i].x, y, poly[i].y), n, poly[i]);
            for (int i = 0; i < poly.Count; i++) Tri(c, idx[i], idx[(i + 1) % poly.Count]);
        }

        /// <summary>A rounded rectangle outline (XZ) centred on the origin, for ExtrudeY: racetrack tops when the radius is half the depth.</summary>
        public static List<Vector2> RoundedRect(float width, float depth, float radius, int cornerSeg)
        {
            var pts = new List<Vector2>();
            float r = Mathf.Min(radius, Mathf.Min(width, depth) * 0.5f);
            float hx = width * 0.5f - r, hz = depth * 0.5f - r;
            var centres = new[] { new Vector2(hx, hz), new Vector2(-hx, hz), new Vector2(-hx, -hz), new Vector2(hx, -hz) };
            for (int c = 0; c < 4; c++)
            for (int s = 0; s <= cornerSeg; s++)
            {
                float a = (c * 90f + s * 90f / cornerSeg) * Mathf.Deg2Rad;
                var p = centres[c] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (pts.Count == 0 || (pts[pts.Count - 1] - p).sqrMagnitude > 1e-8f) pts.Add(p);
            }
            if ((pts[0] - pts[pts.Count - 1]).sqrMagnitude < 1e-8f) pts.RemoveAt(pts.Count - 1);
            return pts;
        }

        // ------------------------------------------------------------------ output

        /// <summary>
        /// Writes the geometry into the Mesh asset at <paramref name="assetPath"/>, updating an
        /// existing asset in place (Clear, then refill) so prefabs and scenes that reference it
        /// keep working across rebuilds. Creates the asset (not its folder) when missing.
        /// </summary>
        public Mesh Save(string assetPath, string meshName)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            bool created = mesh == null;
            if (created) mesh = new Mesh();
            mesh.Clear();
            mesh.name = meshName;
            mesh.indexFormat = verts.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = tris.Count;
            for (int i = 0; i < tris.Count; i++) mesh.SetTriangles(tris[i], i, false);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (created) AssetDatabase.CreateAsset(mesh, assetPath);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
