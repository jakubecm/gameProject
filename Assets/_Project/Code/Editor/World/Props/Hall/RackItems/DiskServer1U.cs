using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Disk server 1U (an SFF 1U server such as a ProLiant DL360): two rows of five horizontal drive trays, each tray a separate carrier (lighter
        /// face, a slotted pull handle, a round release button, a dark red status window) with a black void between trays and between the rows. Left
        /// ear: a real VGA port. Right: the control panel with the power button, health and network LEDs and the UID LED beside their symbols, a USB-A
        /// port and the iLO RJ45 port, all real holes.
        /// </summary>
        private static void BuildDiskServer1U(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float zb = z - PortDepth;
            float top = y + h;
            float left = -PanelHalf + 0.030f;
            float right = PanelHalf - 0.075f;
            float panelRight = PanelHalf - 0.020f;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, top, zb);

            // Left ear with the VGA port, a slim rounded edge on the outside.
            float vx = -PanelHalf + 0.015f;
            float vTop = y + h / 2f + 0.0042f;
            var holes = new List<(float, float, float, float)>();
            VgaHoles(holes, vx, vTop);
            PlateWithHoles(p, SurfaceMaterial.RackBlack, -PanelHalf, y, left, top, zb, z + 0.009f, holes);
            VgaInside(p, vx, vTop, z + 0.009f, zb);
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf + 0.002f, y + 0.002f, z + 0.009f, -PanelHalf + 0.005f, top - 0.002f, z + 0.0105f);

            // Right ear (solid).
            p.Box(SurfaceMaterial.RackBlack, panelRight, y, zb, PanelHalf, top, z + 0.009f);
            p.Box(SurfaceMaterial.RackBlack, PanelHalf - 0.005f, y + 0.002f, z + 0.009f, PanelHalf - 0.002f, top - 0.002f, z + 0.0105f);

            // The control panel.
            float zp = z + 0.0020f;
            float usbX = right + 0.0185f;
            float ethX = right + 0.0345f;
            float usbY = y + 0.0058f;
            float ethY = y + 0.0040f;
            holes = new List<(float, float, float, float)>();
            UsbAHoles(holes, usbX, usbY);
            Rj45Holes(holes, ethX, ethY);
            PlateWithHoles(p, SurfaceMaterial.RackBlack, right, y, panelRight, top, zb, zp, holes);
            UsbAInside(p, usbX, usbY, zp, zb);
            Rj45Inside(p, ethX, ethY, zp, zb);
            p.Frame(SurfaceMaterial.ServerSilver, ethX - Rj45Width / 2f - 0.0016f, ethY - 0.0016f, ethX + Rj45Width / 2f + 0.0016f, ethY + Rj45Height + 0.0016f, zp, zp + 0.0010f, 0.0009f);
            float upper = top - 0.0105f;
            p.Disc(SurfaceMaterial.ServerSilver, right + 0.0075f, upper, zp, 0.0050f, 18);
            p.Disc(SurfaceMaterial.ServerBlack, right + 0.0075f, upper, zp + 0.0004f, 0.0039f, 18);
            IconPower(p, right + 0.0075f, upper, zp + 0.0008f, 0.0021f);
            IconHealth(p, right + 0.0180f, upper, zp, 0.0022f);
            LedBox(p, SurfaceMaterial.SafetyGreen, right + 0.0228f, upper, zp, 0.0036f, 0.0028f);
            IconNic(p, right + 0.0300f, upper, zp, 0.0022f);
            LedBox(p, index % 4 == 3 ? SurfaceMaterial.LedAmber : SurfaceMaterial.SafetyGreen, right + 0.0348f, upper, zp, 0.0036f, 0.0028f);
            IconUid(p, right + 0.0050f, y + 0.0095f, zp, 0.0022f);
            LedBox(p, SurfaceMaterial.SupportBlue, right + 0.0102f, y + 0.0095f, zp, 0.0036f, 0.0028f);

            // The drive cage: a black void with two rows of five horizontal trays.
            p.Box(SurfaceMaterial.ServerVoid, left, y + 0.002f, zb, right, top - 0.002f, zb + 0.0005f);
            p.Frame(SurfaceMaterial.ServerSilver, left, y + 0.0015f, right, top - 0.0015f, zb, z + 0.0035f, 0.0016f);
            const int rows = 2;
            const int columns = 5;
            const float gap = 0.0025f;
            float innerX0 = left + 0.0016f + 0.001f;
            float innerX1 = right - 0.0016f - 0.001f;
            float innerY0 = y + 0.0015f + 0.0016f + 0.0008f;
            float innerY1 = top - 0.0015f - 0.0016f - 0.0008f;
            float trayWidth = (innerX1 - innerX0 - (columns - 1) * gap) / columns;
            float trayHeight = (innerY1 - innerY0 - (rows - 1) * gap) / rows;
            for (int r = 0; r < rows; r++)
            {
                float ya = innerY0 + r * (trayHeight + gap);
                float yb = ya + trayHeight;
                float cy = (ya + yb) / 2f;
                for (int c = 0; c < columns; c++)
                {
                    float xa = innerX0 + c * (trayWidth + gap);
                    float xb = xa + trayWidth;
                    // The carrier face, then the handle (a darker bar with three slots), the round release button and the status window.
                    p.Box(SurfaceMaterial.DriveBay, xa, ya, zb + 0.002f, xb, yb, z + 0.0030f);
                    p.Box(SurfaceMaterial.RackBlack, xa + 0.0100f, ya + 0.0022f, z + 0.0030f, xb - 0.0100f, yb - 0.0022f, z + 0.0058f);
                    for (int slot = 0; slot < 3; slot++)
                    {
                        float sx = xa + 0.0200f + slot * 0.0150f;
                        p.Box(SurfaceMaterial.ServerVoid, sx, cy - 0.0030f, z + 0.0058f, sx + 0.0100f, cy + 0.0030f, z + 0.0061f);
                    }
                    p.Disc(SurfaceMaterial.ServerVoid, xb - 0.0052f, cy, z + 0.0030f, 0.0042f, 14);
                    p.Disc(SurfaceMaterial.RackBlack, xb - 0.0052f, cy, z + 0.0034f, 0.0033f, 14);
                    p.Box(SurfaceMaterial.ServerVoid, xa + 0.0010f, cy - 0.0042f, z + 0.0030f, xa + 0.0082f, cy + 0.0042f, z + 0.0042f);
                    p.Box(SurfaceMaterial.LatchRed, xa + 0.0020f, cy - 0.0032f, z + 0.0042f, xa + 0.0072f, cy + 0.0032f, z + 0.0048f);
                }
            }
        }
    }
}
