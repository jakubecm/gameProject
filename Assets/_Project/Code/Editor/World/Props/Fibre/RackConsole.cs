using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Length of the pull-out console's pan (rear wall to the back of the faceplate): the length of its display, which folds down onto it, so the whole drawer slides into the rack.</summary>
        private const float ConsolePan = 0.28f;

        /// <summary>
        /// The 1U pull-out LCD console of <see cref="Prop.ConsoleRack"/>, built like the real rack-mount KVM drawers and shown pulled out with its display up (no mouse: a keyboard and a display only).
        /// In the rack: a black sheet-metal case (top cover and base, open at the sides) with a three-stage telescopic ball-bearing slide each side: the fixed outer C rail (front ear with screws,
        /// rear bracket to the post, lightening slots), the middle C rail pulled out half the travel (black spring lever and bumper), the inner C rail on the pan, ball cages with the real balls
        /// between the rails. On the slides a pan of galvanised sheet 0.28 m long, as long as the display, with the keyboard in the middle of its depth. The display is a slim 0.276 x 0.44 m panel
        /// on two flat hinges at the pan's rear: folded down it lies on the keyboard (picture facing down), so pan and display slide into the case together; up it stands 10 degrees past vertical, in
        /// front of the units above (the pan is pulled out far enough that it never reaches into them). The faceplate is plain black with rounded edges and rack ears, with only a recessed rounded
        /// pocket and a rounded grip of brushed aluminium in it (no LEDs, no labels). The drawer faces +Z; <paramref name="slot"/> is its unit.
        /// </summary>
        private static void AddConsoleDrawer(Transform root, RackParts p, int slot)
        {
            const float f = FibrePanelFront;
            const float caseBack = -0.255f;
            const float wall = 0.2025f;
            float y0 = FibreSlotY(slot) + 0.001f;
            float zRear = f + 0.090f;
            float zFront = zRear + ConsolePan;
            float zEnd = zFront + 0.006f;
            float travel = zRear - (f - ConsolePan);
            var black = SurfaceMaterial.RackBlack;
            var zinc = SurfaceMaterial.GalvanisedSteel;
            var steel = SurfaceMaterial.ServerSilver;
            var plastic = SurfaceMaterial.BlackHousing;

            // The case in the rack: a top cover and a base from the rear brackets to the front ears, and its back wall.
            p.Box(black, -0.220f, y0 + 0.0405f, caseBack, 0.220f, y0 + 0.0425f, f - 0.006f);
            p.Box(black, -0.220f, y0 + 0.0010f, caseBack, 0.220f, y0 + 0.0030f, f - 0.006f);
            p.Box(SurfaceMaterial.ServerVoid, -0.220f, y0 + 0.003f, caseBack, 0.220f, y0 + 0.0405f, caseBack + 0.003f);

            // The slides, one each side (s = -1 west, +1 east): three nested C rails with ball cages.
            float midBack = caseBack + 0.16f + travel / 2f;
            const float midLength = 0.34f;
            float innerBack = zRear + 0.015f;
            float innerFront = zFront - 0.020f;
            foreach (int s in new[] { -1, 1 })
            {
                void Bar(SurfaceMaterial m, float xa, float xb, float ya, float yb, float za, float zb) =>
                    p.Box(m, Mathf.Min(s * xa, s * xb), y0 + ya, za, Mathf.Max(s * xa, s * xb), y0 + yb, zb);
                void Balls(float x, float za, float zb, float r, params float[] rows)
                {
                    // A cage strip with its balls (spheres) in a row at every height of <rows>, 12 mm apart.
                    Bar(steel, x - 0.0004f, x + 0.0004f, rows[0] - 0.006f, rows[rows.Length - 1] + 0.006f, za, zb);
                    for (float z = za + 0.006f; z < zb - 0.004f; z += 0.012f)
                    {
                        foreach (float row in rows)
                        {
                            p[SurfaceMaterial.Chrome].AddLathe(new[] { P(0.0001f, -r, true), P(r * 0.87f, -r * 0.5f), P(r, 0f), P(r * 0.87f, r * 0.5f), P(0.0001f, r, true) }, 10, true,
                                new Vector3(s * x, y0 + row, z), Quaternion.identity);
                        }
                    }
                }

                // Outer rail (C section opening inwards): web, flanges, lips, lightening slots, front ear with two screws, rear bracket to the post.
                Bar(zinc, 0.2185f, 0.2200f, 0.004f, 0.040f, caseBack, f - 0.010f);
                Bar(zinc, 0.2155f, 0.2200f, 0.004f, 0.0055f, caseBack, f - 0.010f);
                Bar(zinc, 0.2155f, 0.2200f, 0.0385f, 0.040f, caseBack, f - 0.010f);
                Bar(zinc, 0.2155f, 0.2170f, 0.0055f, 0.0095f, caseBack, f - 0.010f);
                Bar(zinc, 0.2155f, 0.2170f, 0.0350f, 0.0385f, caseBack, f - 0.010f);
                for (float z = caseBack + 0.04f; z < f - 0.05f; z += 0.06f) Bar(SurfaceMaterial.ServerVoid, 0.2182f, 0.2186f, 0.016f, 0.028f, z, z + 0.035f);
                Bar(zinc, 0.2200f, 0.2263f, 0.004f, 0.040f, f - 0.016f, f - 0.010f);
                foreach (float sy in new[] { 0.012f, 0.032f })
                {
                    p.Disc(SurfaceMaterial.ServerVoid, s * 0.2232f, y0 + sy, f - 0.0098f, 0.0030f, 12);
                    p.Disc(steel, s * 0.2232f, y0 + sy, f - 0.0097f, 0.0023f, 12);
                }
                Bar(zinc, 0.2200f, 0.3700f, 0.004f, 0.040f, caseBack - 0.015f, caseBack);
                // Middle rail: C section opening inwards, black plastic bumpers at both ends and the spring lever at the front.
                Bar(steel, 0.2140f, 0.2155f, 0.007f, 0.037f, midBack, midBack + midLength);
                Bar(steel, 0.2105f, 0.2155f, 0.007f, 0.0085f, midBack, midBack + midLength);
                Bar(steel, 0.2105f, 0.2155f, 0.0355f, 0.037f, midBack, midBack + midLength);
                Bar(steel, 0.2105f, 0.2120f, 0.0085f, 0.0125f, midBack, midBack + midLength);
                Bar(steel, 0.2105f, 0.2120f, 0.0315f, 0.0355f, midBack, midBack + midLength);
                Bar(plastic, 0.2100f, 0.2160f, 0.007f, 0.037f, midBack - 0.008f, midBack);
                Bar(plastic, 0.2100f, 0.2160f, 0.007f, 0.037f, midBack + midLength, midBack + midLength + 0.006f);
                Bar(plastic, 0.2080f, 0.2105f, 0.0135f, 0.0305f, midBack + midLength - 0.040f, midBack + midLength - 0.004f);
                for (int k = 0; k < 4; k++) Bar(plastic, 0.2073f, 0.2080f, 0.0145f + k * 0.0035f, 0.0165f + k * 0.0035f, midBack + midLength - 0.036f, midBack + midLength - 0.008f);
                // Inner rail on the pan wall: C section opening outwards.
                Bar(steel, 0.2055f, 0.2070f, 0.009f, 0.033f, innerBack, innerFront);
                Bar(steel, 0.2055f, 0.2105f, 0.009f, 0.0105f, innerBack, innerFront);
                Bar(steel, 0.2055f, 0.2105f, 0.0315f, 0.033f, innerBack, innerFront);
                Bar(plastic, 0.2055f, 0.2105f, 0.009f, 0.0315f, innerBack - 0.006f, innerBack);
                // The ball cages and their balls: inner/middle (heights 0.016, 0.026) and middle/outer.
                Balls(0.2122f, Mathf.Max(innerBack, midBack + 0.01f), Mathf.Min(innerFront, midBack + midLength - 0.01f), 0.0018f, 0.0165f, 0.0255f);
                Balls(0.2170f, Mathf.Max(caseBack + 0.02f, midBack + 0.01f), Mathf.Min(f - 0.015f, midBack + midLength - 0.01f), 0.0013f, 0.017f, 0.027f);
                // Rivet heads down the face of the inner rail's flange.
                foreach (float z in new[] { 0.04f, 0.10f, 0.16f, 0.22f })
                {
                    p[steel].AddLathe(new[] { P(0.0021f, 0f, true), P(0.0021f, 0.0008f, true), P(0.0012f, 0.0016f), P(0.0001f, 0.0017f, true) }, 10, true,
                        new Vector3(s * 0.2105f, y0 + 0.0105f, zRear + z), Quaternion.identity);
                }
            }

            // The pan: galvanised floor and walls, a rear wall and a low front wall.
            p.Box(zinc, -0.2060f, y0 + 0.0040f, zRear, 0.2060f, y0 + 0.0065f, zFront);
            p.Box(zinc, wall, y0 + 0.0065f, zRear, 0.2060f, y0 + 0.034f, zFront);
            p.Box(zinc, -0.2060f, y0 + 0.0065f, zRear, -wall, y0 + 0.034f, zFront);
            p.Box(zinc, -wall, y0 + 0.0065f, zRear, wall, y0 + 0.034f, zRear + 0.003f);
            p.Box(zinc, -wall, y0 + 0.0065f, zFront - 0.003f, wall, y0 + 0.020f, zFront);

            // The flat hinges at the rear of the pan: a plate under the display's rear edge on the pan's rear bracket (here), a plate on the display (below), two screws each; no barrels.
            float hingeY = y0 + 0.0345f;
            float hingeZ = zRear + 0.004f;
            foreach (int s in new[] { -1, 1 })
            {
                p.Box(plastic, s * 0.170f - 0.020f, y0 + 0.0065f, zRear, s * 0.170f + 0.020f, hingeY - 0.0015f, zRear + 0.012f);
                p.Box(steel, s * 0.170f - 0.020f, hingeY - 0.0015f, zRear, s * 0.170f + 0.020f, hingeY, zRear + 0.022f);
            }
            p.Box(plastic, -0.020f, y0 + 0.0065f, zRear, 0.020f, y0 + 0.014f, zRear + 0.012f);

            // The display cable: from the hinge, round a loop, back into the case to the connector block on its rear wall.
            var cable = new List<Vector3>();
            AddBezier(cable, new Vector3(0.12f, hingeY - 0.004f, hingeZ + 0.012f), new Vector3(0.12f, y0 + 0.020f, zRear - 0.04f), new Vector3(0.12f, y0 + 0.012f, f - 0.20f),
                new Vector3(0.12f, y0 + 0.020f, caseBack + 0.020f), 14, true);
            p[plastic].AddTube(cable, 0.0045f, 8, Vector3.zero, Quaternion.identity);
            p.Box(steel, 0.10f, y0 + 0.012f, caseBack + 0.003f, 0.14f, y0 + 0.030f, caseBack + 0.014f);

            // The faceplate: black, 6 mm thick, a rounded pocket in the middle (the rack ears are its outer ends), the pocket's floor and the grip in it.
            float py0 = y0 + 0.0010f;
            float py1 = y0 + FibreUnit - 0.0010f - 0.001f;
            float pyc = (py0 + py1) / 2f;
            var faceplate = p[SurfaceMaterial.ServerBlack];
            AddDoorSurround(faceplate, -0.2413f, 0.2413f, py0, py1, -0.115f, 0.115f, pyc - 0.0125f, pyc + 0.0125f, zEnd - 0.006f, zEnd, 0.0125f);
            p.Box(SurfaceMaterial.ServerVoid, -0.115f, pyc - 0.0125f, zEnd - 0.0062f, 0.115f, pyc + 0.0125f, zEnd - 0.0058f);
            foreach (int s in new[] { -1, 1 })
            {
                foreach (float dy in new[] { 0.0105f, 0.0335f })
                {
                    p.Disc(SurfaceMaterial.ServerVoid, s * 0.2365f, y0 + dy, zEnd + 0.0001f, 0.0036f, 12);
                    p.Disc(steel, s * 0.2365f, y0 + dy, zEnd + 0.0002f, 0.0027f, 12);
                }
                // The post of the grip, from the pocket floor to the bar.
                p[steel].AddLathe(new[] { P(0.0035f, 0f, true), P(0.0035f, 0.0036f, true), P(0.0001f, 0.0036f, true) }, 12, true,
                    new Vector3(s * 0.088f, pyc, zEnd - 0.0058f), Quaternion.Euler(90f, 0f, 0f));
            }
            // The grip: a round bar, brushed aluminium, with rounded ends, set in the pocket so that it stands just below the faceplate.
            var grip = new List<Vector3> { new Vector3(-0.093f, pyc, zEnd - 0.0045f), new Vector3(0.093f, pyc, zEnd - 0.0045f) };
            p[SurfaceMaterial.Chrome].AddTube(grip, 0.0050f, 14, Vector3.zero, Quaternion.identity);
            foreach (int s in new[] { -1, 1 })
            {
                p[SurfaceMaterial.Chrome].AddLathe(new[] { P(0.0050f, 0f, true), P(0.0050f, 0.0014f, true), P(0.0034f, 0.0036f), P(0.0001f, 0.0050f, true) }, 14, true,
                    new Vector3(s * 0.093f, pyc, zEnd - 0.0045f), Quaternion.Euler(0f, 0f, -s * 90f));
            }
            p.Attach("Drawer", root);

            // The display: hinged at the rear of the pan, up and tilted 10 degrees back. Folded down it lies on the keyboard, the picture facing down.
            var lid = ModelUtility.CreateGroup("Display", root);
            lid.localPosition = new Vector3(0f, hingeY, hingeZ);
            lid.localRotation = Quaternion.Euler(-100f, 0f, 0f);
            float lidLength = ConsolePan - 0.004f;
            const float lidWidth = 0.440f;
            const float lidThick = 0.008f;
            var body = new MeshBuilder();
            body.AddRoundedBlock(new Vector3(0f, 0f, lidLength / 2f), lidWidth, lidLength, 0.008f, lidThick, 0.0025f, 4);
            // The picture on the underside (the face when the display is up): inset by thin bezels, thicker on the hinge side, which is the bottom of the screen.
            body.AddPanel(new Vector3(-lidWidth / 2f, -0.0002f, 0f), new Vector3(lidWidth / 2f, -0.0002f, 0f), new Vector3(lidWidth / 2f, -0.0002f, lidLength),
                new Vector3(-lidWidth / 2f, -0.0002f, lidLength), Vector3.down, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            body.Attach("Display_Body", lid, SurfaceMaterial.MouseBlack);
            var picture = new MeshBuilder();
            const float bx = 0.008f;
            picture.AddPanel(new Vector3(-lidWidth / 2f + bx, -0.0004f, 0.016f), new Vector3(lidWidth / 2f - bx, -0.0004f, 0.016f),
                new Vector3(lidWidth / 2f - bx, -0.0004f, lidLength - 0.008f), new Vector3(-lidWidth / 2f + bx, -0.0004f, lidLength - 0.008f), Vector3.down,
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            picture.Attach("Display_Picture", lid, SurfaceMaterial.MonitorImage);
            var hingeLeaves = new MeshBuilder();
            foreach (int s in new[] { -1, 1 })
            {
                // The display's hinge plate, flat on its back (the top when folded), with its two screws.
                hingeLeaves.AddBox(new Vector3(s * 0.170f - 0.020f, lidThick, 0f), new Vector3(s * 0.170f + 0.020f, lidThick + 0.0015f, 0.022f));
                foreach (float dx in new[] { -0.011f, 0.011f })
                {
                    hingeLeaves.AddLathe(new[] { P(0.0023f, 0f, true), P(0.0023f, 0.0010f, true), P(0.0001f, 0.0010f, true) }, 10, true, new Vector3(s * 0.170f + dx, lidThick + 0.0015f, 0.011f), Quaternion.identity);
                }
            }
            hingeLeaves.Attach("Display_Hinges", lid, steel);

            // The keyboard in the middle of the pan's depth (the number pad on the user's right, -X).
            BuildKeyboard(root, new Vector3(0f, y0 + 0.0065f, zRear + ConsolePan / 2f));

            AddBoxCollider(root, new Vector3(0f, y0 + 0.02f, (zRear + zEnd) / 2f), new Vector3(0.48f, 0.05f, zEnd - zRear));
            AddBoxCollider(root, new Vector3(0f, y0 + 0.0345f + lidLength / 2f, zRear + 0.004f), new Vector3(0.44f, lidLength, 0.03f));
        }
    }
}
