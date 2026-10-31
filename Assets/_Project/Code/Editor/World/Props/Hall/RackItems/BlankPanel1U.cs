using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Blanking panel 1U: matt black sheet steel with the top and bottom edges folded forward (1.5 mm lips) and, at both ends, two cage-nut slots
        /// with a rack screw in each (silver head with a cross recess).
        /// </summary>
        private static void BuildBlankPanel1U(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float top = y + h;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.03f, PanelHalf, top, z - 0.0015f);
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, y, z - 0.0015f, PanelHalf, top, z);
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, top - 0.0016f, z, PanelHalf, top, z + 0.0015f);
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, y, z, PanelHalf, y + 0.0016f, z + 0.0015f);
            foreach (int sign in new[] { -1, 1 })
            {
                float sx = sign * (PanelHalf - 0.0095f);
                foreach (float cy in new[] { y + 0.0070f, top - 0.0070f })
                {
                    p.Box(SurfaceMaterial.DriveBay, sx - 0.0050f, cy - 0.0032f, z, sx + 0.0050f, cy + 0.0032f, z + 0.0004f);
                    p.Disc(SurfaceMaterial.ServerSilver, sx, cy, z + 0.0006f, 0.0046f, 14);
                    p.Box(SurfaceMaterial.DriveBay, sx - 0.0030f, cy - 0.0004f, z + 0.0007f, sx + 0.0030f, cy + 0.0004f, z + 0.0009f);
                    p.Box(SurfaceMaterial.DriveBay, sx - 0.0004f, cy - 0.0030f, z + 0.0007f, sx + 0.0004f, cy + 0.0030f, z + 0.0009f);
                }
            }
        }
    }
}
