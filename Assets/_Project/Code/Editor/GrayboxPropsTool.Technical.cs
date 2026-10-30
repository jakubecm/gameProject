using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Plant of the technical rooms (power room, cooling plant) and service equipment. The analog gauges carry an
    /// <see cref="AnalogGauge"/>, so the run logic can set their readings as physical clues.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>
        /// Air handling unit with an air-to-water heat exchanger, 3.0 x 2.2 x 1.2 m: filter, coil and fan sections with
        /// access doors, chilled-water flow and return (green) with valves and thermometers rising to the ceiling, the
        /// supply duct on top. The service side faces +Z.
        /// </summary>
        private static void BuildAirHandlingUnit(Transform root)
        {
            const float width = 3.0f;
            const float height = 2.2f;
            const float depth = 1.2f;
            float front = depth / 2f;
            GrayboxUtility.CreateStandingBox("Base", root, Vector3.zero, new Vector3(width, 0.15f, depth), SurfaceMaterial.Steel, false);
            GrayboxUtility.CreateStandingBox("Casing", root, new Vector3(0f, 0.15f, 0f), new Vector3(width, height - 0.15f, depth), SurfaceMaterial.Wall, false);
            string[] sections = { "Filter", "Coil", "Fan" };
            for (int i = 0; i < sections.Length; i++)
            {
                float x = -width / 3f + i * width / 3f;
                GrayboxUtility.CreateStandingBox($"Door_{sections[i]}", root, new Vector3(x, 0.25f, front + 0.005f), new Vector3(width / 3f - 0.08f, height - 0.4f, 0.01f), SurfaceMaterial.Trim, false);
                GrayboxUtility.CreateStandingBox($"Handle_{sections[i]}", root, new Vector3(x + width / 6f - 0.12f, 1.1f, front + 0.02f), new Vector3(0.03f, 0.2f, 0.03f), SurfaceMaterial.Metal, false);
            }
            GrayboxUtility.CreateStandingBox("CoilFins", root, new Vector3(0f, 0.5f, front + 0.011f), new Vector3(0.7f, 1.0f, 0.004f), SurfaceMaterial.FloorVent, false);

            float ceiling = RoomContract.CeilingHeight;
            GrayboxUtility.CreateBoxBetween("SupplyDuct", root, new Vector3(0.6f, height, -0.4f), new Vector3(1.4f, ceiling, 0.3f), SurfaceMaterial.Metal);
            foreach (var (x, name) in new[] { (-0.35f, "ChilledWater_Flow"), (-0.1f, "ChilledWater_Return") })
            {
                RoomPipes.Pipe(root, name, new[] { new Vector3(x, 0.9f, front), new Vector3(x, 0.9f, front + 0.25f), new Vector3(x, ceiling, front + 0.25f) }, RoomPipes.WideRadius, SurfaceMaterial.PipeWater);
                RoomPipes.BallValve(root, $"{name}_Valve", new Vector3(x, 1.3f, front + 0.25f), Vector3.up, Vector3.forward, RoomPipes.WideRadius);
                Place(GrayboxProp.PipeGauge, root, new Vector3(x, 1.7f, front + 0.29f), 0f, $"{name}_Thermometer");
            }
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }

    }
}
