using UnityEngine;
using M = Kit.Editor.OfficeKit.M;

namespace Kit.Editor
{
    /// <summary>
    /// Plants: control_01's long concrete planter of philodendrons, a potted philodendron, and
    /// control_02's lone, sparse atrium tree. Leaves are double-sided heart (or oval) cards that
    /// fold along the midrib and droop, in two greens, all from seeded randomness so every
    /// rebuild is identical.
    /// </summary>
    public static partial class PropShapes
    {
        /// <summary>
        /// One philodendron leaf on an arching petiole rising from <paramref name="root"/>: the stem
        /// climbs to <paramref name="height"/> and leans out along <paramref name="outward"/>, and the
        /// heart-shaped blade continues outward, facing mostly up with a random roll. 40 triangles.
        /// </summary>
        static void PhiloLeaf(MeshKit k, System.Random rng, Vector3 root, Vector3 outward, float height, float reach, float size)
        {
            outward = new Vector3(outward.x, 0f, outward.z).normalized;
            var attach = root + outward * reach + Vector3.up * height;
            var ctrl = root + Vector3.up * (height * 0.85f) + outward * (reach * 0.15f);
            k.Use(M.PlantDark);
            k.Tube(MeshKit.Bezier(root, ctrl, attach, 2), new[] { 0.007f, 0.006f, 0.004f }, 3, false, false);

            var dir = (outward + Vector3.up * R(rng, 0.05f, 0.6f)).normalized;
            var face = Vector3.ProjectOnPlane(Vector3.up, dir).normalized;
            face = Quaternion.AngleAxis(R(rng, -30f, 30f), dir) * face;
            k.Use(rng.NextDouble() < 0.55 ? M.Plant : M.PlantDark);
            k.Leaf(attach, dir, face, size, size * R(rng, 0.72f, 0.86f), size * R(rng, 0.2f, 0.45f), R(rng, 0.25f, 0.5f));
        }

