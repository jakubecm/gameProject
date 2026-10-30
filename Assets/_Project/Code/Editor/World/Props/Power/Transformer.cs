using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Cast-resin transformer in its ventilated enclosure, 2.0 x 2.2 x 1.2 m: a sheet-steel enclosure with a black plinth and lifting eyes. Its front
        /// is a 0.3 m deep skin with real openings: a louver at the bottom and one at the top (slanted slats in a pocket, dark behind) and a deep inspection
        /// window with a rubber bezel and glass, behind which stand a steel core with laminated yokes and three ribbed cast-resin coils with copper
        /// taps. Between the window and the upper louver one row, centered on the door with equal gaps: the control panel (as on the heat exchanger), the
        /// rating plate and the high-voltage warning. A lever lock, three hinges, and the medium-voltage cables leave the roof in a rounded duct. The
        /// front faces +Z.
        /// </summary>
        [Model(Prop.Transformer)]
        private static void BuildTransformer(Transform root)
        {
            const float width = 2.0f;
            const float height = 2.2f;
            const float depth = 1.2f;
            const float skin = 0.3f;
            float front = depth / 2f;
            float core = front - skin;
            var grey = new MeshBuilder();
            var black = new MeshBuilder();
            var joint = new MeshBuilder();
            var chrome = new MeshBuilder();
            var glass = new MeshBuilder();
            var coil = new MeshBuilder();
            var grooves = new MeshBuilder();
            var copper = new MeshBuilder();
            var laminations = new MeshBuilder();
            var slats = new MeshBuilder();
            var dark = new MeshBuilder();
            var duct = new MeshBuilder();
            var rings = new MeshBuilder();

            black.AddRoundedBlock(Vector3.zero, width - 0.05f, depth - 0.05f, 0.02f, 0.1f, 0.006f);
            grey.AddExtrusion(RoundedRectLoop(-width / 2f, width / 2f, -depth / 2f, core, new[] { 0.03f, 0.03f, 0f, 0f }, 3), new Vector3(0f, 0.1f, 0f), new Vector3(0f, height - 0.06f, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            grey.AddExtrusion(RoundedRectLoop(-width / 2f - 0.02f, width / 2f + 0.02f, -depth / 2f - 0.02f, front + 0.02f, 0.04f, 3), new Vector3(0f, height - 0.06f, 0f), new Vector3(0f, height, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    var path = new List<Vector3>();
                    for (int i = 0; i < 16; i++)
                    {
                        float angle = i / 16f * 2f * Mathf.PI;
                        path.Add(new Vector3(sx * 0.8f + Mathf.Cos(angle) * 0.05f, height + 0.045f + Mathf.Sin(angle) * 0.045f, sz * 0.42f));
                    }
                    rings.AddTube(path, 0.009f, 8, Vector3.zero, Quaternion.identity, true);
                }
            }

            // The front skin with its three openings.
            const float winY0 = 0.49f;
            const float winY1 = 1.35f;
            const float winHalf = 0.63f;
            var holes = new List<Vector4>
            {
                new Vector4(-0.80f, 0.20f, 0.80f, 0.44f),
                new Vector4(-0.80f, 1.76f, 0.80f, 2.00f),
                new Vector4(-winHalf, winY0, winHalf, winY1),
            };
            AddSkin(grey, -width / 2f, width / 2f, 0.1f, height - 0.06f, core, front, holes);
            AddVent(slats, dark, -0.80f, 0.80f, 0.20f, 0.44f, core, front - 0.040f, front, 0.032f);
            AddVent(slats, dark, -0.80f, 0.80f, 1.76f, 2.00f, core, front - 0.040f, front, 0.032f);
            AddDoorJoint(joint, -0.93f, 0.93f, 0.14f, 2.08f, front);

            // The window: a dark pocket with the core and the coils, a rubber bezel and the glass 12 mm behind the face.
            dark.AddBox(new Vector3(-winHalf, winY0, core - 0.001f), new Vector3(winHalf, winY1, core + 0.001f), false, false);
            const float bar = 0.026f;
            black.AddBox(new Vector3(-winHalf - bar, winY0 - bar, front), new Vector3(winHalf + bar, winY0, front + 0.012f));
            black.AddBox(new Vector3(-winHalf - bar, winY1, front), new Vector3(winHalf + bar, winY1 + bar, front + 0.012f));
            black.AddBox(new Vector3(-winHalf - bar, winY0, front), new Vector3(-winHalf, winY1, front + 0.012f));
            black.AddBox(new Vector3(winHalf, winY0, front), new Vector3(winHalf + bar, winY1, front + 0.012f));
            glass.AddBox(new Vector3(-winHalf, winY0 + 0.004f, front - 0.014f), new Vector3(winHalf, winY1 - 0.004f, front - 0.012f));
            float coilZ = core + 0.14f;
            const float coilR = 0.13f;
            const float coilY0 = 0.62f;
            const float coilH = 0.62f;
            // Core: two laminated yokes and three limbs inside the coils.
            laminations.AddBox(new Vector3(-0.58f, winY0 + 0.02f, coilZ - 0.07f), new Vector3(0.58f, coilY0 - 0.02f, coilZ + 0.07f));
            laminations.AddBox(new Vector3(-0.58f, coilY0 + coilH + 0.02f, coilZ - 0.07f), new Vector3(0.58f, winY1 - 0.02f, coilZ + 0.07f));
            for (float y = winY0 + 0.03f; y < coilY0 - 0.025f; y += 0.016f)
            {
                grooves.AddBox(new Vector3(-0.58f, y, coilZ + 0.07f), new Vector3(0.58f, y + 0.0035f, coilZ + 0.0705f), true, false);
            }
            for (float y = coilY0 + coilH + 0.03f; y < winY1 - 0.025f; y += 0.016f)
            {
                grooves.AddBox(new Vector3(-0.58f, y, coilZ + 0.07f), new Vector3(0.58f, y + 0.0035f, coilZ + 0.0705f), true, false);
            }
            foreach (float x in new[] { -0.38f, 0f, 0.38f })
            {
                coil.AddLathe(new[]
                {
                    P(0.0001f, 0f, true), P(coilR - 0.02f, 0f, true), P(coilR - 0.004f, 0.008f), P(coilR, 0.02f, true), P(coilR, coilH - 0.02f, true),
                    P(coilR - 0.004f, coilH - 0.008f), P(coilR - 0.02f, coilH, true), P(0.0001f, coilH, true),
                }, 40, true, new Vector3(x, coilY0, coilZ), Quaternion.identity);
                // Cooling-duct spacers and the layer joints, as thin dark rings round the coil.
                for (int i = 1; i < 8; i++)
                {
                    float y = coilY0 + i * coilH / 8f;
                    grooves.AddLathe(new[] { P(coilR - 0.002f, y - 0.003f, true), P(coilR + 0.002f, y - 0.003f, true), P(coilR + 0.002f, y + 0.003f, true), P(coilR - 0.002f, y + 0.003f, true) }, 40, true,
                        new Vector3(x, 0f, coilZ), Quaternion.identity);
                }
                // Copper taps on the front of the coil: a plate with a bolt, three per coil.
                for (int tap = 0; tap < 3; tap++)
                {
                    float y = coilY0 + 0.20f + tap * 0.12f;
                    copper.AddBox(new Vector3(x - 0.028f, y - 0.014f, coilZ + coilR - 0.002f), new Vector3(x + 0.028f, y + 0.014f, coilZ + coilR + 0.006f));
                    copper.AddLathe(new[] { P(0.009f, 0f, true), P(0.009f, 0.007f, true), P(0.0001f, 0.007f, true) }, 6, false, new Vector3(x, y, coilZ + coilR + 0.006f), Quaternion.Euler(90f, 0f, 0f));
                }
            }

            // Lever lock on the right-hand edge, three hinges on the left.
            AddLeverLock(black, chrome, 0.82f, 1.11f, 1f, front);
            foreach (float y in new[] { 0.40f, 1.11f, 1.82f })
            {
                chrome.AddLathe(new[] { P(0.013f, 0f, true), P(0.013f, 0.10f, true), P(0.0001f, 0.10f, true) }, 16, true, new Vector3(-0.945f, y - 0.05f, front), Quaternion.identity);
            }
            AddDuct(duct, -0.2f, 0.2f, -0.4f, -0.2f, height, RoomContract.CeilingHeight);

            // One row between the window and the upper louver: control panel, rating plate, warning, equal gaps, centered on the door.
            const float rowY = 1.57f;
            const float gap = 0.09f;
            float[] widths = { 0.405f, 0.30f, 0.36f };
            float total = widths[0] + widths[1] + widths[2] + 2f * gap;
            float cursor = -total / 2f;
            var panel = new ControlPanelMeshes();
            panel.Add(new Vector3(cursor + widths[0] / 2f, rowY, front), 0.75f);
            cursor += widths[0] + gap;
            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(cursor + widths[1] / 2f, rowY, front), Vector3.left, Vector3.up, widths[1], 0.15f, PlantLabel.TransformerPlate);
            cursor += widths[1] + gap;
            AddLabel(labels, new Vector3(cursor + widths[2] / 2f, rowY, front), Vector3.left, Vector3.up, widths[2], 0.18f, PlantLabel.Danger);

            grey.Attach("Enclosure", root, SurfaceMaterial.ElectricGrey);
            black.Attach("Plinth", root, SurfaceMaterial.BlackHousing);
            joint.Attach("Joints", root, SurfaceMaterial.BlackHousing);
            slats.Attach("Louvers", root, SurfaceMaterial.ElectricGrey);
            dark.Attach("PocketBacks", root, SurfaceMaterial.BlackHousing);
            coil.Attach("Coils", root, SurfaceMaterial.ResinCoil);
            grooves.Attach("CoilRings", root, SurfaceMaterial.BlackHousing);
            copper.Attach("Taps", root, SurfaceMaterial.CopperTrim);
            laminations.Attach("CoreYokes", root, SurfaceMaterial.Steel);
            glass.Attach("Window", root, SurfaceMaterial.DomeClear);
            chrome.Attach("Lock", root, SurfaceMaterial.Chrome);
            rings.Attach("LiftingEyes", root, SurfaceMaterial.Chrome);
            duct.Attach("CableDuct", root, SurfaceMaterial.Trunking);
            panel.Attach("Control", root);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }
    }
}
