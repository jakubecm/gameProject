using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Dresses the graybox rooms. The shell gets surface materials, door frames and a suspended ceiling
    /// (600 mm grid at the 3 m ceiling plane, so the room height does not change); each room gets a
    /// <c>Dressing</c> group with furniture, light fittings, fire safety and the three building services:
    /// pipes (fire water, water, gas risers), electrical power (distribution boards, trunking, power trays)
    /// and data (data trays, data outlets). Every cable and pipe run ends in the floor, the ceiling or a
    /// wall, so rooms stay independent modules.
    ///
    /// Dressing is looks only; the game logic (rack and terminal slots) stays outside it. Everything stays
    /// clear of the openings, the keep-clear walkway (up to the opening height), the rack slots and the
    /// space in front of terminal slots. Ceiling fittings sit in the centre of a grid tile or on a grid line.
    ///
    /// Dress Graybox Rooms only adds what is missing, so hand edits survive. Rebuild Graybox Dressing
    /// throws the generated dressing, props and material settings away and builds them again.
    /// </summary>
    public static class RoomDressingTool
    {
        public const string DressingName = "Dressing";
        public const string SuspendedCeilingName = "SuspendedCeiling";

        /// <summary>Size of a suspended-ceiling tile; grid lines run at multiples of it from the room center.</summary>
        public const float CeilingTile = 0.6f;

        // The first version of Create Graybox Rooms put plain boxes here; the Dressing group replaces them.
        private const string LegacyGrayboxName = "Graybox";

        /// <summary>Half width of the corridor arms (the keep-clear cross is 1.2 m either side).</summary>
        private const float CorridorHalfWidth = 1.5f;

        // Heights of the trays above the server racks: above the 2.6 m door head, below the 3 m ceiling.
        private const float PowerTrayBottom = 2.62f;
        private const float DataTrayBottom = 2.76f;
        private const float TrayWidth = 0.3f;

        // Socket heights: flush wall outlets and the desk-height parapet trunking in the office.
        private const float OutletHeight = 0.3f;
        private const float ParapetHeight = 0.88f;

        private static readonly Dictionary<RoomType, Action<Transform>> Dressers = new Dictionary<RoomType, Action<Transform>>
        {
            { RoomType.Corridor, DressCorridor },
            { RoomType.ServerAisle, DressServerAisle },
            { RoomType.Office, DressOffice },
        };

        private static float Wall => RoomContract.InteriorHalfSize;

        private static float Ceiling => RoomContract.CeilingHeight;

        [MenuItem("Tools/Cold Shutdown/Dress Graybox Rooms")]
        public static void DressGrayboxRooms()
        {
            GrayboxPropsTool.CreateGrayboxProps();
            DressAll(false);
        }

        [MenuItem("Tools/Cold Shutdown/Rebuild Graybox Dressing")]
        public static void RebuildGrayboxDressing()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Cold Shutdown",
                    "Rebuild all graybox props, surface materials, door frames, suspended ceilings and Dressing groups? " +
                    "Hand edits in them are lost; slots and the rest of the rooms are kept.", "Rebuild", "Cancel"))
            {
                return;
            }

            GrayboxMaterials.ResetAll();
            GrayboxPropsTool.BuildProps(true);
            DressAll(true);
        }

        private static void DressAll(bool rebuild)
        {
            EditPrefab(RoomShellTool.ShellPath, root => DressShell(root, rebuild));
            EditPrefab(RoomShellTool.WallPlugPath, root => AssignDefaultSurfaces(root));

            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                if (Dressers.TryGetValue(type, out var dress))
                {
                    EditPrefab(GrayboxRoomsTool.PrefabPath(type), root => DressRoom(root, dress, rebuild));
                }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Opens a prefab, lets <paramref name="edit"/> change it and saves it only if something changed.</summary>
        private static void EditPrefab(string path, Func<GameObject, bool> edit)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError($"{path} not found. Run Create Room Shell and Create Graybox Rooms first.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (edit(root))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"Dressed {path}.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool DressShell(GameObject root, bool rebuild)
        {
            bool changed = AssignDefaultSurfaces(root);
            var shell = root.transform.Find("Shell");

            foreach (Direction side in Enum.GetValues(typeof(Direction)))
            {
                var wall = shell.Find($"Wall_{side}");
                changed |= Replace(wall, nameof(GrayboxProp.DoorFrame), rebuild,
                    // The wall group stands on the opening pose, which is the door frame's pivot.
                    () => GrayboxPropsTool.Place(GrayboxProp.DoorFrame, wall, Vector3.zero));
            }

            changed |= Replace(shell, SuspendedCeilingName, rebuild, () => BuildSuspendedCeiling(shell));
            return changed;
        }

        private static bool DressRoom(GameObject root, Action<Transform> dress, bool rebuild)
        {
            bool changed = false;
            var legacy = root.transform.Find(LegacyGrayboxName);
            if (legacy != null)
            {
                UnityEngine.Object.DestroyImmediate(legacy.gameObject);
                changed = true;
            }

            changed |= Replace(root.transform, DressingName, rebuild,
                () => dress(GrayboxUtility.CreateGroup(DressingName, root.transform)));
            return changed;
        }

        /// <summary>Builds the named child if it is missing; on a rebuild the old one is removed first.</summary>
        private static bool Replace(Transform parent, string childName, bool rebuild, Action build)
        {
            var existing = parent.Find(childName);
            if (existing != null && !rebuild)
            {
                return false;
            }
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            build();
            return true;
        }

        /// <summary>Gives meshes that still use ProBuilder's default material the surface their name implies.</summary>
        private static bool AssignDefaultSurfaces(GameObject root)
        {
            bool changed = false;
            foreach (var mesh in root.GetComponentsInChildren<ProBuilderMesh>())
            {
                var materials = mesh.GetComponent<MeshRenderer>().sharedMaterials;
                if (materials.Length == 0 || materials.Any(m => m != BuiltinMaterials.defaultMaterial))
                {
                    continue;
                }

                var surface = mesh.name == "Floor" ? SurfaceMaterial.Floor
                    : mesh.name == "Ceiling" ? SurfaceMaterial.Ceiling
                    : SurfaceMaterial.Wall;
                GrayboxUtility.SetSurface(mesh, surface);
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Exposed T-bar grid of a lay-in ceiling: main and cross tees every 600 mm from the room center and
        /// a wall angle around the edge. The ceiling slab above is the tile surface, so the grid hangs only
        /// a centimetre below the 3 m ceiling plane and every room keeps its height. The grid is symmetric,
        /// so it lines up whichever way the generator turns the room.
        /// </summary>
        private static void BuildSuspendedCeiling(Transform shell)
        {
            var grid = GrayboxUtility.CreateGroup(SuspendedCeilingName, shell);
            const float bar = 0.024f;
            float depth = GrayboxPropsTool.CeilingGridDepth;
            float bottom = Ceiling - depth;
            int lines = Mathf.FloorToInt(Wall / CeilingTile);

            for (int i = -lines; i <= lines; i++)
            {
                float at = i * CeilingTile;
                GrayboxUtility.CreateBoxBetween($"Tee_X_{i}", grid, new Vector3(-Wall, bottom, at - bar / 2f), new Vector3(Wall, Ceiling, at + bar / 2f), SurfaceMaterial.CeilingGrid);
                GrayboxUtility.CreateBoxBetween($"Tee_Z_{i}", grid, new Vector3(at - bar / 2f, bottom, -Wall), new Vector3(at + bar / 2f, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            }

            GrayboxUtility.CreateBoxBetween("WallAngle_N", grid, new Vector3(-Wall, bottom, Wall - bar), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_S", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(Wall, Ceiling, -Wall + bar), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_E", grid, new Vector3(Wall - bar, bottom, -Wall), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_W", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(-Wall + bar, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
        }

        /// <summary>
        /// A cross-shaped corridor 3 m wide between four partition blocks (technical rooms behind them).
        /// 1200 mm fittings run across the corridor arms; the building risers (fire water with a hose
        /// cabinet, water, gas) and a distribution board stand along the partitions.
        /// </summary>
        private static void DressCorridor(Transform dressing)
        {
            var architecture = GrayboxUtility.CreateGroup("Architecture", dressing);
            float extent = Wall - CorridorHalfWidth;
            float center = CorridorHalfWidth + extent / 2f;
            const float skirtingHeight = 0.1f;
            const float skirtingDepth = 0.015f;

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    string corner = Corner(sx, sz);
                    GrayboxUtility.CreateStandingBox($"Partition_{corner}", architecture, new Vector3(sx * center, 0f, sz * center),
                        new Vector3(extent, Ceiling, extent), SurfaceMaterial.Wall);

                    // Skirting on the two partition faces that line the corridor.
                    float faceX = sx * (CorridorHalfWidth - skirtingDepth / 2f);
                    float faceZ = sz * (CorridorHalfWidth - skirtingDepth / 2f);
                    GrayboxUtility.CreateStandingBox($"Skirting_{corner}_X", architecture, new Vector3(faceX, 0f, sz * center),
                        new Vector3(skirtingDepth, skirtingHeight, extent), SurfaceMaterial.Trim, false);
                    GrayboxUtility.CreateStandingBox($"Skirting_{corner}_Z", architecture, new Vector3(sx * center, 0f, faceZ),
                        new Vector3(extent, skirtingHeight, skirtingDepth), SurfaceMaterial.Trim, false);
                }
            }

            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            foreach (float at in new[] { 2.7f, 5.1f })
            {
                // North-south arms: fittings across the arm (long side along X); east-west arms: along Z.
                GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(0f, Ceiling, at), 90f, $"Light_N_{at:0.0}");
                GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(0f, Ceiling, -at), 90f, $"Light_S_{at:0.0}");
                GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(at, Ceiling, 0f), 0f, $"Light_E_{at:0.0}");
                GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(-at, Ceiling, 0f), 0f, $"Light_W_{at:0.0}");
            }
            PlaceOnCeiling(GrayboxProp.Sprinkler, ceiling, (0.3f, -0.3f), (0.3f, 3.9f), (3.9f, -0.3f), (-0.3f, -3.9f), (-3.9f, 0.3f));
            PlaceOnCeiling(GrayboxProp.SmokeDetector, ceiling, (-0.3f, 1.5f), (0.3f, -1.5f));

            // Pipes: risers clamped to the partitions, each passing from the floor into the ceiling.
            var pipes = GrayboxUtility.CreateGroup("Pipes", dressing);
            var westFacing = new Vector3(-1f, 0f, 0f);
            var eastFacing = new Vector3(1f, 0f, 0f);
            const float fireRadius = 0.045f;
            var fire = RoomInfrastructure.Riser(pipes, "FireWaterRiser", new Vector3(CorridorHalfWidth, 0f, 4.5f), westFacing, fireRadius, SurfaceMaterial.SafetyRed);
            var hydrantZ = 3.3f;
            GrayboxPropsTool.Place(GrayboxProp.HydrantCabinet, pipes, new Vector3(CorridorHalfWidth, 0f, hydrantZ), 270f);
            GrayboxUtility.CreatePipe("HydrantBranch", pipes, fire + Vector3.up * 1.25f,
                new Vector3(fire.x, 1.25f, hydrantZ + 0.325f), 0.025f, SurfaceMaterial.SafetyRed);
            RoomInfrastructure.Riser(pipes, "WaterRiser", new Vector3(-CorridorHalfWidth, 0f, 4.6f), eastFacing, 0.025f, SurfaceMaterial.PipeWater, 1.2f, SurfaceMaterial.PipeWater);
            RoomInfrastructure.Riser(pipes, "GasRiser", new Vector3(-CorridorHalfWidth, 0f, -4.6f), eastFacing, 0.02f, SurfaceMaterial.PipeGas, 1.4f, SurfaceMaterial.PipeGas);

            // Power: a sub-distribution board on the south side of the east arm, with a service outlet.
            var power = GrayboxUtility.CreateGroup("Power", dressing);
            GrayboxPropsTool.Place(GrayboxProp.ElectricalPanel, power, new Vector3(center, 0f, -CorridorHalfWidth), 0f);
            GrayboxPropsTool.Place(GrayboxProp.PowerSocket, power, new Vector3(center + 0.6f, OutletHeight, -CorridorHalfWidth), 0f);

            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            GrayboxPropsTool.Place(GrayboxProp.FireExtinguisher, safety, new Vector3(-center, 0f, CorridorHalfWidth), 180f);
        }

        /// <summary>
        /// Server aisle: perforated cold-aisle tiles in front of the racks; above each rack row a power tray
        /// and a yellow data tray running wall to wall; a data cross-connect tray linking both rows; 1200 mm
        /// fittings over the rack fronts and the aisle; gas extinguishing nozzles instead of sprinklers.
        /// </summary>
        private static void DressServerAisle(Transform dressing)
        {
            var vents = GrayboxUtility.CreateGroup("FloorVents", dressing);
            float rackFront = RoomContract.ServerAisleSlotWallOffset - RoomContract.RackDepth / 2f;
            const float tile = 0.6f;
            int index = 0;
            foreach (var slot in RoomContract.GetServerAisleRackSlots())
            {
                // Two tiles in front of each slot, as wide as the slot pitch.
                float side = Mathf.Sign(slot.X);
                for (int row = 0; row < 2; row++)
                {
                    float x = side * (rackFront - tile / 2f - row * tile);
                    for (int half = -1; half <= 1; half += 2)
                    {
                        float z = slot.Z + half * RoomContract.ServerAisleSlotPitch / 4f;
                        GrayboxPropsTool.Place(GrayboxProp.FloorVentTile, vents, new Vector3(x, 0f, z), 0f, $"VentTile_{index++:00}");
                    }
                }
            }

            // Trays over each rack row: power closer to the wall, data over the rack fronts. Both run from
            // wall to wall above the door head, so the cables come from and go to the neighbouring rooms.
            var power = GrayboxUtility.CreateGroup("Power", dressing);
            var data = GrayboxUtility.CreateGroup("Data", dressing);
            var rowHangers = RoomInfrastructure.Every(-5.4f, 5.4f, 1.2f).ToArray();
            foreach (int side in new[] { -1, 1 })
            {
                string row = side < 0 ? "West" : "East";
                float powerX = side * (Wall - 0.5f);
                float dataX = side * (Wall - 0.95f);
                RoomInfrastructure.Tray(power, $"PowerTray_{row}", new Vector3(powerX, 0f, -Wall), new Vector3(powerX, 0f, Wall),
                    TrayWidth, PowerTrayBottom, SurfaceMaterial.Metal, SurfaceMaterial.PowerCable, rowHangers);
                RoomInfrastructure.Tray(data, $"DataTray_{row}", new Vector3(dataX, 0f, -Wall), new Vector3(dataX, 0f, Wall),
                    TrayWidth, DataTrayBottom, SurfaceMaterial.DataTray, SurfaceMaterial.DataCable, rowHangers);
            }

            // Data cross-connect between the two rows, along the north wall behind the light fittings.
            float crossZ = Wall - 0.25f;
            float crossEnd = Wall - 0.95f - TrayWidth / 2f;
            RoomInfrastructure.Tray(data, "DataTray_CrossConnect", new Vector3(-crossEnd, 0f, crossZ), new Vector3(crossEnd, 0f, crossZ),
                TrayWidth, DataTrayBottom, SurfaceMaterial.DataTray, SurfaceMaterial.DataCable, new[] { -2.4f, 0f, 2.4f });

            // The distribution board feeding the power trays (its trunking goes up into the ceiling).
            GrayboxPropsTool.Place(GrayboxProp.ElectricalPanel, power, new Vector3(2.7f, 0f, Wall), 180f);
            GrayboxPropsTool.Place(GrayboxProp.PowerSocket, power, new Vector3(2.0f, OutletHeight, Wall), 180f);

            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    string corner = Corner(sx, sz);
                    GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(sx * 3.9f, Ceiling, sz * 2.4f), 0f, $"Light_{corner}_Inner");
                    GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(sx * 3.9f, Ceiling, sz * 4.8f), 0f, $"Light_{corner}_Outer");
                }
            }
            GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(0f, Ceiling, 3.3f), 90f, "Light_Aisle_N");
            GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(0f, Ceiling, -3.3f), 90f, "Light_Aisle_S");
            GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(2.7f, Ceiling, 0f), 0f, "Light_Aisle_E");
            GrayboxPropsTool.Place(GrayboxProp.Troffer1200, ceiling, new Vector3(-2.7f, Ceiling, 0f), 0f, "Light_Aisle_W");
            PlaceOnCeiling(GrayboxProp.GasNozzle, ceiling, (-1.5f, 1.5f), (1.5f, -1.5f));
            PlaceOnCeiling(GrayboxProp.SmokeDetector, ceiling, (1.5f, 1.5f), (-1.5f, -1.5f), (2.7f, 4.5f), (-2.7f, 4.5f), (2.7f, -4.5f), (-2.7f, -4.5f));

            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            GrayboxPropsTool.Place(GrayboxProp.FireExtinguisher, safety, new Vector3(-2.7f, 0f, -Wall), 0f);
        }

        /// <summary>
        /// Admin office: two workstations on a parapet trunking with power and data outlets (NW), meeting
        /// corner with a whiteboard (NE), storage (SE), the terminal corner with its outlets (SW), a radiator,
        /// a regular grid of 600 mm fittings, sprinklers and smoke detectors.
        /// </summary>
        private static void DressOffice(Transform dressing)
        {
            var furniture = GrayboxUtility.CreateGroup("Furniture", dressing);
            var power = GrayboxUtility.CreateGroup("PowerAndData", dressing);
            var north = new Vector3(0f, 0f, -1f);

            // NW: two desks against the north wall, chairs facing the wall, outlets on the parapet trunking.
            float deskZ = Wall - 0.4f - 0.02f;
            float trunkingFront = Wall - GrayboxPropsTool.TrunkingDepth;
            foreach (var (x, suffix) in new[] { (-4.6f, "1"), (-2.8f, "2") })
            {
                GrayboxPropsTool.Place(GrayboxProp.Desk, furniture, new Vector3(x, 0f, deskZ), 180f, $"Desk_{suffix}");
                GrayboxPropsTool.Place(GrayboxProp.Workstation, furniture, new Vector3(x, GrayboxPropsTool.DeskHeight, deskZ + 0.05f), 180f, $"Workstation_{suffix}");
                GrayboxPropsTool.Place(GrayboxProp.OfficeChair, furniture, new Vector3(x, 0f, deskZ - 0.78f), 0f, $"OfficeChair_{suffix}");
                GrayboxPropsTool.Place(GrayboxProp.PowerSocket, power, new Vector3(x - 0.25f, ParapetHeight, trunkingFront), 180f, $"PowerSocket_Desk{suffix}");
                GrayboxPropsTool.Place(GrayboxProp.DataSocket, power, new Vector3(x + 0.05f, ParapetHeight, trunkingFront), 180f, $"DataSocket_Desk{suffix}");
            }

            // Parapet trunking from the west wall along the desks, then up into the ceiling.
            const float riseX = -1.66f;
            RoomInfrastructure.Trunking(power, "ParapetTrunking", new Vector3(-Wall, ParapetHeight, Wall), new Vector3(riseX + GrayboxPropsTool.TrunkingWidth / 2f, ParapetHeight, Wall), north);
            RoomInfrastructure.Trunking(power, "ParapetTrunking_Rise", new Vector3(riseX, ParapetHeight, Wall), new Vector3(riseX, Ceiling, Wall), north);

            // NE: meeting table with four chairs, a whiteboard and outlets on the east wall.
            var table = new Vector3(3.55f, 0f, 3.55f);
            GrayboxPropsTool.Place(GrayboxProp.MeetingTable, furniture, table);
            int chair = 1;
            foreach (float dx in new[] { -0.45f, 0.45f })
            {
                GrayboxPropsTool.Place(GrayboxProp.OfficeChair, furniture, table + new Vector3(dx, 0f, 0.85f), 180f, $"OfficeChair_Meeting{chair++}");
                GrayboxPropsTool.Place(GrayboxProp.OfficeChair, furniture, table + new Vector3(dx, 0f, -0.85f), 0f, $"OfficeChair_Meeting{chair++}");
            }
            GrayboxPropsTool.Place(GrayboxProp.Whiteboard, furniture, new Vector3(table.x, 0f, Wall), 180f);
            GrayboxPropsTool.Place(GrayboxProp.PowerSocket, power, new Vector3(Wall, OutletHeight, 2.4f), 270f, "PowerSocket_Meeting");
            GrayboxPropsTool.Place(GrayboxProp.DataSocket, power, new Vector3(Wall, OutletHeight, 2.6f), 270f, "DataSocket_Meeting");

            // SE: filing cabinets on the south wall, shelving on the east wall, an outlet by the cabinets.
            GrayboxPropsTool.Place(GrayboxProp.FilingCabinet, furniture, new Vector3(2.3f, 0f, -Wall + 0.32f), 0f, "FilingCabinet_1");
            GrayboxPropsTool.Place(GrayboxProp.FilingCabinet, furniture, new Vector3(2.8f, 0f, -Wall + 0.32f), 0f, "FilingCabinet_2");
            GrayboxPropsTool.Place(GrayboxProp.Shelf, furniture, new Vector3(Wall - 0.22f, 0f, -2.5f), 270f, "Shelf_1");
            GrayboxPropsTool.Place(GrayboxProp.Shelf, furniture, new Vector3(Wall - 0.22f, 0f, -3.55f), 270f, "Shelf_2");
            GrayboxPropsTool.Place(GrayboxProp.PowerSocket, power, new Vector3(3.6f, OutletHeight, -Wall), 0f, "PowerSocket_Storage");

            // SW: the terminal slot stands on the south wall; its power and data outlets are beside it.
            GrayboxPropsTool.Place(GrayboxProp.Shelf, furniture, new Vector3(-Wall + 0.22f, 0f, -2.5f), 90f, "Shelf_3");
            GrayboxPropsTool.Place(GrayboxProp.PowerSocket, power, new Vector3(-4.55f, OutletHeight, -Wall), 0f, "PowerSocket_Terminal");
            GrayboxPropsTool.Place(GrayboxProp.DataSocket, power, new Vector3(-4.35f, OutletHeight, -Wall), 0f, "DataSocket_Terminal");

            // Heating: a panel radiator on the west wall, its pipes going down into the floor.
            GrayboxPropsTool.Place(GrayboxProp.Radiator, furniture, new Vector3(-Wall, 0f, 3.0f), 90f);

            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            GrayboxPropsTool.Place(GrayboxProp.FireExtinguisher, safety, new Vector3(-Wall, 0f, -4.2f), 90f);

            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            foreach (float x in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            {
                foreach (float z in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
                {
                    GrayboxPropsTool.Place(GrayboxProp.Troffer600, ceiling, new Vector3(x, Ceiling, z), 0f, $"Light_{x:0.0}_{z:0.0}");
                }
            }
            var sprinklers = new List<(float, float)>();
            foreach (float x in new[] { -3.9f, -0.3f, 3.3f })
            {
                foreach (float z in new[] { -3.9f, -0.3f, 3.3f })
                {
                    sprinklers.Add((x, z));
                }
            }
            PlaceOnCeiling(GrayboxProp.Sprinkler, ceiling, sprinklers.ToArray());
            PlaceOnCeiling(GrayboxProp.SmokeDetector, ceiling, (-2.7f, 2.7f), (2.7f, -2.7f));
        }

        /// <summary>Places a ceiling prop at each (x, z); positions are grid-tile centers (odd multiples of 0.3 m).</summary>
        private static void PlaceOnCeiling(GrayboxProp prop, Transform parent, params (float x, float z)[] positions)
        {
            foreach (var (x, z) in positions)
            {
                GrayboxPropsTool.Place(prop, parent, new Vector3(x, Ceiling, z), 0f, $"{prop}_{x:0.0}_{z:0.0}");
            }
        }

        private static string Corner(int sx, int sz) => $"{(sz > 0 ? "N" : "S")}{(sx > 0 ? "E" : "W")}";
    }
}
