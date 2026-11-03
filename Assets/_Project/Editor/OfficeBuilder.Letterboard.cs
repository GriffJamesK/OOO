using Kit.Gameplay.Letterboards;
using UnityEditor;

namespace Kit.Editor
{
    public static partial class OfficeBuilder
    {
        /// <summary>
        /// Points LetterboardConfig at the felt and tile materials, so boards built afterwards (and
        /// the tiles in the player's hand) use them. The materials themselves come from the Look
        /// builder; until they exist OfficeKit.Mat hands back a stand-in, which a later build fixes.
        /// </summary>
        static partial void BuildLetterboardAssets()
        {
            var config = OfficeKit.Config<LetterboardConfig>();
            Letterboard_Assign(config);
            EditorUtility.SetDirty(config);
        }

        /// <summary>Only material references are written; tuned numbers in the asset are left alone.</summary>
        static void Letterboard_Assign(LetterboardConfig config)
        {
            config.FeltMaterial = OfficeKit.Mat(OfficeKit.M.Felt);
            config.TileMaterial = OfficeKit.Mat(OfficeKit.M.TileWhite);
        }
    }
}
