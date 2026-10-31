using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>Builds world maps from the real room prefabs and checks them against the map data and the room contract.</summary>
    public class LevelBuilderTests
    {
        private const float Epsilon = 0.001f;

        /// <summary>A small map without a hall: every corridor shape but the cross and straight, three rooms and the lift end.</summary>
        private const string SmallMap =
            ". O . G\n" +
            "C C C C\n" +
            "E . P .\n";

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var item in created)
            {
                Object.DestroyImmediate(item);
            }
            created.Clear();
        }

        [Test]
        public void Build_MakesACellPerRoom_AtTheContractPosition_AndNoneForEmptyCells()
        {
            var builder = CreateBuilder(SmallMap);
            Assert.IsTrue(builder.Build());
            var level = LevelRoot(builder);
            Assert.AreEqual(builder.Layout.Rooms.Count, level.Cast<Transform>().Count(t => t.name.StartsWith("Cell_")));
            foreach (var room in builder.Layout.Rooms)
            {
                var cell = level.Find($"Cell_{room.Column}_{room.Row}");
                Assert.IsNotNull(cell, $"Cell_{room.Column}_{room.Row}");
                Assert.AreEqual(RoomContract.CellOriginX(room.Column), cell.position.x, Epsilon);
                Assert.AreEqual(0f, cell.position.y, Epsilon);
                Assert.AreEqual(RoomContract.CellOriginZ(room.Row), cell.position.z, Epsilon);
            }
            Assert.IsNull(level.Find("Cell_0_2"), "An empty cell got an object");
        }

        [Test]
        public void Build_PlacesTheRoomOfEveryCellWithItsTypeAndTurn()
        {
            var builder = CreateBuilder(SmallMap);
            builder.Build();
            var level = LevelRoot(builder);
            foreach (var room in builder.Layout.Rooms)
            {
                var cell = level.Find($"Cell_{room.Column}_{room.Row}");
                var instance = cell.Find($"Room_{room.Type}");
                Assert.IsNotNull(instance, $"Cell_{room.Column}_{room.Row} has no Room_{room.Type}");
                Assert.AreEqual(0f, Vector3.Distance(cell.position, instance.position), Epsilon);
                Assert.AreEqual(0f, Mathf.DeltaAngle(instance.eulerAngles.y, room.YawDegrees), Epsilon);
            }
        }

        [Test]
        public void Build_PlugsExactlyTheClosedOpenings_AndHidesTheirDoorFrames()
        {
            var builder = CreateBuilder(SmallMap);
            builder.Build();
            var level = LevelRoot(builder);
            foreach (var room in builder.Layout.Rooms)
            {
                var cell = level.Find($"Cell_{room.Column}_{room.Row}");
                var instance = cell.Find($"Room_{room.Type}");
                for (int i = 0; i < DirectionExtensions.Count; i++)
                {
                    var side = (Direction)i;
                    bool plugged = !builder.Map.IsConnected(room.Column, room.Row, side);
                    Assert.AreEqual(plugged, cell.Find($"WallPlug_{side}") != null, $"({room.Column}, {room.Row}) {side}");
                    // The room's wall on this map side, wherever the room is turned.
                    var local = (Direction)((((int)side - room.QuarterTurns) % 4 + 4) % 4);
                    var frame = instance.Find($"Shell/Wall_{local}/DoorFrame");
                    if (frame == null) continue;
                    Assert.AreEqual(!plugged, frame.gameObject.activeSelf, $"({room.Column}, {room.Row}) {side}: the frame should {(plugged ? "be hidden" : "show")}");
                }
            }
        }

        [Test]
        public void Build_PlugsFillTheirOpeningsExactly()
        {
            var builder = CreateBuilder(SmallMap);
            builder.Build();
            foreach (var plug in LevelRoot(builder).GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("WallPlug_")))
            {
                var side = (Direction)System.Enum.Parse(typeof(Direction), plug.name.Substring("WallPlug_".Length));
                var pose = RoomContract.GetOpeningPose(side);
                var expectedCenter = plug.parent.position + new Vector3(pose.X, RoomContract.OpeningHeight / 2f, pose.Z);
                var bounds = plug.GetComponentInChildren<Renderer>().bounds;
                Assert.AreEqual(0f, Vector3.Distance(bounds.center, expectedCenter), Epsilon, $"{plug.parent.name}/{plug.name}");
                Assert.AreEqual(RoomContract.OpeningHeight, bounds.size.y, Epsilon);
            }
        }

        [Test]
        public void Build_PutsAnAutomaticDoorIntoEveryOpeningOfARoom()
        {
            var builder = CreateBuilder(SmallMap);
            builder.Build();
            var layout = builder.Layout;
            var level = LevelRoot(builder);
            int expected = 0;
            foreach (var room in layout.Rooms)
            {
                foreach (var side in new[] { Direction.East, Direction.North })
                {
                    if (!layout.Map.IsConnected(room.Column, room.Row, side)) continue;
                    var other = layout.GetRoom(room.Column + side.ColumnOffset(), room.Row + side.RowOffset());
                    switch (RoomRules.DoorBetween(room.Type, other.Type))
                    {
                        case DoorPlacement.First:
                            expected++;
                            Assert.IsNotNull(level.Find($"Cell_{room.Column}_{room.Row}/Door_{side}"), $"No door in ({room.Column}, {room.Row}) {side}");
                            break;
                        case DoorPlacement.Second:
                            expected++;
                            Assert.IsNotNull(level.Find($"Cell_{other.Column}_{other.Row}/Door_{side.Opposite()}"), $"No door in ({other.Column}, {other.Row}) {side.Opposite()}");
                            break;
                    }
                }
            }
            // The doors of the openings, not the lift's own door inside its room.
            var doors = level.GetComponentsInChildren<AutoSlidingDoor>().Where(d => d.name.StartsWith("Door_")).ToArray();
            Assert.AreEqual(3, expected, "The office, the security office and the power room have one door each");
            Assert.AreEqual(expected, doors.Length);
            foreach (var door in doors)
            {
                var local = door.transform.localPosition;
                Assert.AreEqual(RoomContract.WallCenterOffset, Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)), Epsilon);
                Assert.AreEqual(0f, Mathf.Min(Mathf.Abs(local.x), Mathf.Abs(local.z)), Epsilon);
            }
        }

        [Test]
        public void Build_PutsARackOnEveryRackSlot()
        {
            var builder = CreateBuilder(SmallMap);
            builder.Build();
            var level = LevelRoot(builder);
            var slots = level.GetComponentsInChildren<RackSlot>();
            Assert.AreEqual(2, slots.Length, "The security office has two rack slots");
            Assert.AreEqual(2, level.GetComponentsInChildren<Rack>().Length);
            Assert.AreEqual(1, level.GetComponentsInChildren<Rack>().Count(rack => rack.IsOpenable), "One openable rack in the security office");
        }

        [Test]
        public void Build_ReplacesThePreviousLevel_AndClearRemovesIt()
        {
            var builder = CreateBuilder(SmallMap);
            builder.Build();
            builder.Build();
            Assert.AreEqual(1, builder.transform.childCount);
            builder.Clear();
            Assert.AreEqual(0, builder.transform.childCount);
            Assert.IsFalse(builder.HasLevel);
        }

        [Test]
        public void Build_RaisesLevelBuilt()
        {
            var builder = CreateBuilder(SmallMap);
            int raised = 0;
            builder.LevelBuilt += _ => raised++;
            builder.Build();
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Build_PutsThePlayerIntoTheLiftCar_FacingItsDoor()
        {
            var builder = CreateBuilder(SmallMap, withPlayer: true);
            builder.Build();
            var (column, row) = builder.StartCell;
            Assert.IsTrue(builder.Layout.HasLift);
            Assert.AreEqual(RoomType.LiftEnd, builder.Layout.GetRoom(column, row).Type);
            Assert.IsTrue(builder.Player.GetComponent<CharacterController>().enabled);
            Assert.AreEqual(0f, Vector3.Distance(builder.StartPose.position, builder.Player.position), Epsilon);

            // Inside the car: between its walls, behind its door, looking at the door (room-local -Z).
            var room = LevelRoot(builder).Find($"Cell_{column}_{row}/Room_{RoomType.LiftEnd}");
            var local = room.InverseTransformPoint(builder.Player.position);
            Assert.Less(Mathf.Abs(local.x), RoomContract.LiftCarWidth / 2f - 0.3f, "The player is not between the car walls");
            Assert.Greater(local.z, RoomContract.LiftCarFront + 0.3f, "The player is not behind the lift door");
            Assert.Less(local.z, RoomContract.LiftCarFront + RoomContract.LiftCarDepth - 0.3f, "The player is behind the car");
            Assert.Greater(Vector3.Dot(builder.Player.forward, -room.forward), 0.99f, "The player does not face the lift door");
        }

        [Test]
        public void Build_TheWorldMap_PlacesTheHallOnceAtItsSouthWestCell()
        {
            var map = AssetDatabase.LoadAssetAtPath<WorldMap>("Assets/_Project/Data/WorldMap.asset");
            Assert.IsNotNull(map, "Data/WorldMap.asset is missing. Run Tools/Cold Shutdown/Create Preview Scene.");
            var builder = CreateBuilder(null);
            builder.WorldMap = map;
            Assert.IsTrue(builder.Build());
            var level = LevelRoot(builder);
            var halls = level.Cast<Transform>().Where(t => t.name == "Room_Hall").ToList();
            var hallCells = builder.Layout.Rooms.Where(room => room.Type == RoomType.Hall).ToList();
            if (hallCells.Count == 0)
            {
                Assert.IsEmpty(halls);
                return;
            }
            Assert.AreEqual(1, halls.Count, "The hall is one room");
            int column = hallCells.Min(room => room.Column), row = hallCells.Min(room => room.Row);
            Assert.AreEqual(0f, Vector3.Distance(halls[0].position, new Vector3(RoomContract.CellOriginX(column), 0f, RoomContract.CellOriginZ(row))), Epsilon);
            Assert.AreEqual(hallCells.Count, halls[0].GetComponentsInChildren<HallSign>().Length, "One tile (with its sign) per hall cell");
            foreach (var cell in hallCells)
            {
                Assert.IsNull(level.Find($"Cell_{cell.Column}_{cell.Row}"), "A hall cell got a cell object of its own");
            }
            Assert.AreEqual(level.GetComponentsInChildren<RackSlot>().Length, level.GetComponentsInChildren<Rack>().Length, "A rack on every slot");
        }

        [Test]
        public void Build_FailsWithoutAWallPlug()
        {
            var builder = CreateBuilder(SmallMap, withPlug: false);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no wall plug"));
            Assert.IsFalse(builder.Build());
            Assert.IsFalse(builder.HasLevel);
        }

        [Test]
        public void Build_FailsWithoutRoomDefinitions()
        {
            var builder = CreateBuilder(SmallMap, withDefinitions: false);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no room definition"));
            Assert.IsFalse(builder.Build());
        }

        [Test]
        public void Build_FailsForAnInvalidMap()
        {
            var builder = CreateBuilder("C C C\n");
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("World map"));
            Assert.IsFalse(builder.Build());
            Assert.IsFalse(builder.HasLevel);
        }

        private LevelBuilder CreateBuilder(string mapText, bool withPlug = true, bool withDefinitions = true, bool withPlayer = false)
        {
            var host = new GameObject("LevelBuilderTest");
            created.Add(host);
            var builder = host.AddComponent<LevelBuilder>();
            if (mapText != null)
            {
                var map = ScriptableObject.CreateInstance<WorldMap>();
                map.Text = mapText;
                created.Add(map);
                builder.WorldMap = map;
            }

            var serialized = new SerializedObject(builder);
            if (withDefinitions)
            {
                var types = (RoomType[])System.Enum.GetValues(typeof(RoomType));
                var property = serialized.FindProperty("roomDefinitions");
                property.arraySize = types.Length;
                for (int i = 0; i < types.Length; i++)
                {
                    var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"Assets/_Project/Data/RoomDefinition_{types[i]}.asset");
                    Assert.IsNotNull(definition, $"RoomDefinition for {types[i]} is missing");
                    property.GetArrayElementAtIndex(i).objectReferenceValue = definition;
                }
            }
            if (withPlug)
            {
                var plug = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Rooms/WallPlug.prefab");
                Assert.IsNotNull(plug, "WallPlug.prefab is missing");
                serialized.FindProperty("wallPlugPrefab").objectReferenceValue = plug;
                var door = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/RoomDoor.prefab");
                Assert.IsNotNull(door, "RoomDoor.prefab is missing");
                serialized.FindProperty("doorPrefab").objectReferenceValue = door;
                var rack = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/Rack.prefab");
                Assert.IsNotNull(rack, "Rack.prefab is missing");
                serialized.FindProperty("rackPrefab").objectReferenceValue = rack;
            }
            if (withPlayer)
            {
                var player = new GameObject("TestPlayer", typeof(CharacterController));
                created.Add(player);
                serialized.FindProperty("player").objectReferenceValue = player.transform;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return builder;
        }

        private static Transform LevelRoot(LevelBuilder builder) => builder.transform.Find("Level");
    }
}
