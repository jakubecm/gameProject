using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Doors, access readers, ventilation grilles, fire and gas safety devices and the light fittings of the
    /// technical spaces. Same pivot conventions as the other props.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>Distance of the room door leaves from the wall center: directly in front of the 4 cm door frame casing, with a 5 mm clearance.</summary>
        public const float RoomDoorLeafOffset = RoomContract.WallThickness / 2f + 0.045f;

        /// <summary>Along the wall from the opening's middle: the access reader on the side the leaves slide on.</summary>
        public const float ReaderAlongWall = 2.59f;

        /// <summary>Along the wall on the other side: the reader beside the frame, inside the 3 m wide corridor arm.</summary>
        public const float OuterReaderAlongWall = 1.37f;

        public const float ReaderHeight = 1.1f;

        /// <summary>Height of the manual call points and the gas release station.</summary>
        public const float CallPointHeight = 1.4f;

        /// <summary>
        /// Automatic bi-parting sliding glass door for a room opening (2.4 x 2.6 m). The pivot is the opening pose
        /// (<see cref="RoomContract.GetOpeningPose"/>); +Z is the owner's side, where the leaves slide along the wall
        /// under the operator box. Photo cells on the frame on both sides, an access reader with a status LED on both
        /// sides, and an exit sign on the front of the operator box, on the owner's side only: the owner is the room, so
        /// the sign shows the way out of the room into the corridor, and the corridor side has none (an exit sign there
        /// would point into a dead end).
        /// </summary>
        private static void BuildRoomSlidingDoor(Transform root)
        {
            float width = RoomContract.OpeningWidth;
            float height = RoomContract.OpeningHeight;
            float half = width / 2f;
            float ownerFace = RoomContract.WallThickness / 2f;
            // The neighbour's wall stands back to back with the owner's, so its face is one more wall thickness out.
            float otherFace = -(ownerFace + RoomContract.WallThickness);
            const float casing = 0.04f;

            var leds = new System.Collections.Generic.List<Renderer>();
            var leaves = BuildLeaves(root, width, height - 0.01f, RoomDoorLeafOffset);

            // Slim header (the operator cover) over the opening, standing right on the leaves and the casing, in the colour of the lift steel,
            // with the photo cell in the middle of its front: a dark lens module with a small status light.
            float operatorEnd = half + half + 0.1f;
            float headerFront = RoomDoorLeafOffset + 0.07f;
            var header = new GrayboxMeshBuilder();
            var sensor = new GrayboxMeshBuilder();
            var sensorLed = new GrayboxMeshBuilder();
            header.AddBox(new Vector3(-operatorEnd, height, RoomDoorLeafOffset), new Vector3(operatorEnd, height + 0.10f, headerFront));
            header.Attach("Header", root, SurfaceMaterial.StainlessSteel);
            // The photo cell: a black landscape housing like the access reader's (125 x 55 x 20 mm, rounded) on the middle of the header,
            // with one round status light exactly in its centre.
            sensor.AddRoundedSlab(new Vector3(0f, height + 0.05f, headerFront), 0.125f, 0.055f, 0.008f, 0.02f, 0.004f, 4);
            sensorLed.AddLathe(new[] { GrayboxMeshBuilder.P(0f, 0f, true), GrayboxMeshBuilder.P(0.0065f, 0f, true), GrayboxMeshBuilder.P(0.0065f, 0.0015f, true), GrayboxMeshBuilder.P(0f, 0.0015f, true) }, 20, true,
                new Vector3(0f, height + 0.05f, headerFront + 0.02f), Quaternion.Euler(90f, 0f, 0f));
            sensor.Attach("PhotoCell_In", root, SurfaceMaterial.ReaderBlack);
            sensorLed.Attach("PhotoCell_In_Led", root, SurfaceMaterial.SafetyGreen);
            // The same photo cell on the corridor side, on the wall over the opening, facing the corridor.
            var outGroup = GrayboxUtility.CreateGroup("PhotoCell_Out", root);
            outGroup.localPosition = new Vector3(0f, height + 0.05f, otherFace);
            outGroup.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var sensorOut = new GrayboxMeshBuilder();
            var sensorOutLed = new GrayboxMeshBuilder();
            sensorOut.AddRoundedSlab(Vector3.zero, 0.125f, 0.055f, 0.008f, 0.02f, 0.004f, 4);
            sensorOutLed.AddLathe(new[] { GrayboxMeshBuilder.P(0f, 0f, true), GrayboxMeshBuilder.P(0.0065f, 0f, true), GrayboxMeshBuilder.P(0.0065f, 0.0015f, true), GrayboxMeshBuilder.P(0f, 0.0015f, true) }, 20, true,
                new Vector3(0f, 0f, 0.02f), Quaternion.Euler(90f, 0f, 0f));
            sensorOut.Attach("Housing", outGroup, SurfaceMaterial.ReaderBlack);
            sensorOutLed.Attach("Led", outGroup, SurfaceMaterial.SafetyGreen);

            leds.Add(BuildReader(root, "Reader_In", new Vector3(ReaderAlongWall, ReaderHeight, ownerFace), 0f));
            leds.Add(BuildReader(root, "Reader_Out", new Vector3(OuterReaderAlongWall, ReaderHeight, otherFace), 180f));

            // The exit sign hangs on the wall on the room side, facing into the room, exactly midway between the top of the header and the suspended ceiling.
            var exit = Place(GrayboxProp.ExitSign, root, new Vector3(0f, (height + 0.10f + RoomContract.CeilingHeight - CeilingGridDepth) / 2f, ownerFace + 0.01f), 0f);
            exit.name = "ExitSign";

            ConfigureDoor(root, leaves, half, leds.ToArray(), new Vector3(0f, 1f, (ownerFace + otherFace) / 2f), new Vector3(width, 2f, 3.6f));
        }

        /// <summary>
        /// Automatic sliding door at the end of a contained cold aisle: ONE leaf, which always slides to the LEFT as seen from outside (local +X, over the end of
        /// the left-hand rack row) so the RFID reader can sit on the right, on the end of the right-hand row, as far from the door's edge as the reader of a room door
        /// is (<see cref="ReaderEdgeGap"/>). The leaf is built exactly like a room door's (frame, one clear pane, <see cref="BuildLeaves"/>) but in the aisle's black. It is the
        /// full width of the aisle plus 2 cm overlap each side and touches the rack ends (no gap), so a closed door shuts the whole gap between the two rack rows, from the
        /// floor (no floor rail at all) to the header, whose top is flush with the containment roof; a jamb on the right closes the gap there.
        /// Pivot: the middle of the aisle end on the floor, +Z points out of the aisle. A separate prop and type (<see cref="AisleDoor"/>) from the room doors.
        /// </summary>
        private static void BuildAisleSlidingDoor(Transform root)
        {
            float aisleHalf = HallContract.AisleWidth / 2f;
            float top = HallContract.ContainmentHeight + HallContract.ContainmentPanel;
            const float offset = 0.002f;
            const float travel = 1.1f;
            float leafWidth = HallContract.AisleWidth + 0.04f;
            float headerBottom = top - 0.11f;
            var black = new GrayboxMeshBuilder();

            // The jamb on the right (-X) beside the leaf, and the header over the opening and the track to the left, where the leaf parks.
            black.AddBox(new Vector3(-aisleHalf, 0f, 0f), new Vector3(-aisleHalf + 0.03f, top, offset));
            black.AddBox(new Vector3(-aisleHalf, headerBottom, 0f), new Vector3(aisleHalf + travel + 0.06f, top, offset + 0.07f));
            black.Attach("Surround", root, SurfaceMaterial.RackBlack);

            var leaf = BuildLeaves(root, leafWidth, headerBottom - 0.003f, offset, true, SurfaceMaterial.RackBlack)[0];
            // The reader on the end of the right-hand rack row, the same distance from the door's edge as at a room door; the leaf slides away from it.
            var led = BuildReader(root, "Reader", new Vector3(-(aisleHalf + ReaderEdgeGap), ReaderHeight, 0f), 0f);
            ConfigureDoor<AisleDoor>(root, new[] { null, leaf }, travel, new[] { led }, new Vector3(0f, 1f, 0.5f), new Vector3(1.6f, 2f, 3.2f));
        }

        /// <summary>Distance from the edge of a doorway to the centre of its reader (a room door's outer reader: <see cref="OuterReaderAlongWall"/> from the middle, the opening 1.2 m either side).</summary>
        public const float ReaderEdgeGap = OuterReaderAlongWall - RoomContract.OpeningWidth / 2f;

        /// <summary>
        /// Two glass leaves, side by side in the opening, after a product photo of an automatic sliding door: a slim frame of stainless
        /// steel (30 mm stiles, a 50 mm top rail, a 45 mm bottom rail) round one clear glass pane. Each leaf has its own collider.
        /// </summary>
        private static Transform[] BuildLeaves(Transform root, float width, float height, float offset, bool single = false, SurfaceMaterial frameSurface = SurfaceMaterial.StainlessSteel)
        {
            const float frame = 0.03f;
            const float depth = 0.03f;
                        float leafWidth = width / 2f;
            if (single) leafWidth = width;
            var leaves = new Transform[single ? 1 : 2];
            for (int i = 0; i < leaves.Length; i++)
            {
                var leaf = GrayboxUtility.CreateGroup(single ? "Leaf" : i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3(single ? 0f : (i == 0 ? -1f : 1f) * leafWidth / 2f, 0f, offset + depth / 2f);
                var steel = new GrayboxMeshBuilder();
                var glass = new GrayboxMeshBuilder();
                float half = leafWidth / 2f;
                float d = depth / 2f;
                // Stiles: the outer one a plain bar, the one where the leaves meet chamfered on both faces (6 mm), so the dividing line
                // shows between the two leaves although they touch.
                const float chamfer = 0.006f;
                var outerStile = i == 0 ? new Vector2(-half, -half + frame) : new Vector2(half - frame, half);
                steel.AddBox(new Vector3(outerStile.x, 0.005f, -d), new Vector3(outerStile.y, height, d));
                var meeting = i == 0
                    ? new[] { new Vector2(half - frame, -d), new Vector2(half - chamfer, -d), new Vector2(half, -d + chamfer), new Vector2(half, d - chamfer), new Vector2(half - chamfer, d), new Vector2(half - frame, d) }
                    : new[] { new Vector2(-half + chamfer, -d), new Vector2(-half + frame, -d), new Vector2(-half + frame, d), new Vector2(-half + chamfer, d), new Vector2(-half, d - chamfer), new Vector2(-half, -d + chamfer) };
                if (single) steel.AddBox(new Vector3(half - frame, 0.005f, -d), new Vector3(half, height, d));
                else steel.AddPrism(meeting, 0.005f, height, Vector3.zero);
                steel.AddBox(new Vector3(-half + frame, height - 0.05f, -d), new Vector3(half - frame, height, d));
                steel.AddBox(new Vector3(-half + frame, 0.005f, -d), new Vector3(half - frame, 0.05f, d));
                glass.AddBox(new Vector3(-half + frame, 0.05f, -0.005f), new Vector3(half - frame, height - 0.05f, 0.005f));
                steel.Attach("Frame", leaf, frameSurface);
                glass.Attach("Glass", leaf, SurfaceMaterial.Glass);
                AddBoxCollider(leaf, new Vector3(0f, height / 2f, 0f), new Vector3(leafWidth, height, depth));
                leaves[i] = leaf;
            }
            return leaves;
        }

        private static void ConfigureDoor(Transform root, Transform[] leaves, float travel, Renderer[] leds, Vector3 sensorCenter, Vector3 sensorSize) =>
            ConfigureDoor<AutoSlidingDoor>(root, leaves, travel, leds, sensorCenter, sensorSize);

        private static void ConfigureDoor<T>(Transform root, Transform[] leaves, float travel, Renderer[] leds, Vector3 sensorCenter, Vector3 sensorSize) where T : AutoSlidingDoor
        {
            var door = root.gameObject.AddComponent<T>();
            var serialized = new SerializedObject(door);
            var leafProperty = serialized.FindProperty("leaves");
            leafProperty.arraySize = leaves.Length;
            for (int i = 0; i < leaves.Length; i++)
            {
                leafProperty.GetArrayElementAtIndex(i).objectReferenceValue = leaves[i];
            }
            serialized.FindProperty("travel").floatValue = travel;
            serialized.FindProperty("sensorCenter").vector3Value = sensorCenter;
            serialized.FindProperty("sensorSize").vector3Value = sensorSize;
            var ledProperty = serialized.FindProperty("readerLeds");
            ledProperty.arraySize = leds.Length;
            for (int i = 0; i < leds.Length; i++)
            {
                ledProperty.GetArrayElementAtIndex(i).objectReferenceValue = leds[i];
            }
            serialized.FindProperty("unlockedLed").objectReferenceValue = GrayboxMaterials.Get(SurfaceMaterial.SafetyGreen);
            serialized.FindProperty("lockedLed").objectReferenceValue = GrayboxMaterials.Get(SurfaceMaterial.LedRed);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Gap between a wall grille and the floor or the ceiling: a low grille starts this high above the floor, a high
        /// grille ends this far below the ceiling (the suspended ceiling, or the girders in the hall), the same in every
        /// room, corridor and hall.
        /// </summary>
        public const float GrilleGap = 0.15f;

        /// <summary>Height of <see cref="BuildWallVentGrille"/>.</summary>
        public const float WallGrilleHeight = 0.3f;

        /// <summary>Center height of a small high grille under a ceiling at <paramref name="ceiling"/>.</summary>
        public static float HighGrilleCenter(float ceiling) => ceiling - GrilleGap - WallGrilleHeight / 2f;

        /// <summary>Width and height of <see cref="BuildFloorVentGrille"/> (it is square).</summary>
        public const float FloorGrilleSize = 0.6f;

        /// <summary>LED strip 1.2 m long (along Z) under the containment roof of a cold aisle; pivot on the roof's underside.</summary>
        private static void BuildAisleLight(Transform root)
        {
            const float length = 1.2f;
            // A white aluminium channel with rounded edges, end caps, an opal diffuser and two mounting clips on the roof panels.
            var body = new GrayboxMeshBuilder();
            var diffuser = new GrayboxMeshBuilder();
            body.AddRoundedBlock(new Vector3(0f, -0.030f, 0f), 0.06f, length, 0.010f, 0.030f, 0.004f, 3);
            foreach (int end in new[] { -1, 1 })
            {
                body.AddBox(new Vector3(-0.032f, -0.034f, end * (length / 2f) - (end > 0 ? 0f : 0.004f)), new Vector3(0.032f, -0.002f, end * (length / 2f) + (end > 0 ? 0.004f : 0f)));
                body.AddBox(new Vector3(-0.012f, -0.002f, end * 0.45f - 0.015f), new Vector3(0.012f, 0.002f, end * 0.45f + 0.015f));
            }
            diffuser.AddRoundedBlock(new Vector3(0f, -0.036f, 0f), 0.048f, length - 0.02f, 0.006f, 0.006f, 0.002f, 2);
            body.Attach("Body", root, SurfaceMaterial.Trunking);
            diffuser.Attach("Diffuser", root, SurfaceMaterial.LightPanel);
            AddSpotLight(root, new Vector3(0f, -0.05f, 0f), 160f, 110f, 3.5f, 1.8f, LedPanelColor);
        }

        /// <summary>Height of the middle of a light switch (1.05 m, the usual height in Europe, beside the door).</summary>
        public const float LightSwitchHeight = 1.05f;

        /// <summary>Clear opening of the freight lift's landing door (a 2000 kg goods lift).</summary>
        public const float FreightDoorWidth = 2.0f;

        public const float FreightDoorHeight = 2.2f;

        /// <summary>Half the width of everything the freight lift puts on the wall (frame, call station).</summary>
        public const float FreightLiftHalfWidth = 1.4f;

        /// <summary>
        /// The passenger lift, which is also the evacuation lift, seen from the corridor: a wall prop on the corridor face
        /// of the landing wall at the end of the lift end tile (<see cref="RoomContract.LiftWallFace"/>; +Z into the
        /// corridor). The landing wall and the shaft are room structure; this prop is everything else (sizes from
        /// <see cref="RoomContract"/>):
        /// - a two-panel centre-opening stainless door 1.1 x 2.1 m: the landing panels behind the wall and the car panels
        ///   behind them move together, like a real lift, and open by themselves (<see cref="AutoSlidingDoor"/>) for anyone
        ///   standing in front of the door or just inside the car;
        /// - the car for 6 to 8 people (1.6 x 1.5 m inside, 2.3 m high): stainless walls, a mirror on the back wall,
        ///   handrails, a rubber floor, a lit ceiling and the car operating panel; the player starts every run in it;
        /// - on the landing: a stainless frame, the sill, the call buttons, the floor indicator, the exit sign over the door
        ///   (the lift is the way out), the evacuation lift sign (EN 81-72).
        /// </summary>
        /// <summary>Segments (a top, b top right, c bottom right, d bottom, e bottom left, f top left, g middle) of the seven-segment digits.</summary>
        private static readonly Dictionary<char, string> SevenSegment = new Dictionary<char, string>
        {
            { '0', "abcdef" }, { '1', "bc" }, { '2', "abdeg" }, { '3', "abcdg" }, { '4', "bcfg" },
            { '5', "acdfg" }, { '6', "acdefg" }, { '7', "abc" }, { '8', "abcdefg" }, { '9', "abcdfg" }, { '-', "g" },
        };

        /// <summary>
        /// Seven-segment text (digits and minus) of the given character <paramref name="height"/> in the XY plane around
        /// <paramref name="centre"/>, standing <paramref name="depth"/> proud along +Z. Seen from +Z the text reads towards -X (the
        /// viewer's right), like every sign face in these props.
        /// </summary>
        private static void AddSegmentText(GrayboxMeshBuilder builder, string text, Vector3 centre, float height, float depth = 0.001f)
        {
            float w = 0.55f * height;
            float gap = 0.25f * height;
            float t = 0.14f * height;
            float total = text.Length * w + (text.Length - 1) * gap;
            for (int i = 0; i < text.Length; i++)
            {
                float cx = centre.x + total / 2f - w / 2f - i * (w + gap);
                float cy = centre.y;
                float left = cx + w / 2f;
                float right = cx - w / 2f;
                float top = cy + height / 2f;
                float bottom = cy - height / 2f;
                void Seg(float x0, float x1, float y0, float y1) =>
                    builder.AddBox(new Vector3(Mathf.Min(x0, x1), y0, centre.z), new Vector3(Mathf.Max(x0, x1), y1, centre.z + depth), true, true);
                foreach (char segment in SevenSegment[text[i]])
                {
                    switch (segment)
                    {
                        case 'a': Seg(right + t * 0.5f, left - t * 0.5f, top - t, top); break;
                        case 'b': Seg(right, right + t, cy + t * 0.5f, top - t * 0.5f); break;
                        case 'c': Seg(right, right + t, bottom + t * 0.5f, cy - t * 0.5f); break;
                        case 'd': Seg(right + t * 0.5f, left - t * 0.5f, bottom, bottom + t); break;
                        case 'e': Seg(left - t, left, bottom + t * 0.5f, cy - t * 0.5f); break;
                        case 'f': Seg(left - t, left, cy + t * 0.5f, top - t * 0.5f); break;
                        case 'g': Seg(right + t * 0.5f, left - t * 0.5f, cy - t / 2f, cy + t / 2f); break;
                    }
                }
            }
        }

        /// <summary>The floor number the lift shows, in the car and on the landing indicator.</summary>
        public const string LiftFloorText = "-4";

        /// <summary>How far the lift door's photo cells see into the car: less than the player's distance at the start.</summary>
        public const float LiftSensorInCar = 0.35f;

        private static void BuildPassengerLift(Transform root)
        {
            float doorHalf = RoomContract.LiftDoorWidth / 2f;
            float doorHeight = RoomContract.LiftDoorHeight;
            float wall = RoomContract.LiftWallThickness;
            float carHalf = RoomContract.LiftCarWidth / 2f;
            float carHeight = RoomContract.LiftCarHeight;
            // Prop-local Z of the car's inner front and back faces (the car is behind the wall, at -Z).
            float carFront = -(RoomContract.LiftCarFront - RoomContract.LiftWallFace);
            float carBack = carFront - RoomContract.LiftCarDepth;
            const float panel = 0.04f;
            const float roof = 0.1f;

            // Doors: a landing panel and a car panel per leaf, 5 cm wider than half the opening, so they overlap the jambs. The edges
            // where the leaves meet are chamfered on both faces, so the dividing line shows without a gap between them.
            float leafWidth = doorHalf + 0.05f;
            const float chamfer = 0.006f;
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var leaf = GrayboxUtility.CreateGroup(i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3(side * leafWidth / 2f, 0f, 0f);
                var panels = new GrayboxMeshBuilder();
                foreach (var (zCentre, thickness) in new[] { (-wall - 0.025f, 0.035f), (carFront + 0.06f, 0.03f) })
                {
                    float x0 = -leafWidth / 2f;
                    float x1 = leafWidth / 2f;
                    float z0 = zCentre - thickness / 2f;
                    float z1 = zCentre + thickness / 2f;
                    var outline = side < 0
                        ? new[] { new Vector2(x0, z0), new Vector2(x1 - chamfer, z0), new Vector2(x1, z0 + chamfer), new Vector2(x1, z1 - chamfer), new Vector2(x1 - chamfer, z1), new Vector2(x0, z1) }
                        : new[] { new Vector2(x0 + chamfer, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0 + chamfer, z1), new Vector2(x0, z1 - chamfer), new Vector2(x0, z0 + chamfer) };
                    panels.AddPrism(outline, 0f, doorHeight, Vector3.zero);
                }
                panels.Attach("Panels", leaf, SurfaceMaterial.StainlessSteel);
                // One collider over both panels.
                float back = carFront + 0.045f;
                float front = -wall - 0.0075f;
                AddBoxCollider(leaf, new Vector3(0f, doorHeight / 2f, (back + front) / 2f), new Vector3(leafWidth, doorHeight, front - back));
                leaves[i] = leaf;
            }

            // Landing: frame, sill, call buttons, floor indicator, signs.
            const float jamb = 0.08f;
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateStandingBox(side < 0 ? "Frame_Left" : "Frame_Right", root, new Vector3(side * (doorHalf + jamb / 2f), 0f, 0.015f),
                    new Vector3(jamb, doorHeight + jamb, 0.03f), SurfaceMaterial.StainlessSteel, false);
                // The wall's reveals inside the opening are lined with steel too.
                GrayboxUtility.CreateStandingBox(side < 0 ? "Reveal_Left" : "Reveal_Right", root, new Vector3(side * (doorHalf - 0.005f), 0f, -wall / 2f),
                    new Vector3(0.01f, doorHeight, wall), SurfaceMaterial.StainlessSteel, false);
            }
            GrayboxUtility.CreateStandingBox("Frame_Head", root, new Vector3(0f, doorHeight, 0.015f), new Vector3(2f * (doorHalf + jamb), jamb, 0.03f), SurfaceMaterial.StainlessSteel, false);
            GrayboxUtility.CreateStandingBox("Sill", root, new Vector3(0f, 0f, (carFront + 0.08f) / 2f + 0.02f), new Vector3(2f * doorHalf + 0.1f, 0.006f, -carFront + 0.04f), SurfaceMaterial.FloorVent, false);

            // Call buttons on a small light plate, to the right of the door as seen from the corridor: two round buttons, backlit,
            // each with a dark arrow (up, down).
            float buttonsX = -(doorHalf + jamb + 0.2f);
            GrayboxUtility.CreateBox("CallButtons", root, new Vector3(buttonsX, 1.0f, 0.008f), new Vector3(0.09f, 0.18f, 0.016f), SurfaceMaterial.DetectorWhite, false);
            var arrows = new GrayboxMeshBuilder();
            foreach (var (y, name, up) in new[] { (1.04f, "Up", true), (0.96f, "Down", false) })
            {
                var button = GrayboxUtility.CreateCylinder($"Button_{name}", root, new Vector3(buttonsX, y, 0.0185f), 0.02f, 0.006f, SurfaceMaterial.LightPanel);
                button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var ring = GrayboxUtility.CreateCylinder($"ButtonRing_{name}", root, new Vector3(buttonsX, y, 0.0165f), 0.025f, 0.002f, SurfaceMaterial.StainlessSteel);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                const float zArrow = 0.0221f;
                if (up) arrows.AddFlatTriangle(new Vector3(buttonsX, y + 0.007f, zArrow), new Vector3(buttonsX - 0.007f, y - 0.005f, zArrow), new Vector3(buttonsX + 0.007f, y - 0.005f, zArrow), Vector3.forward);
                else arrows.AddFlatTriangle(new Vector3(buttonsX, y - 0.007f, zArrow), new Vector3(buttonsX + 0.007f, y + 0.005f, zArrow), new Vector3(buttonsX - 0.007f, y + 0.005f, zArrow), Vector3.forward);
            }
            arrows.Attach("Button_Arrows", root, SurfaceMaterial.BlackHousing);

            float indicatorY = doorHeight + jamb + 0.1f;
            GrayboxUtility.CreateBox("Indicator", root, new Vector3(0f, indicatorY, 0.015f), new Vector3(0.3f, 0.1f, 0.03f), SurfaceMaterial.StainlessSteel, false);
            GrayboxUtility.CreateBox("Indicator_Display", root, new Vector3(0f, indicatorY, 0.031f), new Vector3(0.22f, 0.06f, 0.002f), SurfaceMaterial.Screen, false);
            var indicatorDigits = new GrayboxMeshBuilder();
            AddSegmentText(indicatorDigits, LiftFloorText, new Vector3(0f, indicatorY, 0.0322f), 0.04f);
            indicatorDigits.Attach("Indicator_Digits", root, SurfaceMaterial.SafetyGreen);
            var exit = Place(GrayboxProp.ExitSign, root, new Vector3(0f, indicatorY + 0.2f, 0f), 0f);
            exit.name = "ExitSign";

            // Car, after a product photo: dark grey matt steel panels with vertical joints, a light floor, a frame round the door, a
            // light strip across the ceiling, a handrail across the whole back wall under the mirror and the operating panel on the
            // right-hand wall. The walls stop the player, so they have colliders; the fittings on them do not.
            var car = GrayboxUtility.CreateGroup("Car", root);
            float outer = carHalf + panel;
            float frontFace = carFront + panel;
            GrayboxUtility.CreateBoxBetween("Floor", car, new Vector3(-carHalf, 0f, carBack), new Vector3(carHalf, 0.008f, carFront), SurfaceMaterial.Furniture);
            GrayboxUtility.CreateBoxBetween("Wall_Back", car, new Vector3(-outer, 0f, carBack - panel), new Vector3(outer, carHeight + roof, carBack), SurfaceMaterial.StainlessSteel, true);
            float middle = (carFront + carBack) / 2f;
            foreach (int side in new[] { -1, 1 })
            {
                string name = side < 0 ? "Left" : "Right";
                GrayboxUtility.CreateBoxBetween($"Wall_{name}", car, new Vector3(side * carHalf, 0f, carBack - panel), new Vector3(side * outer, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
                // The car's front returns beside the door.
                GrayboxUtility.CreateBoxBetween($"Return_{name}", car, new Vector3(side * doorHalf, 0f, carFront), new Vector3(side * carHalf, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
                GrayboxUtility.CreateBoxBetween($"Kickplate_{name}", car, new Vector3(side * (carHalf - 0.003f), 0f, carBack), new Vector3(side * carHalf, 0.15f, carFront), SurfaceMaterial.Trim);
                // The frame round the door, standing 15 mm proud of the front wall.
                GrayboxUtility.CreateBox($"DoorFrame_{name}", car, new Vector3(side * (doorHalf + 0.03f), doorHeight / 2f + 0.03f, carFront - 0.0075f), new Vector3(0.06f, doorHeight + 0.06f, 0.015f), SurfaceMaterial.StainlessSteel, false);
                // A vertical joint between two wall panels, halfway along the side wall.
                GrayboxUtility.CreateBox($"Joint_{name}", car, new Vector3(side * (carHalf - 0.001f), carHeight / 2f, middle), new Vector3(0.003f, carHeight, 0.004f), SurfaceMaterial.BlackHousing, false);
            }
            GrayboxUtility.CreateBoxBetween("Transom", car, new Vector3(-doorHalf, doorHeight, carFront), new Vector3(doorHalf, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
            GrayboxUtility.CreateBox("DoorFrame_Head", car, new Vector3(0f, doorHeight + 0.03f, carFront - 0.0075f), new Vector3(2f * doorHalf + 0.12f, 0.06f, 0.015f), SurfaceMaterial.StainlessSteel, false);
            // Handrail under the mirror: a round steel tube, shorter than the wall (0.25 m clear at each end) and bent back into the
            // wall at both ends, each end covered by a round rosette (the way a real lift rail is finished).
            {
                const float railRadius = 0.0175f;
                const float standOff = 0.065f;
                const float bend = 0.035f;
                float railX = carHalf - 0.25f;
                float railY = 0.90f;
                var rail = new GrayboxMeshBuilder();
                var path = new List<Vector3>();
                void Corner(float centreX, float fromDegrees, float toDegrees)
                {
                    for (int step = 0; step <= 5; step++)
                    {
                        float angle = Mathf.Lerp(fromDegrees, toDegrees, step / 5f) * Mathf.Deg2Rad;
                        path.Add(new Vector3(centreX + Mathf.Cos(angle) * bend, railY, carBack + standOff - bend + Mathf.Sin(angle) * bend));
                    }
                }
                path.Add(new Vector3(-railX, railY, carBack));
                Corner(-railX + bend, 180f, 90f);
                Corner(railX - bend, 90f, 0f);
                path.Add(new Vector3(railX, railY, carBack));
                rail.AddTube(path, railRadius, 12, Vector3.zero, Quaternion.identity);
                foreach (float x in new[] { -railX, railX })
                {
                    rail.AddLathe(new[] { GrayboxMeshBuilder.P(0f, 0f, true), GrayboxMeshBuilder.P(0.032f, 0f, true), GrayboxMeshBuilder.P(0.032f, 0.006f, true), GrayboxMeshBuilder.P(0.024f, 0.010f), GrayboxMeshBuilder.P(0f, 0.010f, true) },
                        24, true, new Vector3(x, railY, carBack), Quaternion.Euler(90f, 0f, 0f));
                }
                rail.Attach("Handrail_Back", car, SurfaceMaterial.StainlessSteel);
            }
            GrayboxUtility.CreateBoxBetween("Mirror", car, new Vector3(-carHalf + 0.05f, 0.95f, carBack), new Vector3(carHalf - 0.05f, 2.1f, carBack + 0.004f), SurfaceMaterial.Glass);
            GrayboxUtility.CreateBoxBetween("Ceiling", car, new Vector3(-outer, carHeight, carBack - panel), new Vector3(outer, carHeight + roof, frontFace), SurfaceMaterial.StainlessSteel, true);
            // Light strip across the ceiling over the door.
            GrayboxUtility.CreateBoxBetween("CeilingLight", car, new Vector3(-carHalf + 0.15f, carHeight - 0.006f, carFront - 0.2f), new Vector3(carHalf - 0.15f, carHeight, carFront - 0.12f), SurfaceMaterial.LightPanel);
            AddSpotLight(car, new Vector3(0f, carHeight - 0.03f, (carFront + carBack) / 2f), 160f, 120f, 3.5f, 2.2f, LedPanelColor);

            // Car operating panel on the right-hand wall (+X, as you face the door), in the middle of the wall panel next to the door (the
            // joint halfway along the wall splits the wall in two): the same light plate and backlit round buttons as the call buttons
            // outside, a display with the green floor number on top, two columns of five buttons.
            float copX = carHalf - 0.012f;
            float copZ = (middle + carFront) / 2f;
            GrayboxUtility.CreateBox("OperatingPanel", car, new Vector3(carHalf - 0.006f, 1.2f, copZ), new Vector3(0.012f, 0.42f, 0.13f), SurfaceMaterial.DetectorWhite, false);
            GrayboxUtility.CreateBox("OperatingPanel_Display", car, new Vector3(copX - 0.0005f, 1.375f, copZ), new Vector3(0.001f, 0.04f, 0.09f), SurfaceMaterial.Screen, false);
            var digitsGroup = GrayboxUtility.CreateGroup("OperatingPanel_Digits", car);
            digitsGroup.localPosition = new Vector3(copX - 0.0012f, 1.375f, copZ);
            digitsGroup.localRotation = Quaternion.Euler(0f, -90f, 0f);
            var carDigits = new GrayboxMeshBuilder();
            AddSegmentText(carDigits, LiftFloorText, Vector3.zero, 0.026f);
            carDigits.Attach("Digits", digitsGroup, SurfaceMaterial.SafetyGreen);
            for (int i = 0; i < 10; i++)
            {
                float y = 1.285f - (i / 2) * 0.055f;
                float z = copZ + (i % 2 == 0 ? 0.03f : -0.03f);
                var ring = GrayboxUtility.CreateCylinder($"OperatingPanel_Ring_{i}", car, new Vector3(copX - 0.0005f, y, z), 0.022f, 0.002f, SurfaceMaterial.StainlessSteel);
                ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                var button = GrayboxUtility.CreateCylinder($"OperatingPanel_Button_{i}", car, new Vector3(copX - 0.0035f, y, z), 0.016f, 0.006f, SurfaceMaterial.LightPanel);
                button.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }

            ConfigureDoor(root, leaves, RoomContract.LiftDoorWidth / 2f, new Renderer[0],
                // Photo cells watch the landing in front of the door and the front of the car, not where the player starts.
                new Vector3(0f, 1f, (1.2f + carFront - LiftSensorInCar) / 2f), new Vector3(1.4f, 2f, 1.2f - carFront + LiftSensorInCar));
            var serialized = new SerializedObject(root.GetComponent<AutoSlidingDoor>());
            serialized.FindProperty("openSeconds").floatValue = 1.4f;
            serialized.FindProperty("closeSeconds").floatValue = 1.8f;
            serialized.FindProperty("holdOpenSeconds").floatValue = 2.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
