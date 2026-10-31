using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Depth of the front plate of a rack item behind its front plane: every port is a real hole this deep.</summary>
        private const float PortDepth = 0.016f;

        /// <summary>
        /// A plate x0..x1, y0..y1 (from z0 to z1) with rectangular holes (xa, ya, xb, yb): it is cut into bars round the holes, so the openings are real.
        /// </summary>
        private static void PlateWithHoles(RackParts p, SurfaceMaterial material, float x0, float y0, float x1, float y1, float z0, float z1,
            IList<(float xa, float ya, float xb, float yb)> holes)
        {
            var edges = new SortedSet<float> { y0, y1 };
            foreach (var hole in holes)
            {
                edges.Add(Mathf.Clamp(hole.ya, y0, y1));
                edges.Add(Mathf.Clamp(hole.yb, y0, y1));
            }
            var ys = new List<float>(edges);
            for (int i = 0; i + 1 < ys.Count; i++)
            {
                float ya = ys[i];
                float yb = ys[i + 1];
                if (yb - ya < 1e-5f) continue;
                float mid = (ya + yb) / 2f;
                var inBand = new List<(float xa, float xb)>();
                foreach (var hole in holes)
                {
                    if (hole.ya <= mid && mid <= hole.yb) inBand.Add((hole.xa, hole.xb));
                }
                inBand.Sort((a, b) => a.xa.CompareTo(b.xa));
                float cursor = x0;
                foreach (var (xa, xb) in inBand)
                {
                    if (xa > cursor + 1e-5f) p.Box(material, cursor, ya, z0, xa, yb, z1);
                    cursor = Mathf.Max(cursor, xb);
                }
                if (cursor < x1 - 1e-5f) p.Box(material, cursor, ya, z0, x1, yb, z1);
            }
        }

        // ---------------------------------------------------------------- ports

        private const float Rj45Width = 0.0118f;
        private const float Rj45Height = 0.0092f;
        private const float Rj45NotchWidth = 0.0042f;
        private const float Rj45NotchHeight = 0.0022f;
        private const float UsbAWidth = 0.0125f;
        private const float UsbAHeight = 0.0053f;
        private const float SfpWidth = 0.0135f;
        private const float SfpHeight = 0.0086f;

        /// <summary>Total height of the RJ45 opening including the latch notch.</summary>
        private const float Rj45TotalHeight = Rj45Height + Rj45NotchHeight;

        /// <summary>The RJ45 opening: the jack rectangle and the narrower latch notch over its middle. (<paramref name="cx"/>: middle, <paramref name="yLow"/>: bottom.)</summary>
        private static void Rj45Holes(List<(float xa, float ya, float xb, float yb)> holes, float cx, float yLow)
        {
            holes.Add((cx - Rj45Width / 2f, yLow, cx + Rj45Width / 2f, yLow + Rj45Height));
            holes.Add((cx - Rj45NotchWidth / 2f, yLow + Rj45Height, cx + Rj45NotchWidth / 2f, yLow + Rj45TotalHeight));
        }

        /// <summary>What shows inside an RJ45 jack: black walls, the contact carrier at the back and its eight gold contacts.</summary>
        private static void Rj45Inside(RackParts p, float cx, float yLow, float zFront, float zBack)
        {
            p.Cavity(SurfaceMaterial.ServerVoid, cx - Rj45Width / 2f, yLow, cx + Rj45Width / 2f, yLow + Rj45Height, zFront, zBack);
            p.Cavity(SurfaceMaterial.ServerVoid, cx - Rj45NotchWidth / 2f, yLow + Rj45Height, cx + Rj45NotchWidth / 2f, yLow + Rj45TotalHeight, zFront, zFront - 0.006f);
            p.Box(SurfaceMaterial.DriveBay, cx - Rj45Width / 2f + 0.0012f, yLow + 0.0042f, zBack, cx + Rj45Width / 2f - 0.0012f, yLow + 0.0080f, zBack + 0.0075f);
            for (int i = 0; i < 8; i++)
            {
                float x = cx - 0.0042f + i * 0.0012f;
                p.Box(SurfaceMaterial.Brass, x - 0.0002f, yLow + 0.0034f, zBack + 0.0045f, x + 0.0002f, yLow + 0.0042f, zBack + 0.0078f);
            }
        }

        private static void UsbAHoles(List<(float xa, float ya, float xb, float yb)> holes, float cx, float yLow) =>
            holes.Add((cx - UsbAWidth / 2f, yLow, cx + UsbAWidth / 2f, yLow + UsbAHeight));

        /// <summary>Inside a USB-A port: a steel-lined hole with the blue (USB 3) contact tongue at the back.</summary>
        private static void UsbAInside(RackParts p, float cx, float yLow, float zFront, float zBack)
        {
            p.Cavity(SurfaceMaterial.ServerVoid, cx - UsbAWidth / 2f, yLow, cx + UsbAWidth / 2f, yLow + UsbAHeight, zFront, zBack);
            p.Box(SurfaceMaterial.SupportBlue, cx - 0.0052f, yLow + 0.0015f, zBack, cx + 0.0052f, yLow + 0.0034f, zBack + 0.0080f);
            p.Box(SurfaceMaterial.Brass, cx - 0.0044f, yLow + 0.0034f, zBack + 0.0010f, cx + 0.0044f, yLow + 0.0036f, zBack + 0.0070f);
        }

        private static void SfpHoles(List<(float xa, float ya, float xb, float yb)> holes, float cx, float yLow) =>
            holes.Add((cx - SfpWidth / 2f, yLow, cx + SfpWidth / 2f, yLow + SfpHeight));

        /// <summary>Inside an SFP+ cage: a deep steel cage with the module's edge connector slot at the back.</summary>
        private static void SfpInside(RackParts p, float cx, float yLow, float zFront, float zBack)
        {
            p.Cavity(SurfaceMaterial.ServerVoid, cx - SfpWidth / 2f, yLow, cx + SfpWidth / 2f, yLow + SfpHeight, zFront, zBack);
            p.Box(SurfaceMaterial.ServerSilver, cx - 0.0060f, yLow + 0.0010f, zBack, cx + 0.0060f, yLow + 0.0016f, zBack + 0.0090f);
            p.Box(SurfaceMaterial.Brass, cx - 0.0045f, yLow + 0.0016f, zBack + 0.0040f, cx + 0.0045f, yLow + 0.0019f, zBack + 0.0085f);
        }

        /// <summary>A D-sub 15 (VGA) opening: a trapezoid of three stacked rows, widest at the top (<paramref name="yTop"/> is its upper edge).</summary>
        private static void VgaHoles(List<(float xa, float ya, float xb, float yb)> holes, float cx, float yTop)
        {
            float[] widths = { 0.0150f, 0.0140f, 0.0130f };
            for (int row = 0; row < 3; row++)
            {
                float yb = yTop - row * 0.0028f;
                holes.Add((cx - widths[row] / 2f, yb - 0.0028f, cx + widths[row] / 2f, yb));
            }
        }

        /// <summary>
        /// Inside and round a VGA port: the cavity, the blue insulator at the back with its 15 pin holes in three rows of five, the silver flange round
        /// the opening and the two jack screws.
        /// </summary>
        private static void VgaInside(RackParts p, float cx, float yTop, float zFront, float zBack)
        {
            float yBottom = yTop - 3 * 0.0028f;
            p.Cavity(SurfaceMaterial.ServerVoid, cx - 0.0075f, yBottom, cx + 0.0075f, yTop, zFront, zBack);
            p.Box(SurfaceMaterial.SupportBlue, cx - 0.0068f, yBottom + 0.0006f, zBack, cx + 0.0068f, yTop - 0.0006f, zBack + 0.0040f);
            for (int row = 0; row < 3; row++)
            {
                float y = yTop - (row + 0.5f) * 0.0028f;
                for (int i = 0; i < 5; i++)
                {
                    p.Disc(SurfaceMaterial.ServerVoid, cx + (i - 2) * 0.0024f + (row == 1 ? 0.0012f : 0f), y, zBack + 0.0041f, 0.00065f, 8);
                }
            }
            p.Frame(SurfaceMaterial.ServerSilver, cx - 0.0095f, yBottom - 0.0016f, cx + 0.0095f, yTop + 0.0016f, zFront, zFront + 0.0012f, 0.0016f);
            foreach (int sign in new[] { -1, 1 })
            {
                p.Disc(SurfaceMaterial.ServerSilver, cx + sign * 0.0120f, (yTop + yBottom) / 2f, zFront + 0.0004f, 0.0028f, 10);
                p.Disc(SurfaceMaterial.ServerVoid, cx + sign * 0.0120f, (yTop + yBottom) / 2f, zFront + 0.0008f, 0.0010f, 8);
            }
        }

        // ---------------------------------------------------------------- icons next to LEDs and buttons

        private const float IconStep = 0.0004f;

        /// <summary>The power symbol: a ring open at the top with a bar through the gap, on a button or beside a LED.</summary>
        private static void IconPower(RackParts p, float x, float y, float z, float r)
        {
            p.Disc(SurfaceMaterial.ServerSilver, x, y, z, r, 16);
            p.Disc(SurfaceMaterial.RackBlack, x, y, z + IconStep, r * 0.72f, 16);
            p.Box(SurfaceMaterial.RackBlack, x - r * 0.3f, y + r * 0.1f, z + IconStep, x + r * 0.3f, y + r * 1.05f, z + 2 * IconStep);
            p.Box(SurfaceMaterial.ServerSilver, x - r * 0.11f, y - r * 0.05f, z + 2 * IconStep, x + r * 0.11f, y + r * 0.95f, z + 3 * IconStep);
        }

        /// <summary>The system-health symbol: a plus sign.</summary>
        private static void IconHealth(RackParts p, float x, float y, float z, float r)
        {
            p.Box(SurfaceMaterial.ServerSilver, x - r, y - r * 0.3f, z, x + r, y + r * 0.3f, z + IconStep);
            p.Box(SurfaceMaterial.ServerSilver, x - r * 0.3f, y - r, z, x + r * 0.3f, y + r, z + IconStep);
        }

        /// <summary>The network symbol: a box over two boxes joined by a bus.</summary>
        private static void IconNic(RackParts p, float x, float y, float z, float r)
        {
            p.Box(SurfaceMaterial.ServerSilver, x - r * 0.4f, y + r * 0.2f, z, x + r * 0.4f, y + r, z + IconStep);
            p.Box(SurfaceMaterial.ServerSilver, x - r * 0.1f, y - r * 0.2f, z, x + r * 0.1f, y + r * 0.2f, z + IconStep);
            p.Box(SurfaceMaterial.ServerSilver, x - r * 0.8f, y - r * 0.3f, z, x + r * 0.8f, y - r * 0.1f, z + IconStep);
            p.Box(SurfaceMaterial.ServerSilver, x - r, y - r, z, x - r * 0.4f, y - r * 0.3f, z + IconStep);
            p.Box(SurfaceMaterial.ServerSilver, x + r * 0.4f, y - r, z, x + r, y - r * 0.3f, z + IconStep);
        }

        /// <summary>The unit-identification symbol: a ring with a dot.</summary>
        private static void IconUid(RackParts p, float x, float y, float z, float r)
        {
            p.Disc(SurfaceMaterial.ServerSilver, x, y, z, r, 14);
            p.Disc(SurfaceMaterial.RackBlack, x, y, z + IconStep, r * 0.68f, 14);
            p.Disc(SurfaceMaterial.ServerSilver, x, y, z + 2 * IconStep, r * 0.28f, 10);
        }

        /// <summary>The disk symbol: a platter with a hub.</summary>
        private static void IconDisk(RackParts p, float x, float y, float z, float r)
        {
            p.Disc(SurfaceMaterial.ServerSilver, x, y, z, r, 16);
            p.Disc(SurfaceMaterial.RackBlack, x, y, z + IconStep, r * 0.62f, 16);
            p.Disc(SurfaceMaterial.ServerSilver, x, y, z + 2 * IconStep, r * 0.22f, 10);
        }

        /// <summary>A status LED box, centred on (x, y).</summary>
        private static void LedBox(RackParts p, SurfaceMaterial material, float x, float y, float z, float width = 0.0040f, float height = 0.0030f) =>
            p.Box(material, x - width / 2f, y - height / 2f, z, x + width / 2f, y + height / 2f, z + 0.0015f);
    }
}
