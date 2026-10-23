using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>
    /// Checks the generated room prefabs against the room contract. The prefabs come from
    /// Tools/Cold Shutdown/Create Room Shell and Create Graybox Rooms.
    /// </summary>
    public class RoomPrefabTests
    {
        private const float Epsilon = 0.001f;
        private const string RoomsFolder = "Assets/_Project/Prefabs/Rooms";
        private const string ShellPath = RoomsFolder + "/RoomShell.prefab";
        private const string WallPlugPath = RoomsFolder + "/WallPlug.prefab";

        private static readonly RoomType[] RoomTypes = (RoomType[])System.Enum.GetValues(typeof(RoomType));

        private readonly List<GameObject> instances = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in instances)
            {
                Object.DestroyImmediate(instance);
            }
            instances.Clear();
        }

        [Test]
        public void Shell_HasNothingInAnyOpening()
        {
            var shell = Instantiate(ShellPath);
            foreach (Direction side in System.Enum.GetValues(typeof(Direction)))
            {
                var opening = OpeningBounds(side);
                foreach (var renderer in shell.GetComponentsInChildren<Renderer>())
                {
                    Assert.IsFalse(Overlaps(renderer.bounds, opening), $"{renderer.name} blocks the {side} opening");
                }
            }
        }

        [Test]
        public void Shell_CoversTheWholeCell()
        {
            // The shell's footprint must be the whole cell, from the floor to the ceiling.
            var shell = Instantiate(ShellPath);
            var bounds = shell.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
            Assert.AreEqual(-RoomContract.CellHalfSize, bounds.min.x, Epsilon);
            Assert.AreEqual(RoomContract.CellHalfSize, bounds.max.x, Epsilon);
            Assert.AreEqual(-RoomContract.CellHalfSize, bounds.min.z, Epsilon);
            Assert.AreEqual(RoomContract.CellHalfSize, bounds.max.z, Epsilon);
        }

        [Test]
        public void WallPlug_FillsExactlyOneOpening()
        {
            foreach (Direction side in System.Enum.GetValues(typeof(Direction)))
            {
                var plug = Instantiate(WallPlugPath);
                var pose = RoomContract.GetOpeningPose(side);
                plug.transform.SetPositionAndRotation(new Vector3(pose.X, 0f, pose.Z), Quaternion.Euler(0f, pose.YawDegrees, 0f));

                var bounds = plug.GetComponentInChildren<Renderer>().bounds;
                var opening = OpeningBounds(side);
                Assert.That(Vector3.Distance(bounds.min, opening.min), Is.LessThan(Epsilon), side.ToString());
                Assert.That(Vector3.Distance(bounds.max, opening.max), Is.LessThan(Epsilon), side.ToString());
            }
        }

        [Test]
        public void ServerAisle_RackSlotsMatchContract()
        {
            var room = Instantiate(PrefabPath(RoomType.ServerAisle));
            var slots = room.GetComponentsInChildren<RackSlot>();
            var expected = RoomContract.GetServerAisleRackSlots();
            Assert.AreEqual(expected.Length, slots.Length);

            foreach (var pose in expected)
            {
                bool found = slots.Any(slot =>
                    Mathf.Abs(slot.transform.position.x - pose.X) < Epsilon &&
                    Mathf.Abs(slot.transform.position.y) < Epsilon &&
                    Mathf.Abs(slot.transform.position.z - pose.Z) < Epsilon &&
                    Mathf.Abs(Mathf.DeltaAngle(slot.transform.eulerAngles.y, pose.YawDegrees)) < Epsilon);
                Assert.IsTrue(found, $"No rack slot at ({pose.X}, {pose.Z}) yaw {pose.YawDegrees}");
            }
        }

        [Test]
        public void Office_HasTerminalSlot()
        {
            var room = Instantiate(PrefabPath(RoomType.Office));
            Assert.IsNotEmpty(room.GetComponentsInChildren<TerminalSlot>());
        }

        [Test]
        public void Shell_EveryOpeningHasADoorFrame()
        {
            var shell = Instantiate(ShellPath);
            foreach (Direction side in System.Enum.GetValues(typeof(Direction)))
            {
                Assert.IsNotNull(shell.transform.Find($"Shell/Wall_{side}/DoorFrame"), $"{side} opening has no door frame");
            }
        }

        [Test]
        public void Shell_HasASuspendedCeilingAtTheCeilingPlane()
        {
            var shell = Instantiate(ShellPath);
            var grid = shell.transform.Find("Shell/SuspendedCeiling");
            Assert.IsNotNull(grid, "The shell has no suspended ceiling");
            foreach (var renderer in grid.GetComponentsInChildren<Renderer>())
            {
                // The grid may not lower the room noticeably: it hangs at most 2 cm below the ceiling plane.
                Assert.GreaterOrEqual(renderer.bounds.min.y, RoomContract.CeilingHeight - 0.02f, renderer.name);
                Assert.LessOrEqual(renderer.bounds.max.y, RoomContract.CeilingHeight + Epsilon, renderer.name);
            }
        }

        [Test]
        public void EveryRoom_LightsAreRealtimeWithoutShadows()
        {
            // Task 1.10: the map only exists at runtime, and only one or two lights may cast shadows.
            foreach (var type in RoomTypes)
            {
                var room = Instantiate(PrefabPath(type));
                foreach (var light in room.GetComponentsInChildren<Light>())
                {
                    Assert.AreEqual(LightShadows.None, light.shadows, $"{type}/{light.transform.parent.name} casts shadows");
                    Assert.AreEqual(LightmapBakeType.Realtime, light.lightmapBakeType, $"{type}/{light.transform.parent.name} is not realtime");
                }
            }
        }

        [Test]
        public void GrayboxRooms_AreLitByTheirOwnFittings()
        {
            foreach (var type in new[] { RoomType.Corridor, RoomType.ServerAisle, RoomType.Office })
            {
                var room = Instantiate(PrefabPath(type));
                Assert.GreaterOrEqual(room.GetComponentsInChildren<Light>().Length, 4, $"{type} has too few light fittings");
            }
        }

        [Test]
        public void GrayboxRooms_AreDressed()
        {
            foreach (var type in new[] { RoomType.Corridor, RoomType.ServerAisle, RoomType.Office })
            {
                var room = Instantiate(PrefabPath(type));
                Assert.IsNotNull(room.transform.Find("Dressing"), $"{type} has no Dressing");
            }
        }

        [Test]
        public void EveryRoom_KeepsTheCrossClear()
        {
            float half = RoomContract.KeepClearWidth / 2f;
            foreach (var type in RoomTypes)
            {
                var room = Instantiate(PrefabPath(type));
                foreach (var (name, b) in RoomContentBounds(room))
                {
                    // The walkway reaches up to the opening height; ceiling fittings above it are allowed.
                    if (b.min.y >= RoomContract.KeepClearHeight - Epsilon)
                    {
                        continue;
                    }
                    bool hitsStripAlongX = b.min.z < half - Epsilon && b.max.z > -half + Epsilon;
                    bool hitsStripAlongZ = b.min.x < half - Epsilon && b.max.x > -half + Epsilon;
                    Assert.IsFalse(hitsStripAlongX || hitsStripAlongZ, $"{type}/{name} blocks the keep-clear cross");
                }

                foreach (var slot in room.GetComponentsInChildren<TerminalSlot>())
                {
                    var p = slot.transform.position;
                    Assert.IsTrue(Mathf.Abs(p.x) > half && Mathf.Abs(p.z) > half, $"{type}/{slot.name} stands in the keep-clear cross");
                }
            }
        }

        [Test]
        public void EveryRoom_StaysInsideTheInterior()
        {
            float half = RoomContract.InteriorHalfSize + Epsilon;
            foreach (var type in RoomTypes)
            {
                var room = Instantiate(PrefabPath(type));
                foreach (var (name, b) in RoomContentBounds(room))
                {
                    bool inside = b.min.x >= -half && b.max.x <= half && b.min.z >= -half && b.max.z <= half &&
                                  b.min.y >= -Epsilon && b.max.y <= RoomContract.CeilingHeight + Epsilon;
                    Assert.IsTrue(inside, $"{type}/{name} sticks out of the room interior: {b}");
                }
            }
        }

        [Test]
        public void EveryRoom_LeavesRackSlotsFree()
        {
            var rack = new Bounds(new Vector3(0f, RoomContract.RackHeight / 2f, 0f),
                new Vector3(RoomContract.RackWidth, RoomContract.RackHeight, RoomContract.RackDepth));
            foreach (var type in RoomTypes)
            {
                var room = Instantiate(PrefabPath(type));
                foreach (var slot in room.GetComponentsInChildren<RackSlot>())
                {
                    var space = SlotSpace(slot.transform, rack);
                    foreach (var (name, b) in RoomContentBounds(room))
                    {
                        Assert.IsFalse(Overlaps(b, space), $"{type}/{name} blocks {slot.name}");
                    }
                }
            }
        }

        [Test]
        public void EveryRoom_LeavesSpaceToUseTerminals()
        {
            // The terminal itself (placeholder size until task 2.10) plus 1 m in front of it to stand in.
            const float standDepth = 1f;
            var terminal = new Bounds(new Vector3(0f, 1f, standDepth / 2f), new Vector3(0.8f, 2f, 0.6f + standDepth));
            foreach (var type in RoomTypes)
            {
                var room = Instantiate(PrefabPath(type));
                foreach (var slot in room.GetComponentsInChildren<TerminalSlot>())
                {
                    var space = SlotSpace(slot.transform, terminal);
                    foreach (var (name, b) in RoomContentBounds(room))
                    {
                        Assert.IsFalse(Overlaps(b, space), $"{type}/{name} blocks {slot.name}");
                    }
                }
            }
        }

        [Test]
        public void EveryRoomType_HasDefinitionPointingToItsPrefab()
        {
            foreach (var type in RoomTypes)
            {
                var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>($"Assets/_Project/Data/RoomDefinition_{type}.asset");
                Assert.IsNotNull(definition, $"RoomDefinition for {type} is missing");
                Assert.AreEqual(type, definition.Type);
                Assert.AreEqual(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(type)), definition.Prefab);
            }
        }

        private static string PrefabPath(RoomType type) => $"{RoomsFolder}/Room_{type}.prefab";

        /// <summary>World bounds of every renderer and collider in a room except the shell itself.</summary>
        private static IEnumerable<(string name, Bounds bounds)> RoomContentBounds(GameObject room)
        {
            var shell = room.transform.Find("Shell");
            Assert.IsNotNull(shell, $"{room.name} has no Shell");
            foreach (var renderer in room.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.transform.IsChildOf(shell))
                {
                    yield return (renderer.name, renderer.bounds);
                }
            }
            foreach (var collider in room.GetComponentsInChildren<Collider>())
            {
                if (!collider.transform.IsChildOf(shell))
                {
                    yield return ($"{collider.name} (collider)", collider.bounds);
                }
            }
        }

        /// <summary>World bounds of a slot-local box; slots only turn in 90° steps, so the box stays exact.</summary>
        private static Bounds SlotSpace(Transform slot, Bounds local)
        {
            var world = new Bounds(slot.TransformPoint(local.min), Vector3.zero);
            world.Encapsulate(slot.TransformPoint(local.max));
            return world;
        }

        private GameObject Instantiate(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"{path} is missing");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instances.Add(instance);
            return instance;
        }

        /// <summary>World bounds of the hole in one wall of a room at the origin.</summary>
        private static Bounds OpeningBounds(Direction side)
        {
            var pose = RoomContract.GetOpeningPose(side);
            bool alongX = side == Direction.North || side == Direction.South;
            var size = new Vector3(
                alongX ? RoomContract.OpeningWidth : RoomContract.WallThickness,
                RoomContract.OpeningHeight,
                alongX ? RoomContract.WallThickness : RoomContract.OpeningWidth);
            return new Bounds(new Vector3(pose.X, RoomContract.OpeningHeight / 2f, pose.Z), size);
        }

        private static bool Overlaps(Bounds a, Bounds b)
        {
            return a.min.x < b.max.x - Epsilon && b.min.x < a.max.x - Epsilon &&
                   a.min.y < b.max.y - Epsilon && b.min.y < a.max.y - Epsilon &&
                   a.min.z < b.max.z - Epsilon && b.min.z < a.max.z - Epsilon;
        }
    }
}
