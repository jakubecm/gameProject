using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// What the player wants to do this frame, already independent of the input device. The input
    /// reader produces it; the controller consumes it, so the controller can also be driven by tests.
    /// </summary>
    public struct PlayerCommand
    {
        /// <summary>Walk direction in the player's space: x = right, y = forward, magnitude up to 1.</summary>
        public Vector2 Move;

        /// <summary>View rotation this frame in degrees: x = turn right, y = look up.</summary>
        public Vector2 Look;

        /// <summary>True while the player wants to be crouched.</summary>
        public bool Crouch;

        /// <summary>True while the player wants to sprint; the controller decides whether it can.</summary>
        public bool Sprint;

        /// <summary>True on the frame the jump button was pressed; the controller decides whether it can jump.</summary>
        public bool Jump;
    }
}
