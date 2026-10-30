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
    /// <summary>Builds levels from the real room prefabs and checks them against the map data and the room contract.</summary>
    public class LevelBuilderTests
    {
        private const float Epsilon = 0.001f;
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
        public void Build_MakesOneCellPerGridCell_AtTheContractPosition()
        {
            var builder = CreateBuilder();
            Assert.IsTrue(builder.Build(5, 4, 3));

            var level = LevelRoot(builder);
            Assert.AreEqual(12, level.childCount);
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    var cell = level.Find($"Cell_{column}_{row}");
                    Assert.IsNotNull(cell, $"Cell_{column}_{row}");
                    Assert.AreEqual(RoomContract.CellOriginX(column), cell.position.x, Epsilon);
                    Assert.AreEqual(0f, cell.position.y, Epsilon);
                    Assert.AreEqual(RoomContract.CellOriginZ(row), cell.position.z, Epsilon);
                }
            }
        }

        [Test]
        public void Build_PlacesTheRoomOfEveryCellWithItsTypeAndTurn()
        {
            var builder = CreateBuilder();
            builder.Build(11, 5, 5);

            foreach (var room in builder.Layout.Rooms)
            {
                var cell = LevelRoot(builder).Find($"Cell_{room.Column}_{room.Row}");
                var instance = cell.Find($"Room_{room.Type}");
                Assert.IsNotNull(instance, $"Cell_{room.Column}_{room.Row} has no Room_{room.Type}");
                Assert.AreEqual(0f, Vector3.Distance(cell.position, instance.position), Epsilon);
                Assert.AreEqual(0f, Mathf.DeltaAngle(instance.eulerAngles.y, room.YawDegrees), Epsilon);
            }
        }

        [Test]
        public void Build_PlugsExactlyTheClosedOpenings()
        {
            var builder = CreateBuilder();
            for (int seed = 0; seed < 10; seed++)
            {
                builder.Build(seed, 6, 6);
                var map = builder.Map;
                foreach (var room in builder.Layout.Rooms)
                {
                    var cell = LevelRoot(builder).Find($"Cell_{room.Column}_{room.Row}");
                    foreach (Direction side in System.Enum.GetValues(typeof(Direction)))
                    {
                        var plug = cell.Find($"WallPlug_{side}");
                        bool closed = !map.IsConnected(room.Column, room.Row, side);
                        Assert.AreEqual(closed, plug != null, $"seed {seed}, cell ({room.Column}, {room.Row}), {side}");
                    }
                }
            }
        }

        [Test]
        public void Build_WallsUpPluggedOpeningsWithoutADoorFrame()
        {
            var builder = CreateBuilder();
            builder.Build(5, 6, 6);
            var map = builder.Map;
            foreach (var room in builder.Layout.Rooms)
            {
                if (room.Type == RoomType.Hall) continue;
                var instance = LevelRoot(builder).Find($"Cell_{room.Column}_{room.Row}/Room_{room.Type}");
                foreach (Direction side in System.Enum.GetValues(typeof(Direction)))
                {
                    // The room's wall on this map side, wherever the room is turned.
                    var local = (Direction)((((int)side - room.QuarterTurns) % 4 + 4) % 4);
                    var frame = instance.Find($"Shell/Wall_{local}/DoorFrame");
                    Assert.IsNotNull(frame, $"{room.Type} has no door frame on its {local} wall");
                    bool plugged = !map.IsConnected(room.Column, room.Row, side);
                    Assert.AreEqual(!plugged, frame.gameObject.activeSelf, $"({room.Column}, {room.Row}) {side}: the frame should {(plugged ? "be hidden" : "show")}");
                }
            }
        }

        [Test]
        public void Build_GivesEveryRoomItsOneLayout()
        {
            var builder = CreateBuilder();
            for (int seed = 0; seed < 20; seed++)
            {
                builder.Build(seed, 6, 6);
                foreach (var room in builder.Layout.Rooms.Where(r => !RoomRules.IsCorridor(r.Type) && r.Type != RoomType.Hall))
                {
                    Assert.AreEqual(0, room.Layout, $"{room.Type} has only one layout");
                }
            }
        }

        [Test]
        public void Build_PlugsFillTheirOpeningsExactly()
        {
            var builder = CreateBuilder();
            builder.Build(3, 4, 4);

            foreach (var plug in LevelRoot(builder).GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("WallPlug_")))
            {
                var side = (Direction)System.Enum.Parse(typeof(Direction), plug.name.Substring("WallPlug_".Length));
                var cell = plug.parent;
                var pose = RoomContract.GetOpeningPose(side);
                var expectedCenter = cell.position + new Vector3(pose.X, RoomContract.OpeningHeight / 2f, pose.Z);

                var bounds = plug.GetComponentInChildren<Renderer>().bounds;
                Assert.AreEqual(0f, Vector3.Distance(bounds.center, expectedCenter), Epsilon, $"{cell.name}/{plug.name}");
                Assert.AreEqual(RoomContract.OpeningHeight, bounds.size.y, Epsilon);
            }
        }

        [Test]
        public void Build_ClosesTheOutsideOfTheMap()
        {
            var builder = CreateBuilder();
            builder.Build(9, 5, 4);

            var level = LevelRoot(builder);
            for (int column = 0; column < 5; column++)
            {
                Assert.IsNotNull(level.Find($"Cell_{column}_0/WallPlug_South"), $"column {column} south edge");
                Assert.IsNotNull(level.Find($"Cell_{column}_3/WallPlug_North"), $"column {column} north edge");
            }
            for (int row = 0; row < 4; row++)
            {
                Assert.IsNotNull(level.Find($"Cell_0_{row}/WallPlug_West"), $"row {row} west edge");
                Assert.IsNotNull(level.Find($"Cell_4_{row}/WallPlug_East"), $"row {row} east edge");
            }
        }

        [Test]
        public void Build_SameSeed_GivesTheSameLevel()
        {
            var builder = CreateBuilder();
            builder.Build(42, 6, 6);
            var first = Describe(builder);
            builder.Build(7, 6, 6);
            builder.Build(42, 6, 6);

            CollectionAssert.AreEqual(first, Describe(builder));
        }

        [Test]
        public void Build_ReplacesThePreviousLevel()
        {
            var builder = CreateBuilder();
            builder.Build(1, 6, 6);
            builder.Build(2, 3, 3);

            Assert.AreEqual(1, builder.transform.childCount);
            Assert.AreEqual(9, LevelRoot(builder).childCount);
            Assert.AreEqual(3, builder.Width);
            Assert.AreEqual(2, builder.Seed);
        }

        [Test]
        public void Clear_RemovesTheLevel()
        {
            var builder = CreateBuilder();
            builder.Build(1, 3, 3);
            builder.Clear();

            Assert.AreEqual(0, builder.transform.childCount);
            Assert.IsFalse(builder.HasLevel);
        }

        [Test]
        public void Build_RaisesLevelBuilt()
        {
            var builder = CreateBuilder();
            int raised = 0;
            builder.LevelBuilt += _ => raised++;
            builder.Build(1, 3, 3);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Build_PutsThePlayerIntoTheLiftCar_FacingItsDoor()
        {
            var builder = CreateBuilder(withPlayer: true);
            builder.Build(1, 7, 5);

            Assert.IsTrue(builder.Layout.HasLift);
            var (column, row) = builder.StartCell;
            Assert.AreEqual(RoomType.Elevator, builder.Layout.GetRoom(column, row).Type);
            Assert.IsTrue(builder.Player.GetComponent<CharacterController>().enabled);
            Assert.AreEqual(0f, Vector3.Distance(builder.StartPose.position, builder.Player.position), Epsilon);

            // Inside the car: between its walls, behind its door, looking at the door (room-local -Z).
            var room = LevelRoot(builder).Find($"Cell_{column}_{row}/Room_{RoomType.Elevator}");
            var local = room.InverseTransformPoint(builder.Player.position);
            Assert.Less(Mathf.Abs(local.x), RoomContract.LiftCarWidth / 2f - 0.3f, "The player is not between the car walls");
            Assert.Greater(local.z, RoomContract.LiftCarFront + 0.3f, "The player is not behind the lift door");
            Assert.Less(local.z, RoomContract.LiftCarFront + RoomContract.LiftCarDepth - 0.3f, "The player is behind the car");
            Assert.Greater(Vector3.Dot(builder.Player.forward, -room.forward), 0.99f, "The player does not face the lift door");
        }

        [Test]
        public void Build_FailsWithoutAWallPlug()
        {
            var builder = CreateBuilder(withPlug: false);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no wall plug"));
            Assert.IsFalse(builder.Build(1, 3, 3));
            Assert.IsFalse(builder.HasLevel);
        }

        [Test]
        public void Build_FailsWithoutRoomDefinitions()
        {
            var builder = CreateBuilder(withDefinitions: false);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no room definition"));
            Assert.IsFalse(builder.Build(1, 3, 3));
        }

        [Test]
        public void Build_FailsForAnInvalidSize()
        {
            var builder = CreateBuilder();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("not valid"));
            Assert.IsFalse(builder.Build(1, 0, 3));
        }

        [Test]
        public void Build_PutsAnAutomaticDoorIntoEveryOpeningOfARoom()
        {
            var builder = CreateBuilder();
            builder.Build(21, 6, 6);
            var layout = builder.Layout;
            var level = LevelRoot(builder);
            int expected = 0;
            foreach (var room in layout.Rooms)
            {
                foreach (var side in new[] { Direction.East, Direction.North })
                {
                    if (!layout.Map.IsConnected(room.Column, room.Row, side)) continue;
                    var other = layout.GetRoom(room.Column + side.ColumnOffset(), room.Row + side.RowOffset());
                    var placement = RoomRules.DoorBetween(room.Type, other.Type);
                    if (placement == DoorPlacement.First)
                    {
                        expected++;
                        Assert.IsNotNull(level.Find($"Cell_{room.Column}_{room.Row}/Door_{side}"), $"No door in ({room.Column}, {room.Row}) {side}");
                    }
                    else if (placement == DoorPlacement.Second)
                    {
                        expected++;
                        Assert.IsNotNull(level.Find($"Cell_{other.Column}_{other.Row}/Door_{side.Opposite()}"), $"No door in ({other.Column}, {other.Row}) {side.Opposite()}");
                    }
                }
            }
            // The doors of the openings, not the lift's own door inside its room.
            var doors = level.GetComponentsInChildren<AutoSlidingDoor>().Where(d => d.name.StartsWith("Door_")).ToArray();
            Assert.AreEqual(expected, doors.Length);
            Assert.Greater(expected, 0, "This seed should have rooms with doors");
            foreach (var door in doors)
            {
                // The door stands in its opening: on the cell edge, in the middle of the side.
                var local = door.transform.localPosition;
                Assert.AreEqual(RoomContract.WallCenterOffset, Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)), Epsilon);
                Assert.AreEqual(0f, Mathf.Min(Mathf.Abs(local.x), Mathf.Abs(local.z)), Epsilon);
            }
        }

        private LevelBuilder CreateBuilder(bool withPlug = true, bool withDefinitions = true, bool withPlayer = false)
        {
            var host = new GameObject("LevelBuilderTest");
            created.Add(host);
            var builder = host.AddComponent<LevelBuilder>();

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
                var door = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/AutoSlidingDoor.prefab");
                Assert.IsNotNull(door, "AutoSlidingDoor.prefab is missing");
                serialized.FindProperty("doorPrefab").objectReferenceValue = door;
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

        /// <summary>Name, position and turn of every room and plug in the level: enough to tell two levels apart.</summary>
        private static List<string> Describe(LevelBuilder builder) =>
            LevelRoot(builder).GetComponentsInChildren<Transform>()
                .Where(t => t.parent != null && t.parent.name.StartsWith("Cell_"))
                .Select(t => $"{t.parent.name}/{t.name} {t.position} {t.eulerAngles.y:0}")
                .ToList();
    }
}
