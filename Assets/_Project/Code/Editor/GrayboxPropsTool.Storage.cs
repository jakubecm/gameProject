using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// What the storage room and the other rooms without a suspended ceiling are made of: the galvanised shelving, the cardboard
    /// boxes on it, the goods lift landing and the surface-mounted LED batten. All smooth meshes with rounded edges, one mesh per
    /// material, rebuilt in place by <see cref="RoomPreviewSceneTool.CreatePreviewScene"/>. Same pivot conventions as the other props.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>Height of the top of each shelf board of <see cref="BuildShelf"/>, where boxes and crates stand.</summary>
        public static readonly float[] ShelfBoardTops = { 0.10f, 0.62f, 1.14f, 1.66f };

        public const float ShelfWidth = 1.0f;
        public const float ShelfDepth = 0.4f;
        public const float ShelfHeight = 1.8f;

        /// <summary>
        /// Boltless galvanised shelving 1.0 x 1.8 x 0.4 m after a product photo: four slotted angle posts (slots every 50 mm, black
        /// rubber feet), four chipboard boards each held in a C-shaped steel edge channel on all four sides and braced underneath by
        /// two hat sections. The open side faces +Z.
        /// </summary>
        private static void BuildShelf(Transform root)
        {
            const float halfX = ShelfWidth / 2f;
            const float halfZ = ShelfDepth / 2f;
            const float sheet = 0.0025f;
            const float flange = 0.038f;
            const float foot = 0.012f;
            const float round = 0.0008f;

            var steel = new GrayboxMeshBuilder();
            var slots = new GrayboxMeshBuilder();
            var feet = new GrayboxMeshBuilder();
            var boards = new GrayboxMeshBuilder();

            // Angle posts: one flange across the front (or back), one across the side, the corner outside; slots on both.
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    float cx = sx * (halfX - flange / 2f);
                    float cz = sz * (halfZ - flange / 2f);
                    var start = new Vector3(0f, foot, 0f);
                    var end = new Vector3(0f, ShelfHeight, 0f);
                    float z0 = sz > 0 ? halfZ - sheet : -halfZ;
                    float x0 = sx > 0 ? halfX - sheet : -halfX;
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(cx - flange / 2f, cx + flange / 2f, z0, z0 + sheet, round, 1),
                        start, end, Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(x0, x0 + sheet, cz - flange / 2f, cz + flange / 2f, round, 1),
                        start, end, Vector3.right, Vector3.forward, Vector3.down, Vector3.up);

                    for (float y = 0.05f; y < ShelfHeight - 0.04f; y += 0.05f)
                    {
                        const float slotWidth = 0.005f;
                        const float slotHeight = 0.012f;
                        const float slotDepth = 0.0004f;
                        float faceZ = sz * halfZ;
                        float faceX = sx * halfX;
                        slots.AddBox(new Vector3(cx - slotWidth / 2f, y - slotHeight / 2f, Mathf.Min(faceZ, faceZ + sz * slotDepth)),
                            new Vector3(cx + slotWidth / 2f, y + slotHeight / 2f, Mathf.Max(faceZ, faceZ + sz * slotDepth)), false, false);
                        slots.AddBox(new Vector3(Mathf.Min(faceX, faceX + sx * slotDepth), y - slotHeight / 2f, cz - slotWidth / 2f),
                            new Vector3(Mathf.Max(faceX, faceX + sx * slotDepth), y + slotHeight / 2f, cz + slotWidth / 2f), false, false);
                    }
                    feet.AddRoundedBlock(new Vector3(cx, 0f, cz), flange, flange, 0.008f, foot, 0.003f);
                }
            }

            // Boards: chipboard in a C channel (web, bottom flange, upper lip) on every side, two hat sections underneath.
            const float channelBottom = 0.018f;
            const float channelTop = 0.022f;
            const float flangeWidth = 0.026f;
            const float lipWidth = 0.012f;
            const float boardThickness = 0.016f;
            float railHalfX = halfX - 2f * sheet;
            float railHalfZ = halfZ - 2f * sheet;
            foreach (float top in ShelfBoardTops)
            {
                float y0 = top - channelBottom;
                float y1 = top + channelTop;
                // Front and back channels run along X, the side ones along Z. `outer` is the face of the web away from the middle.
                foreach (int s in new[] { -1, 1 })
                {
                    float outerZ = s * (halfZ - sheet);
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(Mathf.Min(outerZ, outerZ - s * sheet), Mathf.Max(outerZ, outerZ - s * sheet), y0, y1, round, 1),
                        new Vector3(-railHalfX, 0f, 0f), new Vector3(railHalfX, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(Mathf.Min(outerZ, outerZ - s * flangeWidth), Mathf.Max(outerZ, outerZ - s * flangeWidth), y0, y0 + sheet, round, 1),
                        new Vector3(-railHalfX, 0f, 0f), new Vector3(railHalfX, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(Mathf.Min(outerZ, outerZ - s * lipWidth), Mathf.Max(outerZ, outerZ - s * lipWidth), y1 - sheet, y1, round, 1),
                        new Vector3(-railHalfX, 0f, 0f), new Vector3(railHalfX, 0f, 0f), Vector3.forward, Vector3.up, Vector3.left, Vector3.right);

                    float outerX = s * (halfX - sheet);
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(Mathf.Min(outerX, outerX - s * sheet), Mathf.Max(outerX, outerX - s * sheet), y0, y1, round, 1),
                        new Vector3(0f, 0f, -railHalfZ), new Vector3(0f, 0f, railHalfZ), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(Mathf.Min(outerX, outerX - s * flangeWidth), Mathf.Max(outerX, outerX - s * flangeWidth), y0, y0 + sheet, round, 1),
                        new Vector3(0f, 0f, -railHalfZ), new Vector3(0f, 0f, railHalfZ), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                    steel.AddExtrusion(GrayboxMeshBuilder.RoundedRectLoop(Mathf.Min(outerX, outerX - s * lipWidth), Mathf.Max(outerX, outerX - s * lipWidth), y1 - sheet, y1, round, 1),
                        new Vector3(0f, 0f, -railHalfZ), new Vector3(0f, 0f, railHalfZ), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

                    // Hat section under the board: a bottom plate and two upright sides.
                    float bx = s * 0.22f;
                    float zEnd = halfZ - 2f * sheet;
                    steel.AddBox(new Vector3(bx - 0.014f, y0 - 0.030f, -zEnd), new Vector3(bx + 0.014f, y0 - 0.030f + sheet, zEnd));
                    steel.AddBox(new Vector3(bx - 0.014f, y0 - 0.030f, -zEnd), new Vector3(bx - 0.014f + sheet, y0, zEnd));
                    steel.AddBox(new Vector3(bx + 0.014f - sheet, y0 - 0.030f, -zEnd), new Vector3(bx + 0.014f, y0, zEnd));
                }
                boards.AddRoundedBlock(new Vector3(0f, top - boardThickness, 0f), 2f * (railHalfX - 0.002f), 2f * (railHalfZ - 0.002f), 0.003f, boardThickness, 0.0015f);
            }

            steel.Attach("Steel", root, SurfaceMaterial.GalvanisedSteel);
            slots.Attach("Slots", root, SurfaceMaterial.BlackHousing);
            feet.Attach("Feet", root, SurfaceMaterial.BlackHousing);
            boards.Attach("Boards", root, SurfaceMaterial.ShelfBoard);
            AddBoxCollider(root, new Vector3(0f, ShelfHeight / 2f, 0f), new Vector3(ShelfWidth, ShelfHeight, ShelfDepth));
        }

        // Cardboard boxes after two product photos: a lidded box (the lid overlaps the base) and a shipping carton (flaps meeting in a
        // seam, handle holes in both end faces). Each in two sizes, a cube and a lower square. Pivot on the floor in the middle, the
        // handle holes face +Z and -Z.
        public const float LidBoxSize = 0.28f;
        public const float LidCubeHeight = 0.28f;
        public const float LidFlatHeight = 0.14f;
        public const float CartonBoxSize = 0.30f;
        public const float CartonCubeHeight = 0.30f;
        public const float CartonFlatHeight = 0.17f;

        /// <summary>Box with a loose lid: a rounded base with the lid slipped over its top, a visible step where they meet.</summary>
        private static void BuildLidBox(Transform root, float size, float height)
        {
            const float lidOverhang = 0.004f;
            float lidHeight = Mathf.Min(0.06f, height * 0.3f);
            var cardboard = new GrayboxMeshBuilder();
            var gap = new GrayboxMeshBuilder();
            cardboard.AddRoundedBlock(Vector3.zero, size, size, 0.004f, height - 0.004f, 0.003f);
            cardboard.AddRoundedBlock(new Vector3(0f, height - lidHeight, 0f), size + 2f * lidOverhang, size + 2f * lidOverhang, 0.006f, lidHeight, 0.004f);
            // The dark joint at the lid's lower edge, where it casts a little shadow on the base.
            gap.AddBox(new Vector3(-size / 2f - 0.0006f, height - lidHeight - 0.0015f, -size / 2f - 0.0006f), new Vector3(size / 2f + 0.0006f, height - lidHeight + 0.0005f, size / 2f + 0.0006f), false, false);
            gap.Attach("Joint", root, SurfaceMaterial.CardboardDark);
            cardboard.Attach("Cardboard", root, SurfaceMaterial.Cardboard);
        }

        /// <summary>Shipping carton: rounded body, the seam of the two top flaps along Z and an oval handle hole in each end face.</summary>
        private static void BuildCartonBox(Transform root, float size, float height)
        {
            var cardboard = new GrayboxMeshBuilder();
            var seam = new GrayboxMeshBuilder();
            var rim = new GrayboxMeshBuilder();
            var hole = new GrayboxMeshBuilder();
            cardboard.AddRoundedBlock(Vector3.zero, size, size, 0.004f, height, 0.004f);
            seam.AddBox(new Vector3(-0.0012f, height, -size / 2f + 0.002f), new Vector3(0.0012f, height + 0.0004f, size / 2f - 0.002f), true, false);
            float holeY = height * 0.66f;
            var rimFace = new GrayboxMeshBuilder();
            rimFace.AddRoundedSlab(new Vector3(0f, holeY, size / 2f - 0.0002f), 0.118f, 0.036f, 0.017f, 0.0008f, 0.0003f, 6);
            var holeFace = new GrayboxMeshBuilder();
            holeFace.AddRoundedSlab(new Vector3(0f, holeY, size / 2f + 0.0004f), 0.104f, 0.024f, 0.011f, 0.0008f, 0.0002f, 6);
            rim.Append(rimFace, Vector3.zero, Quaternion.identity);
            rim.Append(rimFace, Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            hole.Append(holeFace, Vector3.zero, Quaternion.identity);
            hole.Append(holeFace, Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            cardboard.Attach("Cardboard", root, SurfaceMaterial.Cardboard);
            seam.Attach("Seam", root, SurfaceMaterial.CardboardDark);
            rim.Attach("HoleRim", root, SurfaceMaterial.CardboardEdge);
            hole.Attach("Hole", root, SurfaceMaterial.CardboardDark);
        }

        /// <summary>
        /// Surface-mounted LED batten 1.5 m long (along Z) for rooms without a suspended ceiling, after a tri-proof fitting: a white
        /// powder-coated tray with rounded lower edges, a curved opal diffuser along its whole length, two steel retaining clips and
        /// rounded end caps. Pivot on the ceiling surface, hanging down. A wide downward spot without shadows.
        /// </summary>
        private static void BuildLedBatten(Transform root)
        {
            const float length = 1.5f;
            const float cap = 0.012f;
            const float halfTray = 0.060f;
            const float trayDepth = 0.032f;
            const float halfLens = 0.050f;
            const float lensDepth = 0.026f;
            float half = length / 2f;

            // The diffuser: a half ellipse hanging below the tray, convex, counter-clockwise in (x, y).
            List<Vector2> Lens(float lensHalf, float depth, float chordY)
            {
                var loop = new List<Vector2>();
                const int steps = 16;
                for (int i = 0; i <= steps; i++)
                {
                    float angle = Mathf.Lerp(180f, 360f, i / (float)steps) * Mathf.Deg2Rad;
                    loop.Add(new Vector2(Mathf.Cos(angle) * lensHalf, chordY + Mathf.Sin(angle) * depth));
                }
                return loop;
            }

            var tray = GrayboxMeshBuilder.RoundedRectLoop(-halfTray, halfTray, -trayDepth, 0f, new[] { 0.007f, 0.007f, 0.0015f, 0.0015f }, 3);
            var white = new GrayboxMeshBuilder();
            white.AddExtrusion(tray, new Vector3(0f, 0f, -half + cap), new Vector3(0f, 0f, half - cap), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            var capTray = GrayboxMeshBuilder.RoundedRectLoop(-halfTray - 0.002f, halfTray + 0.002f, -trayDepth - 0.002f, 0f, new[] { 0.009f, 0.009f, 0.002f, 0.002f }, 3);
            foreach (int end in new[] { -1, 1 })
            {
                white.AddExtrusion(capTray, new Vector3(0f, 0f, end * (half - cap)), new Vector3(0f, 0f, end * half), Vector3.right, Vector3.up,
                    end < 0 ? Vector3.forward : Vector3.back, end < 0 ? Vector3.back : Vector3.forward);
            }
            white.Attach("Tray", root, SurfaceMaterial.DetectorWhite);

            var lens = new GrayboxMeshBuilder();
            lens.AddExtrusion(Lens(halfLens, lensDepth, -trayDepth + 0.002f), new Vector3(0f, 0f, -half + cap), new Vector3(0f, 0f, half - cap),
                Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            lens.Attach("Diffuser", root, SurfaceMaterial.LightPanel);

            // End caps over the diffuser, a little larger than it, in the tray's white.
            var caps = new GrayboxMeshBuilder();
            foreach (int end in new[] { -1, 1 })
            {
                caps.AddExtrusion(Lens(halfLens + 0.003f, lensDepth + 0.003f, -trayDepth + 0.002f), new Vector3(0f, 0f, end * (half - cap)), new Vector3(0f, 0f, end * half),
                    Vector3.right, Vector3.up, end < 0 ? Vector3.forward : Vector3.back, end < 0 ? Vector3.back : Vector3.forward);
            }
            caps.Attach("EndCaps", root, SurfaceMaterial.DetectorWhite);

            // Two spring clips holding the diffuser: thin steel bands over its curve.
            var clips = new GrayboxMeshBuilder();
            foreach (float z in new[] { -0.55f, 0.55f })
            {
                clips.AddExtrusion(Lens(halfLens + 0.0015f, lensDepth + 0.0015f, -trayDepth + 0.002f), new Vector3(0f, 0f, z - 0.005f), new Vector3(0f, 0f, z + 0.005f),
                    Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            }
            clips.Attach("Clips", root, SurfaceMaterial.ValveSteel);

            AddSpotLight(root, new Vector3(0f, -trayDepth - lensDepth - 0.01f, 0f), 150f, 100f, 7f, 5.5f, LedPanelColor);
        }

        /// <summary>
        /// Landing of a goods lift on a wall (a wall prop, only looks: it never opens and has one collider), the passenger lift's
        /// design in galvanised steel instead of dark stainless, heavier and plainer: a two-panel centre-opening door 2.0 x 2.2 m
        /// whose meeting edges are chamfered, a frame round it, a sill, the call buttons on a light plate (round backlit buttons with a
        /// dark arrow each) and the floor indicator with its green digits over the door. The yellow keep-clear hatching on the floor in
        /// front of it is painted by the room.
        /// </summary>
        private static void BuildFreightLift(Transform root)
        {
            const float doorHalf = FreightDoorWidth / 2f;
            const float doorHeight = FreightDoorHeight;
            const float jamb = 0.12f;
            const float frameDepth = 0.06f;
            const float leafWidth = doorHalf + 0.05f;
            const float leafThickness = 0.035f;
            const float leafZ = 0.03f;
            const float chamfer = 0.006f;

            // Doors: one panel per leaf, 5 cm wider than half the opening so they slip behind the frame; the edges where the leaves meet
            // are chamfered on both faces, so the dividing line shows without a gap between them.
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var leaf = GrayboxUtility.CreateGroup(i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3(side * leafWidth / 2f, 0f, 0f);
                float x0 = -leafWidth / 2f;
                float x1 = leafWidth / 2f;
                float z0 = leafZ - leafThickness / 2f;
                float z1 = leafZ + leafThickness / 2f;
                var outline = side < 0
                    ? new[] { new Vector2(x0, z0), new Vector2(x1 - chamfer, z0), new Vector2(x1, z0 + chamfer), new Vector2(x1, z1 - chamfer), new Vector2(x1 - chamfer, z1), new Vector2(x0, z1) }
                    : new[] { new Vector2(x0 + chamfer, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0 + chamfer, z1), new Vector2(x0, z1 - chamfer), new Vector2(x0, z0 + chamfer) };
                var panels = new GrayboxMeshBuilder();
                panels.AddPrism(outline, 0.008f, doorHeight, Vector3.zero);
                panels.Attach("Panels", leaf, SurfaceMaterial.GalvanisedSteel);
            }

            // Landing: frame, sill, call buttons, floor indicator.
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateStandingBox(side < 0 ? "Frame_Left" : "Frame_Right", root, new Vector3(side * (doorHalf + jamb / 2f), 0f, frameDepth / 2f),
                    new Vector3(jamb, doorHeight + jamb, frameDepth), SurfaceMaterial.GalvanisedSteel, false);
            }
            GrayboxUtility.CreateStandingBox("Frame_Head", root, new Vector3(0f, doorHeight, frameDepth / 2f), new Vector3(2f * (doorHalf + jamb), jamb, frameDepth), SurfaceMaterial.GalvanisedSteel, false);
            GrayboxUtility.CreateStandingBox("Sill", root, new Vector3(0f, 0f, 0.1f), new Vector3(2f * doorHalf + 0.1f, 0.008f, 0.2f), SurfaceMaterial.FloorVent, false);

            // Call buttons on a small light plate, as at the passenger lift: two round backlit buttons, each with a dark arrow (up, down).
            float buttonsX = -(doorHalf + jamb + 0.2f);
            GrayboxUtility.CreateBox("CallButtons", root, new Vector3(buttonsX, 1.0f, 0.008f), new Vector3(0.09f, 0.18f, 0.016f), SurfaceMaterial.DetectorWhite, false);
            var arrows = new GrayboxMeshBuilder();
            foreach (var (y, name, up) in new[] { (1.04f, "Up", true), (0.96f, "Down", false) })
            {
                var button = GrayboxUtility.CreateCylinder($"Button_{name}", root, new Vector3(buttonsX, y, 0.0185f), 0.02f, 0.006f, SurfaceMaterial.LightPanel);
                button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var ring = GrayboxUtility.CreateCylinder($"ButtonRing_{name}", root, new Vector3(buttonsX, y, 0.0165f), 0.025f, 0.002f, SurfaceMaterial.GalvanisedSteel);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                const float zArrow = 0.0221f;
                if (up) arrows.AddFlatTriangle(new Vector3(buttonsX, y + 0.007f, zArrow), new Vector3(buttonsX - 0.007f, y - 0.005f, zArrow), new Vector3(buttonsX + 0.007f, y - 0.005f, zArrow), Vector3.forward);
                else arrows.AddFlatTriangle(new Vector3(buttonsX, y - 0.007f, zArrow), new Vector3(buttonsX + 0.007f, y + 0.005f, zArrow), new Vector3(buttonsX - 0.007f, y + 0.005f, zArrow), Vector3.forward);
            }
            arrows.Attach("Button_Arrows", root, SurfaceMaterial.BlackHousing);

            float indicatorY = doorHeight + jamb + 0.1f;
            GrayboxUtility.CreateBox("Indicator", root, new Vector3(0f, indicatorY, 0.015f), new Vector3(0.3f, 0.1f, 0.03f), SurfaceMaterial.GalvanisedSteel, false);
            GrayboxUtility.CreateBox("Indicator_Display", root, new Vector3(0f, indicatorY, 0.031f), new Vector3(0.22f, 0.06f, 0.002f), SurfaceMaterial.Screen, false);
            var indicatorDigits = new GrayboxMeshBuilder();
            AddSegmentText(indicatorDigits, LiftFloorText, new Vector3(0f, indicatorY, 0.0322f), 0.04f);
            indicatorDigits.Attach("Indicator_Digits", root, SurfaceMaterial.SafetyGreen);

            AddBoxCollider(root, new Vector3(0f, (doorHeight + jamb) / 2f, frameDepth / 2f), new Vector3(2f * (doorHalf + jamb), doorHeight + jamb, frameDepth));
        }
    }
}
