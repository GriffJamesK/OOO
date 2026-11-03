using System;
using System.Collections.Generic;
using UnityEngine;
using M = Kit.Editor.OfficeKit.M;
using P = Kit.Editor.OfficeKit.PropIds;

namespace Kit.Editor
{
    /// <summary>
    /// The geometry of every office prop, one method per prop id, each writing into a MeshKit in
    /// prop space: metres, pivot at bottom-centre, front facing +Z (a seated person faces +Z).
    /// Colliders are recorded on the kit next to the geometry they wrap, so composites (the
    /// cubicle workstation, the chair row) inherit them. OfficeBuilder.Props.cs turns each recipe
    /// into a mesh asset and a prefab. Kept apart from OfficeBuilder so the builder file stays a
    /// short orchestration and the shapes can be split by theme (Seating, Office, Plants, Lobby).
    /// </summary>
    public static partial class PropShapes
    {
        // Tinted materials the office palette lacks. They are copies of an OfficeKit.M material
        // with a new colour, generated next to the meshes (Generated/Props/<name>.mat) by
        // OfficeBuilder.Props.cs. Proposed for the Look worker's M table in a later pass.
        public const string WaterBlue = "Mat_Prop_WaterBlue";   // water cooler bottle: the one blue accent
        public const string SignalRed = "Mat_Prop_SignalRed";   // "take a number" dispenser
        public const string FlagCloth = "Mat_Prop_FlagCloth";   // plain muted flag cloth
        public const string Fabric = "Mat_Prop_Fabric";         // grey office-chair upholstery (Office_01)

        /// <summary>A generated tint: base material, colour, smoothness, and whether to drop the base texture.</summary>
        public readonly struct Tint
        {
            public readonly string Name, Base;
            public readonly Color Color;
            public readonly float Smoothness;
            public readonly bool ClearMap;
            public Tint(string name, string baseMat, Color color, float smoothness, bool clearMap)
            { Name = name; Base = baseMat; Color = color; Smoothness = smoothness; ClearMap = clearMap; }
        }

        /// <summary>The generated tints (see the constants above).</summary>
        public static readonly Tint[] Tints =
        {
            new Tint(WaterBlue, M.Glass, new Color(0.35f, 0.62f, 0.86f, 0.55f), 0.9f, false),
            new Tint(SignalRed, M.Plastic, new Color(0.72f, 0.12f, 0.10f), 0.45f, true),
            new Tint(FlagCloth, M.Felt, new Color(0.46f, 0.43f, 0.34f), 0.1f, true),
            new Tint(Fabric, M.Felt, new Color(0.52f, 0.50f, 0.47f), 0.12f, true),
        };

        /// <summary>Every prop id with its builder. A test or the gallery can iterate this; OfficeBuilder builds one prefab per entry.</summary>
        public static readonly Dictionary<string, Action<MeshKit>> Recipes = new Dictionary<string, Action<MeshKit>>
        {
            { P.ShellChair, ShellChair }, { P.ShellChairRow, ShellChairRow }, { P.RoundTable, RoundTable },
            { P.FoldingChair, FoldingChair }, { P.SwivelChair, SwivelChair }, { P.Desk, Desk },
            { P.CrtMonitor, CrtMonitor }, { P.Keyboard, Keyboard }, { P.CubicleWorkstation, CubicleWorkstation },
            { P.CubiclePartition, CubiclePartition }, { P.FilingCabinet, FilingCabinet }, { P.ArchiveShelf, ArchiveShelf },
            { P.Planter, Planter }, { P.PottedPlant, PottedPlant }, { P.Tree, Tree },
            { P.Bench, Bench }, { P.LeatherSofa, LeatherSofa }, { P.SideTable, SideTable },
            { P.VendingMachine, VendingMachine }, { P.WaterCooler, WaterCooler }, { P.CoffeeTrolley, CoffeeTrolley },
            { P.WallClock, WallClock }, { P.Corkboard, Corkboard }, { P.Whiteboard, Whiteboard },
            { P.BoardroomTable, BoardroomTable }, { P.ReceptionDesk, ReceptionDesk }, { P.ServingCounter, ServingCounter },
            { P.TrayStack, TrayStack }, { P.Bin, Bin }, { P.PaperStack, PaperStack },
            { P.DeskLamp, DeskLamp }, { P.Telephone, Telephone }, { P.CoatRack, CoatRack },
            { P.TicketDispenser, TicketDispenser }, { P.Turnstile, Turnstile }, { P.Flag, Flag },
        };

        // ------------------------------------------------------------------ shared helpers

        /// <summary>Hermite smoothstep, for easing widths and falloffs.</summary>
        static float Smooth(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Linear interpolation through (key, value) pairs with smoothstep easing between keys.</summary>
        static float Keys(float x, params Vector2[] keys)
        {
            if (x <= keys[0].x) return keys[0].y;
            for (int i = 1; i < keys.Length; i++)
                if (x <= keys[i].x)
                    return Mathf.Lerp(keys[i - 1].y, keys[i].y, Smooth(keys[i - 1].x, keys[i].x, x));
            return keys[keys.Length - 1].y;
        }

        /// <summary>
        /// A cushion (local frame: width along X, depth along Z, thickness along Y) whose top face
        /// is a tufted grid: soft rolled edges, a gentle crown, and button dents on a
        /// <paramref name="cols"/> x <paramref name="rows"/> pattern. The dents only show through
        /// shading, which is exactly how buttoned upholstery reads at a distance. Rotate the frame
        /// to use it as a back cushion (local +Y becomes the front).
        /// </summary>
        static void Tufted(MeshKit k, Vector3 c, float width, float depth, float thick, int cols, int rows, float dent = 0.018f, float roll = 0.035f)
        {
            const int sub = 4;
            int nx = cols * sub + 1, nz = rows * sub + 1;
            float hx = width * 0.5f, hz = depth * 0.5f, top = thick * 0.5f;
            const float rollWidth = 0.05f, sigma = 0.022f;
            var p = new Vector3[nx, nz];
            for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float x = -hx + width * i / (nx - 1), z = -hz + depth * j / (nz - 1);
                float edge = Mathf.Min(hx - Mathf.Abs(x), hz - Mathf.Abs(z));
                float y = top - roll * (1f - Smooth(0f, rollWidth, edge));
                y += 0.012f * (1f - (x / hx) * (x / hx)) * (1f - (z / hz) * (z / hz));
                for (int bi = 0; bi < cols; bi++)
                for (int bj = 0; bj < rows; bj++)
                {
                    float bx = -hx + width * (bi + 0.5f) / cols, bz = -hz + depth * (bj + 0.5f) / rows;
                    float d2 = (x - bx) * (x - bx) + (z - bz) * (z - bz);
                    y -= dent * Mathf.Exp(-d2 / (2f * sigma * sigma));
                }
                p[i, j] = c + new Vector3(x, y, z);
            }
            k.Grid(p, (i, j) => Vector3.up);
            float sideH = thick - roll;
            k.Box(c + new Vector3(0f, -top + sideH * 0.5f, 0f), new Vector3(width, sideH, depth), true, false);
        }
    }
}
