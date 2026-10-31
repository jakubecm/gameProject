using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Disk server 2U after the front of an HPE ProLiant DL380 Gen10 (24 SFF). Black bezel; the drive cage in three silver-outlined groups of eight
        /// vertical trays, every tray a separate carrier (a lighter face, a slotted release lever, a silver label tab, a round release button and a dark
        /// red status window) with a 2.5 mm black void between neighbours. Left ear: a real VGA port (stepped D-shaped opening, blue insulator with 15 pin
        /// holes, jack screws) over the drive-box tag. Right: the control panel with the power button, the health, network and UID LEDs each beside its
        /// symbol, a USB-A port (blue tongue) and the iLO RJ45 port, all real holes; the logo and model plate on the right ear.
        /// </summary>
        private static void BuildDiskServer2U(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float zb = z - PortDepth;
            float top = y + h;
            float left = -PanelHalf + 0.034f;
            float right = PanelHalf - 0.058f;
            float panelRight = PanelHalf - 0.026f;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, top, zb);

            // Left ear with the VGA port.
            float vx = -PanelHalf + 0.017f;
            float vTop = top - 0.0170f;
            var holes = new List<(float, float, float, float)>();
            VgaHoles(holes, vx, vTop);
            PlateWithHoles(p, SurfaceMaterial.RackBlack, -PanelHalf, y, left, top, zb, z + 0.010f, holes);
            VgaInside(p, vx, vTop, z + 0.010f, zb);
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf + 0.002f, y + 0.002f, z + 0.010f, -PanelHalf + 0.006f, top - 0.002f, z + 0.012f);
            // The drive-box tag: a silver plate with a printed grid and a pull tab.
            p.Box(SurfaceMaterial.ServerSilver, -PanelHalf + 0.004f, y + 0.005f, z + 0.010f, -PanelHalf + 0.030f, y + 0.034f, z + 0.0108f);
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf + 0.007f, y + 0.020f, z + 0.0108f, -PanelHalf + 0.027f, y + 0.031f, z + 0.0112f);
            p.Disc(SurfaceMaterial.ServerBlack, -PanelHalf + 0.0115f, y + 0.0145f, z + 0.0109f, 0.0028f, 10);
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf + 0.017f, y + 0.012f, z + 0.0108f, -PanelHalf + 0.027f, y + 0.017f, z + 0.0112f);

            // Right ear (solid) with the logo plate, and the control panel plate beside the cage.
            p.Box(SurfaceMaterial.RackBlack, panelRight, y, zb, PanelHalf, top, z + 0.010f);
            p.Box(SurfaceMaterial.RackBlack, PanelHalf - 0.006f, y + 0.002f, z + 0.010f, PanelHalf - 0.002f, top - 0.002f, z + 0.012f);
            float rx = PanelHalf - 0.013f;
            p.Box(SurfaceMaterial.ServerBlack, rx - 0.0095f, top - 0.0345f, z + 0.0100f, rx + 0.0095f, top - 0.0155f, z + 0.0112f);
            p.Box(SurfaceMaterial.SafetyGreen, rx - 0.0075f, top - 0.0275f, z + 0.0112f, rx + 0.0075f, top - 0.0215f, z + 0.0117f);
            p.Box(SurfaceMaterial.ServerSilver, rx - 0.0075f, top - 0.0430f, z + 0.0100f, rx + 0.0075f, top - 0.0405f, z + 0.0106f);
            p.Box(SurfaceMaterial.ServerSilver, rx - 0.0060f, top - 0.0485f, z + 0.0100f, rx + 0.0060f, top - 0.0465f, z + 0.0106f);
            p.Box(SurfaceMaterial.ServerSilver, rx - 0.0050f, top - 0.0540f, z + 0.0100f, rx + 0.0050f, top - 0.0525f, z + 0.0106f);

            float zp = z + 0.0020f;
            float cp = right + 0.016f;
            float usbY = y + 0.0045f;
            float ethY = y + 0.0185f;
            holes = new List<(float, float, float, float)>();
            UsbAHoles(holes, cp, usbY);
            Rj45Holes(holes, cp, ethY);
            PlateWithHoles(p, SurfaceMaterial.RackBlack, right, y, panelRight, top, zb, zp, holes);
            UsbAInside(p, cp, usbY, zp, zb);
            Rj45Inside(p, cp, ethY, zp, zb);
            p.Frame(SurfaceMaterial.ServerSilver, cp - Rj45Width / 2f - 0.0016f, ethY - 0.0016f, cp + Rj45Width / 2f + 0.0016f, ethY + Rj45Height + 0.0016f, zp, zp + 0.0010f, 0.0009f);
            // Power button (ring, button and symbol) and the LEDs, each beside its symbol.
            p.Disc(SurfaceMaterial.ServerSilver, right + 0.016f, top - 0.0105f, zp, 0.0058f, 18);
            p.Disc(SurfaceMaterial.ServerBlack, right + 0.016f, top - 0.0105f, zp + 0.0004f, 0.0046f, 18);
            IconPower(p, right + 0.016f, top - 0.0105f, zp + 0.0008f, 0.0024f);
            float[] rows = { 0.0235f, 0.0335f, 0.0435f };
            for (int i = 0; i < 3; i++)
            {
                float ly = top - rows[i];
                if (i == 0) IconHealth(p, right + 0.0072f, ly, zp, 0.0026f);
                else if (i == 1) IconNic(p, right + 0.0072f, ly, zp, 0.0026f);
                else IconUid(p, right + 0.0072f, ly, zp, 0.0026f);
                LedBox(p, i == 2 ? SurfaceMaterial.SupportBlue : (i == 1 && index % 4 == 3 ? SurfaceMaterial.LedAmber : SurfaceMaterial.SafetyGreen), right + 0.0168f, ly, zp);
            }

            // The drive cage: a black void with three silver-outlined groups of eight separate trays.
            p.Box(SurfaceMaterial.ServerVoid, left, y + 0.003f, zb, right, top - 0.003f, zb + 0.0005f);
            const int groups = 3;
            const int baysPerGroup = 8;
            const float groupGap = 0.0045f;
            float groupWidth = (right - left - (groups - 1) * groupGap) / groups;
            float pitch = (groupWidth - 0.0048f) / baysPerGroup;
            const float trayGap = 0.0025f;
            float bayWidth = pitch - trayGap;
            for (int g = 0; g < groups; g++)
            {
                float gx = left + g * (groupWidth + groupGap);
                p.Frame(SurfaceMaterial.ServerSilver, gx, y + 0.002f, gx + groupWidth, top - 0.002f, zb, z + 0.0035f, 0.0016f);
                for (int b = 0; b < baysPerGroup; b++)
                {
                    float cx = gx + 0.0024f + (b + 0.5f) * pitch;
                    float xa = cx - bayWidth / 2f;
                    float xb = cx + bayWidth / 2f;
                    // The carrier stands 4 mm out of the void; its face is lighter than the lever and the void beside it.
                    p.Box(SurfaceMaterial.DriveBay, xa, y + 0.0045f, zb + 0.002f, xb, top - 0.0045f, z + 0.0030f);
                    p.Box(SurfaceMaterial.RackBlack, xa + 0.0007f, y + 0.0245f, z + 0.0030f, xb - 0.0007f, top - 0.0105f, z + 0.0062f);
                    for (int slot = 0; slot < 3; slot++)
                    {
                        float sy = y + 0.0320f + slot * 0.0105f;
                        p.Box(SurfaceMaterial.ServerVoid, cx - 0.0016f, sy, z + 0.0062f, cx + 0.0016f, sy + 0.0065f, z + 0.0065f);
                    }
                    p.Box(SurfaceMaterial.ServerSilver, xa + 0.0009f, top - 0.0100f, z + 0.0030f, xb - 0.0009f, top - 0.0052f, z + 0.0040f);
                    p.Box(SurfaceMaterial.ServerBlack, xa + 0.0018f, top - 0.0085f, z + 0.0040f, xb - 0.0018f, top - 0.0075f, z + 0.0042f);
                    p.Disc(SurfaceMaterial.ServerVoid, cx, y + 0.0185f, z + 0.0030f, 0.0050f, 14);
                    p.Disc(SurfaceMaterial.RackBlack, cx, y + 0.0185f, z + 0.0034f, 0.0040f, 14);
                    p.Box(SurfaceMaterial.ServerVoid, xa + 0.0005f, y + 0.0050f, z + 0.0030f, xb - 0.0005f, y + 0.0135f, z + 0.0042f);
                    p.Box(SurfaceMaterial.LatchRed, xa + 0.0018f, y + 0.0060f, z + 0.0042f, xb - 0.0018f, y + 0.0125f, z + 0.0048f);
                }
            }
        }
    }
}
