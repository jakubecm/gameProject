using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>The office: coffee corner, wall TV with the meeting table, a desk, filing cabinets, the printer and the terminal.</summary>
    public static partial class Rooms
    {
        [Template(RoomType.Office)]
        private static RoomTemplate OfficeTemplate(RoomType type) => new RoomTemplate
        {
            // In the corner by the door, 0.4 m from the west wall, facing east (like the security office's).
            Terminal = new SlotPose(-SmallWall + 0.4f, -5.4f, 90f),
            Structure = AddSmallRoomWalls,
            Dress = (dressing, cameras, layout) => DressOffice(dressing, cameras),
        };

        /// <summary>
        /// Small office, 6 x 6 m, with the office props and still room to walk. Back (north) wall, opposite the door: the wall TV centred in the free
        /// part of the wall (between the west wall and the coffee corner) and the meeting table centred on the same axis, turned 90 degrees (its short end
        /// towards the TV, a few centimetres from the wall), with a chair on each long side; the coffee
        /// corner (kitchenette, fridge in the corner) in the north-east corner. West wall: the desk with two monitors, keyboard, mouse and a photo frame, as near
        /// the door wall as the lockers of the security office (0.95 m from it), beside the terminal in the corner. East wall, the same distance from the door wall:
        /// two filing cabinets and the printer with its socket (a few centimetres between them). The walkway from the door stays free.
        /// </summary>
        private static void DressOffice(Transform dressing, Transform cameras)
        {
            var furniture = ModelUtility.CreateGroup("Furniture", dressing);
            var details = ModelUtility.CreateGroup("Details", dressing);
            var power = ModelUtility.CreateGroup("PowerAndData", dressing);
            var ceiling = ModelUtility.CreateGroup("Ceiling", dressing);
            float back = RoomContract.SmallRoomBackWall;

            // North-east corner: the coffee corner, the fridge in the corner.
            const float counterX = 1.15f;
            Kitchenette(furniture, details, power, new Vector3(counterX, 0f, back - 0.3f), 180f);
            Put(Prop.Fridge, furniture, 2.45f, 0f, back - 0.345f, 180f);

            // The free wall: from the west wall to where the kitchenette's cabinets start (counter 1.8 m wide, centred on counterX).
            float freeFrom = -Side;
            float freeTo = counterX - 0.9f;
            float axis = (freeFrom + freeTo) / 2f;
            Put(Prop.WallTv, details, axis, 0f, back, 180f);
            // The table (1.8 x 0.9 m) is turned 90 degrees: its short end stands 5 cm from the wall under the TV.
            const float tableGap = 0.05f;
            MeetingTable(furniture, details, new Vector3(axis, 0f, back - tableGap - 0.9f), 90f, 2);

            // West wall: the desk with two monitors, 0.95 m from the door wall (the terminal's space is in the corner, 0.15 m clear of it).
            var desk = new Vector3(-Side + 0.4f, 0f, -Side * 2f + SecurityLockerGap + 0.8f);
            PlaceDesk(furniture, details, power, desk, 90f, "W1", false);
            var top = desk + Vector3.up * Props.DeskHeight;
            var turn = Quaternion.Euler(0f, 90f, 0f);
            Put(Prop.PhotoFrame, details, top + turn * new Vector3(0.66f, 0f, -0.2f), 78f, "PhotoFrame_W1");
            Put(Prop.Mug, details, top + turn * new Vector3(-0.66f, 0f, 0.15f), 90f, "Mug_W1");
            DressTerminalOutlets(power, -Side, -4.97f, 90f);

            // East wall, from the same distance: two filing cabinets, a small gap, the printer and its socket.
            float start = -Side * 2f + SecurityLockerGap;
            CabinetRow(furniture, details, "E", new[] { start + 0.225f, start + 0.675f }, z => new Vector3(Side - 0.3f, 0f, z), 270f);
            // A few centimetres between the cabinets and the printer, as in a real office.
            const float printerGap = 0.04f;
            float printerZ = start + 0.9f + printerGap + 0.3f;
            Put(Prop.Printer, furniture, Side - 0.325f, 0f, printerZ, 270f);
            Put(Prop.PowerSocket, power, Side, OutletHeight, printerZ + 0.375f, 270f, "PowerSocket_Printer");

            SmallRoomCeiling(ceiling);
            SmallRoomBasics(dressing, cameras, SmallRoomDetector, false, true);
        }

        /// <summary>
        /// The kitchenette of a coffee corner against a wall (<paramref name="yaw"/> turns its front away from the wall):
        /// the closed counter with its sink and tap, the coffee machine and mugs on the worktop, and a socket above it.
        /// </summary>
        private static void Kitchenette(Transform furniture, Transform details, Transform power, Vector3 position, float yaw)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(Prop.CoffeeCounter, furniture, position, yaw);
            Put(Prop.CoffeeMachine, details, position + turn * Props.CounterMachinePosition, yaw);
            Put(Prop.Mug, details, position + turn * new Vector3(-0.09f, Props.CounterHeight, -0.245f), yaw, "Mug_Counter1");
            Put(Prop.Mug, details, position + turn * new Vector3(0.09f, Props.CounterHeight, -0.245f), yaw, "Mug_Counter2");
            Put(Prop.PowerSocket, power, position + turn * new Vector3(-0.55f, 1.15f, -0.3f), yaw, "PowerSocket_Coffee");
        }

        /// <summary>A row of filing cabinets against a wall; every other one has papers on top.</summary>
        private static void CabinetRow(Transform furniture, Transform details, string name, float[] along, Func<float, Vector3> position, float yaw)
        {
            for (int i = 0; i < along.Length; i++)
            {
                var at = position(along[i]);
                Put(Prop.FilingCabinet, furniture, at, yaw, $"FilingCabinet_{name}{i + 1}");
                if (i % 2 == 1)
                {
                    Put(Prop.PaperStack, details, at + Vector3.up * 1.32f, yaw + 80f, $"PaperStack_{name}{i + 1}");
                }
            }
        }

        /// <summary>
        /// A meeting table (1.8 x 0.9 m, long side along X turned by <paramref name="yaw"/>) with
        /// <paramref name="chairsPerSide"/> chairs along each long side only, so there is room to walk around its ends;
        /// a floor box under it, papers and a mug on it.
        /// </summary>
        private static void MeetingTable(Transform furniture, Transform details, Vector3 center, float yaw, int chairsPerSide, string name = "Meeting")
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(Prop.MeetingTable, furniture, center, yaw, $"MeetingTable_{name}");
            float spacing = chairsPerSide == 3 ? 0.6f : 0.9f;
            int chair = 1;
            for (int i = 0; i < chairsPerSide; i++)
            {
                float along = (i - (chairsPerSide - 1) / 2f) * spacing;
                Put(Prop.OfficeChair, furniture, center + turn * new Vector3(along, 0f, 0.85f), yaw + 180f, $"OfficeChair_{name}{chair++}");
                Put(Prop.OfficeChair, furniture, center + turn * new Vector3(along, 0f, -0.85f), yaw, $"OfficeChair_{name}{chair++}");
            }
            Put(Prop.PaperStack, details, center + turn * new Vector3(-0.4f, Props.DeskHeight, -0.1f), yaw + 10f, $"PaperStack_{name}");
            Put(Prop.Mug, details, center + turn * new Vector3(0.35f, Props.DeskHeight, 0.15f), yaw, $"Mug_{name}");
        }
    }
}
