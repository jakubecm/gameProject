using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Double RJ45 data outlet on an 80 x 80 mm plate, flush-mounted, 8 mm proud of the wall: a rounded plate, the
        /// inner cover and two port surrounds with dark openings; the pivot is the middle of its back, +Z away from the wall.
        /// </summary>
        [Model(Prop.DataSocket)]
        private static void BuildDataSocket(Transform root)
        {
            var plate = new MeshBuilder();
            var openings = new MeshBuilder();
            WallJoint(root);
            plate.AddRoundedSlab(Vector3.zero, 0.080f, 0.080f, 0.008f, 0.0035f, 0.0014f);
            plate.AddBox(new Vector3(-0.030f, -0.036f, 0.0032f), new Vector3(0.030f, 0.036f, 0.0037f), true, false);
            foreach (float x in new[] { -0.0175f, 0.0175f })
            {
                plate.AddRoundedSlab(new Vector3(x, 0f, 0.0037f), 0.022f, 0.022f, 0.005f, 0.0013f, 0.0004f, 3);
                openings.AddBox(new Vector3(x - 0.0065f, -0.0050f, 0.0048f), new Vector3(x + 0.0065f, 0.0050f, 0.0052f), true, false);
            }
            plate.Attach("Plate", root, SurfaceMaterial.DetectorWhite);
            openings.Attach("Ports", root, SurfaceMaterial.BlackHousing);
        }
    }
}
