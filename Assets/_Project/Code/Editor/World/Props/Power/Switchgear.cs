using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Low-voltage switchgear section 0.8 x 2.2 x 0.6 m: a sheet-steel cabinet on a plinth with an instrument door on top (a large control panel as on the heat
        /// exchanger over the section tag, both centered) and three breaker doors below it. Each breaker door has a louver vent in a pocket at its top, a
        /// bezel with a molded-case breaker (rounded black housing, ON / OFF marks, a rating plate, four screws, a round rotary knob with a pointer),
        /// the transformer's lever lock and hinges. The busbar duct rises to the ceiling. No meters (the only meters of the power room are on its back wall). The front faces +Z.
        /// </summary>
        [Model(Prop.Switchgear)]
        private static void BuildSwitchgear(Transform root)
        {
            const float width = 0.8f;
            const float height = 2.2f;
            const float depth = 0.6f;
            const float skin = 0.03f;
            float front = depth / 2f;
            float core = front - skin;
            var grey = new MeshBuilder();
            var black = new MeshBuilder();
            var joint = new MeshBuilder();
            var bezel = new MeshBuilder();
            var housing = new MeshBuilder();
            var face = new MeshBuilder();
            var handle = new MeshBuilder();
            var lever = new MeshBuilder();
            var pointer = new MeshBuilder();
            var on = new MeshBuilder();
            var off = new MeshBuilder();
            var screws = new MeshBuilder();
            var chrome = new MeshBuilder();
            var slats = new MeshBuilder();
            var dark = new MeshBuilder();
            var duct = new MeshBuilder();
            black.AddRoundedBlock(Vector3.zero, width - 0.04f, depth - 0.04f, 0.015f, 0.1f, 0.005f);
            grey.AddExtrusion(RoundedRectLoop(-width / 2f, width / 2f, -depth / 2f, core, new[] { 0.015f, 0.015f, 0f, 0f }, 3), new Vector3(0f, 0.1f, 0f), new Vector3(0f, height, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);

            var holes = new List<Vector4>();
            var labels = new MeshBuilder();
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
    }
}
