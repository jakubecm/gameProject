using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Closed server 3U after an Inter-Tech 3U chassis: black steel, the sheet folded round the front, two large drive-bay covers with punched
        /// slot-and-hole rows (<c>T_ChassisVent</c>) standing proud in two steps, between them a lock panel with a round key lock with two wings, power and disk
        /// LEDs and two USB 3.0 ports (blue tongues in steel shells), and at both ends an ear with a rack handle (a loop with a ribbed grip) and the
        /// screw slots.
        /// </summary>
        private static void BuildClosedServer3U(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float top = y + h;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, top, z - PortDepth);
            // The front channel: top and bottom flanges folded forward across the whole front.
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, top - 0.008f, z - 0.006f, PanelHalf, top, z + 0.003f);
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, y, z - 0.006f, PanelHalf, y + 0.008f, z + 0.003f);

            foreach (int sign in new[] { -1, 1 })
            {
                float outer = sign * PanelHalf;
                float inner = sign * (PanelHalf - 0.030f);
                float x0 = Mathf.Min(outer, inner);
                float x1 = Mathf.Max(outer, inner);
                p.Box(SurfaceMaterial.RackBlack, x0, y, z - 0.006f, x1, top, z);
                // Handle: two stubs at the ends of a vertical bar standing 32 mm out, ribbed grip (five bumps) facing the viewer.
                float b0 = sign * (PanelHalf - 0.022f);
                float b1 = sign * (PanelHalf - 0.006f);
                float bx0 = Mathf.Min(b0, b1);
                float bx1 = Mathf.Max(b0, b1);
                p.Box(SurfaceMaterial.RackBlack, bx0, y + 0.004f, z, bx1, y + 0.020f, z + 0.034f);
                p.Box(SurfaceMaterial.RackBlack, bx0, top - 0.020f, z, bx1, top - 0.004f, z + 0.034f);
                p.Box(SurfaceMaterial.RackBlack, bx0, y + 0.004f, z + 0.026f, bx1, top - 0.004f, z + 0.034f);
                for (int i = 0; i < 5; i++)
                {
                    float ry = y + 0.026f + i * (h - 0.052f) / 4f;
                    p.Box(SurfaceMaterial.ServerBlack, bx0 + 0.001f, ry - 0.005f, z + 0.034f, bx1 - 0.001f, ry + 0.005f, z + 0.0365f);
                }
                // Screw slots in the ear between the handle and the cover.
                float sx = sign * (PanelHalf - 0.0045f);
                for (int i = 0; i < 3; i++)
                {
                    float sy = y + h * (0.2f + 0.3f * i);
                    p.Box(SurfaceMaterial.DriveBay, sx - 0.0025f, sy - 0.006f, z + 0.0005f, sx + 0.0025f, sy + 0.006f, z + 0.0012f);
                }
            }

            // Drive-bay covers: a plate and a smaller, higher one on it (two steps), the vent texture on the top face.
            foreach (int sign in new[] { -1, 1 })
            {
                float c0 = sign < 0 ? -0.208f : 0.052f;
                float c1 = sign < 0 ? -0.052f : 0.208f;
                p.Box(SurfaceMaterial.DriveBay, c0 - 0.002f, y + 0.008f, z - 0.004f, c1 + 0.002f, top - 0.008f, z + 0.002f);
                p.Box(SurfaceMaterial.RackBlack, c0, y + 0.010f, z - 0.004f, c1, top - 0.010f, z + 0.0045f);
                p.Box(SurfaceMaterial.ServerBlack, c0 + 0.004f, y + 0.014f, z + 0.0045f, c1 - 0.004f, top - 0.014f, z + 0.0060f);
                p.TexturedPanel(SurfaceMaterial.ChassisVent, c0 + 0.007f, y + 0.017f, c1 - 0.007f, top - 0.017f, z + 0.0061f, ChassisVentTile, ChassisVentTile);
            }

            // Lock panel in the middle: a plate with the two USB 3.0 ports as real holes, the key lock above them.
            float zp = z + 0.0050f;
            var holes = new List<(float, float, float, float)>();
            UsbAHoles(holes, 0f, y + 0.0200f);
            UsbAHoles(holes, 0f, y + 0.0330f);
            PlateWithHoles(p, SurfaceMaterial.RackBlack, -0.040f, y + 0.003f, 0.040f, top - 0.003f, z - PortDepth, zp, holes);
            UsbAInside(p, 0f, y + 0.0200f, zp, z - PortDepth);
            UsbAInside(p, 0f, y + 0.0330f, zp, z - PortDepth);
            p.Frame(SurfaceMaterial.ServerSilver, -UsbAWidth / 2f - 0.0012f, y + 0.0188f, UsbAWidth / 2f + 0.0012f, y + 0.0253f, zp, zp + 0.0008f, 0.0008f);
            p.Frame(SurfaceMaterial.ServerSilver, -UsbAWidth / 2f - 0.0012f, y + 0.0318f, UsbAWidth / 2f + 0.0012f, y + 0.0383f, zp, zp + 0.0008f, 0.0008f);
            p.Box(SurfaceMaterial.ServerVoid, -0.036f, top - 0.0075f, zp, 0.036f, top - 0.0070f, zp + 0.0004f);
            // Key lock: a round black knob with two wings, a silver lock face with the key slot.
            float ky = top - 0.026f;
            p.Disc(SurfaceMaterial.ServerVoid, 0f, ky, zp + 0.0006f, 0.0128f, 20);
            p.Disc(SurfaceMaterial.RackBlack, 0f, ky, zp + 0.0030f, 0.0100f, 20);
            p.Box(SurfaceMaterial.RackBlack, -0.0125f, ky - 0.0035f, zp + 0.0006f, 0.0125f, ky + 0.0035f, zp + 0.0075f);
            p.Disc(SurfaceMaterial.ServerSilver, 0f, ky, zp + 0.0076f, 0.0052f, 16);
            p.Box(SurfaceMaterial.ServerVoid, -0.0040f, ky - 0.0004f, zp + 0.0077f, 0.0040f, ky + 0.0004f, zp + 0.0079f);
            // Power LED and disk LED, each with its symbol beside it, in the row above the ports.
            float ly = y + 0.0575f;
            IconPower(p, -0.0285f, ly, zp, 0.0030f);
            LedBox(p, SurfaceMaterial.SafetyGreen, -0.0205f, ly, zp);
            IconDisk(p, 0.0085f, ly, zp, 0.0030f);
            LedBox(p, index % 4 == 3 ? SurfaceMaterial.LedAmber : SurfaceMaterial.SafetyGreen, 0.0165f, ly, zp);
        }
    }
}
