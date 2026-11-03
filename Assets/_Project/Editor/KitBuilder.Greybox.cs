using UnityEditor;
using UnityEngine;

namespace Kit.Editor
{
    public static partial class KitBuilder
    {
        const string GreyboxDir = PrefabDir + "/Greybox";

        /// <summary>
        /// Unit blocks at real dimensions so agents build to scale. Every pivot sits at the
        /// bottom centre, so a prefab dropped at y = 0 rests on the floor.
        /// </summary>
        static void BuildGreyboxPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(GreyboxDir)) AssetDatabase.CreateFolder(PrefabDir, "Greybox");
            var wall = Mat("Mat_Grid_Wall");
            var floor = Mat("Mat_Grey_Floor");

            // 1 m cube.
            var block = new GameObject("Greybox_Block_1m");
            Box(block.transform, "Mesh", wall, new Vector3(0f, 0.5f, 0f), Vector3.one);
            SavePrefab(block, GreyboxDir + "/Greybox_Block_1m.prefab");

            // Door frame: 1 m x 2 m opening (fits the 1.8 m player), 0.25 m posts and lintel.
            var door = new GameObject("Greybox_DoorFrame_2m");
            Box(door.transform, "Post_L", wall, new Vector3(-0.625f, 1.125f, 0f), new Vector3(0.25f, 2.25f, 0.25f));
            Box(door.transform, "Post_R", wall, new Vector3(0.625f, 1.125f, 0f), new Vector3(0.25f, 2.25f, 0.25f));
            Box(door.transform, "Lintel", wall, new Vector3(0f, 2.125f, 0f), new Vector3(1.0f, 0.25f, 0.25f));
            SavePrefab(door, GreyboxDir + "/Greybox_DoorFrame_2m.prefab");

            // One stair step: 1 m wide, 0.25 m rise (under the controller's 0.3 m step offset), 0.3 m tread.
            var step = new GameObject("Greybox_Step_1m");
            Box(step.transform, "Mesh", floor, new Vector3(0f, 0.125f, 0f), new Vector3(1f, 0.25f, 0.3f));
            SavePrefab(step, GreyboxDir + "/Greybox_Step_1m.prefab");

            // Wall panel: 4 m wide, 3 m tall, 0.25 m thick.
            var panel = new GameObject("Greybox_Wall_4m");
            Box(panel.transform, "Mesh", wall, new Vector3(0f, 1.5f, 0f), new Vector3(4f, 3f, 0.25f));
            SavePrefab(panel, GreyboxDir + "/Greybox_Wall_4m.prefab");

            // Scale reference: 3 m pole banded every metre, beside a 1.8 m human figure.
            var reference = new GameObject("Greybox_ScaleRef_3m");
            for (int i = 0; i < 3; i++)
            {
                var band = Primitive(PrimitiveType.Cube, $"Band_{i + 1}m", reference.transform,
                    i % 2 == 0 ? Mat("Mat_Grey_Wall") : Mat("Mat_Pastel_Butter"));
                band.transform.localPosition = new Vector3(0f, i + 0.5f, 0f);
                band.transform.localScale = new Vector3(0.15f, 1f, 0.15f);
            }
            var figure = Primitive(PrimitiveType.Capsule, "Human_1.8m", reference.transform, Mat("Mat_Pastel_Lilac"));
            figure.transform.localPosition = new Vector3(0.6f, 0.9f, 0f);
            figure.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            SavePrefab(reference, GreyboxDir + "/Greybox_ScaleRef_3m.prefab");
        }

        static GameObject Box(Transform parent, string name, Material material, Vector3 center, Vector3 size)
        {
            var go = SolidPrimitive(PrimitiveType.Cube, name, parent, material);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            return go;
        }
    }
}
