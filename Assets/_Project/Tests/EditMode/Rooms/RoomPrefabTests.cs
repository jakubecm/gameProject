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
        public void EveryRoom_KeepsTheCrossClear()
        {
            float half = RoomContract.KeepClearWidth / 2f;
            foreach (var type in RoomTypes)
            {
                var room = Instantiate(PrefabPath(type));
                var shell = room.transform.Find("Shell");
                Assert.IsNotNull(shell, $"{type} has no Shell");

                foreach (var renderer in room.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.transform.IsChildOf(shell))
                    {
                        continue;
                    }
                    var b = renderer.bounds;
                    bool hitsStripAlongX = b.min.z < half - Epsilon && b.max.z > -half + Epsilon;
                    bool hitsStripAlongZ = b.min.x < half - Epsilon && b.max.x > -half + Epsilon;
                    Assert.IsFalse(hitsStripAlongX || hitsStripAlongZ, $"{type}/{renderer.name} blocks the keep-clear cross");
                }

                foreach (var slot in room.GetComponentsInChildren<TerminalSlot>())
                {
                    var p = slot.transform.position;
                    Assert.IsTrue(Mathf.Abs(p.x) > half && Mathf.Abs(p.z) > half, $"{type}/{slot.name} stands in the keep-clear cross");
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
