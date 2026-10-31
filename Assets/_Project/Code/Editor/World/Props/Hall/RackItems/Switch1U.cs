using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Switch 1U after a 48-port PoE managed switch (TP-Link Omada type). Dark grey faceplate with black ears. Every port is a real hole through the
        /// faceplate with its own inside: on the left a USB-A port (blue tongue), the RJ45 console port and a micro-USB console port beside the mode button,
        /// then two rows of 24 RJ45 ports (latch notch, gold contacts) in groups of eight in a light bezel with a gap between groups, and on the right four
        /// SFP+ cages (2 x 2). The link LED of every port sits right at the port (the top row's above it, the bottom row's below it); the system LEDs
        /// (power, system, PoE) are at the left with their symbols.
        /// </summary>
        private static void BuildSwitch1U(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float zb = z - PortDepth;
            float top = y + h;
            float zFront = z + 0.0020f;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, top, zb);
            foreach (int sign in new[] { -1, 1 })
            {
                float e0 = sign < 0 ? -PanelHalf : PanelHalf - 0.010f;
                p.Box(SurfaceMaterial.RackBlack, e0, y, zb, e0 + 0.010f, top, z + 0.0020f);
                float sx = sign * (PanelHalf - 0.0050f);
                p.Disc(SurfaceMaterial.ServerSilver, sx, y + h * 0.5f, z + 0.0021f, 0.0030f, 10);
                p.Disc(SurfaceMaterial.ServerVoid, sx, y + h * 0.5f, z + 0.0025f, 0.0014f, 8);
            }

            float rowLow = y + 0.0076f;
            float rowHigh = y + 0.0218f;
            const float tileHeight = 0.0128f;
            const float pitch = 0.0136f;
            const float groupGap = 0.0030f;
            float groupWidth = 8 * pitch;
            float px0 = -0.1660f;
            float bezelRight = px0 + 3 * groupWidth + 2 * groupGap;

            // Hole lists: the RJ45 ports of both rows, the SFP+ cages, the left-hand connectors.
            var holes = new List<(float xa, float ya, float xb, float yb)>();
            var ports = new List<(float cx, float yLow)>();
            for (int row = 0; row < 2; row++)
            {
                float yLow = (row == 0 ? rowHigh : rowLow) + 0.0005f;
                for (int port = 0; port < 24; port++)
                {
                    float cx = px0 + (port / 8) * (groupWidth + groupGap) + (port % 8) * pitch + pitch / 2f;
                    Rj45Holes(holes, cx, yLow);
                    ports.Add((cx, yLow));
                }
            }
            float sfpX0 = bezelRight + 0.0120f;
            var sfps = new List<(float cx, float yLow)>();
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    float cx = sfpX0 + 0.0150f + col * 0.0300f;
                    float yLow = (row == 0 ? rowHigh : rowLow) + 0.0019f;
                    SfpHoles(holes, cx, yLow);
                    sfps.Add((cx, yLow));
                }
            }
            float usbX = -PanelHalf + 0.0200f;
            float usbY = y + 0.0170f;
            float conX = -PanelHalf + 0.0380f;
            float conY = y + 0.0165f;
            float microX = -PanelHalf + 0.0525f;
            float microY = y + 0.0190f;
            UsbAHoles(holes, usbX, usbY);
            Rj45Holes(holes, conX, conY);
            holes.Add((microX - 0.0036f, microY, microX + 0.0036f, microY + 0.0026f));

            // The dark faceplate (deep, so every hole is deep) and the light bezel round the ports, with the same holes.
            PlateWithHoles(p, SurfaceMaterial.SwitchFace, -PanelHalf + 0.010f, y, PanelHalf - 0.010f, top, zb, z, holes);
            var bezelHoles = new List<(float xa, float ya, float xb, float yb)>(holes);
            for (int group = 0; group < 3; group++)
            {
                float gx = px0 + group * (groupWidth + groupGap);
                var groupHoles = bezelHoles.FindAll(hole => hole.xa >= gx - 1e-4f && hole.xb <= gx + groupWidth + 1e-4f && hole.ya >= rowLow - 1e-4f);
                PlateWithHoles(p, SurfaceMaterial.ServerSilver, gx, rowLow, gx + groupWidth, rowHigh + tileHeight, z, zFront, groupHoles);
            }
            var sfpHoles = holes.FindAll(hole => hole.xa >= sfpX0);
            PlateWithHoles(p, SurfaceMaterial.ServerSilver, sfpX0, rowLow, sfpX0 + 0.0600f, rowHigh + tileHeight, z, zFront, sfpHoles);

            foreach (var (cx, yLow) in ports) Rj45Inside(p, cx, yLow, zFront, zb);
            foreach (var (cx, yLow) in sfps) SfpInside(p, cx, yLow, zFront, zb);
            UsbAInside(p, usbX, usbY, z, zb);
            Rj45Inside(p, conX, conY, z, zb);
            p.Cavity(SurfaceMaterial.ServerVoid, microX - 0.0036f, microY, microX + 0.0036f, microY + 0.0026f, z, zb);
            p.Box(SurfaceMaterial.ServerSilver, microX - 0.0030f, microY + 0.0008f, zb, microX + 0.0030f, microY + 0.0014f, zb + 0.0050f);
            // Mode button beside the micro-USB port.
            p.Disc(SurfaceMaterial.ServerSilver, microX + 0.0105f, y + 0.0200f, z + 0.0004f, 0.0030f, 12);
            p.Disc(SurfaceMaterial.ServerBlack, microX + 0.0105f, y + 0.0200f, z + 0.0008f, 0.0021f, 12);

            // Link LEDs at the ports: the top row's above it, the bottom row's below it (a few amber for PoE activity).
            for (int row = 0; row < 2; row++)
            {
                float ly = row == 0 ? rowHigh + tileHeight + 0.0024f : rowLow - 0.0024f;
                for (int port = 0; port < 24; port++)
                {
                    float cx = ports[row * 24 + port].cx;
                    var led = (port * 7 + row * 3 + index) % 5 == 0 ? SurfaceMaterial.LedAmber : SurfaceMaterial.SafetyGreen;
                    LedBox(p, led, cx, ly, z, 0.0042f, 0.0026f);
                }
            }
            // The SFP+ LEDs the same way.
            for (int i = 0; i < 4; i++)
            {
                float ly = i < 2 ? rowHigh + tileHeight + 0.0024f : rowLow - 0.0024f;
                LedBox(p, SurfaceMaterial.SafetyGreen, sfps[i].cx, ly, z, 0.0042f, 0.0026f);
            }
            // System LEDs with their symbols: power, system and PoE (a bolt), above the left connectors.
            float sy = y + 0.0338f;
            IconPower(p, -PanelHalf + 0.0160f, sy, z, 0.0021f);
            LedBox(p, SurfaceMaterial.SafetyGreen, -PanelHalf + 0.0210f, sy, z, 0.0036f, 0.0026f);
            IconHealth(p, -PanelHalf + 0.0310f, sy, z, 0.0021f);
            LedBox(p, SurfaceMaterial.SafetyGreen, -PanelHalf + 0.0360f, sy, z, 0.0036f, 0.0026f);
            p.Box(SurfaceMaterial.ServerSilver, -PanelHalf + 0.0452f, sy, z, -PanelHalf + 0.0476f, sy + 0.0022f, z + 0.0004f);
            p.Box(SurfaceMaterial.ServerSilver, -PanelHalf + 0.0446f, sy - 0.0022f, z, -PanelHalf + 0.0470f, sy, z + 0.0004f);
            LedBox(p, SurfaceMaterial.LedAmber, -PanelHalf + 0.0510f, sy, z, 0.0036f, 0.0026f);
        }
    }
}
