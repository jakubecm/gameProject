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
    ///   solid black containment roof on the racks, automatic doors at both ends, LED strips inside), outlined in yellow;
    ///   the rack backs of both pods face the hot aisle in the middle. Overhead, all above the hanging sign: the return
    ///   air duct over the hot aisle with grilles in its underside, over every pod a supply duct from the cooling plant
    ///   with a drop into the containment roof (every aisle is connected to the central HVAC), a power tray over the
    ///   outer row and a data tray over the inner row with one drop each into the pod, an inert-gas line with nozzles,
    ///   smoke detectors, pendant LED high-bays over the walkways, two pendant CCTV domes and the tile sign with its
    ///   code ("B3");
    /// - <c>Hall/HallSide_&lt;Kind&gt;.prefab</c> per <see cref="HallSideKind"/>, authored for the north side: nothing
    ///   (Open, and the unused Closed: a hall is never divided), or the 6 m perimeter wall (Door with the opening, an
    ///   automatic door and a light switch, Wall without) with a manual call point and nothing else (no grilles, no
    ///   camera); a plain wall also carries an inert-gas riser from the floor to the roof;
    /// - <c>Hall/HallCorner_&lt;Kind&gt;.prefab</c> per <see cref="HallCornerKind"/>, authored for the north-east corner:
    ///   a quarter of the building column and the pieces of the walkway line that turn there;
    /// - <c>Data/HallKit.asset</c> pointing at all pieces, and <c>Data/RoomDefinition_Hall.asset</c>.
    /// The hall holds infrastructure only: no plant (heat exchangers are in the cooling plant, transformers and
    /// switchgear in the power room), no water (no sprinklers, hose cabinets or extinguishers; inert gas only), no
    /// outlets and no partitions at all. Every overhead run continues in the neighbouring tile (all tiles
    /// share the same grid) or ends in the perimeter wall, so tiles stay independent modules. Nothing below 2.6 m stands
    /// in the walkways around the pods; the hall tests check that.
    /// <see cref="CreateHallKit"/> only builds what is missing; Create Preview Scene replaces every piece (same GUIDs).
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
        private const float GirderBottom = 5.2f;
        private const float BeamBottom = 5.6f;
        private const float MarkingWidth = 0.05f;

        /// <summary>Distance of the hanging cameras from the middle of an edge, along it: the beam intersections nearest the columns.</summary>
        private const float CameraOffset = 3.0f;
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

        public static void CreateHallKit() => Build(false);

        /// <summary>Builds the kit; <paramref name="replace"/> replaces every piece in place (same GUIDs), without asking.</summary>
        internal static void Build(bool replace)
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
            // The ceiling is the underside of a flat cast-concrete slab (the hall is underground), with the look of 600 mm perforated concrete tiles: a texture.
            GrayboxUtility.CreateBox("RoofDeck", structure, new Vector3(0f, Roof + HallContract.RoofThickness / 2f, 0f),
                new Vector3(RoomContract.CellSize, HallContract.RoofThickness, RoomContract.CellSize), SurfaceMaterial.Concrete);
            var ceilingFace = new GrayboxMeshBuilder();
            float tiles = RoomContract.CellSize / 0.6f;
            ceilingFace.AddPanel(new Vector3(-Half, Roof - 0.001f, -Half), new Vector3(Half, Roof - 0.001f, -Half), new Vector3(Half, Roof - 0.001f, Half), new Vector3(-Half, Roof - 0.001f, Half),
                Vector3.down, Vector2.zero, new Vector2(tiles, 0f), new Vector2(tiles, tiles), new Vector2(0f, tiles));
            ceilingFace.Attach("CeilingTiles", structure, SurfaceMaterial.CeilingTile600);

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

            // (The hanging cameras are on the open sides' edges, see BuildInnerSide: no camera hangs in the middle of a tile.)
            GrayboxUtility.CreateGroup(RoomDressingTool.SecurityCamerasName, root);

            var signs = GrayboxUtility.CreateGroup("Signs", root);
            // Over the middle of the hot aisle, seen from both cross walkways; its rods pass beside the return duct.
            GrayboxPropsTool.Place(GrayboxProp.HallSignCube, signs, new Vector3(0f, Roof, 0f), 0f, "TileSign");
        }

        /// <summary>
        /// One pod (0 = north, 1 = south), after a product photo of an aisle containment: two rows of fourteen racks facing each other across the cold aisle,
        /// closed by a roof of black panels that lies directly on the rack tops (no gap, no fascia) with a strip of clear panels over the aisle, and a
        /// black-framed glass sliding door (<see cref="GrayboxProp.AisleDoor"/>) at each end. The floor of the aisle is perforated tiles only (cold air comes up
        /// from the raised floor), lit by two LED strips under the clear roof. A yellow outline around the pod and pedestrian crossings in front of both doors.
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

            BuildContainmentRoof(pod, center, name);
            foreach (int side in new[] { -1, 1 })
            {
                string edge = side > 0 ? "E" : "W";
                GrayboxPropsTool.Place(GrayboxProp.AisleDoor, pod, new Vector3(side * PodX, 0f, center), side > 0 ? 90f : 270f, $"AisleDoor_{edge}");
                GrayboxPropsTool.Place(GrayboxProp.AisleLight, pod, new Vector3(side * PodX / 2f, Containment, center), 90f, $"AisleLight_{edge}");
            }

            // Yellow outline 0.3 m around the pod, one closed rectangle (no pedestrian crossings).
            var lines = GrayboxUtility.CreateGroup("Markings", pod);
            float outX = PodX + HallContract.PodOutline;
            float outZ = PodZ + HallContract.PodOutline;
            Marking(lines, "Outline_N", new Vector3(-outX, 0f, center + outZ), new Vector3(outX, 0f, center + outZ));
            Marking(lines, "Outline_S", new Vector3(-outX, 0f, center - outZ), new Vector3(outX, 0f, center - outZ));
            Marking(lines, "Outline_E", new Vector3(outX, 0f, center - outZ), new Vector3(outX, 0f, center + outZ));
            Marking(lines, "Outline_W", new Vector3(-outX, 0f, center - outZ), new Vector3(-outX, 0f, center + outZ));
        }

        private static void HallBox(GrayboxMeshBuilder builder, Vector3 a, Vector3 b) => builder.AddBox(Vector3.Min(a, b), Vector3.Max(a, b));

        /// <summary>
        /// The roof of a contained aisle, lying on the rack tops: 0.6 m black panels over both rack rows (a hairline gap between them) and, over the
        /// aisle, a black frame with clear panels (two solid ones where the supply duct lands, two white labels on the clear ones).
        /// </summary>
        private static void BuildContainmentRoof(Transform pod, float center, string name)
        {
            var roof = GrayboxUtility.CreateGroup("ContainmentRoof", pod);
            float y0 = Containment;
            float y1 = Containment + HallContract.ContainmentPanel;
            float aisle = HallContract.AisleWidth / 2f;
            const float module = 0.6f;
            var black = new GrayboxMeshBuilder();
            var frame = new GrayboxMeshBuilder();
            var labels = new GrayboxMeshBuilder();

            for (int i = 0; i < HallContract.RacksPerRow; i++)
            {
                float x0 = -PodX + i * module;
                foreach (int side in new[] { -1, 1 })
                {
                    HallBox(black, new Vector3(x0 + 0.0015f, y0, center + side * aisle), new Vector3(x0 + module - 0.0015f, y1, center + side * PodZ));
                }
                // The aisle roof is solid: black panels over the aisle too (a white label on two of them).
                HallBox(black, new Vector3(x0 + 0.0015f, y0, center - aisle), new Vector3(x0 + module - 0.0015f, y1, center + aisle));
                if (i == 2 || i == 11)
                {
                    labels.AddPanel(new Vector3(x0 + 0.12f, y1 + 0.0008f, center - 0.15f), new Vector3(x0 + 0.42f, y1 + 0.0008f, center - 0.15f),
                        new Vector3(x0 + 0.42f, y1 + 0.0008f, center + 0.05f), new Vector3(x0 + 0.12f, y1 + 0.0008f, center + 0.05f), Vector3.up,
                        Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                }
            }
            // Joint rails over the aisle edges and a cross bar at every module (the panel seams).
            foreach (int side in new[] { -1, 1 })
            {
                HallBox(frame, new Vector3(-PodX, y0, center + side * aisle - (side > 0 ? 0.03f : -0.03f)), new Vector3(PodX, y1 + 0.002f, center + side * aisle));
            }
            for (int i = 0; i <= HallContract.RacksPerRow; i++)
            {
                float x = -PodX + i * module;
                HallBox(frame, new Vector3(x - 0.012f, y0, center - aisle), new Vector3(x + 0.012f, y1 + 0.002f, center + aisle));
            }

            black.Attach($"ContainmentRoof_{name}_Panels", roof, SurfaceMaterial.RackBlack);
            frame.Attach($"ContainmentRoof_{name}_Frame", roof, SurfaceMaterial.ServerBlack);
            labels.Attach($"ContainmentRoof_{name}_Labels", roof, SurfaceMaterial.DetectorWhite);
        }

        private static void BuildTileServices(Transform dressing)
        {
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            float ductMid = (DuctBottom + DuctTop) / 2f;
            // Return air duct along the hot aisle (east-west), flanged aluminium duct like the cooling plant's, continuing into the neighbours;
            // louvred grilles in its underside take the hot air from the rack backs back to the cooling plant.
            InfrastructureModels.RectDuct(air, "ReturnDuct", new Vector3(-Half, ductMid, 0f), new Vector3(Half, ductMid, 0f), DuctHalfWidth, (DuctTop - DuctBottom) / 2f, 3f,
                new[] { -5.25f, -2.25f, 2.25f, 5.25f }, Roof, SurfaceMaterial.DuctStainless);
            var grilles = new GrayboxMeshBuilder();
            foreach (float x in new[] { -4.5f, -1.5f, 1.5f, 4.5f })
            {
                InfrastructureModels.DuctGrille(grilles, new Vector3(x, DuctBottom, 0f));
            }
            grilles.Attach("ReturnGrilles", air, SurfaceMaterial.DetectorWhite);

            // Every pod is connected to the cooling plant: a supply duct along the pod (its joints are 4 m apart, none where the drop leaves it), in brushed stainless like a passenger lift (not a pipe colour),
            // a take-off collar and a rigid drop, a black flexible connector between flanges, and a rigid boot into a bolted collar frame on the aisle roof.
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                float z = HallContract.PodZ(pod);
                string name = pod == 0 ? "N" : "S";
                InfrastructureModels.RectDuct(air, $"SupplyDuct_{name}", new Vector3(-Half, ductMid, z), new Vector3(Half, ductMid, z), SupplyHalfWidth, (DuctTop - DuctBottom) / 2f, 4f,
                    new[] { -5.25f, -2.25f, 2.25f, 5.25f }, Roof, SurfaceMaterial.DuctStainless);
                var drop = GrayboxUtility.CreateGroup($"SupplyDrop_{name}", air);
                float roofTop = Containment + HallContract.ContainmentPanel;
                InfrastructureModels.CollarFrame(drop, $"SupplyDrop_{name}_TakeOff", new Vector3(0f, DuctBottom - 0.03f, z), 0.3f, 0.2f, 0.03f);
                InfrastructureModels.RectDuct(drop, $"SupplyDrop_{name}_Duct", new Vector3(0f, DuctBottom - 0.03f, z), new Vector3(0f, 2.75f, z), 0.3f, 0.2f, 1.2f, null, Roof, SurfaceMaterial.DuctStainless);
                InfrastructureModels.Bellows(drop, $"SupplyDrop_{name}_Flex", new Vector3(0f, 2.75f, z), new Vector3(0f, 2.3f, z), 0.3f, 0.2f);
                InfrastructureModels.RectDuct(drop, $"SupplyDrop_{name}_Boot", new Vector3(0f, 2.3f, z), new Vector3(0f, roofTop + 0.03f, z), 0.3f, 0.2f, 1.0f, null, Roof, SurfaceMaterial.DuctStainless);
                InfrastructureModels.CollarFrame(drop, $"SupplyDrop_{name}_RoofCollar", new Vector3(0f, roofTop, z), 0.3f, 0.2f, 0.03f);
            }

            // Per pod: a power tray over the outer row and a data tray over the inner row, each with a single drop into the pod, and an inert-gas line
            // with two 360° nozzles over the aisle. All run through the tile edge to edge.
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
                    TrayWidth, TrayBottom, SurfaceMaterial.Metal, SurfaceMaterial.HoseRed, hangers, Roof);
                TrayDrop(power, $"PowerDrop_{name}", new Vector3(-3.0f, 0f, powerZ), SurfaceMaterial.ConduitBrown);
                RoomInfrastructure.Tray(data, $"DataTray_{name}", new Vector3(-Half, 0f, dataZ), new Vector3(Half, 0f, dataZ),
                    TrayWidth, TrayBottom, SurfaceMaterial.Metal, SurfaceMaterial.DataCable, hangers, Roof);
                TrayDrop(data, $"DataDrop_{name}", new Vector3(3.0f, 0f, dataZ), SurfaceMaterial.ConduitGreen);

                RoomPipes.Pipe(gas, $"GasLine_{name}", new Vector3(-Half, GasY, gasZ), new Vector3(Half, GasY, gasZ), RoomPipes.NarrowRadius, SurfaceMaterial.PipeGas);
                foreach (float x in new[] { -2.1f, 2.1f })
                {
                    GrayboxPropsTool.Place(GrayboxProp.GasNozzle, gas, new Vector3(x, GasY - RoomPipes.NarrowRadius, gasZ), 0f, $"GasNozzle_{name}_{x:0.0}");
                }
                var gasHangers = new GrayboxMeshBuilder();
                foreach (float x in new[] { -4.8f, -0.6f, 4.8f })
                {
                    InfrastructureModels.PipeHanger(gasHangers, new Vector3(x, GasY, gasZ), RoomPipes.NarrowRadius, Vector3.right, Roof);
                }
                gasHangers.Attach($"GasHangers_{name}", gas, SurfaceMaterial.GalvanisedSteel);
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

        /// <summary>
        /// A single drop from a tray into the aisle roof: a rigid conduit clamped every metre, a gland on the tray end and a junction box on the roof panel.
        /// </summary>
        private static void TrayDrop(Transform parent, string name, Vector3 at, SurfaceMaterial conduit)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            float roofTop = Containment + HallContract.ContainmentPanel;
            float junction = 0.1f;
            GrayboxUtility.CreateBox("JunctionBox", group, new Vector3(at.x, roofTop + junction / 2f, at.z), new Vector3(0.2f, junction, 0.15f), SurfaceMaterial.ElectricGrey, false);
            // The mesh asset is named after the prefab and this name, so every drop needs its own (a shared "Conduit" made all four draw the last one).
            RoomPipes.Pipe(group, $"{name}_Conduit", new Vector3(at.x, roofTop + junction, at.z), new Vector3(at.x, TrayBottom + 0.004f, at.z), 0.028f, conduit);
            GrayboxUtility.CreateCylinder("Gland", group, new Vector3(at.x, TrayBottom + 0.004f, at.z), 0.04f, 0.03f, SurfaceMaterial.Metal);
            for (float y = Mathf.Ceil((roofTop + junction + 0.4f) * 2f) / 2f; y < TrayBottom - 0.3f; y += 1f)
            {
                GrayboxUtility.CreateCylinder($"Clamp_{y:0.0}", group, new Vector3(at.x, y, at.z), 0.04f, 0.02f, SurfaceMaterial.Metal);
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
            // Two hanging cameras on the girder of this edge, 3 m either side of its middle: where the girder meets the small beams z = +-3 / x = +-3, the intersections
            // nearest the columns on the cell corners, in the axis of the street between two server sections, clear of every duct, tray and pipe. Two neighbouring
            // tiles share the edge and the beams only cross it on the east and west edges, so HallKit keeps the group on the east side only.
            var cameras = GrayboxUtility.CreateGroup(RoomDressingTool.SecurityCamerasName, root);
            var hanging = GrayboxUtility.CreateGroup(HallKit.HangingCameraName, cameras);
            foreach (float a in new[] { -CameraOffset, CameraOffset })
            {
                GrayboxPropsTool.Place(GrayboxProp.SecurityPendant, hanging, new Vector3(a, GirderBottom, Half), 90f, $"SecurityPendant_{(a < 0 ? "A" : "B")}");
            }
        }

        /// <summary>
        /// The 6 m perimeter wall on the north edge, with its plinth and the walkway line in front. Wall: a manual call point and an
        /// inert-gas riser from the floor to the roof. Door: the standard opening with its frame, the automatic sliding door (owned by
        /// the hall, its readers and exit sign on it), a call point and the light switch. No grilles, cameras, boards, outlets or water.
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
            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            // The hall's cameras only hang from the girders (BuildInnerSide); the perimeter walls carry none.
            if (door)
            {
                // The door frame's and the door's pivot is the opening pose; the hall owns this door.
                var pose = RoomContract.GetOpeningPose(Direction.North);
                GrayboxPropsTool.Place(GrayboxProp.DoorFrame, architecture, new Vector3(pose.X, 0f, pose.Z), pose.YawDegrees);
                // Its exit sign is on the door's operator, on the hall side: the way out is into the corridor.
                GrayboxPropsTool.Place(GrayboxProp.AutoSlidingDoor, architecture, new Vector3(pose.X, 0f, pose.Z), pose.YawDegrees, "Door");
                GrayboxPropsTool.Place(GrayboxProp.FireCallPoint, safety, new Vector3(-3.2f, GrayboxPropsTool.CallPointHeight, Wall), awayFromWall);
                GrayboxPropsTool.Place(GrayboxProp.LightSwitch, safety, new Vector3(-2.85f, GrayboxPropsTool.LightSwitchHeight, Wall), awayFromWall, "LightSwitch");
            }
            else
            {
                GrayboxPropsTool.Place(GrayboxProp.FireCallPoint, safety, new Vector3(0.85f, GrayboxPropsTool.CallPointHeight, Wall), awayFromWall);
                // Inert gas from the cylinder room comes up out of the floor and runs to the roof, where the gas lines are.
                var pipes = GrayboxUtility.CreateGroup("Pipes", dressing);
                RoomInfrastructure.Riser(pipes, "GasRiser", new Vector3(-4.4f, 0f, Wall), Vector3.back, RoomPipes.NarrowRadius, SurfaceMaterial.PipeGas, 1.2f, Roof);
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

        // ---------------------------------------------------------------- corners

        /// <summary>
        /// The north-east corner with a quarter of the 0.6 m building column on the cell corner (the four tiles around
        /// it make one column; along the perimeter it is a pilaster in the wall). Where the perimeter wall continues
        /// past the corner the walkway line goes on with it; no column carries a camera.
        /// </summary>
        private static void BuildCorner(Transform root, HallCornerKind kind)
        {
            var structure = GrayboxUtility.CreateGroup("Structure", root);
            float column = HallContract.ColumnSize / 2f;
            GrayboxUtility.CreateStandingBox("Column", structure, new Vector3(Half - column / 2f, 0f, Half - column / 2f), new Vector3(column, Roof, column), SurfaceMaterial.Steel);
            // A cap plate under the roof with its bolts (this tile's quarter); the column stands straight on the floor, no base plate or edge.
            var plates = new GrayboxMeshBuilder();
            float plate = column + 0.1f;
            HallBox(plates, new Vector3(Half - plate, Roof - 0.025f, Half - plate), new Vector3(Half, Roof, Half));
            plates.AddLathe(new[] { GrayboxMeshBuilder.P(0.016f, 0f, true), GrayboxMeshBuilder.P(0.016f, 0.016f, true), GrayboxMeshBuilder.P(0.0001f, 0.016f, true) }, 6, false,
                new Vector3(Half - 0.35f, Roof - 0.025f, Half - 0.35f), Quaternion.Euler(180f, 0f, 0f));
            plates.Attach("Column_Plates", structure, SurfaceMaterial.Steel);

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

            // No camera on any column.
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
            var direction = (to - from).normalized;
            var across = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.z - from.z) ? Vector3.forward : Vector3.right;
            // Each line runs half its width past both ends, so two lines that meet at a corner or a T make one closed, square-cornered line; never past the cell.
            var a = from - direction * (MarkingWidth / 2f) - across * (MarkingWidth / 2f);
            var b = to + direction * (MarkingWidth / 2f) + across * (MarkingWidth / 2f);
            a = new Vector3(Mathf.Clamp(a.x, -Half, Half), 0f, Mathf.Clamp(a.z, -Half, Half));
            b = new Vector3(Mathf.Clamp(b.x, -Half, Half), 0.002f, Mathf.Clamp(b.z, -Half, Half));
            GrayboxUtility.CreateBoxBetween(name, parent, a, b, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>A steel I-beam along X: two flanges and a web.</summary>
        private static void IBeamAlongX(Transform parent, string name, float z, float fromX, float toX, float bottom, float height, float flangeWidth)
        {
            var beam = GrayboxUtility.CreateGroup(name, parent);
            const float flange = 0.014f;
            const float web = 0.009f;
            float half = flangeWidth / 2f;
            var steel = new GrayboxMeshBuilder();
            HallBox(steel, new Vector3(fromX, bottom, z - half), new Vector3(toX, bottom + flange, z + half));
            HallBox(steel, new Vector3(fromX, bottom + height - flange, z - half), new Vector3(toX, bottom + height, z + half));
            HallBox(steel, new Vector3(fromX, bottom + flange, z - web / 2f), new Vector3(toX, bottom + height - flange, z + web / 2f));
            // Web-to-flange welds (a small fillet each side), a pair of stiffener plates every 1.5 m and bolted splice plates at both ends.
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float y in new[] { bottom + flange, bottom + height - flange - 0.008f })
                {
                    HallBox(steel, new Vector3(fromX, y, z + side * web / 2f), new Vector3(toX, y + 0.008f, z + side * (web / 2f + 0.008f)));
                }
                for (float x = fromX + 0.75f; x < toX; x += 1.5f)
                {
                    HallBox(steel, new Vector3(x - 0.005f, bottom + flange, z + side * web / 2f), new Vector3(x + 0.005f, bottom + height - flange, z + side * (half - 0.01f)));
                }
                foreach (float end in new[] { fromX + 0.25f, toX - 0.25f })
                {
                    HallBox(steel, new Vector3(end - 0.14f, bottom + flange + 0.01f, z + side * web / 2f), new Vector3(end + 0.14f, bottom + height - flange - 0.01f, z + side * (web / 2f + 0.008f)));
                    foreach (float bx in new[] { end - 0.09f, end, end + 0.09f })
                    {
                        foreach (float by in new[] { bottom + height * 0.3f, bottom + height * 0.7f })
                        {
                            steel.AddLathe(new[] { GrayboxMeshBuilder.P(0.009f, 0f, true), GrayboxMeshBuilder.P(0.009f, 0.006f, true), GrayboxMeshBuilder.P(0.0001f, 0.006f, true) }, 6, false,
                                new Vector3(bx, by, z + side * (web / 2f + 0.008f)), Quaternion.FromToRotation(Vector3.up, Vector3.forward * side));
                        }
                    }
                }
            }
            steel.Attach($"{name}_Steel", beam, SurfaceMaterial.Steel);
        }
    }
}
