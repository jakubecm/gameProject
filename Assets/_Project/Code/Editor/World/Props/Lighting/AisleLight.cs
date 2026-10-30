using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>LED strip 1.2 m long (along Z) under the containment roof of a cold aisle; pivot on the roof's underside.</summary>
        [Model(Prop.AisleLight)]
        private static void BuildAisleLight(Transform root)
        {
            const float length = 1.2f;
            // A white aluminium channel with rounded edges, end caps, an opal diffuser and two mounting clips on the roof panels.
            var body = new MeshBuilder();
            var diffuser = new MeshBuilder();
            body.AddRoundedBlock(new Vector3(0f, -0.030f, 0f), 0.06f, length, 0.010f, 0.030f, 0.004f, 3);
            foreach (int end in new[] { -1, 1 })
            {
                body.AddBox(new Vector3(-0.032f, -0.034f, end * (length / 2f) - (end > 0 ? 0f : 0.004f)), new Vector3(0.032f, -0.002f, end * (length / 2f) + (end > 0 ? 0.004f : 0f)));
                body.AddBox(new Vector3(-0.012f, -0.002f, end * 0.45f - 0.015f), new Vector3(0.012f, 0.002f, end * 0.45f + 0.015f));
            }
            diffuser.AddRoundedBlock(new Vector3(0f, -0.036f, 0f), 0.048f, length - 0.02f, 0.006f, 0.006f, 0.002f, 2);
            body.Attach("Body", root, SurfaceMaterial.Trunking);
            diffuser.Attach("Diffuser", root, SurfaceMaterial.LightPanel);
            AddSpotLight(root, new Vector3(0f, -0.05f, 0f), 160f, 110f, 3.5f, 1.8f, LedPanelColor);
        }
    }
}
