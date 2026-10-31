using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Single-pole light switch on a flush-mounted 80 x 80 mm plate (rounded corners, bevelled edge, 6.5 mm proud of the
        /// wall) with a 56 mm square rocker in a dark square gap, 11 mm proud in all; the pivot is the middle of its back, +Z
        /// away from the wall.
        /// </summary>
        [Model(Prop.LightSwitch)]
        private static void BuildLightSwitch(Transform root)
        {
            var plate = new MeshBuilder();
            var gap = new MeshBuilder();
            var rocker = new MeshBuilder();
            plate.AddRoundedSlab(Vector3.zero, 0.080f, 0.080f, 0.006f, 0.0065f, 0.0018f);
            gap.AddBox(new Vector3(-0.0295f, -0.0295f, 0.0063f), new Vector3(0.0295f, 0.0295f, 0.0070f), true, false);
            rocker.AddRoundedSlab(new Vector3(0f, 0f, 0.0067f), 0.056f, 0.056f, 0.0025f, 0.0042f, 0.0012f);
            plate.Attach("Plate", root, SurfaceMaterial.DetectorWhite);
            gap.Attach("Gap", root, SurfaceMaterial.BlackHousing);
            rocker.Attach("Rocker", root, SurfaceMaterial.DetectorWhite);
        }
    }
}
