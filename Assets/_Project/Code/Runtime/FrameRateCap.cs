using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Caps the frame rate. Without a cap (and without vertical sync) a laptop renders as many frames as it can, runs hot and slows itself down,
    /// which shows as stutter; an even 60 frames per second is steadier than a fast and a slow one in turns. The settings menu (task 5.6) can
    /// change <see cref="TargetFrameRate"/>; 0 or less removes the cap.
    /// </summary>
    public static class FrameRateCap
    {
        /// <summary>Frames per second the game aims for.</summary>
        public const int DefaultTargetFrameRate = 60;

        private static int targetFrameRate = DefaultTargetFrameRate;

        public static int TargetFrameRate
        {
            get => targetFrameRate;
            set
            {
                targetFrameRate = value;
                Apply();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // The cap does not work together with vertical sync, which would set the pace itself.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate > 0 ? targetFrameRate : -1;
        }
    }
}
