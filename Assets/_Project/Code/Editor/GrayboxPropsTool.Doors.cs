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
        /// <summary>Distance of the room door leaves from the wall center, in front of the 4 cm door frame casing.</summary>
        public const float RoomDoorLeafOffset = RoomContract.WallThickness / 2f + 0.06f;

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

            // Operator box with the track over the opening and the leaves' travel.
            float operatorEnd = half + half + 0.1f;
            GrayboxUtility.CreateBoxBetween("Operator", root, new Vector3(-operatorEnd, height + 0.02f, ownerFace + casing),
                new Vector3(operatorEnd, height + 0.32f, ownerFace + casing + 0.16f), SurfaceMaterial.Trim);

            // Photo cells: an emitter and a receiver on the frame, on both sides of the wall.
            foreach (var (face, name) in new[] { (ownerFace + casing, "In"), (otherFace - casing, "Out") })
            {
                float outward = face > 0f ? 1f : -1f;
                foreach (int side in new[] { -1, 1 })
                {
                    GrayboxUtility.CreateBox($"PhotoCell_{name}_{(side < 0 ? "L" : "R")}", root, new Vector3(side * (half + 0.05f), 0.5f, face + outward * 0.008f),
                        new Vector3(0.035f, 0.07f, 0.016f), SurfaceMaterial.Trunking, false);
                }
            }

            leds.Add(BuildReader(root, "Reader_In", new Vector3(ReaderAlongWall, ReaderHeight, ownerFace), 0f));
            leds.Add(BuildReader(root, "Reader_Out", new Vector3(OuterReaderAlongWall, ReaderHeight, otherFace), 180f));

            var exit = Place(GrayboxProp.ExitSign, root, new Vector3(0f, height + 0.17f, ownerFace + casing + 0.16f), 0f);
            exit.name = "ExitSign";

            ConfigureDoor(root, leaves, half, leds.ToArray(), new Vector3(0f, 1f, (ownerFace + otherFace) / 2f), new Vector3(width, 2f, 3.6f));
        }

        /// <summary>
        /// Automatic sliding door at the end of a contained cold aisle (1.8 x 2.0 m): the pivot is the middle of the
        /// aisle end on the floor, +Z points out of the aisle, the leaves slide over the rack row ends outside. The
        /// reader is beside it on the end of the north rack row.
        /// </summary>
        private static void BuildAisleSlidingDoor(Transform root)
        {
            float width = HallContract.AisleWidth;
            float height = RoomContract.RackHeight;
            float half = width / 2f;
            const float offset = 0.03f;
            var leaves = BuildLeaves(root, width, height - 0.01f, offset);
            GrayboxUtility.CreateBoxBetween("Track", root, new Vector3(-width, height, 0f), new Vector3(width, height + 0.08f, offset + 0.07f), SurfaceMaterial.Metal);
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateBox($"PhotoCell_{(side < 0 ? "L" : "R")}", root, new Vector3(side * (half + 0.02f), 0.5f, 0.008f),
                    new Vector3(0.03f, 0.07f, 0.016f), SurfaceMaterial.Trunking, false);
            }
            var led = BuildReader(root, "Reader", new Vector3(-(width + 0.12f), ReaderHeight, 0f), 0f);
            ConfigureDoor(root, leaves, half, new[] { led }, new Vector3(0f, 1f, 0f), new Vector3(width, 2f, 3.2f));
        }

        /// <summary>Two glass leaves in aluminium frames, side by side in the opening; each has its own collider.</summary>
        private static Transform[] BuildLeaves(Transform root, float width, float height, float offset)
        {
            const float frame = 0.035f;
            const float depth = 0.04f;
            float leafWidth = width / 2f;
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var leaf = GrayboxUtility.CreateGroup(i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3((i == 0 ? -1f : 1f) * leafWidth / 2f, 0f, offset + depth / 2f);
                foreach (int side in new[] { -1, 1 })
                {
                    GrayboxUtility.CreateStandingBox(side < 0 ? "Stile_Left" : "Stile_Right", leaf, new Vector3(side * (leafWidth - frame) / 2f, 0.005f, 0f),
                        new Vector3(frame, height - 0.005f, depth), SurfaceMaterial.Metal, false);
                }
                GrayboxUtility.CreateStandingBox("Rail_Top", leaf, new Vector3(0f, height - frame, 0f), new Vector3(leafWidth - 2f * frame, frame, depth), SurfaceMaterial.Metal, false);
                GrayboxUtility.CreateStandingBox("Rail_Bottom", leaf, new Vector3(0f, 0.005f, 0f), new Vector3(leafWidth - 2f * frame, 0.08f, depth), SurfaceMaterial.Metal, false);
                GrayboxUtility.CreateStandingBox("Glass", leaf, new Vector3(0f, 0.085f, 0f), new Vector3(leafWidth - 2f * frame, height - 0.085f - frame, 0.01f), SurfaceMaterial.Glass, false);
                // Safety stripe at eye height, so the glass is seen.
                GrayboxUtility.CreateStandingBox("Marking", leaf, new Vector3(0f, 1.45f, 0.006f), new Vector3(leafWidth - 2f * frame, 0.05f, 0.002f), SurfaceMaterial.Trim, false);
                AddBoxCollider(leaf, new Vector3(0f, height / 2f, 0f), new Vector3(leafWidth, height, depth));
                leaves[i] = leaf;
            }
            return leaves;
        }

        /// <summary>RFID access reader with a status LED: plate on the wall at <paramref name="position"/>, facing yaw.</summary>
        private static Renderer BuildReader(Transform root, string name, Vector3 position, float yaw)
        {
            var reader = GrayboxUtility.CreateGroup(name, root);
            reader.localPosition = position;
            reader.localRotation = Quaternion.Euler(0f, yaw, 0f);
            GrayboxUtility.CreateBox("Body", reader, new Vector3(0f, 0f, 0.0125f), new Vector3(0.08f, 0.13f, 0.025f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateBox("Pad", reader, new Vector3(0f, -0.015f, 0.0255f), new Vector3(0.06f, 0.06f, 0.002f), SurfaceMaterial.Screen, false);
            var led = GrayboxUtility.CreateBox("Led", reader, new Vector3(0f, 0.045f, 0.027f), new Vector3(0.04f, 0.01f, 0.005f), SurfaceMaterial.SafetyGreen, false);
            return led.GetComponent<MeshRenderer>();
        }

        private static void ConfigureDoor(Transform root, Transform[] leaves, float travel, Renderer[] leds, Vector3 sensorCenter, Vector3 sensorSize)
        {
            var door = root.gameObject.AddComponent<AutoSlidingDoor>();
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
        /// Small wall ventilation grille 600 x 300 mm with horizontal louvers, mounted high up under the ceiling (exhaust
        /// and supply air); the pivot is the middle of its back.
        /// </summary>
        private static void BuildWallVentGrille(Transform root)
        {
            const float width = 0.6f;
            const float height = WallGrilleHeight;
            const float depth = 0.03f;
            const float rim = 0.025f;
            GrayboxUtility.CreateBox("Frame_Top", root, new Vector3(0f, (height - rim) / 2f, depth / 2f), new Vector3(width, rim, depth), SurfaceMaterial.CeilingGrid, false);
            GrayboxUtility.CreateBox("Frame_Bottom", root, new Vector3(0f, -(height - rim) / 2f, depth / 2f), new Vector3(width, rim, depth), SurfaceMaterial.CeilingGrid, false);
            GrayboxUtility.CreateBox("Frame_Left", root, new Vector3(-(width - rim) / 2f, 0f, depth / 2f), new Vector3(rim, height - 2f * rim, depth), SurfaceMaterial.CeilingGrid, false);
            GrayboxUtility.CreateBox("Frame_Right", root, new Vector3((width - rim) / 2f, 0f, depth / 2f), new Vector3(rim, height - 2f * rim, depth), SurfaceMaterial.CeilingGrid, false);
            GrayboxUtility.CreateBox("Back", root, new Vector3(0f, 0f, 0.002f), new Vector3(width - 2f * rim, height - 2f * rim, 0.004f), SurfaceMaterial.Upholstery, false);
            for (int i = 0; i < 6; i++)
            {
                float y = -height / 2f + rim + 0.02f + i * 0.04f;
                var blade = GrayboxUtility.CreateBox($"Louver_{i}", root, new Vector3(0f, y, depth / 2f), new Vector3(width - 2f * rim, 0.004f, 0.03f), SurfaceMaterial.CeilingGrid, false);
                blade.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            }
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

        /// <summary>
        /// Large square ventilation grille 600 x 600 mm at the foot of a wall (supply, return or transfer air at floor
        /// level), <see cref="GrilleGap"/> above the floor: just big enough for a person to crawl into the duct behind it.
        /// A deep steel frame with four screws, a dark plenum and fixed louvers tilted down, a mesh behind them. A
        /// wall prop: pivot on the floor at the wall surface.
        /// </summary>
        private static void BuildFloorVentGrille(Transform root)
        {
            const float size = FloorGrilleSize;
            const float depth = 0.05f;
            const float rim = 0.045f;
            float bottom = GrilleGap;
            float top = bottom + size;
            float inner = size - 2f * rim;
            GrayboxUtility.CreateBox("Plenum", root, new Vector3(0f, bottom + size / 2f, 0.004f), new Vector3(inner, inner, 0.008f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateBox("Mesh", root, new Vector3(0f, bottom + size / 2f, 0.012f), new Vector3(inner, inner, 0.002f), SurfaceMaterial.WireMesh, false);
            GrayboxUtility.CreateBoxBetween("Frame_Top", root, new Vector3(-size / 2f, top - rim, 0f), new Vector3(size / 2f, top, depth), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("Frame_Bottom", root, new Vector3(-size / 2f, bottom, 0f), new Vector3(size / 2f, bottom + rim, depth), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("Frame_Left", root, new Vector3(-size / 2f, bottom + rim, 0f), new Vector3(-size / 2f + rim, top - rim, depth), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("Frame_Right", root, new Vector3(size / 2f - rim, bottom + rim, 0f), new Vector3(size / 2f, top - rim, depth), SurfaceMaterial.CeilingGrid);
            foreach (int x in new[] { -1, 1 })
            {
                foreach (int y in new[] { -1, 1 })
                {
                    var screw = GrayboxUtility.CreateCylinder($"Screw_{(y > 0 ? "T" : "B")}{(x > 0 ? "R" : "L")}", root,
                        new Vector3(x * (size / 2f - rim / 2f), bottom + size / 2f + y * (size / 2f - rim / 2f), depth + 0.002f), 0.008f, 0.004f, SurfaceMaterial.Metal);
                    screw.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }
            const int louvers = 10;
            float pitch = inner / louvers;
            for (int i = 0; i < louvers; i++)
            {
                float y = bottom + rim + pitch * (i + 0.5f);
                var blade = GrayboxUtility.CreateBox($"Louver_{i}", root, new Vector3(0f, y, depth / 2f), new Vector3(inner, 0.005f, 0.04f), SurfaceMaterial.CeilingGrid, false);
                blade.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
            }
            // A center mullion stiffens the louvers.
            GrayboxUtility.CreateBoxBetween("Mullion", root, new Vector3(-0.01f, bottom + rim, 0.01f), new Vector3(0.01f, top - rim, depth - 0.005f), SurfaceMaterial.CeilingGrid);
        }

        /// <summary>Width and height of <see cref="BuildFloorVentGrille"/> (it is square).</summary>
        public const float FloorGrilleSize = 0.6f;

        /// <summary>Lay-in four-way ceiling diffuser (supply or return air) for one 600 mm tile, flush with the T-bars.</summary>
        private static void BuildCeilingDiffuser(Transform root)
        {
            float y = -(CeilingGridDepth + 0.004f);
            GrayboxUtility.CreateBox("Plate", root, new Vector3(0f, y, 0f), new Vector3(0.6f, 0.008f, 0.6f), SurfaceMaterial.CeilingGrid, false);
            GrayboxUtility.CreateBox("Throat", root, new Vector3(0f, y - 0.006f, 0f), new Vector3(0.36f, 0.004f, 0.36f), SurfaceMaterial.Upholstery, false);
            for (int i = 0; i < 3; i++)
            {
                float size = 0.5f - i * 0.1f;
                GrayboxUtility.CreateBox($"Cone_{i}", root, new Vector3(0f, y - 0.012f - i * 0.008f, 0f), new Vector3(size, 0.006f, size), SurfaceMaterial.CeilingGrid, false);
            }
        }

        /// <summary>Manual fire alarm call point (red, break glass); the pivot is the middle of its back.</summary>
        private static void BuildFireCallPoint(Transform root)
        {
            GrayboxUtility.CreateBox("Body", root, new Vector3(0f, 0f, 0.025f), new Vector3(0.09f, 0.09f, 0.05f), SurfaceMaterial.SafetyRed, false);
            GrayboxUtility.CreateBox("Window", root, new Vector3(0f, 0f, 0.0505f), new Vector3(0.06f, 0.06f, 0.002f), SurfaceMaterial.SignFace, false);
        }

        /// <summary>
        /// Manual release station of the inert-gas system (yellow): a covered push button that raises the alarm and
        /// floods the hall, a lock-off switch and a beacon on top. The pivot is the middle of its back.
        /// </summary>
        private static void BuildGasReleaseStation(Transform root)
        {
            GrayboxUtility.CreateBox("Body", root, new Vector3(0f, 0f, 0.035f), new Vector3(0.16f, 0.22f, 0.07f), SurfaceMaterial.SafetyYellow, false);
            GrayboxUtility.CreateBox("Cover", root, new Vector3(0f, 0.02f, 0.072f), new Vector3(0.1f, 0.1f, 0.004f), SurfaceMaterial.Glass, false);
            var button = GrayboxUtility.CreateCylinder("Button", root, new Vector3(0f, 0.02f, 0.072f), 0.03f, 0.012f, SurfaceMaterial.SafetyRed);
            button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GrayboxUtility.CreateBox("LockOff", root, new Vector3(0.045f, -0.075f, 0.075f), new Vector3(0.03f, 0.03f, 0.01f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateBox("Label", root, new Vector3(-0.02f, -0.075f, 0.0705f), new Vector3(0.08f, 0.03f, 0.002f), SurfaceMaterial.SignFace, false);
            GrayboxUtility.CreateCylinder("Beacon", root, new Vector3(0f, 0.14f, 0.035f), 0.03f, 0.06f, SurfaceMaterial.LedRed);
        }

        /// <summary>
        /// Twin-spot emergency light with a status LED; the pivot is the middle of its back. It has no Light component
        /// (emergency lights only come on in a blackout), so it costs nothing to render.
        /// </summary>
        private static void BuildEmergencyLight(Transform root)
        {
            GrayboxUtility.CreateBox("Body", root, new Vector3(0f, 0f, 0.03f), new Vector3(0.3f, 0.1f, 0.06f), SurfaceMaterial.Trunking, false);
            foreach (int side in new[] { -1, 1 })
            {
                var head = GrayboxUtility.CreateCylinder(side < 0 ? "Head_Left" : "Head_Right", root, new Vector3(side * 0.1f, -0.065f, 0.045f), 0.03f, 0.05f, SurfaceMaterial.Trunking);
                head.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            }
            GrayboxUtility.CreateBox("Led", root, new Vector3(0f, 0.02f, 0.0605f), new Vector3(0.012f, 0.012f, 0.002f), SurfaceMaterial.SafetyGreen, false);
        }

        /// <summary>
        /// Surface-mounted LED batten 1.5 m long (along Z) for rooms without a suspended ceiling; pivot on the ceiling
        /// surface. A wide downward spot without shadows.
        /// </summary>
        private static void BuildLedBatten(Transform root)
        {
            const float length = 1.5f;
            GrayboxUtility.CreateBox("Body", root, new Vector3(0f, -0.03f, 0f), new Vector3(0.12f, 0.06f, length), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateBox("Diffuser", root, new Vector3(0f, -0.062f, 0f), new Vector3(0.1f, 0.004f, length - 0.04f), SurfaceMaterial.LightPanel, false);
            AddSpotLight(root, new Vector3(0f, -0.08f, 0f), 150f, 100f, 7f, 5.5f, LedPanelColor);
        }

        /// <summary>LED strip 1.2 m long (along Z) under the containment roof of a cold aisle; pivot on the roof's underside.</summary>
        private static void BuildAisleLight(Transform root)
        {
            const float length = 1.2f;
            GrayboxUtility.CreateBox("Body", root, new Vector3(0f, -0.015f, 0f), new Vector3(0.06f, 0.03f, length), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateBox("Diffuser", root, new Vector3(0f, -0.032f, 0f), new Vector3(0.05f, 0.004f, length - 0.02f), SurfaceMaterial.LightPanel, false);
            AddSpotLight(root, new Vector3(0f, -0.05f, 0f), 160f, 110f, 3.5f, 1.8f, LedPanelColor);
        }

        /// <summary>Height of the middle of a light switch (beside the door, at the handle height of a door).</summary>
        public const float LightSwitchHeight = 1.1f;

        /// <summary>Double rocker light switch on an 80 x 80 mm plate; the pivot is the middle of its back.</summary>
        private static void BuildLightSwitch(Transform root)
        {
            GrayboxUtility.CreateBox("Plate", root, new Vector3(0f, 0f, 0.005f), new Vector3(0.08f, 0.08f, 0.01f), SurfaceMaterial.Trunking, false);
            foreach (int side in new[] { -1, 1 })
            {
                var rocker = GrayboxUtility.CreateBox(side < 0 ? "Rocker_Left" : "Rocker_Right", root, new Vector3(side * 0.0145f, 0f, 0.013f), new Vector3(0.026f, 0.05f, 0.006f), SurfaceMaterial.Trunking, false);
                rocker.transform.localRotation = Quaternion.Euler(side * 4f, 0f, 0f);
            }
        }

        /// <summary>
        /// Recessed ceiling loudspeaker (public address and voice alarm) for a 600 mm ceiling tile: a round trim ring
        /// 220 mm across with a perforated grille, flush with the T-bars; it sits in the middle of a tile.
        /// </summary>
        private static void BuildCeilingSpeaker(Transform root)
        {
            float y = -(CeilingGridDepth + 0.004f);
            GrayboxUtility.CreateCylinder("Trim", root, new Vector3(0f, y, 0f), 0.11f, 0.008f, SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateCylinder("Grille", root, new Vector3(0f, y - 0.005f, 0f), 0.09f, 0.003f, SurfaceMaterial.FloorVent);
        }

        /// <summary>Clear opening of the freight lift's landing door (a 2000 kg goods lift).</summary>
        public const float FreightDoorWidth = 2.0f;

        public const float FreightDoorHeight = 2.2f;

        /// <summary>Half the width of everything the freight lift puts on the wall (frame, call station).</summary>
        public const float FreightLiftHalfWidth = 1.4f;

        /// <summary>
        /// Landing of a goods lift on a wall (a wall prop, only looks: it never opens and has one collider). A heavy steel
        /// frame with yellow and black hazard stripes on the jambs, two solid telescopic steel panels (side opening, no
        /// windows) closed in the 2.0 x 2.2 m opening, a chequer-plate sill, steel bump rails across the panels, a floor
        /// indicator over the door, a call station with a key switch beside it and the load plate. The yellow keep-clear
        /// hatching on the floor in front of it is painted by the room.
        /// </summary>
        private static void BuildFreightLift(Transform root)
        {
            const float width = FreightDoorWidth;
            const float height = FreightDoorHeight;
            const float frame = 0.14f;
            const float frameDepth = 0.06f;
            const float panel = 0.04f;
            float half = width / 2f;

            // Telescopic panels: the fast one in front, the slow one behind it, overlapping a little; the seam is off centre.
            const float fastWidth = 1.04f;
            GrayboxUtility.CreateStandingBox("Panel_Slow", root, new Vector3(-half + (width - fastWidth + 0.04f) / 2f, 0.01f, 0.012f),
                new Vector3(width - fastWidth + 0.04f, height - 0.01f, panel / 2f), SurfaceMaterial.Steel, false);
            GrayboxUtility.CreateStandingBox("Panel_Fast", root, new Vector3(half - fastWidth / 2f, 0.01f, 0.034f),
                new Vector3(fastWidth, height - 0.01f, panel / 2f), SurfaceMaterial.Steel, false);
            foreach (float y in new[] { 0.35f, 1.0f })
            {
                GrayboxUtility.CreateStandingBox($"BumpRail_{y:0.00}", root, new Vector3(0f, y, 0.06f), new Vector3(width - 0.1f, 0.08f, 0.03f), SurfaceMaterial.Metal, false);
            }
            GrayboxUtility.CreateStandingBox("Sill", root, new Vector3(0f, 0f, 0.1f), new Vector3(width + 0.1f, 0.008f, 0.2f), SurfaceMaterial.FloorVent, false);

            // Frame: two jambs and a head, the jambs striped yellow and black from the floor up.
            foreach (int side in new[] { -1, 1 })
            {
                string name = side < 0 ? "Left" : "Right";
                float x = side * (half + frame / 2f);
                GrayboxUtility.CreateStandingBox($"Jamb_{name}", root, new Vector3(x, 0f, frameDepth / 2f), new Vector3(frame, height + frame, frameDepth), SurfaceMaterial.Steel, false);
                for (int i = 0; i < 6; i++)
                {
                    var stripe = GrayboxUtility.CreateBox($"Stripe_{name}_{i}", root, new Vector3(x, 0.12f + i * 0.2f, frameDepth + 0.001f),
                        new Vector3(frame - 0.02f, 0.1f, 0.002f), i % 2 == 0 ? SurfaceMaterial.SafetyYellow : SurfaceMaterial.Upholstery, false);
                    stripe.transform.localRotation = Quaternion.Euler(0f, 0f, side * 30f);
                }
            }
            GrayboxUtility.CreateStandingBox("Head", root, new Vector3(0f, height, frameDepth / 2f), new Vector3(width + 2f * frame, frame, frameDepth), SurfaceMaterial.Steel, false);

            // Floor indicator over the door.
            GrayboxUtility.CreateBox("Indicator", root, new Vector3(0f, height + frame + 0.12f, 0.02f), new Vector3(0.36f, 0.12f, 0.04f), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateBox("Indicator_Display", root, new Vector3(0f, height + frame + 0.12f, 0.041f), new Vector3(0.28f, 0.07f, 0.002f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateBox("Indicator_Digit", root, new Vector3(0f, height + frame + 0.12f, 0.0425f), new Vector3(0.04f, 0.05f, 0.001f), SurfaceMaterial.LedRed, false);

            // Call station with up and down buttons and a key switch, the load plate above it.
            float stationX = half + frame + 0.14f;
            GrayboxUtility.CreateBox("CallStation", root, new Vector3(stationX, 1.1f, 0.0125f), new Vector3(0.12f, 0.3f, 0.025f), SurfaceMaterial.Steel, false);
            foreach (var (y, name) in new[] { (1.17f, "Up"), (1.07f, "Down") })
            {
                var button = GrayboxUtility.CreateCylinder($"Button_{name}", root, new Vector3(stationX, y, 0.028f), 0.022f, 0.008f, SurfaceMaterial.Metal);
                button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            GrayboxUtility.CreateBox("KeySwitch", root, new Vector3(stationX, 0.99f, 0.027f), new Vector3(0.03f, 0.03f, 0.006f), SurfaceMaterial.SafetyYellow, false);
            GrayboxUtility.CreateBox("LoadPlate", root, new Vector3(stationX, 1.5f, 0.002f), new Vector3(0.18f, 0.12f, 0.004f), SurfaceMaterial.SignFace, false);
            GrayboxUtility.CreateBox("LoadPlate_Border", root, new Vector3(stationX, 1.5f, 0.001f), new Vector3(0.2f, 0.14f, 0.002f), SurfaceMaterial.SafetyYellow, false);
            AddBoxCollider(root, new Vector3(0f, (height + frame) / 2f, 0.04f), new Vector3(width + 2f * frame, height + frame, 0.08f));
        }

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
        ///   (the lift is the way out), the evacuation lift sign and the firefighters' switch (EN 81-72).
        /// </summary>
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

            // Doors: a landing panel and a car panel per leaf, 5 cm wider than half the opening, so they overlap the jambs.
            float leafWidth = doorHalf + 0.05f;
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var leaf = GrayboxUtility.CreateGroup(i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3(side * leafWidth / 2f, 0f, 0f);
                GrayboxUtility.CreateStandingBox("LandingPanel", leaf, new Vector3(0f, 0f, -wall - 0.025f), new Vector3(leafWidth, doorHeight, 0.035f), SurfaceMaterial.Steel, false);
                GrayboxUtility.CreateStandingBox("CarPanel", leaf, new Vector3(0f, 0f, carFront + 0.06f), new Vector3(leafWidth, doorHeight, 0.03f), SurfaceMaterial.Steel, false);
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
                    new Vector3(jamb, doorHeight + jamb, 0.03f), SurfaceMaterial.Steel, false);
                // The wall's reveals inside the opening are lined with steel too.
                GrayboxUtility.CreateStandingBox(side < 0 ? "Reveal_Left" : "Reveal_Right", root, new Vector3(side * (doorHalf - 0.005f), 0f, -wall / 2f),
                    new Vector3(0.01f, doorHeight, wall), SurfaceMaterial.Steel, false);
            }
            GrayboxUtility.CreateStandingBox("Frame_Head", root, new Vector3(0f, doorHeight, 0.015f), new Vector3(2f * (doorHalf + jamb), jamb, 0.03f), SurfaceMaterial.Steel, false);
            GrayboxUtility.CreateStandingBox("Sill", root, new Vector3(0f, 0f, (carFront + 0.08f) / 2f + 0.02f), new Vector3(2f * doorHalf + 0.1f, 0.006f, -carFront + 0.04f), SurfaceMaterial.FloorVent, false);

            float buttonsX = -(doorHalf + jamb + 0.2f);
            GrayboxUtility.CreateBox("CallButtons", root, new Vector3(buttonsX, 1.0f, 0.008f), new Vector3(0.1f, 0.22f, 0.016f), SurfaceMaterial.Steel, false);
            foreach (var (y, name) in new[] { (1.05f, "Up"), (0.95f, "Down") })
            {
                var button = GrayboxUtility.CreateCylinder($"Button_{name}", root, new Vector3(buttonsX, y, 0.02f), 0.02f, 0.008f, SurfaceMaterial.Metal);
                button.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                var ring = GrayboxUtility.CreateCylinder($"ButtonRing_{name}", root, new Vector3(buttonsX, y, 0.0165f), 0.026f, 0.002f, SurfaceMaterial.LightPanel);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            float indicatorY = doorHeight + jamb + 0.1f;
            GrayboxUtility.CreateBox("Indicator", root, new Vector3(0f, indicatorY, 0.015f), new Vector3(0.3f, 0.1f, 0.03f), SurfaceMaterial.Steel, false);
            GrayboxUtility.CreateBox("Indicator_Display", root, new Vector3(0f, indicatorY, 0.031f), new Vector3(0.22f, 0.06f, 0.002f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateBox("Indicator_Digit", root, new Vector3(0f, indicatorY, 0.0325f), new Vector3(0.035f, 0.045f, 0.001f), SurfaceMaterial.LedRed, false);
            var exit = Place(GrayboxProp.ExitSign, root, new Vector3(0f, indicatorY + 0.2f, 0f), 0f);
            exit.name = "ExitSign";

            float signsX = doorHalf + jamb + 0.2f;
            GrayboxUtility.CreateBox("EvacuationLiftSign", root, new Vector3(signsX, 1.6f, 0.002f), new Vector3(0.2f, 0.2f, 0.004f), SurfaceMaterial.SafetyGreen, false);
            GrayboxUtility.CreateBox("EvacuationLiftSign_Pictogram", root, new Vector3(signsX, 1.6f, 0.0045f), new Vector3(0.12f, 0.14f, 0.001f), SurfaceMaterial.SignFace, false);
            GrayboxUtility.CreateBox("FirefighterSwitch", root, new Vector3(signsX, 1.95f, 0.02f), new Vector3(0.12f, 0.12f, 0.04f), SurfaceMaterial.SafetyRed, false);
            GrayboxUtility.CreateBox("FirefighterSwitch_Glass", root, new Vector3(signsX, 1.95f, 0.0405f), new Vector3(0.08f, 0.08f, 0.002f), SurfaceMaterial.Glass, false);

            // Car: the walls stop the player, so they have colliders; the fittings on them do not.
            var car = GrayboxUtility.CreateGroup("Car", root);
            float outer = carHalf + panel;
            float frontFace = carFront + panel;
            GrayboxUtility.CreateBoxBetween("Floor", car, new Vector3(-carHalf, 0f, carBack), new Vector3(carHalf, 0.008f, carFront), SurfaceMaterial.Upholstery);
            GrayboxUtility.CreateBoxBetween("Wall_Back", car, new Vector3(-outer, 0f, carBack - panel), new Vector3(outer, carHeight + roof, carBack), SurfaceMaterial.Steel, true);
            foreach (int side in new[] { -1, 1 })
            {
                string name = side < 0 ? "Left" : "Right";
                GrayboxUtility.CreateBoxBetween($"Wall_{name}", car, new Vector3(side * carHalf, 0f, carBack - panel), new Vector3(side * outer, carHeight + roof, frontFace), SurfaceMaterial.Steel, true);
                // The car's front returns beside the door.
                GrayboxUtility.CreateBoxBetween($"Return_{name}", car, new Vector3(side * doorHalf, 0f, carFront), new Vector3(side * carHalf, carHeight + roof, frontFace), SurfaceMaterial.Steel, true);
                GrayboxUtility.CreateBoxBetween($"Handrail_{name}", car, new Vector3(side * (carHalf - 0.05f), 0.88f, carBack + 0.2f), new Vector3(side * (carHalf - 0.02f), 0.92f, carFront - 0.4f), SurfaceMaterial.Metal);
                GrayboxUtility.CreateBoxBetween($"Kickplate_{name}", car, new Vector3(side * (carHalf - 0.003f), 0f, carBack), new Vector3(side * carHalf, 0.15f, carFront), SurfaceMaterial.Trim);
            }
            GrayboxUtility.CreateBoxBetween("Transom", car, new Vector3(-doorHalf, doorHeight, carFront), new Vector3(doorHalf, carHeight + roof, frontFace), SurfaceMaterial.Steel, true);
            GrayboxUtility.CreateBoxBetween("Handrail_Back", car, new Vector3(-carHalf + 0.1f, 0.88f, carBack + 0.02f), new Vector3(carHalf - 0.1f, 0.92f, carBack + 0.05f), SurfaceMaterial.Metal);
            GrayboxUtility.CreateBoxBetween("Mirror", car, new Vector3(-carHalf + 0.05f, 0.95f, carBack), new Vector3(carHalf - 0.05f, 2.1f, carBack + 0.004f), SurfaceMaterial.Glass);
            GrayboxUtility.CreateBoxBetween("Ceiling", car, new Vector3(-outer, carHeight, carBack - panel), new Vector3(outer, carHeight + roof, frontFace), SurfaceMaterial.Steel, true);
            GrayboxUtility.CreateBoxBetween("CeilingLight", car, new Vector3(-carHalf + 0.15f, carHeight - 0.006f, carBack + 0.3f), new Vector3(carHalf - 0.15f, carHeight, carFront - 0.3f), SurfaceMaterial.LightPanel);
            AddSpotLight(car, new Vector3(0f, carHeight - 0.03f, (carFront + carBack) / 2f), 160f, 120f, 3.5f, 2.2f, LedPanelColor);

            // Car operating panel on the right-hand wall as you face the door (+X), near the front.
            float copX = carHalf - 0.012f;
            float copZ = carFront - 0.35f;
            GrayboxUtility.CreateBoxBetween("OperatingPanel", car, new Vector3(copX, 0.8f, copZ - 0.13f), new Vector3(carHalf, 1.9f, copZ + 0.13f), SurfaceMaterial.Steel);
            GrayboxUtility.CreateBox("OperatingPanel_Display", car, new Vector3(copX - 0.001f, 1.75f, copZ), new Vector3(0.002f, 0.06f, 0.16f), SurfaceMaterial.Screen, false);
            for (int i = 0; i < 6; i++)
            {
                var button = GrayboxUtility.CreateCylinder($"OperatingPanel_Button_{i}", car, new Vector3(copX - 0.004f, 1.0f + (i / 2) * 0.1f, copZ + (i % 2 == 0 ? -0.04f : 0.04f)), 0.018f, 0.006f, SurfaceMaterial.Metal);
                button.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            GrayboxUtility.CreateBox("OperatingPanel_Alarm", car, new Vector3(copX - 0.004f, 0.9f, copZ), new Vector3(0.006f, 0.04f, 0.04f), SurfaceMaterial.SafetyYellow, false);
            GrayboxUtility.CreateBox("OperatingPanel_Intercom", car, new Vector3(copX - 0.002f, 1.45f, copZ), new Vector3(0.002f, 0.06f, 0.08f), SurfaceMaterial.FloorVent, false);

            ConfigureDoor(root, leaves, RoomContract.LiftDoorWidth / 2f, new Renderer[0],
                // Photo cells watch the landing in front of the door and the front of the car, not where the player starts.
                new Vector3(0f, 1f, (1.2f + carFront - LiftSensorInCar) / 2f), new Vector3(1.4f, 2f, 1.2f - carFront + LiftSensorInCar));
            var serialized = new SerializedObject(root.GetComponent<AutoSlidingDoor>());
            serialized.FindProperty("openSeconds").floatValue = 1.4f;
            serialized.FindProperty("closeSeconds").floatValue = 1.8f;
            serialized.FindProperty("holdOpenSeconds").floatValue = 2.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Stackable plastic storage crate 0.4 x 0.25 x 0.3 m with spare parts in it; stands on a shelf, the label faces +Z.</summary>
        private static void BuildStorageCrate(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Body", root, Vector3.zero, new Vector3(0.4f, 0.25f, 0.3f), SurfaceMaterial.DataCable, false);
            GrayboxUtility.CreateStandingBox("Rim", root, new Vector3(0f, 0.23f, 0f), new Vector3(0.42f, 0.02f, 0.32f), SurfaceMaterial.DataCable, false);
            GrayboxUtility.CreateStandingBox("Parts", root, new Vector3(0f, 0.2f, 0f), new Vector3(0.34f, 0.04f, 0.24f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Label", root, new Vector3(0f, 0.12f, 0.151f), new Vector3(0.12f, 0.06f, 0.002f), SurfaceMaterial.SignFace, false);
        }
    }
}
