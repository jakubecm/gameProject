using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// How a layout is turned out of its design: not at all, mirrored east-west (small rooms, whose one door is in the
    /// middle of the south wall), or mirrored across the south-west/north-east diagonal (the office, whose doors are on
    /// the south and west sides, which swap onto each other). A mirrored layout keeps every rule its design keeps.
    /// </summary>
    internal readonly struct LayoutMirror
    {
        public static readonly LayoutMirror None = new LayoutMirror(false, false);

        public readonly bool FlipX;
        public readonly bool Diagonal;

        public LayoutMirror(bool flipX, bool diagonal)
        {
            FlipX = flipX;
            Diagonal = diagonal;
        }

        /// <summary>A room-local point (or direction) of the design, in the layout.</summary>
        public Vector3 Point(Vector3 p)
        {
            if (Diagonal) p = new Vector3(p.z, p.y, p.x);
            if (FlipX) p.x = -p.x;
            return p;
        }

        /// <summary>A yaw of the design (forward = (sin, 0, cos)), in the layout.</summary>
        public float Yaw(float yaw)
        {
            if (Diagonal) yaw = 90f - yaw;
            if (FlipX) yaw = -yaw;
            return Mathf.Repeat(yaw, 360f);
        }

        public SlotPose Pose(SlotPose pose)
        {
            var p = Point(new Vector3(pose.X, 0f, pose.Z));
            return new SlotPose(p.x, p.z, Yaw(pose.YawDegrees));
        }
    }

    /// <summary>
    /// The fixed layouts of every room type (<see cref="RoomRules.LayoutCount"/>): each room type has two designs, and
    /// layouts 3 and 4 are the mirror images of layouts 1 and 2. Corridor layouts differ in the arm that carries the fire
    /// kit (<see cref="RoomRules.FireArmSide"/>); the corridor dresser mirrors them itself, arm by arm. Layout 1 is <c>Room_&lt;Type&gt;.prefab</c>, the others
    /// <c>Room_&lt;Type&gt;_Layout&lt;n&gt;.prefab</c>. This class holds the game logic of each layout (terminal and rack
    /// slots), so Create Graybox Rooms and Dress Graybox Rooms agree on where they are.
    /// </summary>
    internal static class RoomLayouts
    {
        public static string PrefabPath(RoomType type, int layout) =>
            layout == 0 ? GrayboxRoomsTool.PrefabPath(type) : $"{RoomShellTool.RoomsFolder}/Room_{type}_Layout{layout + 1}.prefab";

        /// <summary>Which of the type's two designs a layout uses.</summary>
        public static int Design(int layout) => layout % 2;

        public static LayoutMirror Mirror(RoomType type, int layout)
        {
            if (layout < 2 || RoomRules.IsCorridor(type)) return LayoutMirror.None;
            return RoomRules.IsSmallRoom(type) ? new LayoutMirror(true, false) : new LayoutMirror(false, true);
        }

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
        /// The terminal slot of a layout, in the design (not mirrored); none for rooms without one. The pivot stands
        /// 0.4 m from the wall, facing into the room.
        /// </summary>
        public static SlotPose? Terminal(RoomType type, int design)
        {
            float back = 0.4f;
            switch (type)
            {
                case RoomType.Office:
                    return new SlotPose(-3.6f, -RoomContract.InteriorHalfSize + back, 0f);
                case RoomType.SmallOffice:
                    return new SlotPose(-SmallWall + back, -4.6f, 90f);
                case RoomType.SecurityOffice:
                    return design == 0 ? new SlotPose(-SmallWall + back, -5.0f, 90f) : new SlotPose(SmallWall - back, -5.0f, 270f);
                default:
                    return null;
            }
        }

        /// <summary>
        /// The rack slots of a layout, in the design: the security office's compute rack and JBOD side by side, backs
        /// against a side wall (west in design 1, east in design 2), fronts facing into the room.
        /// </summary>
        public static SlotPose[] Racks(RoomType type, int design)
        {
            if (type != RoomType.SecurityOffice) return new SlotPose[0];
            float x = SmallWall - RoomContract.RackDepth / 2f;
            return design == 0
                ? new[] { new SlotPose(-x, SecurityRackZ[0], 90f), new SlotPose(-x, SecurityRackZ[1], 90f) }
                : new[] { new SlotPose(x, SecurityRackZ[0], 270f), new SlotPose(x, SecurityRackZ[1], 270f) };
        }

        /// <summary>Z of the two rack slots of the security office (compute rack, then JBOD), side by side.</summary>
        internal static readonly float[] SecurityRackZ = { -3.6f, -4.2f };
    }
}
