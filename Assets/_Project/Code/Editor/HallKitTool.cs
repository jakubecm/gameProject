using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Builds the server hall kit (dimensions from <see cref="HallContract"/>):
    /// - <c>Room_Hall.prefab</c>: one 12 x 12 m tile without walls: epoxy floor, roof deck at 6 m, steel girders on
    ///   the column lines and secondary beams, and two pods side by side across the tile. Each pod is a long contained
    ///   cold aisle running east-west: two rows of fourteen racks facing each other across the aisle (perforated floor,
    ///   containment roof just above the racks, automatic doors at both ends, LED strips inside), outlined in yellow;
    ///   the rack backs of both pods face the hot aisle in the middle. Overhead, all above the hanging sign: the return
    ///   air duct over the hot aisle with grilles in its underside, over every pod a supply duct from the cooling plant
    ///   with a drop into the containment roof (every aisle is connected to the central HVAC), a power tray over the
    ///   outer row and a data tray over the inner row with one drop each into the pod, an inert-gas line with nozzles,
    ///   smoke detectors, pendant LED high-bays over the walkways, two pendant CCTV domes and the tile sign with its
    ///   code ("B3");
    /// - <c>Hall/HallSide_&lt;Kind&gt;.prefab</c> per <see cref="HallSideKind"/>, authored for the north side: nothing
    ///   (Open, and the unused Closed: a hall is never divided), or the 6 m perimeter wall (Door with the opening, an
    ///   automatic door and a light switch, Wall without) with large supply grilles a little above the floor, small
    ///   return grilles under the roof, the gas release station, a manual call point and a wall dome camera; a plain
    ///   wall also carries an inert-gas riser from the floor to the roof;
    /// - <c>Hall/HallCorner_&lt;Kind&gt;.prefab</c> per <see cref="HallCornerKind"/>, authored for the north-east corner:
    ///   a quarter of the building column and the pieces of the walkway line that turn there; an outer corner also has a
    ///   cable tray riser from the floor to the roof;
    /// - <c>Data/HallKit.asset</c> pointing at all pieces, and <c>Data/RoomDefinition_Hall.asset</c>.
    /// The hall holds infrastructure only: no plant (heat exchangers are in the cooling plant, transformers and
    /// switchgear in the power room), no water (no sprinklers, hose cabinets or extinguishers; inert gas only), no
    /// outlets and no partitions at all. Every overhead run continues in the neighbouring tile (all tiles
    /// share the same grid) or ends in the perimeter wall, so tiles stay independent modules. Nothing below 2.6 m stands
    /// in the walkways around the pods; the hall tests check that.
    /// Create Hall Kit only builds what is missing; Rebuild Hall Kit replaces every piece (same GUIDs).
    /// </summary>
    public static class HallKitTool
    {
        public const string HallFolder = RoomShellTool.RoomsFolder + "/Hall";
        public const string KitPath = GrayboxRoomsTool.DefinitionsFolder + "/HallKit.asset";

        // Overhead heights: the hanging signs reach up to 4.2 m (GrayboxPropsTool.HallSignDrop), so the return duct,
        // the trays and the gas lines all run above them, under the girders (5.2 m); secondary beams under the deck.
        // Everything overhead runs east-west, so it continues into the neighbouring tile or ends in a perimeter wall.
        private const float DuctBottom = 4.4f;
        private const float DuctTop = 4.9f;
        // The return duct over the hot aisle; the sign rods (z = ±0.3) pass just beside it.
        private const float DuctHalfWidth = 0.25f;
        // The supply duct over every pod, at the height of the return duct; its drop lands on the containment roof.
        private const float SupplyHalfWidth = 0.25f;
        private const float TrayBottom = 5.0f;
        private const float TrayWidth = 0.3f;
        // Power tray over the outer rack row, data tray over the inner row of each pod.
        private const float PowerTrayZ = HallContract.PodCenterZ + 1.2f;
        private const float DataTrayZ = HallContract.PodCenterZ - 1.2f;
        private const float GasY = 5.15f;
        private const float RoofPanel = 0.03f;
        private const float GirderBottom = 5.2f;
        private const float BeamBottom = 5.6f;
        private const float MarkingWidth = 0.05f;
        // Pendant high-bays: over the north and south walkways (along Z) and over the cross walkways at the pod ends
        // (turned along X, so their wires stay clear of the trays and the gas line).
        private const float WalkwayLightX = 2.4f;
        private const float WalkwayLightZ = 5.1f;
        private const float CrossLightX = 5.1f;
        private const float CrossLightZ = 2.0f;

        private static float Roof => HallContract.CeilingHeight;
        private static float Wall => HallContract.WallInnerOffset;
        private static float Half => RoomContract.CellHalfSize;
        private static float PodX => HallContract.PodHalfLength;
        private static float PodZ => HallContract.PodHalfDepth;
        private static float PerimeterLine => HallContract.PerimeterLineOffset;
        private static float Containment => HallContract.ContainmentHeight;

        public static string TilePath => GrayboxRoomsTool.PrefabPath(RoomType.Hall);

        public static string SidePath(HallSideKind kind) => $"{HallFolder}/HallSide_{kind}.prefab";

        public static string CornerPath(HallCornerKind kind) => $"{HallFolder}/HallCorner_{kind}.prefab";

        [MenuItem("Tools/Cold Shutdown/Create Hall Kit")]
        public static void CreateHallKit() => Build(false);

        [MenuItem("Tools/Cold Shutdown/Rebuild Hall Kit")]
        public static void RebuildHallKit()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Cold Shutdown",
                    "Rebuild the hall tile and all hall side and corner pieces? Hand edits in them are lost.", "Rebuild", "Cancel"))
            {
                return;
            }
            Build(true);
        }

        private static void Build(bool replace)
        {
            GrayboxPropsTool.CreateGrayboxProps();

            Save(TilePath, "Room_Hall", BuildTile, replace);
            foreach (HallSideKind kind in Enum.GetValues(typeof(HallSideKind)))
            {
                Save(SidePath(kind), $"HallSide_{kind}", root => BuildSide(root, kind), replace);
            }
            foreach (HallCornerKind kind in Enum.GetValues(typeof(HallCornerKind)))
            {
                Save(CornerPath(kind), $"HallCorner_{kind}", root => BuildCorner(root, kind), replace);
            }

            SaveKit();
            GrayboxRoomsTool.SaveDefinition(RoomType.Hall, AssetDatabase.LoadAssetAtPath<GameObject>(TilePath));
            AssetDatabase.SaveAssets();
        }

        private static void Save(string path, string name, Action<Transform> build, bool replace)
        {
            if (!replace && RoomShellTool.SkipExisting(path))
            {
                return;
            }
            var root = new GameObject(name);
            build(root.transform);
            RoomShellTool.SaveAsPrefab(root, path);
            Debug.Log($"Hall piece saved to {path}.");
        }

        private static void SaveKit()
        {
            var kit = AssetDatabase.LoadAssetAtPath<HallKit>(KitPath);
            if (kit == null)
            {
                Directory.CreateDirectory(GrayboxRoomsTool.DefinitionsFolder);
                kit = ScriptableObject.CreateInstance<HallKit>();
                AssetDatabase.CreateAsset(kit, KitPath);
            }

            var serialized = new SerializedObject(kit);
            void Assign(string property, string path)
            {
                var field = serialized.FindProperty(property);
                if (field.objectReferenceValue == null)
                {
                    field.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            Assign("sideOpen", SidePath(HallSideKind.Open));
            Assign("sideClosed", SidePath(HallSideKind.Closed));
            Assign("sideDoor", SidePath(HallSideKind.Door));
            Assign("sideWall", SidePath(HallSideKind.Wall));
            Assign("cornerInner", CornerPath(HallCornerKind.Inner));
            Assign("cornerConcave", CornerPath(HallCornerKind.Concave));
            Assign("cornerEdgeFirst", CornerPath(HallCornerKind.EdgeFirst));
            Assign("cornerEdgeSecond", CornerPath(HallCornerKind.EdgeSecond));
            Assign("cornerOuter", CornerPath(HallCornerKind.Outer));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(kit);
        }

        // ---------------------------------------------------------------- tile

        private static void BuildTile(Transform root)
        {
            var structure = GrayboxUtility.CreateGroup("Structure", root);
            GrayboxUtility.CreateBox("Floor", structure, new Vector3(0f, -0.05f, 0f), new Vector3(RoomContract.CellSize, 0.1f, RoomContract.CellSize), SurfaceMaterial.HallFloor);
            GrayboxUtility.CreateBox("RoofDeck", structure, new Vector3(0f, Roof + HallContract.RoofThickness / 2f, 0f),
                new Vector3(RoomContract.CellSize, HallContract.RoofThickness, RoomContract.CellSize), SurfaceMaterial.RoofDeck);

            // Primary girders on the column lines (the cell edges): each tile builds its half of the profile, so
            // two neighbouring tiles make one I-beam; along the perimeter the half sits on the wall.
            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                var edge = GrayboxUtility.CreateGroup($"Girder_{(Direction)i}", structure);
                edge.localRotation = Quaternion.Euler(0f, i * RoomContract.RotationStepDegrees, 0f);
                const float flange = 0.15f;
                GrayboxUtility.CreateBoxBetween("Flange_Bottom", edge, new Vector3(-Half, GirderBottom, Half - flange), new Vector3(Half, GirderBottom + 0.025f, Half), SurfaceMaterial.Steel);
                GrayboxUtility.CreateBoxBetween("Flange_Top", edge, new Vector3(-Half, Roof - 0.025f, Half - flange), new Vector3(Half, Roof, Half), SurfaceMaterial.Steel);
                GrayboxUtility.CreateBoxBetween("Web", edge, new Vector3(-Half, GirderBottom, Half - 0.007f), new Vector3(Half, Roof, Half), SurfaceMaterial.Steel);
            }

            // Secondary beams every 3 m, carrying the deck, the lights and the camera mounts.
            foreach (float z in new[] { -3f, 0f, 3f })
            {
                IBeamAlongX(structure, $"Beam_{z:0}", z, -Half, Half, BeamBottom, Roof - BeamBottom, 0.18f);
            }

            var dressing = GrayboxUtility.CreateGroup(RoomDressingTool.DressingName, root);
            AddRackSlots(root, HallContract.GetPodRackSlots());
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                BuildPod(dressing, pod);
            }
            BuildTileServices(dressing);

            var cameras = GrayboxUtility.CreateGroup(RoomDressingTool.SecurityCamerasName, root);
            // Pendant domes clamped to the secondary beams over the cross walkways, each looking along a pod.
            GrayboxPropsTool.Place(GrayboxProp.SecurityPendant, cameras, new Vector3(CrossLightX, BeamBottom, 3f), 270f, "SecurityPendant_E");
            GrayboxPropsTool.Place(GrayboxProp.SecurityPendant, cameras, new Vector3(-CrossLightX, BeamBottom, -3f), 90f, "SecurityPendant_W");

            var signs = GrayboxUtility.CreateGroup("Signs", root);
            // Over the middle of the hot aisle, seen from both cross walkways; its rods pass beside the return duct.
            GrayboxPropsTool.Place(GrayboxProp.HallSignCube, signs, new Vector3(0f, Roof, 0f), 0f, "TileSign");
        }

        /// <summary>
        /// One pod (0 = north, 1 = south): two rows of fourteen racks facing each other across the cold aisle. The aisle
        /// is contained on every side: rack backs and sides outside, a roof a little above the racks with fascia panels,
        /// automatic doors at both ends. Its floor is perforated tiles only (cold air comes up from the raised floor), lit
        /// by two LED strips. A yellow outline around the pod and pedestrian crossings in front of both aisle doors.
        /// </summary>
        private static void BuildPod(Transform dressing, int index)
        {
            float center = HallContract.PodZ(index);
            string name = index == 0 ? "N" : "S";
            var pod = GrayboxUtility.CreateGroup($"Pod_{name}", dressing);
            const float tile = 0.6f;
            float aisle = HallContract.AisleWidth / 2f;
            var floor = GrayboxUtility.CreateGroup("PerforatedFloor", pod);
            int count = 0;
            for (float x = -PodX + tile / 2f; x < PodX; x += tile)
            {
                for (float z = -aisle + tile / 2f; z < aisle; z += tile)
                {
                    GrayboxPropsTool.Place(GrayboxProp.FloorVentTile, floor, new Vector3(x, 0f, center + z), 0f, $"VentTile_{count++:00}");
                }
            }

            float rackTop = RoomContract.RackHeight;
            GrayboxUtility.CreateBoxBetween("ContainmentRoof", pod, new Vector3(-PodX, Containment, center - PodZ), new Vector3(PodX, Containment + RoofPanel, center + PodZ), SurfaceMaterial.Metal);
            // Fascia panels close the gap between the rack tops and the roof all round, just inside the pod's outline.
            const float fascia = 0.02f;
            foreach (int side in new[] { -1, 1 })
            {
                string edge = side > 0 ? "N" : "S";
                GrayboxUtility.CreateBoxBetween($"Fascia_{edge}", pod, new Vector3(-PodX, rackTop, center + side * (PodZ - fascia)), new Vector3(PodX, Containment, center + side * PodZ), SurfaceMaterial.Metal);
                edge = side > 0 ? "E" : "W";
                GrayboxUtility.CreateBoxBetween($"Fascia_{edge}", pod, new Vector3(side * (PodX - fascia), rackTop, center - PodZ), new Vector3(side * PodX, Containment, center + PodZ), SurfaceMaterial.Metal);
                GrayboxPropsTool.Place(GrayboxProp.AisleDoor, pod, new Vector3(side * PodX, 0f, center), side > 0 ? 90f : 270f, $"AisleDoor_{edge}");
                GrayboxPropsTool.Place(GrayboxProp.AisleLight, pod, new Vector3(side * PodX / 2f, Containment, center), 90f, $"AisleLight_{edge}");
            }

            // Yellow outline 0.3 m around the pod and crossings out of both aisle doors.
            var lines = GrayboxUtility.CreateGroup("Markings", pod);
            float outX = PodX + HallContract.PodOutline;
            float outZ = PodZ + HallContract.PodOutline;
            Marking(lines, "Outline_N", new Vector3(-outX, 0f, center + outZ), new Vector3(outX, 0f, center + outZ));
            Marking(lines, "Outline_S", new Vector3(-outX, 0f, center - outZ), new Vector3(outX, 0f, center - outZ));
            Marking(lines, "Outline_E", new Vector3(outX, 0f, center - outZ), new Vector3(outX, 0f, center + outZ));
            Marking(lines, "Outline_W", new Vector3(-outX, 0f, center - outZ), new Vector3(-outX, 0f, center + outZ));
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float z in new[] { -0.4f, 0f, 0.4f })
                {
                    GrayboxUtility.CreateBoxBetween($"Crossing_{(side > 0 ? "E" : "W")}_{z:0.0}", lines, new Vector3(side * (PodX + 0.25f), 0f, center + z - 0.1f),
                        new Vector3(side * (PodX + 1.0f), 0.002f, center + z + 0.1f), SurfaceMaterial.SafetyYellow);
                }
            }
        }

        private static void BuildTileServices(Transform dressing)
        {
            // Return air duct along the hot aisle (east-west), continuing into the neighbours; small grilles in its
            // underside take the hot air from the rack backs back to the cooling plant.
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            GrayboxUtility.CreateBoxBetween("ReturnDuct", air, new Vector3(-Half, DuctBottom, -DuctHalfWidth), new Vector3(Half, DuctTop, DuctHalfWidth), SurfaceMaterial.Metal);
            foreach (float x in new[] { -3.6f, -1.2f, 1.2f, 3.6f })
            {
                GrayboxUtility.CreateBox($"ReturnGrille_{x:0.0}", air, new Vector3(x, DuctBottom - 0.005f, 0f), new Vector3(0.6f, 0.01f, 0.3f), SurfaceMaterial.FloorVent, false);
            }
            foreach (float x in new[] { -4.8f, -2.4f, 2.4f, 4.8f })
            {
                foreach (var (z, edge) in new[] { (-DuctHalfWidth - 0.03f, "A"), (DuctHalfWidth + 0.03f, "B") })
                {
                    GrayboxUtility.CreateBoxBetween($"DuctRod_{x:0.0}_{edge}", air, new Vector3(x - 0.006f, DuctBottom - 0.02f, z - 0.006f), new Vector3(x + 0.006f, Roof, z + 0.006f), SurfaceMaterial.Metal);
                }
                GrayboxUtility.CreateBoxBetween($"DuctTrapeze_{x:0.0}", air, new Vector3(x - 0.02f, DuctBottom - 0.04f, -DuctHalfWidth - 0.05f), new Vector3(x + 0.02f, DuctBottom - 0.02f, DuctHalfWidth + 0.05f), SurfaceMaterial.Metal);
            }

            // Every pod is connected to the cooling plant: a supply duct along the pod (it continues in the neighbouring
            // tiles and ends in the perimeter wall) with a drop down onto the middle of the containment roof.
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                float z = HallContract.PodZ(pod);
                string name = pod == 0 ? "N" : "S";
                GrayboxUtility.CreateBoxBetween($"SupplyDuct_{name}", air, new Vector3(-Half, DuctBottom, z - SupplyHalfWidth), new Vector3(Half, DuctTop, z + SupplyHalfWidth), SurfaceMaterial.Metal);
                GrayboxUtility.CreateBoxBetween($"SupplyDrop_{name}", air, new Vector3(-0.3f, Containment + RoofPanel, z - 0.2f), new Vector3(0.3f, DuctBottom, z + 0.2f), SurfaceMaterial.Metal);
                foreach (float x in new[] { -4.2f, 1.8f })
                {
                    foreach (var (dz, edge) in new[] { (-SupplyHalfWidth - 0.03f, "A"), (SupplyHalfWidth + 0.03f, "B") })
                    {
                        GrayboxUtility.CreateBoxBetween($"SupplyRod_{name}_{x:0.0}_{edge}", air, new Vector3(x - 0.006f, DuctBottom - 0.02f, z + dz - 0.006f), new Vector3(x + 0.006f, Roof, z + dz + 0.006f), SurfaceMaterial.Metal);
                    }
                    GrayboxUtility.CreateBoxBetween($"SupplyTrapeze_{name}_{x:0.0}", air, new Vector3(x - 0.02f, DuctBottom - 0.04f, z - SupplyHalfWidth - 0.05f), new Vector3(x + 0.02f, DuctBottom - 0.02f, z + SupplyHalfWidth + 0.05f), SurfaceMaterial.Metal);
                }
            }

            // Per pod: a power tray over the outer row and a data tray over the inner row, each with a single drop into
            // the pod, and an inert-gas line with two 360° nozzles over the aisle. All run through the tile edge to edge.
            var hangers = RoomInfrastructure.Every(-5.4f, 5.4f, 1.2f).ToList();
            var power = GrayboxUtility.CreateGroup("Power", dressing);
            var data = GrayboxUtility.CreateGroup("Data", dressing);
            var gas = GrayboxUtility.CreateGroup("GasExtinguishing", dressing);
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                float sign = pod == 0 ? 1f : -1f;
                string name = pod == 0 ? "N" : "S";
                float powerZ = sign * PowerTrayZ;
                float dataZ = sign * DataTrayZ;
                float gasZ = HallContract.PodZ(pod);
                RoomInfrastructure.Tray(power, $"PowerTray_{name}", new Vector3(-Half, 0f, powerZ), new Vector3(Half, 0f, powerZ),
                    TrayWidth, TrayBottom, SurfaceMaterial.Metal, SurfaceMaterial.PowerCable, hangers, Roof);
                GrayboxUtility.CreatePipe($"PowerDrop_{name}", power, new Vector3(-3.0f, Containment + RoofPanel, powerZ), new Vector3(-3.0f, TrayBottom, powerZ), 0.05f, SurfaceMaterial.Trunking);
                RoomInfrastructure.Tray(data, $"DataTray_{name}", new Vector3(-Half, 0f, dataZ), new Vector3(Half, 0f, dataZ),
                    TrayWidth, TrayBottom, SurfaceMaterial.DataTray, SurfaceMaterial.DataCable, hangers, Roof);
                GrayboxUtility.CreatePipe($"DataDrop_{name}", data, new Vector3(3.0f, Containment + RoofPanel, dataZ), new Vector3(3.0f, TrayBottom, dataZ), 0.05f, SurfaceMaterial.DataTray);

                GrayboxUtility.CreatePipe($"GasLine_{name}", gas, new Vector3(-Half, GasY, gasZ), new Vector3(Half, GasY, gasZ), 0.025f, SurfaceMaterial.PipeGas);
                foreach (float x in new[] { -2.1f, 2.1f })
                {
                    GrayboxPropsTool.Place(GrayboxProp.GasNozzle, gas, new Vector3(x, GasY - 0.025f, gasZ), 0f, $"GasNozzle_{name}_{x:0.0}");
                }
                foreach (float x in new[] { -4.8f, -0.6f, 4.8f })
                {
                    GrayboxUtility.CreateBoxBetween($"GasHanger_{name}_{x:0.0}", gas, new Vector3(x - 0.006f, GasY + 0.025f, gasZ - 0.006f), new Vector3(x + 0.006f, Roof, gasZ + 0.006f), SurfaceMaterial.Metal);
                }
            }

            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            foreach (int x in new[] { -1, 1 })
            {
                foreach (int z in new[] { -1, 1 })
                {
                    string corner = $"{(z > 0 ? "N" : "S")}{(x > 0 ? "E" : "W")}";
                    GrayboxPropsTool.Place(GrayboxProp.PendantLight, ceiling, new Vector3(x * WalkwayLightX, Roof, z * WalkwayLightZ), 0f, $"Light_Walkway_{corner}");
                    GrayboxPropsTool.Place(GrayboxProp.PendantLight, ceiling, new Vector3(x * CrossLightX, Roof, z * CrossLightZ), 90f, $"Light_Cross_{corner}");
                }
            }
            foreach (var (x, z) in new[] { (-3.6f, 2.0f), (3.6f, 2.0f), (-3.6f, -2.0f), (3.6f, -2.0f), (-1.2f, 4.8f), (1.2f, -4.8f) })
            {
                GrayboxPropsTool.Place(GrayboxProp.SmokeDetector, ceiling, new Vector3(x, Roof, z), 0f, $"SmokeDetector_{x:0.0}_{z:0.0}");
            }
        }

        // ---------------------------------------------------------------- sides

        private static void BuildSide(Transform root, HallSideKind kind)
        {
            switch (kind)
            {
                case HallSideKind.Open:
                case HallSideKind.Closed:
                    BuildInnerSide(root);
                    break;
                default:
                    BuildPerimeterSide(root, kind == HallSideKind.Door);
                    break;
            }
        }

        /// <summary>
        /// A side towards another hall tile: open floor, the walkways continue into the neighbour. A hall is never
        /// divided, so the (unused) closed side is open too.
        /// </summary>
        private static void BuildInnerSide(Transform root)
        {
            GrayboxUtility.CreateGroup(RoomDressingTool.DressingName, root);
        }

        /// <summary>
        /// The 6 m perimeter wall on the north edge. Cooling air comes from the cooling plant: large supply grilles a
        /// little above the floor and small return grilles under the roof, one above the other. Wall: two pairs of
        /// grilles, the gas release station and a manual call point between them, a wall dome camera and an inert-gas
        /// riser from the floor to the roof. Door: the standard opening with its frame, the automatic sliding door (it
        /// opens into the hall, its reader beside it), an exit sign and a camera over it, the light switch, a pair of
        /// grilles on each side beyond the door pockets, the gas release station and a call point. Only grilles,
        /// readers, switches, call points, the gas riser and cable conduits are on the wall: no air handlers, no
        /// electrical boards, no outlets, no water. Both keep the walkway in front free.
        /// </summary>
        private static void BuildPerimeterSide(Transform root, bool door)
        {
            var structure = GrayboxUtility.CreateGroup("Structure", root);
            float wallZ = RoomContract.WallCenterOffset;
            float thickness = RoomContract.WallThickness;
            float opening = RoomContract.OpeningHeight;
            float cross = RoomContract.OpeningWidth / 2f;
            if (door)
            {
                float segment = Wall - cross;
                foreach (int side in new[] { -1, 1 })
                {
                    GrayboxUtility.CreateStandingBox(side < 0 ? "Wall_Left" : "Wall_Right", structure, new Vector3(side * (cross + segment / 2f), 0f, wallZ),
                        new Vector3(segment, Roof, thickness), SurfaceMaterial.HallWall);
                }
                GrayboxUtility.CreateStandingBox("Lintel", structure, new Vector3(0f, opening, wallZ), new Vector3(2f * cross, Roof - opening, thickness), SurfaceMaterial.HallWall);
            }
            else
            {
                GrayboxUtility.CreateStandingBox("Wall", structure, new Vector3(0f, 0f, wallZ), new Vector3(2f * Wall, Roof, thickness), SurfaceMaterial.HallWall);
            }

            var dressing = GrayboxUtility.CreateGroup(RoomDressingTool.DressingName, root);
            var architecture = GrayboxUtility.CreateGroup("Architecture", dressing);
            const float plinth = 0.15f;
            foreach (var (from, to, name) in PerimeterSegments(door))
            {
                GrayboxUtility.CreateBoxBetween($"Plinth{name}", architecture, new Vector3(from, 0f, Wall - 0.015f), new Vector3(to, plinth, Wall), SurfaceMaterial.Trim);
            }
            // Walkway line in front of the wall equipment; the corner pieces carry it on where the wall continues.
            Marking(architecture, "Marking", new Vector3(-PerimeterLine, 0f, PerimeterLine), new Vector3(PerimeterLine, 0f, PerimeterLine));

            const float awayFromWall = 180f;
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            var cameras = GrayboxUtility.CreateGroup(RoomDressingTool.SecurityCamerasName, root);
            if (door)
            {
                // The door frame's and the door's pivot is the opening pose; the hall owns this door.
                var pose = RoomContract.GetOpeningPose(Direction.North);
                GrayboxPropsTool.Place(GrayboxProp.DoorFrame, architecture, new Vector3(pose.X, 0f, pose.Z), pose.YawDegrees);
                // Its exit sign is on the door's operator, on the hall side: the way out is into the corridor.
                GrayboxPropsTool.Place(GrayboxProp.AutoSlidingDoor, architecture, new Vector3(pose.X, 0f, pose.Z), pose.YawDegrees, "Door");
                PlaceGrilles(air, new[] { -4.2f, 4.2f });
                GrayboxPropsTool.Place(GrayboxProp.GasReleaseStation, safety, new Vector3(3.0f, GrayboxPropsTool.CallPointHeight, Wall), awayFromWall);
                GrayboxPropsTool.Place(GrayboxProp.FireCallPoint, safety, new Vector3(-3.2f, GrayboxPropsTool.CallPointHeight, Wall), awayFromWall);
                GrayboxPropsTool.Place(GrayboxProp.LightSwitch, safety, new Vector3(-2.85f, GrayboxPropsTool.LightSwitchHeight, Wall), awayFromWall, "LightSwitch");
                // The camera over the door watches everyone coming in.
                PlaceWallCamera(cameras, "SecurityWallDome_Door", new Vector3(0f, 3.5f, Wall), awayFromWall);
            }
            else
            {
                PlaceGrilles(air, new[] { -3.0f, 3.0f });
                GrayboxPropsTool.Place(GrayboxProp.GasReleaseStation, safety, new Vector3(0.45f, GrayboxPropsTool.CallPointHeight, Wall), awayFromWall);
                GrayboxPropsTool.Place(GrayboxProp.FireCallPoint, safety, new Vector3(0.85f, GrayboxPropsTool.CallPointHeight, Wall), awayFromWall);
                PlaceWallCamera(cameras, "SecurityWallDome_Wall", new Vector3(0.2f, 4.5f, Wall), awayFromWall);
                // Inert gas from the cylinder room comes up out of the floor and runs to the roof, where the gas lines are.
                var pipes = GrayboxUtility.CreateGroup("Pipes", dressing);
                RoomInfrastructure.Riser(pipes, "GasRiser", new Vector3(-4.4f, 0f, Wall), Vector3.back, 0.025f, SurfaceMaterial.PipeGas, ceiling: Roof);
            }
        }

        /// <summary>
        /// A large square supply grille low on the north wall and a small return grille high above it, at each
        /// <paramref name="xs"/>: cold air in low, warm air out high. Both keep <see cref="GrayboxPropsTool.GrilleGap"/>: the
        /// low one to the floor (on top of the plinth, as high as in the corridors), the high one to the underside of the
        /// girders.
        /// </summary>
        private static void PlaceGrilles(Transform parent, float[] xs)
        {
            foreach (float x in xs)
            {
                string side = x > 0 ? "E" : "W";
                GrayboxPropsTool.Place(GrayboxProp.FloorVentGrille, parent, new Vector3(x, 0f, Wall), 180f, $"SupplyGrille_{side}");
                GrayboxPropsTool.Place(GrayboxProp.WallVentGrille, parent, new Vector3(x, GrayboxPropsTool.HighGrilleCenter(GirderBottom), Wall), 180f, $"ReturnGrille_{side}");
            }
        }

        /// <summary>Stretches of the perimeter wall between the corners (and beside the door).</summary>
        private static IEnumerable<(float from, float to, string name)> PerimeterSegments(bool door)
        {
            float cross = RoomContract.OpeningWidth / 2f;
            if (door)
            {
                yield return (-Wall, -cross - 0.1f, "_W");
                yield return (cross + 0.1f, Wall, "_E");
            }
            else
            {
                yield return (-Wall, Wall, string.Empty);
            }
        }

        /// <summary>A wall dome camera on a wall or column with its cable conduit up to the roof.</summary>
        private static void PlaceWallCamera(Transform parent, string name, Vector3 position, float yaw)
        {
            var camera = GrayboxPropsTool.Place(GrayboxProp.SecurityWallDome, parent, position, yaw, name);
            var cableStart = camera.localPosition + Vector3.up * GrayboxPropsTool.WallDomeCableHeight + camera.localRotation * new Vector3(0f, 0f, 0.012f);
            RoomInfrastructure.Conduit(parent, $"{name}_Conduit", cableStart, Roof);
        }

        // ---------------------------------------------------------------- corners

        /// <summary>
        /// The north-east corner with a quarter of the 0.6 m building column on the cell corner (the four tiles around
        /// it make one column; along the perimeter it is a pilaster in the wall). Where the perimeter wall continues
        /// past the corner the walkway line goes on with it; a free-standing (inner) column carries a wall dome camera.
        /// </summary>
        private static void BuildCorner(Transform root, HallCornerKind kind)
        {
            var structure = GrayboxUtility.CreateGroup("Structure", root);
            float column = HallContract.ColumnSize / 2f;
            GrayboxUtility.CreateStandingBox("Column", structure, new Vector3(Half - column / 2f, 0f, Half - column / 2f), new Vector3(column, Roof, column), SurfaceMaterial.Steel);

            var dressing = GrayboxUtility.CreateGroup(RoomDressingTool.DressingName, root);
            var lanes = GrayboxUtility.CreateGroup("Lanes", dressing);
            // EdgeFirst: the north side is perimeter and its wall goes on east; EdgeSecond: the east wall goes on north.
            // Concave: the neighbours' walls meet here, so both lines turn this corner.
            if (kind == HallCornerKind.EdgeFirst || kind == HallCornerKind.Concave)
            {
                Marking(lanes, "Marking_N", new Vector3(PerimeterLine, 0f, PerimeterLine), new Vector3(Half, 0f, PerimeterLine));
            }
            if (kind == HallCornerKind.EdgeSecond || kind == HallCornerKind.Concave)
            {
                Marking(lanes, "Marking_E", new Vector3(PerimeterLine, 0f, PerimeterLine), new Vector3(PerimeterLine, 0f, Half));
            }

            if (kind == HallCornerKind.Outer)
            {
                // Data cables come up out of the floor in the corner of the hall and rise to the trays under the roof.
                var services = GrayboxUtility.CreateGroup("Power", dressing);
                RoomInfrastructure.TrayRiser(services, "CableRiser", new Vector3(Half - HallContract.ColumnSize / 2f - 0.5f, 0f, Wall - 0.04f), Vector3.right, 0.3f,
                    0f, Roof, SurfaceMaterial.DataTray, SurfaceMaterial.DataCable);
            }

            if (kind == HallCornerKind.Inner)
            {
                var cameras = GrayboxUtility.CreateGroup(RoomDressingTool.SecurityCamerasName, root);
                PlaceWallCamera(cameras, "SecurityWallDome_Column", new Vector3(Half - column, 3.5f, Half - column / 2f), 270f);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static void AddRackSlots(Transform root, SlotPose[] poses)
        {
            var group = GrayboxUtility.CreateGroup("RackSlots", root);
            for (int i = 0; i < poses.Length; i++)
            {
                var slot = new GameObject($"RackSlot_{i:00}", typeof(RackSlot)).transform;
                slot.SetParent(group, false);
                RoomShellTool.ApplyPose(slot, poses[i]);
            }
        }

        /// <summary>A painted floor line between two points on the floor that differ along one axis.</summary>
        private static void Marking(Transform parent, string name, Vector3 from, Vector3 to)
        {
            var across = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.z - from.z) ? Vector3.forward : Vector3.right;
            GrayboxUtility.CreateBoxBetween(name, parent, from - across * (MarkingWidth / 2f), to + across * (MarkingWidth / 2f) + Vector3.up * 0.002f, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>A steel I-beam along X: two flanges and a web.</summary>
        private static void IBeamAlongX(Transform parent, string name, float z, float fromX, float toX, float bottom, float height, float flangeWidth)
        {
            var beam = GrayboxUtility.CreateGroup(name, parent);
            const float flange = 0.014f;
            const float web = 0.009f;
            float half = flangeWidth / 2f;
            GrayboxUtility.CreateBoxBetween("Flange_Bottom", beam, new Vector3(fromX, bottom, z - half), new Vector3(toX, bottom + flange, z + half), SurfaceMaterial.Steel);
            GrayboxUtility.CreateBoxBetween("Flange_Top", beam, new Vector3(fromX, bottom + height - flange, z - half), new Vector3(toX, bottom + height, z + half), SurfaceMaterial.Steel);
            GrayboxUtility.CreateBoxBetween("Web", beam, new Vector3(fromX, bottom + flange, z - web / 2f), new Vector3(toX, bottom + height - flange, z + web / 2f), SurfaceMaterial.Steel);
        }
    }
}
