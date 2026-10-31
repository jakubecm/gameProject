using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>The security office: the video wall, two consoles, the two rack slots, the terminal, lockers and a bench.</summary>
    public static partial class Rooms
    {
        [Template(RoomType.SecurityOffice)]
        private static RoomTemplate SecurityOfficeTemplate(RoomType type)
        {
            float x = SmallWall - RoomContract.RackDepth / 2f;
            return new RoomTemplate
            {
                // In the corner by the door: the racks stand centred with the lockers (z = -4.25), right up to the terminal's space.
                Terminal = new SlotPose(-SmallWall + 0.4f, -5.4f, 90f),
                // The compute rack and the JBOD side by side, backs against the west wall, fronts into the room.
                Racks = new[] { new SlotPose(-x, SecurityRackZ[0], 90f), new SlotPose(-x, SecurityRackZ[1], 90f) },
                Structure = room =>
                {
                    AddSmallRoomWalls(room);
                    BuildVideoWall(room);
                },
                Dress = (dressing, cameras, layout) => DressSecurityOffice(dressing, cameras),
            };
        }

        /// <summary>
        /// Security office, 6 x 6 m: the video wall of 2 x 4 screens across the whole back wall (<see cref="Rooms"/>), two
        /// desks with two monitors each facing it, the compute and JBOD rack slots on a side wall and the terminal by the
        /// door; nothing else but lockers and a bench on the east wall
        /// on the west wall (the racks and the terminal then stand on the east wall). Few lights, so the screens stay
        /// readable.
        /// </summary>
        private static void DressSecurityOffice(Transform dressing, Transform cameras)
        {
            var furniture = ModelUtility.CreateGroup("Furniture", dressing);
            var details = ModelUtility.CreateGroup("Details", dressing);
            var power = ModelUtility.CreateGroup("PowerAndData", dressing);
            var ceiling = ModelUtility.CreateGroup("Ceiling", dressing);
            float back = RoomContract.SmallRoomBackWall;

            // The two consoles stand 2 mm apart (their tops are 1.6 m wide).
            foreach (var (x, suffix) in new[] { (-0.801f, "1"), (0.801f, "2") })
            {
                PlaceDesk(furniture, details, power, new Vector3(x, 0f, -1.9f), 180f, $"Console{suffix}", true);
            }
            Put(Prop.PaperStack, details, -0.2f, Props.DeskHeight, -2.1f, 350f, "ShiftLog");

            // Power for the screens: trunking behind each column of screens, up into the ceiling.
            foreach (float x in VideoWallColumns)
            {
                Trunking(power, $"ScreenTrunking_{x:0.00}", new Vector3(x, 2.42f, back), new Vector3(x, Ceiling, back), Vector3.back);
            }

            // Outlets for the two racks, beside them on their wall.
            float rackSide = -1f;
            float rackYaw = 90f;
            Put(Prop.PowerSocket, power, rackSide * Side, OutletHeight, -3.45f, rackYaw, "PowerSocket_Racks");
            Put(Prop.DataSocket, power, rackSide * Side, OutletHeight, -3.2f, rackYaw, "DataSocket_Racks");
            DressTerminalOutlets(power, rackSide * Side, -4.97f, rackYaw);

            // The changing corner on the east wall, as near the door wall as the extinguisher corner allows (0.3 m clear of it): two lockers and the bench in front.
            foreach (float z in new[] { -3.95f, -4.55f })
            {
                Put(Prop.Locker, furniture, Side - 0.25f, 0f, z, 270f, $"Locker_{z:0.0}");
            }
            Put(Prop.LockerBench, furniture, 1.9f, 0f, -4.25f, 90f);

            SmallRoomCeiling(ceiling);
            SmallRoomBasics(dressing, cameras, SmallRoomDetector, false, true);
        }

        /// <summary>
        /// The security office's video wall: 2 x 4 screens across the whole back wall, driven by a
        /// <see cref="SecurityMonitorWall"/>. The screens are game logic (they show the camera feeds), so they sit in their
        /// own <c>VideoWall</c> group, not in the dressing. Every layout has it on the back wall, facing the door.
        /// </summary>
        private static void BuildVideoWall(Transform room)
        {
            var videoWall = ModelUtility.CreateGroup(VideoWallName, room);
            videoWall.gameObject.AddComponent<SecurityMonitorWall>();
            foreach (float y in VideoWallRows)
            {
                foreach (float x in VideoWallColumns)
                {
                    Props.Place(Prop.VideoScreen, videoWall, new Vector3(x, y, RoomContract.SmallRoomBackWall), 180f, $"Screen_{x:0.00}_{y:0.00}");
                }
            }
        }

        public const string VideoWallName = "VideoWall";

        /// <summary>Centers of the video wall screens along the back wall (X), measured from the room center.</summary>
        internal static readonly float[] VideoWallColumns = { -1.875f, -0.625f, 0.625f, 1.875f };

        /// <summary>Heights of the screen centers.</summary>
        internal static readonly float[] VideoWallRows = { 1.3f, 2.05f };

        /// <summary>Z of the two rack slots of the security office (compute rack, then JBOD), side by side.</summary>
        internal static readonly float[] SecurityRackZ = { -3.95f, -4.55f };
    }
}
