using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>The storage room: shelving with boxes and the goods lift.</summary>
    public static partial class Rooms
    {
        [Template(RoomType.StorageRoom)]
        private static RoomTemplate StorageRoomTemplate(RoomType type) => new RoomTemplate
        {
            Structure = AddSmallRoomWalls,
            Dress = (dressing, cameras, layout) => DressStorageRoom(dressing, cameras),
        };

        /// <summary>
        /// Storage room, 6 x 6 m, filled with galvanised shelving that carries cardboard boxes, every shelf
        /// reachable from an aisle at least 0.9 m wide: a row along the back wall, a row along the west wall, a
        /// back-to-back double row in the middle (along Z) and one unit on the east wall.
        /// The east wall has the landing of the goods lift (only looks: its solid doors never open) with yellow keep-clear
        /// hatching on the floor in front.
        /// Two shaft grilles high on the back wall, two LED battens, sprinklers, a detector in the middle, an extinguisher.
        /// </summary>
        private static void DressStorageRoom(Transform dressing, Transform cameras)
        {
            var shelving = ModelUtility.CreateGroup("Shelving", dressing);
            var details = ModelUtility.CreateGroup("Details", dressing);
            var air = ModelUtility.CreateGroup("Ventilation", dressing);
            var markings = ModelUtility.CreateGroup("Markings", dressing);
            var ceiling = ModelUtility.CreateGroup("Ceiling", dressing);
            const float half = 0.2f;
            float back = RoomContract.SmallRoomBackWall;

            int index = 0;
            void Shelf(float x, float z, float yaw)
            {
                ShelfWithItems(shelving, details, new Vector3(x, 0f, z), yaw, index++);
            }

            foreach (float x in new[] { -2f, -1f, 0f, 1f, 2f })
            {
                Shelf(x, back - half, 180f);
            }
            foreach (float z in new[] { -0.9f, -1.9f, -2.9f, -3.9f, -4.9f })
            {
                Shelf(-Side + half, z, 90f);
            }
            Shelf(Side - half, -0.9f, 270f);
            // The double row is centred in the free space between the west row and the start of the keep-clear hatching.
            float rowX = (-Side + 2f * half + FreightHatchStartX) / 2f;
            foreach (float z in new[] { -1.9f, -2.9f })
            {
                Shelf(rowX - half, z, 270f);
                Shelf(rowX + half, z, 90f);
            }
            

            // The goods lift on the east wall and the space in front of it that must stay clear for loading.
            var lift = ModelUtility.CreateGroup("FreightLift", dressing);
            Put(Prop.FreightLift, lift, Side, 0f, FreightLiftZ, 270f, "FreightLift");
            float hatch = Props.FreightDoorWidth / 2f + 0.15f;
            Hatching(markings, "KeepClear_FreightLift", FreightHatchStartX, FreightLiftZ - hatch, Side, FreightLiftZ + hatch);

            TechnicalCeiling(ceiling, air);
            SmallRoomBasics(dressing, cameras, TechnicalDetector);
        }

        /// <summary>Room-local Z of the middle of the goods lift door in the storage room (on the east wall).</summary>
        private const float FreightLiftZ = -3.55f;

        /// <summary>Room-local X where the goods lift's keep-clear hatching starts (its edge furthest from the wall).</summary>
        private const float FreightHatchStartX = 1.6f;

        /// <summary>
        /// A galvanised shelving unit (1 m wide) with cardboard boxes on its boards: every board carries one or two of the four box
        /// kinds, chosen by a fixed pattern from the unit's index so neighbouring units differ.
        /// </summary>
        private static void ShelfWithItems(Transform shelving, Transform details, Vector3 position, float yaw, int index)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(Prop.Shelf, shelving, position, yaw, $"Shelf_{index + 1}");
            var boards = Props.ShelfBoardTops;
            var kinds = new[] { Prop.BoxCartonCube, Prop.BoxLidFlat, Prop.BoxLidCube, Prop.BoxCartonFlat };
            for (int board = 0; board < boards.Length; board++)
            {
                // Two boxes or one box (left, middle or right), depending on unit and board.
                int pattern = (index * 3 + board * 5) % 6;
                if (board == 0 && index % 2 == 0)
                {
                    Put(kinds[(index + board) % 4], details, position + turn * new Vector3(-0.25f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_A");
                    Put(kinds[(index + board + 1) % 4], details, position + turn * new Vector3(0.27f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_B");
                    continue;
                }
                // x of the first and the second box: two boxes side by side, or just one somewhere along the board.
                float[] first = { -0.24f, -0.24f, -0.08f, -0.08f, 0.27f, 0.0f };
                Put(kinds[(index + board * 2 + pattern) % 4], details, position + turn * new Vector3(first[pattern], boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_A");
                if (pattern == 0 || pattern == 2)
                {
                    Put(kinds[(index + board + pattern + 1) % 4], details, position + turn * new Vector3(0.27f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_B");
                }
                else if (pattern == 4)
                {
                    Put(kinds[(index + board + 3) % 4], details, position + turn * new Vector3(-0.2f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_B");
                }
            }
        }
    }
}
