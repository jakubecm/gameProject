using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Server racks (0.6 x 2.0 x 1.2 m, <see cref="RoomContract"/>). The cabinet is closed on the sides, the back and
    /// the top; the front is filled with equipment, with the odd blanking panel. The pivot is on the floor under the
    /// middle of the rack, +Z is the front. Racks are placed on rack slots at runtime (task 2.9); the preview scene
    /// puts them on every slot.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        internal enum RackKind
        {
            Compute,
            Gpu,
            Jbod,
            Network,
        }

        /// <summary>Height of one rack unit (1U = 44.45 mm).</summary>
        private const float RackUnit = 0.04445f;

        private const int RackUnits = 42;

        /// <summary>What fills the 42 U of each kind of rack, from the bottom: (height in U, what it is).</summary>
        private static IEnumerable<(int units, RackItem item)> RackContents(RackKind kind)
        {
            switch (kind)
            {
                case RackKind.Compute:
                    yield return (2, RackItem.Power);
                    for (int i = 0; i < 8; i++) yield return (2, RackItem.Server);
                    yield return (1, RackItem.Blank);
                    for (int i = 0; i < 10; i++) yield return (2, RackItem.Server);
                    yield return (1, RackItem.Blank);
                    yield return (1, RackItem.Switch);
                    break;
                case RackKind.Gpu:
                    yield return (2, RackItem.Power);
                    for (int i = 0; i < 4; i++) yield return (4, RackItem.GpuChassis);
                    yield return (2, RackItem.Blank);
                    for (int i = 0; i < 4; i++) yield return (4, RackItem.GpuChassis);
                    yield return (4, RackItem.Blank);
                    yield return (2, RackItem.Switch);
                    break;
                case RackKind.Jbod:
                    yield return (2, RackItem.Power);
                    yield return (2, RackItem.Server);
                    for (int i = 0; i < 4; i++) yield return (4, RackItem.DiskShelf);
                    yield return (2, RackItem.Blank);
                    for (int i = 0; i < 4; i++) yield return (4, RackItem.DiskShelf);
                    yield return (2, RackItem.Server);
                    break;
                default:
                    yield return (2, RackItem.Power);
                    for (int i = 0; i < 12; i++)
                    {
                        yield return (1, RackItem.Switch);
                        yield return (1, RackItem.CableManager);
                    }
                    yield return (4, RackItem.Blank);
                    for (int i = 0; i < 4; i++) yield return (3, RackItem.Patch);
                    break;
            }
        }

        private enum RackItem
        {
            Blank,
            Power,
            Server,
            GpuChassis,
            DiskShelf,
            Switch,
            CableManager,
            Patch,
        }

        private static void BuildRack(Transform root, RackKind kind)
        {
            float width = RoomContract.RackWidth;
            float height = RoomContract.RackHeight;
            float depth = RoomContract.RackDepth;
            float front = depth / 2f;
            const float frame = 0.06f;
            const float mount = 0.45f;

            // The closed cabinet: sides, back and top in one box, a plinth below.
            GrayboxUtility.CreateStandingBox("Cabinet", root, new Vector3(0f, 0.05f, -0.01f), new Vector3(width, height - 0.05f, depth - 0.02f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("Plinth", root, Vector3.zero, new Vector3(width - 0.02f, 0.05f, depth - 0.06f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateStandingBox("Frame_Left", root, new Vector3(-(width - frame) / 2f, 0.05f, front - 0.01f), new Vector3(frame, height - 0.05f, 0.02f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("Frame_Right", root, new Vector3((width - frame) / 2f, 0.05f, front - 0.01f), new Vector3(frame, height - 0.05f, 0.02f), SurfaceMaterial.Upholstery, false);

            float bottom = 0.05f + (height - 0.05f - RackUnits * RackUnit) / 2f;
            int unit = 0;
            int index = 0;
            foreach (var (units, item) in RackContents(kind))
            {
                float y = bottom + unit * RackUnit;
                float h = units * RackUnit - 0.003f;
                BuildRackItem(root, item, $"{item}_{index++:00}", y, h, mount, front);
                unit += units;
            }

            // Status LEDs down the left frame, as on the PDU of a live rack.
            GrayboxUtility.CreateStandingBox("StatusLeds", root, new Vector3(-(width - frame) / 2f, 1.75f, front + 0.001f), new Vector3(0.01f, 0.12f, 0.004f), SurfaceMaterial.SafetyGreen, false);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }

        private static void BuildRackItem(Transform root, RackItem item, string name, float y, float h, float width, float front)
        {
            float faceZ = front + 0.006f;
            switch (item)
            {
                case RackItem.Blank:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, front - 0.002f), new Vector3(width, h, 0.004f), SurfaceMaterial.Upholstery, false);
                    break;
                case RackItem.Power:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, faceZ), new Vector3(width, h, 0.012f), SurfaceMaterial.Trim, false);
                    GrayboxUtility.CreateStandingBox($"{name}_Display", root, new Vector3(0.15f, y + h * 0.3f, faceZ + 0.0065f), new Vector3(0.06f, h * 0.4f, 0.001f), SurfaceMaterial.SafetyGreen, false);
                    break;
                case RackItem.Server:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, faceZ), new Vector3(width, h, 0.012f), SurfaceMaterial.Metal, false);
                    GrayboxUtility.CreateStandingBox($"{name}_Bays", root, new Vector3(-0.06f, y + 0.008f, faceZ + 0.0065f), new Vector3(0.26f, h - 0.016f, 0.001f), SurfaceMaterial.Screen, false);
                    break;
                case RackItem.GpuChassis:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, faceZ), new Vector3(width, h, 0.012f), SurfaceMaterial.Trim, false);
                    GrayboxUtility.CreateStandingBox($"{name}_Intake", root, new Vector3(0f, y + 0.015f, faceZ + 0.0065f), new Vector3(width - 0.06f, h - 0.03f, 0.001f), SurfaceMaterial.FloorVent, false);
                    break;
                case RackItem.DiskShelf:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, faceZ), new Vector3(width, h, 0.012f), SurfaceMaterial.Furniture, false);
                    GrayboxUtility.CreateStandingBox($"{name}_Drives", root, new Vector3(0f, y + 0.01f, faceZ + 0.0065f), new Vector3(width - 0.04f, h - 0.02f, 0.001f), SurfaceMaterial.CeilingGrid, false);
                    break;
                case RackItem.Switch:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, faceZ), new Vector3(width, h, 0.012f), SurfaceMaterial.Screen, false);
                    GrayboxUtility.CreateStandingBox($"{name}_Ports", root, new Vector3(0f, y + h * 0.25f, faceZ + 0.0065f), new Vector3(width - 0.08f, h * 0.5f, 0.001f), SurfaceMaterial.DataCable, false);
                    break;
                case RackItem.CableManager:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, faceZ + 0.02f), new Vector3(width, h, 0.05f), SurfaceMaterial.Upholstery, false);
                    break;
                default:
                    GrayboxUtility.CreateStandingBox(name, root, new Vector3(0f, y, faceZ), new Vector3(width, h, 0.012f), SurfaceMaterial.Trim, false);
                    // A loom of blue patch cords leaving the panel sideways into the cable managers.
                    GrayboxUtility.CreateStandingBox($"{name}_Cords", root, new Vector3(0f, y + h * 0.3f, faceZ + 0.03f), new Vector3(width - 0.02f, h * 0.4f, 0.04f), SurfaceMaterial.DataCable, false);
                    break;
            }
        }
    }
}
