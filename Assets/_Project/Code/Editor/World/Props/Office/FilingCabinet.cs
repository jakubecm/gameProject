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
        /// <summary>
        /// Five-drawer filing cabinet 0.45 x 1.32 x 0.6 m after a product photo, in cool white sheet steel: a body on a recessed plinth,
        /// five flat drawer fronts with 4 mm joints and a deep pocket in the middle of each, open in its lower half only so a finger can hook behind the filled upper half (no lock). The drawers face +Z.
        /// </summary>
        [Model(Prop.FilingCabinet)]
        private static void BuildFilingCabinet(Transform root)
        {
            const float width = 0.45f;
            const float height = 1.32f;
            const float depth = 0.6f;
            const float plinth = 0.03f;
            const float fronts = 5;
            var steel = new MeshBuilder();
            var pulls = new MeshBuilder();
            steel.AddRoundedBlock(new Vector3(0f, plinth, 0f), width, depth, 0.006f, height - plinth, 0.004f, 3);
            float pitch = (height - plinth - 0.02f) / fronts;
            for (int i = 0; i < fronts; i++)
            {
                float y0 = plinth + 0.01f + i * pitch + 0.002f;
                float y1 = y0 + pitch - 0.004f;
                float cy = (y0 + y1) / 2f;
                float xe = width / 2f - 0.012f;
                const float pocketHalfX = 0.05f;
                const float pocketHalfY = 0.0225f;
                float z0 = depth / 2f;
                float z1 = depth / 2f + 0.020f;
                // The drawer front (20 mm proud of the body) with a deep pocket (100 x 45 mm) for the pull. Only the lower half of the pocket is
                // open: the upper half is filled by the front itself, so a finger put into the pocket hooks behind it and can pull the drawer.
                steel.AddBox(new Vector3(-xe, y0, z0), new Vector3(-pocketHalfX, y1, z1));
                steel.AddBox(new Vector3(pocketHalfX, y0, z0), new Vector3(xe, y1, z1));
                steel.AddBox(new Vector3(-pocketHalfX, cy, z0), new Vector3(pocketHalfX, y1, z1));
                steel.AddBox(new Vector3(-pocketHalfX, y0, z0), new Vector3(pocketHalfX, cy - pocketHalfY, z1));
                // The pocket's lining in a darker grey: the back, the floor and the two sides.
                pulls.AddBox(new Vector3(-pocketHalfX, cy - pocketHalfY, z0), new Vector3(pocketHalfX, cy, z0 + 0.002f));
                pulls.AddBox(new Vector3(-pocketHalfX, cy - pocketHalfY, z0 + 0.002f), new Vector3(pocketHalfX, cy - pocketHalfY + 0.003f, z1 - 0.001f));
                pulls.AddBox(new Vector3(-pocketHalfX, cy - pocketHalfY + 0.003f, z0 + 0.002f), new Vector3(-pocketHalfX + 0.002f, cy, z1 - 0.001f));
                pulls.AddBox(new Vector3(pocketHalfX - 0.002f, cy - pocketHalfY + 0.003f, z0 + 0.002f), new Vector3(pocketHalfX, cy, z1 - 0.001f));
            }
            steel.Attach("Cabinet_Body", root, SurfaceMaterial.CabinetWhite);
            pulls.Attach("Cabinet_Pulls", root, SurfaceMaterial.CabinetPull);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.01f), new Vector3(width, height, depth + 0.02f));
        }
    }
}
