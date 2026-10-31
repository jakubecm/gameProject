using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Large office multifunction printer 0.60 x 1.27 x 0.65 m, after a product photo (an A3 colour copier on its four-drawer
        /// paper cabinet): a white body with the logo, four paper drawers with handle recesses and numbers on feet, the engine
        /// above them, an open black output tray at the left under the scanner, the scanner with a document feeder and its
        /// black paper tray on top, and a tilted black control panel with a screen and a green start button. The front faces +Z.
        /// </summary>
        [Model(Prop.Printer)]
        private static void BuildPrinter(Transform root)
        {
            const float width = 0.6f;
            const float depth = 0.65f;
            float front = depth / 2f;
            float half = width / 2f;
            var white = new MeshBuilder();
            var seam = new MeshBuilder();
            var black = new MeshBuilder();
            var grey = new MeshBuilder();
            var red = new MeshBuilder();

            // Feet and the four-drawer cabinet (0.03 - 0.64 m).
            foreach (var corner in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
            {
                grey.AddLathe(new[] { P(0f, 0f, true), P(0.022f, 0f, true), P(0.022f, 0.030f, true), P(0f, 0.030f, true) }, 20, true,
                    new Vector3(corner.x * (half - 0.04f), 0f, corner.y * (front - 0.04f)), Quaternion.identity);
            }
            white.AddRoundedBlock(new Vector3(0f, 0.030f, 0f), width, depth, 0.020f, 0.610f, 0.008f, 5);
            float[] drawerBottom = { 0.045f, 0.190f, 0.335f, 0.480f };
            for (int i = 0; i < 4; i++)
            {
                float y0 = drawerBottom[i];
                // Drawer front: a flat panel a hair proud, a dark handle pocket (cut-out look) at the left of centre, and the number.
                white.AddBox(new Vector3(-half + 0.012f, y0, front), new Vector3(half - 0.012f, y0 + 0.138f, front + 0.004f));
                grey.AddBox(new Vector3(-0.075f, y0 + 0.070f, front + 0.004f), new Vector3(0.075f, y0 + 0.118f, front + 0.0055f));
                black.AddBox(new Vector3(-0.068f, y0 + 0.088f, front + 0.0055f), new Vector3(0.068f, y0 + 0.112f, front + 0.0062f));
                grey.AddBox(new Vector3(-0.235f, y0 + 0.058f, front + 0.004f), new Vector3(-0.215f, y0 + 0.082f, front + 0.0046f));
                seam.AddBox(new Vector3(-half + 0.012f, y0 - 0.004f, front), new Vector3(half - 0.012f, y0, front + 0.002f));
            }

            // Engine (0.64 - 0.99 m): white block with the logo, the model strip and a red mark.
            white.AddRoundedBlock(new Vector3(0f, 0.640f, 0f), width, depth, 0.018f, 0.350f, 0.008f, 5);
            black.AddBox(new Vector3(-0.045f, 0.835f, front + 0.0005f), new Vector3(0.045f, 0.848f, front + 0.0025f));
            black.AddBox(new Vector3(-0.205f, 0.668f, front + 0.0005f), new Vector3(-0.115f, 0.674f, front + 0.0020f));
            red.AddBox(new Vector3(-0.212f, 0.650f, front + 0.0005f), new Vector3(-0.200f, 0.662f, front + 0.0025f));
            grey.AddBox(new Vector3(-0.195f, 0.652f, front + 0.0005f), new Vector3(-0.110f, 0.659f, front + 0.0015f));

            // Open output space above the left of the engine: a black tray that slopes down to the front, a black back wall, and a
            // white support column at the right that carries the scanner.
            const float trayTop = 0.995f;
            const float scannerBottom = 1.115f;
            black.AddBox(new Vector3(-0.130f, 0.990f, -front + 0.012f), new Vector3(half - 0.012f, trayTop, front - 0.012f));
            black.AddStrip(new List<Vector3> { new Vector3(0f, trayTop, front - 0.050f), new Vector3(0f, trayTop + 0.040f, front - 0.150f), new Vector3(0f, trayTop + 0.050f, -front + 0.120f) },
                Vector3.right, 0.390f, 0.012f, new Vector3(0.065f, 0f, 0f), Quaternion.identity);
            black.AddBox(new Vector3(-0.130f, trayTop, -front + 0.012f), new Vector3(half - 0.012f, scannerBottom, -front + 0.080f));
            white.AddRoundedBlock(new Vector3(-0.215f, 0.990f, 0f), 0.170f, depth, 0.012f, scannerBottom - 0.990f, 0.006f, 4);

            // Scanner (full width) and the document feeder on it with its black paper tray.
            white.AddRoundedBlock(new Vector3(0f, scannerBottom, 0f), width, depth - 0.02f, 0.016f, 0.060f, 0.007f, 5);
            white.AddRoundedBlock(new Vector3(0f, scannerBottom + 0.060f, -0.02f), width - 0.060f, depth - 0.110f, 0.020f, 0.058f, 0.008f, 5);
            black.AddStrip(new List<Vector3> { new Vector3(0f, scannerBottom + 0.112f, -0.060f), new Vector3(0f, scannerBottom + 0.134f, -0.200f), new Vector3(0f, scannerBottom + 0.150f, -0.330f) },
                Vector3.right, 0.420f, 0.008f, Vector3.zero, Quaternion.identity);
            black.AddBox(new Vector3(-0.150f, scannerBottom + 0.112f, 0.060f), new Vector3(0.150f, scannerBottom + 0.119f, 0.215f));

            // Control panel: a black slab on the user's right (-X) at the front of the scanner level, carried out on an arm and tilted up
            // towards the user (+Z): its front edge is the low one. Screen on the user's left of it, keys and the green start button right.
            var panel = ModelUtility.CreateGroup("ControlPanel", root);
            panel.localPosition = new Vector3(-0.12f, scannerBottom + 0.020f, front + 0.040f);
            panel.localRotation = Quaternion.Euler(26f, 0f, 0f);
            var panelBody = new MeshBuilder();
            var panelScreen = new MeshBuilder();
            var panelStart = new MeshBuilder();
            var panelKeys = new MeshBuilder();
            panelBody.AddRoundedBlock(Vector3.zero, 0.320f, 0.150f, 0.012f, 0.026f, 0.008f, 4);
            panelScreen.AddPanel(new Vector3(-0.060f, 0.0268f, 0.050f), new Vector3(0.140f, 0.0268f, 0.050f), new Vector3(0.140f, 0.0268f, -0.050f), new Vector3(-0.060f, 0.0268f, -0.050f),
                Vector3.up, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            panelStart.AddBox(new Vector3(-0.130f, 0.0262f, 0.025f), new Vector3(-0.100f, 0.0285f, 0.055f));
            panelKeys.AddBox(new Vector3(-0.145f, 0.0262f, -0.055f), new Vector3(-0.085f, 0.0268f, 0.012f), true, false);
            panelBody.Attach("Panel_Body", panel, SurfaceMaterial.BlackHousing);
            panelScreen.Attach("Panel_Screen", panel, SurfaceMaterial.MonitorImage);
            panelStart.Attach("Panel_Start", panel, SurfaceMaterial.SafetyGreen);
            panelKeys.Attach("Panel_Keys", panel, SurfaceMaterial.KeyboardKey);
            // The arm that carries the panel out from the scanner.
            black.AddBox(new Vector3(-0.240f, scannerBottom - 0.020f, front - 0.110f), new Vector3(0.000f, scannerBottom + 0.020f, front + 0.020f));

            white.Attach("Printer_Body", root, SurfaceMaterial.Trunking);
            seam.Attach("Printer_Seams", root, SurfaceMaterial.KeyboardKey);
            black.Attach("Printer_Black", root, SurfaceMaterial.ReaderBlack);
            grey.Attach("Printer_Grey", root, SurfaceMaterial.Metal);
            red.Attach("Printer_Mark", root, SurfaceMaterial.SafetyRed);
            AddBoxCollider(root, new Vector3(0f, 0.635f, 0.03f), new Vector3(width, 1.27f, depth + 0.06f));
        }
    }
}
