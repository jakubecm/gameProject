using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Mid-tower PC case after a product photo (Dell OptiPlex MT), 200 x 450 x 450 mm (W x H x D), matt black. The front has a
        /// diamond-perforated strip down its left and the whole lower part, a raised black upper panel with the round power button
        /// at its top left corner, the slim vertical optical drive, the headphone jack, two USB 2.0, a blue USB 3.0 and a USB-C
        /// port at its right, and under it a silver band with the round logo. It stands on the floor, centered, and the front
        /// faces +Z.
        /// </summary>
        [Model(Prop.PcTower)]
        private static void BuildPcTower(Transform root)
        {
            const float w = 0.200f;
            const float h = 0.450f;
            const float d = 0.450f;
            float front = d / 2f;
            var shell = new MeshBuilder();
            var vent = new MeshBuilder();
            var plate = new MeshBuilder();
            var drive = new MeshBuilder();
            var dark = new MeshBuilder();
            var grey = new MeshBuilder();
            var blue = new MeshBuilder();
            var chrome = new MeshBuilder();
            var band = new MeshBuilder();
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            // Front coordinates as the person sees them: f runs from the left edge (+X) to the right edge (-X), v is the
            // fraction of the height down from the top edge.
            float X(float f) => w / 2f - f * w;
            float Y(float v) => h * (1f - v);
            void Box(MeshBuilder b, float f0, float f1, float v0, float v1, float raise, float thickness) =>
                b.AddBox(new Vector3(X(f1), Y(v1), front + raise), new Vector3(X(f0), Y(v0), front + raise + thickness));
            void Vent(float f0, float f1, float v0, float v1)
            {
                float z = front + 0.0004f;
                Vector3 p00 = new Vector3(X(f1), Y(v1), z), p10 = new Vector3(X(f0), Y(v1), z);
                Vector3 p11 = new Vector3(X(f0), Y(v0), z), p01 = new Vector3(X(f1), Y(v0), z);
                Vector2 Uv(Vector3 p) => new Vector2(p.x, p.y) / PcVentTile;
                vent.AddPanel(p00, p10, p11, p01, Vector3.forward, Uv(p00), Uv(p10), Uv(p11), Uv(p01));
            }
            void Port(float f, float v, float portW, float portH, bool usbC, bool blueTongue)
            {
                float fw = portW / w / 2f;
                float vh = portH / h / 2f;
                Box(grey, f - fw, f + fw, v - vh, v + vh, 0.0042f, 0.0008f);
                Box(dark, f - fw * 0.82f, f + fw * 0.82f, v - vh * 0.82f, v + vh * 0.82f, 0.0050f, 0.0003f);
                if (!usbC) Box(blueTongue ? blue : grey, f - fw * 0.35f, f + fw * 0.35f, v - vh * 0.7f, v + vh * 0.7f, 0.0053f, 0.0003f);
            }

            shell.AddBox(new Vector3(-w / 2f, 0f, -d / 2f), new Vector3(w / 2f, h, front));

            // Perforated areas: the strip left of the upper panel, and everything under the silver band.
            const float panelF = 0.32f;
            const float bandTop = 0.451f;
            const float bandBottom = 0.545f;
            Vent(0.02f, panelF, 0.006f, bandTop);
            Vent(0.02f, 0.98f, bandBottom, 0.992f);

            // Raised upper panel and the silver band at its foot.
            Box(plate, panelF, 1f, 0f, bandTop, 0f, 0.004f);
            Box(band, panelF, 1f, bandTop, bandBottom, 0f, 0.004f);

            // Slim vertical optical drive down the left of the panel: a black front, a thin slot and the eject button.
            Box(drive, 0.37f, 0.44f, 0.057f, 0.440f, 0.004f, 0.0012f);
            Box(dark, 0.402f, 0.408f, 0.200f, 0.400f, 0.0052f, 0.0004f);
            Box(grey, 0.380f, 0.400f, 0.250f, 0.275f, 0.0052f, 0.0010f);

            // Power button: a round black knob with a bright rim, at the panel's top left corner.
            var power = new Vector3(X(0.39f), Y(0.030f), front + 0.004f);
            dark.AddLathe(new[] { P(0.0065f, 0f, true), P(0.0065f, 0.0026f, true), P(0f, 0.0026f, true) }, 28, true, power, toZ);
            chrome.AddLathe(new[] { P(0.0075f, 0f, true), P(0.0075f, 0.0029f, true), P(0.0065f, 0.0029f, true), P(0.0065f, 0f, true) }, 28, true, power, toZ);

            // Right column: headphone jack, two USB 2.0 above a USB 3.0 and a USB-C port, and the small status light.
            var jack = new Vector3(X(0.895f), Y(0.252f), front + 0.0042f);
            dark.AddLathe(new[] { P(0.0042f, 0f, true), P(0.0042f, 0.0006f, true), P(0f, 0.0006f, true) }, 20, true, jack, toZ);
            chrome.AddLathe(new[] { P(0.0054f, 0f, true), P(0.0054f, 0.0010f, true), P(0.0042f, 0.0010f, true), P(0.0042f, 0f, true) }, 20, true, jack, toZ);
            Port(0.858f, 0.292f, 0.0072f, 0.0125f, false, false);
            Port(0.895f, 0.292f, 0.0072f, 0.0125f, false, false);
            Port(0.858f, 0.353f, 0.0072f, 0.0125f, false, true);
            Port(0.895f, 0.353f, 0.0095f, 0.0042f, true, false);
            Box(grey, 0.890f, 0.900f, 0.096f, 0.100f, 0.0042f, 0.0006f);

            // The logo on the band: a dark ring with a short bar for the lettering.
            var logo = new Vector3(X(0.42f), Y((bandTop + bandBottom) / 2f), front + 0.0042f);
            var ring = new List<Vector3>();
            for (int i = 0; i < 32; i++)
            {
                float angle = 2f * Mathf.PI * i / 32f;
                ring.Add(new Vector3(Mathf.Cos(angle) * 0.0105f, Mathf.Sin(angle) * 0.0105f, 0f));
            }
            dark.AddTube(ring, 0.0008f, 8, logo, Quaternion.identity, true);
            dark.AddBox(logo + new Vector3(-0.0052f, -0.0014f, 0f), logo + new Vector3(0.0052f, 0.0014f, 0.0006f));

            // The small raised box low in the right corner.
            Box(plate, 0.73f, 0.94f, 0.945f, 0.972f, 0.0004f, 0.003f);

            shell.Attach("Case", root, SurfaceMaterial.MouseBlack);
            vent.Attach("FrontMesh", root, SurfaceMaterial.PcVent);
            plate.Attach("FrontPanel", root, SurfaceMaterial.ReaderBlack);
            band.Attach("SilverBand", root, SurfaceMaterial.Metal);
            drive.Attach("OpticalDrive", root, SurfaceMaterial.BlackHousing);
            dark.Attach("Ports", root, SurfaceMaterial.Screen);
            grey.Attach("PortShells", root, SurfaceMaterial.KeyboardKey);
            blue.Attach("Usb3", root, SurfaceMaterial.DataCable);
            chrome.Attach("Trim", root, SurfaceMaterial.Chrome);
            AddBoxCollider(root, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d));
        }
    }
}
