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
        /// <summary>
        /// Floor box 150 x 150 mm (after a product photo), flush with the floor: a dark hammered-pewter square cover plate with
        /// rounded corners, 6 mm proud at its rim, a recessed rectangular lid inside it with two hinge slots along the back edge
        /// (-Z) and a round chrome screw lock in the middle of the front edge (+Z). It lies under the back of the desk it feeds.
        /// </summary>
        [Model(Prop.FloorBox)]
        private static void BuildFloorBox(Transform root)
        {
            const float size = 0.150f;
            var model = ModelUtility.CreateGroup("Model", root);
            // A slab's depth runs along +Z; stand it on the floor (depth up, its +Y edge towards -Z, the back).
            model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var plate = new MeshBuilder();
            var lid = new MeshBuilder();
            var dark = new MeshBuilder();
            var chrome = new MeshBuilder();
            plate.AddRoundedSlab(Vector3.zero, size, size, 0.010f, 0.0060f, 0.0015f, 4);
            // Inset lid, a millimetre below the rim, with a thin dark groove round it.
            dark.AddRoundedSlab(new Vector3(0f, 0f, 0.0058f), 0.118f, 0.118f, 0.004f, 0.0004f, 0.0001f, 3);
            lid.AddRoundedSlab(new Vector3(0f, 0f, 0.0058f), 0.114f, 0.114f, 0.003f, 0.0016f, 0.0006f, 3);
            // Hinge slots on the back edge (model +Y), the screw lock on the front edge (model -Y).
            foreach (float x in new[] { -0.040f, 0.040f })
            {
                dark.AddBox(new Vector3(x - 0.012f, 0.0525f, 0.0058f), new Vector3(x + 0.012f, 0.0545f, 0.0064f), true, false);
            }
            chrome.AddLathe(new[] { P(0f, 0.0080f), P(0.0045f, 0.0078f), P(0.0050f, 0.0074f, true), P(0.0050f, 0.0066f, true), P(0f, 0.0066f, true) }, 24, true,
                new Vector3(0f, -0.0455f, 0f), Quaternion.Euler(90f, 0f, 0f));
            plate.Attach("CoverPlate", model, SurfaceMaterial.FloorBoxMetal);
            lid.Attach("Lid", model, SurfaceMaterial.FloorBoxMetal);
            dark.Attach("Groove", model, SurfaceMaterial.BlackHousing);
            chrome.Attach("Lock", model, SurfaceMaterial.Chrome);
        }
    }
}
