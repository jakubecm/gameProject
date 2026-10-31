using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>
        /// The thermometer wall of the cooling plant, opposite the door: six narrow pipes rise side by side from the floor, each with
        /// a large dial thermometer (three times life size, so the reading can be read in the game) on a brass stem. They bend under
        /// nested lanes, three left and three right, run along the back wall and turn into the end faces of the two heat exchangers
        /// (the exchangers' centres stand at x = +-2.33 m, their end faces at z = -0.55 m): every pipe ends in an exchanger. The needles
        /// are only a look. A wall prop.
        /// </summary>
        [Model(Prop.CoolingMeterPanel)]
        private static void BuildCoolingMeterPanel(Transform root)
        {
            float r = RoomPipes.NarrowRadius;
            const float gap = 0.04f;
            float z = r + gap;
            const float dialRadius = 0.15f;
            const float dialHeight = 1.15f;
            const float exchangerEnd = -0.57f;
            float[] x = { -0.95f, -0.57f, -0.19f, 0.19f, 0.57f, 0.95f };
            float[] lane = { 1.50f, 1.65f, 1.80f, 1.80f, 1.65f, 1.50f };
            float[] into = { -2.55f, -2.35f, -2.15f, 2.15f, 2.35f, 2.55f };
            float[] readings = { 0.26f, 0.31f, 0.28f, 0.37f, 0.33f, 0.42f };
            var dials = new DialMeshes();
            var strap = new MeshBuilder();
            var screws = new MeshBuilder();
            var wall = Vector3.forward;
            for (int i = 0; i < x.Length; i++)
            {
                RoomPipes.Pipe(root, $"Pipe_{i}", new[]
                {
                    new Vector3(x[i], 0f, z), new Vector3(x[i], lane[i], z), new Vector3(into[i], lane[i], z), new Vector3(into[i], lane[i], exchangerEnd),
                }, r, SurfaceMaterial.PipeWater);
                RoomInfrastructure.AddRosette(root, $"Rosette_{i}", new Vector3(x[i], 0f, z), r, false);
                RoomPipes.Clamp(strap, screws, new Vector3(x[i], 0.45f, z), Vector3.up, wall, r, gap);
                RoomPipes.Clamp(strap, screws, new Vector3(x[i], 1.42f, z), Vector3.up, wall, r, gap);
                float direction = Mathf.Sign(into[i] - x[i]);
                for (float at = x[i] + direction * 0.7f; Mathf.Abs(at - into[i]) > 0.35f; at += direction * 0.8f)
                {
                    RoomPipes.Clamp(strap, screws, new Vector3(at, lane[i], z), Vector3.right, wall, r, gap);
                }
                ModelUtility.CreateGroup($"Thermometer_{i}", root).localPosition = new Vector3(x[i], dialHeight, z + r + 0.07f);
                dials.AddDial(new Vector3(x[i], dialHeight, z + r + 0.07f), dialRadius, 0.07f, readings[i]);
            }
            strap.Attach("Clamps", root, SurfaceMaterial.PipeClamp);
            screws.Attach("Screws", root, SurfaceMaterial.Chrome);
            dials.Attach("Dials", root);
        }
    }
}
