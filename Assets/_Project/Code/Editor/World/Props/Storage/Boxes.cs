using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Box with a loose lid: a rounded base with the lid slipped over its top, a visible step where they meet.</summary>
        private static void BuildLidBox(Transform root, float size, float height)
        {
            const float lidOverhang = 0.004f;
            float lidHeight = Mathf.Min(0.06f, height * 0.3f);
            var cardboard = new MeshBuilder();
            var gap = new MeshBuilder();
            cardboard.AddRoundedBlock(Vector3.zero, size, size, 0.004f, height - 0.004f, 0.003f);
            cardboard.AddRoundedBlock(new Vector3(0f, height - lidHeight, 0f), size + 2f * lidOverhang, size + 2f * lidOverhang, 0.006f, lidHeight, 0.004f);
            // The dark joint at the lid's lower edge, where it casts a little shadow on the base.
            gap.AddBox(new Vector3(-size / 2f - 0.0006f, height - lidHeight - 0.0015f, -size / 2f - 0.0006f), new Vector3(size / 2f + 0.0006f, height - lidHeight + 0.0005f, size / 2f + 0.0006f), false, false);
            gap.Attach("Joint", root, SurfaceMaterial.CardboardDark);
            cardboard.Attach("Cardboard", root, SurfaceMaterial.Cardboard);
        }

        /// <summary>Shipping carton: rounded body, the seam of the two top flaps along Z and an oval handle hole in each end face.</summary>
        private static void BuildCartonBox(Transform root, float size, float height)
        {
            var cardboard = new MeshBuilder();
            var seam = new MeshBuilder();
            var rim = new MeshBuilder();
            var hole = new MeshBuilder();
            cardboard.AddRoundedBlock(Vector3.zero, size, size, 0.004f, height, 0.004f);
            seam.AddBox(new Vector3(-0.0012f, height, -size / 2f + 0.002f), new Vector3(0.0012f, height + 0.0004f, size / 2f - 0.002f), true, false);
            float holeY = height * 0.66f;
            var rimFace = new MeshBuilder();
            rimFace.AddRoundedSlab(new Vector3(0f, holeY, size / 2f - 0.0002f), 0.118f, 0.036f, 0.017f, 0.0008f, 0.0003f, 6);
            var holeFace = new MeshBuilder();
            holeFace.AddRoundedSlab(new Vector3(0f, holeY, size / 2f + 0.0004f), 0.104f, 0.024f, 0.011f, 0.0008f, 0.0002f, 6);
            rim.Append(rimFace, Vector3.zero, Quaternion.identity);
            rim.Append(rimFace, Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            hole.Append(holeFace, Vector3.zero, Quaternion.identity);
            hole.Append(holeFace, Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            cardboard.Attach("Cardboard", root, SurfaceMaterial.Cardboard);
            seam.Attach("Seam", root, SurfaceMaterial.CardboardDark);
            rim.Attach("HoleRim", root, SurfaceMaterial.CardboardEdge);
            hole.Attach("Hole", root, SurfaceMaterial.CardboardDark);
        }

        [Model(Prop.BoxLidCube)]
        private static void BuildBoxLidCube(Transform root) => BuildLidBox(root, LidBoxSize, LidCubeHeight);

        [Model(Prop.BoxLidFlat)]
        private static void BuildBoxLidFlat(Transform root) => BuildLidBox(root, LidBoxSize, LidFlatHeight);

        [Model(Prop.BoxCartonCube)]
        private static void BuildBoxCartonCube(Transform root) => BuildCartonBox(root, CartonBoxSize, CartonCubeHeight);

        [Model(Prop.BoxCartonFlat)]
        private static void BuildBoxCartonFlat(Transform root) => BuildCartonBox(root, CartonBoxSize, CartonFlatHeight);
    }
}
