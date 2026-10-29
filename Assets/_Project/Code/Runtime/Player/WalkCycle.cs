using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Where one foot is during a walk, relative to the hip, for a point in the walking cycle. A cycle is two
    /// steps: for the first half the foot is on the floor and slides back under the body (so it does not slip
    /// on the ground), for the second half it swings forward through the air with a lift. A whole number of
    /// steps (the <see cref="StrideModel"/> phase) is a foot landing, so the left foot lands on even phases and
    /// the right foot on odd ones; the footstep sound and the head bob dip at the same moments.
    /// </summary>
    public static class WalkCycle
    {
        /// <summary>The position in the cycle, 0 to 1, of a foot for a stride phase. The right foot is half a cycle behind.</summary>
        public static float CycleOf(float stridePhase, bool rightFoot)
        {
            float cycle = stridePhase * 0.5f + (rightFoot ? 0.5f : 0f);
            return cycle - Mathf.Floor(cycle);
        }

        /// <summary>
        /// Ankle position for a cycle position. <paramref name="halfStride"/> is half the step length (the foot
        /// travels from +half to -half under the body); <paramref name="lift"/> is how high the foot rises
        /// while swinging. Returns x = forward from the hip, y = height above the floor level of the planted foot.
        /// </summary>
        public static Vector2 Foot(float cycle, float halfStride, float lift)
        {
            if (cycle < 0.5f)
            {
                float stance = cycle / 0.5f;
                return new Vector2(Mathf.Lerp(halfStride, -halfStride, stance), 0f);
            }

            float swing = (cycle - 0.5f) / 0.5f;
            float eased = Mathf.SmoothStep(0f, 1f, swing);
            return new Vector2(Mathf.Lerp(-halfStride, halfStride, eased), lift * Mathf.Sin(Mathf.PI * swing));
        }
    }
}
