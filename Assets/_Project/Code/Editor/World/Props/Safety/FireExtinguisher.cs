using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// Wall-mounted 6 kg ABC powder extinguisher (160 mm across, about 0.6 m high on its bracket), after a product photo:
        /// a glossy red cylinder with a rounded shoulder and rolled foot, a generic printed label on the front, the chrome valve
        /// head with a pressure gauge and the two red levers with a safety wire, and the thick black rubber hose that leaves
        /// the head to the left, sweeps out and down in one smooth curve and is clipped to the body above its black nozzle.
        /// A floor-based wall prop: the pivot is on the floor at the wall, the axis 100 mm out, +Z away from the wall.
        /// </summary>
        [Model(Prop.FireExtinguisher)]
        private static void BuildFireExtinguisher(Transform root)
        {
            var offset = new Vector3(0f, 0f, 0.10f);
            var none = Quaternion.identity;
            var toZ = Quaternion.Euler(90f, 0f, 0f);
            var red = new MeshBuilder();
            var levers = new MeshBuilder();
            var rubber = new MeshBuilder();
            var label = new MeshBuilder();
            var chrome = new MeshBuilder();
            var gauge = new MeshBuilder();
            var bracket = new MeshBuilder();

            red.AddLathe(new[]
            {
                P(0f, 0.545f, true), P(0.0600f, 0.545f, true), P(0.0690f, 0.550f), P(0.0770f, 0.562f), P(0.0800f, 0.582f), P(0.0788f, 0.600f), P(0.0800f, 0.614f),
                P(0.0800f, 0.930f), P(0.0782f, 0.952f), P(0.0720f, 0.980f), P(0.0580f, 1.002f), P(0.0420f, 1.014f), P(0.0300f, 1.020f, true), P(0.0245f, 1.022f, true),
                P(0.0245f, 1.036f, true),
            }, 48, true, offset, none);
            // The label wraps 119 degrees of the straight part of the body, facing +Z, in the proportions of its texture (330 x 562).
            label.AddLathe(new[] { P(0.0806f, 0.630f, true), P(0.0806f, 0.915f, true) }, 36, true, offset, none, 30.6f, 118.8f, true);

            // Valve head: body, hose connector with its nut on the left, gauge on the front, rivet.
            chrome.AddLathe(new[]
            {
                P(0.0300f, 1.034f, true), P(0.0300f, 1.060f), P(0.0278f, 1.075f), P(0.0200f, 1.086f), P(0.0100f, 1.090f, true), P(0f, 1.090f, true),
            }, 32, true, offset, none);
            chrome.AddLathe(new[]
            {
                P(0.0125f, 0f, true), P(0.0125f, 0.004f), P(0.0158f, 0.007f, true), P(0.0158f, 0.021f, true), P(0.0128f, 0.024f, true), P(0.0125f, 0.036f),
                P(0.0108f, 0.040f, true), P(0f, 0.040f, true),
            }, 24, true, new Vector3(-0.020f, 1.060f, 0.10f), Quaternion.Euler(0f, 0f, 90f));
            chrome.AddLathe(new[] { P(0.0165f, 0f, true), P(0.0165f, 0.012f), P(0.0150f, 0.015f, true), P(0f, 0.015f, true) }, 28, true, new Vector3(0f, 1.056f, 0.12f), toZ);
            gauge.AddLathe(new[] { P(0.0135f, 0.0152f, true), P(0f, 0.0152f, true) }, 28, true, new Vector3(0f, 1.056f, 0.12f), toZ);
            chrome.AddLathe(new[] { P(0.0055f, -0.004f, true), P(0.0055f, 0.004f, true), P(0f, 0.004f, true) }, 12, true, new Vector3(0.002f, 1.088f, 0.10f), toZ);

            // Levers: the long top one rises to the right and bends down at its end, the short one below it curves down.
            levers.AddStrip(SmoothPath(new[]
            {
                new Vector3(0.002f, 1.086f, 0f), new Vector3(0.035f, 1.100f, 0f), new Vector3(0.080f, 1.124f, 0f), new Vector3(0.125f, 1.150f, 0f),
                new Vector3(0.146f, 1.156f, 0f), new Vector3(0.152f, 1.147f, 0f),
            }, 6), Vector3.forward, 0.024f, 0.005f, offset, none);
            levers.AddStrip(SmoothPath(new[]
            {
                new Vector3(-0.004f, 1.064f, 0f), new Vector3(0.035f, 1.080f, 0f), new Vector3(0.080f, 1.088f, 0f), new Vector3(0.118f, 1.074f, 0f),
                new Vector3(0.134f, 1.050f, 0f),
            }, 6), Vector3.forward, 0.022f, 0.005f, offset, none);
            // The safety wire between the levers.
            chrome.AddTube(new List<Vector3> { new Vector3(0.112f, 1.140f, 0f), new Vector3(0.126f, 1.108f, 0f), new Vector3(0.108f, 1.076f, 0f) }, 0.0013f, 6, offset, none);

            // The hose: out of the connector, a wide smooth sweep to the left and down, back in to the body above the nozzle.
            rubber.AddTube(SmoothPath(new[]
            {
                new Vector3(-0.058f, 1.060f, 0f), new Vector3(-0.088f, 1.058f, 0f), new Vector3(-0.116f, 1.034f, 0f), new Vector3(-0.130f, 0.985f, 0f),
                new Vector3(-0.126f, 0.910f, 0f), new Vector3(-0.112f, 0.820f, 0f), new Vector3(-0.098f, 0.730f, 0f), new Vector3(-0.090f, 0.660f, 0f),
                new Vector3(-0.0885f, 0.605f, 0f),
            }, 8), 0.0115f, 14, offset, none);
            // Nozzle: a chrome ferrule and the black rubber horn.
            chrome.AddLathe(new[] { P(0.0125f, 0.600f, true), P(0.0125f, 0.616f, true), P(0.0105f, 0.620f, true) }, 20, true, new Vector3(-0.0885f, 0f, 0.10f), none);
            rubber.AddLathe(new[]
            {
                P(0f, 0.540f, true), P(0.0110f, 0.540f, true), P(0.0132f, 0.548f), P(0.0128f, 0.600f, true), P(0f, 0.600f, true),
            }, 20, true, new Vector3(-0.0885f, 0f, 0.10f), none);
            // The clip that holds the hose to the body.
            rubber.AddBox(new Vector3(-0.108f, 0.655f, 0.085f), new Vector3(-0.072f, 0.700f, 0.115f));

            // Wall bracket: a plate, an arm round the neck and a lower cradle.
            bracket.AddBox(new Vector3(-0.025f, 0.955f, 0f), new Vector3(0.025f, 1.040f, 0.004f));
            bracket.AddBox(new Vector3(-0.018f, 1.030f, 0.004f), new Vector3(0.018f, 1.036f, 0.078f));
            bracket.AddBox(new Vector3(-0.030f, 0.690f, 0f), new Vector3(0.030f, 0.730f, 0.012f));

            red.Attach("Body", root, SurfaceMaterial.SafetyRed);
            label.Attach("Label", root, SurfaceMaterial.ExtinguisherLabel);
            chrome.Attach("Head", root, SurfaceMaterial.Chrome);
            gauge.Attach("GaugeFace", root, SurfaceMaterial.DetectorWhite);
            levers.Attach("Levers", root, SurfaceMaterial.SafetyRed);
            rubber.Attach("Hose", root, SurfaceMaterial.BlackHousing);
            bracket.Attach("Bracket", root, SurfaceMaterial.Metal);
        }
    }
}
