using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Office furniture and the small things on desks and shelves that tell who worked here. Same pivot conventions
    /// as the other props: floor props on the floor, centered, +Z the side people use; desk-top props on the surface.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>Height of the worktop of the coffee counter.</summary>
        public const float CounterHeight = 0.9f;

        /// <summary>Two monitors angled towards the user, one keyboard and a mouse to its right; on a desk top, screens face +Z.</summary>
        private static void BuildDualWorkstation(Transform root)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var monitor = GrayboxUtility.CreateGroup(side < 0 ? "Monitor_Left" : "Monitor_Right", root);
                monitor.localPosition = new Vector3(side * 0.268f, 0f, -0.10f);
                monitor.localRotation = Quaternion.Euler(0f, -side * 12f, 0f);
                BuildMonitor(monitor, Vector3.zero);
            }
            BuildKeyboard(root, new Vector3(0f, 0f, 0.18f));
            BuildMouse(root, new Vector3(-0.3f, 0f, 0.19f));
        }

        /// <summary>
        /// 65" display on a flat wall bracket, after a product photo: a very thin matt black slab (1.46 x 0.84 m, 28 mm deep, rounded
        /// edges) with a slim bezel round a dark glass screen. The screen centre is at 1.6 m; a wall prop.
        /// </summary>
        private static void BuildWallTv(Transform root)
        {
            const float depth = 0.028f;
            const float back = 0.012f;
            var body = new GrayboxMeshBuilder();
            var screen = new GrayboxMeshBuilder();
            var bracket = new GrayboxMeshBuilder();
            bracket.AddBox(new Vector3(-0.2f, WallScreenCenter - 0.15f, 0f), new Vector3(0.2f, WallScreenCenter + 0.15f, back));
            body.AddRoundedSlab(new Vector3(0f, WallScreenCenter, back), 1.46f, 0.84f, 0.006f, depth, 0.003f, 3, true);
            float z = back + depth + 0.0003f;
            screen.AddPanel(new Vector3(-0.7225f, WallScreenCenter - 0.4025f, z), new Vector3(0.7225f, WallScreenCenter - 0.4025f, z),
                new Vector3(0.7225f, WallScreenCenter + 0.4025f, z), new Vector3(-0.7225f, WallScreenCenter + 0.4025f, z), Vector3.forward,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            bracket.Attach("Bracket", root, SurfaceMaterial.Metal);
            body.Attach("Body", root, SurfaceMaterial.BlackHousing);
            screen.Attach("Screen", root, SurfaceMaterial.Screen);
        }

        /// <summary>
        /// Kitchenette 1.8 m long against a wall, after a product photo of a modern kitchen: white high-gloss base units (three
        /// modules of 0.6 m: three drawers, two drawers, and the sink unit with a drawer front over a door), long thin steel bar
        /// handles, a recessed plinth, a light stone worktop, a dark double-bowl sink with a matt dark gooseneck tap, and wall
        /// cabinets above. The front faces +Z; coffee machine and mugs stand on the worktop (<see cref="CounterHeight"/>) left of the sink.
        /// </summary>
        private static void BuildCoffeeCounter(Transform root)
        {
            const float width = 1.8f;
            const float depth = 0.6f;
            const float top = 0.04f;
            const float plinth = 0.1f;
            const float panel = 0.018f;
            float worktop = CounterHeight - top;
            float front = depth / 2f - 0.02f;
            // The sink: a 0.52 x 0.34 m opening in the worktop in the right-hand module, centered at x = 0.6.
            const float sinkX0 = 0.34f;
            const float sinkX1 = 0.86f;
            const float sinkZ0 = -0.15f;
            const float sinkZ1 = 0.19f;

            // Recessed plinth and the carcasses: solid for the two drawer modules, hollow under the sink (the basin hangs in it).
            GrayboxUtility.CreateStandingBox("Plinth", root, new Vector3(0f, 0f, -0.02f), new Vector3(width, plinth, depth - 0.06f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateBoxBetween("Carcass_Drawers", root, new Vector3(-width / 2f, plinth, -depth / 2f), new Vector3(0.3f, worktop, front), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Bottom", root, new Vector3(0.3f, plinth, -depth / 2f), new Vector3(width / 2f, plinth + panel, front), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Back", root, new Vector3(0.3f, plinth, -depth / 2f), new Vector3(width / 2f, worktop, -depth / 2f + panel), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Right", root, new Vector3(width / 2f - panel, plinth, -depth / 2f), new Vector3(width / 2f, worktop, front), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Carcass_Divider", root, new Vector3(0.3f, plinth, -depth / 2f), new Vector3(0.3f + panel, worktop, front), SurfaceMaterial.Furniture);

            // Gloss white fronts with a 3 mm joint between them and a long thin steel bar handle near the top edge of each.
            var fronts = new GrayboxMeshBuilder();
            var handles = new GrayboxMeshBuilder();
            float[][] modules = { new[] { -0.9f, -0.3f, 1f, 1f, 1f }, new[] { -0.3f, 0.3f, 1f, 1f }, new[] { 0.3f, 0.9f, 0.25f, 0.75f } };
            foreach (var module in modules)
            {
                float x0 = module[0] + 0.003f;
                float x1 = module[1] - 0.003f;
                float total = 0f;
                for (int k = 2; k < module.Length; k++) total += module[k];
                float y = worktop;
                for (int k = 2; k < module.Length; k++)
                {
                    float h = (worktop - plinth) * module[k] / total;
                    float yTop = y - 0.0015f;
                    float yBottom = y - h + 0.0015f;
                    fronts.AddBox(new Vector3(x0, yBottom, front), new Vector3(x1, yTop, front + 0.018f));
                    // Handle: a 0.36 m bar, 12 mm tall, held 22 mm off the front by two small posts.
                    float cx = (x0 + x1) / 2f;
                    float hy = yTop - 0.04f;
                    handles.AddBox(new Vector3(cx - 0.18f, hy - 0.006f, front + 0.04f), new Vector3(cx + 0.18f, hy + 0.006f, front + 0.052f));
                    foreach (float post in new[] { -0.17f, 0.17f })
                    {
                        handles.AddBox(new Vector3(cx + post - 0.005f, hy - 0.004f, front + 0.018f), new Vector3(cx + post + 0.005f, hy + 0.004f, front + 0.04f));
                    }
                    y -= h;
                }
            }
            fronts.Attach("Fronts", root, SurfaceMaterial.DeskWhite);
            handles.Attach("Handles", root, SurfaceMaterial.Metal);

            // Light stone worktop in four pieces around the sink opening.
            GrayboxUtility.CreateBoxBetween("Worktop_Left", root, new Vector3(-width / 2f, worktop, -depth / 2f), new Vector3(sinkX0, CounterHeight, depth / 2f), SurfaceMaterial.StoneTop);
            GrayboxUtility.CreateBoxBetween("Worktop_Right", root, new Vector3(sinkX1, worktop, -depth / 2f), new Vector3(width / 2f, CounterHeight, depth / 2f), SurfaceMaterial.StoneTop);
            GrayboxUtility.CreateBoxBetween("Worktop_Back", root, new Vector3(sinkX0, worktop, -depth / 2f), new Vector3(sinkX1, CounterHeight, sinkZ0), SurfaceMaterial.StoneTop);
            GrayboxUtility.CreateBoxBetween("Worktop_Front", root, new Vector3(sinkX0, worktop, sinkZ1), new Vector3(sinkX1, CounterHeight, depth / 2f), SurfaceMaterial.StoneTop);

            // Double-bowl under-mount sink in dark matt steel: a wide bowl (0.31 m, 0.2 m deep) and a narrower one (0.21 m, half as deep) with a low divider. A mug stands in the wide bowl.
            const float wall = 0.008f;
            const float splitX = 0.65f;
            var basin = new GrayboxMeshBuilder();
            var drains = new GrayboxMeshBuilder();
            foreach (var (x0, x1, bottom) in new[] { (sinkX0, splitX, CounterHeight - 0.2f), (splitX, sinkX1, CounterHeight - 0.1f) })
            {
                basin.AddBox(new Vector3(x0, bottom, sinkZ0), new Vector3(x1, bottom + wall, sinkZ1));
                basin.AddBox(new Vector3(x0, bottom, sinkZ0), new Vector3(x1, CounterHeight, sinkZ0 + wall), true, false);
                basin.AddBox(new Vector3(x0, bottom, sinkZ1 - wall), new Vector3(x1, CounterHeight, sinkZ1), true, false);
                drains.AddLathe(new[] { P(0f, 0f, true), P(0.028f, 0f, true), P(0.028f, 0.003f, true), P(0f, 0.003f, true) }, 24, true,
                    new Vector3((x0 + x1) / 2f, bottom + wall, (sinkZ0 + sinkZ1) / 2f), Quaternion.identity);
            }
            basin.AddBox(new Vector3(sinkX0, CounterHeight - 0.2f, sinkZ0), new Vector3(sinkX0 + wall, CounterHeight, sinkZ1), true, false);
            basin.AddBox(new Vector3(sinkX1 - wall, CounterHeight - 0.1f, sinkZ0), new Vector3(sinkX1, CounterHeight, sinkZ1), true, false);
            basin.AddBox(new Vector3(splitX - wall / 2f, CounterHeight - 0.2f, sinkZ0), new Vector3(splitX + wall / 2f, CounterHeight - 0.03f, sinkZ1), true, false);
            basin.Attach("Basin", root, SurfaceMaterial.StainlessSteel);
            BuildMug(GrayboxUtility.CreateGroup("Mug_Sink", root), new Vector3(sinkX0 + 0.075f, CounterHeight - 0.2f + wall, 0.04f));
            drains.Attach("Drains", root, SurfaceMaterial.BlackHousing);

            // Gooseneck tap in the same matt dark steel, on the back strip: a round foot, a tall column bent over in a half circle and down
            // into the spout with its pull-out head, and a single side lever.
            float tapX = (sinkX0 + splitX) / 2f; // centred on the wide bowl
            float tapZ = (-depth / 2f + sinkZ0) / 2f;
            const float archRadius = 0.09f;
            float columnTop = CounterHeight + 0.30f;
            var tap = new GrayboxMeshBuilder();
            tap.AddLathe(new[] { P(0f, 0f, true), P(0.03f, 0f, true), P(0.03f, 0.012f, true), P(0.022f, 0.014f, true), P(0f, 0.014f, true) }, 32, true, new Vector3(tapX, CounterHeight, tapZ), Quaternion.identity);
            var neck = new List<Vector3> { new Vector3(tapX, CounterHeight + 0.012f, tapZ), new Vector3(tapX, columnTop - 0.05f, tapZ), new Vector3(tapX, columnTop, tapZ) };
            for (int step = 1; step <= 12; step++)
            {
                float angle = (180f - 180f * step / 12f) * Mathf.Deg2Rad;
                neck.Add(new Vector3(tapX, columnTop + Mathf.Sin(angle) * archRadius, tapZ + archRadius + Mathf.Cos(angle) * archRadius));
            }
            neck.Add(new Vector3(tapX, columnTop - 0.06f, tapZ + 2f * archRadius));
            neck.Add(new Vector3(tapX, columnTop - 0.10f, tapZ + 2f * archRadius));
            tap.AddTube(neck, 0.0135f, 16, Vector3.zero, Quaternion.identity);
            // Pull-out head at the end of the spout.
            float headTop = columnTop - 0.10f;
            tap.AddTube(new List<Vector3> { new Vector3(tapX, headTop, tapZ + 2f * archRadius), new Vector3(tapX, headTop - 0.10f, tapZ + 2f * archRadius) }, 0.0185f, 16, Vector3.zero, Quaternion.identity);
            tap.AddLathe(new[] { P(0f, 0f, true), P(0.0185f, 0f, true) }, 16, true, new Vector3(tapX, headTop - 0.10f, tapZ + 2f * archRadius), Quaternion.identity);
            // Lever: a short horizontal stub out of the column and a thin rod up from its end.
            tap.AddLathe(new[] { P(0f, 0f, true), P(0.011f, 0f, true), P(0.011f, 0.05f, true), P(0f, 0.05f, true) }, 20, true, new Vector3(tapX, CounterHeight + 0.09f, tapZ), Quaternion.Euler(0f, 0f, -90f));
            tap.AddTube(new List<Vector3> { new Vector3(tapX + 0.04f, CounterHeight + 0.09f, tapZ), new Vector3(tapX + 0.04f, CounterHeight + 0.17f, tapZ) }, 0.004f, 8, Vector3.zero, Quaternion.identity);
            tap.Attach("Tap", root, SurfaceMaterial.StainlessSteel);

            // Wall cabinets: a carcass with three gloss doors and a bar handle on each.
            var wallDoors = new GrayboxMeshBuilder();
            var wallHandles = new GrayboxMeshBuilder();
            GrayboxUtility.CreateStandingBox("WallCabinets", root, new Vector3(0f, 1.5f, -depth / 2f + 0.17f), new Vector3(width, 0.7f, 0.34f), SurfaceMaterial.Furniture, false);
            for (int i = 0; i < 3; i++)
            {
                float x0 = -width / 2f + i * width / 3f + 0.003f;
                float x1 = -width / 2f + (i + 1) * width / 3f - 0.003f;
                wallDoors.AddBox(new Vector3(x0, 1.5015f, -depth / 2f + 0.34f), new Vector3(x1, 2.1985f, -depth / 2f + 0.358f));
                float cx = (x0 + x1) / 2f;
                wallHandles.AddBox(new Vector3(cx - 0.12f, 1.55f, -depth / 2f + 0.38f), new Vector3(cx + 0.12f, 1.562f, -depth / 2f + 0.392f));
                foreach (float post in new[] { -0.11f, 0.11f })
                {
                    wallHandles.AddBox(new Vector3(cx + post - 0.005f, 1.552f, -depth / 2f + 0.358f), new Vector3(cx + post + 0.005f, 1.56f, -depth / 2f + 0.38f));
                }
            }
            wallDoors.Attach("WallFronts", root, SurfaceMaterial.DeskWhite);
            wallHandles.Attach("WallHandles", root, SurfaceMaterial.Metal);
            AddBoxCollider(root, new Vector3(0f, CounterHeight / 2f, 0f), new Vector3(width, CounterHeight, depth));
        }

        /// <summary>Where the coffee machine stands on the worktop (counter-local): centred on the left-hand base unit (x = -0.6) and on the worktop's depth.</summary>
        public static readonly Vector3 CounterMachinePosition = new Vector3(-0.6f, CounterHeight, -0.04f);

        /// <summary>
        /// Large office multifunction printer 0.60 x 1.27 x 0.65 m, after a product photo (an A3 colour copier on its four-drawer
        /// paper cabinet): a white body with the logo, four paper drawers with handle recesses and numbers on feet, the engine
        /// above them, an open black output tray at the left under the scanner, the scanner with a document feeder and its
        /// black paper tray on top, and a tilted black control panel with a screen and a green start button. The front faces +Z.
        /// </summary>
        private static void BuildPrinter(Transform root)
        {
            const float width = 0.6f;
            const float depth = 0.65f;
            float front = depth / 2f;
            float half = width / 2f;
            var white = new GrayboxMeshBuilder();
            var seam = new GrayboxMeshBuilder();
            var black = new GrayboxMeshBuilder();
            var grey = new GrayboxMeshBuilder();
            var red = new GrayboxMeshBuilder();

            // Feet and the four-drawer cabinet (0.03 - 0.64 m).
            foreach (var corner in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
            {
                grey.AddLathe(new[] { P(0f, 0f, true), P(0.022f, 0f, true), P(0.022f, 0.030f, true), P(0f, 0.030f, true) }, 20, true,
                    new Vector3(corner.x * (half - 0.04f), 0f, corner.y * (front - 0.04f)), Quaternion.identity);
            }
            white.AddRoundedBlock(new Vector3(0f, 0.030f, 0f), width, depth, 0.020f, 0.610f, 0.008f, 5);
            float[] drawerBottom = { 0.045f, 0.190f, 0.335f, 0.480f };
            for (int i = 0; i < 4; i++)
            {
                float y0 = drawerBottom[i];
                // Drawer front: a flat panel a hair proud, a dark handle pocket (cut-out look) at the left of centre, and the number.
                white.AddBox(new Vector3(-half + 0.012f, y0, front), new Vector3(half - 0.012f, y0 + 0.138f, front + 0.004f));
                grey.AddBox(new Vector3(-0.075f, y0 + 0.070f, front + 0.004f), new Vector3(0.075f, y0 + 0.118f, front + 0.0055f));
                black.AddBox(new Vector3(-0.068f, y0 + 0.088f, front + 0.0055f), new Vector3(0.068f, y0 + 0.112f, front + 0.0062f));
                grey.AddBox(new Vector3(-0.235f, y0 + 0.058f, front + 0.004f), new Vector3(-0.215f, y0 + 0.082f, front + 0.0046f));
                seam.AddBox(new Vector3(-half + 0.012f, y0 - 0.004f, front), new Vector3(half - 0.012f, y0, front + 0.002f));
            }

            // Engine (0.64 - 0.99 m): white block with the logo, the model strip and a red mark.
            white.AddRoundedBlock(new Vector3(0f, 0.640f, 0f), width, depth, 0.018f, 0.350f, 0.008f, 5);
            black.AddBox(new Vector3(-0.045f, 0.835f, front + 0.0005f), new Vector3(0.045f, 0.848f, front + 0.0025f));
            black.AddBox(new Vector3(-0.205f, 0.668f, front + 0.0005f), new Vector3(-0.115f, 0.674f, front + 0.0020f));
            red.AddBox(new Vector3(-0.212f, 0.650f, front + 0.0005f), new Vector3(-0.200f, 0.662f, front + 0.0025f));
            grey.AddBox(new Vector3(-0.195f, 0.652f, front + 0.0005f), new Vector3(-0.110f, 0.659f, front + 0.0015f));

            // Open output space above the left of the engine: a black tray that slopes down to the front, a black back wall, and a
            // white support column at the right that carries the scanner.
            const float trayTop = 0.995f;
            const float scannerBottom = 1.115f;
            black.AddBox(new Vector3(-0.130f, 0.990f, -front + 0.012f), new Vector3(half - 0.012f, trayTop, front - 0.012f));
            black.AddStrip(new List<Vector3> { new Vector3(0f, trayTop, front - 0.050f), new Vector3(0f, trayTop + 0.040f, front - 0.150f), new Vector3(0f, trayTop + 0.050f, -front + 0.120f) },
                Vector3.right, 0.390f, 0.012f, new Vector3(0.065f, 0f, 0f), Quaternion.identity);
            black.AddBox(new Vector3(-0.130f, trayTop, -front + 0.012f), new Vector3(half - 0.012f, scannerBottom, -front + 0.080f));
            white.AddRoundedBlock(new Vector3(-0.215f, 0.990f, 0f), 0.170f, depth, 0.012f, scannerBottom - 0.990f, 0.006f, 4);

            // Scanner (full width) and the document feeder on it with its black paper tray.
            white.AddRoundedBlock(new Vector3(0f, scannerBottom, 0f), width, depth - 0.02f, 0.016f, 0.060f, 0.007f, 5);
            white.AddRoundedBlock(new Vector3(0f, scannerBottom + 0.060f, -0.02f), width - 0.060f, depth - 0.110f, 0.020f, 0.058f, 0.008f, 5);
            black.AddStrip(new List<Vector3> { new Vector3(0f, scannerBottom + 0.112f, -0.060f), new Vector3(0f, scannerBottom + 0.134f, -0.200f), new Vector3(0f, scannerBottom + 0.150f, -0.330f) },
                Vector3.right, 0.420f, 0.008f, Vector3.zero, Quaternion.identity);
            black.AddBox(new Vector3(-0.150f, scannerBottom + 0.112f, 0.060f), new Vector3(0.150f, scannerBottom + 0.119f, 0.215f));

            // Control panel: a black slab on the user's right (-X) at the front of the scanner level, carried out on an arm and tilted up
            // towards the user (+Z): its front edge is the low one. Screen on the user's left of it, keys and the green start button right.
            var panel = GrayboxUtility.CreateGroup("ControlPanel", root);
            panel.localPosition = new Vector3(-0.12f, scannerBottom + 0.020f, front + 0.040f);
            panel.localRotation = Quaternion.Euler(26f, 0f, 0f);
            var panelBody = new GrayboxMeshBuilder();
            var panelScreen = new GrayboxMeshBuilder();
            var panelStart = new GrayboxMeshBuilder();
            var panelKeys = new GrayboxMeshBuilder();
            panelBody.AddRoundedBlock(Vector3.zero, 0.320f, 0.150f, 0.012f, 0.026f, 0.008f, 4);
            panelScreen.AddPanel(new Vector3(-0.060f, 0.0268f, 0.050f), new Vector3(0.140f, 0.0268f, 0.050f), new Vector3(0.140f, 0.0268f, -0.050f), new Vector3(-0.060f, 0.0268f, -0.050f),
                Vector3.up, new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            panelStart.AddBox(new Vector3(-0.130f, 0.0262f, 0.025f), new Vector3(-0.100f, 0.0285f, 0.055f));
            panelKeys.AddBox(new Vector3(-0.145f, 0.0262f, -0.055f), new Vector3(-0.085f, 0.0268f, 0.012f), true, false);
            panelBody.Attach("Panel_Body", panel, SurfaceMaterial.BlackHousing);
            panelScreen.Attach("Panel_Screen", panel, SurfaceMaterial.MonitorImage);
            panelStart.Attach("Panel_Start", panel, SurfaceMaterial.SafetyGreen);
            panelKeys.Attach("Panel_Keys", panel, SurfaceMaterial.KeyboardKey);
            // The arm that carries the panel out from the scanner.
            black.AddBox(new Vector3(-0.240f, scannerBottom - 0.020f, front - 0.110f), new Vector3(0.000f, scannerBottom + 0.020f, front + 0.020f));

            white.Attach("Printer_Body", root, SurfaceMaterial.Trunking);
            seam.Attach("Printer_Seams", root, SurfaceMaterial.KeyboardKey);
            black.Attach("Printer_Black", root, SurfaceMaterial.ReaderBlack);
            grey.Attach("Printer_Grey", root, SurfaceMaterial.Metal);
            red.Attach("Printer_Mark", root, SurfaceMaterial.SafetyRed);
            AddBoxCollider(root, new Vector3(0f, 0.635f, 0.03f), new Vector3(width, 1.27f, depth + 0.06f));
        }

        /// <summary>
        /// Kitchen fridge-freezer 0.6 x 1.8 x 0.65 m after a product photo: a white body on small feet, a tall upper door and a lower
        /// freezer door (a 10 mm joint between them), each with a vertical silver D-handle on the left edge held by two bracket
        /// stand-offs; the doors face +Z.
        /// </summary>
        private static void BuildFridge(Transform root)
        {
            const float width = 0.6f;
            const float height = 1.8f;
            const float depth = 0.65f;
            const float doorThickness = 0.022f;
            const float split = 0.85f;
            float bodyFront = depth / 2f - 0.003f - doorThickness;
            var white = new GrayboxMeshBuilder();
            var silver = new GrayboxMeshBuilder();
            var feet = new GrayboxMeshBuilder();

            white.AddRoundedBlock(new Vector3(0f, 0.03f, (bodyFront - depth / 2f) / 2f), width, bodyFront + depth / 2f, 0.02f, height - 0.03f, 0.006f, 5);
            // Lower freezer door and upper fridge door: gloss slabs with a rounded edge.
            white.AddRoundedSlab(new Vector3(0f, (0.035f + split - 0.005f) / 2f, bodyFront), width - 0.01f, split - 0.04f, 0.012f, doorThickness, 0.005f, 4, true);
            white.AddRoundedSlab(new Vector3(0f, (split + 0.005f + height - 0.005f) / 2f, bodyFront), width - 0.01f, height - split - 0.01f, 0.012f, doorThickness, 0.005f, 4, true);
            foreach (var corner in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
            {
                feet.AddLathe(new[] { P(0f, 0f, true), P(0.016f, 0f, true), P(0.016f, 0.03f, true), P(0f, 0.03f, true) }, 16, true,
                    new Vector3(corner.x * (width / 2f - 0.05f), 0f, corner.y * (depth / 2f - 0.07f)), Quaternion.identity);
            }
            // D-handles on the viewer's left (+X): a vertical bar and a stand-off at each end.
            foreach (var (y0, y1) in new[] { (0.52f, 0.80f), (0.94f, 1.22f) })
            {
                float x = width / 2f - 0.045f;
                silver.AddRoundedBlock(new Vector3(x, y0, depth / 2f + 0.026f), 0.022f, 0.016f, 0.004f, y1 - y0, 0.003f, 2);
                foreach (float y in new[] { y0 + 0.012f, y1 - 0.034f })
                {
                    silver.AddBox(new Vector3(x - 0.011f, y, depth / 2f - 0.004f), new Vector3(x + 0.011f, y + 0.022f, depth / 2f + 0.030f));
                }
            }
            white.Attach("Fridge_Body", root, SurfaceMaterial.DeskWhite);
            silver.Attach("Fridge_Handles", root, SurfaceMaterial.Metal);
            feet.Attach("Fridge_Feet", root, SurfaceMaterial.MouseBlack);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }

        /// <summary>
        /// Bean-to-cup coffee machine after product photos, 0.19 x 0.34 x 0.48 m: a slim matt graphite body whose water tank is built
        /// into its upper rear (clear, flush with the body's sides and top, nothing sticks out), a lighter grey control strip, only as big as its round
        /// buttons, on the top front, a squared dispenser column on the front in the body's colour with a copper band at its foot and
        /// two spouts, and a drip tray with a black grille reaching forward from the foot. The espresso cup stands on the grille
        /// under the spouts, clear of the machine. A desk-top prop, its front faces +Z.
        /// </summary>
        private static void BuildCoffeeMachine(Transform root)
        {
            const float half = 0.095f;
            const float bodyBack = -0.20f;
            const float bodyFront = 0.15f;
            const float trayFront = 0.28f;
            const float trayTop = 0.032f;
            const float shoulder = 0.20f;
            const float top = 0.335f;
            const float tankFront = -0.03f;
            var graphite = new GrayboxMeshBuilder();
            var copper = new GrayboxMeshBuilder();
            var tank = new GrayboxMeshBuilder();
            var grille = new GrayboxMeshBuilder();
            var buttons = new GrayboxMeshBuilder();
            var panel = new GrayboxMeshBuilder();

            // Body: the full-depth lower block, and over it the upper block in front of the tank (the tank itself fills the upper rear).
            graphite.AddRoundedBlock(new Vector3(0f, 0.012f, (bodyBack + bodyFront) / 2f), 2f * half, bodyFront - bodyBack, 0.015f, shoulder - 0.012f, 0.004f, 5);
            graphite.AddRoundedBlock(new Vector3(0f, shoulder, (tankFront + bodyFront) / 2f), 2f * half, bodyFront - tankFront, 0.015f, top - shoulder, 0.006f, 5);
            tank.AddRoundedBlock(new Vector3(0f, shoulder, (bodyBack + tankFront) / 2f), 2f * half, tankFront - bodyBack, 0.015f, top - shoulder, 0.006f, 5);
            // Drip tray: a shallow tray out of the foot with a raised black grille of slats.
            graphite.AddRoundedBlock(new Vector3(0f, 0f, (bodyFront + trayFront) / 2f - 0.005f), 0.17f, trayFront - bodyFront + 0.01f, 0.008f, trayTop, 0.003f, 4);
            for (int i = 0; i < 8; i++)
            {
                float z = bodyFront + 0.012f + i * 0.0145f;
                grille.AddBox(new Vector3(-0.072f, trayTop, z), new Vector3(0.072f, trayTop + 0.003f, z + 0.007f));
            }
            // Dispenser column, all in the body's colour except the copper band at its foot (it stays below the top, no coplanar faces).
            graphite.AddRoundedBlock(new Vector3(0f, 0.14f, bodyFront + 0.0425f), 0.075f, 0.085f, 0.006f, 0.115f, 0.003f, 3);
            copper.AddRoundedBlock(new Vector3(0f, 0.14f, bodyFront + 0.0425f), 0.077f, 0.087f, 0.006f, 0.04f, 0.002f, 3);
            // The underside with the spouts is in the body's colour: a graphite plate under the copper band, just inside its outline.
            graphite.AddRoundedBlock(new Vector3(0f, 0.1385f, bodyFront + 0.0425f), 0.0764f, 0.0864f, 0.006f, 0.0015f, 0.0005f, 3);
            foreach (float x in new[] { -0.013f, 0.013f })
            {
                grille.AddLathe(new[] { P(0f, 0f, true), P(0.0032f, 0f, true), P(0.0032f, 0.006f, true), P(0f, 0.006f, true) }, 10, true, new Vector3(x, 0.1325f, bodyFront + 0.05f), Quaternion.identity);
            }
            // Control strip on the top front: a flat lighter grey plate with two rows of round buttons (the middle one is the power button).
            panel.AddRoundedBlock(new Vector3(0f, top, bodyFront - 0.0363f), 0.178f, 0.0535f, 0.009f, 0.002f, 0.0007f, 5);
            for (int i = 0; i < 9; i++)
            {
                bool row = i % 2 == 0;
                float x = -0.072f + i * 0.018f;
                float z = bodyFront - (row ? 0.045f : 0.025f);
                float r = i == 4 ? 0.0095f : 0.0068f;
                buttons.AddLathe(new[] { P(0f, 0f, true), P(r, 0f, true), P(r, 0.0018f, true), P(0f, 0.0018f, true) }, 18, true, new Vector3(x, top + 0.002f, z), Quaternion.identity);
            }

            graphite.Attach("Body", root, SurfaceMaterial.CoffeeGraphite);
            panel.Attach("ControlPanel", root, SurfaceMaterial.CoffeePanel);
            copper.Attach("Copper", root, SurfaceMaterial.CopperTrim);
            tank.Attach("Tank", root, SurfaceMaterial.SmokedTank);
            grille.Attach("Grille", root, SurfaceMaterial.BlackHousing);
            buttons.Attach("Buttons", root, SurfaceMaterial.Chrome);
            // The cup stands on the grille under the spouts (their z is bodyFront + 0.05), 4 cm in front of the body and well under the column.
            BuildMug(GrayboxUtility.CreateGroup("Cup", root), new Vector3(0f, trayTop + 0.003f, bodyFront + 0.05f));
        }

        private static void BuildMug(Transform root) => BuildMug(root, Vector3.zero);

        /// <summary>
        /// A white ceramic mug standing on <paramref name="position"/>, after a product photo: 83 mm tall, 85 mm across the rim and
        /// narrowing to the base, rounded at the bottom edge, thin walls with a rolled rim and the open inside, and a handle
        /// like a rounded triangle on +X that joins the wall near the rim and again lower down.
        /// </summary>
        private static void BuildMug(Transform root, Vector3 position)
        {
            var ceramic = new GrayboxMeshBuilder();
            ceramic.AddLathe(new[]
            {
                P(0f, 0f, true), P(0.0255f, 0f, true), P(0.0300f, 0.0016f), P(0.0330f, 0.0065f), P(0.0342f, 0.0125f), P(0.0426f, 0.0814f, true),
                P(0.0420f, 0.0829f), P(0.0405f, 0.0834f), P(0.0392f, 0.0829f), P(0.0386f, 0.0814f, true),
                P(0.0307f, 0.0125f, true), P(0.0292f, 0.0085f), P(0.0262f, 0.0064f, true), P(0f, 0.0064f, true),
            }, 48, true, position, Quaternion.identity);
            ceramic.AddTube(SmoothPath(new[]
            {
                new Vector3(0.0395f, 0.0670f, 0f), new Vector3(0.0500f, 0.0700f, 0f), new Vector3(0.0615f, 0.0660f, 0f), new Vector3(0.0680f, 0.0560f, 0f),
                new Vector3(0.0690f, 0.0480f, 0f), new Vector3(0.0640f, 0.0390f, 0f), new Vector3(0.0545f, 0.0335f, 0f), new Vector3(0.0330f, 0.0245f, 0f),
            }, 6), 0.0043f, 14, position, Quaternion.identity);
            ceramic.Attach("Mug", root, SurfaceMaterial.Whiteboard);
        }

        /// <summary>
        /// Two-door steel clothes locker 0.6 x 1.8 x 0.5 m (two 0.3 m compartments, product photo): a light grey carcass on a
        /// recessed plinth, two slate-blue doors set 4 mm back in the frame with soft edges, a name plate slot and five slanted
        /// vent slots above and below, and a white cylinder lock by the outer edge of each door. The doors face +Z.
        /// </summary>
        private static void BuildLocker(Transform root)
        {
            const float width = 0.6f;
            const float height = 1.8f;
            const float depth = 0.5f;
            const float plinth = 0.07f;
            const float frame = 0.025f;
            const float gap = 0.002f;
            float front = depth / 2f;
            float doorFront = front - 0.004f;
            float doorBack = front - 0.022f;
            float doorBottom = plinth + frame;
            float doorTop = height - frame;
            float doorWidth = width / 2f - frame - frame / 2f;

            var body = new GrayboxMeshBuilder();
            body.AddBox(new Vector3(-width / 2f + 0.01f, 0f, -front + 0.01f), new Vector3(width / 2f - 0.01f, plinth, front - 0.02f));
            body.AddBox(new Vector3(-width / 2f, plinth, -front), new Vector3(-width / 2f + frame, height, front));
            body.AddBox(new Vector3(width / 2f - frame, plinth, -front), new Vector3(width / 2f, height, front));
            body.AddBox(new Vector3(-width / 2f + frame, doorTop, -front), new Vector3(width / 2f - frame, height, front));
            body.AddBox(new Vector3(-width / 2f + frame, plinth, -front), new Vector3(width / 2f - frame, doorBottom, front));
            body.AddBox(new Vector3(-frame / 2f, doorBottom, -front), new Vector3(frame / 2f, doorTop, front));
            body.AddBox(new Vector3(-width / 2f + frame, doorBottom, -front), new Vector3(width / 2f - frame, doorTop, -front + 0.01f));
            body.Attach("Body", root, SurfaceMaterial.LockerBody);

            var doors = new GrayboxMeshBuilder();
            var plates = new GrayboxMeshBuilder();
            var vents = new GrayboxMeshBuilder();
            var locks = new GrayboxMeshBuilder();
            var keyholes = new GrayboxMeshBuilder();
            foreach (int side in new[] { -1, 1 })
            {
                float inner = side * (frame / 2f + gap);
                float outer = side * (width / 2f - frame - gap);
                float left = Mathf.Min(inner, outer);
                float right = Mathf.Max(inner, outer);
                float centre = (left + right) / 2f;
                doors.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(left, right, doorBottom + gap, doorTop - gap, 0.004f, 2),
                    new Vector3(0f, 0f, doorBack), new Vector3(0f, 0f, doorFront), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

                float faceZ = doorFront + 0.0008f;
                plates.AddPanel(new Vector3(centre - 0.035f, 1.64f, faceZ), new Vector3(centre + 0.035f, 1.64f, faceZ),
                    new Vector3(centre + 0.035f, 1.66f, faceZ), new Vector3(centre - 0.035f, 1.66f, faceZ), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);

                foreach (float y in new[] { 1.715f, 0.135f })
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float x = centre + (i - 2) * 0.0155f;
                        var slant = new Vector3(0.0045f, 0.0105f, 0f);
                        var half = new Vector3(0.0012f, -0.0005f, 0f);
                        var c = new Vector3(x, y, faceZ);
                        vents.AddPanel(c - slant - half, c - slant + half, c + slant + half, c + slant - half, Vector3.forward,
                            Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                    }
                }

                var lockCentre = new Vector3(left + 0.045f, 0.88f, doorFront);
                locks.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(-0.009f, 0.009f, -0.014f, 0.014f, 0.004f, 3),
                    lockCentre, lockCentre + new Vector3(0f, 0f, 0.012f), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                var keyFace = lockCentre + new Vector3(0f, 0f, 0.0128f);
                keyholes.AddPanel(keyFace + new Vector3(-0.0012f, -0.005f, 0f), keyFace + new Vector3(0.0012f, -0.005f, 0f),
                    keyFace + new Vector3(0.0012f, 0.005f, 0f), keyFace + new Vector3(-0.0012f, 0.005f, 0f), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            doors.Attach("Doors", root, SurfaceMaterial.LockerDoor);
            plates.Attach("NamePlates", root, SurfaceMaterial.LockerPlate);
            vents.Attach("VentSlots", root, SurfaceMaterial.BlackHousing);
            locks.Attach("Locks", root, SurfaceMaterial.DetectorWhite);
            keyholes.Attach("Keyholes", root, SurfaceMaterial.BlackHousing);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }

        /// <summary>
        /// Changing-room bench 1.5 x 0.45 x 0.38 m (product photo): three oak slats with rounded edges, overhanging the frame at
        /// both ends, on a cream square-tube frame (four legs, a rail under each long edge, end rails, and a lower
        /// stretcher with two cross bars). Its long side runs along X; people sit facing +Z.
        /// </summary>
        private static void BuildLockerBench(Transform root)
        {
            const float length = 1.5f;
            const float depth = 0.38f;
            const float seatTop = 0.45f;
            const float slatThickness = 0.03f;
            const float slatWidth = 0.112f;
            const float slatGap = 0.022f;
            const float tube = 0.04f;
            const float tubeRadius = 0.005f;
            const float legX = 0.67f;
            const float legZ = 0.15f;
            const float upperRail = seatTop - slatThickness - tube / 2f;
            const float lowerRail = 0.17f;

            var wood = new GrayboxMeshBuilder();
            for (int i = 0; i < 3; i++)
            {
                float z = (i - 1) * (slatWidth + slatGap);
                wood.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(z - slatWidth / 2f, z + slatWidth / 2f, seatTop - slatThickness, seatTop, 0.007f, 3),
                    new Vector3(-length / 2f, 0f, 0f), new Vector3(length / 2f, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
            }
            wood.Attach("Slats", root, SurfaceMaterial.BenchWood);

            var steel = new GrayboxMeshBuilder();
            var section = GrayboxMeshBuilder.RoundedRectLoop(-tube / 2f, tube / 2f, -tube / 2f, tube / 2f, tubeRadius, 2);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    steel.AddExtrusion(section, new Vector3(sx * legX, 0f, sz * legZ), new Vector3(sx * legX, upperRail + tube / 2f, sz * legZ),
                        Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
                }
            }
            // Rails run between the legs: along X under both long edges, along Z at both ends (high and low).
            foreach (int sz in new[] { -1, 1 })
            {
                steel.AddExtrusion(section, new Vector3(-legX + tube / 2f, upperRail, sz * legZ), new Vector3(legX - tube / 2f, upperRail, sz * legZ),
                    Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
            }
            foreach (float y in new[] { upperRail, lowerRail })
            {
                foreach (int sx in new[] { -1, 1 })
                {
                    steel.AddExtrusion(section, new Vector3(sx * legX, y, -legZ + tube / 2f), new Vector3(sx * legX, y, legZ - tube / 2f),
                        Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                }
            }
            steel.AddExtrusion(section, new Vector3(-legX + tube / 2f, lowerRail, 0f), new Vector3(legX - tube / 2f, lowerRail, 0f),
                Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
            steel.Attach("Frame", root, SurfaceMaterial.BenchFrame);
            AddBoxCollider(root, new Vector3(0f, seatTop / 2f, 0f), new Vector3(length, seatTop, depth));
        }

        /// <summary>
        /// A stack of 16 sheets of A4 paper (210 x 297 mm, 0.1 mm thick each), every sheet a little askew like a loose pile. The top
        /// sheet carries the printed page texture (<c>T_PaperPage</c>, text inside 25 mm margins). Lies on a desk; the page reads
        /// upright for a person standing at +Z.
        /// </summary>
        private static void BuildPaperStack(Transform root)
        {
            const int sheets = 16;
            const float thickness = 0.0001f;
            const float halfX = 0.105f;
            const float halfZ = 0.1485f;
            var edges = new GrayboxMeshBuilder();
            var top = new GrayboxMeshBuilder();
            for (int i = 0; i < sheets; i++)
            {
                // Deterministic scatter: each sheet is shifted a few millimetres and turned a degree or two.
                float yaw = Mathf.Sin(i * 2.7f) * 2.2f * Mathf.Deg2Rad;
                var shift = new Vector3(Mathf.Sin(i * 1.9f + 0.4f) * 0.006f, 0f, Mathf.Cos(i * 2.3f) * 0.006f);
                float y0 = i * thickness;
                float y1 = y0 + thickness;
                Vector3 Corner(float x, float z, float y) => shift + new Vector3(x * Mathf.Cos(yaw) + z * Mathf.Sin(yaw), y, -x * Mathf.Sin(yaw) + z * Mathf.Cos(yaw));
                var c0 = new[] { new Vector2(-halfX, -halfZ), new Vector2(halfX, -halfZ), new Vector2(halfX, halfZ), new Vector2(-halfX, halfZ) };
                // Edges of the sheet (the faces under the top one are never seen).
                for (int k = 0; k < 4; k++)
                {
                    var a = c0[k];
                    var b = c0[(k + 1) % 4];
                    var outward = Corner((a.x + b.x), (a.y + b.y), 0f) - shift;
                    edges.AddPanel(Corner(b.x, b.y, y0), Corner(a.x, a.y, y0), Corner(a.x, a.y, y1), Corner(b.x, b.y, y1), outward, Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                }
                // Top face: the printed page on the last sheet; the others are plain paper (a blank sheet shows through the edges' material).
                var plain = i == sheets - 1 ? top : edges;
                // Viewed from +Z looking down: u runs from +X (left) to -X, v from +Z (bottom of the page) to -Z (head of the page).
                plain.AddPanel(Corner(-halfX, halfZ, y1), Corner(halfX, halfZ, y1), Corner(halfX, -halfZ, y1), Corner(-halfX, -halfZ, y1), Vector3.up,
                    new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            }
            edges.Attach("Paper", root, SurfaceMaterial.Whiteboard);
            top.Attach("TopSheet", root, SurfaceMaterial.PaperPage);
        }

        /// <summary>
        /// Photo frame 15 x 20 cm on a desk (photo-based): a black mitred moulding with a rounded outer edge round a white mount,
        /// the picture set back behind a pane of glass, a thin back panel, and a slim flat leg on a hinge on the back that props
        /// it up leaning back 12 degrees. The picture faces +Z; the pivot is on the desk under the frame's back edge.
        /// </summary>
        private static void BuildPhotoFrame(Transform root)
        {
            const float width = 0.15f;
            const float height = 0.2f;
            const float depth = 0.016f;
            const float border = 0.018f;
            const float lean = 12f;
            var frame = GrayboxUtility.CreateGroup("Frame", root);
            frame.localRotation = Quaternion.Euler(-lean, 0f, 0f);

            var moulding = new GrayboxMeshBuilder();
            var corners = new[]
            {
                new Vector3(-width / 2f + border / 2f, border / 2f, 0f), new Vector3(width / 2f - border / 2f, border / 2f, 0f),
                new Vector3(width / 2f - border / 2f, height - border / 2f, 0f), new Vector3(-width / 2f + border / 2f, height - border / 2f, 0f),
            };
            // Bars run round the frame counter-clockwise seen from the front, mitred at every corner; the profile's u points
            // outwards, v towards the viewer: a rounded outer edge and a sharp inner one.
            var section = GrayboxMeshBuilder.RoundedRectLoop(-border / 2f, border / 2f, 0f, depth, new[] { 0f, 0f, 0f, 0.005f }, 3);
            for (int i = 0; i < 4; i++)
            {
                var from = corners[i];
                var to = corners[(i + 1) % 4];
                var direction = (to - from).normalized;
                var before = (from - corners[(i + 3) % 4]).normalized;
                var after = (corners[(i + 2) % 4] - to).normalized;
                var outwards = new Vector3(direction.y, -direction.x, 0f);
                moulding.AddExtrusion(section, from, to, -outwards, Vector3.forward, -(direction + before).normalized, (direction + after).normalized);
            }
            moulding.Attach("Moulding", frame, SurfaceMaterial.BlackHousing);

            float inner = border;
            var back = new GrayboxMeshBuilder();
            // The back panel is set into the moulding, 2 mm behind its back face, and its edges lie hidden inside the moulding.
            const float backInset = 0.002f;
            back.AddBox(new Vector3(-width / 2f + border - 0.004f, border - 0.004f, backInset), new Vector3(width / 2f - border + 0.004f, height - border + 0.004f, backInset + 0.003f));
            back.Attach("Back", frame, SurfaceMaterial.Furniture);
            float mountZ = 0.007f;
            float margin = inner + 0.012f;
            float pictureZ = mountZ - 0.0015f;
            var picture = new GrayboxMeshBuilder();
            picture.AddPanel(new Vector3(-width / 2f + margin, margin, pictureZ), new Vector3(width / 2f - margin, margin, pictureZ),
                new Vector3(width / 2f - margin, height - margin, pictureZ), new Vector3(-width / 2f + margin, height - margin, pictureZ), Vector3.forward,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            picture.Attach("Picture", frame, SurfaceMaterial.PhotoPicture);
            // The white mount is four strips round the picture, which sits a millimetre and a half behind its opening.
            var mountFrame = new GrayboxMeshBuilder();
            float m0 = inner - 0.003f;
            float m1 = margin;
            foreach (var strip in new[]
            {
                (new Vector2(-width / 2f + m0, m0), new Vector2(width / 2f - m0, m1)),
                (new Vector2(-width / 2f + m0, height - m1), new Vector2(width / 2f - m0, height - m0)),
                (new Vector2(-width / 2f + m0, m1), new Vector2(-width / 2f + m1, height - m1)),
                (new Vector2(width / 2f - m1, m1), new Vector2(width / 2f - m0, height - m1)),
            })
            {
                mountFrame.AddPanel(new Vector3(strip.Item1.x, strip.Item1.y, mountZ), new Vector3(strip.Item2.x, strip.Item1.y, mountZ),
                    new Vector3(strip.Item2.x, strip.Item2.y, mountZ), new Vector3(strip.Item1.x, strip.Item2.y, mountZ), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            mountFrame.Attach("Mount", frame, SurfaceMaterial.Whiteboard);
            var glass = new GrayboxMeshBuilder();
            float glassZ = depth - 0.003f;
            glass.AddPanel(new Vector3(-width / 2f + border - 0.003f, border - 0.003f, glassZ), new Vector3(width / 2f - border + 0.003f, border - 0.003f, glassZ),
                new Vector3(width / 2f - border + 0.003f, height - border + 0.003f, glassZ), new Vector3(-width / 2f + border - 0.003f, height - border + 0.003f, glassZ), Vector3.forward,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            glass.Attach("Glass", frame, SurfaceMaterial.DomeClear);

            // Hinge on the back panel near the top and the flat leg, in the desk's frame: the leg runs down and back to the desk.
            float c = Mathf.Cos(lean * Mathf.Deg2Rad);
            float sn = Mathf.Sin(lean * Mathf.Deg2Rad);
            float hingeHeight = 0.085f;
            var hinge = new Vector3(0f, hingeHeight * c - 0.0022f * sn, -hingeHeight * sn - 0.0022f * c);
            // The foot stays under the frame: no further back than the top edge of the leaning frame seen from above.
            float footZ = -height * sn + 0.004f;
            var legDirection = new Vector3(0f, -hinge.y, footZ - hinge.z).normalized;
            var foot = hinge + legDirection * (hinge.y / -legDirection.y);
            var steel = new GrayboxMeshBuilder();
            var legSection = GrayboxMeshBuilder.RoundedRectLoop(-0.0125f, 0.0125f, -0.001f, 0.001f, 0.0005f, 1);
            var legUp = Vector3.Cross(legDirection, Vector3.right).normalized;
            steel.AddExtrusion(legSection, hinge, foot, Vector3.right, legUp, -legDirection, legDirection);
            steel.AddTube(new[] { hinge + Vector3.left * 0.016f, hinge + Vector3.right * 0.016f }, 0.003f, 10, Vector3.zero, Quaternion.identity);
            steel.AddDisc(hinge + Vector3.left * 0.016f, Vector3.left, 0.003f, 10);
            steel.AddDisc(hinge + Vector3.right * 0.016f, Vector3.right, 0.003f, 10);
            steel.Attach("Leg", root, SurfaceMaterial.Chrome);
            var plate = new GrayboxMeshBuilder();
            var backNormal = new Vector3(0f, -sn, -c);
            var up = new Vector3(0f, c, -sn);
            var plateCentre = new Vector3(0f, hingeHeight * c, -hingeHeight * sn) - backNormal * (backInset - 0.0006f);
            plate.AddBox(new Vector3(-0.015f, -0.007f, -0.0006f), new Vector3(0.015f, 0.007f, 0.0006f));
            var plateTurn = Quaternion.LookRotation(-backNormal, up);
            var plateMesh = new GrayboxMeshBuilder();
            plateMesh.Append(plate, plateCentre, plateTurn);
            plateMesh.Attach("HingePlate", root, SurfaceMaterial.Chrome);
        }
    }
}
