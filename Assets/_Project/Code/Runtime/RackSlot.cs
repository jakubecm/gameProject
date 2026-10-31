using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Marks a spot where a rack may stand. The pivot is on the floor, centered under the rack,
    /// and the transform's forward axis points the way the rack faces (into the room).
    /// The level builder puts a rack on every slot (<see cref="RackPlacement"/>).
    /// </summary>
    public class RackSlot : MonoBehaviour
    {
        private static readonly Color GizmoColor = new Color(0.2f, 0.8f, 1f);

        private void OnDrawGizmos()
        {
            var size = new Vector3(RoomContract.RackWidth, RoomContract.RackHeight, RoomContract.RackDepth);
            SlotGizmos.DrawSlot(transform, size, GizmoColor);
        }
    }
}
