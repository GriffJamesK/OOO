using UnityEngine;

namespace Kit.Editor
{
    /// <summary>
    /// Every shared number of the OOO world plan (docs/ARCHITECTURE.md, "World layout"): floor
    /// heights, area envelopes, sockets where areas meet, the launch geometry and the spawn. Area
    /// builders read these instead of repeating numbers, so the pieces built in parallel line up.
    /// Lead-owned: ask before changing anything here, because other areas depend on it.
    /// </summary>
    public static class OfficeLayout
    {
        /// <summary>Floor-to-floor height (m).</summary>
        public const float Storey = 4f;

        /// <summary>Height of floor level n (L-1 = -4, L0 = 0, ... L6 = 24).</summary>
        public static float L(int n) => n * Storey;

        /// <summary>Thickness of the atrium's outer walls (centred on their planes).</summary>
        public const float AtriumWallThickness = 0.5f;

        /// <summary>Envelope from two corners (world).</summary>
        static Bounds Box(Vector3 min, Vector3 max)
        {
            var b = new Bounds();
            b.SetMinMax(min, max);
            return b;
        }

        // Area envelopes: an area builds only inside its own box (sockets excepted).
        public static readonly Bounds A1 = Box(new Vector3(-52f, 16f, -12f), new Vector3(-20f, 22f, 12f));
        public static readonly Bounds A2 = Box(new Vector3(-20f, 0f, -30f), new Vector3(20f, 34f, 30f));
        public static readonly Bounds A3 = Box(new Vector3(-35f, 12f, 30f), new Vector3(35f, 16f, 90f));
        public static readonly Bounds A4 = Box(new Vector3(20f, 4f, -25f), new Vector3(72f, 18f, 25f));
        public static readonly Bounds A5 = Box(new Vector3(-40f, -4f, -34f), new Vector3(-20f, 34f, -14f));
        public static readonly Bounds A6 = Box(new Vector3(20f, 24f, -20f), new Vector3(60f, 29f, 20f));
        public static readonly Bounds A7 = Box(new Vector3(72f, -1f, -200f), new Vector3(420f, 60f, 200f));

        /// <summary>An opening both sides keep clear: Min/Max corners (world) and the direction from the atrium into the area.</summary>
        public readonly struct Socket
        {
            public readonly Bounds Opening;
            public readonly Vector3 IntoArea;
            public Socket(Vector3 min, Vector3 max, Vector3 intoArea) { Opening = Box(min, max); IntoArea = intoArea; }
        }

        // Sockets (the atrium owns its four outer walls and cuts these openings; areas dress their side).
        public static readonly Socket A1_Transom = new Socket(new Vector3(-20.25f, 16f + 2.6f, 2f), new Vector3(-19.75f, 16f + 3.4f, 4f), Vector3.left);
        public static readonly Socket A1_Shutter = new Socket(new Vector3(-20.25f, 16f, -6f), new Vector3(-19.75f, 16f + 0.9f, -3f), Vector3.left);
        public static readonly Socket A3_Entry = new Socket(new Vector3(-2f, 12f, 29.75f), new Vector3(2f, 15f, 30.25f), Vector3.forward);
        public static readonly Socket A4_Entry = new Socket(new Vector3(19.75f, 4f, -3f), new Vector3(20.25f, 7f, 3f), Vector3.right);
        public static readonly Socket A5_L0 = new Socket(new Vector3(-20.25f, 0f, -26f), new Vector3(-19.75f, 3f, -22f), Vector3.left);
        public static readonly Socket A5_L2 = new Socket(new Vector3(-20.25f, 8f, -26f), new Vector3(-19.75f, 11f, -22f), Vector3.left);
        public static readonly Socket A6_Bridge = new Socket(new Vector3(19.75f, 24f, -2f), new Vector3(20.25f, 27f, 2f), Vector3.right);

        /// <summary>The atrium's cantilevered boardroom pod top surface (reachable only by the launch).</summary>
        public static readonly Bounds BoardroomPod = Box(new Vector3(0f, 23.6f, -6f), new Vector3(20f, 24f, 6f));

        /// <summary>Launch exit (threshold 6) on the atrium west wall: centre, facing +X tilted 30 deg up, 2.5 x 2.5 m.</summary>
        public static readonly Vector3 LaunchExitCentre = new Vector3(-20f, 29f, 0f);
        public static readonly Quaternion LaunchExitRotation = Quaternion.LookRotation(new Vector3(Mathf.Cos(30f * Mathf.Deg2Rad), Mathf.Sin(30f * Mathf.Deg2Rad), 0f), Vector3.up);
        public static readonly Vector2 LaunchOpening = new Vector2(2.5f, 2.5f);

        /// <summary>Launch hatch (threshold 6) on the A5 void floor: centre, facing +Y, 3 x 3 m.</summary>
        public static readonly Vector3 LaunchHatchCentre = new Vector3(-32f, -4f + 0.01f, -26f);
        public static readonly Quaternion LaunchHatchRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        public static readonly Vector2 HatchOpening = new Vector2(3f, 3f);

        /// <summary>The A5 void, open from the top landing (y 24) down to the hatch floor (y -4).</summary>
        public static readonly Bounds A5Void = Box(new Vector3(-36f, -4f, -30f), new Vector3(-28f, 34f, -22f));

        /// <summary>Where the player wakes: in the A1 waiting room, facing east toward the reception hatch.</summary>
        public static readonly Vector3 SpawnPosition = new Vector3(-40f, 16.05f, 0f);
        public static readonly Quaternion SpawnRotation = Quaternion.LookRotation(Vector3.right);

        /// <summary>
        /// The free-standing exterior door 000 in the A7 fog: centre of its standard Door-sized
        /// opening (bottom on the ground at y 0), front facing +X (away from the building). Paired
        /// openings should share a size and sill height so the player arrives on the floor.
        /// </summary>
        public static readonly Vector3 ExteriorDoorCentre = new Vector3(112f, 1.05f, 0f);
        public static readonly Quaternion ExteriorDoorRotation = Quaternion.LookRotation(Vector3.right);

        /// <summary>Standard door opening (m).</summary>
        public static readonly Vector2 Door = new Vector2(1f, 2.1f);
        public static readonly Vector2 ExecutiveDoor = new Vector2(1.2f, 2.4f);
    }
}
