using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The video wall of the security office. Its monitors tour all <see cref="SecurityCamera"/>s of the scene: every 8 s, all monitors
    /// at once, the next cameras come up, and no two monitors ever show the same shot (<see cref="SecurityCamera.ShotKey"/>: all offices
    /// look alike, so are some corridor and hall views). The wall is live only while the player is in the room: the feeds then
    /// render round robin, a few per frame. When the player leaves, the pictures freeze
    /// as they are and nothing renders or changes until they come back. Each feed is rendered once at the start, so the screens never
    /// start black.
    /// </summary>
    public class SecurityMonitorWall : MonoBehaviour
    {
        [SerializeField] private Vector2Int resolution = new Vector2Int(256, 144);
        // Seconds before the monitors switch to the next cameras (a constant: a value saved in the prefab must not change it).
        private const float PageSeconds = 8f;

        [Tooltip("Feeds rendered per frame; every monitor refreshes once per (monitors / this) frames.")]
        [SerializeField, Min(1)] private int feedsPerFrame = 2;

        private readonly List<SecurityCamera> rendering = new List<SecurityCamera>();
        private readonly HashSet<string> shown = new HashSet<string>();
        private SecurityMonitor[] monitors;
        private Transform room;
        private int nextMonitor;
        private int cursor;
        private int cameraCount = -1;
        private float pageTimer;

        public IReadOnlyList<SecurityMonitor> Monitors => monitors;

        private void Awake()
        {
            monitors = GetComponentsInChildren<SecurityMonitor>();
            room = transform;
            for (var t = transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith("Room_")) { room = t; break; }
            }
        }

        private void OnDisable() => StopRendering();

        private void LateUpdate()
        {
            // Cameras turned on last frame have rendered by now.
            StopRendering();
            if (monitors.Length == 0) return;

            bool live = IsPlayerInRoom();
            if (live)
            {
                pageTimer += Time.deltaTime;
                if (pageTimer >= PageSeconds)
                {
                    pageTimer = 0f;
                    AssignFeeds();
                }
            }
            if (cameraCount != SecurityCamera.All.Count)
            {
                cameraCount = SecurityCamera.All.Count;
                AssignFeeds();
            }

            // A feed that has not been rendered yet gets its first picture now (the start, a new set of shots), live or not.
            foreach (var monitor in monitors)
            {
                if (monitor.Feed != null && monitor.RenderedFeed != monitor.Feed) Render(monitor);
            }
            if (!live) return;

            // Round robin, a few feeds per frame.
            for (int i = 0; i < Mathf.Min(feedsPerFrame, monitors.Length); i++)
            {
                var monitor = monitors[nextMonitor];
                nextMonitor = (nextMonitor + 1) % monitors.Length;
                if (monitor.Feed != null && !rendering.Contains(monitor.Feed)) Render(monitor);
            }
        }

        private bool IsPlayerInRoom()
        {
            var viewer = Camera.main;
            if (viewer == null) return false;

            var local = room.InverseTransformPoint(viewer.transform.position);
            return Mathf.Abs(local.x) <= RoomContract.CellHalfSize && Mathf.Abs(local.z) <= RoomContract.CellHalfSize;
        }

        // Each monitor takes the next camera of the list whose shot is not on another monitor of this page; the list is walked round
        // and round, so every camera comes up in turn.
        private void AssignFeeds()
        {
            var cameras = SecurityCamera.All;
            int count = cameras.Count;
            shown.Clear();
            foreach (var monitor in monitors)
            {
                monitor.Feed = null;
                for (int tried = 0; tried < count; tried++)
                {
                    var camera = cameras[cursor++ % count];
                    if (!shown.Add(camera.ShotKey)) continue;

                    monitor.Feed = camera;
                    break;
                }
            }
            cursor %= Mathf.Max(count, 1);
        }

        private void Render(SecurityMonitor monitor)
        {
            monitor.Prepare(resolution.x, resolution.y);
            monitor.Feed.SetRendering(monitor.Texture, true);
            monitor.RenderedFeed = monitor.Feed;
            rendering.Add(monitor.Feed);
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
