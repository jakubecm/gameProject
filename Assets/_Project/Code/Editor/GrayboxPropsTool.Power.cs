using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Plant of the power room, modelled in the same detail as the cooling plant: the cast-resin transformer in its louvered
    /// enclosure, the low-voltage switchgear section, the UPS cabinet and the panel of kW meters opposite the door. Sizes are those of
    /// the earlier graybox props; every front faces +Z. The control panels are the one of the plate heat exchanger scaled to fit.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>
        /// The control panel of the heat exchanger (rounded grey plate, display, start / stop and amber buttons, emergency stop) at any size:
        /// <c>k</c> is its width over 0.54 m. It faces +Z and stands on the surface at <c>z</c> of the centre.
        /// </summary>
        private sealed class ControlPanelMeshes
        {
            private readonly GrayboxMeshBuilder plastic = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder screen = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder green = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder red = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder amber = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder grey = new GrayboxMeshBuilder();

            public void Add(Vector3 c, float k)
            {
                var toZ = Quaternion.Euler(90f, 0f, 0f);
                float t = 0.03f * k;
                float z = c.z;
                plastic.AddExtrusion(RoundedRectLoop(c.x - 0.27f * k, c.x + 0.27f * k, c.y - 0.13f * k, c.y + 0.13f * k, 0.018f * k, 3),
                    new Vector3(0f, 0f, z), new Vector3(0f, 0f, z + t), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                screen.AddExtrusion(RoundedRectLoop(c.x - 0.245f * k, c.x - 0.045f * k, c.y + 0.01f * k, c.y + 0.105f * k, 0.006f * k, 2),
                    new Vector3(0f, 0f, z + t), new Vector3(0f, 0f, z + t + 0.004f * k), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                void Button(GrayboxMeshBuilder b, float x, float y, float radius, float lift)
                {
                    radius *= k;
                    lift *= k;
                    b.AddLathe(new[] { P(radius, 0f, true), P(radius, lift * 0.7f, true), P(radius * 0.8f, lift, true), P(0.0001f, lift, true) }, 24, true,
                        new Vector3(c.x + x * k, c.y + y * k, z + t), toZ);
                }
                for (int i = 0; i < 4; i++) Button(grey, -0.225f + i * 0.055f, -0.07f, 0.017f, 0.008f);
                Button(green, 0.045f, 0.07f, 0.028f, 0.012f);
                Button(red, 0.125f, 0.07f, 0.028f, 0.012f);
                Button(amber, 0.205f, 0.07f, 0.02f, 0.01f);
                Button(amber, 0.045f, -0.055f, 0.012f, 0.008f);
                Button(amber, 0.205f, -0.055f, 0.012f, 0.008f);
                Button(amber, 0.125f, -0.04f, 0.045f, 0.008f);
                red.AddLathe(new[] { P(0.0001f, 0.008f * k, true), P(0.032f * k, 0.008f * k, true), P(0.034f * k, 0.018f * k), P(0.026f * k, 0.03f * k), P(0.0001f, 0.032f * k, true) }, 24, true,
                    new Vector3(c.x + 0.125f * k, c.y - 0.04f * k, z + t), toZ);
            }

            public void Attach(string prefix, Transform parent)
            {
                plastic.Attach($"{prefix}_Plate", parent, SurfaceMaterial.CabinetGrey);
                screen.Attach($"{prefix}_Display", parent, SurfaceMaterial.Screen);
                green.Attach($"{prefix}_Start", parent, SurfaceMaterial.SafetyGreen);
                red.Attach($"{prefix}_Stop", parent, SurfaceMaterial.SafetyRed);
                amber.Attach($"{prefix}_Amber", parent, SurfaceMaterial.SafetyYellow);
                grey.Attach($"{prefix}_Buttons", parent, SurfaceMaterial.KeyboardKey);
            }
        }

        /// <summary>A rounded rectangular block between two Z positions, for door panels, ducts and plates.</summary>
        private static void AddPlate(GrayboxMeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1, float radius) =>
            b.AddExtrusion(RoundedRectLoop(x0, x1, y0, y1, radius, 2), new Vector3(0f, 0f, z0), new Vector3(0f, 0f, z1), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

        /// <summary>The outline of a door on the front face: four thin dark joints, 3 mm wide, flush with the surface.</summary>
        private static void AddDoorJoint(GrayboxMeshBuilder joint, float x0, float x1, float y0, float y1, float zFace)
        {
            const float w = 0.003f;
            const float lift = 0.0006f;
            joint.AddBox(new Vector3(x0, y0, zFace), new Vector3(x1, y0 + w, zFace + lift), true, false);
            joint.AddBox(new Vector3(x0, y1 - w, zFace), new Vector3(x1, y1, zFace + lift), true, false);
            joint.AddBox(new Vector3(x0, y0 + w, zFace), new Vector3(x0 + w, y1 - w, zFace + lift), true, false);
            joint.AddBox(new Vector3(x1 - w, y0 + w, zFace), new Vector3(x1, y1 - w, zFace + lift), true, false);
        }

        /// <summary>
        /// The front skin of a cabinet between <paramref name="zBack"/> and <paramref name="zFront"/> over x0..x1 and y0..y1 with rectangular openings
        /// (x0, y0, x1, y1 each) cut out, as boxes that tile the face exactly: the cut edges are the walls of the pockets the vents sit in.
        /// </summary>
        private static void AddSkin(GrayboxMeshBuilder b, float x0, float x1, float y0, float y1, float zBack, float zFront, IList<Vector4> holes)
        {
            var ys = new List<float> { y0, y1 };
            foreach (var h in holes)
            {
                ys.Add(Mathf.Clamp(h.y, y0, y1));
                ys.Add(Mathf.Clamp(h.w, y0, y1));
            }
            ys.Sort();
            for (int i = 0; i < ys.Count - 1; i++)
            {
                float ya = ys[i];
                float yb = ys[i + 1];
                if (yb - ya < 1e-4f) continue;
                float ym = (ya + yb) / 2f;
                var spans = new List<Vector2>();
                foreach (var h in holes)
                {
                    if (ym > h.y && ym < h.w) spans.Add(new Vector2(h.x, h.z));
                }
                spans.Sort((p, q) => p.x.CompareTo(q.x));
                float x = x0;
                foreach (var span in spans)
                {
                    if (span.x - x > 1e-4f) b.AddBox(new Vector3(x, ya, zBack), new Vector3(span.x, yb, zFront));
                    x = Mathf.Max(x, span.y);
                }
                if (x1 - x > 1e-4f) b.AddBox(new Vector3(x, ya, zBack), new Vector3(x1, yb, zFront));
            }
        }

        /// <summary>
        /// A louvered vent in a pocket cut in the skin: slanted slats (sloping down and out, each overlapping the one below, as on a real weather
        /// louver) from <paramref name="zIn"/> to <paramref name="zFront"/>, and a dark back plate at <paramref name="zBackPlane"/>.
        /// </summary>
        private static void AddVent(GrayboxMeshBuilder slats, GrayboxMeshBuilder back, float x0, float x1, float y0, float y1, float zBackPlane, float zIn, float zFront, float pitch)
        {
            back.AddBox(new Vector3(x0, y0, zBackPlane - 0.001f), new Vector3(x1, y1, zBackPlane + 0.001f), false, false);
            int count = Mathf.Max(1, Mathf.RoundToInt((y1 - y0) / pitch));
            float step = (y1 - y0) / count;
            float zOut = zFront - 0.003f;
            float drop = Mathf.Min(step * 0.95f, (zOut - zIn) * 0.8f);
            const float t = 0.003f;
            for (int i = 0; i < count; i++)
            {
                float y = y0 + step * (i + 1) - 0.002f;
                var slat = new List<Vector2> { new Vector2(zIn, y), new Vector2(zOut, y - drop), new Vector2(zOut, y - drop + t), new Vector2(zIn, y + t) };
                slats.AddExtrusion(slat, new Vector3(x0, 0f, 0f), new Vector3(x1, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
            }
        }

        /// <summary>Flange ring round a duct where it meets the roof or the ceiling.</summary>
        private static void AddDuct(GrayboxMeshBuilder duct, float x0, float x1, float z0, float z1, float yBottom, float yTop)
        {
            duct.AddExtrusion(RoundedRectLoop(x0, x1, z0, z1, 0.02f, 3), new Vector3(0f, yBottom, 0f), new Vector3(0f, yTop, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            const float flange = 0.025f;
            foreach (float y in new[] { yBottom, yTop - 0.012f })
            {
                duct.AddExtrusion(RoundedRectLoop(x0 - flange, x1 + flange, z0 - flange, z1 + flange, 0.03f, 3), new Vector3(0f, y, 0f), new Vector3(0f, y + 0.012f, 0f),
                    Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            }
        }

        /// <summary>
        /// The lever lock of the transformer door at any size (<paramref name="scale"/> 1 = 0.06 x 0.24 m): a black rounded plate, a chrome lever bar and a round lock in the
        /// middle, centered on (<paramref name="x"/>, <paramref name="y"/>) of the front face at <paramref name="z"/>.
        /// </summary>
        private static void AddLeverLock(GrayboxMeshBuilder plate, GrayboxMeshBuilder metal, float x, float y, float scale, float z)
        {
            AddPlate(plate, x - 0.03f * scale, x + 0.03f * scale, y - 0.12f * scale, y + 0.12f * scale, z, z + 0.012f * scale, 0.012f * scale);
            AddPlate(metal, x - 0.012f * scale, x + 0.012f * scale, y - 0.09f * scale, y + 0.09f * scale, z + 0.012f * scale, z + 0.026f * scale, 0.008f * scale);
            metal.AddLathe(new[] { P(0.016f * scale, 0.012f * scale, true), P(0.016f * scale, 0.034f * scale, true), P(0.0001f, 0.034f * scale, true) }, 20, true, new Vector3(x, y, z), Quaternion.Euler(90f, 0f, 0f));
        }

        /// <summary>
        /// Cast-resin transformer in its ventilated enclosure, 2.0 x 2.2 x 1.2 m: a sheet-steel enclosure with a black plinth and lifting eyes. Its front
        /// is a 0.3 m deep skin with real openings: a louver at the bottom and one at the top (slanted slats in a pocket, dark behind) and a deep inspection
        /// window with a rubber bezel and glass, behind which stand a steel core with laminated yokes and three ribbed cast-resin coils with copper
        /// taps. Between the window and the upper louver one row, centered on the door with equal gaps: the control panel (as on the heat exchanger), the
        /// rating plate and the high-voltage warning. A lever lock, three hinges, and the medium-voltage cables leave the roof in a rounded duct. The
        /// front faces +Z.
        /// </summary>
        private static void BuildTransformer(Transform root)
        {
            const float width = 2.0f;
            const float height = 2.2f;
            const float depth = 1.2f;
            const float skin = 0.3f;
            float front = depth / 2f;
            float core = front - skin;
            var grey = new GrayboxMeshBuilder();
            var black = new GrayboxMeshBuilder();
            var joint = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            var glass = new GrayboxMeshBuilder();
            var coil = new GrayboxMeshBuilder();
            var grooves = new GrayboxMeshBuilder();
            var copper = new GrayboxMeshBuilder();
            var laminations = new GrayboxMeshBuilder();
            var slats = new GrayboxMeshBuilder();
            var dark = new GrayboxMeshBuilder();
            var duct = new GrayboxMeshBuilder();
            var rings = new GrayboxMeshBuilder();

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
            var labels = new GrayboxMeshBuilder();
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

        /// <summary>
        /// Low-voltage switchgear section 0.8 x 2.2 x 0.6 m: a sheet-steel cabinet on a plinth with an instrument door on top (a large control panel as on the heat
        /// exchanger over the section tag, both centered) and three breaker doors below it. Each breaker door has a louver vent in a pocket at its top, a
        /// bezel with a molded-case breaker (rounded black housing, ON / OFF marks, a rating plate, four screws, a round rotary knob with a pointer),
        /// the transformer's lever lock and hinges. The busbar duct rises to the ceiling. No meters (the only meters of the power room are on its back wall). The front faces +Z.
        /// </summary>
        private static void BuildSwitchgearCabinet(Transform root)
        {
            const float width = 0.8f;
            const float height = 2.2f;
            const float depth = 0.6f;
            const float skin = 0.03f;
            float front = depth / 2f;
            float core = front - skin;
            var grey = new GrayboxMeshBuilder();
            var black = new GrayboxMeshBuilder();
            var joint = new GrayboxMeshBuilder();
            var bezel = new GrayboxMeshBuilder();
            var housing = new GrayboxMeshBuilder();
            var face = new GrayboxMeshBuilder();
            var handle = new GrayboxMeshBuilder();
            var lever = new GrayboxMeshBuilder();
            var pointer = new GrayboxMeshBuilder();
            var on = new GrayboxMeshBuilder();
            var off = new GrayboxMeshBuilder();
            var screws = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            var slats = new GrayboxMeshBuilder();
            var dark = new GrayboxMeshBuilder();
            var duct = new GrayboxMeshBuilder();
            black.AddRoundedBlock(Vector3.zero, width - 0.04f, depth - 0.04f, 0.015f, 0.1f, 0.005f);
            grey.AddExtrusion(RoundedRectLoop(-width / 2f, width / 2f, -depth / 2f, core, new[] { 0.015f, 0.015f, 0f, 0f }, 3), new Vector3(0f, 0.1f, 0f), new Vector3(0f, height, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);

            var holes = new List<Vector4>();
            var labels = new GrayboxMeshBuilder();
            PlantLabel[] plates = { PlantLabel.BreakerQ3, PlantLabel.BreakerQ2, PlantLabel.BreakerQ1 };
            for (int i = 0; i < 3; i++)
            {
                float y0 = 0.14f + i * 0.49f;
                float y1 = y0 + 0.45f;
                holes.Add(new Vector4(-0.20f, y1 - 0.075f, 0.20f, y1 - 0.02f));
                AddDoorJoint(joint, -0.36f, 0.36f, y0, y1, front);
            }
            AddDoorJoint(joint, -0.36f, 0.36f, 1.62f, 2.14f, front);
            AddSkin(grey, -width / 2f, width / 2f, 0.1f, height, core, front, holes);
            for (int i = 0; i < 3; i++)
            {
                float y0 = 0.14f + i * 0.49f;
                float y1 = y0 + 0.45f;
                float yc = y0 + 0.225f;
                AddVent(slats, dark, -0.20f, 0.20f, y1 - 0.075f, y1 - 0.02f, core, front - 0.024f, front, 0.0185f);

                // Breaker in a bezel: a raised ring, the rounded housing, its darker face with the lever on a base, indicators, buttons, a plate and four screws.
                const float bx = 0.17f;
                const float by0 = -0.105f;
                const float by1 = 0.055f;
                float cy = yc + 0.025f;   // the housing's face (cy - 0.025) is centered on the door
                black.AddBox(new Vector3(-bx - 0.018f, cy + by0 - 0.018f, front), new Vector3(bx + 0.018f, cy + by0, front + 0.007f));
                black.AddBox(new Vector3(-bx - 0.018f, cy + by1, front), new Vector3(bx + 0.018f, cy + by1 + 0.018f, front + 0.007f));
                black.AddBox(new Vector3(-bx - 0.018f, cy + by0, front), new Vector3(-bx, cy + by1, front + 0.007f));
                black.AddBox(new Vector3(bx, cy + by0, front), new Vector3(bx + 0.018f, cy + by1, front + 0.007f));
                AddPlate(housing, -bx, bx, cy + by0, cy + by1, front, front + 0.052f, 0.014f);
                AddPlate(face, -bx + 0.02f, bx - 0.02f, cy + by0 + 0.02f, cy + by1 - 0.02f, front + 0.052f, front + 0.058f, 0.008f);
                // A round rotary knob (turn left or right) with a white pointer, in the free space between the plate and the top edge of the face, flanked by the ON / OFF marks.
                float knobY = cy - 0.0165f + 0.023f;
                handle.AddLathe(new[] { P(0.024f, 0f, true), P(0.024f, 0.003f, true), P(0.020f, 0.004f, true) }, 28, true, new Vector3(0f, knobY, front + 0.058f), Quaternion.Euler(90f, 0f, 0f));
                lever.AddLathe(new[] { P(0.0185f, 0.004f, true), P(0.0185f, 0.015f, true), P(0.0165f, 0.019f), P(0.0001f, 0.020f, true) }, 28, true, new Vector3(0f, knobY, front + 0.058f), Quaternion.Euler(90f, 0f, 0f));
                pointer.AddBox(new Vector3(-0.0016f, knobY, front + 0.0775f), new Vector3(0.0016f, knobY + 0.0165f, front + 0.0782f), true, false);
                on.AddBox(new Vector3(0.055f, knobY - 0.005f, front + 0.058f), new Vector3(0.085f, knobY + 0.005f, front + 0.0595f));
                off.AddBox(new Vector3(-0.085f, knobY - 0.005f, front + 0.058f), new Vector3(-0.055f, knobY + 0.005f, front + 0.0595f));
                AddLabel(labels, new Vector3(0f, cy - 0.052f, front + 0.058f), Vector3.left, Vector3.up, 0.12f, 0.06f, plates[i]);
                foreach (int sx in new[] { -1, 1 })
                {
                    foreach (int sy in new[] { -1, 1 })
                    {
                        screws.AddLathe(new[] { P(0.0065f, 0f, true), P(0.0065f, 0.004f, true), P(0.0050f, 0.0055f), P(0.0001f, 0.0055f, true) }, 6, false,
                            new Vector3(sx * (bx - 0.011f), cy + (sy < 0 ? by0 + 0.011f : by1 - 0.011f), front + 0.052f), Quaternion.Euler(90f, 0f, 0f));
                    }
                }
                // Quarter-turn handle on the right, two hinges on the left.
                AddLeverLock(handle, chrome, 0.31f, yc, 0.65f, front);
                foreach (float hy in new[] { y0 + 0.06f, y1 - 0.06f })
                {
                    chrome.AddLathe(new[] { P(0.007f, 0f, true), P(0.007f, 0.06f, true), P(0.0001f, 0.06f, true) }, 12, true, new Vector3(-0.372f, hy - 0.03f, front), Quaternion.identity);
                }
            }

            // Instrument door: one large control panel over the section tag, equally spaced and centered on the door.
            var panel = new ControlPanelMeshes();
            panel.Add(new Vector3(0f, 1.9675f, front), 1.05f);
            AddLabel(labels, new Vector3(0f, 1.725f, front), Vector3.left, Vector3.up, 0.28f, 0.14f, PlantLabel.SwitchgearTag);
            AddDuct(duct, -0.15f, 0.15f, -0.2f, 0.05f, height, RoomContract.CeilingHeight);

            grey.Attach("Cabinet", root, SurfaceMaterial.ElectricGrey);
            black.Attach("Plinth", root, SurfaceMaterial.BlackHousing);
            joint.Attach("Joints", root, SurfaceMaterial.BlackHousing);
            slats.Attach("Vents", root, SurfaceMaterial.ElectricGrey);
            dark.Attach("VentBacks", root, SurfaceMaterial.BlackHousing);
            housing.Attach("BreakerHousings", root, SurfaceMaterial.BlackHousing);
            face.Attach("BreakerFaces", root, SurfaceMaterial.KeyboardKey);
            lever.Attach("BreakerKnobs", root, SurfaceMaterial.BlackHousing);
            pointer.Attach("KnobPointers", root, SurfaceMaterial.SignFace);
            on.Attach("IndicatorOn", root, SurfaceMaterial.SafetyGreen);
            off.Attach("IndicatorOff", root, SurfaceMaterial.SafetyRed);
            screws.Attach("Screws", root, SurfaceMaterial.Chrome);
            chrome.Attach("Hardware", root, SurfaceMaterial.Chrome);
            handle.Attach("Handles", root, SurfaceMaterial.BlackHousing);
            duct.Attach("BusbarDuct", root, SurfaceMaterial.Trunking);
            panel.Attach("Control", root);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }

        /// <summary>
        /// UPS cabinet 0.6 x 2.0 x 0.8 m: a graphite cabinet on four feet whose front has two real vents cut in a 3 cm skin (a louver at the top and a larger one at the
        /// bottom, slanted slats in pockets), a control panel as on the heat exchanger over the unit tag (both centered on the cabinet), a green status strip and a
        /// vertical handle with a lock. The front faces +Z.
        /// </summary>
        private static void BuildEquipmentCabinet(Transform root)
        {
            const float width = 0.6f;
            const float height = 2.0f;
            const float depth = 0.8f;
            const float skin = 0.03f;
            float front = depth / 2f;
            float core = front - skin;
            var body = new GrayboxMeshBuilder();
            var feet = new GrayboxMeshBuilder();
            var trim = new GrayboxMeshBuilder();
            var slats = new GrayboxMeshBuilder();
            var dark = new GrayboxMeshBuilder();
            var led = new GrayboxMeshBuilder();
            var handle = new GrayboxMeshBuilder();
            var handleBase = new GrayboxMeshBuilder();
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    feet.AddRoundedBlock(new Vector3(sx * 0.25f, 0f, sz * 0.35f), 0.07f, 0.07f, 0.015f, 0.05f, 0.005f);
                }
            }
            // Square corners and no roof lip, so two cabinets side by side share one flush top and read as one unit.
            body.AddExtrusion(RoundedRectLoop(-width / 2f, width / 2f, -depth / 2f, core, 0f), new Vector3(0f, 0.05f, 0f), new Vector3(0f, height, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            var holes = new List<Vector4> { new Vector4(-0.23f, 1.56f, 0.23f, 1.88f), new Vector4(-0.23f, 0.16f, 0.23f, 0.84f) };
            AddSkin(body, -width / 2f, width / 2f, 0.05f, height - 0.03f, core, front, holes);
            AddVent(slats, dark, -0.23f, 0.23f, 1.56f, 1.88f, core, front - 0.024f, front, 0.032f);
            AddVent(slats, dark, -0.23f, 0.23f, 0.16f, 0.84f, core, front - 0.024f, front, 0.032f);
            AddDoorJoint(trim, -0.28f, 0.28f, 0.10f, 1.94f, front);
            led.AddBox(new Vector3(-0.15f, 1.47f, front), new Vector3(0.15f, 1.482f, front + 0.002f), true, false);
            AddLeverLock(handleBase, handle, 0.2f, 1.02f, 0.85f, front);

            var panel = new ControlPanelMeshes();
            panel.Add(new Vector3(0f, 1.30f, front), 0.75f);
            var labels = new GrayboxMeshBuilder();
            AddLabel(labels, new Vector3(0f, 1.02f, front), Vector3.left, Vector3.up, 0.30f, 0.15f, PlantLabel.UpsTag);

            body.Attach("Cabinet", root, SurfaceMaterial.UpsGraphite);
            feet.Attach("Feet", root, SurfaceMaterial.BlackHousing);
            trim.Attach("Joints", root, SurfaceMaterial.CabinetPull);
            slats.Attach("Vents", root, SurfaceMaterial.UpsGraphite);
            dark.Attach("VentBacks", root, SurfaceMaterial.BlackHousing);
            led.Attach("StatusStrip", root, SurfaceMaterial.SafetyGreen);
            handleBase.Attach("HandlePlate", root, SurfaceMaterial.BlackHousing);
            handle.Attach("Handle", root, SurfaceMaterial.Chrome);
            panel.Attach("Control", root);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }

        /// <summary>
        /// The panel of six power meters opposite the door of the power room (load of the hall sections): large dials, the same size as the
        /// thermometers of the cooling plant, but reading the input power in kW, in two rows of three, exactly centered on a board with rounded corners
        /// (the same 10 cm margin all round, no labels) that is fed by the one electrical trunking of the power room up the wall to the ceiling
        /// (<see cref="RoomInfrastructure.Trunking"/>). The needles stand still: the panel is only a look. A wall prop.
        /// </summary>
        private static void BuildAnalogMeterPanel(Transform root)
        {
            const float dialRadius = 0.15f;
            const float margin = 0.10f;
            const float pitchX = 0.40f;
            const float pitchY = 0.42f;
            const float width = 2f * pitchX + 2f * dialRadius + 2f * margin;
            const float height = pitchY + 2f * dialRadius + 2f * margin;
            const float bottom = 1.15f;
            var board = new GrayboxMeshBuilder();
            var lip = new GrayboxMeshBuilder();
            board.AddExtrusion(RoundedRectLoop(-width / 2f, width / 2f, bottom, bottom + height, 0.05f, 4), Vector3.zero, new Vector3(0f, 0f, 0.04f),
                Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            lip.AddExtrusion(RoundedRectLoop(-width / 2f + 0.012f, width / 2f - 0.012f, bottom + 0.012f, bottom + height - 0.012f, 0.04f, 4), new Vector3(0f, 0f, 0.04f), new Vector3(0f, 0f, 0.046f),
                Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            board.Attach("Board", root, SurfaceMaterial.CabinetGrey);
            lip.Attach("BoardLip", root, SurfaceMaterial.ElectricGrey);

            var dials = new DialMeshes(SurfaceMaterial.PowerDialFace);
            float[] readings = { 0.28f, 0.34f, 0.31f, 0.40f, 0.26f, 0.36f };
            float centreY = bottom + height / 2f;
            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    float x = (column - 1) * pitchX;
                    float y = centreY + (row == 0 ? 1f : -1f) * pitchY / 2f;
                    GrayboxUtility.CreateGroup($"Thermometer_{row}{column}", root).localPosition = new Vector3(x, y, 0.046f);
                    dials.AddDial(new Vector3(x, y, 0.046f), dialRadius, 0.005f, readings[row * 3 + column]);
                }
            }
            dials.Attach("Dials", root);
            RoomInfrastructure.Trunking(root, "Trunking", new Vector3(0f, bottom + height, 0f), new Vector3(0f, RoomContract.CeilingHeight, 0f), Vector3.forward);
        }
    }
}
