using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Unit (slot) of the console drawer in <see cref="Prop.ConsoleRack"/>: a 1U drawer at 1.21 m, so the pulled-out screen stands at eye level.</summary>
        private const int ConsoleSlot = 24;

        /// <summary>
        /// The console rack of the meet-me room, 0.8 x 0.6 x 2.2 m, standing next to the entrance cabinet: the black rack frame closed at the sides and on the roof, no door, its front open.
        /// Filled with the hall's real equipment from the bottom (UPS, 2U and 1U disk servers, 3U servers, switches), a 1U pull-out LCD console drawer
        /// (<see cref="AddConsoleDrawer"/>, at <see cref="ConsoleSlot"/>) and more servers above. It carries no cords; the pipe of the room stands on its roof and joins it to the tray.
        /// The front faces +Z; the pivot is the middle of its footprint on the floor.
        /// </summary>
        [Model(Prop.ConsoleRack)]
        private static void BuildConsoleRack(Transform root)
        {
            var p = new RackParts();
            BuildMeetFrame(p);
            const float half = MeetRackWidth / 2f;
            const float d = MeetRackDepth / 2f;
            float top = MeetRackHeight - 0.04f;
            var m = SurfaceMaterial.RackBlack;
            // Closed sides and roof.
            p.Box(m, -half, MeetPlinth, -d, -half + 0.012f, top, d);
            p.Box(m, half - 0.012f, MeetPlinth, -d, half, top, d);
            p.Box(m, -half + 0.012f, top, -d + 0.03f, half - 0.012f, MeetRackHeight, d - 0.03f);

            // Bottom up: (item, first unit, units).
            var items = new List<(RackItem item, int slot, int units)>
            {
                (RackItem.Ups2U, 0, 2), (RackItem.DiskServer2U, 2, 2), (RackItem.DiskServer2U, 4, 2), (RackItem.ClosedServer3U, 6, 3), (RackItem.Switch1U, 9, 1),
                (RackItem.DiskServer1U, 10, 1), (RackItem.DiskServer1U, 11, 1), (RackItem.DiskServer1U, 12, 1), (RackItem.Switch1U, 13, 1), (RackItem.ClosedServer3U, 14, 3),
                (RackItem.DiskServer1U, 17, 1), (RackItem.DiskServer1U, 18, 1), (RackItem.DiskServer1U, 19, 1), (RackItem.Switch1U, 20, 1), (RackItem.DiskServer1U, 21, 1), (RackItem.DiskServer1U, 22, 1), (RackItem.DiskServer1U, 23, 1), (RackItem.DiskServer1U, 25, 1),
                (RackItem.DiskServer2U, 26, 2), (RackItem.DiskServer2U, 28, 2), (RackItem.ClosedServer3U, 30, 3), (RackItem.DiskServer1U, 33, 1), (RackItem.DiskServer1U, 34, 1),
                (RackItem.Switch1U, 35, 1), (RackItem.DiskServer1U, 36, 1), (RackItem.DiskServer2U, 37, 2), (RackItem.Switch1U, 39, 1), (RackItem.DiskServer1U, 40, 1), (RackItem.BlankPanel1U, 41, 1),
            };
            var equipment = new RackParts();
            int index = 0;
            foreach (var (item, slot, units) in items) BuildRackItem(equipment, item, FibreSlotY(slot) + 0.001f, units * FibreUnit - 0.003f, index++);
            equipment.AppendTo(p, new Vector3(0f, 0f, FibrePanelFront - ServerFront));

            var labels = new MeshBuilder();
            AddLabel(labels, new Vector3(0f, MeetRackHeight - 0.02f, MeetRackDepth / 2f), Vector3.left, Vector3.up, 0.14f, 0.035f, MeetLabel.Console);
            AddConsoleDrawer(root, p, ConsoleSlot);

            p.Attach("Frame", root);
            labels.Attach("Labels", root, SurfaceMaterial.MeetLabels);
            AddBoxCollider(root, new Vector3(0f, MeetRackHeight / 2f, 0.01f), new Vector3(MeetRackWidth, MeetRackHeight, 0.62f));
        }
    }
}
