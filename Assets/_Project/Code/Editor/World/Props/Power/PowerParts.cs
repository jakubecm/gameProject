using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// The control panel of the heat exchanger (rounded grey plate, display, start / stop and amber buttons, emergency stop) at any size:
        /// <c>k</c> is its width over 0.54 m. It faces +Z and stands on the surface at <c>z</c> of the centre.
        /// </summary>
        private sealed class ControlPanelMeshes
        {
            private readonly MeshBuilder plastic = new MeshBuilder();
            private readonly MeshBuilder screen = new MeshBuilder();
            private readonly MeshBuilder green = new MeshBuilder();
            private readonly MeshBuilder red = new MeshBuilder();
            private readonly MeshBuilder amber = new MeshBuilder();
            private readonly MeshBuilder grey = new MeshBuilder();

            public void Add(Vector3 c, float k)
            {
                var toZ = Quaternion.Euler(90f, 0f, 0f);
                float t = 0.03f * k;
                float z = c.z;
                plastic.AddExtrusion(RoundedRectLoop(c.x - 0.27f * k, c.x + 0.27f * k, c.y - 0.13f * k, c.y + 0.13f * k, 0.018f * k, 3),
                    new Vector3(0f, 0f, z), new Vector3(0f, 0f, z + t), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                screen.AddExtrusion(RoundedRectLoop(c.x - 0.245f * k, c.x - 0.045f * k, c.y + 0.01f * k, c.y + 0.105f * k, 0.006f * k, 2),
                    new Vector3(0f, 0f, z + t), new Vector3(0f, 0f, z + t + 0.004f * k), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                void Button(MeshBuilder b, float x, float y, float radius, float lift)
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
        private static void AddPlate(MeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1, float radius) =>
            b.AddExtrusion(RoundedRectLoop(x0, x1, y0, y1, radius, 2), new Vector3(0f, 0f, z0), new Vector3(0f, 0f, z1), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

        /// <summary>The outline of a door on the front face: four thin dark joints, 3 mm wide, flush with the surface.</summary>
        private static void AddDoorJoint(MeshBuilder joint, float x0, float x1, float y0, float y1, float zFace)
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
        private static void AddSkin(MeshBuilder b, float x0, float x1, float y0, float y1, float zBack, float zFront, IList<Vector4> holes)
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
        private static void AddVent(MeshBuilder slats, MeshBuilder back, float x0, float x1, float y0, float y1, float zBackPlane, float zIn, float zFront, float pitch)
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
        private static void AddDuct(MeshBuilder duct, float x0, float x1, float z0, float z1, float yBottom, float yTop)
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
        private static void AddLeverLock(MeshBuilder plate, MeshBuilder metal, float x, float y, float scale, float z)
        {
            AddPlate(plate, x - 0.03f * scale, x + 0.03f * scale, y - 0.12f * scale, y + 0.12f * scale, z, z + 0.012f * scale, 0.012f * scale);
            AddPlate(metal, x - 0.012f * scale, x + 0.012f * scale, y - 0.09f * scale, y + 0.09f * scale, z + 0.012f * scale, z + 0.026f * scale, 0.008f * scale);
            metal.AddLathe(new[] { P(0.016f * scale, 0.012f * scale, true), P(0.016f * scale, 0.034f * scale, true), P(0.0001f, 0.034f * scale, true) }, 20, true, new Vector3(x, y, z), Quaternion.Euler(90f, 0f, 0f));
        }
    }
}
