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

        /// <summary>
        /// Identifies the shot: the room type (all offices look alike) or the hall, and the camera's pose in that room or hall tile, rounded.
        /// Two cameras with the same key show the same picture.
        /// </summary>
        public string ShotKey
        {
            get
            {
                var anchor = transform;
                for (var t = transform; t != null; t = t.parent)
                {
                    if (t.name.StartsWith("Room_") || t.name.StartsWith("Cell_")) { anchor = t; break; }
                }
                var view = feedCamera != null ? feedCamera.transform : transform;
                var position = anchor.InverseTransformPoint(view.position);
                var angles = (Quaternion.Inverse(anchor.rotation) * view.rotation).eulerAngles;
                string place = anchor.name.StartsWith("Room_") ? anchor.name.Replace("(Clone)", "") : "Hall";
                return $"{place}|{Mathf.RoundToInt(position.x * 10f)},{Mathf.RoundToInt(position.y * 10f)},{Mathf.RoundToInt(position.z * 10f)}|" +
                       $"{Mathf.RoundToInt(angles.x / 10f) % 36},{Mathf.RoundToInt(angles.y / 10f) % 36}";
            }
        }

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
