using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Double RJ45 data outlet on an 80 x 80 mm plate, flush-mounted, 12.5 mm proud of the wall, detailed like the ports of the rack items: a white plate
        /// 9.5 mm thick with two real RJ45 openings (jack rectangle with the latch notch) each inside a raised surround, and in every jack the black walls, the
        /// contact carrier and its eight gold contacts, two screw heads (top and bottom, with a cross recess) and a small blue label strip under each port.
        /// The pivot is the middle of its back, +Z away from the wall.
        /// </summary>
        [Model(Prop.DataSocket)]
        private static void BuildDataSocket(Transform root)
        {
            const float plateThickness = 0.0095f;
            const float surroundTop = 0.0125f;
            const float portCentreY = 0.004f;
            WallJoint(root);
            var p = new RackParts();
            var holes = new List<(float xa, float ya, float xb, float yb)>();
            var centres = new[] { -0.0185f, 0.0185f };
            foreach (float x in centres)
            {
                Rj45Holes(holes, x, portCentreY - Rj45Height / 2f);
            }
            PlateWithHoles(p, SurfaceMaterial.DetectorWhite, -0.040f, -0.040f, 0.040f, 0.040f, 0f, plateThickness, holes);
            foreach (float x in centres)
            {
                float yLow = portCentreY - Rj45Height / 2f;
                // The raised surround: a square ring round the opening, its inner edge 2 mm from the jack.
                p.Frame(SurfaceMaterial.DetectorWhite, x - 0.0108f, yLow - 0.0040f, x + 0.0108f, yLow + Rj45TotalHeight + 0.0030f, plateThickness, surroundTop, 0.0030f);
                Rj45Inside(p, x, yLow, plateThickness, 0.0008f);
                p.Box(SurfaceMaterial.SupportBlue, x - 0.0085f, yLow - 0.0125f, plateThickness, x + 0.0085f, yLow - 0.0105f, plateThickness + 0.0004f);
            }
            foreach (float y in new[] { 0.0300f, -0.0300f })
            {
                p.Disc(SurfaceMaterial.ServerSilver, 0f, y, plateThickness + 0.0002f, 0.0035f, 14);
                p.Box(SurfaceMaterial.DriveBay, -0.0024f, y - 0.0004f, plateThickness + 0.0004f, 0.0024f, y + 0.0004f, plateThickness + 0.0006f);
                p.Box(SurfaceMaterial.DriveBay, -0.0004f, y - 0.0024f, plateThickness + 0.0004f, 0.0004f, y + 0.0024f, plateThickness + 0.0006f);
            }
            p.Attach("Socket", root);
        }
    }
}
