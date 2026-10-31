using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>
    /// Assembles hall tiles from the real kit (built by Tools/Cold Shutdown/Create Preview Scene) the way the level builder
    /// does, and checks them against the hall contract: walkways stay free, rack slots stay usable, everything
    /// stays inside the cell, signs carry the right codes.
    /// </summary>
    public class HallKitTests
    {
        private const float Epsilon = 0.001f;
        private const string TilePath = "Assets/_Project/Prefabs/Rooms/Hall/HallTile.prefab";
        private const string HallPath = "Assets/_Project/Prefabs/Rooms/Room_Hall.prefab";
        private const string KitPath = "Assets/_Project/Data/HallKit.asset";

        /// <summary>Walkways reach from just above the floor (painted lines are allowed) up to the opening height.</summary>
        private const float WalkFloor = 0.01f;

        /// <summary>The aisle doors (leaves, track, readers) stand on the pod's ends, so walkways are checked from 25 cm out.</summary>
        private const float DoorMargin = 0.25f;

        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var gameObject in created)
            {
                Object.DestroyImmediate(gameObject);
            }
            created.Clear();
        }

        [Test]
        public void Kit_HasEveryPieceAndTheHallDefinition()
        {
            var kit = AssetDatabase.LoadAssetAtPath<HallKit>(KitPath);
            Assert.IsNotNull(kit, $"{KitPath} is missing. Run Tools/Cold Shutdown/Create Preview Scene.");
            Assert.IsTrue(kit.IsComplete, "The hall kit is missing a piece");

            var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>("Assets/_Project/Data/RoomDefinition_Hall.asset");
            Assert.IsNotNull(definition);
            // The hall is one room: the definition points at the hall baked for the world map, made of tile instances.
            var hall = AssetDatabase.LoadAssetAtPath<GameObject>(HallPath);
            Assert.IsNotNull(hall, $"{HallPath} is missing. Run Tools/Cold Shutdown/Create Preview Scene.");
            Assert.AreEqual(hall, definition.Prefab);
            var tile = AssetDatabase.LoadAssetAtPath<GameObject>(TilePath);
            Assert.IsNotNull(tile, $"{TilePath} is missing");
            foreach (var instance in hall.GetComponentsInChildren<Transform>(true).Where(t => t.name == "HallTile"))
            {
                Assert.AreEqual(tile, PrefabUtility.GetCorrespondingObjectFromSource(instance.gameObject), "A hall cell is not an instance of the tile");
            }
        }

        [Test]
        public void InnerTile_KeepsTheWalkwaysAroundThePodsFree()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            float column = RoomContract.CellHalfSize - HallContract.ColumnSize / 2f;
            float end = HallContract.PodHalfLength + DoorMargin;
            float back = HallContract.PodCenterZ - HallContract.PodHalfDepth;
            AssertFree(cell, new[]
            {
                Box(-column, HallContract.PodOuterZ, column, RoomContract.CellHalfSize),
                Box(-column, -RoomContract.CellHalfSize, column, -HallContract.PodOuterZ),
                Box(end, -column, RoomContract.CellHalfSize, column),
                Box(-RoomContract.CellHalfSize, -column, -end, column),
            }, "walkway around the pods");
            AssertFree(cell, new[] { Box(-column, -back, column, back) }, "hot aisle");
        }

        [Test]
        public void Pods_AislesAreContainedAndHaveOnlyPerforatedTilesOnTheirFloor()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var tile = cell.transform.Find("HallTile");
            float aisle = HallContract.AisleWidth / 2f;
            var vents = PropInstances(tile.gameObject, "PerforatedFloorTile").ToList();
            Assert.AreEqual(HallContract.PodsPerTile * HallContract.RacksPerRow * 2, vents.Count, "The aisle floors are not all perforated tiles");
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                float center = HallContract.PodZ(pod);
                var inside = new Bounds();
                inside.SetMinMax(new Vector3(-HallContract.PodHalfLength, WalkFloor, center - aisle), new Vector3(HallContract.PodHalfLength, RoomContract.RackHeight - 0.08f, center + aisle));
                // (The LED strips hang 5 cm under the roof panels, which lie on the rack tops.)
                AssertFree(cell, new[] { inside }, $"cold aisle {pod}");
                Assert.AreEqual(HallContract.RacksPerRow * 2, vents.Count(v => Mathf.Abs(v.transform.localPosition.z - center) < aisle &&
                                                                               Mathf.Abs(v.transform.localPosition.x) < HallContract.PodHalfLength));

                var roof = tile.Find($"Dressing/Pod_{(pod == 0 ? "N" : "S")}/ContainmentRoof");
                Assert.IsNotNull(roof, $"Aisle {pod} has no roof");
                // The roof lies directly on the rack tops: no gap, no fascia.
                Assert.AreEqual(HallContract.ContainmentHeight, CombinedBounds(roof, _ => false).min.y, Epsilon);
                Assert.IsNull(roof.parent.Find("Fascia_N"), "The aisle still has a fascia");
            }

            var doors = tile.GetComponentsInChildren<AutoSlidingDoor>();
            Assert.AreEqual(2 * HallContract.PodsPerTile, doors.Length, "Both ends of every aisle need an automatic door");
            Assert.AreEqual(doors.Length, tile.GetComponentsInChildren<AisleDoor>().Length, "Aisle doors are their own type, apart from the room doors");
            foreach (var door in doors)
            {
                Assert.AreEqual(HallContract.PodHalfLength, Mathf.Abs(door.transform.localPosition.x), Epsilon);
                Assert.IsFalse(door.IsLocked, "Preview doors are unlocked");
            }
        }

        [Test]
        public void InnerTile_HasAllRackSlotsAndLeavesThemFree()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            Assert.AreEqual(HallContract.TileRackSlots, cell.GetComponentsInChildren<RackSlot>().Length);
            AssertRackSlotsFree(cell);
        }

        [Test]
        public void HallTiles_AreNeverDivided()
        {
            // Two hall tiles the map does not connect are still one open hall: nothing stands between them.
            var map = new MapData(2, 1);
            var cell = Assemble(map, (c, r) => true, 0, 0, out var plan);
            Assert.AreEqual(HallSideKind.Open, plan.Side(Direction.East));
            Assert.IsEmpty(cell.transform.Find("HallSide_East").GetComponentsInChildren<Renderer>(), "Something divides the hall");
            var kit = AssetDatabase.LoadAssetAtPath<HallKit>(KitPath);
            Assert.IsEmpty(kit.SidePrefab(HallSideKind.Closed).GetComponentsInChildren<Renderer>(), "The unused closed side is not open");
            AssertRackSlotsFree(cell);
        }

        [Test]
        public void Hall_IsNeverDividedByGlass()
        {
            var cell = Assemble(new MapData(2, 1), (c, r) => true, 0, 0, out _);
            foreach (var renderer in cell.GetComponentsInChildren<Renderer>())
            {
                // Only the glass leaves of the automatic doors and the clear panels of the aisle roofs (horizontal, not partitions) may be glass;
                // small covers (a call point's) are no partition.
                if (renderer.GetComponentInParent<AutoSlidingDoor>() != null || renderer.transform.parent.name == "ContainmentRoof") continue;
                if (Mathf.Max(renderer.bounds.size.x, renderer.bounds.size.z) < 1f) continue;
                foreach (var material in renderer.sharedMaterials)
                {
                    Assert.IsFalse(material != null && (material.name == "M_Glass" || material.name == "M_WireMesh"), $"{renderer.transform.parent.name}/{renderer.name} is see-through");
                }
            }
        }

        [Test]
        public void LoneTile_KeepsAWalkwayAlongEveryWall()
        {
            var cell = Assemble(new MapData(1, 1), (c, r) => true, 0, 0, out var plan);
            Assert.AreEqual(HallSideKind.Wall, plan.Side(Direction.North));
            Assert.AreEqual(HallCornerKind.Outer, plan.Corner(HallCorner.NorthEast));

            // In front of the wall equipment (grilles, call points and the gas station stand at most 0.1 m deep).
            float front = HallContract.WallInnerOffset - 0.1f;
            float end = HallContract.PodHalfLength + DoorMargin;
            AssertFree(cell, new[]
            {
                Box(-front, HallContract.PodOuterZ, front, front),
                Box(-front, -front, front, -HallContract.PodOuterZ),
                Box(end, -front, front, front),
                Box(-front, -front, -end, front),
            }, "perimeter walkway");
            Assert.AreEqual(HallContract.TileRackSlots, cell.GetComponentsInChildren<RackSlot>().Length);
        }

        [Test]
        public void PerimeterWalls_HaveNoVentilationGrilles()
        {
            var cell = Assemble(new MapData(1, 1), (c, r) => true, 0, 0, out _);
            foreach (var direction in new[] { "North", "East", "South", "West" })
            {
                var side = cell.transform.Find($"HallSide_{direction}").gameObject;
                Assert.IsEmpty(PropInstances(side, "FloorVentGrille"), $"{direction} wall has supply grilles");
                Assert.IsEmpty(PropInstances(side, "WallVentGrille"), $"{direction} wall has return grilles");
            }
        }

        [Test]
        public void Cameras_OnlyHang_NoneOnWallsOrColumns()
        {
            // Walls (with or without a door) and columns carry no camera: a lone tile has four walls and no open edge.
            var lone = Assemble(new MapData(1, 1), (c, r) => true, 0, 0, out _);
            Assert.IsEmpty(lone.GetComponentsInChildren<SecurityCamera>(), "A camera on a wall or a column");
            var map = new MapData(2, 1);
            map.Connect(0, 0, Direction.East);
            var withDoor = Assemble(map, (c, r) => c == 0, 0, 0, out _);
            Assert.IsEmpty(withDoor.transform.Find("HallSide_East").GetComponentsInChildren<SecurityCamera>(), "A camera on the door wall");

            // Hanging cameras: on the east edge only (the neighbour shares it), 3 m either side of its middle, over the intersections with the small beams.
            var middle = AssembleMiddleOfOpenHall(out _);
            var hanging = middle.GetComponentsInChildren<SecurityCamera>().Where(c => c.name.StartsWith("CameraPendant")).ToList();
            Assert.AreEqual(2, hanging.Count, "Two hanging cameras per open east edge");
            foreach (var camera in hanging)
            {
                var local = middle.transform.Find("HallTile").InverseTransformPoint(camera.transform.position);
                Assert.AreEqual(RoomContract.CellHalfSize, local.x, 0.001f);
                Assert.AreEqual(3f, Mathf.Abs(local.z), 0.001f);
            }
        }

        [Test]
        public void DoorSide_HasAnAutomaticDoorAndAFreePathToIt()
        {
            var map = new MapData(2, 1);
            map.Connect(0, 0, Direction.East);
            var cell = Assemble(map, (c, r) => c == 0, 0, 0, out var plan);
            Assert.AreEqual(HallSideKind.Door, plan.Side(Direction.East));
            Assert.AreEqual(HallSideKind.Wall, plan.Side(Direction.West));

            // From the pod ends up to the leaves of the closed door, which open into the hall.
            float half = RoomContract.OpeningWidth / 2f;
            var path = Box(HallContract.PodHalfLength + DoorMargin, -half, HallContract.WallInnerOffset - RoomContract.DoorPocketDepth, half);
            AssertFree(cell, new[] { path }, "door path");
            Assert.IsNotNull(cell.transform.Find("HallSide_East/Dressing/Architecture/DoorFrame"), "The door has no frame");
            var door = cell.transform.Find("HallSide_East/Dressing/Architecture/Door");
            Assert.IsNotNull(door, "The door side has no door");
            Assert.IsNotNull(door.GetComponent<AutoSlidingDoor>());
            Assert.IsFalse(door.GetComponent<AutoSlidingDoor>().IsLocked);
        }

        [Test]
        public void ConcaveCorner_KeepsTheWalkwayAroundTheInsideCorner()
        {
            // An L-shaped hall: the north-east cell is not hall, so (0, 0) has an inside corner in the north-east.
            var map = new MapData(2, 2);
            map.Connect(0, 0, Direction.East);
            map.Connect(0, 0, Direction.North);
            var cell = Assemble(map, (c, r) => !(c == 1 && r == 1), 0, 0, out var plan);
            Assert.AreEqual(HallCornerKind.Concave, plan.Corner(HallCorner.NorthEast));
            AssertFree(cell, new[] { CornerWalkway() }, "inside corner walkway");
            AssertRackSlotsFree(cell);
        }

        [Test]
        public void Hall_HasNoWaterNoOutletsAndAGasSystem()
        {
            var map = new MapData(2, 1);
            map.Connect(0, 0, Direction.East);
            var withDoor = Assemble(map, (c, r) => c == 0, 0, 0, out _);
            var lone = Assemble(new MapData(1, 1), (c, r) => true, 0, 0, out _);
            var inner = AssembleMiddleOfOpenHall(out _);
            foreach (var root in new[] { withDoor, lone, inner })
            {
                // Infrastructure only: no water, no outlets, no plant (that lives in the power room and the cooling plant).
                foreach (var banned in new[] { "Sprinkler", "HoseCabinet", "FireExtinguisher", "PowerSocket", "DataSocket", "Transformer",
                             "Switchgear", "ElectricalPanel", "AirHandlingUnit", "HeatExchanger", "CirculationPump", "BufferTank", "Shelf", "Desk" })
                {
                    Assert.IsEmpty(PropInstances(root, banned), $"The hall has a {banned}");
                }
                Assert.IsNotEmpty(PropInstances(root, "GasNozzle"), "The hall has no inert-gas nozzles");
                // No fire water anywhere in the hall.
                Assert.IsEmpty(root.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial != null && r.sharedMaterial.name == "M_SafetyRed" && r.name == "Pipe"),
                    "The hall has a fire-water pipe");
            }
            Assert.IsNotNull(lone.transform.Find("HallSide_North/Dressing/Pipes/GasRiser"), "A plain perimeter wall has no gas riser");
            Assert.IsNotEmpty(PropInstances(lone, "FireCallPoint"), "The perimeter has no call point");
        }

        [Test]
        public void Services_RunAboveTheHangingSigns()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var tile = cell.transform.Find("HallTile");
            float signTop = HallContract.CeilingHeight - SignDrop;
            foreach (var path in new[] { "Dressing/Power/PowerTray_N", "Dressing/Power/PowerTray_S", "Dressing/Data/DataTray_N", "Dressing/Data/DataTray_S",
                         "Dressing/Ventilation/ReturnDuct", "Dressing/Ventilation/SupplyDuct_N", "Dressing/Ventilation/SupplyDuct_S",
                         "Dressing/GasExtinguishing/GasLine_N", "Dressing/GasExtinguishing/GasLine_S" })
            {
                var run = tile.Find(path);
                Assert.IsNotNull(run, path);
                foreach (var renderer in run.GetComponentsInChildren<Renderer>())
                {
                    // Hangers (rods, nuts, struts) reach down to the run; only the run itself is checked.
                    if (renderer.name.Contains("Hanger") || renderer.name.StartsWith("Rod") || renderer.name.StartsWith("Trapeze")) continue;
                    Assert.GreaterOrEqual(renderer.bounds.min.y, signTop, $"{path}/{renderer.name} hangs among the signs");
                }
            }
        }

        [Test]
        public void EveryAisle_IsConnectedToTheCentralHvac()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var tile = cell.transform.Find("HallTile");
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                string name = pod == 0 ? "N" : "S";
                var duct = CombinedBounds(tile.Find($"Dressing/Ventilation/SupplyDuct_{name}"), r => r.name.Contains("Hanger"));
                var drop = CombinedBounds(tile.Find($"Dressing/Ventilation/SupplyDrop_{name}"), _ => false);
                // The duct runs through the whole tile (on into the neighbours), the drop from it down onto the aisle's roof.
                Assert.AreEqual(RoomContract.CellSize, duct.size.x, Epsilon);
                Assert.GreaterOrEqual(drop.max.y, duct.min.y, $"The drop of pod {name} does not reach its duct");
                Assert.LessOrEqual(drop.max.y, duct.max.y, $"The drop of pod {name} sticks out above its duct");
                Assert.AreEqual(HallContract.ContainmentHeight + HallContract.ContainmentPanel, drop.min.y, 0.01f, $"The drop of pod {name} does not reach the aisle roof");
                Assert.AreEqual(HallContract.PodZ(pod), drop.center.z, Epsilon);
            }
        }

        [Test]
        public void EveryPiece_StaysInsideTheCell()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var lone = Assemble(new MapData(1, 1), (c, r) => true, 0, 0, out _);
            float half = RoomContract.CellHalfSize + Epsilon;
            foreach (var root in new[] { cell, lone })
            {
                foreach (var (name, b) in ContentBounds(root))
                {
                    bool inside = b.min.x >= -half && b.max.x <= half && b.min.z >= -half && b.max.z <= half &&
                                  b.min.y >= -0.1f - Epsilon && b.max.y <= HallContract.CeilingHeight + HallContract.RoofThickness + Epsilon;
                    Assert.IsTrue(inside, $"{name} sticks out of the cell: {b}");
                }
            }
        }

        [Test]
        public void EveryPiece_LightsAreRealtimeWithoutShadows()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var lights = cell.GetComponentsInChildren<Light>();
            Assert.GreaterOrEqual(lights.Length, 4, "A hall tile has too few lights");
            foreach (var light in lights)
            {
                Assert.AreEqual(LightShadows.None, light.shadows, light.transform.parent.name);
                Assert.AreEqual(LightmapBakeType.Realtime, light.lightmapBakeType, light.transform.parent.name);
            }
        }

        [Test]
        public void BakedHall_HasOneLightPerWalkwaySegment_ClearOfEverythingOnTheCeiling()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HallPath);
            Assert.IsNotNull(prefab, $"{HallPath} is missing. Run Tools/Cold Shutdown/Create Preview Scene.");
            var hall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            created.Add(hall);
            var tiles = hall.GetComponentsInChildren<Transform>(true).Where(t => t.name == "HallTile").ToList();
            float cell = RoomContract.CellSize;
            int columns = Mathf.RoundToInt(tiles.Max(t => t.position.x) / cell) + 1;
            int rows = Mathf.RoundToInt(tiles.Max(t => t.position.z) / cell) + 1;
            Assert.AreEqual(columns * rows, tiles.Count);

            var fittings = hall.transform.Find("Lights").Cast<Transform>().ToList();
            Assert.AreEqual(rows * (columns + 1) + columns * (rows + 1), fittings.Count, "One light per walkway segment");
            Assert.IsEmpty(tiles.SelectMany(t => t.GetComponentsInChildren<Transform>()).Where(t => t.name.StartsWith("PendantLight")), "Tiles carry no lights of their own");

            // Everything on the ceiling: ducts, trays, gas lines, hangers, detectors, beams, girders, signs, cameras.
            var ceiling = hall.GetComponentsInChildren<Renderer>(true)
                .Where(r => !r.transform.IsChildOf(hall.transform.Find("Lights")) && r.bounds.max.y > HallContract.LightTop - 0.15f)
                .Select(r => (r.name, bounds: r.bounds)).ToList();
            foreach (var fitting in fittings)
            {
                var light = fitting.GetComponentInChildren<Light>();
                Assert.AreEqual(LightShadows.None, light.shadows, fitting.name);
                Assert.AreEqual(LightmapBakeType.Realtime, light.lightmapBakeType, fitting.name);
                // From the deck along a wall, from a girder between two tiles; the fitting's top is always at the same height.
                bool girder = PrefabUtility.GetCorrespondingObjectFromSource(fitting.gameObject).name == "PendantLightGirder";
                Assert.AreEqual(girder ? HallContract.GirderBottom : HallContract.CeilingHeight, fitting.position.y, 0.001f, $"{fitting.name} is not mounted on the deck or a girder");
                Assert.AreEqual(HallContract.LightTop, fitting.position.y - (girder ? GirderDrop : DeckDrop), 0.001f, $"{fitting.name} hangs at the wrong height");

                // Over a walkway: not over a pod or the hot aisle of any tile.
                foreach (var tile in tiles)
                {
                    var local = tile.InverseTransformPoint(fitting.position);
                    bool overPods = Mathf.Abs(local.x) < HallContract.PodHalfLength && Mathf.Abs(local.z) < HallContract.PodOuterZ;
                    Assert.IsFalse(overPods, $"{fitting.name} hangs over the pods of {tile.parent.name}");
                }

                // The two wire ropes (0.55 m either side along the fitting) from the mount down to the fitting, and the fitting itself.
                var points = new List<Vector3>();
                foreach (float offset in new[] { -0.55f, 0.55f })
                {
                    for (float y = fitting.position.y - 0.08f; y > HallContract.LightTop; y -= 0.05f)
                    {
                        points.Add(new Vector3(fitting.position.x, y, fitting.position.z) + fitting.forward * offset);
                    }
                }
                var along = new Vector3(Mathf.Abs(fitting.forward.x), 0f, Mathf.Abs(fitting.forward.z));
                var body = new Bounds(new Vector3(fitting.position.x, HallContract.LightTop - 0.05f, fitting.position.z),
                    along * 1.5f + (Vector3.one - along - Vector3.up) * 0.12f + Vector3.up * 0.1f);
                foreach (var (name, bounds) in ceiling)
                {
                    foreach (var point in points)
                    {
                        Assert.IsFalse(bounds.Contains(point), $"A wire rope of {fitting.name} runs through {name}");
                    }
                    Assert.IsFalse(bounds.Intersects(body), $"{fitting.name} touches {name}");
                }
            }
        }

        private const float DeckDrop = HallContract.CeilingHeight - HallContract.LightTop;
        private const float GirderDrop = HallContract.GirderBottom - HallContract.LightTop;

        [Test]
        public void Signs_ShowOnlyTheTileCode()
        {
            // A 2 x 2 hall at columns 3-4, rows 2-3 of a bigger map: codes count from the hall's south-west tile.
            var map = new MapData(6, 5);
            var cell = Assemble(map, (c, r) => c >= 3 && c <= 4 && r >= 2 && r <= 3, 4, 3, out var plan);
            Assert.AreEqual("B2", plan.Code);
            var signs = cell.GetComponentsInChildren<HallSign>();
            Assert.AreEqual(1, signs.Length, "Only the tile sign over the aisles hangs in the hall");
            Assert.AreEqual("B2", signs[0].Code);
            Assert.IsTrue(signs[0].transform.IsChildOf(cell.transform.Find("HallTile")));
        }

        [Test]
        public void EveryTile_HasSecurityCameras()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var cameras = cell.GetComponentsInChildren<SecurityCamera>();
            Assert.GreaterOrEqual(cameras.Length, 2);
            Assert.IsTrue(cameras.All(camera => camera.FeedCamera != null && !camera.FeedCamera.enabled));
        }

        // ---------------------------------------------------------------- helpers

        private GameObject AssembleMiddleOfOpenHall(out HallCell plan)
        {
            var map = new MapData(3, 3);
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (column < 2) map.Connect(column, row, Direction.East);
                    if (row < 2) map.Connect(column, row, Direction.North);
                }
            }
            var cell = Assemble(map, (c, r) => true, 1, 1, out plan);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(HallSideKind.Open, plan.Side((Direction)i));
                Assert.AreEqual(HallCornerKind.Inner, plan.Corner((HallCorner)i));
            }
            return cell;
        }

        private GameObject Assemble(MapData map, Func<int, int, bool> isHall, int column, int row, out HallCell plan)
        {
            var kit = AssetDatabase.LoadAssetAtPath<HallKit>(KitPath);
            Assert.IsNotNull(kit, $"{KitPath} is missing");
            Assert.IsTrue(HallPlan.Create(map, isHall).TryGetCell(column, row, out plan));

            var cell = new GameObject("Cell");
            created.Add(cell);
            var tile = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TilePath), cell.transform);
            tile.name = "HallTile";
            HallKit.LabelTile(tile, plan);
            kit.AddPieces(cell.transform, plan, (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent));
            // Collider bounds follow the turned pieces only after the transforms reach the physics scene.
            Physics.SyncTransforms();
            return cell;
        }

        /// <summary>World bounds of every renderer under <paramref name="root"/> that <paramref name="exclude"/> does not drop.</summary>
        private static Bounds CombinedBounds(Transform root, System.Func<Renderer, bool> exclude)
        {
            Assert.IsNotNull(root);
            Bounds? total = null;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (exclude(renderer)) continue;
                if (total == null) total = renderer.bounds;
                else
                {
                    var b = total.Value;
                    b.Encapsulate(renderer.bounds);
                    total = b;
                }
            }
            Assert.IsTrue(total.HasValue, $"{root.name} has no geometry");
            return total.Value;
        }

        /// <summary>Distance from the roof deck down to the top of a hanging sign (Props.HallSignDrop).</summary>
        private const float SignDrop = 1.8f;

        /// <summary>Instances of the prop prefab <c>Props/&lt;name&gt;.prefab</c> anywhere under <paramref name="root"/>.</summary>
        private static IEnumerable<GameObject> PropInstances(GameObject root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject)) continue;
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject);
                if (path == $"Assets/_Project/Prefabs/Props/{name}.prefab") yield return transform.gameObject;
            }
        }

        /// <summary>The walkway through the north-east corner square, clear of the column and the wall equipment.</summary>
        private static Bounds CornerWalkway()
        {
            float column = RoomContract.CellHalfSize - HallContract.ColumnSize / 2f;
            return Box(HallContract.PodHalfLength + DoorMargin, HallContract.PodOuterZ, column, column);
        }

        /// <summary>A walkway box from the floor to the opening height over the rectangle (x0, z0)-(x1, z1).</summary>
        private static Bounds Box(float x0, float z0, float x1, float z1)
        {
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(x0, WalkFloor, z0), new Vector3(x1, RoomContract.KeepClearHeight, z1));
            return bounds;
        }

        private static void AssertFree(GameObject cell, IEnumerable<Bounds> regions, string what)
        {
            foreach (var region in regions)
            {
                foreach (var (name, b) in ContentBounds(cell))
                {
                    Assert.IsFalse(Overlaps(b, region), $"{name} blocks the {what}: {b}");
                }
            }
        }

        private static void AssertRackSlotsFree(GameObject cell)
        {
            var rack = new Bounds(new Vector3(0f, RoomContract.RackHeight / 2f, 0f),
                new Vector3(RoomContract.RackWidth, RoomContract.RackHeight, RoomContract.RackDepth));
            foreach (var slot in cell.GetComponentsInChildren<RackSlot>())
            {
                var space = new Bounds(slot.transform.TransformPoint(rack.min), Vector3.zero);
                space.Encapsulate(slot.transform.TransformPoint(rack.max));
                foreach (var (name, b) in ContentBounds(cell))
                {
                    Assert.IsFalse(Overlaps(b, space), $"{name} blocks {slot.transform.parent.parent.name}/{slot.name}");
                }
            }
        }

        private static IEnumerable<(string name, Bounds bounds)> ContentBounds(GameObject cell)
        {
            // A hanging camera hangs on the girder right on the cell edge (shared with the neighbour): its dome reaches over the edge by design.
            bool IsHangingCamera(Component component) => component.GetComponentInParent<SecurityCamera>() is SecurityCamera camera && camera.name == HallKit.HangingCameraName;
            foreach (var renderer in cell.GetComponentsInChildren<Renderer>())
            {
                if (IsHangingCamera(renderer)) continue;
                yield return ($"{renderer.transform.parent.name}/{renderer.name}", renderer.bounds);
            }
            foreach (var collider in cell.GetComponentsInChildren<Collider>())
            {
                if (IsHangingCamera(collider)) continue;
                yield return ($"{collider.name} (collider)", collider.bounds);
            }
        }

        private static bool Overlaps(Bounds a, Bounds b)
        {
            return a.min.x < b.max.x - Epsilon && b.min.x < a.max.x - Epsilon &&
                   a.min.y < b.max.y - Epsilon && b.min.y < a.max.y - Epsilon &&
                   a.min.z < b.max.z - Epsilon && b.min.z < a.max.z - Epsilon;
        }
    }
}
