using System.Collections.Generic;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>Drives the automatic door prefab (built by Tools/Cold Shutdown/Create Preview Scene) without Play mode.</summary>
    public class AutoSlidingDoorTests
    {
        private const string DoorPath = "Assets/_Project/Prefabs/Props/RoomDoor.prefab";
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
        public void Door_OpensWhileSomeoneIsNearAndClosesAfterwards()
        {
            var door = CreateDoor(out var left, out var right);
            float leftClosed = left.localPosition.x;
            float rightClosed = right.localPosition.x;

            for (int i = 0; i < 20; i++) door.Step(true, 0.05f);
            Assert.IsTrue(door.IsOpen);
            Assert.AreEqual(leftClosed - door.Travel, left.localPosition.x, 0.001f, "The left leaf slides to -X");
            Assert.AreEqual(rightClosed + door.Travel, right.localPosition.x, 0.001f, "The right leaf slides to +X");

            for (int i = 0; i < 60; i++) door.Step(false, 0.05f);
            Assert.IsTrue(door.IsClosed);
            Assert.AreEqual(leftClosed, left.localPosition.x, 0.001f);
            Assert.AreEqual(rightClosed, right.localPosition.x, 0.001f);
        }

        [Test]
        public void LockedDoor_StaysShutAndShowsRed()
        {
            var door = CreateDoor(out _, out _);
            var leds = door.GetComponentsInChildren<Renderer>();
            door.IsLocked = true;
            for (int i = 0; i < 20; i++) door.Step(true, 0.05f);
            Assert.IsTrue(door.IsClosed);
            bool red = false;
            foreach (var led in leds)
            {
                if (led.name == "Led") red |= led.sharedMaterial.name == "M_LedRed";
            }
            Assert.IsTrue(red, "The reader LED is not red");

            door.IsLocked = false;
            for (int i = 0; i < 20; i++) door.Step(true, 0.05f);
            Assert.IsTrue(door.IsOpen);
        }

        [Test]
        public void PhotoCells_SeeACharacterOnEitherSide()
        {
            var door = CreateDoor(out _, out _);
            var person = new GameObject("Person", typeof(CharacterController));
            created.Add(person);
            foreach (float z in new[] { 1.2f, -1.4f })
            {
                person.transform.position = new Vector3(0.3f, 1f, z);
                Physics.SyncTransforms();
                Assert.IsTrue(door.SenseSomeone(), $"Nobody seen at z = {z}");
            }
            person.transform.position = new Vector3(0f, 1f, 4f);
            Physics.SyncTransforms();
            Assert.IsFalse(door.SenseSomeone(), "Someone seen far away");
        }

        private AutoSlidingDoor CreateDoor(out Transform left, out Transform right)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPath);
            Assert.IsNotNull(prefab, $"{DoorPath} is missing");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            created.Add(instance);
            left = instance.transform.Find("Leaf_Left");
            right = instance.transform.Find("Leaf_Right");
            Assert.IsNotNull(left);
            Assert.IsNotNull(right);
            var door = instance.GetComponent<AutoSlidingDoor>();
            Assert.IsFalse(door.IsLocked, "Doors start unlocked");
            return door;
        }
    }
}
