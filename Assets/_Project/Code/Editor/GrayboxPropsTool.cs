using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

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
        LedBatten,
        AisleLight,
        Rack,
        PcTower,
        DualWorkstation,
        WallTv,
        CoffeeCounter,
        CoffeeMachine,
        Mug,
        Locker,
        LockerBench,
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
        BufferTank,
        LightSwitch,
        CeilingSpeaker,
        FreightLift,
        BoxLidCube,
        BoxLidFlat,
        BoxCartonCube,
        BoxCartonFlat,
        PlateHeatExchanger,
        PassengerLift,
        CoolingMeterPanel,
        ExpansionVessel,
        PlateHeatExchangerMirrored,
        AirHandlerUnit,
    }

    /// <summary>
    /// Creates the graybox props as ProBuilder prefabs with real-world dimensions. Rooms hold instances of
    /// these prefabs, so replacing a graybox prop with a final model (task 4.7) is one prefab edit.
    /// Existing prop prefabs are never replaced, except by Create Preview Scene (in place, same GUIDs).
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
            { GrayboxProp.LedBatten, BuildLedBatten },
            { GrayboxProp.AisleLight, BuildAisleLight },
            { GrayboxProp.Rack, BuildRack },
            { GrayboxProp.PcTower, BuildPcTower },
            { GrayboxProp.DualWorkstation, BuildDualWorkstation },
            { GrayboxProp.WallTv, BuildWallTv },
            { GrayboxProp.CoffeeCounter, BuildCoffeeCounter },
            { GrayboxProp.CoffeeMachine, BuildCoffeeMachine },
            { GrayboxProp.Mug, BuildMug },
            { GrayboxProp.Locker, BuildLocker },
            { GrayboxProp.LockerBench, BuildLockerBench },
            { GrayboxProp.PaperStack, BuildPaperStack },
            { GrayboxProp.PhotoFrame, BuildPhotoFrame },
            { GrayboxProp.Transformer, BuildTransformer },
            { GrayboxProp.SwitchgearCabinet, BuildSwitchgearCabinet },
            { GrayboxProp.AnalogMeterPanel, BuildAnalogMeterPanel },
            { GrayboxProp.PipeGauge, BuildPipeGauge },
            { GrayboxProp.CoolingMeterPanel, BuildCoolingMeterPanel },
            { GrayboxProp.ExpansionVessel, BuildExpansionVessel },
            { GrayboxProp.PlateHeatExchangerMirrored, BuildPlateHeatExchangerMirrored },
            { GrayboxProp.AirHandlerUnit, BuildAirHandlerUnit },
            { GrayboxProp.AirHandlingUnit, BuildAirHandlingUnit },
            { GrayboxProp.CirculationPump, BuildCirculationPump },
            { GrayboxProp.FloorVentGrille, BuildFloorVentGrille },
            { GrayboxProp.Printer, BuildPrinter },
            { GrayboxProp.Fridge, BuildFridge },
            { GrayboxProp.BufferTank, BuildBufferTank },
            { GrayboxProp.LightSwitch, BuildLightSwitch },
            { GrayboxProp.CeilingSpeaker, BuildCeilingSpeaker },
            { GrayboxProp.FreightLift, BuildFreightLift },
            { GrayboxProp.BoxLidCube, root => BuildLidBox(root, LidBoxSize, LidCubeHeight) },
            { GrayboxProp.BoxLidFlat, root => BuildLidBox(root, LidBoxSize, LidFlatHeight) },
            { GrayboxProp.BoxCartonCube, root => BuildCartonBox(root, CartonBoxSize, CartonCubeHeight) },
            { GrayboxProp.BoxCartonFlat, root => BuildCartonBox(root, CartonBoxSize, CartonFlatHeight) },
            { GrayboxProp.PlateHeatExchanger, BuildPlateHeatExchanger },
            { GrayboxProp.PassengerLift, BuildPassengerLift },
        };

        public static string PathOf(GrayboxProp prop) => $"{PropsFolder}/{prop}.prefab";

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

        /// <summary>
        /// Deletes the prefabs in <see cref="PropsFolder"/> that no <see cref="GrayboxProp"/> builds any more (removed props such as the
        /// emergency light or the old rack variants), so a rebuild leaves only what the tools make.
        /// </summary>
        internal static void DeleteObsoleteProps()
        {
            var current = new HashSet<string>();
            foreach (GrayboxProp prop in Enum.GetValues(typeof(GrayboxProp))) current.Add(PathOf(prop));
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PropsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (current.Contains(path) || System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') != PropsFolder) continue;
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Deleted {path}: no prop is built from it any more.");
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

        /// <summary>
        /// Office desk 1.6 x 0.8 m after a product photo (a white panel desk): a 25 mm top, two full-depth side panels (18 mm, set
        /// in 20 mm from the ends and from the front and back edges of the top) and a modesty panel between them at the back (-Z),
        /// hanging 0.30 m below the top.
        /// </summary>
        private static void BuildDesk(Transform root)
        {
            const float width = 1.6f;
            const float depth = 0.8f;
            const float top = 0.025f;
            const float panel = 0.018f;
            const float inset = 0.02f;
            float legHeight = DeskHeight - top;

            var topMesh = new GrayboxMeshBuilder();
            var panels = new GrayboxMeshBuilder();
            topMesh.AddRoundedBlock(new Vector3(0f, legHeight, 0f), width, depth, 0.006f, top, 0.002f, 2);
            float sideDepth = depth - 2f * inset;
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (width / 2f - inset - panel / 2f);
                panels.AddBox(new Vector3(x - panel / 2f, 0f, -sideDepth / 2f), new Vector3(x + panel / 2f, legHeight, sideDepth / 2f));
            }
            float innerHalf = width / 2f - inset - panel;
            panels.AddBox(new Vector3(-innerHalf, legHeight - 0.30f, -depth / 2f + 0.08f), new Vector3(innerHalf, legHeight, -depth / 2f + 0.08f + panel));
            topMesh.Attach("Top", root, SurfaceMaterial.DeskWhite);
            panels.Attach("Panels", root, SurfaceMaterial.DeskWhite);
            AddBoxCollider(root, new Vector3(0f, DeskHeight / 2f, 0f), new Vector3(width, DeskHeight, depth));
        }

        /// <summary>
        /// Monitor with a keyboard in front of it and the mouse to the right of the keyboard (as seen by the user, who
        /// faces the screen); stands on a desk top, the screen faces +Z.
        /// </summary>
        private static void BuildWorkstation(Transform root)
        {
            BuildMonitor(root, new Vector3(0f, 0f, -0.10f));
            BuildKeyboard(root, new Vector3(0f, 0f, 0.16f));
            BuildMouse(root, new Vector3(-0.3f, 0f, 0.17f));
        }

        /// <summary>
        /// Conference table 1.8 x 0.9 m after product photos: a light 30 mm top with slightly rounded edges on two T-legs (a
        /// square steel column set in 0.15 m from each end, on a flat foot with rounded ends running across the table, with
        /// rubber glides and a mounting plate under the top). The long side runs along X.
        /// </summary>
        private static void BuildMeetingTable(Transform root)
        {
            const float width = 1.8f;
            const float depth = 0.9f;
            const float top = 0.03f;
            const float footHeight = 0.025f;
            const float footLength = 0.70f;
            const float footWidth = 0.07f;
            var topMesh = new GrayboxMeshBuilder();
            var steel = new GrayboxMeshBuilder();
            var glide = new GrayboxMeshBuilder();
            topMesh.AddRoundedBlock(new Vector3(0f, DeskHeight - top, 0f), width, depth, 0.012f, top, 0.004f, 4);
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (width / 2f - 0.15f);
                steel.AddRoundedBlock(new Vector3(x, 0.006f, 0f), footWidth, footLength, footWidth / 2f, footHeight - 0.006f, 0.004f, 8);
                steel.AddRoundedBlock(new Vector3(x, footHeight - 0.002f, 0f), 0.06f, 0.10f, 0.004f, DeskHeight - top - footHeight + 0.002f, 0.002f, 2);
                steel.AddRoundedBlock(new Vector3(x, DeskHeight - top - 0.012f, 0f), 0.09f, 0.34f, 0.006f, 0.012f, 0.002f, 3);
                foreach (int end in new[] { -1, 1 })
                {
                    glide.AddLathe(new[] { P(0f, 0f, true), P(0.014f, 0f, true), P(0.014f, 0.006f, true), P(0f, 0.006f, true) }, 16, true, new Vector3(x, 0f, end * (footLength / 2f - 0.04f)), Quaternion.identity);
                }
            }
            topMesh.Attach("Top", root, SurfaceMaterial.OfficeTopLight);
            steel.Attach("Legs", root, SurfaceMaterial.OfficeLegWhite);
            glide.Attach("Glides", root, SurfaceMaterial.MouseBlack);
            AddBoxCollider(root, new Vector3(0f, DeskHeight / 2f, 0f), new Vector3(width, DeskHeight, depth));
        }

        /// <summary>
        /// Five-drawer filing cabinet 0.45 x 1.32 x 0.6 m after a product photo, in cool white sheet steel: a body on a recessed plinth,
        /// five flat drawer fronts with 4 mm joints and a deep pocket in the middle of each, open in its lower half only so a finger can hook behind the filled upper half (no lock). The drawers face +Z.
        /// </summary>
        private static void BuildFilingCabinet(Transform root)
        {
            const float width = 0.45f;
            const float height = 1.32f;
            const float depth = 0.6f;
            const float plinth = 0.03f;
            const float fronts = 5;
            var steel = new GrayboxMeshBuilder();
            var pulls = new GrayboxMeshBuilder();
            steel.AddRoundedBlock(new Vector3(0f, plinth, 0f), width, depth, 0.006f, height - plinth, 0.004f, 3);
            float pitch = (height - plinth - 0.02f) / fronts;
            for (int i = 0; i < fronts; i++)
            {
                float y0 = plinth + 0.01f + i * pitch + 0.002f;
                float y1 = y0 + pitch - 0.004f;
                float cy = (y0 + y1) / 2f;
                float xe = width / 2f - 0.012f;
                const float pocketHalfX = 0.05f;
                const float pocketHalfY = 0.0225f;
                float z0 = depth / 2f;
                float z1 = depth / 2f + 0.020f;
                // The drawer front (20 mm proud of the body) with a deep pocket (100 x 45 mm) for the pull. Only the lower half of the pocket is
                // open: the upper half is filled by the front itself, so a finger put into the pocket hooks behind it and can pull the drawer.
                steel.AddBox(new Vector3(-xe, y0, z0), new Vector3(-pocketHalfX, y1, z1));
                steel.AddBox(new Vector3(pocketHalfX, y0, z0), new Vector3(xe, y1, z1));
                steel.AddBox(new Vector3(-pocketHalfX, cy, z0), new Vector3(pocketHalfX, y1, z1));
                steel.AddBox(new Vector3(-pocketHalfX, y0, z0), new Vector3(pocketHalfX, cy - pocketHalfY, z1));
                // The pocket's lining in a darker grey: the back, the floor and the two sides.
                pulls.AddBox(new Vector3(-pocketHalfX, cy - pocketHalfY, z0), new Vector3(pocketHalfX, cy, z0 + 0.002f));
                pulls.AddBox(new Vector3(-pocketHalfX, cy - pocketHalfY, z0 + 0.002f), new Vector3(pocketHalfX, cy - pocketHalfY + 0.003f, z1 - 0.001f));
                pulls.AddBox(new Vector3(-pocketHalfX, cy - pocketHalfY + 0.003f, z0 + 0.002f), new Vector3(-pocketHalfX + 0.002f, cy, z1 - 0.001f));
                pulls.AddBox(new Vector3(pocketHalfX - 0.002f, cy - pocketHalfY + 0.003f, z0 + 0.002f), new Vector3(pocketHalfX, cy, z1 - 0.001f));
            }
            steel.Attach("Cabinet_Body", root, SurfaceMaterial.CabinetWhite);
            pulls.Attach("Cabinet_Pulls", root, SurfaceMaterial.CabinetPull);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.01f), new Vector3(width, height, depth + 0.02f));
        }

        /// <summary>Height of the middle of a wall TV's screen.</summary>
        public const float WallScreenCenter = 1.6f;

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
        /// Perforated raised-floor tile 0.6 x 0.6 m (cold aisle air supply), lying on the floor, after a product photo: a light galvanised steel
        /// panel perforated with round holes at a 25 mm pitch (a texture, averaging out at a distance) in a 20 mm rolled frame, over the dark
        /// plenum. Its top is 12 mm above the floor.
        /// </summary>
        private static void BuildFloorVentTile(Transform root)
        {
            const float half = 0.3f;
            const float top = 0.012f;
            const float rim = 0.02f;
            var panel = new GrayboxMeshBuilder();
            var frame = new GrayboxMeshBuilder();
            var plenum = new GrayboxMeshBuilder();
            plenum.AddBox(new Vector3(-half + 0.004f, 0f, -half + 0.004f), new Vector3(half - 0.004f, 0.002f, half - 0.004f));
            float uv = 2f * (half - rim) / 0.025f;
            panel.AddPanel(new Vector3(-half + rim, top - 0.001f, -half + rim), new Vector3(half - rim, top - 0.001f, -half + rim), new Vector3(half - rim, top - 0.001f, half - rim),
                new Vector3(-half + rim, top - 0.001f, half - rim), Vector3.up, Vector2.zero, new Vector2(uv, 0f), new Vector2(uv, uv), new Vector2(0f, uv));
            // The frame: four rolled edges, with a small step down to the panel.
            frame.AddBox(new Vector3(-half, 0.002f, -half), new Vector3(half, top, -half + rim));
            frame.AddBox(new Vector3(-half, 0.002f, half - rim), new Vector3(half, top, half));
            frame.AddBox(new Vector3(-half, 0.002f, -half + rim), new Vector3(-half + rim, top, half - rim));
            frame.AddBox(new Vector3(half - rim, 0.002f, -half + rim), new Vector3(half, top, half - rim));
            plenum.Attach("Plenum", root, SurfaceMaterial.Upholstery);
            panel.Attach("Panel", root, SurfaceMaterial.FloorTilePerforated);
            frame.Attach("Frame", root, SurfaceMaterial.GalvanisedSteel);
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
            RoomInfrastructure.Trunking(root, "Trunking", new Vector3(0f, bottom + height, 0f), new Vector3(0f, ceiling, 0f), Vector3.forward);
            AddBoxCollider(root, new Vector3(0f, bottom + height / 2f, depth / 2f), new Vector3(0.6f, height, depth));
        }

        /// <summary>360° nozzle of the server room's inert-gas extinguishing system (no water near the racks).</summary>
        private static void BuildGasNozzle(Transform root)
        {
            // Hangs below its pivot (the underside of the pipe): a threaded stem, a hex collar, a chrome body with twelve outlet ports and a flat deflector cap.
            var chrome = new GrayboxMeshBuilder();
            var ports = new GrayboxMeshBuilder();
            chrome.AddLathe(new[] { P(0.0075f, 0f, true), P(0.0075f, -0.020f, true), P(0.0001f, -0.020f, true) }, 12, true, Vector3.zero, Quaternion.identity);
            chrome.AddLathe(new[] { P(0.0165f, -0.012f, true), P(0.0165f, -0.026f, true), P(0.0001f, -0.026f, true) }, 6, false, Vector3.zero, Quaternion.identity);
            chrome.AddLathe(new[] { P(0.0145f, -0.026f, true), P(0.0145f, -0.066f, true), P(0.019f, -0.070f), P(0.019f, -0.078f, true), P(0.0001f, -0.078f, true) }, 24, true, Vector3.zero, Quaternion.identity);
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * 2f * Mathf.PI;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                ports.AddDisc(new Vector3(0f, -0.048f, 0f) + outward * 0.0147f, outward, 0.0042f, 8);
            }
            chrome.Attach("Nozzle", root, SurfaceMaterial.Chrome);
            ports.Attach("Ports", root, SurfaceMaterial.BlackHousing);
        }
    }
}
