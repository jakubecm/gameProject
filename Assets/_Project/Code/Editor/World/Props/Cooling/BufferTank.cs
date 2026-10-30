using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Chilled-water buffer tank 0.7 m across and 2.2 m high on three tubular legs with foot plates, insulated and clad in aluminium with
        /// rolled seams and two bands in the colour of the water, an equipment tag and two stickers centred between the seams. Two
        /// flanged nozzles on top (flow at -X, return at +X, 0.12 m off the axis, at 2.2 m) for the room's wall pipes. No gauge. A floor prop.
        /// </summary>
        [Model(Prop.BufferTank)]
        private static void BuildBufferTank(Transform root)
        {
            const float radius = 0.35f;
            const float height = 2.2f;
            var blue = new MeshBuilder();
            var chrome = new MeshBuilder();
            var leg = new MeshBuilder();
            var legChrome = new MeshBuilder();
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

            var shell = new MeshBuilder();
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
            var bands = new MeshBuilder();
            foreach (float y in new[] { 0.45f, 1.95f })
            {
                bands.AddLathe(new[] { P(radius, y, true), P(radius + 0.012f, y, true), P(radius + 0.012f, y + 0.07f, true), P(radius, y + 0.07f, true) }, 64, true, Vector3.zero, Quaternion.identity);
            }
            bands.Attach("Bands", root, SurfaceMaterial.PipeWater);

            // The nozzles on top: a flange with bolts each (the wall pipes start on them).
            var nozzles = new MeshBuilder();
            var nozzleBolts = new MeshBuilder();
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

            var labels = new MeshBuilder();
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Vector3.forward, radius, 1.375f, 0.19f, 0.36f, false, PlantLabel.ExchangerTag);
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Quaternion.Euler(0f, 40f, 0f) * Vector3.forward, radius, 0.925f, 0.12f, 0.24f, false, PlantLabel.Warning);
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Quaternion.Euler(0f, -40f, 0f) * Vector3.forward, radius, 0.925f, 0.12f, 0.24f, false, PlantLabel.PipeLabel);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, (height + 0.05f) / 2f, 0f), new Vector3(2f * radius, height + 0.05f, 2f * radius));
        }
    }
}
