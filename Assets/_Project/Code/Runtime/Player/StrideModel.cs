using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Walking rhythm of the player. The phase counts steps: it grows by one every time the player covers
    /// one step length on the ground, and every whole number is a foot touching the floor. Footsteps and
    /// head bob both read this one phase, so the bob always dips exactly when a footstep sounds.
    /// </summary>
    public sealed class StrideModel
    {
        /// <summary>Steps taken so far; the fractional part is how far into the current step the player is.</summary>
        public float Phase { get; private set; }

        /// <summary>
        /// Advances the stride by a distance walked on the ground and returns how many feet touched the
        /// floor on the way (usually 0 or 1).
        /// </summary>
        public int Advance(float distance, float stepLength)
        {
            if (distance <= 0f || stepLength <= 0f) return 0;

            int before = Mathf.FloorToInt(Phase);
            Phase += distance / stepLength;
            return Mathf.FloorToInt(Phase) - before;
        }

        /// <summary>Puts the stride on a foot contact, e.g. after landing, so the next step comes a full step later.</summary>
        public void PlantFoot() => Phase = Mathf.Round(Phase);

        /// <summary>
        /// Camera offset for a stride phase at full intensity. The head is lowest when a foot lands (whole
        /// phase) and highest in mid-step; it sways sideways once per two steps, over the standing foot.
        /// </summary>
        public static Vector2 BobOffset(float phase, float verticalAmplitude, float lateralAmplitude)
        {
            float vertical = -verticalAmplitude * Mathf.Cos(2f * Mathf.PI * phase);
            float lateral = lateralAmplitude * Mathf.Sin(Mathf.PI * phase);
            return new Vector2(lateral, vertical);
        }
    }
}
