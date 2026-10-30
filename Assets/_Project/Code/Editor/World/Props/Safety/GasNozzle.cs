using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>360° nozzle of the server room's inert-gas extinguishing system (no water near the racks).</summary>
        [Model(Prop.GasNozzle)]
        private static void BuildGasNozzle(Transform root)
        {
            // Hangs below its pivot (the underside of the pipe): a threaded stem, a hex collar, a chrome body with twelve outlet ports and a flat deflector cap.
            var chrome = new MeshBuilder();
            var ports = new MeshBuilder();
            chrome.AddLathe(new[] { P(0.0075f, 0f, true), P(0.0075f, -0.020f, true), P(0.0001f, -0.020f, true) }, 12, true, Vector3.zero, Quaternion.identity);
            chrome.AddLathe(new[] { P(0.0165f, -0.012f, true), P(0.0165f, -0.026f, true), P(0.0001f, -0.026f, true) }, 6, false, Vector3.zero, Quaternion.identity);
            chrome.AddLathe(new[] { P(0.0145f, -0.026f, true), P(0.0145f, -0.066f, true), P(0.019f, -0.070f), P(0.019f, -0.078f, true), P(0.0001f, -0.078f, true) }, 24, true, Vector3.zero, Quaternion.identity);
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * 2f * Mathf.PI;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                ports.AddDisc(new Vector3(0f, -0.048f, 0f) + outward * 0.0147f, outward, 0.0042f, 8);
            }
            chrome.Attach("Nozzle", root, SurfaceMaterial.Chrome);
            ports.Attach("Ports", root, SurfaceMaterial.BlackHousing);
        }
    }
}
