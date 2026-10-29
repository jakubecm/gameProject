using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Editor
{
    /// <summary>The graybox props rooms are dressed with. Each one is a prefab in <c>Prefabs/Props</c>.</summary>
    public enum GrayboxProp
    {
        DoorFrame,
        Desk,
        OfficeChair,
        Workstation,
        MeetingTable,
        Shelf,
        FilingCabinet,
        Whiteboard,
        LedPanel600,
        LedPanel1200,
        FloorVentTile,
        FireExtinguisher,
        ElectricalPanel,
        HydrantCabinet,
        SmokeDetector,
        Sprinkler,
        GasNozzle,
        PowerSocket,
        DataSocket,
        Radiator,
        PendantLight,
        HallSignCube,
        SecurityDome,
        SecurityWallDome,
        SecurityPendant,
        VideoScreen,
        EquipmentCabinet,
        FloorBox,
        ExitSign,
        AutoSlidingDoor,
        AisleDoor,
        WallVentGrille,
        CeilingDiffuser,
        FireCallPoint,
        GasReleaseStation,
        EmergencyLight,
        LedBatten,
        AisleLight,
        RackCompute,
        RackGpu,
        RackJbod,
        RackNetwork,
        PcTower,
        DualWorkstation,
        WallTv,
        CoffeeCounter,
        CoffeeMachine,
        Mug,
        Locker,
        LockerBench,
        Binders,
        PaperStack,
        PhotoFrame,
        Transformer,
        SwitchgearCabinet,
        AnalogMeterPanel,
        PipeGauge,
        AirHandlingUnit,
        CirculationPump,
        FloorVentGrille,
        Printer,
        Fridge,
        CoffeeTable,
        BufferTank,
        LightSwitch,
        CeilingSpeaker,
        FreightLift,
        StorageCrate,
        PlateHeatExchanger,
        PassengerLift,
    }

    /// <summary>
    /// Creates the graybox props as ProBuilder prefabs with real-world dimensions. Rooms hold instances of
    /// these prefabs, so replacing a graybox prop with a final model (task 4.7) is one prefab edit.
    /// Existing prop prefabs are never replaced, except by Rebuild Graybox Dressing.
    ///
    /// Pivot conventions (local +Z is "forward"):
    /// - floor props: pivot on the floor, centered under the prop; forward is the side people use
    ///   (desk and cabinet fronts, the way a seated person faces, the open side of a shelf);
    /// - wall props: pivot on the floor at the wall surface; forward points away from the wall;
    /// - small wall fittings (sockets): pivot at the center of the back plate; forward away from the wall;
    /// - ceiling props: pivot on the ceiling surface, hanging down; the long side runs along Z;
    /// - desk-top props: pivot on the desk surface; forward is the way the screen faces;
    /// - door frame: pivot is the opening pose (<see cref="RoomContract.GetOpeningPose"/>).
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        public const string PropsFolder = "Assets/_Project/Prefabs/Props";

        public const float DeskHeight = 0.75f;

        /// <summary>
        /// Cross-section of every electrical trunking run (width across the wall, depth off the wall), so the
        /// trunking leaving a distribution panel and the trunking along walls are the same product.
        /// </summary>
        public const float TrunkingWidth = 0.12f;
        public const float TrunkingDepth = 0.06f;

        /// <summary>Height of the suspended-ceiling T-bars below the ceiling plane; ceiling fittings cover it.</summary>
        public const float CeilingGridDepth = 0.012f;

        // LED panels and battens: 4000 K, no shadows (task 1.10 allows shadows on one or two lights only).
        private static readonly Color LedPanelColor = new Color(1f, 0.95f, 0.88f);

        private static readonly Dictionary<GrayboxProp, Action<Transform>> Builders = new Dictionary<GrayboxProp, Action<Transform>>
        {
            { GrayboxProp.DoorFrame, BuildDoorFrame },
            { GrayboxProp.Desk, BuildDesk },
            { GrayboxProp.OfficeChair, BuildOfficeChair },
            { GrayboxProp.Workstation, BuildWorkstation },
            { GrayboxProp.MeetingTable, BuildMeetingTable },
            { GrayboxProp.Shelf, BuildShelf },
            { GrayboxProp.FilingCabinet, BuildFilingCabinet },
            { GrayboxProp.Whiteboard, BuildWhiteboard },
            { GrayboxProp.LedPanel600, root => BuildLedPanel(root, 0.6f, 3.0f) },
            { GrayboxProp.LedPanel1200, root => BuildLedPanel(root, 1.2f, 5.0f) },
            { GrayboxProp.FloorVentTile, BuildFloorVentTile },
            { GrayboxProp.FireExtinguisher, BuildFireExtinguisher },
            { GrayboxProp.ElectricalPanel, root => BuildElectricalPanel(root, RoomContract.CeilingHeight) },
            { GrayboxProp.HydrantCabinet, BuildHydrantCabinet },
            { GrayboxProp.SmokeDetector, BuildSmokeDetector },
            { GrayboxProp.Sprinkler, BuildSprinkler },
            { GrayboxProp.GasNozzle, BuildGasNozzle },
            { GrayboxProp.PowerSocket, BuildPowerSocket },
            { GrayboxProp.DataSocket, BuildDataSocket },
            { GrayboxProp.Radiator, BuildRadiator },
            { GrayboxProp.PendantLight, BuildPendantLight },
            { GrayboxProp.HallSignCube, root => BuildHallSign(root, SurfaceMaterial.SignFace) },
            { GrayboxProp.SecurityDome, BuildSecurityDome },
            { GrayboxProp.SecurityWallDome, BuildSecurityWallDome },
            { GrayboxProp.SecurityPendant, BuildSecurityPendant },
            { GrayboxProp.VideoScreen, BuildVideoScreen },
            { GrayboxProp.EquipmentCabinet, BuildEquipmentCabinet },
            { GrayboxProp.FloorBox, BuildFloorBox },
            { GrayboxProp.ExitSign, BuildExitSign },
            { GrayboxProp.AutoSlidingDoor, BuildRoomSlidingDoor },
            { GrayboxProp.AisleDoor, BuildAisleSlidingDoor },
            { GrayboxProp.WallVentGrille, BuildWallVentGrille },
            { GrayboxProp.CeilingDiffuser, BuildCeilingDiffuser },
            { GrayboxProp.FireCallPoint, BuildFireCallPoint },
            { GrayboxProp.GasReleaseStation, BuildGasReleaseStation },
            { GrayboxProp.EmergencyLight, BuildEmergencyLight },
            { GrayboxProp.LedBatten, BuildLedBatten },
            { GrayboxProp.AisleLight, BuildAisleLight },
            { GrayboxProp.RackCompute, root => BuildRack(root, RackKind.Compute) },
            { GrayboxProp.RackGpu, root => BuildRack(root, RackKind.Gpu) },
            { GrayboxProp.RackJbod, root => BuildRack(root, RackKind.Jbod) },
            { GrayboxProp.RackNetwork, root => BuildRack(root, RackKind.Network) },
            { GrayboxProp.PcTower, BuildPcTower },
            { GrayboxProp.DualWorkstation, BuildDualWorkstation },
            { GrayboxProp.WallTv, BuildWallTv },
            { GrayboxProp.CoffeeCounter, BuildCoffeeCounter },
            { GrayboxProp.CoffeeMachine, BuildCoffeeMachine },
            { GrayboxProp.Mug, BuildMug },
            { GrayboxProp.Locker, BuildLocker },
            { GrayboxProp.LockerBench, BuildLockerBench },
            { GrayboxProp.Binders, BuildBinders },
            { GrayboxProp.PaperStack, BuildPaperStack },
            { GrayboxProp.PhotoFrame, BuildPhotoFrame },
            { GrayboxProp.Transformer, BuildTransformer },
            { GrayboxProp.SwitchgearCabinet, BuildSwitchgearCabinet },
            { GrayboxProp.AnalogMeterPanel, BuildAnalogMeterPanel },
            { GrayboxProp.PipeGauge, BuildPipeGauge },
            { GrayboxProp.AirHandlingUnit, BuildAirHandlingUnit },
            { GrayboxProp.CirculationPump, BuildCirculationPump },
            { GrayboxProp.FloorVentGrille, BuildFloorVentGrille },
            { GrayboxProp.Printer, BuildPrinter },
            { GrayboxProp.Fridge, BuildFridge },
            { GrayboxProp.CoffeeTable, BuildCoffeeTable },
            { GrayboxProp.BufferTank, BuildBufferTank },
            { GrayboxProp.LightSwitch, BuildLightSwitch },
            { GrayboxProp.CeilingSpeaker, BuildCeilingSpeaker },
            { GrayboxProp.FreightLift, BuildFreightLift },
            { GrayboxProp.StorageCrate, BuildStorageCrate },
            { GrayboxProp.PlateHeatExchanger, BuildPlateHeatExchanger },
            { GrayboxProp.PassengerLift, BuildPassengerLift },
        };

        public static string PathOf(GrayboxProp prop) => $"{PropsFolder}/{prop}.prefab";

        [MenuItem("Tools/Cold Shutdown/Create Graybox Props")]
        public static void CreateGrayboxProps() => BuildProps(false);

        /// <summary>Builds every prop prefab; <paramref name="replace"/> rebuilds existing ones in place (same GUID).</summary>
        internal static void BuildProps(bool replace)
        {
            foreach (GrayboxProp prop in Enum.GetValues(typeof(GrayboxProp)))
            {
                string path = PathOf(prop);
                if (!replace && RoomShellTool.SkipExisting(path))
                {
                    continue;
                }

                var root = new GameObject(prop.ToString());
                Builders[prop](root.transform);
                RoomShellTool.SaveAsPrefab(root, path);
                Debug.Log($"Prop saved to {path}.");
            }
        }

        /// <summary>Places an instance of a prop prefab (created first if missing) under <paramref name="parent"/>.</summary>
        internal static Transform Place(GrayboxProp prop, Transform parent, Vector3 localPosition, float yawDegrees = 0f, string name = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(prop));
            if (prefab == null)
            {
                CreateGrayboxProps();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(prop));
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name ?? prop.ToString();
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            return instance.transform;
        }

        /// <summary>One box collider over the whole prop, instead of a mesh collider on every small part.</summary>
        internal static void AddBoxCollider(Transform root, Vector3 center, Vector3 size)
        {
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        /// <summary>
        /// Casing on the room side of the wall around an opening. It frames the opening without entering
        /// it, so the opening stays 2.4 x 2.6 m and a wall plug inside it reads as a closed door.
        /// </summary>
        private static void BuildDoorFrame(Transform root)
        {
            const float width = 0.1f;
            const float depth = 0.04f;
            float halfOpening = RoomContract.OpeningWidth / 2f;
            float openingHeight = RoomContract.OpeningHeight;
            float z = RoomContract.WallThickness / 2f + depth / 2f;

            var jambSize = new Vector3(width, openingHeight + width, depth);
            GrayboxUtility.CreateStandingBox("Jamb_Left", root, new Vector3(-(halfOpening + width / 2f), 0f, z), jambSize, SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("Jamb_Right", root, new Vector3(halfOpening + width / 2f, 0f, z), jambSize, SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("Head", root, new Vector3(0f, openingHeight, z), new Vector3(halfOpening * 2f, width, depth), SurfaceMaterial.Trim, false);
        }

        /// <summary>Office desk 1.6 x 0.8 m: top, two side panels and a modesty panel at the back (-Z).</summary>
        private static void BuildDesk(Transform root)
        {
            const float width = 1.6f;
            const float depth = 0.8f;
            const float top = 0.025f;
            const float panel = 0.025f;
            float legHeight = DeskHeight - top;

            GrayboxUtility.CreateStandingBox("Top", root, new Vector3(0f, legHeight, 0f), new Vector3(width, top, depth), SurfaceMaterial.Furniture, false);
            var sideSize = new Vector3(panel, legHeight, depth - 0.05f);
            GrayboxUtility.CreateStandingBox("Side_Left", root, new Vector3(-(width - panel) / 2f, 0f, 0f), sideSize, SurfaceMaterial.Furniture, false);
            GrayboxUtility.CreateStandingBox("Side_Right", root, new Vector3((width - panel) / 2f, 0f, 0f), sideSize, SurfaceMaterial.Furniture, false);
            GrayboxUtility.CreateStandingBox("ModestyPanel", root, new Vector3(0f, 0.35f, -depth / 2f + 0.1f), new Vector3(width - 2f * panel, 0.35f, 0.018f), SurfaceMaterial.Furniture, false);
            AddBoxCollider(root, new Vector3(0f, DeskHeight / 2f, 0f), new Vector3(width, DeskHeight, depth));
        }

        /// <summary>
        /// Swivel chair on a five-star base with casters; a seated person faces +Z. The backrest is carried
        /// by a bracket from under the seat, so nothing floats.
        /// </summary>
        private static void BuildOfficeChair(Transform root)
        {
            const float legLength = 0.3f;
            for (int i = 0; i < 5; i++)
            {
                var leg = GrayboxUtility.CreateGroup($"Leg_{i}", root);
                leg.localRotation = Quaternion.Euler(0f, i * 72f, 0f);
                GrayboxUtility.CreateStandingBox("Arm", leg, new Vector3(0f, 0.05f, legLength / 2f), new Vector3(0.045f, 0.03f, legLength), SurfaceMaterial.Upholstery, false);
                GrayboxUtility.CreateCylinder("Caster", leg, new Vector3(0f, 0.025f, legLength - 0.02f), 0.025f, 0.05f, SurfaceMaterial.Upholstery);
            }
            GrayboxUtility.CreateCylinder("Hub", root, new Vector3(0f, 0.065f, 0f), 0.05f, 0.05f, SurfaceMaterial.Upholstery);
            GrayboxUtility.CreateCylinder("Column", root, new Vector3(0f, 0.24f, 0f), 0.025f, 0.3f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateStandingBox("SeatPlate", root, new Vector3(0f, 0.39f, 0f), new Vector3(0.24f, 0.03f, 0.24f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Seat", root, new Vector3(0f, 0.42f, 0.02f), new Vector3(0.48f, 0.08f, 0.46f), SurfaceMaterial.Upholstery, false);

            // Backrest bracket: runs back under the seat, then up behind the backrest.
            GrayboxUtility.CreateStandingBox("Bracket_Under", root, new Vector3(0f, 0.38f, -0.155f), new Vector3(0.05f, 0.03f, 0.25f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Bracket_Up", root, new Vector3(0f, 0.38f, -0.265f), new Vector3(0.05f, 0.45f, 0.03f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Backrest", root, new Vector3(0f, 0.56f, -0.22f), new Vector3(0.46f, 0.5f, 0.06f), SurfaceMaterial.Upholstery, false);
            AddBoxCollider(root, new Vector3(0f, 0.53f, 0f), new Vector3(0.6f, 1.06f, 0.6f));
        }

        /// <summary>
        /// Monitor with a keyboard in front of it and the mouse to the right of the keyboard (as seen by the user, who
        /// faces the screen); stands on a desk top, the screen faces +Z.
        /// </summary>
        private static void BuildWorkstation(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Foot", root, new Vector3(0f, 0f, -0.12f), new Vector3(0.22f, 0.015f, 0.18f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Neck", root, new Vector3(0f, 0.015f, -0.15f), new Vector3(0.04f, 0.14f, 0.025f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Screen", root, new Vector3(0f, 0.11f, -0.13f), new Vector3(0.62f, 0.37f, 0.025f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateStandingBox("Keyboard", root, new Vector3(0f, 0f, 0.16f), new Vector3(0.44f, 0.02f, 0.14f), SurfaceMaterial.Screen, false);
            BuildMouse(root, new Vector3(-0.3f, 0f, 0.17f));
        }

        /// <summary>A mouse on its pad; the user faces -Z, so their right hand is at -X.</summary>
        private static void BuildMouse(Transform root, Vector3 position)
        {
            GrayboxUtility.CreateStandingBox("MousePad", root, position, new Vector3(0.2f, 0.003f, 0.17f), SurfaceMaterial.Upholstery, false);
            GrayboxUtility.CreateStandingBox("Mouse", root, position + new Vector3(0f, 0.003f, 0f), new Vector3(0.06f, 0.035f, 0.1f), SurfaceMaterial.Screen, false);
        }

        /// <summary>Meeting table 1.8 x 0.9 m on two pedestal legs; the long side runs along X.</summary>
        private static void BuildMeetingTable(Transform root)
        {
            const float width = 1.8f;
            const float depth = 0.9f;
            const float top = 0.03f;

            GrayboxUtility.CreateStandingBox("Top", root, new Vector3(0f, DeskHeight - top, 0f), new Vector3(width, top, depth), SurfaceMaterial.Furniture, false);
            foreach (int side in new[] { -1, 1 })
            {
                string suffix = side < 0 ? "Left" : "Right";
                GrayboxUtility.CreateStandingBox($"Leg_{suffix}", root, new Vector3(side * 0.6f, 0.02f, 0f), new Vector3(0.08f, DeskHeight - top - 0.02f, 0.5f), SurfaceMaterial.Metal, false);
                GrayboxUtility.CreateStandingBox($"Foot_{suffix}", root, new Vector3(side * 0.6f, 0f, 0f), new Vector3(0.1f, 0.02f, 0.7f), SurfaceMaterial.Metal, false);
            }
            AddBoxCollider(root, new Vector3(0f, DeskHeight / 2f, 0f), new Vector3(width, DeskHeight, depth));
        }

        /// <summary>Open steel shelving 1.0 x 2.0 x 0.4 m with a few archive boxes; the open side faces +Z.</summary>
        private static void BuildShelf(Transform root)
        {
            const float width = 1.0f;
            const float height = 2.0f;
            const float depth = 0.4f;
            const float post = 0.03f;

            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                {
                    var position = new Vector3(x * (width - post) / 2f, 0f, z * (depth - post) / 2f);
                    GrayboxUtility.CreateStandingBox($"Post_{(z > 0 ? "F" : "B")}{(x > 0 ? "R" : "L")}", root, position, new Vector3(post, height, post), SurfaceMaterial.Metal, false);
                }
            }

            float[] levels = { 0.1f, 0.55f, 1.0f, 1.45f, 1.9f };
            for (int i = 0; i < levels.Length; i++)
            {
                GrayboxUtility.CreateStandingBox($"Board_{i}", root, new Vector3(0f, levels[i], 0f), new Vector3(width, 0.02f, depth), SurfaceMaterial.Metal, false);
            }

            var boxSize = new Vector3(0.3f, 0.28f, 0.34f);
            GrayboxUtility.CreateStandingBox("ArchiveBox_0", root, new Vector3(-0.3f, 0.57f, 0f), boxSize, SurfaceMaterial.Furniture, false);
            GrayboxUtility.CreateStandingBox("ArchiveBox_1", root, new Vector3(0.05f, 0.57f, 0f), boxSize, SurfaceMaterial.Furniture, false);
            GrayboxUtility.CreateStandingBox("ArchiveBox_2", root, new Vector3(0.25f, 1.47f, 0f), boxSize, SurfaceMaterial.Furniture, false);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));
        }

        /// <summary>Four-drawer filing cabinet 0.45 x 1.32 x 0.6 m; the drawers face +Z.</summary>
        private static void BuildFilingCabinet(Transform root)
        {
            const float width = 0.45f;
            const float height = 1.32f;
            const float depth = 0.6f;

            GrayboxUtility.CreateStandingBox("Body", root, Vector3.zero, new Vector3(width, height, depth), SurfaceMaterial.Furniture, false);
            for (int i = 0; i < 4; i++)
            {
                float y = 0.03f + i * 0.32f;
                GrayboxUtility.CreateStandingBox($"Drawer_{i}", root, new Vector3(0f, y, depth / 2f + 0.005f), new Vector3(width - 0.04f, 0.3f, 0.01f), SurfaceMaterial.Furniture, false);
                GrayboxUtility.CreateStandingBox($"Handle_{i}", root, new Vector3(0f, y + 0.22f, depth / 2f + 0.02f), new Vector3(0.12f, 0.02f, 0.02f), SurfaceMaterial.Metal, false);
            }
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.01f), new Vector3(width, height, depth + 0.02f));
        }

        /// <summary>
        /// Wall-mounted whiteboard 1.5 x 1.0 m with a marker tray. Its center is at <see cref="WallScreenCenter"/>, the
        /// same height as the wall TV's screen, so the two line up in a meeting room.
        /// </summary>
        private static void BuildWhiteboard(Transform root)
        {
            float bottom = WallScreenCenter - 0.5f;
            GrayboxUtility.CreateStandingBox("Frame", root, new Vector3(0f, bottom - 0.02f, 0.015f), new Vector3(1.54f, 1.04f, 0.03f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("Board", root, new Vector3(0f, bottom, 0.031f), new Vector3(1.5f, 1.0f, 0.002f), SurfaceMaterial.Whiteboard, false);
            GrayboxUtility.CreateStandingBox("Tray", root, new Vector3(0f, bottom - 0.04f, 0.05f), new Vector3(0.6f, 0.02f, 0.06f), SurfaceMaterial.Metal, false);
        }

        /// <summary>Height of the middle of a wall TV's screen and of a whiteboard.</summary>
        public const float WallScreenCenter = 1.6f;

        /// <summary>
        /// Lay-in LED panel for the 600 mm ceiling grid: 0.6 m wide, <paramref name="length"/> long (along Z), a slim
        /// aluminium frame around an opal diffuser flush with the T-bars. It sits in one grid cell (600) or two (1200)
        /// and covers the T-bars under it. The light is a wide downward spot with no shadows.
        /// </summary>
        private static void BuildLedPanel(Transform root, float length, float intensity)
        {
            const float width = 0.6f;
            const float rim = 0.015f;
            float frameY = -(CeilingGridDepth + 0.004f);
            GrayboxUtility.CreateBox("Frame", root, new Vector3(0f, frameY, 0f), new Vector3(width, 0.008f, length), SurfaceMaterial.CeilingGrid, false);
            GrayboxUtility.CreateBox("Diffuser", root, new Vector3(0f, frameY - 0.0055f, 0f), new Vector3(width - 2f * rim, 0.003f, length - 2f * rim), SurfaceMaterial.LightPanel, false);
            AddSpotLight(root, new Vector3(0f, frameY - 0.03f, 0f), 150f, 100f, 6.5f, intensity, LedPanelColor);
        }

        /// <summary>A downward spot light without shadows (task 1.10 allows shadows on one or two lights only).</summary>
        internal static Light AddSpotLight(Transform root, Vector3 position, float angle, float innerAngle, float range, float intensity, Color color)
        {
            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = position;
            lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = angle;
            light.innerSpotAngle = innerAngle;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            return light;
        }

        /// <summary>
        /// Perforated raised-floor tile 0.6 x 0.6 m (cold aisle air supply), lying on the floor: a steel grating of
        /// 100 mm meshes in a frame over the dark plenum, so the cold aisle floor reads as a grid.
        /// </summary>
        private static void BuildFloorVentTile(Transform root)
        {
            const float size = 0.6f;
            const float height = 0.01f;
            const float frame = 0.025f;
            const float bar = 0.012f;
            const float mesh = 0.1f;
            GrayboxUtility.CreateStandingBox("Plenum", root, Vector3.zero, new Vector3(size - 0.01f, 0.002f, size - 0.01f), SurfaceMaterial.Upholstery, false);
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateStandingBox(side < 0 ? "Frame_S" : "Frame_N", root, new Vector3(0f, 0f, side * (size - frame) / 2f), new Vector3(size, height, frame), SurfaceMaterial.FloorVent, false);
                GrayboxUtility.CreateStandingBox(side < 0 ? "Frame_W" : "Frame_E", root, new Vector3(side * (size - frame) / 2f, 0f, 0f), new Vector3(frame, height, size - 2f * frame), SurfaceMaterial.FloorVent, false);
            }
            float inner = size - 2f * frame;
            for (float at = -size / 2f + mesh; at < size / 2f - 0.01f; at += mesh)
            {
                GrayboxUtility.CreateStandingBox($"Bar_X_{at:0.0}", root, new Vector3(0f, 0f, at), new Vector3(inner, height - 0.002f, bar), SurfaceMaterial.FloorVent, false);
                GrayboxUtility.CreateStandingBox($"Bar_Z_{at:0.0}", root, new Vector3(at, 0f, 0f), new Vector3(bar, height - 0.002f, inner), SurfaceMaterial.FloorVent, false);
            }
        }

        /// <summary>Wall-mounted 6 kg fire extinguisher, top at about 1.1 m.</summary>
        private static void BuildFireExtinguisher(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Bracket", root, new Vector3(0f, 0.75f, 0.01f), new Vector3(0.08f, 0.2f, 0.02f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateCylinder("Body", root, new Vector3(0f, 0.8f, 0.1f), 0.08f, 0.5f, SurfaceMaterial.SafetyRed);
            GrayboxUtility.CreateStandingBox("Valve", root, new Vector3(0f, 1.05f, 0.1f), new Vector3(0.04f, 0.08f, 0.1f), SurfaceMaterial.Metal, false);
        }

        /// <summary>
        /// Wall-mounted distribution board 0.6 x 0.8 x 0.22 m. Its cables leave upward in trunking of the
        /// standard profile and disappear into the suspended ceiling, like every other cable run.
        /// <paramref name="ceiling"/> is where the trunking ends: the room ceiling, or the roof of the hall.
        /// </summary>
        private static void BuildElectricalPanel(Transform root, float ceiling)
        {
            const float depth = 0.22f;
            const float bottom = 1.0f;
            const float height = 0.8f;

            GrayboxUtility.CreateStandingBox("Cabinet", root, new Vector3(0f, bottom, depth / 2f), new Vector3(0.6f, height, depth), SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("Door", root, new Vector3(0f, bottom + 0.02f, depth + 0.005f), new Vector3(0.56f, height - 0.04f, 0.01f), SurfaceMaterial.Trim, false);
            GrayboxUtility.CreateStandingBox("Handle", root, new Vector3(0.24f, bottom + 0.34f, depth + 0.02f), new Vector3(0.02f, 0.12f, 0.02f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateBoxBetween("Trunking", root, new Vector3(-TrunkingWidth / 2f, bottom + height, 0f),
                new Vector3(TrunkingWidth / 2f, ceiling, TrunkingDepth), SurfaceMaterial.Trunking);
            AddBoxCollider(root, new Vector3(0f, bottom + height / 2f, depth / 2f), new Vector3(0.6f, height, depth));
        }

        /// <summary>Wall fire-hose cabinet (hydrant D25) 0.65 x 0.65 x 0.24 m; the water pipe enters its side.</summary>
        private static void BuildHydrantCabinet(Transform root)
        {
            const float size = 0.65f;
            const float depth = 0.24f;
            const float bottom = 0.9f;

            GrayboxUtility.CreateStandingBox("Body", root, new Vector3(0f, bottom, depth / 2f), new Vector3(size, size, depth), SurfaceMaterial.SafetyRed, false);
            GrayboxUtility.CreateStandingBox("Window", root, new Vector3(0f, bottom + 0.1f, depth + 0.003f), new Vector3(size - 0.16f, size - 0.2f, 0.006f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateStandingBox("Handle", root, new Vector3(size / 2f - 0.05f, bottom + 0.28f, depth + 0.012f), new Vector3(0.02f, 0.1f, 0.02f), SurfaceMaterial.Metal, false);
            AddBoxCollider(root, new Vector3(0f, bottom + size / 2f, depth / 2f), new Vector3(size, size, depth));
        }

        /// <summary>Optical smoke detector on its base, 110 mm across.</summary>
        private static void BuildSmokeDetector(Transform root)
        {
            GrayboxUtility.CreateCylinder("Base", root, new Vector3(0f, -0.006f, 0f), 0.055f, 0.012f, SurfaceMaterial.Trunking);
            GrayboxUtility.CreateCylinder("Head", root, new Vector3(0f, -0.03f, 0f), 0.05f, 0.036f, SurfaceMaterial.Trunking);
            GrayboxUtility.CreateBox("Led", root, new Vector3(0.04f, -0.035f, 0f), new Vector3(0.006f, 0.006f, 0.006f), SurfaceMaterial.SafetyRed, false);
        }

        /// <summary>Pendent sprinkler head with escutcheon; it is fed by the fire-water pipes above the ceiling.</summary>
        private static void BuildSprinkler(Transform root)
        {
            GrayboxUtility.CreateCylinder("Escutcheon", root, new Vector3(0f, -0.003f, 0f), 0.035f, 0.006f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Body", root, new Vector3(0f, -0.025f, 0f), 0.009f, 0.04f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateBox("Bulb", root, new Vector3(0f, -0.035f, 0f), new Vector3(0.005f, 0.018f, 0.005f), SurfaceMaterial.SafetyRed, false);
            GrayboxUtility.CreateCylinder("Deflector", root, new Vector3(0f, -0.047f, 0f), 0.018f, 0.002f, SurfaceMaterial.Metal);
        }

        /// <summary>360° nozzle of the server room's inert-gas extinguishing system (no water near the racks).</summary>
        private static void BuildGasNozzle(Transform root)
        {
            GrayboxUtility.CreateCylinder("Escutcheon", root, new Vector3(0f, -0.003f, 0f), 0.04f, 0.006f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Nozzle", root, new Vector3(0f, -0.04f, 0f), 0.02f, 0.07f, SurfaceMaterial.Metal);
        }

        /// <summary>Double 230 V socket outlet (type E), 150 x 80 mm plate.</summary>
        private static void BuildPowerSocket(Transform root)
        {
            GrayboxUtility.CreateBox("Plate", root, new Vector3(0f, 0f, 0.006f), new Vector3(0.15f, 0.08f, 0.012f), SurfaceMaterial.Trunking, false);
            foreach (int side in new[] { -1, 1 })
            {
                var outlet = GrayboxUtility.CreateCylinder(side < 0 ? "Outlet_Left" : "Outlet_Right", root, new Vector3(side * 0.036f, 0f, 0.0135f), 0.022f, 0.003f, SurfaceMaterial.Trunking);
                outlet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                GrayboxUtility.CreateBox(side < 0 ? "Pin_Left" : "Pin_Right", root, new Vector3(side * 0.036f, 0.012f, 0.017f), new Vector3(0.005f, 0.005f, 0.006f), SurfaceMaterial.Metal, false);
            }
        }

        /// <summary>Double RJ45 data outlet, 80 x 80 mm plate.</summary>
        private static void BuildDataSocket(Transform root)
        {
            GrayboxUtility.CreateBox("Plate", root, new Vector3(0f, 0f, 0.006f), new Vector3(0.08f, 0.08f, 0.012f), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateBox("Port_Left", root, new Vector3(-0.017f, 0f, 0.0125f), new Vector3(0.016f, 0.014f, 0.002f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateBox("Port_Right", root, new Vector3(0.017f, 0f, 0.0125f), new Vector3(0.016f, 0.014f, 0.002f), SurfaceMaterial.Screen, false);
            GrayboxUtility.CreateBox("Label", root, new Vector3(0f, 0.027f, 0.0125f), new Vector3(0.05f, 0.01f, 0.002f), SurfaceMaterial.DataCable, false);
        }

        /// <summary>Panel radiator 1.0 x 0.6 m on the wall; its flow and return pipes go down into the floor.</summary>
        private static void BuildRadiator(Transform root)
        {
            const float width = 1.0f;
            const float bottom = 0.15f;
            GrayboxUtility.CreateStandingBox("Panel", root, new Vector3(0f, bottom, 0.07f), new Vector3(width, 0.6f, 0.1f), SurfaceMaterial.Trunking, false);
            foreach (int side in new[] { -1, 1 })
            {
                string suffix = side < 0 ? "Left" : "Right";
                var x = side * (width / 2f - 0.05f);
                GrayboxUtility.CreatePipe($"Pipe_{suffix}", root, new Vector3(x, 0f, 0.07f), new Vector3(x, bottom, 0.07f), 0.0075f, SurfaceMaterial.Metal);
                GrayboxUtility.CreateCylinder($"Valve_{suffix}", root, new Vector3(x, bottom - 0.03f, 0.07f), 0.015f, 0.04f, SurfaceMaterial.Trunking);
            }
            AddBoxCollider(root, new Vector3(0f, 0.45f, 0.07f), new Vector3(width, 0.6f, 0.1f));
        }
    }
}
