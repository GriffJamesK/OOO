using UnityEngine;
using M = Kit.Editor.OfficeKit.M;

namespace Kit.Editor
{
    /// <summary>
    /// Lobby, cafeteria and circulation props: vending machine, water cooler, coffee trolley,
    /// reception desk, serving counter, tray stack, bin, coat rack, "take a number" dispenser,
    /// turnstile, furled flag, round cafeteria table and side table.
    /// </summary>
    public static partial class PropShapes
    {
        /// <summary>
        /// A snack vending machine: dark body, a glazed window onto five shelves of coloured
        /// packets, warm lit strips inside the window, a cool lit header panel (no text), a
        /// control panel and a dispenser flap. Front at z = +0.4.
        /// </summary>
        static void VendingMachine(MeshKit k)
        {
            const float wx0 = -0.42f, wx1 = 0.16f, wy0 = 0.62f, wy1 = 1.78f, front = 0.38f, back = 0.2f;
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.95f, -0.1f), new Vector3(0.9f, 1.9f, 0.6f), false);
            k.BoxMinMax(new Vector3(-0.45f, 0f, back), new Vector3(wx0, 1.9f, front), false);
            k.BoxMinMax(new Vector3(wx0, wy1, back), new Vector3(wx1, 1.9f, front));
            k.BoxMinMax(new Vector3(wx0, 0f, back), new Vector3(wx1, wy0, front), false);
            k.BoxMinMax(new Vector3(wx1, 0f, back), new Vector3(0.45f, 1.9f, front), false);
            k.Use(M.Metal);
            k.Box(new Vector3((wx0 + wx1) * 0.5f, 0.27f, front + 0.005f), new Vector3(0.5f, 0.17f, 0.01f));
            k.Box(new Vector3(0.305f, 1.05f, front + 0.008f), new Vector3(0.03f, 0.006f, 0.016f));
            k.Use(M.Plastic);
            k.Box(new Vector3(0.305f, 1.3f, front + 0.005f), new Vector3(0.18f, 0.36f, 0.01f));
            k.Use(M.MetalDark);
            for (int r = 0; r < 4; r++)
            for (int c = 0; c < 3; c++)
                k.Box(new Vector3(0.255f + c * 0.05f, 1.2f + r * 0.055f, front + 0.012f), new Vector3(0.035f, 0.03f, 0.006f));
            k.Use(M.LightPanel);
            k.Box(new Vector3((wx0 + wx1) * 0.5f, 1.84f, front + 0.005f), new Vector3(0.54f, 0.08f, 0.01f));

            k.Use(M.LightWarm);
            k.Box(new Vector3(wx0 + 0.012f, (wy0 + wy1) * 0.5f, front - 0.03f), new Vector3(0.016f, wy1 - wy0, 0.016f));
            k.Box(new Vector3((wx0 + wx1) * 0.5f, wy1 - 0.012f, front - 0.03f), new Vector3(wx1 - wx0, 0.016f, 0.016f));

