using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>The power room: transformers, switchgear, UPS cabinets and the kW meter panel (a clue).</summary>
    public static partial class Rooms
    {
        [Template(RoomType.PowerRoom)]
        private static RoomTemplate PowerRoomTemplate(RoomType type) => new RoomTemplate
        {
            Structure = AddSmallRoomWalls,
            Dress = (dressing, cameras, layout) => DressPowerRoom(dressing, cameras),
        };

        /// <summary>
        /// Electrical switch room, 6 x 6 m, filled with what belongs there and nothing else. Two cast-resin transformers
        /// stand along the side walls in the north half (0.8 m behind them, 1.8 m between their fronts), each behind a
        /// yellow keep-out line (no floor grilles in this room; the two shaft grilles are high on the back wall). On the back wall between
        /// them the panel of kW meters for all hall sections. In the
        /// south half, 1 m in front of the transformers: two switchgear sections on the west wall and two UPS
        /// cabinets on the east wall. An exposed power tray across the room. LED battens, sprinklers, a detector, a CO2 extinguisher.
        /// </summary>
        private static void DressPowerRoom(Transform dressing, Transform cameras)
        {
            var plant = ModelUtility.CreateGroup("Plant", dressing);
            var power = ModelUtility.CreateGroup("Power", dressing);
            var air = ModelUtility.CreateGroup("Ventilation", dressing);
            var markings = ModelUtility.CreateGroup("Markings", dressing);
            var ceiling = ModelUtility.CreateGroup("Ceiling", dressing);

            const float transformerZ = -1.6f;
            Put(Prop.Transformer, plant, -1.5f, 0f, transformerZ, 90f, "Transformer_W");
            Put(Prop.Transformer, plant, 1.5f, 0f, transformerZ, 270f, "Transformer_E");
            // Yellow lines, one exact mesh: the aisle lines (1.4 m either side of the middle, from the door as in the cooling plant) run into the keep-out lines
            // 0.5 m round both enclosures (the front line along z = -3.1 m, the side line at x = 0.4 m), closed by the walls; nothing overlaps, every corner is square.
            var lines = new List<(Vector3, Vector3)>();
            foreach (int side in new[] { -1, 1 })
            {
                lines.Add((new Vector3(side * 1.4f, 0f, -2f * Side), new Vector3(side * 1.4f, 0f, -3.1f)));
                lines.Add((new Vector3(side * Side, 0f, -3.1f), new Vector3(side * 0.4f, 0f, -3.1f)));
                lines.Add((new Vector3(side * 0.4f, 0f, -3.1f), new Vector3(side * 0.4f, 0f, RoomContract.SmallRoomBackWall)));
            }
            PaintedLines(markings, "KeepOut", lines, -Side, Side, -2f * Side, RoomContract.SmallRoomBackWall);
            TechnicalCeiling(ceiling, air);

            Put(Prop.PowerMeterPanel, plant, 0f, 0f, RoomContract.SmallRoomBackWall, 180f, "MeterPanel");

            foreach (float z in new[] { -4.0f, -4.8f })
            {
                Put(Prop.Switchgear, plant, -Side + 0.3f, 0f, z, 90f, $"Switchgear_{z:0.0}");
            }
            // Their north edge is 0.5 m from the transformers' front line (z = -3.1), as the switchgear's and the transformers' are.
            foreach (float z in new[] { -3.9f, -4.5f })
            {
                Put(Prop.UpsCabinet, plant, Side - 0.4f, 0f, z, 270f, $"UpsCabinet_{z:0.0}");
            }
            

            SmallRoomBasics(dressing, cameras, TechnicalDetector);
        }
    }
}
