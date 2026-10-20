using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>Editor gizmo drawing shared by the slot markers.</summary>
    internal static class SlotGizmos
    {
        /// <summary>
        /// Draws a wire box standing on the transform's pivot plus a floor arrow that starts at the
        /// pivot and points out of the front face (local +Z).
        /// </summary>
        public static void DrawSlot(Transform transform, Vector3 size, Color color)
        {
            Gizmos.color = color;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(0f, size.y / 2f, 0f), size);

            const float floorLift = 0.02f;
            const float headLength = 0.2f;
            const float headWidth = 0.15f;
            float tip = size.z / 2f + 0.4f;
            var from = new Vector3(0f, floorLift, 0f);
            var to = new Vector3(0f, floorLift, tip);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawLine(to, new Vector3(-headWidth, floorLift, tip - headLength));
            Gizmos.DrawLine(to, new Vector3(headWidth, floorLift, tip - headLength));
        }
    }
}
