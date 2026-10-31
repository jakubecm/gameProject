using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Distance from the roof deck down to the top of a pendant light fitting (to <see cref="HallContract.LightTop"/>).</summary>
        public const float PendantLightDrop = HallContract.CeilingHeight - HallContract.LightTop;

        /// <summary>Distance from a girder's bottom flange down to the top of a girder-hung fitting (the same height as the others).</summary>
        public const float PendantLightGirderDrop = HallContract.GirderBottom - HallContract.LightTop;

        /// <summary>Width of a girder's bottom flange (two tiles' halves of the I-beam), which the beam clamps grip.</summary>
        private const float GirderFlangeWidth = 0.3f;

        // LED high-bay fittings, 4000 K, no shadows.
        private static readonly Color LedColor = new Color(1f, 0.97f, 0.92f);

        /// <summary>
        /// The hall's luminaire: the storage room's LED batten (<see cref="BuildLedBatten"/>, 1.5 m long along Z, its light a wide downward spot without shadows)
        /// hanging <see cref="PendantLightDrop"/> below the roof deck on two steel wire ropes, each with a rounded ceiling plate, a chrome cup with a cable
        /// gripper, a crimped ferrule and a thimble eye on a bracket that sits on the tray, and a round black power cable that curves down from a small junction
        /// box on the roof to a gland on the tray. Pivot on the roof deck, hanging down.
        /// </summary>
        [Model(Prop.PendantLight)]
        private static void BuildPendantLight(Transform root) => BuildPendantLight(root, PendantLightDrop, false);

        /// <summary>
        /// The same luminaire hung from the bottom flange of a girder (over a walkway between two hall tiles, whose middle is the girder): each wire
        /// rope hangs from a galvanised beam clamp gripping both edges of the flange, the ropes run along the girder, and the power cable comes from
        /// a junction box clamped under the flange. Pivot on the underside of the flange, hanging down; the fitting's top is as high as the others'.
        /// </summary>
        [Model(Prop.PendantLightGirder)]
        private static void BuildPendantLightGirder(Transform root) => BuildPendantLight(root, PendantLightGirderDrop, true);

        private static void BuildPendantLight(Transform root, float drop, bool girder)
        {
            float top = -drop;
            var lamp = ModelUtility.CreateGroup("Lamp", root);
            lamp.localPosition = new Vector3(0f, top, 0f);
            BuildLedBatten(lamp);
            var light = lamp.GetComponentInChildren<Light>();
            light.spotAngle = 140f;
            light.innerSpotAngle = 90f;
            light.range = 12f;
            light.intensity = 9f;
            light.color = LedColor;

            var zinc = new MeshBuilder();
            var chrome = new MeshBuilder();
            var rope = new MeshBuilder();
            var eyes = new MeshBuilder();
            var black = new MeshBuilder();
            var box = new MeshBuilder();
            foreach (float z in new[] { -0.55f, 0.55f })
            {
                if (girder)
                {
                    // Beam clamp: a plate under the flange and a jaw hooked over each of its edges.
                    float half = GirderFlangeWidth / 2f;
                    zinc.AddRoundedBlock(new Vector3(0f, -0.008f, z), GirderFlangeWidth + 0.04f, 0.05f, 0.004f, 0.008f, 0.0015f);
                    foreach (int side in new[] { -1, 1 })
                    {
                        zinc.AddRoundedBlock(new Vector3(side * (half + 0.01f), 0f, z), 0.02f, 0.05f, 0.003f, 0.035f, 0.0015f);
                        zinc.AddRoundedBlock(new Vector3(side * (half - 0.005f), 0.025f, z), 0.05f, 0.05f, 0.003f, 0.01f, 0.0015f);
                    }
                }
                else
                {
                    // Ceiling plate.
                    zinc.AddRoundedBlock(new Vector3(0f, -0.006f, z), 0.07f, 0.07f, 0.012f, 0.006f, 0.002f);
                }
                // The cup with the cable gripper under the plate.
                chrome.AddLathe(new[] { P(0.026f, -0.006f, true), P(0.026f, -0.016f), P(0.016f, -0.028f), P(0.0075f, -0.032f, true), P(0.0075f, -0.056f, true), P(0.0035f, -0.058f, true) }, 24, true,
                    new Vector3(0f, 0f, z), Quaternion.identity);
                // The wire rope down to the lamp, a crimped ferrule round its end, and the thimble eye on the bracket.
                float eyeY = top + 0.022f;
                rope.AddTube(new[] { new Vector3(0f, -0.058f, z), new Vector3(0f, eyeY + 0.01f, z) }, 0.0018f, 8, Vector3.zero, Quaternion.identity);
                chrome.AddLathe(new[] { P(0.0042f, eyeY + 0.012f, true), P(0.0062f, eyeY + 0.016f), P(0.0062f, eyeY + 0.046f, true), P(0.0042f, eyeY + 0.050f, true) }, 16, true,
                    new Vector3(0f, 0f, z), Quaternion.identity);
                var ring = new List<Vector3>();
                for (int i = 0; i < 16; i++)
                {
                    float angle = i / 16f * 2f * Mathf.PI;
                    ring.Add(new Vector3(Mathf.Cos(angle) * 0.010f, eyeY + Mathf.Sin(angle) * 0.013f, z));
                }
                eyes.AddTube(ring, 0.0022f, 8, Vector3.zero, Quaternion.identity, true);
                // The bracket: a strap over the tray with the eye bolted on.
                zinc.AddExtrusion(RoundedRectLoop(-0.052f, 0.052f, z - 0.018f, z + 0.018f, 0.006f, 3), new Vector3(0f, top, 0f), new Vector3(0f, top + 0.004f, 0f),
                    Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
                chrome.AddLathe(new[] { P(0.0035f, 0f, true), P(0.0035f, 0.014f, true), P(0.0001f, 0.014f, true) }, 10, true, new Vector3(0f, top + 0.004f, z), Quaternion.identity);
            }

            // Power cable: junction box on the roof (under the flange on a girder), a gentle S down to the gland near the end of the tray.
            const float cableZ = -0.68f;
            box.AddRoundedBlock(new Vector3(0.06f, -0.04f, cableZ), 0.09f, 0.09f, 0.01f, 0.04f, 0.004f);
            black.AddLathe(new[] { P(0.012f, top, true), P(0.012f, top + 0.014f, true), P(0.0075f, top + 0.018f, true), P(0.0075f, top + 0.030f, true) }, 16, true, new Vector3(0f, 0f, cableZ), Quaternion.identity);
            black.AddTube(SmoothPath(new[]
            {
                new Vector3(0f, top + 0.030f, cableZ), new Vector3(0f, top + 0.12f, cableZ), new Vector3(0.03f, top * 0.5f, cableZ), new Vector3(0.06f, -0.16f, cableZ), new Vector3(0.06f, -0.04f, cableZ),
            }, 6), 0.0045f, 10, Vector3.zero, Quaternion.identity);

            zinc.Attach("Plates", root, SurfaceMaterial.ValveSteel);
            chrome.Attach("Cups", root, SurfaceMaterial.Chrome);
            rope.Attach("WireRopes", root, SurfaceMaterial.ValveSteel);
            eyes.Attach("Thimbles", root, SurfaceMaterial.Chrome);
            black.Attach("PowerCable", root, SurfaceMaterial.BlackHousing);
            box.Attach("JunctionBox", root, SurfaceMaterial.ElectricGrey);
        }
    }
}
