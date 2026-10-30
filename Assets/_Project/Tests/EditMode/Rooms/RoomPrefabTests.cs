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
    /// Tools/Cold Shutdown/Create Preview Scene.
    /// </summary>
    public class RoomPrefabTests
    {
        private const float Epsilon = 0.001f;
        private const string RoomsFolder = "Assets/_Project/Prefabs/Rooms";
        private const string ShellPath = RoomsFolder + "/RoomShell.prefab";
        private const string WallPlugPath = RoomsFolder + "/WallPlug.prefab";

        private static readonly RoomType[] RoomTypes = (RoomType[])System.Enum.GetValues(typeof(RoomType));

        /// <summary>Rooms built from the shell. Hall tiles have their own contract and tests (HallKitTests).</summary>
        private static readonly RoomType[] ShellRoomTypes = RoomTypes.Where(type => type != RoomType.Hall).ToArray();

        
        /// <summary>Every layout prefab of every shell room: rooms have four, corridors one.</summary>
        private static IEnumerable<(RoomType type, int layout, string path)> Layouts(IEnumerable<RoomType> types) =>
            types.SelectMany(type => Enumerable.Range(0, RoomRules.LayoutCount(type)).Select(layout => (type, layout, LayoutPath(type, layout))));

        private static string LayoutPath(RoomType type, int layout) =>
            layout == 0 ? PrefabPath(type) : $"{RoomsFolder}/Room_{type}_Layout{layout + 1}.prefab";

        private static float SmallSide => RoomContract.SmallRoomHalfWidth;

        /// <summary>Floor paint (markings) may lie in walkways and pockets; anything from 1 cm up counts.</summary>
        private const float WalkFloor = 0.01f;

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
        public void SecurityOffice_HasTwoRackSlotsSideBySide()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.SecurityOffice }))
            {
                var room = Instantiate(path);
                var slots = room.GetComponentsInChildren<RackSlot>();
                Assert.AreEqual(2, slots.Length, $"Layout {layout + 1}: the security office needs a compute rack and a JBOD, no more");
                Assert.AreEqual(RoomContract.RackWidth, Mathf.Abs(slots[0].transform.position.z - slots[1].transform.position.z), Epsilon);
                foreach (var slot in slots)
                {
                    // Backs against a side wall, fronts facing into the room.
                    Assert.AreEqual(SmallSide - RoomContract.RackDepth / 2f, Mathf.Abs(slot.transform.position.x), Epsilon);
                    Assert.AreEqual(slot.transform.position.x < 0f ? 90f : 270f, slot.transform.eulerAngles.y, Epsilon);
                    Assert.Less(slot.transform.position.z, RoomContract.SmallRoomBackWall);
                }
            }
        }

        [Test]
        public void SmallRooms_AreStrictlySquare()
        {
            Assert.AreEqual(RoomContract.SmallRoomDepth, 2f * SmallSide, Epsilon, "A small room is as wide as it is deep");
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes.Where(RoomRules.IsSmallRoom)))
            {
                var room = Instantiate(path);
                string what = $"{type} layout {layout + 1}";
                var wall = room.transform.Find("Structure/BackWall");
                Assert.IsNotNull(wall, $"{what} has no back wall");
                var bounds = wall.GetComponent<Renderer>().bounds;
                Assert.AreEqual(RoomContract.SmallRoomBackWall, bounds.min.z, Epsilon, what);
                Assert.AreEqual(RoomContract.InteriorSize, bounds.size.x, Epsilon, what);
                Assert.AreEqual(RoomContract.CeilingHeight, bounds.max.y, Epsilon, what);
                foreach (var (name, sign) in new[] { ("SideWall_W", -1f), ("SideWall_E", 1f) })
                {
                    var side = room.transform.Find($"Structure/{name}");
                    Assert.IsNotNull(side, $"{what} has no {name}");
                    var b = side.GetComponent<Renderer>().bounds;
                    Assert.AreEqual(SmallSide, sign < 0 ? -b.max.x : b.min.x, Epsilon, $"{what}/{name}");
                    Assert.AreEqual(-RoomContract.InteriorHalfSize, b.min.z, Epsilon, $"{what}/{name}");
                    Assert.GreaterOrEqual(b.max.z, RoomContract.SmallRoomBackWall, $"{what}/{name}");
                }
            }
        }

        [Test]
        public void TechnicalRooms_HaveNoSuspendedCeiling()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.PowerRoom, RoomType.CoolingPlant, RoomType.StorageRoom }))
            {
                var room = Instantiate(path);
                Assert.IsFalse(room.transform.Find("Shell/SuspendedCeiling").gameObject.activeSelf, $"{type} layout {layout + 1} still has a suspended ceiling");
            }
            Assert.IsTrue(Instantiate(PrefabPath(RoomType.Office)).transform.Find("Shell/SuspendedCeiling").gameObject.activeSelf);
        }

        [Test]
        public void EveryRoom_HasSprinklersDetectorsAndAnExtinguisher()
        {
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                foreach (var prop in new[] { "Sprinkler", "SmokeDetector", "FireExtinguisher" })
                {
                    Assert.IsNotEmpty(PropInstances(room, prop), $"{type} layout {layout + 1} has no {prop}");
                }
                // A light switch everywhere, but no more than one per door side (one per corridor cell).
                int switches = PropInstances(room, "LightSwitch").Count();
                int allowedSwitches = RoomRules.IsCorridor(type) ? 1 : RoomRules.BitCount(RoomRules.DoorSides(type));
                Assert.That(switches, Is.InRange(1, allowedSwitches), $"{type} layout {layout + 1} has {switches} light switches");
                // Not too many: one detector per corridor cell or small room.
                int detectors = PropInstances(room, "SmokeDetector").Count();
                Assert.LessOrEqual(detectors, 1, $"{type} layout {layout + 1} has {detectors} smoke detectors");
                int sprinklers = PropInstances(room, "Sprinkler").Count();
                Assert.LessOrEqual(sprinklers, RoomRules.IsSmallRoom(type) ? 4 : 2 * RoomRules.BitCount(RoomRules.DoorSides(type)) + 1,
                    $"{type} layout {layout + 1} has {sprinklers} sprinklers");
            }
        }

        [Test]
        public void Corridors_HaveCamerasOnlyByTheCrossingAndOneGrillePerCell()
        {
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes.Where(RoomRules.IsCorridor)))
            {
                var room = Instantiate(path);
                var cameras = room.GetComponentsInChildren<SecurityCamera>();
                bool watched = type == RoomType.CorridorCross || type == RoomType.CorridorT || type == RoomType.CorridorCorner;
                Assert.AreEqual(watched ? 1 : 0, cameras.Length, $"{type} has {cameras.Length} cameras");
                foreach (var camera in cameras)
                {
                    var p = camera.transform.position;
                    Assert.LessOrEqual(Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z)), Epsilon, $"{type}/{camera.name} is not in the middle of the crossing");
                }
                int grilles = PropInstances(room, "FloorVentGrille").Count() + PropInstances(room, "WallVentGrille").Count();
                Assert.AreEqual(1, grilles, $"{type} needs exactly one ventilation grille");
                // The large square one at the floor, in the middle of its wall (a partition along an arm, or the wall that
                // closes a missing arm), away from the fire kit, the light switch right above it.
                var grille = PropInstances(room, "FloorVentGrille").Single().transform.position;
                float across = Mathf.Min(Mathf.Abs(grille.x), Mathf.Abs(grille.z));
                float along = Mathf.Max(Mathf.Abs(grille.x), Mathf.Abs(grille.z));
                float armMiddle = (1.5f + RoomContract.InteriorHalfSize) / 2f;
                bool centered = (across < Epsilon && Mathf.Abs(along - 1.5f) < Epsilon) || (Mathf.Abs(across - 1.5f) < Epsilon && Mathf.Abs(along - armMiddle) < Epsilon);
                Assert.IsTrue(centered, $"{type} layout {layout + 1}: the grille is not in the middle of its wall: {grille}");
                foreach (var fire in new[] { "FireExtinguisher", "HoseCabinet", "FireCallPoint" }.SelectMany(name => PropInstances(room, name)))
                {
                    Assert.Greater(Vector3.Distance(Flat(fire.transform.position), Flat(grille)), 3f, $"{type} layout {layout + 1}: the grille is next to {fire.name}");
                }
                var lightSwitch = PropInstances(room, "LightSwitch").Single().transform.position;
                Assert.AreEqual(0f, Vector3.Distance(Flat(lightSwitch), Flat(grille)), Epsilon, $"{type} layout {layout + 1}: the light switch is not above the grille");
                // Sprinklers in a corridor sit on its axis, in line with the lights.
                foreach (var sprinkler in PropInstances(room, "Sprinkler"))
                {
                    var p = sprinkler.transform.position;
                    Assert.Less(Mathf.Min(Mathf.Abs(p.x), Mathf.Abs(p.z)), Epsilon, $"{type}/{sprinkler.name} is off the corridor axis: {p}");
                }
                // No duct shows in a corridor.
                foreach (var renderer in room.GetComponentsInChildren<Renderer>())
                {
                    Assert.IsFalse(renderer.name.Contains("Duct"), $"{type}/{renderer.name} is a visible duct");
                }
            }
        }

        [Test]
        public void Corridors_HaveTheFireKitInTheirFireArm()
        {
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes.Where(RoomRules.IsCorridor)))
            {
                var room = Instantiate(path);
                var arm = room.transform.Find($"Dressing/Arm_{RoomRules.FireArmSide(type, layout)}");
                Assert.IsNotNull(arm, $"{type} layout {layout + 1} has no fire arm");
                foreach (var name in new[] { "FireExtinguisher", "HoseCabinet", "FireCallPoint" })
                {
                    var kit = PropInstances(room, name).Single();
                    Assert.IsTrue(kit.transform.IsChildOf(arm), $"{type} layout {layout + 1}: the {name} is not in the fire arm");
                }
            }
        }

        [Test]
        public void VentGrilles_AreAsFarFromTheFloorAsFromTheCeiling()
        {
            const float gap = 0.15f;
            var low = Instantiate($"{PropsFolder}/FloorVentGrille.prefab");
            var bounds = RendererBounds(low);
            Assert.AreEqual(bounds.size.x, bounds.size.y, 0.01f, "The large grille is not square");
            Assert.That(bounds.size.y, Is.InRange(0.55f, 0.7f), "A person should just fit through the large grille");
            Assert.AreEqual(gap, bounds.min.y, 0.005f, "The large grille is not at the floor");
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                foreach (var grille in PropInstances(room, "WallVentGrille"))
                {
                    Assert.AreEqual(RoomContract.CeilingHeight - gap, RendererBounds(grille).max.y, 0.005f, $"{type} layout {layout + 1}/{grille.name}");
                }
                foreach (var grille in PropInstances(room, "FloorVentGrille"))
                {
                    Assert.AreEqual(gap, RendererBounds(grille).min.y, 0.005f, $"{type} layout {layout + 1}/{grille.name}");
                }
            }
        }

        [Test]
        public void RoomDoor_ShowsTheExitSignOnlyOnTheRoomSide()
        {
            // The door's +Z is the owner's side: the room, whose way out leads through it into the corridor.
            var door = Instantiate($"{PropsFolder}/RoomDoor.prefab");
            var signs = PropInstances(door, "ExitSign").ToList();
            Assert.AreEqual(1, signs.Count);
            Assert.Greater(signs[0].transform.localPosition.z, RoomContract.WallThickness / 2f, "The exit sign is not on the room side");
            Assert.Greater(signs[0].transform.forward.z, 0.99f, "The exit sign does not face into the room");
        }

        [Test]
        public void LiftEnd_OpensTheLiftOntoTheCorridor_WithAnExitSign_AndThePlayerStartsInTheCar()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.LiftEnd }))
            {
                var room = Instantiate(path);
                var lift = PropInstances(room, "PassengerLift").Single();
                var door = lift.GetComponent<AutoSlidingDoor>();
                Assert.IsNotNull(door, "The lift door does not open by itself");
                Assert.IsNotNull(room.transform.Find("Structure/LiftWall_Lintel"), "The lift has no landing wall");
                var sign = PropInstances(lift, "ExitSign").Single();
                Assert.LessOrEqual(sign.transform.position.z, RoomContract.LiftWallFace + Epsilon, "The exit sign is not on the lobby side");
                Assert.Less(sign.transform.forward.z, -0.99f, "The exit sign does not face the lobby");
                Assert.Less(Mathf.Abs(sign.transform.position.x), Epsilon, "The exit sign is not over the lift door");
                Assert.Greater(sign.transform.position.y, RoomContract.LiftDoorHeight, "The exit sign is not over the lift door");
                Assert.IsEmpty(PropInstances(room, "RoomDoor"), "The lift end is a corridor: no glass door");
                // No room in between: the lift door is the closed end of the corridor arm.
                Assert.AreEqual(RoomDressingCorridorHalfWidth, RoomContract.LiftWallFace, Epsilon, "The lift does not close the corridor's crossing");
                Assert.LessOrEqual(RoomContract.LiftCarWidth, 2f * RoomDressingCorridorHalfWidth - 0.2f, "The car does not fit behind the corridor");
                // Room for 6 to 8 people: at least 2 m² and a door wide enough for two.
                Assert.GreaterOrEqual(RoomContract.LiftCarWidth * RoomContract.LiftCarDepth, 2f);
                Assert.GreaterOrEqual(RoomContract.LiftDoorWidth, 1.1f - Epsilon);

                // The photo cells see someone in front of the door, but not the player standing in the car at the start.
                var player = new GameObject("TestPlayer");
                instances.Add(player);
                var body = player.AddComponent<CharacterController>();
                body.height = 1.8f;
                body.radius = 0.3f;
                body.center = new Vector3(0f, 0.9f, 0f);
                var start = RoomContract.LiftStartPose;
                player.transform.position = new Vector3(start.X, 0f, start.Z);
                Physics.SyncTransforms();
                Assert.IsFalse(door.SenseSomeone(), "The lift door opens while the player stands in the middle of the car");
                player.transform.position = new Vector3(0f, 0f, RoomContract.LiftWallFace - 0.6f);
                Physics.SyncTransforms();
                Assert.IsTrue(door.SenseSomeone(), "The lift door does not open for someone in front of it");
                player.transform.position = new Vector3(0f, 0f, RoomContract.LiftCarFront + 0.35f);
                Physics.SyncTransforms();
                Assert.IsTrue(door.SenseSomeone(), "The lift door does not open for someone walking up to it in the car");
            }
        }

        [Test]
        public void TechnicalRooms_HaveTwoBattensOnTheSprinklerAxesAndTwoShaftGrillesOppositeTheDoor()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.PowerRoom, RoomType.CoolingPlant, RoomType.StorageRoom }))
            {
                var room = Instantiate(path);
                string what = $"{type} layout {layout + 1}";
                var battens = PropInstances(room, "LedBatten").ToList();
                Assert.AreEqual(2, battens.Count, $"{what}: two LED battens");
                foreach (var batten in battens)
                {
                    var local = room.transform.InverseTransformPoint(batten.transform.position);
                    Assert.AreEqual(1.2f, Mathf.Abs(local.x), 0.01f, $"{what}: batten not on a sprinkler axis");
                }
                Assert.AreEqual(2, PropInstances(room, "WallVentGrille").Count(), $"{what}: two shaft grilles");
                var detector = room.transform.InverseTransformPoint(PropInstances(room, "SmokeDetector").Single().transform.position);
                Assert.AreEqual(0f, detector.x, 0.01f, $"{what}: detector off the middle");
                Assert.AreEqual(-3.0f, detector.z, 0.01f, $"{what}: detector off the middle");
                if (type != RoomType.StorageRoom)
                {
                    var panel = room.transform.InverseTransformPoint(PropInstances(room, type == RoomType.CoolingPlant ? "CoolingMeterPanel" : "PowerMeterPanel").Single().transform.position);
                    Assert.AreEqual(0f, panel.x, 0.01f, $"{what}: meter panel not opposite the door");
                    Assert.AreEqual(0f, panel.z, 0.01f, $"{what}: meter panel not on the back wall");
                }
            }
        }

        [Test]
        public void TechnicalRooms_HaveTheirPlantAndAnalogMeters()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.PowerRoom, RoomType.CoolingPlant }))
            {
                var room = Instantiate(path);
                string what = $"{type} layout {layout + 1}";
                var panels = PropInstances(room, type == RoomType.CoolingPlant ? "CoolingMeterPanel" : "PowerMeterPanel").ToList();
                Assert.IsNotEmpty(panels, $"{what} has no meter panel");
                Assert.GreaterOrEqual(panels[0].GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("Thermometer_")), 6, $"{what} has too few thermometers");
                Assert.IsEmpty(panels[0].GetComponentsInChildren<AnalogGauge>(), $"{what}: the thermometer panel is only a look, it has no logic");
                if (type == RoomType.PowerRoom)
                {
                    Assert.AreEqual(2, PropInstances(room, "Transformer").Count(), $"{what} needs two transformers");
                    Assert.GreaterOrEqual(PropInstances(room, "Switchgear").Count(), 2, $"{what} needs switchgear");
                }
                else
                {
                    Assert.AreEqual(2, PropInstances(room, "HeatExchanger").Count() + PropInstances(room, "HeatExchangerMirrored").Count(), $"{what} needs two heat exchangers");
                    Assert.AreEqual(2, PropInstances(room, "CirculationPump").Count(), $"{what} needs two pumps");
                }
                foreach (var banned in new[] { "Desk", "Shelf", "FilingCabinet", "CoffeeCounter" })
                {
                    Assert.IsEmpty(PropInstances(room, banned), $"{what} has a {banned}");
                }
            }
        }

        [Test]
        public void Offices_HaveTerminalSlot()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.Office, RoomType.SecurityOffice }))
            {
                Assert.AreEqual(1, Instantiate(path).GetComponentsInChildren<TerminalSlot>().Length, $"{type} layout {layout + 1}");
            }
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
            foreach (var (type, _, path) in Layouts(RoomTypes))
            {
                var room = Instantiate(path);
                foreach (var light in room.GetComponentsInChildren<Light>())
                {
                    Assert.AreEqual(LightShadows.None, light.shadows, $"{type}/{light.transform.parent.name} casts shadows");
                    Assert.AreEqual(LightmapBakeType.Realtime, light.lightmapBakeType, $"{type}/{light.transform.parent.name} is not realtime");
                }
            }
        }

        [Test]
        public void Rooms_AreLitByTheirOwnFittings()
        {
            foreach (var (type, _, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                int lights = room.GetComponentsInChildren<Light>().Length;
                // Few lights save GPU time; every arm of a corridor and every part of a room still gets one.
                int needed = RoomRules.IsCorridor(type) ? RoomRules.BitCount(RoomRules.DoorSides(type)) + 1 : RoomRules.IsSmallRoom(type) ? 3 : 6;
                Assert.GreaterOrEqual(lights, needed, $"{type} has too few light fittings");
                Assert.LessOrEqual(lights, 10, $"{type} has more lights than the GPU budget allows");
            }
        }

        [Test]
        public void Rooms_AreDressed()
        {
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                Assert.IsNotNull(room.transform.Find("Dressing"), $"{type} layout {layout + 1} has no Dressing");
            }
        }

        [Test]
        public void RoomLayouts_AreDifferent()
        {
            // Every layout of a room type is its own arrangement: no two layouts have their furniture in the same places.
            foreach (var type in ShellRoomTypes.Where(t => RoomRules.LayoutCount(t) > 1))
            {
                var seen = new HashSet<string>();
                for (int layout = 0; layout < RoomRules.LayoutCount(type); layout++)
                {
                    var room = Instantiate(LayoutPath(type, layout));
                    var signature = string.Join(";", room.transform.Find("Dressing").GetComponentsInChildren<Transform>()
                        .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                        .Select(t => $"{t.name}@{t.position.x:0.0},{t.position.z:0.0}").OrderBy(x => x));
                    Assert.IsTrue(seen.Add(signature), $"{type} layout {layout + 1} repeats an earlier layout");
                }
            }
        }

        [Test]
        public void Rooms_HaveSecurityCameras()
        {
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                var cameras = room.transform.Find("SecurityCameras");
                Assert.IsNotNull(cameras, $"{type} has no SecurityCameras group");
                var found = cameras.GetComponentsInChildren<SecurityCamera>();
                if (!RoomRules.IsCorridor(type) || type == RoomType.CorridorCross || type == RoomType.CorridorT || type == RoomType.CorridorCorner) Assert.IsNotEmpty(found, $"{type} has no camera");
                Assert.LessOrEqual(found.Length, 2, $"{type} layout {layout + 1} has too many cameras");
                if (RoomRules.IsSmallRoom(type)) Assert.AreEqual(1, found.Length, $"{type} layout {layout + 1}: a small room has one camera");
                foreach (var camera in found)
                {
                    Assert.IsNotNull(camera.FeedCamera, $"{type}/{camera.name} has no feed camera");
                    Assert.IsFalse(camera.FeedCamera.enabled, $"{type}/{camera.name} renders all the time");
                }
            }
        }

        [Test]
        public void SecurityOffice_HasAVideoWallAndATerminal()
        {
            foreach (var (_, layout, path) in Layouts(new[] { RoomType.SecurityOffice }))
            {
                var room = Instantiate(path);
                var wall = room.GetComponentInChildren<SecurityMonitorWall>();
                Assert.IsNotNull(wall, "The security office has no video wall");
                var screens = wall.GetComponentsInChildren<SecurityMonitor>();
                Assert.GreaterOrEqual(screens.Length, 6);
                // Across the whole back wall, all on it.
                float span = screens.Max(m => m.transform.position.x) - screens.Min(m => m.transform.position.x) + 1.23f;
                Assert.GreaterOrEqual(span, 0.8f * 2f * SmallSide, $"Layout {layout + 1}: the video wall does not cover the back wall");
                Assert.IsTrue(screens.All(m => Mathf.Abs(m.transform.position.z - RoomContract.SmallRoomBackWall) < 0.01f), "A screen is not on the back wall");
                Assert.IsNotEmpty(room.GetComponentsInChildren<TerminalSlot>());
            }
        }

        [Test]
        public void Furniture_StaysInTheRoomsItBelongsTo()
        {
            foreach (var (type, _, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                // Shelving only in the storage room, lockers and benches only in the security office.
                if (type != RoomType.StorageRoom)
                {
                    Assert.IsEmpty(PropInstances(room, "Shelf"), $"{type} has shelving");
                }
                if (type != RoomType.SecurityOffice)
                {
                    Assert.IsEmpty(PropInstances(room, "Locker"), $"{type} has lockers");
                    Assert.IsEmpty(PropInstances(room, "LockerBench"), $"{type} has a locker bench");
                }
            }
            // Service carts, laptops and cable drums are gone.
            foreach (var name in new[] { "ServiceCart", "Laptop", "CableSpool", "Radiator", "Whiteboard" })
            {
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Props/{name}.prefab"), $"{name} still exists");
            }
        }

        [Test]
        public void Offices_HaveEverythingAnOfficeNeeds()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.Office }))
            {
                var room = Instantiate(path);
                var needed = new[] { "Workstation", "Printer", "FilingCabinet", "CeilingSpeaker", "MeetingTable", "WallTv",
                    "CoffeeCounter", "CoffeeMachine", "Fridge", "PhotoFrame", "Mug", "PaperStack" };
                foreach (var prop in needed)
                {
                    Assert.IsNotEmpty(PropInstances(room, prop), $"{type} layout {layout + 1} has no {prop}");
                }
            }
        }

        [Test]
        public void Desks_LeaveRoomForTheirChairAndToWalkPast()
        {
            // Behind every chair (away from its desk or table) there is 0.4 m that nothing stands in and that is still in the room.
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes.Where(t => !RoomRules.IsCorridor(t))))
            {
                var room = Instantiate(path);
                Physics.SyncTransforms();
                var solids = SolidProps(room).ToList();
                foreach (var chair in PropInstances(room, "OfficeChair"))
                {
                    var position = chair.transform.position;
                    var forward = chair.transform.forward;
                    var right = chair.transform.right;
                    var space = new Bounds(position - forward * 0.5f - right * 0.25f + Vector3.up * 0.1f, Vector3.zero);
                    space.Encapsulate(position - forward * 0.9f + right * 0.25f + Vector3.up * 1.1f);
                    Assert.IsTrue(InsideInterior(type, space), $"{type} layout {layout + 1}: {chair.name} stands against a wall");
                    foreach (var (prop, bounds) in solids)
                    {
                        if (prop == chair) continue;
                        Assert.IsFalse(Overlaps(bounds, space), $"{type} layout {layout + 1}: {prop.name} stands right behind {chair.name}");
                    }
                }
            }
        }

        [Test]
        public void FloorBoxes_LieUnderTheDesksTheyFeed()
        {
            foreach (var (type, layout, path) in Layouts(new[] { RoomType.SecurityOffice }))
            {
                var room = Instantiate(path);
                Physics.SyncTransforms();
                var desks = PropInstances(room, "Desk").Select(d => d.GetComponent<Collider>().bounds).ToList();
                foreach (var box in PropInstances(room, "FloorBox"))
                {
                    var p = box.transform.position;
                    Assert.IsTrue(desks.Any(d => d.min.x + 0.1f < p.x && p.x < d.max.x - 0.1f && d.min.z + 0.1f < p.z && p.z < d.max.z - 0.1f),
                        $"{type} layout {layout + 1}: {box.name} is not under a desk");
                }
            }
        }

        [Test]
        public void SecurityOffice_HasOnlyConsolesRacksAndLockersOrCoffee()
        {
            var allowed = new[] { "Desk", "Workstation", "PcTower", "OfficeChair", "FloorBox", "PaperStack", "Locker", "LockerBench", "CoffeeCounter",
                "CoffeeMachine", "Mug", "PowerSocket", "DataSocket", "LedPanel", "CeilingDiffuser", "CeilingSpeaker", "Sprinkler", "SmokeDetector",
                "FireExtinguisher", "FireCallPoint", "LightSwitch" };
            foreach (var (_, layout, path) in Layouts(new[] { RoomType.SecurityOffice }))
            {
                var room = Instantiate(path);
                Assert.AreEqual(2, PropInstances(room, "Desk").Count(), $"Layout {layout + 1}: two desks");
                Assert.AreEqual(2, PropInstances(room, "Workstation").Count(), $"Layout {layout + 1}: two desks with two monitors");
                Assert.AreEqual(2, room.GetComponentsInChildren<RackSlot>().Length);
                bool lockers = PropInstances(room, "Locker").Count() >= 2 && PropInstances(room, "LockerBench").Any();
                bool coffee = PropInstances(room, "CoffeeCounter").Any();
                Assert.IsTrue(lockers || coffee, $"Layout {layout + 1} has neither lockers nor a coffee corner");
                foreach (var t in room.transform.Find("Dressing").GetComponentsInChildren<Transform>())
                {
                    if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                    string prop = System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));
                    Assert.Contains(prop, allowed, $"Layout {layout + 1}: a {prop} does not belong in the security office");
                }
            }
        }

        [Test]
        public void StorageRoom_IsFullOfShelvingAndHasAFreightLift()
        {
            foreach (var (_, layout, path) in Layouts(new[] { RoomType.StorageRoom }))
            {
                var room = Instantiate(path);
                Assert.GreaterOrEqual(PropInstances(room, "Shelf").Count(), 12, $"Layout {layout + 1}");
                Assert.GreaterOrEqual(new[] { "BoxLidCube", "BoxLidFlat", "BoxCartonCube", "BoxCartonFlat" }.Sum(box => PropInstances(room, box).Count()), 24, $"Layout {layout + 1}: boxes on the shelves");
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsFolder}/Binders.prefab"), "The binder model still exists");
                var lift = PropInstances(room, "FreightLift").Single();
                Assert.IsNull(lift.GetComponentInChildren<AutoSlidingDoor>(), "The freight lift is only looks; its doors never open");
                Assert.AreEqual(SmallSide, Mathf.Abs(lift.transform.position.x), Epsilon, "The freight lift is on a side wall");
                Assert.IsNotNull(room.transform.Find("Dressing/Markings/KeepClear_FreightLift"), $"Layout {layout + 1} has no keep-clear hatching");
                Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>($"{PropsFolder}/SlidingGate.prefab"), "The old sliding gate still exists");
            }
        }

        [Test]
        public void EveryRoom_PropsDoNotStandInEachOther()
        {
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                Physics.SyncTransforms();
                var solids = SolidProps(room).ToList();
                for (int i = 0; i < solids.Count; i++)
                {
                    for (int j = i + 1; j < solids.Count; j++)
                    {
                        var (a, boundsA) = solids[i];
                        var (b, boundsB) = solids[j];
                        if (a == b || IsTowerUnderDesk(a, b) || IsTowerUnderDesk(b, a)) continue;
                        Assert.IsFalse(Overlaps(boundsA, boundsB), $"{type} layout {layout + 1}: {a.name} and {b.name} stand in each other");
                    }
                }
            }
        }

        [Test]
        public void SuspendedCeilings_FittingsSitInTheGrid()
        {
            // A tile is centered on the room center: 600 fittings on tile centers (multiples of 0.6 m), 1200 panels
            // across two tiles along Z (centered on a grid line).
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes.Where(t => !new[] { RoomType.PowerRoom, RoomType.CoolingPlant, RoomType.StorageRoom }.Contains(t))))
            {
                var room = Instantiate(path);
                foreach (var name in new[] { "LedPanel", "CeilingDiffuser", "Sprinkler", "SmokeDetector", "CameraDome", "CeilingSpeaker" })
                {
                    foreach (var fitting in PropInstances(room, name))
                    {
                        var p = fitting.transform.position;
                        Assert.IsTrue(OnGrid(p.x, 0f) && OnGrid(p.z, 0f), $"{type}/{fitting.name} is not on a tile center: {p}");
                    }
                }
                foreach (var panel in PropInstances(room, "LedPanel1200"))
                {
                    // Across two tiles along its length, in the middle of a tile across it.
                    var p = panel.transform.position;
                    bool alongX = Mathf.Abs(panel.transform.forward.x) > 0.5f;
                    bool onGrid = alongX ? OnGrid(p.x, 0.3f) && OnGrid(p.z, 0f) : OnGrid(p.x, 0f) && OnGrid(p.z, 0.3f);
                    Assert.IsTrue(onGrid, $"{type} layout {layout + 1}/{panel.name} does not span two tiles: {p}");
                }
            }
        }

        private static bool OnGrid(float value, float offset)
        {
            float steps = (value - offset) / 0.6f;
            return Mathf.Abs(steps - Mathf.Round(steps)) < 0.01f;
        }

        private static bool IsTowerUnderDesk(GameObject tower, GameObject desk) =>
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tower).EndsWith("/PcTower.prefab") &&
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(desk).EndsWith("/Desk.prefab");

        [Test]
        public void EveryRoom_KeepsTheWalkwaysFromItsDoorsClear()
        {
            foreach (var (type, _, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                var walkways = KeepClearZones(type).ToList();
                foreach (var (name, b) in RoomContentBounds(room))
                {
                    // The walkway reaches up to the opening height; ceiling fittings above it are allowed.
                    if (b.min.y >= RoomContract.KeepClearHeight - Epsilon) continue;
                    foreach (var walkway in walkways)
                    {
                        Assert.IsFalse(Overlaps(b, walkway), $"{type}/{name} blocks the walkway from a door: {b}");
                    }
                }

                foreach (var slot in room.GetComponentsInChildren<TerminalSlot>())
                {
                    var p = slot.transform.position;
                    Assert.IsFalse(walkways.Any(w => w.Contains(new Vector3(p.x, 0.5f, p.z))), $"{type}/{slot.name} stands in a walkway");
                }
            }
        }

        [Test]
        public void RoomsWithAutomaticDoors_KeepTheDoorPocketsFree()
        {
            foreach (var (type, _, path) in Layouts(ShellRoomTypes.Where(RoomRules.HasAutomaticDoors)))
            {
                var room = Instantiate(path);
                var pockets = DoorPockets(RoomRules.DoorSides(type)).ToList();
                foreach (var (name, b) in RoomContentBounds(room))
                {
                    foreach (var pocket in pockets)
                    {
                        Assert.IsFalse(Overlaps(b, pocket), $"{type}/{name} is in the way of a door leaf or reader: {b}");
                    }
                }
            }
        }

        [Test]
        public void EveryRoom_StaysInsideTheInterior()
        {
            foreach (var (type, layout, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
                foreach (var (name, b) in RoomContentBounds(room))
                {
                    Assert.IsTrue(InsideInterior(type, b), $"{type} layout {layout + 1}/{name} sticks out of the room interior: {b}");
                }
            }
        }

        /// <summary>Whether a box lies inside the room's interior: the whole cell, or a small room's 6 x 6 m square.</summary>
        private static bool InsideInterior(RoomType type, Bounds b)
        {
            float half = RoomContract.InteriorHalfSize + Epsilon;
            float side = RoomRules.IsSmallRoom(type) ? SmallSide + Epsilon : half;
            float north = RoomRules.IsSmallRoom(type) ? RoomContract.SmallRoomBackWall + Epsilon : half;
            return b.min.x >= -side && b.max.x <= side && b.min.z >= -half && b.max.z <= north &&
                   b.min.y >= -Epsilon && b.max.y <= RoomContract.CeilingHeight + Epsilon;
        }

        /// <summary>The colliders of the props in a room (shell and small-room walls excluded), with the prop they belong to.</summary>
        private static IEnumerable<(GameObject prop, Bounds bounds)> SolidProps(GameObject room)
        {
            foreach (var collider in room.GetComponentsInChildren<Collider>())
            {
                var prop = PrefabUtility.GetNearestPrefabInstanceRoot(collider.gameObject);
                if (prop == null || prop == room) continue;
                yield return (prop, collider.bounds);
            }
        }

        [Test]
        public void EveryRoom_LeavesRackSlotsFree()
        {
            var rack = new Bounds(new Vector3(0f, RoomContract.RackHeight / 2f, 0f),
                new Vector3(RoomContract.RackWidth, RoomContract.RackHeight, RoomContract.RackDepth));
            foreach (var (type, _, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
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
            foreach (var (type, _, path) in Layouts(ShellRoomTypes))
            {
                var room = Instantiate(path);
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
                Assert.AreEqual(RoomRules.LayoutCount(type), definition.LayoutCount, $"{type} definition lists the wrong number of layouts");
                for (int layout = 0; layout < RoomRules.LayoutCount(type) && type != RoomType.Hall; layout++)
                {
                    Assert.AreEqual(AssetDatabase.LoadAssetAtPath<GameObject>(LayoutPath(type, layout)), definition.GetLayout(layout), $"{type} layout {layout + 1}");
                }
            }
        }

        private static string PrefabPath(RoomType type) => $"{RoomsFolder}/Room_{type}.prefab";

        private const string PropsFolder = "Assets/_Project/Prefabs/Props";

        /// <summary>Half width of a corridor arm (the dresser's <c>CorridorHalfWidth</c>; tests cannot see the Editor assembly).</summary>
        private const float RoomDressingCorridorHalfWidth = 1.5f;

        private static Vector3 Flat(Vector3 p) => new Vector3(p.x, 0f, p.z);

        /// <summary>World bounds of every renderer of an object.</summary>
        private static Bounds RendererBounds(GameObject root) =>
            root.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });

        /// <summary>
        /// World bounds of every renderer and collider in a room except the shell itself and the structure of a small
        /// room (its back wall). Floor paint below <see cref="WalkFloor"/> is left out.
        /// </summary>
        private static IEnumerable<(string name, Bounds bounds)> RoomContentBounds(GameObject room)
        {
            var shell = room.transform.Find("Shell");
            Assert.IsNotNull(shell, $"{room.name} has no Shell");
            var structure = room.transform.Find("Structure");
            bool IsContent(Transform t) => !t.IsChildOf(shell) && (structure == null || !t.IsChildOf(structure));
            foreach (var renderer in room.GetComponentsInChildren<Renderer>())
            {
                if (IsContent(renderer.transform) && renderer.bounds.max.y > WalkFloor)
                {
                    yield return ($"{renderer.transform.parent.name}/{renderer.name}", renderer.bounds);
                }
            }
            foreach (var collider in room.GetComponentsInChildren<Collider>())
            {
                if (IsContent(collider.transform))
                {
                    yield return ($"{collider.name} (collider)", collider.bounds);
                }
            }
        }

        /// <summary>
        /// The walkways that must stay free: from every door side straight to the middle of the room (the keep-clear
        /// cross, or only the arms a corridor has); in a small room only <see cref="RoomContract.SmallRoomKeepClearDepth"/>
        /// in from its door.
        /// </summary>
        private static IEnumerable<Bounds> KeepClearZones(RoomType type)
        {
            float half = RoomContract.KeepClearWidth / 2f;
            float edge = RoomContract.CellHalfSize;
            float top = RoomContract.KeepClearHeight;
            if (RoomRules.IsSmallRoom(type))
            {
                yield return Region(-half, -edge, half, -RoomContract.InteriorHalfSize + RoomContract.SmallRoomKeepClearDepth, top);
                yield break;
            }
            int sides = RoomRules.DoorSides(type);
            if ((sides & RoomRules.North) != 0) yield return Region(-half, -half, half, edge, top);
            if ((sides & RoomRules.South) != 0) yield return Region(-half, -edge, half, half, top);
            if ((sides & RoomRules.East) != 0) yield return Region(-half, -half, edge, half, top);
            if ((sides & RoomRules.West) != 0) yield return Region(-edge, -half, half, half, top);
        }

        /// <summary>Beside every door side, on both sides of the opening: where the open leaves, the operator and the reader go.</summary>
        private static IEnumerable<Bounds> DoorPockets(int sides)
        {
            float start = RoomContract.OpeningWidth / 2f;
            float end = RoomContract.DoorPocketEnd;
            float wall = RoomContract.InteriorHalfSize;
            float depth = RoomContract.DoorPocketDepth;
            for (int d = 0; d < 4; d++)
            {
                if ((sides & RoomRules.Mask((Direction)d)) == 0) continue;
                var turn = Quaternion.Euler(0f, d * 90f, 0f);
                foreach (int side in new[] { -1, 1 })
                {
                    // Authored for the north wall, then turned to the door's side.
                    var bounds = new Bounds(turn * new Vector3(side * start, WalkFloor, wall - depth), Vector3.zero);
                    bounds.Encapsulate(turn * new Vector3(side * end, RoomContract.DoorPocketHeight, wall));
                    yield return bounds;
                }
            }
        }

        private static Bounds Region(float x0, float z0, float x1, float z1, float top)
        {
            var bounds = new Bounds();
            bounds.SetMinMax(new Vector3(x0, WalkFloor, z0), new Vector3(x1, top, z1));
            return bounds;
        }

        /// <summary>Instances of the prop prefab <c>Props/&lt;name&gt;.prefab</c> anywhere in the room.</summary>
        private static IEnumerable<GameObject> PropInstances(GameObject room, string name)
        {
            foreach (var transform in room.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject)) continue;
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject) == $"Assets/_Project/Prefabs/Props/{name}.prefab")
                {
                    yield return transform.gameObject;
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
