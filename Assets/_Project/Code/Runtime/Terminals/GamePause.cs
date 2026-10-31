using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// Pauses the game while something asks for it (reading a lore article, task 3.2). Several things may ask at
    /// the same time; the game runs again when the last one lets go. The time scale is stopped, so physics,
    /// animations and anything on scaled time stand still, while the interface keeps working.
    /// </summary>
    public static class GamePause
    {
        private static readonly HashSet<object> holders = new HashSet<object>();

        public static bool IsPaused => holders.Count > 0;

        /// <summary>Raised when the game stops (true) or runs again (false).</summary>
        public static event Action<bool> Changed;

        public static void Request(object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (holders.Add(owner) && holders.Count == 1) Apply(true);
        }

        public static void Release(object owner)
        {
            if (owner != null && holders.Remove(owner) && holders.Count == 0) Apply(false);
        }

        private static void Apply(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
            Changed?.Invoke(paused);
        }

        // A new Play session starts running, whatever was left over from the last one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            holders.Clear();
            Changed = null;
            Time.timeScale = 1f;
        }
    }
}
