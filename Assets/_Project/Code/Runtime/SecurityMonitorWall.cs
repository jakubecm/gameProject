using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The video wall of the security office. Its monitors cycle through every <see cref="SecurityCamera"/>
    /// in the scene, a page of cameras at a time, like a real CCTV tour. Feeds are rendered round robin, only a
    /// few cameras per frame, and only while the player's camera is close enough to see the wall.
    /// </summary>
    public class SecurityMonitorWall : MonoBehaviour
    {
        [SerializeField] private Vector2Int resolution = new Vector2Int(256, 144);
        [Tooltip("Seconds before the monitors switch to the next page of cameras.")]
        [SerializeField, Min(1f)] private float pageSeconds = 8f;
        [Tooltip("Feeds rendered per frame; every monitor refreshes once per (monitors / this) frames.")]
        [SerializeField, Min(1)] private int feedsPerFrame = 2;
        [Tooltip("The wall only renders while the main camera is closer than this.")]
        [SerializeField, Min(1f)] private float activeDistance = 20f;

        private readonly List<SecurityCamera> rendering = new List<SecurityCamera>();
        private SecurityMonitor[] monitors;
        private int page;
        private int nextMonitor;
        private float pageTimer;

        public IReadOnlyList<SecurityMonitor> Monitors => monitors;

        private void Awake()
        {
            monitors = GetComponentsInChildren<SecurityMonitor>();
        }

        private void OnDisable() => StopRendering();

        private void LateUpdate()
        {
            // Cameras turned on last frame have rendered by now.
            StopRendering();

            var viewer = Camera.main;
            if (viewer == null || monitors.Length == 0 ||
                (viewer.transform.position - transform.position).sqrMagnitude > activeDistance * activeDistance)
            {
                return;
            }

            pageTimer += Time.deltaTime;
            if (pageTimer >= pageSeconds)
            {
                pageTimer = 0f;
                page++;
            }
            AssignFeeds();

            for (int i = 0; i < Mathf.Min(feedsPerFrame, monitors.Length); i++)
            {
                var monitor = monitors[nextMonitor];
                nextMonitor = (nextMonitor + 1) % monitors.Length;
                if (monitor.Feed == null) continue;

                monitor.Prepare(resolution.x, resolution.y);
                monitor.Feed.SetRendering(monitor.Texture, true);
                rendering.Add(monitor.Feed);
            }
        }

        private void AssignFeeds()
        {
            var cameras = SecurityCamera.All;
            int count = cameras.Count;
            for (int i = 0; i < monitors.Length; i++)
            {
                monitors[i].Feed = count == 0 ? null : cameras[(page * monitors.Length + i) % count];
            }
        }

        private void StopRendering()
        {
            foreach (var camera in rendering)
            {
                if (camera != null) camera.SetRendering(null, false);
            }
            rendering.Clear();
        }
    }
}
