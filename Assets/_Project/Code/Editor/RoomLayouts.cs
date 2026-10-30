using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// The fixed layouts. Every room type has exactly one (its prefab is <c>Room_&lt;Type&gt;.prefab</c>: the small office, the big
    /// office, the security office, the storage room, the power room, the cooling plant (HVAC room), the hall and the lift end);
    /// only corridors have more, which differ in the arm that carries the fire kit (<see cref="RoomRules.FireArmSide"/>; the
    /// corridor dresser mirrors them itself, arm by arm), as <c>Room_&lt;Type&gt;_Layout&lt;n&gt;.prefab</c>. This class holds the game logic
    /// of each layout (terminal and rack slots), so <see cref="GrayboxRoomsTool"/> and <see cref="RoomDressingTool"/> agree on where they are.
    /// </summary>
    internal static class RoomLayouts
    {
        public static string PrefabPath(RoomType type, int layout) =>
            layout == 0 ? GrayboxRoomsTool.PrefabPath(type) : $"{RoomShellTool.RoomsFolder}/Room_{type}_Layout{layout + 1}.prefab";

        /// <summary>Every layout of every room type built from the shell (not the hall).</summary>
        public static IEnumerable<(RoomType type, int layout)> All()
        {
            foreach (RoomType type in System.Enum.GetValues(typeof(RoomType)))
            {
                if (type == RoomType.Hall) continue;
                for (int layout = 0; layout < RoomRules.LayoutCount(type); layout++)
                {
                    yield return (type, layout);
                }
            }
        }

        private static float SmallWall => RoomContract.SmallRoomHalfWidth;

        /// <summary>
        /// The terminal slot of a room; none for rooms without one. The pivot stands
        /// 0.4 m from the wall, facing into the room.
        /// </summary>
        public static SlotPose? Terminal(RoomType type)
        {
            float back = 0.4f;
            switch (type)
            {
                case RoomType.SmallOffice:
                    // In the corner by the door, like the security office's, so the desk can stand as near the door wall as the lockers there.
                    return new SlotPose(-SmallWall + back, -5.4f, 90f);
                case RoomType.SecurityOffice:
                    // In the corner by the door: the racks stand centred with the lockers (z = -4.25), right up to the terminal's space.
                    return new SlotPose(-SmallWall + back, -5.4f, 90f);
                default:
                    return null;
            }
        }

        /// <summary>
        /// The rack slots of the security office: the security office's compute rack and JBOD side by side, backs
        /// against the west wall, fronts facing into the room.
        /// </summary>
        public static SlotPose[] Racks(RoomType type)
        {
            if (type != RoomType.SecurityOffice) return new SlotPose[0];
            float x = SmallWall - RoomContract.RackDepth / 2f;
            return new[] { new SlotPose(-x, SecurityRackZ[0], 90f), new SlotPose(-x, SecurityRackZ[1], 90f) };
        }

        /// <summary>Z of the two rack slots of the security office (compute rack, then JBOD), side by side.</summary>
        internal static readonly float[] SecurityRackZ = { -3.95f, -4.55f };
    }
}
