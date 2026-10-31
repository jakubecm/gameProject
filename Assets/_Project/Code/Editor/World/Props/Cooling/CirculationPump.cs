using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Base-mounted end-suction circulation pump 1.0 x 0.7 x 0.5 m (photo): a cast-iron volute casing with suction and discharge
        /// flanges, a ribbed lantern, and a blue three-phase motor with a ribbed front end shield, cooling fins, a nameplate on a pad,
        /// a terminal box with a cable gland and a fan cover, on tapered cast feet; the blue skid is made of channels with anchor lugs
        /// and a pump plate. The discharge rises from the top of the casing to 0.7 m (the room carries it on), the suction flange faces
        /// -X for the room's pipe. The pivot is the discharge axis, the motor extends towards +X; terminal box and nameplate face +Z.
        /// </summary>
        [Model(Prop.CirculationPump)]
        private static void BuildCirculationPump(Transform root)
        {
            const float axisHeight = 0.34f;
            const float baseTop = 0.10f;
            var toX = Quaternion.Euler(0f, 0f, -90f);
            var toMinusX = Quaternion.Euler(0f, 0f, 90f);
            var toZ = Quaternion.Euler(90f, 0f, 0f);
            var axis = new Vector3(0f, axisHeight, 0f);

            // Skid: two C-channels along X, cross channels, anchor lugs with bolts.
            var blue = new MeshBuilder();
            var chrome = new MeshBuilder();
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
            var iron = new MeshBuilder();

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
            var post = new MeshBuilder();
            post.AddBox(new Vector3(0.10f, 0.11f, -0.016f), new Vector3(0.20f, 0.15f, 0.016f));
            for (int i = 0; i < 4; i++)
            {
                iron.Append(post, axis, Quaternion.Euler(45f + 90f * i, 0f, 0f));
            }
            iron.AddLathe(new[] { P(0.10f, 0.095f, true), P(0.16f, 0.095f, true), P(0.16f, 0.105f, true), P(0.10f, 0.105f, true) }, 40, true, axis, toX);

            // Motor: front end shield with a cone and bolts, a finned frame, a fan cover; tapered cast feet; terminal box; nameplate pad.
            var motor = new MeshBuilder();
            motor.AddLathe(new[]
            {
                P(0.12f, 0.20f, true), P(0.17f, 0.20f, true), P(0.17f, 0.225f, true), P(0.15f, 0.24f), P(0.138f, 0.265f, true), P(0.138f, 0.555f, true),
                P(0.152f, 0.56f), P(0.158f, 0.585f), P(0.15f, 0.625f), P(0.11f, 0.65f), P(0.0001f, 0.655f, true),
            }, 56, true, axis, toX);
            for (int i = 0; i < 6; i++)
            {
                var rib = new MeshBuilder();
                rib.AddBox(new Vector3(0.205f, 0.16f, -0.006f), new Vector3(0.24f, 0.172f, 0.006f));
                motor.Append(rib, axis, Quaternion.Euler(i * 60f + 30f, 0f, 0f));
            }
            var fin = new MeshBuilder();
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
            var black = new MeshBuilder();
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

            var stub = new MeshBuilder();
            stub.AddLathe(new[] { P(RoomPipes.WideRadius, axisHeight + 0.295f, true), P(RoomPipes.WideRadius, 0.70f, true) }, 24, true, Vector3.zero, Quaternion.identity);
            stub.Attach("Discharge", root, SurfaceMaterial.AluminiumClad);

            var labels = new MeshBuilder();
            // Nameplates on both sides, upright for someone standing at either side (+Z and -Z).
            AddLabel(labels, new Vector3(0.41f, axisHeight, 0.172f), Vector3.left, Vector3.up, 0.17f, 0.085f, PlantLabel.MotorPlate);
            AddLabel(labels, new Vector3(0.41f, axisHeight, -0.172f), Vector3.right, Vector3.up, 0.17f, 0.085f, PlantLabel.MotorPlate);
            AddLabel(labels, new Vector3(0.385f, axisHeight + 0.20f, 0f), Vector3.left, Vector3.back, 0.12f, 0.06f, PlantLabel.Warning);
            AddLabel(labels, new Vector3(0.135f, baseTop / 2f, 0.235f), Vector3.left, Vector3.up, 0.20f, 0.075f, PlantLabel.PumpPlate);
            AddLabel(labels, new Vector3(0.135f, baseTop / 2f, -0.235f), Vector3.right, Vector3.up, 0.20f, 0.075f, PlantLabel.PumpPlate);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0.135f, 0.35f, 0f), new Vector3(1.09f, 0.7f, 0.5f));
        }
    }
}
