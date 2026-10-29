using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Two-bone leg solver in the side view of the player (forward and down). Given the thigh and shin
    /// lengths and where the ankle has to be relative to the hip, it returns the angles of the hip and the
    /// knee, so a foot can stay planted on the floor while the hips move. The knee always bends forward.
    /// Angles are in degrees: the thigh angle is measured from straight down (forward is positive), the knee
    /// flexion is how far the shin is bent back relative to the thigh (0 = straight leg).
    /// </summary>
    public static class LegSolver
    {
        public readonly struct Pose
        {
            public readonly float ThighAngle;
            public readonly float KneeFlexion;

            public Pose(float thighAngle, float kneeFlexion)
            {
                ThighAngle = thighAngle;
                KneeFlexion = kneeFlexion;
            }

            /// <summary>Angle of the shin from straight down, forward positive.</summary>
            public float ShinAngle => ThighAngle - KneeFlexion;
        }

        /// <summary>
        /// <paramref name="forward"/> and <paramref name="down"/> are the distances from the hip to the wanted
        /// ankle position. A target farther than the leg is long is reached as far as possible along its direction.
        /// </summary>
        public static Pose Solve(float thighLength, float shinLength, float forward, float down)
        {
            const float margin = 0.001f;
            float reach = Mathf.Sqrt(forward * forward + down * down);
            float distance = Mathf.Clamp(reach, Mathf.Abs(thighLength - shinLength) + margin, thighLength + shinLength - margin);

            // Angle of the hip-to-ankle line from straight down.
            float lineAngle = Mathf.Atan2(forward, down);
            float hipOffset = Mathf.Acos(Mathf.Clamp((thighLength * thighLength + distance * distance - shinLength * shinLength) / (2f * thighLength * distance), -1f, 1f));
            float kneeInterior = Mathf.Acos(Mathf.Clamp((thighLength * thighLength + shinLength * shinLength - distance * distance) / (2f * thighLength * shinLength), -1f, 1f));

            float thigh = (lineAngle + hipOffset) * Mathf.Rad2Deg;
            float flexion = (Mathf.PI - kneeInterior) * Mathf.Rad2Deg;
            return new Pose(thigh, flexion);
        }

        /// <summary>Where the ankle ends up for a pose: x = forward, y = down, from the hip. The reverse of <see cref="Solve"/>.</summary>
        public static Vector2 AnklePosition(float thighLength, float shinLength, Pose pose)
        {
            float thigh = pose.ThighAngle * Mathf.Deg2Rad;
            float shin = pose.ShinAngle * Mathf.Deg2Rad;
            return new Vector2(
                thighLength * Mathf.Sin(thigh) + shinLength * Mathf.Sin(shin),
                thighLength * Mathf.Cos(thigh) + shinLength * Mathf.Cos(shin));
        }
    }
}
