using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Rack UPS 2U after an Eaton 9E: black bezel, a perforated honeycomb panel (<c>T_ServerHoneycomb</c>, sunken) on each side of a raised control
        /// module with a blue LCD (<c>T_UpsDisplay</c>, "ONLINE MODE / OUT:230V 49.9HZ") and a row of five buttons (ESC, up, down, enter, power), the make
        /// plate on the right panel, and the ears with their rack screws.
        /// </summary>
        private static void BuildUps2U(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float top = y + h;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, top, z - 0.004f);
            // The bezel: one black plate with the two ears.
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, y, z - 0.004f, PanelHalf, top, z + 0.0030f);
            foreach (int sign in new[] { -1, 1 })
            {
                float sx = sign * (PanelHalf - 0.0055f);
                p.Disc(SurfaceMaterial.ServerSilver, sx, y + 0.0155f, z + 0.0031f, 0.0040f, 12);
                p.Disc(SurfaceMaterial.ServerSilver, sx, top - 0.0155f, z + 0.0031f, 0.0040f, 12);
                p.Box(SurfaceMaterial.DriveBay, sx - 0.0030f, y + 0.0152f, z + 0.0032f, sx + 0.0030f, y + 0.0158f, z + 0.0034f);
                p.Box(SurfaceMaterial.DriveBay, sx - 0.0030f, top - 0.0158f, z + 0.0032f, sx + 0.0030f, top - 0.0152f, z + 0.0034f);
            }

            // Perforated panels, sunken 3 mm into a frame.
            foreach (int sign in new[] { -1, 1 })
            {
                float x0 = sign < 0 ? -0.2200f : 0.0640f;
                float x1 = sign < 0 ? -0.0640f : 0.2200f;
                p.Frame(SurfaceMaterial.RackBlack, x0 - 0.003f, y + 0.0040f, x1 + 0.003f, top - 0.0040f, z + 0.0030f, z + 0.0042f, 0.003f);
                p.Box(SurfaceMaterial.DriveBay, x0, y + 0.0070f, z - 0.004f, x1, top - 0.0070f, z + 0.0005f);
                p.TexturedPanel(SurfaceMaterial.ServerHoneycomb, x0, y + 0.0070f, x1, top - 0.0070f, z + 0.0006f, HoneycombTile, HoneycombTile * 1.732f);
            }
            // The make plate on the right panel: a dark plate with a light wordmark bar.
            p.Box(SurfaceMaterial.ServerBlack, 0.1520f, y + 0.0290f, z + 0.0008f, 0.2080f, y + 0.0570f, z + 0.0016f);
            p.Box(SurfaceMaterial.ServerSilver, 0.1620f, y + 0.0390f, z + 0.0016f, 0.1980f, y + 0.0470f, z + 0.0019f);
            p.Box(SurfaceMaterial.ServerBlack, 0.1650f, y + 0.0410f, z + 0.0019f, 0.1950f, y + 0.0450f, z + 0.0021f);

            // A black gap (3 mm) round the control module so it reads as a separate part.
            p.Box(SurfaceMaterial.ServerVoid, -0.0640f, y + 0.0040f, z + 0.0004f, -0.0600f, top - 0.0040f, z + 0.0032f);
            p.Box(SurfaceMaterial.ServerVoid, 0.0600f, y + 0.0040f, z + 0.0004f, 0.0640f, top - 0.0040f, z + 0.0032f);

            // Control module, raised 2 mm: the LCD in a black bezel, five buttons below.
            p.Box(SurfaceMaterial.ServerBlack, -0.0600f, y + 0.0030f, z + 0.0030f, 0.0600f, top - 0.0030f, z + 0.0048f);
            p.Box(SurfaceMaterial.ServerBlack, -0.0345f, top - 0.0535f, z + 0.0048f, 0.0345f, top - 0.0115f, z + 0.0054f);
            p.TexturedPanel(SurfaceMaterial.UpsDisplay, -0.0295f, top - 0.0490f, 0.0295f, top - 0.0160f, z + 0.0055f, 0.0590f, 0.0330f);
            for (int i = 0; i < 5; i++)
            {
                float bx = -0.0300f + i * 0.0150f;
                p.Box(SurfaceMaterial.RackBlack, bx - 0.0058f, y + 0.0060f, z + 0.0048f, bx + 0.0058f, y + 0.0150f, z + 0.0068f);
                p.Box(SurfaceMaterial.ServerBlack, bx - 0.0050f, y + 0.0068f, z + 0.0068f, bx + 0.0050f, y + 0.0142f, z + 0.0070f);
                float cy = y + 0.0105f;
                switch (i)
                {
                    case 0: // ESC: three short bars
                        p.Box(SurfaceMaterial.ServerSilver, bx - 0.0030f, cy - 0.0008f, z + 0.0070f, bx + 0.0030f, cy + 0.0008f, z + 0.0072f);
                        break;
                    case 1: // up
                        p.Box(SurfaceMaterial.ServerSilver, bx - 0.0022f, cy - 0.0016f, z + 0.0070f, bx + 0.0022f, cy - 0.0004f, z + 0.0072f);
                        p.Box(SurfaceMaterial.ServerSilver, bx - 0.0012f, cy - 0.0004f, z + 0.0070f, bx + 0.0012f, cy + 0.0012f, z + 0.0072f);
                        break;
                    case 2: // down
                        p.Box(SurfaceMaterial.ServerSilver, bx - 0.0022f, cy + 0.0004f, z + 0.0070f, bx + 0.0022f, cy + 0.0016f, z + 0.0072f);
                        p.Box(SurfaceMaterial.ServerSilver, bx - 0.0012f, cy - 0.0012f, z + 0.0070f, bx + 0.0012f, cy + 0.0004f, z + 0.0072f);
                        break;
                    case 3: // enter
                        p.Box(SurfaceMaterial.ServerSilver, bx - 0.0026f, cy - 0.0006f, z + 0.0070f, bx + 0.0026f, cy + 0.0006f, z + 0.0072f);
                        p.Box(SurfaceMaterial.ServerSilver, bx + 0.0014f, cy, z + 0.0070f, bx + 0.0026f, cy + 0.0022f, z + 0.0072f);
                        break;
                    default: // power ring
                        p.Disc(SurfaceMaterial.ServerSilver, bx, cy, z + 0.0070f, 0.0030f, 14);
                        p.Disc(SurfaceMaterial.ServerBlack, bx, cy, z + 0.0072f, 0.0022f, 14);
                        break;
                }
            }
        }
    }
}
