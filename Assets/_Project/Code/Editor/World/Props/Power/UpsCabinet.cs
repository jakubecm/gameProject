using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// UPS cabinet 0.6 x 2.0 x 0.8 m: a graphite cabinet on four feet whose front has two real vents cut in a 3 cm skin (a louver at the top and a larger one at the
        /// bottom, slanted slats in pockets), a control panel as on the heat exchanger over the unit tag (both centered on the cabinet), a green status strip and a
        /// vertical handle with a lock. The front faces +Z.
        /// </summary>
        [Model(Prop.UpsCabinet)]
        private static void BuildUpsCabinet(Transform root)
        {
            const float width = 0.6f;
            const float height = 2.0f;
            const float depth = 0.8f;
            const float skin = 0.03f;
            float front = depth / 2f;
            float core = front - skin;
            var body = new MeshBuilder();
            var feet = new MeshBuilder();
            var trim = new MeshBuilder();
            var slats = new MeshBuilder();
            var dark = new MeshBuilder();
            var led = new MeshBuilder();
            var handle = new MeshBuilder();
            var handleBase = new MeshBuilder();
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    feet.AddRoundedBlock(new Vector3(sx * 0.25f, 0f, sz * 0.35f), 0.07f, 0.07f, 0.015f, 0.05f, 0.005f);
                }
            }
            // Square corners and no roof lip, so two cabinets side by side share one flush top and read as one unit.
            body.AddExtrusion(RoundedRectLoop(-width / 2f, width / 2f, -depth / 2f, core, 0f), new Vector3(0f, 0.05f, 0f), new Vector3(0f, height, 0f),
                Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            var holes = new List<Vector4> { new Vector4(-0.23f, 1.56f, 0.23f, 1.88f), new Vector4(-0.23f, 0.16f, 0.23f, 0.84f) };
            AddSkin(body, -width / 2f, width / 2f, 0.05f, height - 0.03f, core, front, holes);
            AddVent(slats, dark, -0.23f, 0.23f, 1.56f, 1.88f, core, front - 0.024f, front, 0.032f);
            AddVent(slats, dark, -0.23f, 0.23f, 0.16f, 0.84f, core, front - 0.024f, front, 0.032f);
            AddDoorJoint(trim, -0.28f, 0.28f, 0.10f, 1.94f, front);
            led.AddBox(new Vector3(-0.15f, 1.47f, front), new Vector3(0.15f, 1.482f, front + 0.002f), true, false);
            AddLeverLock(handleBase, handle, 0.2f, 1.02f, 0.85f, front);

            var panel = new ControlPanelMeshes();
            panel.Add(new Vector3(0f, 1.30f, front), 0.75f);
            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(0f, 1.02f, front), Vector3.left, Vector3.up, 0.30f, 0.15f, PlantLabel.UpsTag);

            body.Attach("Cabinet", root, SurfaceMaterial.UpsGraphite);
            feet.Attach("Feet", root, SurfaceMaterial.BlackHousing);
            trim.Attach("Joints", root, SurfaceMaterial.CabinetPull);
            slats.Attach("Vents", root, SurfaceMaterial.UpsGraphite);
            dark.Attach("VentBacks", root, SurfaceMaterial.BlackHousing);
            led.Attach("StatusStrip", root, SurfaceMaterial.SafetyGreen);
            handleBase.Attach("HandlePlate", root, SurfaceMaterial.BlackHousing);
            handle.Attach("Handle", root, SurfaceMaterial.Chrome);
            panel.Attach("Control", root);
            labels.Attach("Labels", root, SurfaceMaterial.PlantLabels);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }
    }
}