            var goods = new[] { M.ChairOrange, M.Paper, M.BenchGreen, SignalRed, M.Beige, M.Plant, M.Cork };
            int g = 0;
            for (int s = 0; s < 5; s++)
            {
                float y = 0.66f + s * 0.23f;
                k.Use(M.Metal);
                k.Box(new Vector3((wx0 + wx1) * 0.5f, y, back + 0.08f), new Vector3(wx1 - wx0, 0.012f, 0.16f));
                for (int c = 0; c < 6; c++)
                {
                    k.Use(goods[g++ % goods.Length]);
                    float h = 0.12f + 0.02f * ((c + s) % 3);
                    k.Box(new Vector3(wx0 + 0.05f + c * 0.096f, y + 0.006f + h * 0.5f, back + 0.1f), new Vector3(0.07f, h, 0.06f), false);
                }
            }
            k.Use(M.Glass);
            k.Box(new Vector3((wx0 + wx1) * 0.5f, (wy0 + wy1) * 0.5f, front - 0.006f), new Vector3(wx1 - wx0, wy1 - wy0, 0.006f));
            k.Collider(new Vector3(0f, 0.95f, -0.01f), new Vector3(0.9f, 1.9f, 0.78f));
        }

        /// <summary>A bottled water cooler: beige cabinet with taps and drip tray, and an upturned blue bottle (the one blue accent).</summary>
        static void WaterCooler(MeshKit k)
        {
            k.Use(M.Plastic);
            k.RoundedBox(new Vector3(0f, 0.46f, 0f), new Vector3(0.32f, 0.92f, 0.32f), 0.02f, 1);
            k.Lathe(new[] { new Vector2(0.13f, 0.92f), new Vector2(0.13f, 0.94f), new Vector2(0.13f, 0.94f), new Vector2(0.04f, 0.95f) }, 16, Vector3.zero);
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.79f, 0.162f), new Vector3(0.18f, 0.2f, 0.006f));
            k.Box(new Vector3(-0.04f, 0.84f, 0.18f), new Vector3(0.025f, 0.03f, 0.035f));
            k.Box(new Vector3(0.04f, 0.84f, 0.18f), new Vector3(0.025f, 0.03f, 0.035f));
            k.Use(M.Metal);
            k.Box(new Vector3(0f, 0.7f, 0.19f), new Vector3(0.17f, 0.015f, 0.06f));
            k.Use(WaterBlue);
            k.Lathe(new[]
            {
                new Vector2(0.032f, 0.93f), new Vector2(0.032f, 0.975f), new Vector2(0.075f, 0.995f), new Vector2(0.125f, 1.025f),
                new Vector2(0.135f, 1.06f), new Vector2(0.135f, 1.15f), new Vector2(0.122f, 1.185f), new Vector2(0.06f, 1.2f), new Vector2(0f, 1.202f),
            }, 16, Vector3.zero);
            k.Collider(new Vector3(0f, 0.6f, 0f), new Vector3(0.33f, 1.2f, 0.33f));
        }

        /// <summary>control_01's steel coffee trolley: three shelves on posts and casters, a push handle, a coffee machine, carafe and cups.</summary>
        static void CoffeeTrolley(MeshKit k)
        {
            k.Use(M.Metal);
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                k.Cylinder(new Vector3(sx * 0.37f, 0.06f, sz * 0.22f), new Vector3(sx * 0.37f, 0.88f, sz * 0.22f), 0.012f, 0.012f, 6, false, true);
            foreach (float y in new[] { 0.15f, 0.48f, 0.80f })
                k.Box(new Vector3(0f, y, 0f), new Vector3(0.78f, 0.02f, 0.46f));
            k.Cylinder(new Vector3(-0.4f, 0.86f, -0.2f), new Vector3(-0.4f, 0.86f, 0.2f), 0.012f, 0.012f, 6);
            k.Beam(new Vector3(-0.37f, 0.86f, -0.2f), new Vector3(-0.4f, 0.86f, -0.2f), 0.015f, 0.015f, Vector3.up);
            k.Beam(new Vector3(-0.37f, 0.86f, 0.2f), new Vector3(-0.4f, 0.86f, 0.2f), 0.015f, 0.015f, Vector3.up);
            k.Use(M.MetalDark);
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
            {
                var c = new Vector3(sx * 0.37f, 0.03f, sz * 0.22f);
                k.Cylinder(c + Vector3.left * 0.012f, c + Vector3.right * 0.012f, 0.03f, 0.03f, 8);
                k.Box(c + Vector3.up * 0.035f, new Vector3(0.03f, 0.02f, 0.03f));
            }
            k.Box(new Vector3(0.15f, 0.81f + 0.16f, -0.08f), new Vector3(0.24f, 0.32f, 0.24f), false);
            k.Box(new Vector3(0.15f, 0.825f, 0.1f), new Vector3(0.16f, 0.03f, 0.14f), false);
            k.Use(M.Glass);
            k.Lathe(new[] { new Vector2(0f, 0.84f), new Vector2(0.055f, 0.84f), new Vector2(0.055f, 0.84f), new Vector2(0.06f, 0.9f), new Vector2(0.045f, 0.96f), new Vector2(0.03f, 0.975f), new Vector2(0f, 0.975f) }, 12, new Vector3(0.15f, 0f, 0.1f));
            k.Use(M.Paper);
            k.Lathe(new[] { new Vector2(0f, 0.81f), new Vector2(0.03f, 0.81f), new Vector2(0.03f, 0.81f), new Vector2(0.04f, 0.96f), new Vector2(0.04f, 0.96f), new Vector2(0f, 0.96f) }, 10, new Vector3(-0.2f, 0f, 0.08f));
            k.Use(M.Cork);
            k.Box(new Vector3(-0.15f, 0.16f + 0.1f, 0f), new Vector3(0.3f, 0.2f, 0.3f), false);
            k.Box(new Vector3(0.2f, 0.16f + 0.07f, 0.02f), new Vector3(0.22f, 0.14f, 0.28f), false);
            k.Collider(new Vector3(0f, 0.45f, 0f), new Vector3(0.8f, 0.9f, 0.5f));
        }

        /// <summary>
        /// A wood and concrete reception counter: a tall board-marked concrete front with a wood
        /// fascia band and a wood transaction ledge (visitors on +Z), and a lower wood work surface
        /// behind it on concrete end panels.
        /// </summary>
        static void ReceptionDesk(MeshKit k)
        {
            k.Use(M.Concrete);
            k.BoxMinMax(new Vector3(-1.5f, 0f, 0.12f), new Vector3(1.5f, 1.05f, 0.4f), false);
            k.Box(new Vector3(-1.46f, 0.36f, -0.14f), new Vector3(0.08f, 0.72f, 0.52f), false);
            k.Box(new Vector3(1.46f, 0.36f, -0.14f), new Vector3(0.08f, 0.72f, 0.52f), false);
            k.Use(M.WoodVeneer);
            k.Box(new Vector3(0f, 1.075f, 0.25f), new Vector3(3.04f, 0.05f, 0.36f));
            k.Box(new Vector3(0f, 0.735f, -0.14f), new Vector3(2.84f, 0.03f, 0.52f));
            k.Box(new Vector3(0f, 0.82f, 0.41f), new Vector3(3f, 0.28f, 0.02f));
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.04f, 0.405f), new Vector3(3f, 0.08f, 0.012f), false);
            k.Collider(new Vector3(0f, 0.55f, 0.25f), new Vector3(3f, 1.1f, 0.36f));
            k.Collider(new Vector3(0f, 0.375f, -0.14f), new Vector3(3f, 0.75f, 0.52f));
        }

        /// <summary>A stainless cafeteria serving counter: panelled steel body on a black plinth, steel top with dark food wells, and a three-rail tray slide on the +Z side.</summary>
        static void ServingCounter(MeshKit k)
        {
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.05f, -0.1f), new Vector3(2.9f, 0.1f, 0.56f), false);
            k.Use(M.Metal);
            k.BoxMinMax(new Vector3(-1.5f, 0.1f, -0.4f), new Vector3(1.5f, 0.88f, 0.2f));
            k.BoxMinMax(new Vector3(-1.52f, 0.88f, -0.4f), new Vector3(1.52f, 0.92f, 0.24f));
            for (int r = 0; r < 3; r++)
                k.Cylinder(new Vector3(-1.5f, 0.84f, 0.29f + r * 0.045f), new Vector3(1.5f, 0.84f, 0.29f + r * 0.045f), 0.01f, 0.01f, 6);
            foreach (float x in new[] { -1.4f, -0.47f, 0.47f, 1.4f })
                k.Box(new Vector3(x, 0.82f, 0.3f), new Vector3(0.02f, 0.05f, 0.2f));
            k.Use(M.MetalDark);
            foreach (float x in new[] { -0.9f, -0.3f, 0.3f, 0.9f })
                k.Box(new Vector3(x, 0.922f, -0.1f), new Vector3(0.5f, 0.004f, 0.36f), false);
            foreach (float x in new[] { -0.75f, 0f, 0.75f })
                k.Box(new Vector3(x, 0.49f, 0.201f), new Vector3(0.008f, 0.76f, 0.003f), false);
            k.Collider(new Vector3(0f, 0.46f, 0f), new Vector3(3f, 0.92f, 0.8f));
        }

        /// <summary>A stack of fourteen brown cafeteria trays, each a little out of line.</summary>
        static void TrayStack(MeshKit k)
        {
            var rng = new System.Random(2290);
            k.Use(M.WoodDark);
            for (int i = 0; i < 14; i++)
                k.Box(new Vector3(R(rng, -0.006f, 0.006f), 0.009f + i * 0.02f, R(rng, -0.006f, 0.006f)), new Vector3(0.45f, 0.018f, 0.35f), new Vector3(0f, R(rng, -1.5f, 1.5f), 0f));
        }

        /// <summary>An open steel bin: tapered outer wall, rolled rim, and a visible inside with a floor.</summary>
        static void Bin(MeshKit k)
        {
            k.Use(M.Metal);
            k.Lathe(new[]
            {
                new Vector2(0f, 0f), new Vector2(0.15f, 0f), new Vector2(0.15f, 0f), new Vector2(0.172f, 0.6f), new Vector2(0.172f, 0.6f),
                new Vector2(0.162f, 0.6f), new Vector2(0.162f, 0.6f), new Vector2(0.142f, 0.02f), new Vector2(0.142f, 0.02f), new Vector2(0f, 0.02f),
            }, 16, Vector3.zero);
            k.Collider(new Vector3(0f, 0.3f, 0f), new Vector3(0.3f, 0.6f, 0.3f));
        }

        /// <summary>A midcentury coat stand: dark wood pole on three splayed black feet, brass hooks and a brass finial.</summary>
        static void CoatRack(MeshKit k)
        {
            k.Use(M.WoodDark);
            k.Cylinder(new Vector3(0f, 0.06f, 0f), new Vector3(0f, 1.76f, 0f), 0.02f, 0.017f, 8, false, true);
            k.Use(M.MetalDark);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + 30f) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                k.Cylinder(new Vector3(0f, 0.16f, 0f), dir * 0.25f + Vector3.up * 0.01f, 0.016f, 0.01f, 6);
            }
            k.Use(M.Brass);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float y = i % 2 == 0 ? 1.7f : 1.58f;
                var from = new Vector3(0f, y, 0f) + dir * 0.015f;
                var tip = from + dir * 0.13f + Vector3.up * 0.07f;
                k.Tube(MeshKit.Bezier(from, from + dir * 0.11f, tip, 3), new[] { 0.008f, 0.007f, 0.007f, 0.006f }, 5, false, true);
                k.Cylinder(tip, tip + Vector3.up * 0.012f, 0.011f, 0.009f, 6);
            }
            var ball = new Vector2[6];
            for (int i = 0; i < ball.Length; i++)
            {
                float t = i / (float)(ball.Length - 1) * Mathf.PI;
                ball[i] = new Vector2(Mathf.Sin(t) * 0.03f, 1.79f - Mathf.Cos(t) * 0.03f);
            }
            k.Lathe(ball, 10, Vector3.zero);
            k.Collider(new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f));
        }

        /// <summary>A "take a number" stand: weighted black base, steel pole, and a red ticket dispenser with a ticket showing at the front slot.</summary>
        static void TicketDispenser(MeshKit k)
        {
            k.Use(M.MetalDark);
            k.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.15f, 0f), new Vector2(0.15f, 0f), new Vector2(0.15f, 0.018f), new Vector2(0.12f, 0.035f), new Vector2(0.025f, 0.04f), new Vector2(0f, 0.04f) }, 20, Vector3.zero);
            k.Use(M.Metal);
            k.Cylinder(new Vector3(0f, 0.04f, 0f), new Vector3(0f, 1.08f, 0f), 0.016f, 0.016f, 8, false, false);
            k.Use(SignalRed);
            k.RoundedBox(new Vector3(0f, 1.17f, 0.02f), new Vector3(0.15f, 0.2f, 0.11f), 0.035f, 2);
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 1.12f, 0.076f), new Vector3(0.07f, 0.012f, 0.006f));
            k.Use(M.Paper);
            k.Box(new Vector3(0f, 1.105f, 0.095f), new Vector3(0.05f, 0.003f, 0.05f), new Vector3(35f, 0f, 0f));
            k.Collider(new Vector3(0f, 0.65f, 0f), new Vector3(0.16f, 1.3f, 0.16f));
        }

        /// <summary>
        /// A steel tripod turnstile: cabinet (0.3 wide, 0.9 long along the walking direction Z)
        /// with a reader and a green indicator on its +Z end, and a rotor on its +X side whose
        /// axis tilts up toward +Z; one arm is horizontal across the passage (+X), the other two
        /// hang at 120 degrees.
        /// </summary>
        static void Turnstile(MeshKit k)
        {
            k.Use(M.Metal);
            k.Box(new Vector3(0f, 0.43f, 0f), new Vector3(0.28f, 0.86f, 0.9f), false);
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.875f, 0f), new Vector3(0.3f, 0.03f, 0.92f));
            k.Box(new Vector3(0f, 0.895f, 0.28f), new Vector3(0.14f, 0.01f, 0.18f));
            k.Box(new Vector3(0f, 0.04f, 0f), new Vector3(0.29f, 0.08f, 0.91f), false);
            k.Use(M.Crt);
            k.Box(new Vector3(0f, 0.74f, 0.451f), new Vector3(0.06f, 0.06f, 0.004f));

            var axis = new Vector3(0f, 1f, 1f).normalized;
            // The rotor housing stands proud of the cabinet side, so the hanging arms mostly clear it.
            var mount = new Vector3(0.14f, 0.8f, 0.05f);
            var hub = mount + Vector3.right * 0.07f + axis * 0.05f;
            k.Use(M.Metal);
            k.Cylinder(mount, hub, 0.05f, 0.045f, 12);
            var a0 = Vector3.right;
            var a1 = Vector3.Cross(axis, a0);
            for (int i = 0; i < 3; i++)
            {
                float ang = i * 120f * Mathf.Deg2Rad;
                var d = a0 * Mathf.Cos(ang) + a1 * Mathf.Sin(ang);
                k.Cylinder(hub, hub + d * 0.48f, 0.017f, 0.017f, 8, false, true);
            }
            k.Collider(new Vector3(0f, 0.5f, 0f), new Vector3(0.3f, 1f, 0.9f));
            k.Collider(hub + a0 * 0.26f, new Vector3(0.48f, 0.05f, 0.05f));
        }

        /// <summary>
        /// A furled flag on a pole (plain muted cloth, no emblem): a weighted base, a steel pole
        /// with a brass finial, and cloth gathered around the pole in soft vertical folds, hanging
        /// a little off-centre. No collider.
        /// </summary>
        static void Flag(MeshKit k)
        {
            k.Use(M.MetalDark);
            k.Lathe(new[] { new Vector2(0f, 0f), new Vector2(0.18f, 0f), new Vector2(0.18f, 0f), new Vector2(0.18f, 0.02f), new Vector2(0.14f, 0.045f), new Vector2(0.03f, 0.05f), new Vector2(0f, 0.05f) }, 20, Vector3.zero);
            k.Use(M.Metal);
            k.Cylinder(new Vector3(0f, 0.05f, 0f), new Vector3(0f, 2.1f, 0f), 0.014f, 0.012f, 8, false, true);
            k.Use(M.Brass);
            var ball = new Vector2[6];
            for (int i = 0; i < ball.Length; i++)
            {
                float t = i / (float)(ball.Length - 1) * Mathf.PI;
                ball[i] = new Vector2(Mathf.Sin(t) * 0.028f, 2.128f - Mathf.Cos(t) * 0.028f);
            }
            k.Lathe(ball, 10, Vector3.zero);
            k.Cylinder(new Vector3(0f, 2.15f, 0f), new Vector3(0f, 2.2f, 0f), 0.012f, 0f, 8, false, false);

            k.Use(FlagCloth);
            const int nu = 17, nv = 12;
            const float top = 2.04f, bottom = 1.02f;
            var p = new Vector3[nu, nv];
            for (int j = 0; j < nv; j++)
            {
                float t = j / (float)(nv - 1);
                float y = Mathf.Lerp(top, bottom, t);
                float bulge = 0.022f + 0.07f * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f)) * (1f - 0.35f * t);
                // The cloth's centre drifts off the pole as it hangs, but never so far that the pole pokes through.
                var drift = new Vector3(0.035f * t * t + 0.02f * Mathf.Sin(t * Mathf.PI), 0f, 0.015f * Mathf.Sin(t * 5f)) * Mathf.Clamp01(t * 2.5f);
                drift = Vector3.ClampMagnitude(drift, Mathf.Max(0f, bulge * 0.78f - 0.016f));
                for (int i = 0; i < nu; i++)
                {
                    float a = i / (float)(nu - 1) * Mathf.PI * 2f;
                    float folds = 1f + 0.22f * Mathf.Sin(a * 6f + t * 3.5f) * Mathf.Clamp01(t * 3f);
                    var off = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (bulge * folds);
                    p[i, j] = new Vector3(0f, y, 0f) + drift + off;
                }
            }
            k.Grid(p, (i, j) => new Vector3(p[i, j].x, 0f, p[i, j].z), false, true);
        }

        /// <summary>inside_02's cafeteria table: a white round top on a black column with a cross foot.</summary>
        static void RoundTable(MeshKit k)
        {
            k.Use(M.Whiteboard);
            k.Lathe(new[] { new Vector2(0f, 0.712f), new Vector2(0.6f, 0.712f), new Vector2(0.6f, 0.712f), new Vector2(0.6f, 0.74f), new Vector2(0.6f, 0.74f), new Vector2(0f, 0.74f) }, 32, Vector3.zero);
            k.Use(M.MetalDark);
            k.Cylinder(new Vector3(0f, 0.69f, 0f), new Vector3(0f, 0.712f, 0f), 0.16f, 0.16f, 16, true, false);
            k.Cylinder(new Vector3(0f, 0.035f, 0f), new Vector3(0f, 0.69f, 0f), 0.038f, 0.038f, 10, false, false);
            k.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.7f, 0.04f, 0.07f), new Vector3(0f, 45f, 0f));
            k.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.7f, 0.04f, 0.07f), new Vector3(0f, -45f, 0f));
            k.Collider(new Vector3(0f, 0.37f, 0f), new Vector3(0.85f, 0.74f, 0.85f));
            k.Push(Vector3.zero, Quaternion.Euler(0f, 45f, 0f));
            k.Collider(new Vector3(0f, 0.71f, 0f), new Vector3(0.85f, 0.06f, 0.85f));
            k.Pop();
        }

        /// <summary>A small side table: dark wood top on a chrome tube frame with a black lower shelf.</summary>
        static void SideTable(MeshKit k)
        {
            k.Use(M.WoodDark);
            k.Box(new Vector3(0f, 0.5375f, 0f), new Vector3(0.5f, 0.025f, 0.5f));
            k.Use(M.Metal);
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                k.Cylinder(new Vector3(sx * 0.225f, 0f, sz * 0.225f), new Vector3(sx * 0.225f, 0.525f, sz * 0.225f), 0.011f, 0.011f, 6, true, false);
            for (int s = -1; s <= 1; s += 2)
            {
                k.Cylinder(new Vector3(-0.225f, 0.505f, s * 0.225f), new Vector3(0.225f, 0.505f, s * 0.225f), 0.009f, 0.009f, 6, false, false);
                k.Cylinder(new Vector3(s * 0.225f, 0.505f, -0.225f), new Vector3(s * 0.225f, 0.505f, 0.225f), 0.009f, 0.009f, 6, false, false);
            }
            k.Use(M.MetalDark);
            k.Box(new Vector3(0f, 0.15f, 0f), new Vector3(0.46f, 0.012f, 0.46f));
            k.Collider(new Vector3(0f, 0.275f, 0f), new Vector3(0.5f, 0.55f, 0.5f));
        }
    }
}
