using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>Path helpers for bent parts (hoses, cables).</summary>
    public static partial class Props
    {
        /// <summary>A smooth path through <paramref name="points"/> (Catmull-Rom), <paramref name="perSegment"/> samples between each pair.</summary>
        private static List<Vector3> SmoothPath(IList<Vector3> points, int perSegment)
        {
            var path = new List<Vector3>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[Mathf.Max(i - 1, 0)];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[Mathf.Min(i + 2, points.Count - 1)];
                for (int k = 0; k < perSegment; k++)
                {
                    float t = k / (float)perSegment;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    path.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }
            path.Add(points[points.Count - 1]);
            return path;
        }
    }
}
