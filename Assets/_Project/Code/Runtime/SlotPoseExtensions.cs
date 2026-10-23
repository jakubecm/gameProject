using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    public static class SlotPoseExtensions
    {
        /// <summary>Puts a transform on a room-local pose from the room contract (on the floor, turned around +Y).</summary>
        public static void ApplyTo(this SlotPose pose, Transform transform)
        {
            transform.localPosition = new Vector3(pose.X, 0f, pose.Z);
            transform.localRotation = Quaternion.Euler(0f, pose.YawDegrees, 0f);
        }
    }
}
