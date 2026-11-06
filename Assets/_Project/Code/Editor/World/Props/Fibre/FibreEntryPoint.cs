using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Fibre entry point: the entrance cabinet, 0.8 x 0.6 x 2.2 m like the racks beside it, light grey, standing directly on the floor, with one glazed door. The door (no hinges, lever lock centred on the free width of the right stile, a sticker centred above the window) is a grey leaf with rounded corners in a black frame whose
        /// opening has rounded corners too, 3 mm clear all round and the leaf 6 mm proud; it has a window and a louvred pocket below it. The room's trunk duct stands on the roof
        /// (<see cref="FibreDuct"/>); the armoured trunk cables come down out of it through the roof and behind the equipment. What it holds is the entrance's own 48 V DC plant (see
        /// <c>AddDcPlant</c>), seen through the window. Pivot on the floor at the wall surface in the middle of the cabinet, +Z away from the wall.
        /// </summary>
        [Model(Prop.FibreEntryPoint)]
        private static void BuildFibreEntryPoint(Transform root)
        {
            const float half = 0.4f;
            const float depth = 0.6f;
            const float top = 2.2f;
            const float skin = 0.015f;
            const float inner = half - skin;
            const float trunkX = 0.07f;
            const float trunkZ = 0.30f;
            var p = new RackParts();
            var grey = p[SurfaceMaterial.CabinetGrey];
            var dark = p[SurfaceMaterial.BlackHousing];

            // The shell: two sides, the floor plate and the roof with an opening under the pipe (x +-0.1, z 0.2-0.4).
            p.Box(SurfaceMaterial.CabinetGrey, -half, 0f, 0f, -inner, top, depth);
            p.Box(SurfaceMaterial.CabinetGrey, inner, 0f, 0f, half, top, depth);
            p.Box(SurfaceMaterial.CabinetGrey, -inner, 0f, 0f, inner, skin, depth);
            p.Box(SurfaceMaterial.CabinetGrey, -inner, top - skin, 0f, inner, top, 0.20f);
            p.Box(SurfaceMaterial.CabinetGrey, -inner, top - skin, 0.40f, inner, top, depth);
            p.Box(SurfaceMaterial.CabinetGrey, -inner, top - skin, 0.20f, -0.10f, top, 0.40f);
            p.Box(SurfaceMaterial.CabinetGrey, 0.10f, top - skin, 0.20f, inner, top, 0.40f);

            // The door: a grey leaf (rounded corners, window, louvred pocket) in a black frame whose opening has the same rounded corners, 3 mm clear all round; the leaf stands 6 mm proud.
            const float windowX = 0.27f;
            const float leafX = 0.365f;
            const float leafY0 = 0.04f;
            const float seam = 0.003f;
            float leafY1 = top - 0.04f;
            AddDoorSurround(dark, -inner, inner, skin, top - skin, -leafX - seam, leafX + seam, leafY0 - seam, leafY1 + seam, depth - 0.03f, depth - DoorProud, DoorCorner + seam);
            AddDoorStiles(grey, -leafX, leafX, leafY0, leafY1, -windowX, windowX, depth - 0.03f, depth);
            p.Box(SurfaceMaterial.CabinetGrey, -windowX, leafY0, depth - 0.03f, windowX, 0.16f, depth);
            p.Box(SurfaceMaterial.CabinetGrey, -windowX, 0.58f, depth - 0.03f, windowX, 0.72f, depth);
            p.Box(SurfaceMaterial.CabinetGrey, -windowX, 1.90f, depth - 0.03f, windowX, leafY1, depth);
            AddVent(grey, dark, -windowX, windowX, 0.16f, 0.58f, depth - 0.03f, depth - 0.024f, depth, 0.034f);
            p.Box(SurfaceMaterial.ClearGlass, -windowX, 0.72f, depth - 0.0125f, windowX, 1.90f, depth - 0.0105f);
            // Lever lock on the right (the viewer's right is -X), centred on the free width of the stile between the window and the edge of the leaf, and on its height.
            AddLeverLock(dark, p[SurfaceMaterial.Chrome], -(leafX + windowX) / 2f, (leafY0 + leafY1) / 2f, 0.7f, depth);

            // Inside: a back plate and, behind the window, the DC plant, moved forward to the glass.
            p.Box(SurfaceMaterial.ServerBlack, -inner, skin, 0f, inner, top - skin, 0.012f);
            var plant = new RackParts();
            AddDcPlant(plant);
            plant.AppendTo(p, new Vector3(0f, 0f, 0.16f));

            // The armoured trunk cables come down out of the pipe through the roof, bend back behind the equipment and go down the back plate into the floor of the cabinet.
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * trunkX;
                var trunk = new List<Vector3> { new Vector3(x, FibreTrayBottom + 0.02f, trunkZ), new Vector3(x, top + 0.02f, trunkZ), new Vector3(x, 2.10f, trunkZ) };
                AddBezier(trunk, new Vector3(x, 2.10f, trunkZ), new Vector3(x, 2.00f, trunkZ), new Vector3(x, 2.00f, 0.03f), new Vector3(x, 1.90f, 0.03f), 10, false);
                trunk.Add(new Vector3(x, 0.14f, 0.03f));
                p[SurfaceMaterial.FibreCord].AddTube(trunk, 0.016f, 12, Vector3.zero, Quaternion.identity);
            }

            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(0f, 2.03f, depth), Vector3.left, Vector3.up, 0.16f, 0.04f, MeetLabel.EntryPoint);

            p.Attach("Cabinet", root);
            labels.Attach("Labels", root, SurfaceMaterial.MeetLabels);
            AddBoxCollider(root, new Vector3(0f, top / 2f, depth / 2f), new Vector3(2f * half, top, depth));
        }

        /// <summary>
        /// The DC plant behind the entrance cabinet's window (cabinet-local, front at z 0.30), every part with its real details and its own sticker (<c>T_MeetLabels</c>): an earthing bar (copper
        /// on two white insulators, eight rounded lugs with bolts, green and yellow earth cables with ferrules) that bonds the cable armour; a 5U rectifier subrack with four plug-in modules
        /// (rounded silver faces, vent slots, pull handle, three LEDs, rating sticker, two captive screws) and a controller (LCD like the UPS's, six domed buttons with symbols, two ports,
        /// sticker); a slotted wiring duct; a fuse panel of seven fuse-switch disconnectors (rounded bases, grip-ribbed handles, fuse windows with the ceramic fuse, terminal blocks with
        /// cross-slot screws); and two shelves of two front-terminal 12 V battery blocks (rounded cases with stiffening ribs, a lid with a vent strip and six vent caps, label,
        /// terminal bosses with studs, washers and nuts, polarity marks, copper links); no LEDs on the batteries and no loose cables.
        /// </summary>
        private static void AddDcPlant(RackParts p)
        {
            const float x0 = -0.241f;
            const float x1 = 0.241f;
            const float front = 0.30f;
            var steel = SurfaceMaterial.ServerSilver;
            var voidM = SurfaceMaterial.ServerVoid;
            var white = SurfaceMaterial.DetectorWhite;
            var rot = Quaternion.Euler(90f, 0f, 0f);

            // A hex bolt head or nut standing out of a face towards +Z.
            void Hex(float x, float y, float z, float r, float h, SurfaceMaterial m) =>
                p[m].AddLathe(new[] { P(r, 0f, true), P(r, h, true), P(0.0001f, h, true) }, 6, false, new Vector3(x, y, z), rot);
            // A round screw head with a cross slot, facing +Z.
            void Screw(float x, float y, float z, float r)
            {
                p.Disc(voidM, x, y, z, r + 0.0006f, 12);
                p.Disc(steel, x, y, z + 0.0001f, r, 12);
                p.Box(voidM, x - r * 0.8f, y - r * 0.12f, z + 0.0002f, x + r * 0.8f, y + r * 0.12f, z + 0.0004f);
                p.Box(voidM, x - r * 0.12f, y - r * 0.8f, z + 0.0002f, x + r * 0.12f, y + r * 0.8f, z + 0.0004f);
            }
            // A sticker of the plant (4:1) centred at (cx, cy), facing +Z.
            void Sticker(MeetLabel label, float cx, float cy, float width, float z) =>
                AddLabel(p[SurfaceMaterial.MeetLabels], new Vector3(cx, cy, z), Vector3.left, Vector3.up, width, width / 4f, label);

            // Earthing bar on two insulators, eight rounded lugs with bolts, its own sticker, earth cables with ferrules running down the left wall.
            p.Box(SurfaceMaterial.Brass, -0.20f, 1.79f, 0.034f, 0.20f, 1.82f, 0.044f);
            foreach (float x in new[] { -0.17f, 0.17f })
            {
                p[white].AddLathe(new[] { P(0.010f, 0f, true), P(0.010f, 0.016f, true), P(0.0001f, 0.016f, true) }, 16, true, new Vector3(x, 1.8025f, 0.018f), rot);
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -0.175f + i * 0.05f;
                p[SurfaceMaterial.Brass].AddRoundedSlab(new Vector3(x, 1.766f, 0.044f), 0.016f, 0.048f, 0.005f, 0.006f, 0.0015f, 3);
                p.Disc(voidM, x, 1.752f, 0.0506f, 0.003f, 10);
                Hex(x, 1.775f, 0.050f, 0.0045f, 0.0035f, steel);
                if (i < 3)
                {
                    var wire = new[] { new Vector3(x, 1.742f, 0.046f), new Vector3(x, 1.72f, 0.07f), new Vector3(x - 0.01f, 1.70f, 0.09f) };
                    p[i == 0 ? SurfaceMaterial.SafetyGreen : SurfaceMaterial.SafetyYellow].AddTube(wire, 0.004f, 8, Vector3.zero, Quaternion.identity);
                    p[steel].AddLathe(new[] { P(0.0048f, 0f, true), P(0.0048f, 0.012f, true), P(0.0001f, 0.012f, true) }, 8, true, new Vector3(x, 1.738f, 0.047f), Quaternion.Euler(-12f, 0f, 0f));
                }
            }
            Sticker(MeetLabel.EarthBar, 0f, 1.830f, 0.072f, 0.0445f);

            // The rectifier subrack: a galvanised tray with cheeks, and five slots (four modules and the controller).
            const float shelfY0 = 1.50f;
            const float shelfY1 = 1.72f;
            p.Box(SurfaceMaterial.ServerBlack, x0, shelfY0, 0.06f, x1, shelfY1, front - 0.004f);
            p.Box(SurfaceMaterial.GalvanisedSteel, x0, shelfY0 - 0.004f, 0.06f, x1, shelfY0, front);
            p.Box(SurfaceMaterial.GalvanisedSteel, x0, shelfY1, 0.06f, x1, shelfY1 + 0.004f, front);
            p.Box(SurfaceMaterial.RackBlack, x0, shelfY0, front - 0.004f, x1, shelfY1, front);
            const float slotWidth = (x1 - x0 - 0.036f) / 5f;
            for (int i = 0; i < 5; i++)
            {
                float mx0 = x0 + 0.018f + i * slotWidth + 0.002f;
                float mx1 = mx0 + slotWidth - 0.004f;
                float mc = (mx0 + mx1) / 2f;
                float my = (shelfY0 + shelfY1) / 2f;
                float zf = front + 0.012f;
                if (i < 4)
                {
                    p[steel].AddRoundedSlab(new Vector3(mc, my, front), mx1 - mx0, shelfY1 - shelfY0 - 0.016f, 0.004f, 0.012f, 0.002f, 3);
                    for (int k = 0; k < 8; k++) p.Box(voidM, mx0 + 0.008f, shelfY0 + 0.058f + k * 0.0072f, zf + 0.0001f, mx1 - 0.008f, shelfY0 + 0.0625f + k * 0.0072f, zf + 0.0006f);
                    // Pull handle: a chrome bar on two posts, in a dark recess.
                    p.Box(voidM, mx0 + 0.008f, shelfY0 + 0.012f, zf + 0.0001f, mx1 - 0.008f, shelfY0 + 0.034f, zf + 0.0004f);
                    p[SurfaceMaterial.Chrome].AddTube(new[] { new Vector3(mx0 + 0.014f, shelfY0 + 0.023f, front + 0.026f), new Vector3(mx1 - 0.014f, shelfY0 + 0.023f, front + 0.026f) }, 0.0045f, 12, Vector3.zero, Quaternion.identity);
                    foreach (float hx in new[] { mx0 + 0.014f, mx1 - 0.014f })
                    {
                        p[SurfaceMaterial.Chrome].AddLathe(new[] { P(0.003f, 0f, true), P(0.003f, 0.014f, true), P(0.0001f, 0.014f, true) }, 10, true, new Vector3(hx, shelfY0 + 0.023f, zf), rot);
                    }
                    // Three LEDs in small bezels, the rating sticker and two captive screws.
                    for (int k = 0; k < 3; k++)
                    {
                        float lx = mc - 0.014f + k * 0.014f;
                        p.Box(voidM, lx - 0.0045f, shelfY1 - 0.040f, zf, lx + 0.0045f, shelfY1 - 0.029f, zf + 0.0006f);
                        p.Box(k == 1 && i == 2 ? SurfaceMaterial.LedAmber : SurfaceMaterial.SafetyGreen, lx - 0.003f, shelfY1 - 0.0385f, zf + 0.0006f, lx + 0.003f, shelfY1 - 0.0305f, zf + 0.0016f);
                    }
                    Sticker(MeetLabel.RectModule, mc, shelfY1 - 0.060f, mx1 - mx0 - 0.016f, zf);
                    foreach (float sy in new[] { shelfY0 + 0.006f, shelfY1 - 0.022f }) Screw(mc, sy, zf + 0.0002f, 0.0034f);
                }
                else
                {
                    p[SurfaceMaterial.ServerBlack].AddRoundedSlab(new Vector3(mc, my, front), mx1 - mx0, shelfY1 - shelfY0 - 0.016f, 0.004f, 0.012f, 0.002f, 3);
                    // LCD in a black bezel, as the UPS's.
                    p.Box(voidM, mc - 0.038f, 1.668f, zf, mc + 0.038f, 1.704f, zf + 0.0012f);
                    AddDisplayPicture(p[SurfaceMaterial.DeviceDisplays], mc - 0.033f, 1.6725f, mc + 0.033f, 1.6995f, zf + 0.0013f, DeviceDisplay.Dc);
                    // Six domed buttons: a four-way pad round OK, and ESC and MENU below, each with its own symbol.
                    const float cy = 1.625f;
                    void Button(float bx, float by, float r, int symbol)
                    {
                        p[SurfaceMaterial.RackBlack].AddLathe(new[] { P(r + 0.0025f, 0f, true), P(r + 0.0025f, 0.0018f, true), P(r + 0.0005f, 0.0022f, true), P(0.0001f, 0.0022f, true) }, 18, true, new Vector3(bx, by, zf), rot);
                        p[SurfaceMaterial.KeyboardKey].AddLathe(new[] { P(r, 0.0018f, true), P(r, 0.0036f, true), P(r * 0.78f, 0.0050f), P(0.0001f, 0.0054f, true) }, 18, true, new Vector3(bx, by, zf), rot);
                        float zs = zf + 0.0053f;
                        switch (symbol)
                        {
                            case 0: p.Box(steel, bx - 0.0035f, by - 0.0018f, zs, bx + 0.0035f, by - 0.0006f, zs + 0.0003f); p.Box(steel, bx - 0.0015f, by - 0.0006f, zs, bx + 0.0015f, by + 0.0024f, zs + 0.0003f); break; // up
                            case 1: p.Box(steel, bx - 0.0035f, by + 0.0006f, zs, bx + 0.0035f, by + 0.0018f, zs + 0.0003f); p.Box(steel, bx - 0.0015f, by - 0.0024f, zs, bx + 0.0015f, by + 0.0006f, zs + 0.0003f); break; // down
                            case 2: p.Box(steel, bx - 0.0024f, by - 0.0015f, zs, bx + 0.0006f, by + 0.0015f, zs + 0.0003f); p.Box(steel, bx + 0.0006f, by - 0.0035f, zs, bx + 0.0018f, by + 0.0035f, zs + 0.0003f); break; // left
                            case 3: p.Box(steel, bx - 0.0006f, by - 0.0015f, zs, bx + 0.0024f, by + 0.0015f, zs + 0.0003f); p.Box(steel, bx - 0.0018f, by - 0.0035f, zs, bx - 0.0006f, by + 0.0035f, zs + 0.0003f); break; // right
                            case 4: p.Disc(steel, bx, by, zs, 0.0030f, 14); p.Disc(SurfaceMaterial.KeyboardKey, bx, by, zs + 0.0001f, 0.0021f, 14); break; // OK ring
                            case 5: p.Box(steel, bx - 0.0035f, by - 0.0008f, zs, bx + 0.0035f, by + 0.0008f, zs + 0.0003f); p.Box(steel, bx - 0.0035f, by + 0.0024f, zs, bx + 0.0035f, by + 0.0040f, zs + 0.0003f); p.Box(steel, bx - 0.0035f, by - 0.0040f, zs, bx + 0.0035f, by - 0.0024f, zs + 0.0003f); break; // menu
                            default: p.Box(steel, bx - 0.0035f, by - 0.0035f, zs, bx - 0.0020f, by + 0.0035f, zs + 0.0003f); p.Box(steel, bx - 0.0020f, by + 0.0020f, zs, bx + 0.0035f, by + 0.0035f, zs + 0.0003f); p.Box(steel, bx - 0.0020f, by - 0.0035f, zs, bx + 0.0035f, by - 0.0020f, zs + 0.0003f); break; // ESC
                        }
                    }
                    Button(mc, cy + 0.0175f, 0.0068f, 0);
                    Button(mc, cy - 0.0175f, 0.0068f, 1);
                    Button(mc - 0.0175f, cy, 0.0068f, 2);
                    Button(mc + 0.0175f, cy, 0.0068f, 3);
                    Button(mc, cy, 0.0072f, 4);
                    Button(mc - 0.027f, 1.590f, 0.0062f, 6);
                    Button(mc + 0.027f, 1.590f, 0.0062f, 5);
                    // A USB-A port and a network port with their dark openings, and the sticker.
                    p.Box(steel, mc - 0.030f, 1.560f, zf, mc - 0.012f, 1.570f, zf + 0.0020f);
                    p.Box(voidM, mc - 0.028f, 1.5625f, zf + 0.0020f, mc - 0.014f, 1.5675f, zf + 0.0023f);
                    p.Box(steel, mc + 0.000f, 1.558f, zf, mc + 0.030f, 1.572f, zf + 0.0020f);
                    p.Box(voidM, mc + 0.002f, 1.5605f, zf + 0.0020f, mc + 0.028f, 1.5695f, zf + 0.0023f);
                    Sticker(MeetLabel.Controller, mc, 1.536f, mx1 - mx0 - 0.014f, zf);
                }
            }
            foreach (int s in new[] { -1, 1 })
            {
                foreach (float y in new[] { shelfY0 + 0.012f, shelfY1 - 0.012f }) Screw(s < 0 ? x0 + 0.009f : x1 - 0.009f, y, front + 0.0002f, 0.0028f);
            }

            // Slotted wiring duct between the subrack and the fuse panel, and below it.
            foreach (float duct in new[] { 1.470f, 1.352f })
            {
                p.Box(SurfaceMaterial.CabinetGrey, x0, duct, 0.18f, x1, duct + 0.022f, front + 0.030f);
                for (int k = 0; k < 24; k++)
                {
                    float sx = x0 + 0.010f + k * 0.0195f;
                    p.Box(voidM, sx, duct + 0.004f, front + 0.0301f, sx + 0.012f, duct + 0.018f, front + 0.0304f);
                }
            }

            // The fuse panel: seven fuse-switch disconnectors on a DIN rail. Each: a rounded base, a grip-ribbed handle over the fuse (its ceramic body seen through a window), a terminal
            // block with two cross-slot screws at the top and the bottom, and a rating sticker.
            const float bankY0 = 1.382f;
            const float bankY1 = 1.466f;
            p.Box(SurfaceMaterial.ServerBlack, x0, bankY0, 0.06f, x1, bankY1, front - 0.004f);
            p.Box(SurfaceMaterial.GalvanisedSteel, x0 + 0.004f, bankY0 + 0.036f, front - 0.004f, x1 - 0.004f, bankY0 + 0.048f, front - 0.001f);
            const float fuseWidth = 0.056f;
            const float fusePitch = (x1 - x0 - 0.020f) / 7f;
            for (int i = 0; i < 7; i++)
            {
                float fc = x0 + 0.010f + (i + 0.5f) * fusePitch;
                float fy = (bankY0 + bankY1) / 2f;
                p[SurfaceMaterial.BlackHousing].AddRoundedSlab(new Vector3(fc, fy, front - 0.001f), fuseWidth, 0.078f, 0.003f, 0.026f, 0.0015f, 3);
                float zb = front + 0.025f;
                // The handle: a grey rounded cover with a dark fuse window, grip ribs and a pivot.
                p[SurfaceMaterial.CabinetGrey].AddRoundedSlab(new Vector3(fc, fy - 0.001f, zb), 0.046f, 0.046f, 0.004f, 0.009f, 0.0015f, 3);
                p.Box(voidM, fc - 0.014f, fy + 0.0035f, zb + 0.0091f, fc + 0.014f, fy + 0.0115f, zb + 0.0094f);
                p[white].AddLathe(new[] { P(0.0030f, -0.012f, true), P(0.0030f, 0.012f, true), P(0.0001f, 0.012f, true) }, 10, true, new Vector3(fc, fy + 0.0075f, zb + 0.0090f), Quaternion.Euler(0f, 0f, 90f));
                for (int k = 0; k < 5; k++) p.Box(SurfaceMaterial.ServerBlack, fc - 0.016f, fy - 0.019f + k * 0.0042f, zb + 0.0091f, fc + 0.016f, fy - 0.0175f + k * 0.0042f, zb + 0.0099f);
                p.Disc(steel, fc + 0.017f, fy + 0.0155f, zb + 0.0091f, 0.0026f, 10);
                // Terminal blocks with two screws each, top and bottom.
                foreach (float ty in new[] { bankY0 + 0.0035f, bankY1 - 0.0035f })
                {
                    p.Box(SurfaceMaterial.RackBlack, fc - 0.020f, ty - 0.0035f, front + 0.0008f, fc + 0.020f, ty + 0.0035f, front + 0.0100f);
                    Screw(fc - 0.010f, ty, front + 0.0100f, 0.0030f);
                    Screw(fc + 0.010f, ty, front + 0.0100f, 0.0030f);
                }
            }

            // Two shelves of two battery blocks (0.225 x 0.30 x 0.20 m): a rounded case with stiffening ribs, a lid with a vent strip and six vent caps, a label, terminal bosses with studs,
            // washers and nuts, polarity marks, copper links. No LEDs, no cables.
            foreach (float shelfY in new[] { 0.80f, 1.06f })
            {
                p.Box(SurfaceMaterial.GalvanisedSteel, x0, shelfY, 0.05f, x1, shelfY + 0.015f, 0.40f);
                p.Box(SurfaceMaterial.GalvanisedSteel, x0, shelfY + 0.015f, 0.395f, x1, shelfY + 0.05f, 0.40f);
                for (int b = 0; b < 2; b++)
                {
                    float bx0 = x0 + 0.01f + b * 0.235f;
                    float cx = bx0 + 0.1125f;
                    float by0 = shelfY + 0.015f;
                    float zFace = 0.36f;
                    p[SurfaceMaterial.RackDoor].AddRoundedBlock(new Vector3(cx, by0, 0.21f), 0.225f, 0.30f, 0.010f, 0.170f, 0.003f, 4);
                    p[SurfaceMaterial.ServerBlack].AddRoundedBlock(new Vector3(cx, by0 + 0.170f, 0.21f), 0.229f, 0.304f, 0.011f, 0.030f, 0.006f, 4);
                    foreach (float ry in new[] { 0.030f, 0.075f, 0.120f }) p.Box(SurfaceMaterial.RackDoor, cx - 0.095f, by0 + ry, zFace, cx + 0.095f, by0 + ry + 0.006f, zFace + 0.0015f);
                    // The vent strip on the lid with six caps (a slot across each).
                    p.Box(SurfaceMaterial.ServerBlack, cx - 0.098f, by0 + 0.200f, 0.19f, cx + 0.098f, by0 + 0.204f, 0.23f);
                    for (int k = 0; k < 6; k++)
                    {
                        float vx = cx - 0.085f + k * 0.034f;
                        p[white].AddLathe(new[] { P(0.0105f, 0f, true), P(0.0105f, 0.003f, true), P(0.0075f, 0.0035f, true), P(0.0075f, 0.0075f, true), P(0.0055f, 0.0080f), P(0.0001f, 0.0080f, true) }, 14, true, new Vector3(vx, by0 + 0.204f, 0.21f), Quaternion.identity);
                        p.Box(voidM, vx - 0.0045f, by0 + 0.2118f, 0.2085f, vx + 0.0045f, by0 + 0.2123f, 0.2115f);
                    }
                    // The label and a small caution sticker.
                    Sticker(MeetLabel.Battery, cx, by0 + 0.082f, 0.150f, zFace + 0.0015f);
                    Sticker(MeetLabel.Caution, cx, by0 + 0.040f, 0.070f, zFace + 0.0015f);
                    // Terminals on the front of the lid: a rounded boss, a stud with washer and nut, a polarity mark.
                    float postY = by0 + 0.148f;
                    for (int t = 0; t < 2; t++)
                    {
                        float tx = t == 0 ? cx - 0.075f : cx + 0.075f;
                        p[SurfaceMaterial.ServerBlack].AddRoundedSlab(new Vector3(tx, postY, zFace), 0.046f, 0.046f, 0.009f, 0.008f, 0.002f, 4);
                        p[SurfaceMaterial.Brass].AddLathe(new[] { P(0.0062f, 0f, true), P(0.0062f, 0.0165f, true), P(0.0001f, 0.0165f, true) }, 14, true, new Vector3(tx, postY, zFace + 0.008f), rot);
                        p[steel].AddLathe(new[] { P(0.0115f, 0f, true), P(0.0115f, 0.0022f, true), P(0.0001f, 0.0022f, true) }, 18, true, new Vector3(tx, postY, zFace + 0.0095f), rot);
                        Hex(tx, postY, zFace + 0.0117f, 0.0092f, 0.0075f, steel);
                        float my = postY + 0.0335f;
                        p.Box(white, tx - 0.0055f, my - 0.0007f, zFace + 0.0015f, tx + 0.0055f, my + 0.0007f, zFace + 0.0020f);
                        if (t == 1) p.Box(white, tx - 0.0007f, my - 0.0055f, zFace + 0.0015f, tx + 0.0007f, my + 0.0055f, zFace + 0.0020f);
                    }
                    // The copper link to the next block: from this block's right post to the next one's left post.
                    if (b == 0)
                    {
                        float nx = cx + 0.075f;
                        float nx2 = nx + 0.235f - 0.150f;
                        p[SurfaceMaterial.Brass].AddRoundedSlab(new Vector3((nx + nx2) / 2f, postY, zFace + 0.0165f), nx2 - nx + 0.030f, 0.015f, 0.004f, 0.0025f, 0.0008f, 3);
                    }
                }
            }
        }
    }
}
