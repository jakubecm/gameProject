using System.Collections.Generic;
using UnityEngine;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The coffee in the mug, as physics would have it. The coffee is a body of liquid in the inside of the mug (the real inner profile of
    /// the Mug prop, a little inset) whose free surface is a plane that stays level in the world. Only that surface is drawn: the mug is
    /// opaque, so the rest of the liquid cannot be seen. For every tilt and every amount the plane is moved until the volume under it is the
    /// wanted share of a full mug (the volume is integrated exactly over slices of the mug), but never higher than the lowest point of the
    /// rim: what does not fit runs out over it (<see cref="Capacity"/>). The surface is the exact cut of the plane through the mug, so it is
    /// a disc when the mug stands, an ellipse-like pool when it tilts, and a long thin film along the low wall in the last moments before the
    /// coffee has gone over the rim, with no switch of shape anywhere: everything is continuous in the tilt and in the amount.
    /// </summary>
    internal sealed class CoffeeLiquidMesh
    {
        // Mug interior in the mug's local space (pivot at the centre of its base), inset 0.4 mm from the ceramic: (radius, height) from the
        // flat floor, round its corner, up the wall to the rim. The radius never shrinks with the height, so the interior is convex.
        private static readonly Vector2[] Profile =
        {
            new Vector2(0.0258f, 0.0068f), new Vector2(0.0288f, 0.0089f), new Vector2(0.0303f, 0.0129f), new Vector2(0.0382f, 0.0814f),
        };

        public const float FloorY = 0.0068f;
        public const float RimY = 0.0814f;
        private const float RimRadius = 0.0382f;

        // Sectors of the polygon the interior is approximated by (the cut is clipped against its faces), and slices of the volume integral.
        private const int Sectors = 64;
        private const int Slices = 128;
        private const int SolverSteps = 28;
        private const float SolverLow = -0.12f;
        private const float Big = 0.12f;

        // Below this share of a full mug, while drinking, the last of the coffee is a film that draws back to the lowest point of the rim
        // (its size follows the logarithm of the amount, so the draw-back is slow and even although the amount drops ever faster) and is gone there, instead of staying a long thin strip along the wall until it vanishes.
        private const float FilmFill = 0.04f;

        /// <summary>Below this share of a full mug there is no coffee to draw: the film has drawn back to a point.</summary>
        public const float VanishFill = 0.00001f;

        // Faces of the interior: face points p with normal . p <= offset.
        private static readonly Vector3[] FaceNormals;
        private static readonly float[] FaceOffsets;

        static CoffeeLiquidMesh()
        {
            var faces = new List<Vector3>();
            var offsets = new List<float>();
            float half = Mathf.PI / Sectors;
            float shrink = Mathf.Cos(half);
            for (int band = 0; band + 1 < Profile.Length; band++)
            {
                var from = Profile[band];
                var to = Profile[band + 1];
                // The face between two neighbouring generatrices is flat: its radial distance is the ring radius times cos(half a sector).
                var radial = new Vector2(to.y - from.y, -(to.x - from.x) * shrink).normalized;
                float offset = radial.x * from.x * shrink + radial.y * from.y;
                for (int sector = 0; sector < Sectors; sector++)
                {
                    float angle = (sector + 0.5f) * 2f * half;
                    faces.Add(new Vector3(Mathf.Cos(angle) * radial.x, radial.y, Mathf.Sin(angle) * radial.x));
                    offsets.Add(offset);
                }
            }
            faces.Add(Vector3.down);
            offsets.Add(-FloorY);
            faces.Add(Vector3.up);
            offsets.Add(RimY);
            FaceNormals = faces.ToArray();
            FaceOffsets = offsets.ToArray();
        }

        private readonly List<Vector3> polygon = new List<Vector3>();
        private readonly List<Vector3> scratch = new List<Vector3>();
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();

        /// <summary>
        /// Fills <paramref name="mesh"/> with the surface. <paramref name="up"/> is the world's up direction in the mug's local space (the
        /// surface normal), <paramref name="fill"/> the share of a full mug (0 to 1) and <paramref name="fullLevelY"/> the height of the
        /// surface of a full, upright mug. The mesh is empty when there is no coffee. With <paramref name="draining"/> (the mug is being
        /// emptied over its rim) the last film shrinks towards the lowest point of the rim.
        /// </summary>
        public void Rebuild(Mesh mesh, Vector3 up, float fill, float fullLevelY, bool draining = false)
        {
            mesh.Clear();
            up.Normalize();
            float target = fill * Volume(Vector3.up, fullLevelY);
            float rim = RimPlane(up);
            if (target <= 0f || Volume(up, rim) <= 0f) return;

            // Move the plane until the volume under it is the wanted one; it cannot rise above the lowest point of the rim.
            float low = SolverLow;
            float high = rim;
            if (Volume(up, high) > target)
            {
                for (int i = 0; i < SolverSteps; i++)
                {
                    float middle = (low + high) * 0.5f;
                    if (Volume(up, middle) < target) low = middle;
                    else high = middle;
                }
            }
            BuildSurface(up, high);
            if (polygon.Count < 3) return;
            if (draining && fill < FilmFill) DrawBackToRim(up, high, Mathf.Clamp01(Mathf.Log(fill / VanishFill) / Mathf.Log(FilmFill / VanishFill)));

            vertices.Clear();
            normals.Clear();
            triangles.Clear();
            vertices.AddRange(polygon);
            for (int i = 0; i < polygon.Count; i++) normals.Add(up);
            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        // Scales the surface towards the lowest point of the rim (projected on the surface plane) to <paramref name="share"/> of its size.
        private void DrawBackToRim(Vector3 up, float offset, float share)
        {
            float lean = Mathf.Sqrt(up.x * up.x + up.z * up.z);
            if (lean < 1e-5f) return;

            var rim = new Vector3(-RimRadius * up.x / lean, RimY, -RimRadius * up.z / lean);
            var anchor = rim + up * (offset - Vector3.Dot(up, rim));
            for (int i = 0; i < polygon.Count; i++) polygon[i] = anchor + (polygon[i] - anchor) * share;
        }

        /// <summary>
        /// The most coffee (share of a full mug, 0 to 1) the mug holds with <paramref name="up"/> (world up in the mug's space) without it
        /// running over the lowest point of the rim. Falls smoothly to 0 as the mug is tipped towards horizontal.
        /// </summary>
        public static float Capacity(Vector3 up, float fullLevelY)
        {
            up.Normalize();
            return Mathf.Clamp01(Volume(up, RimPlane(up)) / Volume(Vector3.up, fullLevelY));
        }

        /// <summary>The share of a full mug of an upright mug filled to <paramref name="level"/> (0 at the floor, 1 at the surface of a full mug).</summary>
        public static float FillAtLevel(float level, float fullLevelY)
        {
            float height = Mathf.Lerp(FloorY, fullLevelY, Mathf.Clamp01(level));
            return Mathf.Clamp01(Volume(Vector3.up, height) / Volume(Vector3.up, fullLevelY));
        }

        // Offset of the plane with the normal up through the lowest point of the rim.
        private static float RimPlane(Vector3 up) => up.y * RimY - RimRadius * Mathf.Sqrt(up.x * up.x + up.z * up.z);

        // Volume of the interior under the plane (normal . p = offset; for an upright mug at the height h pass Vector3.up and h), summed over
        // slices of the mug: each slice is a disc cut by a straight line, whose area is a circular segment.
        private static float Volume(Vector3 normal, float offset)
        {
            float lean = Mathf.Sqrt(normal.x * normal.x + normal.z * normal.z);
            float step = (RimY - FloorY) / Slices;
            float sum = 0f;
            for (int i = 0; i < Slices; i++)
            {
                float y = FloorY + (i + 0.5f) * step;
                float radius = RadiusAt(y);
                float along = offset - normal.y * y;
                if (lean < 1e-5f)
                {
                    if (along >= 0f) sum += Mathf.PI * radius * radius;
                    continue;
                }

                float s = along / lean;
                if (s >= radius) sum += Mathf.PI * radius * radius;
                else if (s > -radius) sum += radius * radius * Mathf.Acos(-s / radius) + s * Mathf.Sqrt(radius * radius - s * s);
            }
            return sum * step;
        }

        private static float RadiusAt(float y)
        {
            for (int i = 1; i < Profile.Length; i++)
            {
                if (y > Profile[i].y) continue;
                float t = Mathf.InverseLerp(Profile[i - 1].y, Profile[i].y, y);
                return Mathf.Lerp(Profile[i - 1].x, Profile[i].x, t);
            }
            return Profile[Profile.Length - 1].x;
        }

        // The cut of the plane through the interior: a big square in the plane, clipped by every face of the interior. The interior is
        // convex, so the result is one convex polygon, ordered counter-clockwise seen from the normal.
        private void BuildSurface(Vector3 normal, float offset)
        {
            var helper = Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right;
            var u = Vector3.Cross(helper, normal).normalized * Big;
            var v = Vector3.Cross(normal, u).normalized * Big;
            var centre = normal * offset;
            polygon.Clear();
            polygon.Add(centre - u - v);
            polygon.Add(centre + u - v);
            polygon.Add(centre + u + v);
            polygon.Add(centre - u + v);

            for (int f = 0; f < FaceNormals.Length && polygon.Count >= 3; f++) Clip(FaceNormals[f], FaceOffsets[f]);
        }

        // Keeps the part of the polygon with face . p <= offset.
        private void Clip(Vector3 face, float offset)
        {
            scratch.Clear();
            int count = polygon.Count;
            var previous = polygon[count - 1];
            float previousSide = Vector3.Dot(face, previous) - offset;
            for (int i = 0; i < count; i++)
            {
                var current = polygon[i];
                float side = Vector3.Dot(face, current) - offset;
                if ((side <= 0f) != (previousSide <= 0f))
                {
                    scratch.Add(Vector3.Lerp(previous, current, previousSide / (previousSide - side)));
                }
                if (side <= 0f) scratch.Add(current);
                previous = current;
                previousSide = side;
            }
            polygon.Clear();
            polygon.AddRange(scratch);
        }
    }
}
