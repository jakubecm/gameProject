using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Slim graphite keyboard with a number pad, after a product photo: a rounded case (about 395 x 136 mm, 11 mm thick)
        /// and about a hundred separate rounded keys on a 19.05 mm pitch (function row, the main block with the wide keys,
        /// the arrows, and a four-column number pad with its tall plus and enter keys). It lies on a desk, the number
        /// pad on the user's right (-X, the user faces -Z). <paramref name="position"/> is the middle of its footprint.
        /// </summary>
        private static void BuildKeyboard(Transform root, Vector3 position)
        {
            const float u = 0.01905f;
            const float totalU = 19.5f;
            var body = new MeshBuilder();
            var keys = new MeshBuilder();
            float width = totalU * u + 0.016f;
            float depth = 6f * u + 0.018f;
            body.AddRoundedBlock(position, width, depth, 0.006f, 0.011f, 0.003f, 3);

            float left = -totalU * u / 2f;
            float far = -3f * u;
            void Key(float column, float row, float w, float h = 1f)
            {
                float cx = left + (column + w / 2f) * u;
                float cz = far + (row + h / 2f) * u;
                // The user's right is -X: a key at user-x cx is at world x -cx.
                keys.AddRoundedBlock(position + new Vector3(-cx, 0.0108f, cz), w * u - 0.0022f, h * u - 0.0022f, 0.0022f, 0.0045f, 0.0014f, 2);
            }
            void Row(int row, float startColumn, params float[] widths)
            {
                float column = startColumn;
                foreach (float w in widths)
                {
                    Key(column, row, w);
                    column += w;
                }
            }

            Row(0, 0f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
            Row(1, 0f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2);
            Row(2, 0f, 1.5f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.5f);
            Row(3, 0f, 1.75f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1.25f);
            Row(4, 0f, 1.25f, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2.75f);
            Row(5, 0f, 1.25f, 1, 1.25f, 1.25f, 4.75f, 1.25f, 1.25f, 1, 1, 1);
            // Number pad: four columns starting 0.5 u after the main block.
            const float pad = 15.5f;
            Row(0, pad, 1, 1, 1, 1);
            Row(1, pad, 1, 1, 1, 1);
            Row(2, pad, 1, 1, 1);
            Row(3, pad, 1, 1, 1);
            Row(4, pad, 1, 1, 1);
            Key(pad + 3f, 2f, 1f, 2f);
            Key(pad + 3f, 4f, 1f, 2f);
            Key(pad, 5f, 2f);
            Key(pad + 2f, 5f, 1f);

            body.Attach("Keyboard_Case", root, SurfaceMaterial.KeyboardGraphite);
            keys.Attach("Keyboard_Keys", root, SurfaceMaterial.KeyboardKey);
        }

        /// <summary>
        /// Compact wireless mouse: a short, smooth half-oval dome 88 x 62 x 41 mm with fully rounded (blunt) front and rear ends, matt
        /// black, a split between the two buttons and a scroll wheel in front of the middle; no mat. The wheel end is the
        /// front and points away from the user (-Z), at the monitor. <paramref name="position"/> is the middle of its footprint.
        /// </summary>
        private static void BuildMouse(Transform root, Vector3 position)
        {
            const float length = 0.088f;
            const float endPower = 2.6f;
            const float halfWidth = 0.031f;
            const float domeHeight = 0.034f;
            const float bottomRound = 0.0075f;
            const float baseY = bottomRound;
            const int rings = 44;
            const int around = 44;
            var body = new MeshBuilder();
            var wheel = new MeshBuilder();
            var slot = new MeshBuilder();
            var details = new MeshBuilder();

            // Outline of the dome along its length: a superellipse, so the front and rear are rounded off (blunt) instead of tapering.
            float Shape(float u) => Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(u), endPower)), 1f / endPower);

            // Rings along the length at u = -cos(phi), so the ends are closely spaced where the shape curves in.
            var loft = new List<Vector3[]>();
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.Lerp(0.04f, Mathf.PI - 0.04f, r / (float)rings);
                float u = -Mathf.Cos(phi);
                float scale = Shape(u);
                var ring = new Vector3[around];
                for (int k = 0; k < around; k++)
                {
                    float angle = 2f * Mathf.PI * k / around;
                    float s = Mathf.Sin(angle);
                    ring[k] = new Vector3(halfWidth * scale * Mathf.Cos(angle), baseY + (s > 0f ? domeHeight * scale * s : bottomRound * scale * s), u * length / 2f);
                }
                loft.Add(ring);
            }
            body.AddLoft(loft, position);

            // Scroll wheel sunk into a dark slot, and the split between the buttons, on the top line of the dome.
            float TopY(float u) => baseY + domeHeight * Shape(u);
            const float wheelU = -0.40f;
            wheel.AddLathe(new[] { P(0f, -0.0036f, true), P(0.0075f, -0.0036f, true), P(0.0075f, 0.0036f, true), P(0f, 0.0036f, true) }, 28, true,
                position + new Vector3(0f, TopY(wheelU) - 0.0050f, wheelU * length / 2f), Quaternion.Euler(0f, 0f, 90f));
            var slotPath = new List<Vector3>();
            for (int i = 0; i <= 8; i++)
            {
                float u = Mathf.Lerp(-0.58f, -0.22f, i / 8f);
                slotPath.Add(new Vector3(0f, TopY(u) + 0.0004f, u * length / 2f));
            }
            slot.AddStrip(slotPath, Vector3.right, 0.013f, 0.0014f, position, Quaternion.identity);
            var split = new List<Vector3>();
            for (int i = 0; i <= 10; i++)
            {
                float u = Mathf.Lerp(-0.93f, -0.60f, i / 10f);
                split.Add(new Vector3(0f, TopY(u) + 0.0003f, u * length / 2f));
            }
            details.AddStrip(split, Vector3.right, 0.0014f, 0.0008f, position, Quaternion.identity);

            body.Attach("Mouse_Body", root, SurfaceMaterial.MouseBlack);
            slot.Attach("Mouse_Slot", root, SurfaceMaterial.BlackHousing);
            wheel.Attach("Mouse_Wheel", root, SurfaceMaterial.KeyboardKey);
            details.Attach("Mouse_Details", root, SurfaceMaterial.KeyboardKey);
        }

        /// <summary>
        /// 24" desk monitor, after product photos: a slim matt black body 540 x 325 mm with thin bezels (a little deeper at the
        /// bottom with the small logo and power light) around a lit picture, a plain flat rear with only the logo; a tall rectangular black neck on a flat silver base. The screen faces +Z
        /// and the bottom of the picture is 0.105 m above the desk; <paramref name="position"/> is the middle of the base on the desk.
        /// </summary>
        private static void BuildMonitor(Transform root, Vector3 position)
        {
            const float width = 0.540f;
            const float height = 0.325f;
            const float bottom = 0.105f;
            const float depth = 0.014f;
            const float back = -0.060f;
            float face = back + depth;
            float centerY = bottom + height / 2f;
            var body = new MeshBuilder();
            var picture = new MeshBuilder();
            var silver = new MeshBuilder();
            var marks = new MeshBuilder();
            var led = new MeshBuilder();

            // Body: panel with a closed, completely plain back, and the neck that joins it.
            body.AddRoundedSlab(position + new Vector3(0f, centerY, back), width, height, 0.005f, depth, 0.0025f, 4, true);
            body.AddBox(position + new Vector3(-0.036f, 0.010f, back - 0.032f), position + new Vector3(0.036f, bottom + 0.100f, back + 0.005f));

            // Silver base: a flat rounded plate.
            silver.AddRoundedBlock(position + new Vector3(0f, 0f, -0.045f), 0.270f, 0.200f, 0.030f, 0.012f, 0.0025f, 5);

            // The lit picture, inset by the bezels (thicker at the bottom).
            float z = face + 0.0003f;
            float x0 = -width / 2f + 0.0045f;
            float x1 = width / 2f - 0.0045f;
            float y0 = bottom + 0.0120f;
            float y1 = bottom + height - 0.0045f;
            picture.AddPanel(position + new Vector3(x0, y0, z), position + new Vector3(x1, y0, z), position + new Vector3(x1, y1, z), position + new Vector3(x0, y1, z),
                Vector3.forward, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));

            // Logo mark on the chin and the power light at its right (the viewer's right is -X).
            marks.AddBox(position + new Vector3(-0.0075f, bottom + 0.0045f, face), position + new Vector3(0.0075f, bottom + 0.0072f, face + 0.0006f));
            led.AddBox(position + new Vector3(-0.2380f, bottom + 0.0050f, face), position + new Vector3(-0.2360f, bottom + 0.0066f, face + 0.0005f));

            // Rear: just the logo (four letter blocks).
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 0.0125f;
                marks.AddBox(position + new Vector3(x - 0.0035f, bottom + height - 0.060f, back - 0.0006f), position + new Vector3(x + 0.0035f, bottom + height - 0.049f, back));
            }

            body.Attach("Monitor_Body", root, SurfaceMaterial.MouseBlack);
            silver.Attach("Monitor_Base", root, SurfaceMaterial.Metal);
            picture.Attach("Monitor_Picture", root, SurfaceMaterial.MonitorImage);
            marks.Attach("Monitor_Marks", root, SurfaceMaterial.KeyboardKey);
            led.Attach("Monitor_Led", root, SurfaceMaterial.SafetyGreen);
        }

        /// <summary>Two monitors angled towards the user, one keyboard and a mouse to its right; on a desk top, screens face +Z.</summary>
        [Model(Prop.Workstation)]
        private static void BuildWorkstation(Transform root)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var monitor = ModelUtility.CreateGroup(side < 0 ? "Monitor_Left" : "Monitor_Right", root);
                monitor.localPosition = new Vector3(side * 0.268f, 0f, -0.10f);
                monitor.localRotation = Quaternion.Euler(0f, -side * 12f, 0f);
                BuildMonitor(monitor, Vector3.zero);
            }
            BuildKeyboard(root, new Vector3(0f, 0f, 0.18f));
            BuildMouse(root, new Vector3(-0.3f, 0f, 0.19f));
        }
    }
}
