using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Office furniture and the small things on desks and shelves that tell who worked here. Same pivot conventions
    /// as the other props: floor props on the floor, centered, +Z the side people use; desk-top props on the surface.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>Height of a shelf board of <see cref="BuildShelf"/> that small props can stand on.</summary>
        public static readonly float[] ShelfBoardTops = { 0.12f, 0.57f, 1.02f, 1.47f };

        /// <summary>Height of the worktop of the coffee counter.</summary>
        public const float CounterHeight = 0.9f;

        /// <summary>PC tower 0.2 x 0.45 x 0.45 m standing beside a desk; the front with the power button faces +Z.</summary>
        private static void BuildPcTower(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Case", root, Vector3.zero, new Vector3(0.2f, 0.45f, 0.45f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("Front", root, new Vector3(0f, 0.02f, 0.226f), new Vector3(0.18f, 0.41f, 0.003f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateStandingBox("PowerLed", root, new Vector3(0f, 0.4f, 0.229f), new Vector3(0.012f, 0.012f, 0.002f), SurfaceMaterial.SafetyGreen, false);
            AddBoxCollider(root, new Vector3(0f, 0.225f, 0f), new Vector3(0.2f, 0.45f, 0.45f));
        }

        /// <summary>Two monitors angled towards the user, one keyboard and a mouse to its right; on a desk top, screens face +Z.</summary>
        private static void BuildDualWorkstation(Transform root)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var monitor = GrayboxUtility.CreateGroup(side < 0 ? "Monitor_Left" : "Monitor_Right", root);
                monitor.localPosition = new Vector3(side * 0.32f, 0f, -0.12f);
                monitor.localRotation = Quaternion.Euler(0f, -side * 12f, 0f);
                GrayboxUtility.CreateStandingBox("Foot", monitor, new Vector3(0f, 0f, -0.02f), new Vector3(0.22f, 0.015f, 0.18f), SurfaceMaterial.Metal, false);
                GrayboxUtility.CreateStandingBox("Neck", monitor, new Vector3(0f, 0.015f, -0.05f), new Vector3(0.04f, 0.14f, 0.025f), SurfaceMaterial.Metal, false);
                GrayboxUtility.CreateStandingBox("Screen", monitor, new Vector3(0f, 0.11f, -0.03f), new Vector3(0.62f, 0.37f, 0.025f), SurfaceMaterial.Screen, false);
            }
            GrayboxUtility.CreateStandingBox("Keyboard", root, new Vector3(0f, 0f, 0.18f), new Vector3(0.44f, 0.02f, 0.14f), SurfaceMaterial.Screen, false);
            BuildMouse(root, new Vector3(-0.3f, 0f, 0.19f));
        }

        /// <summary>65" television on a flat wall bracket, screen center at 1.6 m; a wall prop.</summary>
        private static void BuildWallTv(Transform root)
        {
            GrayboxUtility.CreateBox("Bracket", root, new Vector3(0f, WallScreenCenter, 0.015f), new Vector3(0.4f, 0.3f, 0.03f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateBox("Bezel", root, new Vector3(0f, WallScreenCenter, 0.05f), new Vector3(1.46f, 0.84f, 0.04f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateBox("Screen", root, new Vector3(0f, WallScreenCenter, 0.0705f), new Vector3(1.43f, 0.81f, 0.001f), SurfaceMaterial.Screen, false);
        }

        /// <summary>
        /// Kitchenette 1.8 m long against a wall: base cabinets with a worktop, a stainless sink with a deep basin and a
        /// mixer tap, wall cabinets above. The front faces +Z; coffee machine and mugs stand on the worktop
        /// (<see cref="CounterHeight"/>) left of the sink.
        /// </summary>
        private static void BuildCoffeeCounter(Transform root)
        {
            const float width = 1.8f;
            const float depth = 0.6f;
            const float top = 0.04f;
            const float basinDepth = 0.18f;
            float worktop = CounterHeight - top;
            // The sink: a 0.45 x 0.36 m opening in the worktop, centered at x = 0.45.
            const float sinkX0 = 0.225f;
            const float sinkX1 = 0.675f;
            const float sinkZ0 = -0.16f;
            const float sinkZ1 = 0.2f;
            float basinBottom = CounterHeight - basinDepth;

            // A closed carcass: plinth, sides, back and bottom, doors over the whole front from the plinth to the worktop.
            // The basin hangs inside it, so no gap shows anywhere under the worktop.
            const float panel = 0.018f;
            float doorTop = worktop;
            float front = depth / 2f - 0.02f;
            GrayboxUtility.CreateStandingBox("Plinth", root, new Vector3(0f, 0f, -0.02f), new Vector3(width, 0.1f, depth - 0.06f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateBoxBetween("Carcass_Bottom", root, new Vector3(-width / 2f, 0.1f, -depth / 2f), new Vector3(width / 2f, 0.1f + panel, front), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Back", root, new Vector3(-width / 2f, 0.1f, -depth / 2f), new Vector3(width / 2f, worktop, -depth / 2f + panel), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Left", root, new Vector3(-width / 2f, 0.1f, -depth / 2f), new Vector3(-width / 2f + panel, worktop, front), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Right", root, new Vector3(width / 2f - panel, 0.1f, -depth / 2f), new Vector3(width / 2f, worktop, front), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Divider", root, new Vector3(sinkX0 - 0.025f - panel / 2f, 0.1f, -depth / 2f), new Vector3(sinkX0 - 0.025f + panel / 2f, worktop, front), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Base", root, new Vector3(-width / 2f + panel, 0.1f + panel, -depth / 2f + panel), new Vector3(sinkX0 - 0.025f - panel / 2f, worktop, front), SurfaceMaterial.Furniture);
            for (int i = 0; i < 3; i++)
            {
                float x = -width / 2f + width / 6f * (2 * i + 1);
                GrayboxUtility.CreateBoxBetween($"Door_{i}", root, new Vector3(x - width / 6f + 0.003f, 0.1f, front), new Vector3(x + width / 6f - 0.003f, doorTop, front + 0.018f), SurfaceMaterial.Trunking);
                GrayboxUtility.CreateStandingBox($"Handle_{i}", root, new Vector3(x, doorTop - 0.08f, front + 0.028f), new Vector3(0.12f, 0.015f, 0.015f), SurfaceMaterial.Metal, false);
            }

            // Worktop in four pieces around the sink opening.
            GrayboxUtility.CreateBoxBetween("Worktop_Left", root, new Vector3(-width / 2f, worktop, -depth / 2f), new Vector3(sinkX0, CounterHeight, depth / 2f), SurfaceMaterial.Trim);
            GrayboxUtility.CreateBoxBetween("Worktop_Right", root, new Vector3(sinkX1, worktop, -depth / 2f), new Vector3(width / 2f, CounterHeight, depth / 2f), SurfaceMaterial.Trim);
            GrayboxUtility.CreateBoxBetween("Worktop_Back", root, new Vector3(sinkX0, worktop, -depth / 2f), new Vector3(sinkX1, CounterHeight, sinkZ0), SurfaceMaterial.Trim);
            GrayboxUtility.CreateBoxBetween("Worktop_Front", root, new Vector3(sinkX0, worktop, sinkZ1), new Vector3(sinkX1, CounterHeight, depth / 2f), SurfaceMaterial.Trim);

            // Stainless basin hanging in the opening, with a rim around it and a drain.
            const float wall = 0.008f;
            GrayboxUtility.CreateBoxBetween("Basin_Bottom", root, new Vector3(sinkX0, basinBottom, sinkZ0), new Vector3(sinkX1, basinBottom + wall, sinkZ1), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Basin_Left", root, new Vector3(sinkX0, basinBottom, sinkZ0), new Vector3(sinkX0 + wall, CounterHeight, sinkZ1), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Basin_Right", root, new Vector3(sinkX1 - wall, basinBottom, sinkZ0), new Vector3(sinkX1, CounterHeight, sinkZ1), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Basin_Back", root, new Vector3(sinkX0, basinBottom, sinkZ0), new Vector3(sinkX1, CounterHeight, sinkZ0 + wall), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Basin_Front", root, new Vector3(sinkX0, basinBottom, sinkZ1 - wall), new Vector3(sinkX1, CounterHeight, sinkZ1), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Basin_Rim", root, new Vector3(sinkX0 - 0.02f, CounterHeight, sinkZ0 - 0.02f), new Vector3(sinkX1 + 0.02f, CounterHeight + 0.004f, sinkZ0), SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Drain", root, new Vector3((sinkX0 + sinkX1) / 2f, basinBottom + wall + 0.002f, (sinkZ0 + sinkZ1) / 2f), 0.03f, 0.004f, SurfaceMaterial.Upholstery);

            // Single-lever mixer tap on the back strip: base, column, spout over the basin, lever on top.
            float tapX = (sinkX0 + sinkX1) / 2f;
            float tapZ = (-depth / 2f + sinkZ0) / 2f;
            GrayboxUtility.CreateCylinder("Tap_Base", root, new Vector3(tapX, CounterHeight + 0.015f, tapZ), 0.025f, 0.03f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Tap_Column", root, new Vector3(tapX, CounterHeight + 0.14f, tapZ), 0.014f, 0.22f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Tap_Spout", root, new Vector3(tapX - 0.012f, CounterHeight + 0.225f, tapZ), new Vector3(tapX + 0.012f, CounterHeight + 0.25f, tapZ + 0.2f), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Tap_Outlet", root, new Vector3(tapX - 0.012f, CounterHeight + 0.2f, tapZ + 0.18f), new Vector3(tapX + 0.012f, CounterHeight + 0.225f, tapZ + 0.2f), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Tap_Lever", root, new Vector3(tapX - 0.008f, CounterHeight + 0.25f, tapZ - 0.06f), new Vector3(tapX + 0.008f, CounterHeight + 0.27f, tapZ + 0.01f), SurfaceMaterial.Metal);

            GrayboxUtility.CreateStandingBox("WallCabinets", root, new Vector3(0f, 1.5f, -depth / 2f + 0.175f), new Vector3(width, 0.7f, 0.35f), SurfaceMaterial.Trunking, false);
            AddBoxCollider(root, new Vector3(0f, CounterHeight / 2f, 0f), new Vector3(width, CounterHeight, depth));
        }

        /// <summary>Where the coffee machine stands on the worktop (counter-local), left of the sink.</summary>
        public static readonly Vector3 CounterMachinePosition = new Vector3(-0.55f, CounterHeight, -0.05f);

        /// <summary>
        /// Office multifunction printer 0.6 x 1.15 x 0.65 m on its paper cabinet: drawers, output tray, scanner lid and a
        /// control panel. The front faces +Z.
        /// </summary>
        private static void BuildPrinter(Transform root)
        {
            const float width = 0.6f;
            const float depth = 0.65f;
            float front = depth / 2f;
            GrayboxUtility.CreateStandingBox("Cabinet", root, Vector3.zero, new Vector3(width, 0.45f, depth), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("Engine", root, new Vector3(0f, 0.45f, 0f), new Vector3(width, 0.5f, depth), SurfaceMaterial.Trunking, false);
            for (int i = 0; i < 3; i++)
            {
                float y = 0.06f + i * 0.13f;
                GrayboxUtility.CreateStandingBox($"Drawer_{i}", root, new Vector3(0f, y, front + 0.004f), new Vector3(width - 0.06f, 0.11f, 0.008f), SurfaceMaterial.Trunking, false);
                GrayboxUtility.CreateStandingBox($"DrawerHandle_{i}", root, new Vector3(0f, y + 0.08f, front + 0.012f), new Vector3(0.2f, 0.015f, 0.01f), SurfaceMaterial.Upholstery, false);
            }
            GrayboxUtility.CreateStandingBox("OutputTray", root, new Vector3(0f, 0.8f, 0.05f), new Vector3(width - 0.12f, 0.01f, depth - 0.2f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("PrintedSheets", root, new Vector3(0.02f, 0.81f, 0.05f), new Vector3(0.21f, 0.01f, 0.297f), SurfaceMaterial.Whiteboard, false);
            GrayboxUtility.CreateStandingBox("Scanner", root, new Vector3(0f, 0.95f, 0f), new Vector3(width, 0.14f, depth), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateStandingBox("Lid", root, new Vector3(0f, 1.09f, -0.02f), new Vector3(width - 0.04f, 0.05f, depth - 0.08f), SurfaceMaterial.Upholstery, false);
            var panel = GrayboxUtility.CreateStandingBox("ControlPanel", root, new Vector3(0.12f, 1.0f, front + 0.06f), new Vector3(0.28f, 0.03f, 0.14f), SurfaceMaterial.Screen, false);
            panel.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            GrayboxUtility.CreateStandingBox("StatusLed", root, new Vector3(-0.2f, 0.98f, front + 0.002f), new Vector3(0.02f, 0.01f, 0.004f), SurfaceMaterial.SafetyGreen, false);
            AddBoxCollider(root, new Vector3(0f, 0.57f, 0.03f), new Vector3(width, 1.14f, depth + 0.06f));
        }

        /// <summary>Kitchen fridge 0.6 x 1.8 x 0.65 m with a freezer door below and a handle on each door; the doors face +Z.</summary>
        private static void BuildFridge(Transform root)
        {
            const float width = 0.6f;
            const float height = 1.8f;
            const float depth = 0.65f;
            float front = depth / 2f;
            GrayboxUtility.CreateStandingBox("Body", root, Vector3.zero, new Vector3(width, height, depth), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateStandingBox("Door_Freezer", root, new Vector3(0f, 0.04f, front + 0.01f), new Vector3(width - 0.01f, 0.6f, 0.02f), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateStandingBox("Door_Fridge", root, new Vector3(0f, 0.67f, front + 0.01f), new Vector3(width - 0.01f, height - 0.69f, 0.02f), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateStandingBox("Handle_Freezer", root, new Vector3(-0.25f, 0.4f, front + 0.04f), new Vector3(0.02f, 0.2f, 0.02f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Handle_Fridge", root, new Vector3(-0.25f, 0.8f, front + 0.04f), new Vector3(0.02f, 0.3f, 0.02f), SurfaceMaterial.Metal, false);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }

        /// <summary>Round standing table by the coffee corner, 0.7 m across and 1.05 m high, on a pedestal and a round foot.</summary>
        private static void BuildCoffeeTable(Transform root)
        {
            const float height = 1.05f;
            const float radius = 0.35f;
            GrayboxUtility.CreateCylinder("Foot", root, new Vector3(0f, 0.01f, 0f), 0.25f, 0.02f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Pedestal", root, new Vector3(0f, (height - 0.03f) / 2f, 0f), 0.035f, height - 0.05f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Top", root, new Vector3(0f, height - 0.015f, 0f), radius, 0.03f, SurfaceMaterial.Furniture);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(2f * radius, height, 2f * radius));
        }

        /// <summary>Height of the top of the standing coffee table.</summary>
        public const float CoffeeTableHeight = 1.05f;

        /// <summary>Bean-to-cup coffee machine 0.3 x 0.4 x 0.4 m; a desk-top prop, its front faces +Z.</summary>
        private static void BuildCoffeeMachine(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Body", root, new Vector3(0f, 0f, -0.03f), new Vector3(0.3f, 0.4f, 0.36f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("DripTray", root, new Vector3(0f, 0f, 0.17f), new Vector3(0.22f, 0.02f, 0.08f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Spout", root, new Vector3(0f, 0.17f, 0.16f), new Vector3(0.06f, 0.05f, 0.06f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Display", root, new Vector3(0f, 0.3f, 0.151f), new Vector3(0.14f, 0.06f, 0.002f), SurfaceMaterial.SafetyGreen, false);
            BuildMug(GrayboxUtility.CreateGroup("Cup", root), new Vector3(0f, 0.02f, 0.17f));
        }

        private static void BuildMug(Transform root) => BuildMug(root, Vector3.zero);

        /// <summary>A coffee mug standing on <paramref name="position"/>.</summary>
        private static void BuildMug(Transform root, Vector3 position)
        {
            GrayboxUtility.CreateCylinder("Body", root, position + new Vector3(0f, 0.0475f, 0f), 0.04f, 0.095f, SurfaceMaterial.Whiteboard);
            GrayboxUtility.CreateBox("Handle", root, position + new Vector3(0.05f, 0.05f, 0f), new Vector3(0.015f, 0.05f, 0.012f), SurfaceMaterial.Whiteboard, false);
        }

        /// <summary>Steel clothes locker 0.4 x 1.8 x 0.5 m with vent slots and a handle; the door faces +Z.</summary>
        private static void BuildLocker(Transform root)
        {
            const float width = 0.4f;
            const float height = 1.8f;
            const float depth = 0.5f;
            GrayboxUtility.CreateStandingBox("Body", root, Vector3.zero, new Vector3(width, height, depth), SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("Door", root, new Vector3(0f, 0.03f, depth / 2f + 0.004f), new Vector3(width - 0.03f, height - 0.06f, 0.008f), SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("Vents", root, new Vector3(0f, 1.5f, depth / 2f + 0.009f), new Vector3(0.2f, 0.12f, 0.002f), SurfaceMaterial.FloorVent, false);
            GrayboxUtility.CreateStandingBox("Handle", root, new Vector3(0.13f, 0.95f, depth / 2f + 0.015f), new Vector3(0.02f, 0.1f, 0.015f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("NamePlate", root, new Vector3(0f, 1.3f, depth / 2f + 0.009f), new Vector3(0.1f, 0.03f, 0.002f), SurfaceMaterial.SignFace, false);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.01f), new Vector3(width, height, depth + 0.02f));
        }

        /// <summary>Changing-room bench 1.2 x 0.45 x 0.35 m: a slatted seat on two steel frames.</summary>
        private static void BuildLockerBench(Transform root)
        {
            const float length = 1.2f;
            for (int i = 0; i < 3; i++)
            {
                GrayboxUtility.CreateStandingBox($"Slat_{i}", root, new Vector3(0f, 0.42f, -0.12f + i * 0.12f), new Vector3(length, 0.03f, 0.1f), SurfaceMaterial.Furniture, false);
            }
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateStandingBox(side < 0 ? "Frame_Left" : "Frame_Right", root, new Vector3(side * (length / 2f - 0.1f), 0f, 0f), new Vector3(0.04f, 0.42f, 0.32f), SurfaceMaterial.Metal, false);
            }
            AddBoxCollider(root, new Vector3(0f, 0.225f, 0f), new Vector3(length, 0.45f, 0.35f));
        }

        /// <summary>Five lever-arch binders in a row (spines towards +Z), standing on a shelf or desk.</summary>
        private static void BuildBinders(Transform root)
        {
            SurfaceMaterial[] colours = { SurfaceMaterial.DataCable, SurfaceMaterial.SafetyRed, SurfaceMaterial.Upholstery, SurfaceMaterial.DataCable, SurfaceMaterial.Whiteboard };
            for (int i = 0; i < colours.Length; i++)
            {
                float x = -0.16f + i * 0.08f;
                GrayboxUtility.CreateStandingBox($"Binder_{i}", root, new Vector3(x, 0f, 0f), new Vector3(0.075f, 0.32f, 0.28f), colours[i], false);
                GrayboxUtility.CreateStandingBox($"Label_{i}", root, new Vector3(x, 0.18f, 0.1405f), new Vector3(0.05f, 0.09f, 0.001f), SurfaceMaterial.SignFace, false);
            }
        }

        /// <summary>A stack of printouts and a folder, slightly askew.</summary>
        private static void BuildPaperStack(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Folder", root, Vector3.zero, new Vector3(0.24f, 0.012f, 0.32f), SurfaceMaterial.SafetyYellow, false);
            var stack = GrayboxUtility.CreateStandingBox("Paper", root, new Vector3(0.01f, 0.012f, 0f), new Vector3(0.21f, 0.03f, 0.297f), SurfaceMaterial.Whiteboard, false);
            stack.transform.localRotation = Quaternion.Euler(0f, 7f, 0f);
        }

        /// <summary>Photo frame 13 x 18 cm on its stand, picture facing +Z.</summary>
        private static void BuildPhotoFrame(Transform root)
        {
            var frame = GrayboxUtility.CreateGroup("Frame", root);
            frame.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            GrayboxUtility.CreateStandingBox("Border", frame, Vector3.zero, new Vector3(0.15f, 0.2f, 0.015f), SurfaceMaterial.Furniture, false);
            GrayboxUtility.CreateStandingBox("Picture", frame, new Vector3(0f, 0.02f, 0.008f), new Vector3(0.11f, 0.16f, 0.001f), SurfaceMaterial.SignFace, false);
            GrayboxUtility.CreateStandingBox("Stand", root, new Vector3(0f, 0f, -0.05f), new Vector3(0.02f, 0.15f, 0.01f), SurfaceMaterial.Furniture, false);
        }
    }
}
