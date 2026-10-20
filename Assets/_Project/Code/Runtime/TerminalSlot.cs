using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Marks a spot where a terminal may stand. The pivot is on the floor and the transform's
    /// forward axis points the way the screen faces (toward the player).
    /// </summary>
    public class TerminalSlot : MonoBehaviour
    {
        private static readonly Color GizmoColor = new Color(1f, 0.7f, 0.1f);

        // Placeholder gizmo size (W x H x D); the real terminal size is not part of the room contract yet.
        private static readonly Vector3 GizmoSize = new Vector3(0.8f, 1.2f, 0.6f);

        private void OnDrawGizmos()
        {
            SlotGizmos.DrawSlot(transform, GizmoSize, GizmoColor);
        }
    }
}
