using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Diaphragm expansion vessel 0.44 m across and 1.1 m high (photo): a regular cylinder with semicircular ends, white, on three
        /// tubular legs with foot plates, a blue cap on the air valve on top, a yellow pressure sticker centred on the cylinder, and a
        /// flanged nozzle on the side facing -Z (the wall) at 0.5 m for the room's pipe. A floor prop.
        /// </summary>
        [Model(Prop.ExpansionVessel)]
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

            var blue = new MeshBuilder();
            var chrome = new MeshBuilder();
            var leg = new MeshBuilder();
            var legChrome = new MeshBuilder();
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
            var shell = new MeshBuilder();
            shell.AddLathe(profile, 64, true, Vector3.zero, Quaternion.identity);
            shell.Attach("Shell", root, SurfaceMaterial.DetectorWhite);

            // The nozzle: a short stub with a flange, facing the wall (-Z) at 0.5 m.
            var nozzle = new MeshBuilder();
            nozzle.AddLathe(new[] { P(0.030f, 0f, true), P(0.030f, 0.05f, true), P(0.05f, 0.05f, true), P(0.05f, 0.068f, true), P(RoomPipes.WideRadius, 0.068f, true) }, 24, true,
                new Vector3(0f, 0.5f, -radius + 0.01f), Quaternion.Euler(-90f, 0f, 0f));
            nozzle.Attach("Nozzle", root, SurfaceMaterial.AluminiumClad);
            var labels = new MeshBuilder();
            AddCurvedLabel(labels, Vector3.zero, Vector3.up, Vector3.forward, radius, (bottomPole + top) / 2f, 0.14f, 0.28f, false, PlantLabel.VesselLabel);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, (top + 0.05f) / 2f, 0f), new Vector3(2f * radius, top + 0.05f, 2f * radius));
        }
    }
}
