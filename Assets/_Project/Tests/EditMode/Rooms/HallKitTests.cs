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
    /// Assembles hall tiles from the real kit (Tools/Cold Shutdown/Create Hall Kit) the way the level builder
    /// does, and checks them against the hall contract: walkways stay free, rack slots stay usable, everything
    /// stays inside the cell, signs carry the right codes.
    /// </summary>
    public class HallKitTests
    {
        private const float Epsilon = 0.001f;
        private const string TilePath = "Assets/_Project/Prefabs/Rooms/Room_Hall.prefab";
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
            Assert.IsNotNull(kit, $"{KitPath} is missing. Run Tools/Cold Shutdown/Create Graybox Rooms.");
            Assert.IsTrue(kit.IsComplete, "The hall kit is missing a piece");

            var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>("Assets/_Project/Data/RoomDefinition_Hall.asset");
            Assert.IsNotNull(definition);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<GameObject>(TilePath), definition.Prefab);
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
            var tile = cell.transform.Find("Room_Hall");
            float aisle = HallContract.AisleWidth / 2f;
            var vents = PropInstances(tile.gameObject, "FloorVentTile").ToList();
            Assert.AreEqual(HallContract.PodsPerTile * HallContract.RacksPerRow * 2, vents.Count, "The aisle floors are not all perforated tiles");
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                float center = HallContract.PodZ(pod);
                var inside = new Bounds();
                inside.SetMinMax(new Vector3(-HallContract.PodHalfLength, WalkFloor, center - aisle), new Vector3(HallContract.PodHalfLength, RoomContract.RackHeight, center + aisle));
                AssertFree(cell, new[] { inside }, $"cold aisle {pod}");
                Assert.AreEqual(HallContract.RacksPerRow * 2, vents.Count(v => Mathf.Abs(v.transform.localPosition.z - center) < aisle &&
                                                                               Mathf.Abs(v.transform.localPosition.x) < HallContract.PodHalfLength));

                var roof = tile.Find($"Dressing/Pod_{(pod == 0 ? "N" : "S")}/ContainmentRoof");
                Assert.IsNotNull(roof, $"Aisle {pod} has no roof");
                Assert.AreEqual(HallContract.ContainmentHeight, roof.GetComponent<Renderer>().bounds.min.y, Epsilon);
            }

            var doors = tile.GetComponentsInChildren<AutoSlidingDoor>();
            Assert.AreEqual(2 * HallContract.PodsPerTile, doors.Length, "Both ends of every aisle need an automatic door");
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
                // Only the glass leaves of the automatic doors may be glass; small covers (a call point's) are no partition.
                if (renderer.GetComponentInParent<AutoSlidingDoor>() != null) continue;
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
        public void PerimeterWall_HasLargeGrillesLowAndSmallGrillesHigh()
        {
            var cell = Assemble(new MapData(1, 1), (c, r) => true, 0, 0, out _);
            var side = cell.transform.Find("HallSide_North").gameObject;
            var low = PropInstances(side, "FloorVentGrille").ToList();
            var high = PropInstances(side, "WallVentGrille").ToList();
            Assert.IsNotEmpty(low, "No supply grilles at the floor");
            Assert.IsNotEmpty(high, "No return grilles under the roof");
            // The low grilles start on top of the plinth, as high above the floor as in the corridors (0.15 m) and as far
            // as the high grilles stay below the girders.
            float Bottom(GameObject g) => g.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y);
            float Top(GameObject g) => g.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y);
            Assert.IsTrue(low.All(g => Mathf.Abs(Bottom(g) - 0.15f) < 0.005f), "The low grilles are not 0.15 m above the floor");
            Assert.IsTrue(high.All(g => g.transform.position.y > HallContract.CeilingHeight - 2f));
            Assert.IsTrue(high.All(g => Mathf.Abs(Top(g) - 5.05f) < 0.005f), "The high grilles are not 0.15 m below the girders");
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
                foreach (var banned in new[] { "Sprinkler", "HydrantCabinet", "FireExtinguisher", "PowerSocket", "DataSocket", "Transformer",
                             "SwitchgearCabinet", "ElectricalPanel", "AirHandlingUnit", "PlateHeatExchanger", "CirculationPump", "BufferTank", "Shelf", "Desk" })
                {
                    Assert.IsEmpty(PropInstances(root, banned), $"The hall has a {banned}");
                }
                Assert.IsNotEmpty(PropInstances(root, "GasNozzle"), "The hall has no inert-gas nozzles");
                // No fire water anywhere in the hall.
                Assert.IsEmpty(root.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterial != null && r.sharedMaterial.name == "M_SafetyRed" && r.name == "Pipe"),
                    "The hall has a fire-water pipe");
            }
            Assert.IsNotNull(lone.transform.Find("HallSide_North/Dressing/Pipes/GasRiser"), "A plain perimeter wall has no gas riser");
            Assert.IsNotEmpty(PropInstances(withDoor, "GasReleaseStation"), "The perimeter has no gas release station");
            Assert.IsNotEmpty(PropInstances(lone, "FireCallPoint"), "The perimeter has no call point");
        }

        [Test]
        public void Services_RunAboveTheHangingSigns()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var tile = cell.transform.Find("Room_Hall");
            float signTop = HallContract.CeilingHeight - GrayboxPropsDrop;
            foreach (var path in new[] { "Dressing/Power/PowerTray_N", "Dressing/Power/PowerTray_S", "Dressing/Data/DataTray_N", "Dressing/Data/DataTray_S",
                         "Dressing/Ventilation/ReturnDuct", "Dressing/Ventilation/SupplyDuct_N", "Dressing/Ventilation/SupplyDuct_S",
                         "Dressing/GasExtinguishing/GasLine_N", "Dressing/GasExtinguishing/GasLine_S" })
            {
                var run = tile.Find(path);
                Assert.IsNotNull(run, path);
                foreach (var renderer in run.GetComponentsInChildren<Renderer>())
                {
                    // Hanger rods reach down to the run; only the run itself is checked.
                    if (renderer.name.StartsWith("Rod") || renderer.name.StartsWith("Trapeze")) continue;
                    Assert.GreaterOrEqual(renderer.bounds.min.y, signTop, $"{path}/{renderer.name} hangs among the signs");
                }
            }
        }

        [Test]
        public void EveryAisle_IsConnectedToTheCentralHvac()
        {
            var cell = AssembleMiddleOfOpenHall(out _);
            var tile = cell.transform.Find("Room_Hall");
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                string name = pod == 0 ? "N" : "S";
                var duct = tile.Find($"Dressing/Ventilation/SupplyDuct_{name}").GetComponent<Renderer>().bounds;
                var drop = tile.Find($"Dressing/Ventilation/SupplyDrop_{name}").GetComponent<Renderer>().bounds;
                // The duct runs through the whole tile (on into the neighbours), the drop from it down onto the aisle's roof.
                Assert.AreEqual(RoomContract.CellSize, duct.size.x, Epsilon);
                Assert.AreEqual(duct.min.y, drop.max.y, Epsilon, $"The drop of pod {name} does not reach its duct");
                Assert.AreEqual(HallContract.ContainmentHeight, drop.min.y, 0.05f, $"The drop of pod {name} does not reach the aisle");
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
        public void Signs_ShowOnlyTheTileCode()
        {
            // A 2 x 2 hall at columns 3-4, rows 2-3 of a bigger map: codes count from the hall's south-west tile.
            var map = new MapData(6, 5);
            var cell = Assemble(map, (c, r) => c >= 3 && c <= 4 && r >= 2 && r <= 3, 4, 3, out var plan);
            Assert.AreEqual("B2", plan.Code);
            var signs = cell.GetComponentsInChildren<HallSign>();
            Assert.AreEqual(1, signs.Length, "Only the tile sign over the aisles hangs in the hall");
            Assert.AreEqual("B2", signs[0].Code);
            Assert.IsTrue(signs[0].transform.IsChildOf(cell.transform.Find("Room_Hall")));
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
            tile.name = "Room_Hall";
            HallKit.LabelTile(tile, plan);
            kit.AddPieces(cell.transform, plan, (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent));
            // Collider bounds follow the turned pieces only after the transforms reach the physics scene.
            Physics.SyncTransforms();
            return cell;
        }

        /// <summary>Distance from the roof deck down to the top of a hanging sign (GrayboxPropsTool.HallSignDrop).</summary>
        private const float GrayboxPropsDrop = 1.8f;

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
            foreach (var renderer in cell.GetComponentsInChildren<Renderer>())
            {
                yield return ($"{renderer.transform.parent.name}/{renderer.name}", renderer.bounds);
            }
            foreach (var collider in cell.GetComponentsInChildren<Collider>())
            {
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
