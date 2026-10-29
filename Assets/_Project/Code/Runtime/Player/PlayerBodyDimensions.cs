namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Sizes of the player's body in metres, shared by the tool that builds the body and the animator that
    /// moves it. They add up to the 1.8 m capsule of the <see cref="FirstPersonController"/>: ankle 0.08 +
    /// legs 1.0 + pelvis and torso 0.6 + neck and head 0.12 on a 1.8 m person with the eyes at 1.66 m.
    /// </summary>
    public static class PlayerBodyDimensions
    {
        public const float ThighLength = 0.5f;
        public const float ShinLength = 0.5f;

        /// <summary>Height of the ankle joint above the sole of the foot.</summary>
        public const float AnkleHeight = 0.08f;

        public const float HipHalfWidth = 0.09f;

        /// <summary>Height of the pelvis joint above the floor when standing straight.</summary>
        public const float StandingHipHeight = 0.95f;

        /// <summary>Distance from the hips up to the eyes; the body hangs below the camera by this much.</summary>
        public const float HipToEyes = 0.71f;

        /// <summary>The chest joint sits this far above the pelvis joint.</summary>
        public const float PelvisToChest = 0.09f;

        public const float TorsoHeight = 0.52f;
        public const float ShoulderHalfWidth = 0.24f;

        /// <summary>Height of the shoulder joint above the chest joint.</summary>
        public const float ShoulderHeight = 0.46f;

        public const float UpperArmLength = 0.30f;
        public const float ForearmLength = 0.28f;

        public const float FootLength = 0.26f;
        public const float FootWidth = 0.1f;
    }
}
