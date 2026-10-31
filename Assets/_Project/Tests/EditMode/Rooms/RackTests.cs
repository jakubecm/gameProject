using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>The one rack prop (built by Tools/Cold Shutdown/Create Preview Scene), its door, and which racks a level makes openable.</summary>
    public class RackTests
    {
        private const string RackPath = "Assets/_Project/Prefabs/Props/Rack.prefab";
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
        public void OnlyOneRackPropExists()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(RackPath), "Rack.prefab is missing");
            foreach (var name in new[] { "RackCompute", "RackJbod", "RackMixed", "RackGpu", "RackNetwork" })
            {
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Props/{name}.prefab"), $"{name} still exists");
            }
        }

        [Test]
        public void Rack_FitsItsSlotAndEveryFillingHasLeds()
        {
            var rack = Create();
            // The body and the door together fill the slot: the body's collider stops behind the door, the door's covers the front.
            var body = rack.GetComponent<BoxCollider>();
            var door = (BoxCollider)rack.transform.Find("DoorHinge").GetComponent<Collider>();
            Assert.AreEqual(RoomContract.RackWidth, body.size.x, 0.001f);
            Assert.AreEqual(RoomContract.RackHeight, body.size.y, 0.001f);
            Assert.AreEqual(-RoomContract.RackDepth / 2f, body.center.z - body.size.z / 2f, 0.001f);
            float doorFront = rack.transform.InverseTransformPoint(door.transform.TransformPoint(door.center + Vector3.forward * door.size.z / 2f)).z;
            Assert.AreEqual(RoomContract.RackDepth / 2f, doorFront, 0.001f);
            Assert.AreEqual(doorFront - door.size.z, body.center.z + body.size.z / 2f, 0.001f, "The body's collider must stop behind the door");

            var library = AssetDatabase.LoadAssetAtPath<RackContentLibrary>("Assets/_Project/Data/RackContents.asset");
            Assert.IsNotNull(library, "The rack content library is missing");
            Assert.GreaterOrEqual(library.Count, 8, "Too few random fillings");
            var meshes = new HashSet<Mesh>();
            for (int i = 0; i < library.Count; i++)
            {
                var variant = library[i];
                Assert.IsNotNull(variant.mesh);
                Assert.AreEqual(variant.mesh.subMeshCount, variant.materials.Length);
                Assert.IsTrue(variant.materials.Any(m => m.name == "M_SafetyGreen"), $"Filling {i} has no status LEDs");
                Assert.IsTrue(variant.mesh.bounds.size.y > 1.7f, $"Filling {i} does not fill the rack");
                meshes.Add(variant.mesh);
            }
            Assert.AreEqual(library.Count, meshes.Count, "The fillings are not different meshes");
        }

        [Test]
        public void OnlyTheDoor_OffersToOpen()
        {
            var rack = Create();
            rack.GetComponent<Rack>().Configure(true, 1);
            Assert.IsEmpty(rack.GetComponent<Collider>().GetComponentsInParent<IInteractable>(), "Looking at the rack's side or back must not offer the door");
            var doorCollider = rack.transform.Find("DoorHinge").GetComponent<Collider>();
            Assert.IsTrue(doorCollider.enabled);
            Assert.IsInstanceOf<RackDoor>(doorCollider.GetComponentInParent<IInteractable>(), "Looking at the door offers it");
        }

        [Test]
        public void ClosedRack_HasNoDoorLogicAndAFlushHandle()
        {
            var rack = Create();
            rack.GetComponent<Rack>().Configure(false, 1);
            Assert.IsFalse(rack.GetComponent<Rack>().IsOpenable);
            Assert.IsFalse(rack.transform.Find("DoorHinge").GetComponent<RackDoor>().enabled, "A closed rack must not be interactable");
            Assert.IsTrue(rack.transform.Find("DoorHinge/HandleFlush").gameObject.activeSelf);
            Assert.IsFalse(rack.transform.Find("DoorHinge/HandleLever").gameObject.activeSelf);
            Assert.IsFalse(rack.transform.Find("DoorHinge").GetComponent<Collider>().enabled);
            Assert.IsFalse(rack.transform.Find("DoorHinge/DoorGlow").gameObject.activeSelf, "A closed rack does not glow");
            Assert.AreEqual(Quaternion.identity, rack.transform.Find("DoorHinge").localRotation);
        }

        [Test]
        public void OpenableRack_ShowsTheLeverHandle_StartsAjar_OpensFully_AndShutsCompletely()
        {
            var rack = Create();
            rack.GetComponent<Rack>().Configure(true, 1);
            var door = rack.transform.Find("DoorHinge").GetComponent<RackDoor>();
            Assert.IsTrue(door.enabled);
            Assert.IsTrue(rack.transform.Find("DoorHinge/HandleLever").gameObject.activeSelf, "The lever handle shows");
            Assert.IsFalse(rack.transform.Find("DoorHinge/HandleFlush").gameObject.activeSelf);
            var glow = rack.transform.Find("DoorHinge/DoorGlow").GetComponent<RackGlow>();
            Assert.IsTrue(rack.transform.Find("DoorHinge/DoorGlow").gameObject.activeSelf, "An openable rack carries the glow");
            glow.Refresh();
            Assert.IsFalse(glow.IsLit, "The glow is off while the door stands ajar");
            Assert.AreEqual(InteractKey.Secondary, door.Key, "Rack doors are used with F");
            Assert.IsFalse(door.IsOpen);
            Assert.Greater(door.Angle, 0f, "The door starts ajar");
            Assert.Less(door.Angle, door.OpenAngle);

            door.Interact(null);
            door.Step(10f);
            Assert.IsTrue(door.IsOpen);
            Assert.AreEqual(door.OpenAngle, door.Angle, 0.01f);

            door.Interact(null);
            door.Step(10f);
            Assert.IsFalse(door.IsOpen);
            Assert.AreEqual(0f, door.Angle, 0.01f, "E on an open door shuts it completely");
            glow.Refresh();
            Assert.IsTrue(glow.IsLit, "The glow shows once the door is shut perfectly");
            Assert.IsTrue(rack.transform.Find("DoorHinge/HandleLever").gameObject.activeSelf, "The lever stays out when the door is shut");

            door.Interact(null);
            door.Step(10f);
            Assert.AreEqual(door.OpenAngle, door.Angle, 0.01f, "It opens again, repeatedly");
        }

        [Test]
        public void SecurityOffice_HasExactlyOneOpenableRack()
        {
            var room = new GameObject($"Room_{RoomType.SecurityOffice}");
            created.Add(room);
            var group = new GameObject("RackSlots");
            group.transform.SetParent(room.transform, false);
            foreach (var z in new[] { -3.6f, -4.2f })
            {
                MakeSlot(group.transform, new Vector3(-2.6f, 0f, z));
            }
            var racks = RackPlacement.Assign(room.GetComponentsInChildren<RackSlot>(), slot => MakeRack(slot), 7);
            Assert.AreEqual(2, racks.Count);
            Assert.AreEqual(1, racks.Count(r => r.IsOpenable));
        }

        [Test]
        public void ServerAisle_HasOneOpenableRackOnEachSide()
        {
            var room = new GameObject("Room_Hall");
            created.Add(room);
            var group = new GameObject("RackSlots");
            group.transform.SetParent(room.transform, false);
            foreach (var pose in HallContract.GetPodRackSlots())
            {
                MakeSlot(group.transform, new Vector3(pose.X, 0f, pose.Z));
            }
            var racks = RackPlacement.Assign(room.GetComponentsInChildren<RackSlot>(), slot => MakeRack(slot), 3);
            Assert.AreEqual(HallContract.TileRackSlots, racks.Count);
            // Two pods of two rows: one openable rack per row, so two per aisle, one on each side.
            var openable = racks.Where(r => r.IsOpenable).Select(r => r.transform.position).ToList();
            Assert.AreEqual(4, openable.Count);
            for (int pod = 0; pod < HallContract.PodsPerTile; pod++)
            {
                float center = HallContract.PodZ(pod);
                Assert.AreEqual(1, openable.Count(p => p.z > center && p.z < center + 2f), $"Pod {pod}: north row");
                Assert.AreEqual(1, openable.Count(p => p.z < center && p.z > center - 2f), $"Pod {pod}: south row");
            }

            // The openable rack is never the first or the last of its row.
            float rowEnd = HallContract.PodHalfLength - RoomContract.RackWidth / 2f;
            foreach (var position in openable)
            {
                Assert.Less(Mathf.Abs(position.x), rowEnd - 0.01f, $"An openable rack stands at the end of its row: {position}");
            }
            for (int seed = 0; seed < 50; seed++)
            {
                foreach (var rack in RackPlacement.Assign(room.GetComponentsInChildren<RackSlot>(), slot => MakeRack(slot), seed).Where(r => r.IsOpenable))
                {
                    Assert.Less(Mathf.Abs(rack.transform.position.x), rowEnd - 0.01f, $"Seed {seed}: an openable rack stands at the end of its row");
                }
            }

            // The same seed makes the same racks.
            var again = RackPlacement.Assign(room.GetComponentsInChildren<RackSlot>(), slot => MakeRack(slot), 3);
            CollectionAssert.AreEqual(racks.Select(r => r.IsOpenable), again.Select(r => r.IsOpenable));
        }

        private static void MakeSlot(Transform parent, Vector3 position)
        {
            var slot = new GameObject("RackSlot", typeof(RackSlot)).transform;
            slot.SetParent(parent, false);
            slot.localPosition = position;
        }

        private Rack MakeRack(RackSlot slot)
        {
            var rack = new GameObject("Rack", typeof(Rack));
            created.Add(rack);
            rack.transform.position = slot.transform.position;
            return rack.GetComponent<Rack>();
        }

        private GameObject Create()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RackPath);
            Assert.IsNotNull(prefab, "Rack.prefab is missing");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            created.Add(instance);
            return instance;
        }
    }
}
