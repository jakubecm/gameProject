using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// A CCTV camera. Its feed camera stays disabled; a <see cref="SecurityMonitorWall"/> turns it on for
    /// single frames to render into a monitor, so cameras cost nothing while nobody watches the monitors.
    /// Every enabled camera in the scene registers itself in <see cref="All"/>.
    /// </summary>
    public class SecurityCamera : MonoBehaviour
    {
        private static readonly List<SecurityCamera> Registered = new List<SecurityCamera>();

        [SerializeField] private Camera feedCamera;

        /// <summary>All active cameras, in the order they were enabled.</summary>
        public static IReadOnlyList<SecurityCamera> All => Registered;

        public Camera FeedCamera => feedCamera;

        private void OnEnable()
        {
            if (feedCamera != null) feedCamera.enabled = false;
            Registered.Add(this);
        }

        private void OnDisable()
        {
            Registered.Remove(this);
        }

        /// <summary>Renders the feed into <paramref name="target"/> this frame (or stops rendering when false).</summary>
        internal void SetRendering(RenderTexture target, bool render)
        {
            if (feedCamera == null) return;
            feedCamera.targetTexture = target;
            feedCamera.enabled = render && target != null;
        }
    }
}
