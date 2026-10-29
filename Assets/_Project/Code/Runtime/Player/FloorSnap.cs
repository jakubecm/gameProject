using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The CharacterController floats about a skin width above the floor, by a varying amount, and the floor
    /// may be a ramp. A visible body is lowered or raised by this offset so the soles stand on the floor that
    /// is really under the player.
    /// </summary>
    public static class FloorSnap
    {
        /// <summary>Height of the floor under the player relative to the player's position (usually between -0.05 and 0). Zero in the air.</summary>
        public static float Offset(Vector3 playerPosition, bool grounded)
        {
            if (!grounded) return 0f;

            // The ray starts inside the player's own capsule, which a ray never hits from the inside.
            if (Physics.Raycast(playerPosition + Vector3.up * 0.2f, Vector3.down, out var hit, 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                return Mathf.Clamp(hit.point.y - playerPosition.y, -0.1f, 0.1f);
            }
            return 0f;
        }
    }
}
