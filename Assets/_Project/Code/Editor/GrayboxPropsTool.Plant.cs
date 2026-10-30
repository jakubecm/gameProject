using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Plant of the cooling plant (HVAC room), modelled on product and plant-room photos: the dial thermometer, the panel of
    /// thermometers, the aluminium-clad plate heat exchanger, the base-mounted circulation pump with a green motor and the
    /// insulated buffer tank. The dials are only a look: no logic, the needle just stands at a fixed reading.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>The meshes of any number of dial thermometers (one mesh per material); each dial faces +Z.</summary>
        private sealed class DialMeshes
        {
            private readonly GrayboxMeshBuilder chrome = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder face = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder glass = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder needle = new GrayboxMeshBuilder();
            private readonly GrayboxMeshBuilder brass = new GrayboxMeshBuilder();
            private readonly SurfaceMaterial faceMaterial;

            public DialMeshes(SurfaceMaterial faceMaterial = SurfaceMaterial.DialFace) => this.faceMaterial = faceMaterial;

            /// <summary>
            /// A bimetal dial thermometer (0 to 120 degrees C, photo): chrome rolled bezel, white face with the scale texture, a flat
            /// glass, a black needle with a paddle tail on a round hub, and a brass stem with a hex collar running back for
            /// <paramref name="stemLength"/>. <paramref name="origin"/> is the middle of the back of the case, <paramref name="radius"/>
            /// the outer radius, <paramref name="reading"/> between 0 and 1 the needle position.
            /// </summary>
            public void AddDial(Vector3 origin, float radius, float stemLength, float reading, bool needleMesh = true)
            {
                float r = radius;
                var toZ = Quaternion.Euler(90f, 0f, 0f);
                chrome.AddLathe(new[]
                {
                    P(0.98f * r, 0f, true), P(1.0f * r, 0.05f * r), P(1.0f * r, 0.30f * r), P(0.96f * r, 0.37f * r), P(0.90f * r, 0.38f * r),
                    P(0.87f * r, 0.35f * r, true), P(0.87f * r, 0.20f * r, true),
                }, 48, true, origin, toZ);
                chrome.AddDisc(origin, Vector3.back, 0.98f * r, 48);
                // Seen from +Z the viewer's right is -X, so the texture's u runs along -X.
                face.AddDiscUv(origin + Vector3.forward * 0.20f * r, Vector3.left, Vector3.up, Vector3.forward, 0.87f * r, 48);
                glass.AddDisc(origin + Vector3.forward * 0.36f * r, Vector3.forward, 0.88f * r, 48);

                if (needleMesh)
                {
                    float angle = Mathf.Lerp(-135f, 135f, reading) * Mathf.Deg2Rad;
                    var pointer = Turned(new[] { new Vector2(-0.03f * r, -0.02f * r), new Vector2(0.03f * r, -0.02f * r), new Vector2(0.008f * r, 0.74f * r), new Vector2(-0.008f * r, 0.74f * r) }, angle);
                    var tail = Turned(new[] { new Vector2(-0.03f * r, 0f), new Vector2(-0.055f * r, -0.24f * r), new Vector2(0.055f * r, -0.24f * r), new Vector2(0.03f * r, 0f) }, angle);
                    var from = origin + Vector3.forward * 0.22f * r;
                    var to = origin + Vector3.forward * 0.235f * r;
                    needle.AddExtrusion(pointer, from, to, Vector3.left, Vector3.up, Vector3.back, Vector3.forward);
                    needle.AddExtrusion(tail, from, to, Vector3.left, Vector3.up, Vector3.back, Vector3.forward);
                }
                needle.AddLathe(new[] { P(0.08f * r, 0.22f * r, true), P(0.08f * r, 0.25f * r), P(0.05f * r, 0.275f * r), P(0.0001f, 0.28f * r, true) }, 20, true, origin, toZ);

                brass.AddLathe(new[] { P(0.12f * r, -stemLength, true), P(0.12f * r, 0f, true) }, 16, true, origin, toZ);
                brass.AddLathe(new[] { P(0.19f * r, -0.30f * r, true), P(0.19f * r, -0.15f * r, true), P(0.10f * r, -0.15f * r, true) }, 6, false, origin, toZ);
            }

            public void Attach(string prefix, Transform parent)
            {
                chrome.Attach($"{prefix}_Chrome", parent, SurfaceMaterial.Chrome);
                face.Attach($"{prefix}_Face", parent, faceMaterial);
                glass.Attach($"{prefix}_Glass", parent, SurfaceMaterial.DomeClear);
                needle.Attach($"{prefix}_Needle", parent, SurfaceMaterial.BlackHousing);
                brass.Attach($"{prefix}_Stem", parent, SurfaceMaterial.Brass);
            }

            /// <summary>Turns a convex outline clockwise (seen from the front) by <paramref name="angle"/> radians.</summary>
            private static List<Vector2> Turned(IList<Vector2> outline, float angle)
            {
                var result = new List<Vector2>();
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                foreach (var point in outline) result.Add(new Vector2(point.x * cos + point.y * sin, -point.x * sin + point.y * cos));
                return result;
            }
        }

        /// <summary>Dial thermometer on a short brass stem; the pivot is where the stem meets the pipe, the dial faces +Z.</summary>
        private static void BuildPipeGauge(Transform root)
        {
            var dial = new DialMeshes();
            dial.AddDial(new Vector3(0f, 0f, 0.07f), 0.045f, 0.07f, 0.3f);
            dial.Attach("Dial", root);
        }

        /// <summary>
        /// The thermometer wall of the cooling plant, opposite the door: six narrow pipes rise side by side from the floor, each with
        /// a large dial thermometer (three times life size, so the reading can be read in the game) on a brass stem. They bend under
        /// nested lanes, three left and three right, run along the back wall and turn into the end faces of the two heat exchangers
        /// (the exchangers' centres stand at x = +-2.33 m, their end faces at z = -0.55 m): every pipe ends in an exchanger. The needles
        /// are only a look. A wall prop.
        /// </summary>
        private static void BuildCoolingMeterPanel(Transform root)
        {
            float r = RoomPipes.NarrowRadius;
            const float gap = 0.04f;
            float z = r + gap;
            const float dialRadius = 0.15f;
            const float dialHeight = 1.15f;
            const float exchangerEnd = -0.57f;
            float[] x = { -0.95f, -0.57f, -0.19f, 0.19f, 0.57f, 0.95f };
            float[] lane = { 1.50f, 1.65f, 1.80f, 1.80f, 1.65f, 1.50f };
            float[] into = { -2.55f, -2.35f, -2.15f, 2.15f, 2.35f, 2.55f };
            float[] readings = { 0.26f, 0.31f, 0.28f, 0.37f, 0.33f, 0.42f };
            var dials = new DialMeshes();
            var strap = new GrayboxMeshBuilder();
            var screws = new GrayboxMeshBuilder();
            var wall = Vector3.forward;
            for (int i = 0; i < x.Length; i++)
            {
                RoomPipes.Pipe(root, $"Pipe_{i}", new[]
                {
                    new Vector3(x[i], 0f, z), new Vector3(x[i], lane[i], z), new Vector3(into[i], lane[i], z), new Vector3(into[i], lane[i], exchangerEnd),
                }, r, SurfaceMaterial.PipeWater);
                RoomInfrastructure.AddRosette(root, $"Rosette_{i}", new Vector3(x[i], 0f, z), r, false);
                RoomPipes.Clamp(strap, screws, new Vector3(x[i], 0.45f, z), Vector3.up, wall, r, gap);
                RoomPipes.Clamp(strap, screws, new Vector3(x[i], 1.42f, z), Vector3.up, wall, r, gap);
                float direction = Mathf.Sign(into[i] - x[i]);
                for (float at = x[i] + direction * 0.7f; Mathf.Abs(at - into[i]) > 0.35f; at += direction * 0.8f)
                {
                    RoomPipes.Clamp(strap, screws, new Vector3(at, lane[i], z), Vector3.right, wall, r, gap);
                }
                GrayboxUtility.CreateGroup($"Thermometer_{i}", root).localPosition = new Vector3(x[i], dialHeight, z + r + 0.07f);
                dials.AddDial(new Vector3(x[i], dialHeight, z + r + 0.07f), dialRadius, 0.07f, readings[i]);
            }
            strap.Attach("Clamps", root, SurfaceMaterial.PipeClamp);
            screws.Attach("Screws", root, SurfaceMaterial.Chrome);
            dials.Attach("Dials", root);
        }

        private static void BuildPlateHeatExchanger(Transform root) => BuildExchanger(root, 1f);

        /// <summary>The same exchanger with its connections at the other end, for the exchanger that stands on the other side wall.</summary>
        private static void BuildPlateHeatExchangerMirrored(Transform root) => BuildExchanger(root, -1f);

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
            var blue = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
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

            var cladding = new GrayboxMeshBuilder();
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
            var plastic = new GrayboxMeshBuilder();
            var screen = new GrayboxMeshBuilder();
            var green = new GrayboxMeshBuilder();
            var red = new GrayboxMeshBuilder();
            var amber = new GrayboxMeshBuilder();
            var grey = new GrayboxMeshBuilder();
            plastic.AddExtrusion(RoundedRectLoop(-0.27f, 0.27f, stripCentre - 0.13f, stripCentre + 0.13f, 0.018f, 3),
                new Vector3(0f, 0f, zf), new Vector3(0f, 0f, zf + 0.03f), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            screen.AddExtrusion(RoundedRectLoop(-0.245f, -0.045f, stripCentre + 0.01f, stripCentre + 0.105f, 0.006f, 2),
                new Vector3(0f, 0f, zf + 0.03f), new Vector3(0f, 0f, zf + 0.034f), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            void Button(GrayboxMeshBuilder b, float x, float y, float radius, float lift)
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
            var labels = new GrayboxMeshBuilder();
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

        /// <summary>
        /// Base-mounted end-suction circulation pump 1.0 x 0.7 x 0.5 m (photo): a cast-iron volute casing with suction and discharge
        /// flanges, a ribbed lantern, and a blue three-phase motor with a ribbed front end shield, cooling fins, a nameplate on a pad,
        /// a terminal box with a cable gland and a fan cover, on tapered cast feet; the blue skid is made of channels with anchor lugs
        /// and a pump plate. The discharge rises from the top of the casing to 0.7 m (the room carries it on), the suction flange faces
        /// -X for the room's pipe. The pivot is the discharge axis, the motor extends towards +X; terminal box and nameplate face +Z.
        /// </summary>
        private static void BuildCirculationPump(Transform root)
        {
            const float axisHeight = 0.34f;
            const float baseTop = 0.10f;
            var toX = Quaternion.Euler(0f, 0f, -90f);
            var toMinusX = Quaternion.Euler(0f, 0f, 90f);
            var toZ = Quaternion.Euler(90f, 0f, 0f);
            var axis = new Vector3(0f, axisHeight, 0f);

            // Skid: two C-channels along X, cross channels, anchor lugs with bolts.
            var blue = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            foreach (int sz in new[] { -1, 1 })
            {
                float outer = sz * 0.235f;
                float inner = outer - sz * 0.07f;
                float web = outer - sz * 0.012f;
                blue.AddBox(new Vector3(-0.38f, 0f, Mathf.Min(web, outer)), new Vector3(0.65f, baseTop, Mathf.Max(web, outer)));
                blue.AddBox(new Vector3(-0.38f, 0f, Mathf.Min(inner, outer)), new Vector3(0.65f, 0.014f, Mathf.Max(inner, outer)));
                blue.AddBox(new Vector3(-0.38f, baseTop - 0.014f, Mathf.Min(inner, outer)), new Vector3(0.65f, baseTop, Mathf.Max(inner, outer)));
            }
            foreach (float x in new[] { -0.34f, 0.0f, 0.30f, 0.60f })
            {
                blue.AddBox(new Vector3(x - 0.035f, 0.014f, -0.225f), new Vector3(x + 0.035f, baseTop - 0.014f, 0.225f));
            }
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    var lug = new Vector3(sx > 0 ? 0.65f + 0.035f : -0.38f - 0.035f, 0f, sz * 0.2f);
                    blue.AddBox(lug + new Vector3(-0.035f, 0f, -0.035f), lug + new Vector3(0.035f, 0.012f, 0.035f));
                    chrome.AddLathe(new[] { P(0.012f, 0f, true), P(0.012f, 0.008f, true), P(0.0001f, 0.008f, true) }, 6, false, lug + new Vector3(0f, 0.012f, 0f), Quaternion.identity);
                }
            }
            var iron = new GrayboxMeshBuilder();

            // Cast-iron volute casing (domed cover to -X), the suction nozzle with its flange and bolts, the discharge nozzle on top.
            iron.AddLathe(new[]
            {
                P(0.15f, -0.10f, true), P(0.205f, -0.09f), P(0.215f, -0.03f), P(0.215f, 0.06f), P(0.20f, 0.12f), P(0.14f, 0.16f), P(0.07f, 0.175f), P(0.0001f, 0.18f, true),
            }, 56, true, axis, toMinusX);
            iron.AddLathe(new[] { P(0.05f, 0.16f, true), P(0.05f, 0.26f, true), P(0.085f, 0.26f, true), P(0.085f, 0.285f, true), P(RoomPipes.WideRadius, 0.285f, true) }, 40, true, axis, toMinusX);
            iron.AddLathe(new[] { P(0.05f, 0.14f, true), P(0.05f, 0.27f, true), P(0.085f, 0.27f, true), P(0.085f, 0.295f, true), P(RoomPipes.WideRadius, 0.295f, true) },
                40, true, axis, Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                chrome.AddLathe(new[] { P(0.0105f, 0f, true), P(0.0105f, 0.010f, true), P(0.007f, 0.013f, true), P(0.0001f, 0.013f, true) }, 6, false,
                    axis + new Vector3(-0.285f, Mathf.Cos(angle) * 0.065f, Mathf.Sin(angle) * 0.065f), toMinusX);
                chrome.AddLathe(new[] { P(0.0105f, 0f, true), P(0.0105f, 0.010f, true), P(0.007f, 0.013f, true), P(0.0001f, 0.013f, true) }, 6, false,
                    axis + new Vector3(Mathf.Cos(angle) * 0.065f, 0.295f, Mathf.Sin(angle) * 0.065f), Quaternion.identity);
            }
            // The casing sits on a cast foot cradle on the skid.
            iron.AddExtrusion(RoundedRectLoop(-0.12f, 0.12f, baseTop, baseTop + 0.07f, 0.01f, 2), new Vector3(0f, 0f, -0.11f), new Vector3(0f, 0f, 0.11f),
                Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            var post = new GrayboxMeshBuilder();
            post.AddBox(new Vector3(0.10f, 0.11f, -0.016f), new Vector3(0.20f, 0.15f, 0.016f));
            for (int i = 0; i < 4; i++)
            {
                iron.Append(post, axis, Quaternion.Euler(45f + 90f * i, 0f, 0f));
            }
            iron.AddLathe(new[] { P(0.10f, 0.095f, true), P(0.16f, 0.095f, true), P(0.16f, 0.105f, true), P(0.10f, 0.105f, true) }, 40, true, axis, toX);

            // Motor: front end shield with a cone and bolts, a finned frame, a fan cover; tapered cast feet; terminal box; nameplate pad.
            var motor = new GrayboxMeshBuilder();
            motor.AddLathe(new[]
            {
                P(0.12f, 0.20f, true), P(0.17f, 0.20f, true), P(0.17f, 0.225f, true), P(0.15f, 0.24f), P(0.138f, 0.265f, true), P(0.138f, 0.555f, true),
                P(0.152f, 0.56f), P(0.158f, 0.585f), P(0.15f, 0.625f), P(0.11f, 0.65f), P(0.0001f, 0.655f, true),
            }, 56, true, axis, toX);
            for (int i = 0; i < 6; i++)
            {
                var rib = new GrayboxMeshBuilder();
                rib.AddBox(new Vector3(0.205f, 0.16f, -0.006f), new Vector3(0.24f, 0.172f, 0.006f));
                motor.Append(rib, axis, Quaternion.Euler(i * 60f + 30f, 0f, 0f));
            }
            var fin = new GrayboxMeshBuilder();
            fin.AddBox(new Vector3(0.275f, 0.13f, -0.005f), new Vector3(0.525f, 0.168f, 0.005f));
            for (int i = 0; i < 24; i++)
            {
                float angle = i * 15f;
                if (angle < 40f || angle > 320f || (angle > 70f && angle < 110f) || (angle > 250f && angle < 290f)) continue; // keep the top (terminal box) and both sides (nameplates) free
                motor.Append(fin, axis, Quaternion.Euler(angle, 0f, 0f));
            }
            foreach (float x in new[] { 0.27f, 0.43f })
            {
                motor.AddExtrusion(new[] { new Vector2(x, baseTop + 0.016f), new Vector2(x + 0.13f, baseTop + 0.016f), new Vector2(x + 0.115f, axisHeight - 0.085f), new Vector2(x + 0.015f, axisHeight - 0.085f) },
                    new Vector3(0f, 0f, -0.095f), new Vector3(0f, 0f, 0.095f), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                motor.AddBox(new Vector3(x - 0.012f, baseTop, -0.125f), new Vector3(x + 0.142f, baseTop + 0.016f, 0.125f));
                foreach (int sz in new[] { -1, 1 })
                {
                    chrome.AddLathe(new[] { P(0.011f, 0f, true), P(0.011f, 0.008f, true), P(0.0001f, 0.008f, true) }, 6, false, new Vector3(x + 0.065f, baseTop + 0.016f, sz * 0.108f), Quaternion.identity);
                }
            }
            motor.AddBox(new Vector3(0.31f, axisHeight + 0.10f, -0.075f), new Vector3(0.46f, axisHeight + 0.19f, 0.075f));
            motor.AddBox(new Vector3(0.295f, axisHeight + 0.19f, -0.087f), new Vector3(0.475f, axisHeight + 0.20f, 0.087f));
            motor.AddBox(new Vector3(0.31f, axisHeight - 0.055f, 0.13f), new Vector3(0.51f, axisHeight + 0.055f, 0.172f));
            motor.AddBox(new Vector3(0.31f, axisHeight - 0.055f, -0.172f), new Vector3(0.51f, axisHeight + 0.055f, -0.13f));
            motor.Attach("Motor", root, SurfaceMaterial.SupportBlue);
            var black = new GrayboxMeshBuilder();
            black.AddLathe(new[] { P(0.022f, 0f, true), P(0.022f, 0.026f, true), P(0.014f, 0.032f, true), P(0.0001f, 0.032f, true) }, 24, true,
                new Vector3(0.385f, axisHeight + 0.15f, 0.075f), toZ);
            black.AddLathe(new[] { P(0.011f, 0.032f, true), P(0.011f, 0.06f, true), P(0.0001f, 0.06f, true) }, 16, true, new Vector3(0.385f, axisHeight + 0.15f, 0.075f), toZ);
            foreach (var corner in new[] { new Vector2(0.30f, -0.077f), new Vector2(0.30f, 0.077f), new Vector2(0.47f, -0.077f), new Vector2(0.47f, 0.077f) })
            {
                chrome.AddLathe(new[] { P(0.007f, 0f, true), P(0.007f, 0.004f), P(0.005f, 0.006f, true), P(0.0001f, 0.006f, true) }, 10, true,
                    new Vector3(corner.x, axisHeight + 0.20f, corner.y), Quaternion.identity);
            }
            black.Attach("Gland", root, SurfaceMaterial.BlackHousing);
            blue.Attach("Skid", root, SurfaceMaterial.SupportBlue);
            iron.Attach("Casing", root, SurfaceMaterial.CastIron);
            chrome.Attach("Bolts", root, SurfaceMaterial.Chrome);

            var stub = new GrayboxMeshBuilder();
            stub.AddLathe(new[] { P(RoomPipes.WideRadius, axisHeight + 0.295f, true), P(RoomPipes.WideRadius, 0.70f, true) }, 24, true, Vector3.zero, Quaternion.identity);
            stub.Attach("Discharge", root, SurfaceMaterial.AluminiumClad);

            var labels = new GrayboxMeshBuilder();
            // Nameplates on both sides, upright for someone standing at either side (+Z and -Z).
            AddLabel(labels, new Vector3(0.41f, axisHeight, 0.172f), Vector3.left, Vector3.up, 0.17f, 0.085f, PlantLabel.MotorPlate);
            AddLabel(labels, new Vector3(0.41f, axisHeight, -0.172f), Vector3.right, Vector3.up, 0.17f, 0.085f, PlantLabel.MotorPlate);
            AddLabel(labels, new Vector3(0.385f, axisHeight + 0.20f, 0f), Vector3.left, Vector3.back, 0.12f, 0.06f, PlantLabel.Warning);
            AddLabel(labels, new Vector3(0.135f, baseTop / 2f, 0.235f), Vector3.left, Vector3.up, 0.20f, 0.075f, PlantLabel.PumpPlate);
            AddLabel(labels, new Vector3(0.135f, baseTop / 2f, -0.235f), Vector3.right, Vector3.up, 0.20f, 0.075f, PlantLabel.PumpPlate);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0.135f, 0.35f, 0f), new Vector3(1.09f, 0.7f, 0.5f));
        }

        /// <summary>
        /// Chilled-water buffer tank 0.7 m across and 2.2 m high on three tubular legs with foot plates, insulated and clad in aluminium with
        /// rolled seams and two bands in the colour of the water, an equipment tag and two stickers centred between the seams. Two
        /// flanged nozzles on top (flow at -X, return at +X, 0.12 m off the axis, at 2.2 m) for the room's wall pipes. No gauge. A floor prop.
        /// </summary>
        private static void BuildBufferTank(Transform root)
        {
            const float radius = 0.35f;
            const float height = 2.2f;
            var blue = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            var leg = new GrayboxMeshBuilder();
            var legChrome = new GrayboxMeshBuilder();
            // Tubular leg close to the axis, where the dished bottom (0.25 m up) is: a plain tube that runs 5 cm up into the shell, a ring half way, a foot plate with four bolts.
            const float legRadial = 0.14f;
            leg.AddTube(new[] { new Vector3(0f, 0.014f, legRadial), new Vector3(0f, 0.30f, legRadial) }, 0.032f, 20, Vector3.zero, Quaternion.identity);
            leg.AddDisc(new Vector3(0f, 0.30f, legRadial), Vector3.up, 0.032f, 20);
            leg.AddLathe(new[] { P(0.032f, 0.12f, true), P(0.040f, 0.12f, true), P(0.040f, 0.135f, true), P(0.032f, 0.135f, true) }, 20, true, new Vector3(0f, 0f, legRadial), Quaternion.identity);
            leg.AddLathe(new[] { P(0.0001f, 0f, true), P(0.08f, 0f, true), P(0.08f, 0.014f, true), P(0.0001f, 0.014f, true) }, 24, true, new Vector3(0f, 0f, legRadial), Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                legChrome.AddLathe(new[] { P(0.009f, 0.014f, true), P(0.009f, 0.022f, true), P(0.0001f, 0.022f, true) }, 6, false,
                    new Vector3(Mathf.Cos(angle) * 0.058f, 0f, legRadial + Mathf.Sin(angle) * 0.058f), Quaternion.identity);
            }
            for (int i = 0; i < 3; i++)
            {
                blue.Append(leg, Vector3.zero, Quaternion.Euler(0f, i * 120f, 0f));
                chrome.Append(legChrome, Vector3.zero, Quaternion.Euler(0f, i * 120f, 0f));
            }
            blue.Attach("Legs", root, SurfaceMaterial.SupportBlue);
            chrome.Attach("LegBolts", root, SurfaceMaterial.Chrome);

            var shell = new GrayboxMeshBuilder();
            shell.AddLathe(new[]
            {
                P(0.0001f, 0.26f, true), P(0.24f, 0.245f), P(0.32f, 0.285f), P(radius, 0.36f, true), P(radius, height - 0.25f, true),
                P(0.335f, height - 0.1f), P(0.26f, height - 0.02f), P(0.13f, height, true), P(0.0001f, height, true),
            }, 64, true, Vector3.zero, Quaternion.identity);
            foreach (float y in new[] { 0.7f, 1.15f, 1.6f })
            {
                shell.AddLathe(new[] { P(radius, y, true), P(radius + 0.006f, y + 0.004f), P(radius + 0.006f, y + 0.014f), P(radius, y + 0.018f, true) }, 64, true, Vector3.zero, Quaternion.identity);
            }
            shell.Attach("Shell", root, SurfaceMaterial.AluminiumClad);
            var bands = new GrayboxMeshBuilder();
            foreach (float y in new[] { 0.45f, 1.95f })
            {
                bands.AddLathe(new[] { P(radius, y, true), P(radius + 0.012f, y, true), P(radius + 0.012f, y + 0.07f, true), P(radius, y + 0.07f, true) }, 64, true, Vector3.zero, Quaternion.identity);
            }
            bands.Attach("Bands", root, SurfaceMaterial.PipeWater);

            // The nozzles on top: a flange with bolts each (the wall pipes start on them).
            var nozzles = new GrayboxMeshBuilder();
            var nozzleBolts = new GrayboxMeshBuilder();
            foreach (float x in new[] { -0.12f, 0.12f })
            {
                nozzles.AddLathe(new[] { P(RoomPipes.WideRadius + 0.012f, height - 0.02f, true), P(RoomPipes.WideRadius + 0.012f, height + 0.012f, true), P(0.058f, height + 0.012f, true),
                    P(0.058f, height + 0.03f, true), P(RoomPipes.WideRadius, height + 0.03f, true) }, 32, true, new Vector3(x, 0f, 0f), Quaternion.identity);
                for (int i = 0; i < 4; i++)
                {
                    float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                    nozzleBolts.AddLathe(new[] { P(0.008f, 0f, true), P(0.008f, 0.008f, true), P(0.0001f, 0.008f, true) }, 6, false,
                        new Vector3(x + Mathf.Cos(angle) * 0.045f, height + 0.03f, Mathf.Sin(angle) * 0.045f), Quaternion.identity);
                }
            }
            nozzles.Attach("Nozzles", root, SurfaceMaterial.AluminiumClad);
            nozzleBolts.Attach("NozzleBolts", root, SurfaceMaterial.Chrome);

            var labels = new GrayboxMeshBuilder();
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Vector3.forward, radius, 1.375f, 0.19f, 0.36f, false, PlantLabel.ExchangerTag);
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Quaternion.Euler(0f, 40f, 0f) * Vector3.forward, radius, 0.925f, 0.12f, 0.24f, false, PlantLabel.Warning);
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Quaternion.Euler(0f, -40f, 0f) * Vector3.forward, radius, 0.925f, 0.12f, 0.24f, false, PlantLabel.PipeLabel);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, (height + 0.05f) / 2f, 0f), new Vector3(2f * radius, height + 0.05f, 2f * radius));
        }

        /// <summary>
        /// Diaphragm expansion vessel 0.44 m across and 1.1 m high (photo): a regular cylinder with semicircular ends, white, on three
        /// tubular legs with foot plates, a blue cap on the air valve on top, a yellow pressure sticker centred on the cylinder, and a
        /// flanged nozzle on the side facing -Z (the wall) at 0.5 m for the room's pipe. A floor prop.
        /// </summary>
        private static void BuildExpansionVessel(Transform root)
        {
            const float radius = 0.22f;
            const float legs = 0.22f;
            const float straight = 0.30f;
            float bottomPole = legs;
            float lowerCentre = bottomPole + radius;
            float upperCentre = lowerCentre + straight;
            float top = upperCentre + radius;
            // The legs stand close to the vessel's axis, where the dished bottom is low, and run 5 cm up into the shell.
            const float legRadial = 0.12f;

            var blue = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            var leg = new GrayboxMeshBuilder();
            var legChrome = new GrayboxMeshBuilder();
            // The leg runs up into the dished bottom: the hemisphere is at 0.30 m above the leg's axis and rises to 0.333 m on its outer
            // side, so the top end (0.345 m) is buried inside the shell and the leg meets the vessel without a gap.
            float legTop = lowerCentre - Mathf.Sqrt(radius * radius - (legRadial + 0.022f) * (legRadial + 0.022f)) + 0.05f;
            leg.AddTube(new[] { new Vector3(0f, 0.012f, legRadial), new Vector3(0f, legTop, legRadial) }, 0.022f, 16, Vector3.zero, Quaternion.identity);
            leg.AddDisc(new Vector3(0f, legTop, legRadial), Vector3.up, 0.022f, 16);
            leg.AddLathe(new[] { P(0.0001f, 0f, true), P(0.06f, 0f, true), P(0.06f, 0.012f, true), P(0.0001f, 0.012f, true) }, 20, true, new Vector3(0f, 0f, legRadial), Quaternion.identity);
            leg.AddLathe(new[] { P(0.022f, 0.09f, true), P(0.03f, 0.09f, true), P(0.03f, 0.1f, true), P(0.022f, 0.1f, true) }, 16, true, new Vector3(0f, 0f, legRadial), Quaternion.identity);
            for (int i = 0; i < 4; i++)
            {
                float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                legChrome.AddLathe(new[] { P(0.007f, 0.012f, true), P(0.007f, 0.019f, true), P(0.0001f, 0.019f, true) }, 6, false,
                    new Vector3(Mathf.Cos(angle) * 0.043f, 0f, legRadial + Mathf.Sin(angle) * 0.043f), Quaternion.identity);
            }
            for (int i = 0; i < 3; i++)
            {
                blue.Append(leg, Vector3.zero, Quaternion.Euler(0f, i * 120f, 0f));
                chrome.Append(legChrome, Vector3.zero, Quaternion.Euler(0f, i * 120f, 0f));
            }
            blue.AddLathe(new[] { P(0.012f, top - 0.005f, true), P(0.026f, top - 0.005f, true), P(0.026f, top + 0.035f), P(0.018f, top + 0.05f), P(0.0001f, top + 0.052f, true) }, 20, true, Vector3.zero, Quaternion.identity);
            blue.Attach("LegsAndCap", root, SurfaceMaterial.SupportBlue);
            chrome.AddLathe(new[] { P(0.03f, top - 0.02f, true), P(0.03f, top - 0.005f, true), P(0.012f, top - 0.005f, true) }, 6, false, Vector3.zero, Quaternion.identity);
            chrome.Attach("LegBolts", root, SurfaceMaterial.Chrome);

            // Shell: semicircle at the bottom, a straight cylinder, a semicircle at the top.
            var profile = new List<ProfilePoint>();
            for (int i = 0; i <= 16; i++)
            {
                float angle = Mathf.Lerp(-90f, 0f, i / 16f) * Mathf.Deg2Rad;
                profile.Add(P(Mathf.Max(0.0001f, radius * Mathf.Cos(angle)), lowerCentre + radius * Mathf.Sin(angle), i == 0));
            }
            for (int i = 0; i <= 16; i++)
            {
                float angle = Mathf.Lerp(0f, 90f, i / 16f) * Mathf.Deg2Rad;
                profile.Add(P(Mathf.Max(0.0001f, radius * Mathf.Cos(angle)), upperCentre + radius * Mathf.Sin(angle), i == 16));
            }
            var shell = new GrayboxMeshBuilder();
            shell.AddLathe(profile, 64, true, Vector3.zero, Quaternion.identity);
            shell.Attach("Shell", root, SurfaceMaterial.DetectorWhite);

            // The nozzle: a short stub with a flange, facing the wall (-Z) at 0.5 m.
            var nozzle = new GrayboxMeshBuilder();
            nozzle.AddLathe(new[] { P(0.030f, 0f, true), P(0.030f, 0.05f, true), P(0.05f, 0.05f, true), P(0.05f, 0.068f, true), P(RoomPipes.WideRadius, 0.068f, true) }, 24, true,
                new Vector3(0f, 0.5f, -radius + 0.01f), Quaternion.Euler(-90f, 0f, 0f));
            nozzle.Attach("Nozzle", root, SurfaceMaterial.AluminiumClad);
            var labels = new GrayboxMeshBuilder();
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Vector3.forward, radius, (bottomPole + top) / 2f, 0.14f, 0.28f, false, PlantLabel.VesselLabel);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, (top + 0.05f) / 2f, 0f), new Vector3(2f * radius, top + 0.05f, 2f * radius));
        }

        /// <summary>
        /// Air handling unit (CRAH, fan coil) 0.7 x 0.6 m and 1.9 m high with a supply duct of 0.5 x 0.3 m rising to the ceiling, on a dark
        /// plinth: grey sheet-metal body with rounded corners, an air inlet grille with slots, a service hatch with a lever handle, a blue
        /// stripe, a unit tag and a warning sticker centred on the front; the duct has flanged joints and a damper with a quadrant lever.
        /// Two flanged water connections on top at the back (flow at -X, return at +X, 0.2 m off the axis, z = -0.23 m, 1.9 m high) for the
        /// room's wall pipes. The service side faces +Z.
        /// </summary>
        /// <summary>A slab on a front face (+Z) with 45 degree chamfered edges: its base on the face at <paramref name="z"/>, <paramref name="depth"/> proud, the top face inset by <paramref name="inset"/> on every side.</summary>
        private static void AddChamferedSlab(GrayboxMeshBuilder b, float x0, float x1, float y0, float y1, float z, float depth, float inset)
        {
            var a0 = new Vector3(x0, y0, z);
            var a1 = new Vector3(x1, y0, z);
            var a2 = new Vector3(x1, y1, z);
            var a3 = new Vector3(x0, y1, z);
            float zt = z + depth;
            var t0 = new Vector3(x0 + inset, y0 + inset, zt);
            var t1 = new Vector3(x1 - inset, y0 + inset, zt);
            var t2 = new Vector3(x1 - inset, y1 - inset, zt);
            var t3 = new Vector3(x0 + inset, y1 - inset, zt);
            var uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            b.AddPanel(t0, t1, t2, t3, Vector3.forward, uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a0, a1, t1, t0, new Vector3(0f, -1f, 1f), uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a1, a2, t2, t1, new Vector3(1f, 0f, 1f), uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a2, a3, t3, t2, new Vector3(0f, 1f, 1f), uv[0], uv[1], uv[2], uv[3]);
            b.AddPanel(a3, a0, t0, t3, new Vector3(-1f, 0f, 1f), uv[0], uv[1], uv[2], uv[3]);
        }

        private static void BuildAirHandlerUnit(Transform root)
        {
            const float halfX = 0.35f;
            const float halfZ = 0.30f;
            const float plinth = 0.08f;
            const float bodyTop = 1.9f;
            float ceiling = RoomContract.CeilingHeight;
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            var body = new GrayboxMeshBuilder();
            body.AddExtrusion(RoundedRectLoop(-halfX, halfX, -halfZ, halfZ, 0.025f, 3), new Vector3(0f, plinth, 0f), new Vector3(0f, bodyTop, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            body.AddExtrusion(RoundedRectLoop(-halfX - 0.012f, halfX + 0.012f, -halfZ - 0.012f, halfZ + 0.012f, 0.03f, 3), new Vector3(0f, bodyTop - 0.02f, 0f), new Vector3(0f, bodyTop + 0.01f, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            body.Attach("Body", root, SurfaceMaterial.CabinetGrey);
            var dark = new GrayboxMeshBuilder();
            dark.AddBox(new Vector3(-halfX + 0.015f, 0f, -halfZ + 0.015f), new Vector3(halfX - 0.015f, plinth, halfZ - 0.015f));
            // Two doors, the upper one (louvered) over the lower one (service hatch), each a slab with 45 degree chamfered edges, touching: the edges show as V grooves, no gap.
            const float doorDepth = 0.010f;
            var doors = new GrayboxMeshBuilder();
            AddChamferedSlab(doors, -0.30f, 0.30f, 0.98f, 1.82f, halfZ, doorDepth, doorDepth);
            AddChamferedSlab(doors, -0.30f, 0.30f, 0.20f, 0.98f, halfZ, doorDepth, doorDepth);
            doors.Attach("Doors", root, SurfaceMaterial.CabinetGrey);
            float front = halfZ + doorDepth + 0.0005f;
            for (int i = 0; i < 12; i++)
            {
                float y = 1.12f + i * 0.055f;
                dark.AddPanel(new Vector3(-0.24f, y, front), new Vector3(0.24f, y, front), new Vector3(0.24f, y + 0.016f, front), new Vector3(-0.24f, y + 0.016f, front), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            dark.Attach("Slots", root, SurfaceMaterial.BlackHousing);
            var metal = new GrayboxMeshBuilder();
            // Lever handle of the hatch: a plate, a stand-off and a bar.
            metal.AddBox(new Vector3(0.16f, 0.57f, halfZ + doorDepth), new Vector3(0.20f, 0.61f, halfZ + doorDepth + 0.014f));
            metal.AddBox(new Vector3(0.17f, 0.53f, halfZ + doorDepth + 0.014f), new Vector3(0.19f, 0.65f, halfZ + doorDepth + 0.03f));
            metal.AddLathe(new[] { P(0.016f, 0f, true), P(0.016f, 0.01f, true), P(0.0001f, 0.01f, true) }, 20, true, new Vector3(0.18f, 0.59f, halfZ + doorDepth + 0.03f), toZ);
            metal.Attach("Handle", root, SurfaceMaterial.Chrome);

            // Water connections on top at the back, flanged, short insulated stubs.
            var nozzles = new GrayboxMeshBuilder();
            var bolts = new GrayboxMeshBuilder();
            foreach (float x in new[] { -0.2f, 0.2f })
            {
                nozzles.AddLathe(new[] { P(RoomPipes.WideRadius, bodyTop, true), P(RoomPipes.WideRadius, bodyTop + 0.07f, true), P(0.058f, bodyTop + 0.07f, true),
                    P(0.058f, bodyTop + 0.088f, true), P(RoomPipes.WideRadius, bodyTop + 0.088f, true) }, 32, true, new Vector3(x, 0f, -0.23f), Quaternion.identity);
                for (int i = 0; i < 4; i++)
                {
                    float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                    bolts.AddLathe(new[] { P(0.008f, 0f, true), P(0.008f, 0.008f, true), P(0.0001f, 0.008f, true) }, 6, false,
                        new Vector3(x + Mathf.Cos(angle) * 0.045f, bodyTop + 0.088f, -0.23f + Mathf.Sin(angle) * 0.045f), Quaternion.identity);
                }
            }
            nozzles.Attach("WaterNozzles", root, SurfaceMaterial.AluminiumClad);
            bolts.Attach("NozzleBolts", root, SurfaceMaterial.Chrome);

            // Supply duct to the ceiling with flanged joints and a damper.
            var duct = new GrayboxMeshBuilder();
            var ductLoop = RoundedRectLoop(-0.25f, 0.25f, -0.15f, 0.15f, 0.012f, 2);
            duct.AddExtrusion(ductLoop, new Vector3(0f, bodyTop + 0.01f, 0f), new Vector3(0f, ceiling, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            foreach (float y in new[] { 2.30f, 2.70f })
            {
                duct.AddExtrusion(RoundedRectLoop(-0.275f, 0.275f, -0.175f, 0.175f, 0.014f, 2), new Vector3(0f, y, 0f), new Vector3(0f, y + 0.035f, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            }
            duct.Attach("Duct", root, SurfaceMaterial.AluminiumClad);
            var damper = new GrayboxMeshBuilder();
            damper.AddLathe(new[] { P(0.05f, 0f, true), P(0.05f, 0.006f, true), P(0.0001f, 0.006f, true) }, 28, true, new Vector3(0f, 2.05f, 0.15f), toZ);
            damper.AddBox(new Vector3(-0.012f, 2.05f - 0.01f, 0.156f), new Vector3(0.012f, 2.05f + 0.085f, 0.164f));
            damper.AddLathe(new[] { P(0.012f, 0.164f, true), P(0.012f, 0.172f, true), P(0.0001f, 0.172f, true) }, 16, true, new Vector3(0f, 2.05f + 0.07f, 0f), toZ);
            damper.Attach("Damper", root, SurfaceMaterial.Chrome);

            var labels = new GrayboxMeshBuilder();
            AddLabel(labels, new Vector3(0f, 1.50f, front), Vector3.left, Vector3.up, 0.30f, 0.15f, PlantLabel.UnitTag);
            AddLabel(labels, new Vector3(-0.0f, 0.40f, front), Vector3.left, Vector3.up, 0.24f, 0.12f, PlantLabel.Warning);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, (bodyTop + 0.02f) / 2f, 0f), new Vector3(2f * halfX, bodyTop + 0.02f, 2f * halfZ));
        }
    }
}