        /// <summary>
        /// The long board-marked concrete trough (control_01): four walls, a soil bed, and 36 heart
        /// leaves in seven clumps of varied height, reach, tilt and size. About 1500 triangles.
        /// </summary>
        static void Planter(MeshKit k)
        {
            const float w = 2.4f, d = 0.8f, h = 0.65f, wall = 0.1f, soil = 0.58f;
            k.Use(M.Concrete);
            k.Box(new Vector3(0f, h * 0.5f, (d - wall) * 0.5f), new Vector3(w, h, wall), false);
            k.Box(new Vector3(0f, h * 0.5f, -(d - wall) * 0.5f), new Vector3(w, h, wall), false);
            k.Box(new Vector3(-(w - wall) * 0.5f, h * 0.5f, 0f), new Vector3(wall, h, d - wall * 2f), false);
            k.Box(new Vector3((w - wall) * 0.5f, h * 0.5f, 0f), new Vector3(wall, h, d - wall * 2f), false);
            k.Use(M.Soil);
            float ix = w * 0.5f - wall, iz = d * 0.5f - wall;
            k.FlatQuad(new Vector3(-ix, soil, -iz), new Vector3(-ix, soil, iz), new Vector3(ix, soil, iz), new Vector3(ix, soil, -iz), Vector3.up);

            var rng = new System.Random(9127);
            const int clumps = 7;
            for (int c = 0; c < clumps; c++)
            {
                var centre = new Vector3(-0.95f + 1.9f * c / (clumps - 1) + R(rng, -0.08f, 0.08f), soil, R(rng, -0.12f, 0.12f));
                int leaves = c % 2 == 0 ? 5 : 6;
                if (c == clumps - 1) leaves = 4;
                float clumpHeight = R(rng, 0.75f, 1.15f);
                for (int i = 0; i < leaves; i++)
                {
                    float az = (i / (float)leaves + R(rng, -0.08f, 0.08f)) * Mathf.PI * 2f;
                    var outward = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az) * 1.4f);
                    var root = centre + new Vector3(R(rng, -0.04f, 0.04f), 0f, R(rng, -0.04f, 0.04f));
                    float height = clumpHeight * R(rng, 0.3f, 0.75f);
                    PhiloLeaf(k, rng, root, outward, height, R(rng, 0.08f, 0.26f), R(rng, 0.24f, 0.38f));
                }
            }
            k.Collider(new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d));
        }

        /// <summary>A tapered concrete pot (rolled lip, soil) with a 14-leaf philodendron, about 1.2 m tall.</summary>
        static void PottedPlant(MeshKit k)
        {
            k.Use(M.ConcreteLight);
            k.Lathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.15f, 0f), new Vector2(0.15f, 0f), new Vector2(0.2f, 0.40f), new Vector2(0.2f, 0.40f),
                new Vector2(0.215f, 0.405f), new Vector2(0.215f, 0.43f), new Vector2(0.215f, 0.43f), new Vector2(0.185f, 0.43f), new Vector2(0.185f, 0.43f),
                new Vector2(0.18f, 0.38f),
            }, 16, Vector3.zero);
            k.Use(M.Soil);
            k.Lathe(new[] { new Vector2(0.18f, 0.38f), new Vector2(0f, 0.38f) }, 16, Vector3.zero);

            var rng = new System.Random(5521);
            const int leaves = 14;
            for (int i = 0; i < leaves; i++)
            {
                float az = (i * 137.5f + R(rng, -10f, 10f)) * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az));
                var root = new Vector3(R(rng, -0.03f, 0.03f), 0.38f, R(rng, -0.03f, 0.03f));
                float height = R(rng, 0.25f, 0.62f);
                PhiloLeaf(k, rng, root, outward, height, R(rng, 0.06f, 0.2f), R(rng, 0.26f, 0.4f));
            }
            k.Collider(new Vector3(0f, 0.215f, 0f), new Vector3(0.42f, 0.43f, 0.42f));
        }

        /// <summary>
        /// control_02's lone atrium tree, about 4.5 m: a thin leaning trunk with a root flare,
        /// seven drooping branches with a couple of twigs each, and only about a hundred small
        /// hanging leaves, so it reads as slightly sad and sparse against the concrete.
        /// </summary>
        static void Tree(MeshKit k)
        {
            var rng = new System.Random(3301);
            k.Use(M.WoodDark);
            var trunk = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0.005f, 0.15f, 0f), new Vector3(0.03f, 0.8f, 0.01f), new Vector3(0.08f, 1.6f, 0.02f),
                new Vector3(0.12f, 2.4f, 0.02f), new Vector3(0.11f, 3.2f, 0.0f), new Vector3(0.06f, 3.8f, -0.02f), new Vector3(0.0f, 4.3f, -0.03f),
            };
            k.Tube(trunk, new[] { 0.15f, 0.095f, 0.08f, 0.068f, 0.055f, 0.042f, 0.028f, 0.012f }, 8, false, true);

            var leafSpots = new System.Collections.Generic.List<(Vector3 pos, Vector3 outward)>();
            const int branches = 7;
            for (int b = 0; b < branches; b++)
            {
                float t = b / (float)(branches - 1);
                int seg = Mathf.Min(Mathf.FloorToInt(Mathf.Lerp(3.2f, 6.2f, t)), trunk.Length - 2);
                var start = Vector3.Lerp(trunk[seg], trunk[seg + 1], R(rng, 0.2f, 0.8f));
                float az = (b * 137.5f + R(rng, -15f, 15f)) * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az));
                float len = Mathf.Lerp(1.5f, 0.8f, t) * R(rng, 0.85f, 1.1f);
                var ctrl = start + outward * (len * 0.45f) + Vector3.up * (len * Mathf.Lerp(0.45f, 0.6f, t));
                var end = start + outward * len + Vector3.up * (len * Mathf.Lerp(-0.05f, 0.25f, t));
                var path = MeshKit.Bezier(start, ctrl, end, 4);
                k.Use(M.WoodDark);
                k.Tube(path, new[] { 0.035f, 0.026f, 0.018f, 0.011f, 0.005f }, 5, false, false);
                leafSpots.Add((end, outward));

                for (int tw = 0; tw < 2; tw++)
                {
                    var from = path[2 + tw];
                    var side = Quaternion.AngleAxis(tw == 0 ? 40f : -35f, Vector3.up) * outward;
                    float tl = R(rng, 0.35f, 0.55f);
                    var twigEnd = from + side * tl + Vector3.down * R(rng, 0.05f, 0.2f);
                    var twigCtrl = from + side * (tl * 0.5f) + Vector3.up * 0.1f;
                    k.Tube(MeshKit.Bezier(from, twigCtrl, twigEnd, 2), new[] { 0.012f, 0.008f, 0.004f }, 4, false, false);
                    leafSpots.Add((twigEnd, side));
                    leafSpots.Add((Vector3.Lerp(from, twigEnd, 0.5f) + Vector3.up * 0.05f, side));
                }
            }
            leafSpots.Add((trunk[trunk.Length - 1], Vector3.forward));

            // Small leaves hang in loose clusters at branch and twig ends.
            foreach (var (pos, outward) in leafSpots)
            {
                int count = rng.Next(2, 4);
                for (int i = 0; i < count; i++)
                {
                    var at = pos + new Vector3(R(rng, -0.12f, 0.12f), R(rng, -0.1f, 0.06f), R(rng, -0.12f, 0.12f));
                    var dir = (outward * R(rng, 0.2f, 0.8f) + Random3(rng) * 0.6f + Vector3.down * R(rng, 0.4f, 1.0f)).normalized;
                    var face = Vector3.ProjectOnPlane(Vector3.up + outward * 0.3f, dir);
                    if (face.sqrMagnitude < 1e-4f) face = outward;
                    k.Use(rng.NextDouble() < 0.5 ? M.Plant : M.PlantDark);
                    float len = R(rng, 0.09f, 0.14f);
                    k.Leaf(at, dir, face.normalized, len, len * 0.5f, len * 0.15f, 0.3f, false);
                }
            }
            k.Collider(new Vector3(0.06f, 1.4f, 0f), new Vector3(0.26f, 2.8f, 0.26f));
        }

        /// <summary>A random vector inside the unit cube's centred span (-1..1 per axis).</summary>
        static Vector3 Random3(System.Random rng) => new Vector3(R(rng, -1f, 1f), R(rng, -1f, 1f), R(rng, -1f, 1f));
    }
}
