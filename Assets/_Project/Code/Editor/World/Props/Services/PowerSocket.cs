using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Flush-mounted 230 V socket (type E: round recess, two shuttered holes and the earth pin) on an 80 x 80 mm plate,
        /// 10 mm proud of the wall; the pivot is the middle of its back, +Z away from the wall.
        /// </summary>
        [Model(Prop.PowerSocket)]
        private static void BuildPowerSocket(Transform root)
        {
            var plate = new MeshBuilder();
            var ring = new MeshBuilder();
            var holes = new MeshBuilder();
            var pin = new MeshBuilder();
            var toZ = Quaternion.Euler(90f, 0f, 0f);
            const float top = 0.0035f;
            WallJoint(root);
            plate.AddRoundedSlab(Vector3.zero, 0.080f, 0.080f, 0.006f, top, 0.0012f);
            // Round recess: a raised collar, then the recess floor (set 1.5 mm back from the collar's top).
            ring.AddLathe(new[]
            {
                P(0.0300f, 0f), P(0.0300f, 0.0018f), P(0.0288f, 0.0024f), P(0.0262f, 0.0022f), P(0.0255f, 0.0016f, true), P(0.0255f, 0.0004f, true), P(0f, 0.0004f, true),
            }, 48, true, new Vector3(0f, 0f, top), toZ);
            // The two plug holes (4 mm, dark) and the earth pin between them.
            foreach (float x in new[] { -0.0105f, 0.0105f })
            {
                holes.AddLathe(new[] { P(0.0042f, 0.0006f, true), P(0f, 0.0006f, true) }, 20, true, new Vector3(x, -0.0060f, top), toZ);
            }
            pin.AddLathe(new[] { P(0.0023f, 0.0006f, true), P(0.0023f, 0.0040f), P(0.0016f, 0.0052f), P(0f, 0.0054f) }, 16, true, new Vector3(0f, 0.0115f, top), toZ);

            plate.Attach("Plate", root, SurfaceMaterial.DetectorWhite);
            ring.Attach("Recess", root, SurfaceMaterial.DetectorWhite);
            holes.Attach("Holes", root, SurfaceMaterial.BlackHousing);
            pin.Attach("EarthPin", root, SurfaceMaterial.Chrome);
        }
    }
}
