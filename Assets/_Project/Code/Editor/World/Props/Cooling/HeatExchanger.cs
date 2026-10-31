using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        [Model(Prop.HeatExchanger)]
        private static void BuildHeatExchanger(Transform root) => BuildExchanger(root, 1f);

        /// <summary>The same exchanger with its connections at the other end, for the exchanger that stands on the other side wall.</summary>
        [Model(Prop.HeatExchangerMirrored)]
        private static void BuildHeatExchangerMirrored(Transform root) => BuildExchanger(root, -1f);

        /// <summary>
        /// Large plate heat exchanger 2.2 x 2.0 x 0.9 m (chilled water to the hall's cooling loop), insulated like the photo: a box
        /// clad in aluminium whose lower front is folded into 4 x 3 diamond panels and whose upper front is a flat control strip with
        /// a centred control panel (display, push buttons, emergency stop), a warning sticker and the equipment tag, on a blue steel skid
        /// made of channels with levelling feet and anchor bolts. No gauges. Four insulated pipes leave the back: the primary flow and
        /// return rise to the ceiling, the secondary flow (to 2.62 m) and return (to 2.40 m) rise to the lanes the room's wall pipes
        /// take from there. <paramref name="sign"/> -1 puts the secondary pair at the other end. The service side faces +Z.
        /// </summary>
        private static void BuildExchanger(Transform root, float sign)
        {
            const float length = 2.2f;
            const float height = 2.0f;
            const float depth = 0.9f;
            const float skid = 0.1f;
            const float bodyHalfX = 1.0f;
            const float bodyHalfZ = 0.38f;
            const float stripBottom = 1.5f;
            float ceiling = RoomContract.CeilingHeight;
            float zf = bodyHalfZ;
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            // Skid: C-channels along X (web on the outside, flanges inward) and across, levelling feet and anchor bolts.
            var blue = new MeshBuilder();
            var chrome = new MeshBuilder();
            foreach (int sz in new[] { -1, 1 })
            {
                float outer = sz * 0.34f;
                float inner = outer - sz * 0.09f;
                float web = outer - sz * 0.012f;
                blue.AddBox(new Vector3(-length / 2f, 0.014f, Mathf.Min(web, outer)), new Vector3(length / 2f, skid - 0.014f, Mathf.Max(web, outer)));
                blue.AddBox(new Vector3(-length / 2f, 0.014f, Mathf.Min(inner, outer)), new Vector3(length / 2f, 0.03f, Mathf.Max(inner, outer)));
                blue.AddBox(new Vector3(-length / 2f, skid - 0.03f, Mathf.Min(inner, outer)), new Vector3(length / 2f, skid - 0.014f, Mathf.Max(inner, outer)));
            }
            foreach (float x in new[] { -0.95f, -0.35f, 0.35f, 0.95f })
            {
                blue.AddBox(new Vector3(x - 0.04f, 0.03f, -0.33f), new Vector3(x + 0.04f, skid - 0.03f, 0.33f));
                blue.AddBox(new Vector3(x - 0.04f, 0.03f, -0.33f), new Vector3(x - 0.032f, skid - 0.014f, 0.33f));
            }
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    // Under the middle of the channel's flange band (z 0.25 to 0.34), wholly inside the frame's outline, so no foot sticks out.
                    var corner = new Vector3(sx * (length / 2f - 0.06f), 0f, sz * 0.295f);
                    chrome.AddLathe(new[] { P(0.04f, 0f, true), P(0.04f, 0.014f, true), P(0.0001f, 0.014f, true) }, 24, true, corner, Quaternion.identity);
                    chrome.AddLathe(new[] { P(0.016f, 0.014f, true), P(0.016f, 0.026f, true), P(0.010f, 0.032f, true), P(0.0001f, 0.032f, true) }, 6, false, corner, Quaternion.identity);
                }
            }
            blue.Attach("Skid", root, SurfaceMaterial.SupportBlue);
            chrome.Attach("Feet", root, SurfaceMaterial.Chrome);

            var cladding = new MeshBuilder();
            cladding.AddExtrusion(RoundedRectLoop(-bodyHalfX, bodyHalfX, -bodyHalfZ, bodyHalfZ, 0.04f, 3),
                new Vector3(0f, skid, 0f), new Vector3(0f, height - 0.05f, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            cladding.AddExtrusion(RoundedRectLoop(-bodyHalfX - 0.02f, bodyHalfX + 0.02f, -bodyHalfZ - 0.02f, bodyHalfZ + 0.02f, 0.045f, 3),
                new Vector3(0f, height - 0.05f, 0f), new Vector3(0f, height - 0.03f, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            float panelHeight = (stripBottom - skid) / 3f;
            float panelWidth = 2f * (bodyHalfX - 0.03f) / 4f;
            for (int column = 0; column < 4; column++)
            {
                for (int row = 0; row < 3; row++)
                {
                    float x0 = -bodyHalfX + 0.03f + column * panelWidth + 0.004f;
                    float x1 = x0 + panelWidth - 0.008f;
                    float y0 = skid + row * panelHeight + 0.004f;
                    float y1 = y0 + panelHeight - 0.008f;
                    var centre = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, zf + 0.016f);
                    var corners = new[] { new Vector3(x0, y0, zf), new Vector3(x1, y0, zf), new Vector3(x1, y1, zf), new Vector3(x0, y1, zf) };
                    for (int i = 0; i < 4; i++)
                    {
                        var a = corners[i];
                        var b = corners[(i + 1) % 4];
                        var normal = Vector3.Cross(b - a, centre - a);
                        if (normal.z < 0f) normal = -normal;
                        cladding.AddFlatTriangle(a, b, centre, normal);
                    }
                }
            }
            foreach (float y in new[] { skid + panelHeight, skid + 2f * panelHeight, stripBottom })
            {
                cladding.AddBox(new Vector3(-bodyHalfX - 0.004f, y - 0.012f, -bodyHalfZ - 0.004f), new Vector3(bodyHalfX + 0.004f, y + 0.012f, bodyHalfZ + 0.004f), false, false);
            }
            cladding.Attach("Cladding", root, SurfaceMaterial.AluminiumClad);

            // Control strip, everything centred on it vertically (y = 1.725): sticker, control panel in the middle, tag.
            const float stripCentre = (stripBottom + height - 0.05f) / 2f;
            var plastic = new MeshBuilder();
            var screen = new MeshBuilder();
            var green = new MeshBuilder();
            var red = new MeshBuilder();
            var amber = new MeshBuilder();
            var grey = new MeshBuilder();
            plastic.AddExtrusion(RoundedRectLoop(-0.27f, 0.27f, stripCentre - 0.13f, stripCentre + 0.13f, 0.018f, 3),
                new Vector3(0f, 0f, zf), new Vector3(0f, 0f, zf + 0.03f), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            screen.AddExtrusion(RoundedRectLoop(-0.245f, -0.045f, stripCentre + 0.01f, stripCentre + 0.105f, 0.006f, 2),
                new Vector3(0f, 0f, zf + 0.03f), new Vector3(0f, 0f, zf + 0.034f), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            void Button(MeshBuilder b, float x, float y, float radius, float lift)
            {
                b.AddLathe(new[] { P(radius, 0f, true), P(radius, lift * 0.7f, true), P(radius * 0.8f, lift, true), P(0.0001f, lift, true) }, 24, true,
                    new Vector3(x, y, zf + 0.03f), toZ);
            }
            for (int i = 0; i < 4; i++)
            {
                Button(grey, -0.225f + i * 0.055f, stripCentre - 0.07f, 0.017f, 0.008f);
            }
            Button(green, 0.045f, stripCentre + 0.07f, 0.028f, 0.012f);
            Button(red, 0.125f, stripCentre + 0.07f, 0.028f, 0.012f);
            Button(amber, 0.205f, stripCentre + 0.07f, 0.02f, 0.01f);
            Button(amber, 0.0f - 0.0f + 0.045f, stripCentre - 0.055f, 0.012f, 0.008f);
            Button(amber, 0.205f, stripCentre - 0.055f, 0.012f, 0.008f);
            // Emergency stop: a yellow collar with a red mushroom head.
            Button(amber, 0.125f, stripCentre - 0.04f, 0.045f, 0.008f);
            red.AddLathe(new[] { P(0.0001f, 0.008f, true), P(0.032f, 0.008f, true), P(0.034f, 0.018f), P(0.026f, 0.03f), P(0.0001f, 0.032f, true) }, 24, true,
                new Vector3(0.125f, stripCentre - 0.04f, zf + 0.03f), toZ);
            plastic.Attach("ControlPanel", root, SurfaceMaterial.CabinetGrey);
            screen.Attach("Display", root, SurfaceMaterial.Screen);
            green.Attach("ButtonStart", root, SurfaceMaterial.SafetyGreen);
            red.Attach("ButtonStop", root, SurfaceMaterial.SafetyRed);
            amber.Attach("ButtonsAmber", root, SurfaceMaterial.SafetyYellow);
            grey.Attach("ButtonsGrey", root, SurfaceMaterial.KeyboardKey);
            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(-sign * 0.64f, stripCentre, zf), Vector3.left, Vector3.up, 0.32f, 0.16f, PlantLabel.Warning);
            AddLabel(labels, new Vector3(sign * 0.64f, stripCentre, zf), Vector3.left, Vector3.up, 0.40f, 0.20f, PlantLabel.ExchangerTag);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);

            // Four insulated pipes leave the back (no gauges): secondary return (high port) and flow (low port) rise to the wall lanes,
            // the primary pair to the ceiling. Secondary at -sign, primary at +sign.
            float rise = -bodyHalfZ - 0.12f;
            var ports = new[]
            {
                (-0.75f * sign, 1.40f, 2.40f, "SecondaryReturn"), (-0.45f * sign, 0.55f, 2.62f, "SecondaryFlow"),
                (0.45f * sign, 0.55f, ceiling, "PrimaryFlow"), (0.75f * sign, 1.40f, ceiling, "PrimaryReturn"),
            };
            foreach (var (x, y, top, name) in ports)
            {
                RoomPipes.Pipe(root, name, new[] { new Vector3(x, y, -bodyHalfZ + 0.02f), new Vector3(x, y, rise), new Vector3(x, top, rise) }, RoomPipes.WideRadius, SurfaceMaterial.PipeWater);
                RoomPipes.BallValve(root, $"{name}_Valve", new Vector3(x, y + 0.45f, rise), Vector3.up, Vector3.forward, RoomPipes.WideRadius);
            }
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(length, height, depth));
        }
    }
}
