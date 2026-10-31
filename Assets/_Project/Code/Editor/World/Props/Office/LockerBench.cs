using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Changing-room bench 1.5 x 0.45 x 0.38 m (product photo): three oak slats with rounded edges, overhanging the frame at
        /// both ends, on a cream square-tube frame (four legs, a rail under each long edge, end rails, and a lower
        /// stretcher with two cross bars). Its long side runs along X; people sit facing +Z.
        /// </summary>
        [Model(Prop.LockerBench)]
        private static void BuildLockerBench(Transform root)
        {
            const float length = 1.5f;
            const float depth = 0.38f;
            const float seatTop = 0.45f;
            const float slatThickness = 0.03f;
            const float slatWidth = 0.112f;
            const float slatGap = 0.022f;
            const float tube = 0.04f;
            const float tubeRadius = 0.005f;
            const float legX = 0.67f;
            const float legZ = 0.15f;
            const float upperRail = seatTop - slatThickness - tube / 2f;
            const float lowerRail = 0.17f;

            var wood = new MeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1) * (slatWidth + slatGap);
                wood.AddExtrusion(MeshBuilder.RoundedRectLoop(z - slatWidth / 2f, z + slatWidth / 2f, seatTop - slatThickness, seatTop, 0.007f, 3),
                    new Vector3(-length / 2f, 0f, 0f), new Vector3(length / 2f, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
            }
            wood.Attach("Slats", root, SurfaceMaterial.BenchWood);

            var steel = new MeshBuilder();
            var section = MeshBuilder.RoundedRectLoop(-tube / 2f, tube / 2f, -tube / 2f, tube / 2f, tubeRadius, 2);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    steel.AddExtrusion(section, new Vector3(sx * legX, 0f, sz * legZ), new Vector3(sx * legX, upperRail + tube / 2f, sz * legZ),
                        Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
                }
            }
            // Rails run between the legs: along X under both long edges, along Z at both ends (high and low).
            foreach (int sz in new[] { -1, 1 })
            {
                steel.AddExtrusion(section, new Vector3(-legX + tube / 2f, upperRail, sz * legZ), new Vector3(legX - tube / 2f, upperRail, sz * legZ),
                    Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
            }
            foreach (float y in new[] { upperRail, lowerRail })
            {
                foreach (int sx in new[] { -1, 1 })
                {
                    steel.AddExtrusion(section, new Vector3(sx * legX, y, -legZ + tube / 2f), new Vector3(sx * legX, y, legZ - tube / 2f),
                        Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                }
            }
            steel.AddExtrusion(section, new Vector3(-legX + tube / 2f, lowerRail, 0f), new Vector3(legX - tube / 2f, lowerRail, 0f),
                Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
            steel.Attach("Frame", root, SurfaceMaterial.BenchFrame);
            AddBoxCollider(root, new Vector3(0f, seatTop / 2f, 0f), new Vector3(length, seatTop, depth));
        }
    }
}
