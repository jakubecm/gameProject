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
    /// <summary>What every small room shares: its walls, safety basics and the two kinds of ceiling.</summary>
    public static partial class Rooms
    {
        // ---------------------------------------------------------------- small rooms (6 x 6 m by the south door)

        /// <summary>
        /// What every small room has, placed the same way: four sprinklers (2.4 m apart across, 3 m along the room, at most
        /// 2.2 m from a wall), one smoke detector, a ceiling dome in the corner by the door looking in, the light switch on
        /// the west wall beside the door, and the extinguisher and the call point on
        /// the east wall by the door (west if <paramref name="extinguisherWest"/>).
        /// </summary>
        private static void SmallRoomBasics(Transform dressing, Transform cameras, (float x, float z) detector, bool extinguisherWest = false, bool switchEast = false)
        {
            var safety = ModelUtility.CreateGroup("Safety", dressing);
            var ceiling = dressing.Find("Ceiling") ?? ModelUtility.CreateGroup("Ceiling", dressing);
            // At the corners of the 3 x 3 tiles around the room's middle tile (every other tile), symmetric about the center (0, -3).
            PutOnCeiling(Prop.Sprinkler, ceiling, (-1.2f, -1.8f), (1.2f, -1.8f), (-1.2f, -4.2f), (1.2f, -4.2f));
            PutOnCeiling(Prop.SmokeDetector, ceiling, detector);
            Put(Prop.CameraDome, cameras, 2.4f, Ceiling, -5.4f, 315f, "CameraDome");
            if (switchEast) Put(Prop.LightSwitch, safety, Side, Props.LightSwitchHeight, -5.7f, 270f, "LightSwitch");
            else Put(Prop.LightSwitch, safety, -Side, Props.LightSwitchHeight, -5.6f, 90f, "LightSwitch");
            float x = extinguisherWest ? -Side : Side;
            float yaw = extinguisherWest ? 90f : 270f;
            Put(Prop.FireExtinguisher, safety, x, 0f, -5.3f, yaw);
            Put(Prop.FireCallPoint, safety, x, Props.CallPointHeight, -5.3f, yaw);
        }

        /// <summary>
        /// The suspended ceiling of the small offices and the security office, built like the storage room's: two 1x1 (600 mm)
        /// LED panels on the axis of the air unit (the door axis), in line with the sprinkler rows, the air diffuser in the middle of the room and, diagonally beside it, one loudspeaker and the smoke detector
        /// (<see cref="SmallRoomDetector"/>). The camera sits in the right-hand corner by the door
        /// (<see cref="SmallRoomBasics"/>).
        /// </summary>
        private static void SmallRoomCeiling(Transform ceiling)
        {
            PutOnCeiling(Prop.LedPanel, ceiling, (0f, -1.8f), (0f, -4.2f));
            PutOnCeiling(Prop.CeilingDiffuser, ceiling, (0f, -3.0f));
            PutOnCeiling(Prop.CeilingSpeaker, ceiling, (0.6f, -2.4f));
        }

        private static readonly (float x, float z) SmallRoomDetector = (-0.6f, -3.6f);

        /// <summary>Distance from the door wall's inner face to the nearest edge of the security office's lockers (and of the office's cabinets and desk): 0.95 m.</summary>
        private const float SecurityLockerGap = 0.95f;

        // ---------------------------------------------------------------- technical rooms (6 x 6 m, one door)

        /// <summary>
        /// The ceiling of the technical rooms (power room, cooling plant, storage room), the same in all three: two LED
        /// battens on the sprinkler axes (x = ±1.2 m, along the door axis, centered between the two sprinkler rows), and on the
        /// wall opposite the door two ventilation shaft grilles high up on those same axes. Nothing runs across the ceiling.
        /// The smoke detector sits in the exact middle of the room (<see cref="TechnicalDetector"/>).
        /// </summary>
        private static void TechnicalCeiling(Transform ceiling, Transform air)
        {
            foreach (float x in new[] { -1.2f, 1.2f })
            {
                Put(Prop.LedBatten, ceiling, x, Ceiling, -3.0f, 0f, $"Light_{x:0.0}_-3.0");
                Put(Prop.WallVentGrille, air, x, HighGrilleHeight, RoomContract.SmallRoomBackWall, 180f, $"ShaftGrille_{(x < 0 ? "W" : "E")}");
            }
        }

        /// <summary>The middle of a small room (room-local), where the technical rooms have their smoke detector.</summary>
        private static readonly (float x, float z) TechnicalDetector = (0f, -3.0f);

        /// <summary>
        /// A small room is a 6 x 6 m square by its south door: a full-height wall across the cell (its room face at
        /// <see cref="RoomContract.SmallRoomBackWall"/>) and two side walls (room faces at
        /// ±<see cref="RoomContract.SmallRoomHalfWidth"/>) close it. The rest of the cell is built up and never seen.
        /// </summary>
        private static void AddSmallRoomWalls(Transform room)
        {
            var structure = ModelUtility.CreateGroup(StructureName, room);
            float wall = RoomContract.InteriorHalfSize;
            float thickness = RoomContract.WallThickness;
            float side = RoomContract.SmallRoomHalfWidth;
            float back = RoomContract.SmallRoomBackWall;
            ModelUtility.CreateStandingBox("BackWall", structure, new Vector3(0f, 0f, back + thickness / 2f),
                new Vector3(2f * wall, RoomContract.CeilingHeight, thickness), SurfaceMaterial.Wall);
            foreach (int x in new[] { -1, 1 })
            {
                ModelUtility.CreateStandingBox(x < 0 ? "SideWall_W" : "SideWall_E", structure, new Vector3(x * (side + thickness / 2f), 0f, (back - wall) / 2f),
                    new Vector3(thickness, RoomContract.CeilingHeight, back + wall), SurfaceMaterial.Wall);
            }
        }
    }
}
