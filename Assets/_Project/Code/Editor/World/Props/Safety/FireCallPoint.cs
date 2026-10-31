using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Surface-mounted manual call point, 92 x 92 x 52 mm: a rounded red housing with a lid ridge, a raised frame round
        /// the recessed white window with the push symbol (a circle between two arrows), the fire pictogram above it and
        /// a status lens below. The pivot is the middle of its back, +Z away from the wall.
        /// </summary>
        [Model(Prop.FireCallPoint)]
        private static void BuildFireCallPoint(Transform root)
        {
            const float width = 0.092f;
            const float depth = 0.052f;
            const float cy = -0.004f;
            var body = new MeshBuilder();
            var panel = new MeshBuilder();
            var black = new MeshBuilder();
            var white = new MeshBuilder();
            var lens = new MeshBuilder();
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            body.AddRoundedSlab(Vector3.zero, width, width, 0.010f, depth, 0.006f);
            body.AddBox(new Vector3(-width / 2f - 0.001f, 0.0305f, 0f), new Vector3(width / 2f + 0.001f, 0.0345f, depth - 0.004f), true, false);

            // Raised frame (outer 80 x 54, opening 66 x 40) round the recessed window.
            const float frame = 0.007f;
            body.AddBox(new Vector3(-0.040f, cy + 0.027f - frame, depth - 0.001f), new Vector3(0.040f, cy + 0.027f, depth + 0.004f));
            body.AddBox(new Vector3(-0.040f, cy - 0.027f, depth - 0.001f), new Vector3(0.040f, cy - 0.027f + frame, depth + 0.004f));
            body.AddBox(new Vector3(-0.040f, cy - 0.027f + frame, depth - 0.001f), new Vector3(-0.040f + frame, cy + 0.027f - frame, depth + 0.004f));
            body.AddBox(new Vector3(0.040f - frame, cy - 0.027f + frame, depth - 0.001f), new Vector3(0.040f, cy + 0.027f - frame, depth + 0.004f));
            panel.AddBox(new Vector3(-0.033f, cy - 0.020f, depth - 0.001f), new Vector3(0.033f, cy + 0.020f, depth + 0.0008f), true, false);

            // Symbol: a circle between two arrows pointing at it.
            float zs = depth + 0.0008f;
            black.AddLathe(new[] { P(0.0078f, 0.0003f, true), P(0f, 0.0003f, true) }, 32, true, new Vector3(0f, cy, zs), toZ);
            foreach (float side in new[] { -1f, 1f })
            {
                float x0 = side * 0.030f;
                float x1 = side * 0.016f;
                black.AddBox(new Vector3(Mathf.Min(x0, x1), cy - 0.0022f, zs), new Vector3(Mathf.Max(x0, x1), cy + 0.0022f, zs + 0.0003f), true, false);
                black.AddFlatTriangle(new Vector3(x1, cy + 0.0075f, zs + 0.0003f), new Vector3(side * 0.0075f, cy, zs + 0.0003f),
                    new Vector3(x1, cy - 0.0075f, zs + 0.0003f), Vector3.forward);
            }

            // Fire pictogram above the window: a house outline with a flame in it.
            float zp = depth;
            white.AddBox(new Vector3(-0.0112f, 0.0200f, zp), new Vector3(-0.0092f, 0.0292f, zp + 0.0008f), true, false);
            white.AddBox(new Vector3(0.0092f, 0.0200f, zp), new Vector3(0.0112f, 0.0292f, zp + 0.0008f), true, false);
            white.AddBox(new Vector3(-0.0112f, 0.0200f, zp), new Vector3(0.0112f, 0.0218f, zp + 0.0008f), true, false);
            white.AddStrip(new List<Vector3> { new Vector3(-0.0140f, 0.0285f, 0f), new Vector3(0f, 0.0415f, 0f), new Vector3(0.0140f, 0.0285f, 0f) },
                Vector3.forward, 0.0008f, 0.0020f, new Vector3(0f, 0f, zp + 0.0004f), Quaternion.identity);
            white.AddFlatTriangle(new Vector3(-0.0035f, 0.0222f, zp + 0.0008f), new Vector3(0f, 0.0310f, zp + 0.0008f),
                new Vector3(0.0035f, 0.0222f, zp + 0.0008f), Vector3.forward);

            lens.AddLathe(new[] { P(0.0035f, 0.0008f, true), P(0.0025f, 0.0013f), P(0f, 0.0014f) }, 20, true, new Vector3(0f, -0.0385f, depth), toZ);

            body.Attach("Body", root, SurfaceMaterial.SafetyRed);
            panel.Attach("Window", root, SurfaceMaterial.SignFace);
            black.Attach("Symbol", root, SurfaceMaterial.BlackHousing);
            white.Attach("Pictogram", root, SurfaceMaterial.DetectorWhite);
            lens.Attach("Lens", root, SurfaceMaterial.BulbClear);
        }
    }
}
