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
        /// Cast-resin transformer in its ventilated enclosure, 2.0 x 2.2 x 1.2 m: louvered panels, a viewing window,
        /// a warning plate, and the medium-voltage cables leaving the roof in trunking up to the ceiling.
        /// </summary>
        private static void BuildTransformer(Transform root)
        {
            const float width = 2.0f;
            const float height = 2.2f;
            const float depth = 1.2f;
            GrayboxUtility.CreateStandingBox("Plinth", root, Vector3.zero, new Vector3(width - 0.05f, 0.1f, depth - 0.05f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("Enclosure", root, new Vector3(0f, 0.1f, 0f), new Vector3(width, height - 0.1f, depth), SurfaceMaterial.Trim, false);
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateStandingBox(side < 0 ? "Louvers_Low" : "Louvers_High", root, new Vector3(0f, side < 0 ? 0.25f : 1.75f, depth / 2f + 0.004f),
                    new Vector3(width - 0.2f, 0.3f, 0.008f), SurfaceMaterial.FloorVent, false);
            }
            GrayboxUtility.CreateStandingBox("Window", root, new Vector3(0f, 0.75f, depth / 2f + 0.004f), new Vector3(1.2f, 0.75f, 0.008f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateStandingBox("WarningPlate", root, new Vector3(0.7f, 1.55f, depth / 2f + 0.009f), new Vector3(0.2f, 0.2f, 0.002f), SurfaceMaterial.SafetyYellow, false);
            GrayboxUtility.CreateStandingBox("RatingPlate", root, new Vector3(-0.7f, 1.55f, depth / 2f + 0.009f), new Vector3(0.25f, 0.15f, 0.002f), SurfaceMaterial.SignFace, false);
            GrayboxUtility.CreateBoxBetween("CableTrunking", root, new Vector3(-0.2f, height, -0.4f), new Vector3(0.2f, RoomContract.CeilingHeight, -0.2f), SurfaceMaterial.Trunking);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }

        /// <summary>
        /// Low-voltage switchgear section 0.8 x 2.2 x 0.6 m: three breaker compartments with lever handles, a load
        /// ammeter on top (<see cref="AnalogGauge"/>) and indicator lamps; its busbar trunking rises to the ceiling.
        /// </summary>
        private static void BuildSwitchgearCabinet(Transform root)
        {
            const float width = 0.8f;
            const float height = 2.2f;
            const float depth = 0.6f;
            float front = depth / 2f;
            GrayboxUtility.CreateStandingBox("Cabinet", root, Vector3.zero, new Vector3(width, height, depth), SurfaceMaterial.Trim, false);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.15f + i * 0.5f;
                GrayboxUtility.CreateStandingBox($"Compartment_{i}", root, new Vector3(0f, y, front + 0.005f), new Vector3(width - 0.06f, 0.46f, 0.01f), SurfaceMaterial.Trim, false);
                GrayboxUtility.CreateStandingBox($"Breaker_{i}", root, new Vector3(0f, y + 0.15f, front + 0.02f), new Vector3(0.3f, 0.16f, 0.03f), SurfaceMaterial.Upholstery, false);
                GrayboxUtility.CreateStandingBox($"Lever_{i}", root, new Vector3(0.25f, y + 0.2f, front + 0.03f), new Vector3(0.03f, 0.1f, 0.04f), SurfaceMaterial.SafetyRed, false);
            }
            CreateGauge(root, "Ammeter", new Vector3(-0.15f, 1.85f, front + 0.01f), 0.08f);
            foreach (var (x, lamp) in new[] { (0.12f, SurfaceMaterial.SafetyGreen), (0.22f, SurfaceMaterial.LedRed) })
            {
                var light = GrayboxUtility.CreateCylinder($"Lamp_{lamp}", root, new Vector3(x, 1.85f, front + 0.015f), 0.02f, 0.02f, lamp);
                light.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            GrayboxUtility.CreateBoxBetween("BusbarTrunking", root, new Vector3(-0.15f, height, -0.2f), new Vector3(0.15f, RoomContract.CeilingHeight, 0.05f), SurfaceMaterial.Trunking);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }

        /// <summary>
        /// Wall panel of six analog meters (two rows of three) with a label strip under each: load and power per hall
        /// section in the power room, coolant temperatures and air flow in the cooling plant. A wall prop.
        /// </summary>
        private static void BuildAnalogMeterPanel(Transform root)
        {
            const float width = 1.2f;
            const float height = 0.8f;
            const float bottom = 1.2f;
            GrayboxUtility.CreateStandingBox("Board", root, new Vector3(0f, bottom, 0.02f), new Vector3(width, height, 0.04f), SurfaceMaterial.Trim, false);
            for (int row = 0; row < 2; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    float x = -0.38f + column * 0.38f;
                    float y = bottom + 0.58f - row * 0.36f;
                    CreateGauge(root, $"Gauge_{row}{column}", new Vector3(x, y, 0.04f), 0.12f);
                    GrayboxUtility.CreateBox($"Label_{row}{column}", root, new Vector3(x, y - 0.155f, 0.041f), new Vector3(0.18f, 0.035f, 0.002f), SurfaceMaterial.SignFace, false);
                }
            }
            GrayboxUtility.CreateBoxBetween("Conduit", root, new Vector3(-0.03f, bottom + height, 0f), new Vector3(0.03f, RoomContract.CeilingHeight, 0.05f), SurfaceMaterial.Trunking);
        }

        /// <summary>Dial thermometer or pressure gauge on a short stem; the pivot is where the stem meets the pipe, the dial faces +Z.</summary>
        private static void BuildPipeGauge(Transform root)
        {
            GrayboxUtility.CreatePipe("Stem", root, Vector3.zero, new Vector3(0f, 0f, 0.06f), 0.008f, SurfaceMaterial.Metal);
            CreateGauge(root, "Dial", new Vector3(0f, 0f, 0.06f), 0.05f);
        }

        /// <summary>
        /// A round analog meter facing +Z at <paramref name="center"/>: bezel, white face, a red needle turned by an
        /// <see cref="AnalogGauge"/> on the gauge object.
        /// </summary>
        private static Transform CreateGauge(Transform parent, string name, Vector3 center, float radius)
        {
            var gauge = GrayboxUtility.CreateGroup(name, parent);
            gauge.localPosition = center;
            var bezel = GrayboxUtility.CreateCylinder("Bezel", gauge, new Vector3(0f, 0f, 0.012f), radius, 0.024f, SurfaceMaterial.Metal);
            bezel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var face = GrayboxUtility.CreateCylinder("Face", gauge, new Vector3(0f, 0f, 0.0245f), radius - 0.008f, 0.002f, SurfaceMaterial.Whiteboard);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GrayboxUtility.CreateBox("Scale", gauge, new Vector3(0f, radius * 0.55f, 0.0258f), new Vector3(radius * 1.2f, radius * 0.06f, 0.0005f), SurfaceMaterial.Upholstery, false);

            var pivot = GrayboxUtility.CreateGroup("Needle", gauge);
            pivot.localPosition = new Vector3(0f, 0f, 0.027f);
            GrayboxUtility.CreateBox("Pointer", pivot, new Vector3(0f, radius * 0.4f, 0f), new Vector3(radius * 0.06f, radius * 0.8f, 0.002f), SurfaceMaterial.SafetyRed, false);

            var analog = gauge.gameObject.AddComponent<AnalogGauge>();
            var serialized = new SerializedObject(analog);
            serialized.FindProperty("needle").objectReferenceValue = pivot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gauge;
        }

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
                GrayboxUtility.CreatePipe($"{name}_In", root, new Vector3(x, 0.9f, front), new Vector3(x, 0.9f, front + 0.25f), 0.04f, SurfaceMaterial.PipeWater);
                GrayboxUtility.CreatePipe(name, root, new Vector3(x, 0.9f, front + 0.25f), new Vector3(x, ceiling, front + 0.25f), 0.04f, SurfaceMaterial.PipeWater);
                GrayboxUtility.CreateCylinder($"{name}_Valve", root, new Vector3(x, 1.3f, front + 0.25f), 0.055f, 0.1f, SurfaceMaterial.Metal);
                Place(GrayboxProp.PipeGauge, root, new Vector3(x, 1.7f, front + 0.29f), 0f, $"{name}_Thermometer");
            }
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }

        /// <summary>Inline circulation pump on a base frame (motor, pump housing), 0.5 x 0.7 x 0.4 m; the terminal box faces +Z.</summary>
        private static void BuildCirculationPump(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Frame", root, Vector3.zero, new Vector3(0.5f, 0.08f, 0.4f), SurfaceMaterial.Steel, false);
            GrayboxUtility.CreateCylinder("Housing", root, new Vector3(0f, 0.25f, 0f), 0.15f, 0.34f, SurfaceMaterial.PipeWater);
            GrayboxUtility.CreateCylinder("Motor", root, new Vector3(0f, 0.55f, 0f), 0.12f, 0.3f, SurfaceMaterial.DataCable);
            GrayboxUtility.CreateStandingBox("TerminalBox", root, new Vector3(0f, 0.5f, 0.13f), new Vector3(0.12f, 0.1f, 0.06f), SurfaceMaterial.Trim, false);
            AddBoxCollider(root, new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.7f, 0.4f));
        }

        /// <summary>
        /// Chilled-water buffer tank 0.8 m across and 2.2 m high on three feet (green, insulated), a dial thermometer
        /// on the front, flow and return connections rising from its top to the ceiling. A floor prop.
        /// </summary>
        private static void BuildBufferTank(Transform root)
        {
            const float radius = 0.4f;
            const float height = 2.2f;
            float ceiling = RoomContract.CeilingHeight;
            for (int i = 0; i < 3; i++)
            {
                var foot = GrayboxUtility.CreateGroup($"Foot_{i}", root);
                foot.localRotation = Quaternion.Euler(0f, i * 120f, 0f);
                GrayboxUtility.CreateStandingBox("Leg", foot, new Vector3(0f, 0f, radius - 0.08f), new Vector3(0.06f, 0.2f, 0.06f), SurfaceMaterial.Steel, false);
            }
            GrayboxUtility.CreateCylinder("Shell", root, new Vector3(0f, 0.2f + (height - 0.2f) / 2f, 0f), radius, height - 0.2f, SurfaceMaterial.PipeWater);
            GrayboxUtility.CreateCylinder("Band_Low", root, new Vector3(0f, 0.6f, 0f), radius + 0.008f, 0.04f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Band_High", root, new Vector3(0f, height - 0.3f, 0f), radius + 0.008f, 0.04f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateStandingBox("RatingPlate", root, new Vector3(0f, 1.6f, radius + 0.002f), new Vector3(0.2f, 0.12f, 0.004f), SurfaceMaterial.SignFace, false);
            Place(GrayboxProp.PipeGauge, root, new Vector3(0f, 1.2f, radius - 0.02f), 0f, "Thermometer");
            foreach (var (x, name) in new[] { (-0.15f, "Flow"), (0.15f, "Return") })
            {
                GrayboxUtility.CreatePipe(name, root, new Vector3(x, height, 0f), new Vector3(x, ceiling, 0f), 0.04f, SurfaceMaterial.PipeWater);
                GrayboxUtility.CreateCylinder($"{name}_Valve", root, new Vector3(x, height + 0.3f, 0f), 0.055f, 0.1f, SurfaceMaterial.Metal);
            }
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(2f * radius, height, 2f * radius));
        }

        /// <summary>
        /// Plate heat exchanger 1.4 x 1.6 x 0.6 m (chilled water to the hall's cooling loop): a fixed and a pressure frame
        /// with the plate pack clamped between them on two guide bars, four flanged ports on the fixed frame, and the
        /// primary and secondary flow and return pipes (green) rising from the ports along its back to the ceiling, so
        /// they lead into the risers above. The service side faces +Z; the pipes stay at the back (-Z).
        /// </summary>
        private static void BuildPlateHeatExchanger(Transform root)
        {
            const float length = 1.4f;
            const float height = 1.6f;
            const float depth = 0.6f;
            float ceiling = RoomContract.CeilingHeight;
            GrayboxUtility.CreateStandingBox("FixedFrame", root, new Vector3(-length / 2f + 0.05f, 0f, 0f), new Vector3(0.1f, height, depth), SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("PressureFrame", root, new Vector3(0.3f, 0.05f, 0f), new Vector3(0.08f, height - 0.15f, depth - 0.05f), SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("Support", root, new Vector3(length / 2f - 0.04f, 0f, 0f), new Vector3(0.08f, height, 0.12f), SurfaceMaterial.Steel, false);
            GrayboxUtility.CreateStandingBox("PlatePack", root, new Vector3(-0.17f, 0.15f, 0f), new Vector3(0.84f, height - 0.35f, depth - 0.12f), SurfaceMaterial.Metal, false);
            for (int i = 0; i < 9; i++)
            {
                GrayboxUtility.CreateStandingBox($"Plate_{i}", root, new Vector3(-0.55f + i * 0.095f, 0.14f, 0f), new Vector3(0.01f, height - 0.33f, depth - 0.1f), SurfaceMaterial.Steel, false);
            }
            foreach (float y in new[] { 0.12f, height - 0.1f })
            {
                GrayboxUtility.CreateStandingBox($"GuideBar_{y:0.0}", root, new Vector3(0f, y, 0f), new Vector3(length - 0.1f, 0.05f, 0.05f), SurfaceMaterial.Metal, false);
            }
            GrayboxUtility.CreateStandingBox("RatingPlate", root, new Vector3(-length / 2f + 0.05f, 1.1f, depth / 2f + 0.002f), new Vector3(0.08f, 0.12f, 0.004f), SurfaceMaterial.SignFace, false);
            // Four flanged ports on the back of the fixed frame and the plate pack (two low, two high), each with a pipe
            // that leaves backwards and rises behind the unit to the ceiling.
            var ports = new[] { (-0.55f, 0.35f, "PrimaryFlow"), (-0.35f, height - 0.3f, "PrimaryReturn"), (-0.1f, 0.35f, "SecondaryFlow"), (0.1f, height - 0.3f, "SecondaryReturn") };
            float rise = -depth / 2f - 0.1f;
            foreach (var (x, y, name) in ports)
            {
                var flange = GrayboxUtility.CreateCylinder($"{name}_Flange", root, new Vector3(x, y, -depth / 2f - 0.01f), 0.07f, 0.03f, SurfaceMaterial.Metal);
                flange.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                GrayboxUtility.CreatePipe($"{name}_Stub", root, new Vector3(x, y, -depth / 2f), new Vector3(x, y, rise), 0.04f, SurfaceMaterial.PipeWater);
                GrayboxUtility.CreatePipe($"{name}_Rise", root, new Vector3(x, y - 0.04f, rise), new Vector3(x, ceiling, rise), 0.04f, SurfaceMaterial.PipeWater);
            }
            CreateGauge(root, "Thermometer", new Vector3(0.3f, 1.2f, depth / 2f - 0.02f), 0.05f);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(length, height, depth));
        }
    }
}
