using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// One foot touching the floor. Sound (task 4.10) and anything else that reacts to steps listen to
    /// <see cref="FirstPersonController.Footstep"/> and choose what to play from this information.
    /// </summary>
    public readonly struct FootstepEvent
    {
        /// <summary>World position of the player's feet.</summary>
        public readonly Vector3 Position;

        /// <summary>Horizontal speed in m/s when the foot landed.</summary>
        public readonly float Speed;

        public readonly bool Crouching;

        public readonly bool Sprinting;

        /// <summary>The collider under the feet, or null; use it (or its material) to pick a surface sound.</summary>
        public readonly Collider Ground;

        public FootstepEvent(Vector3 position, float speed, bool crouching, bool sprinting, Collider ground)
        {
            Position = position;
            Speed = speed;
            Crouching = crouching;
            Sprinting = sprinting;
            Ground = ground;
        }

        /// <summary>Physics material of the ground, or null.</summary>
        public PhysicsMaterial GroundMaterial => Ground != null ? Ground.sharedMaterial : null;
    }
}
