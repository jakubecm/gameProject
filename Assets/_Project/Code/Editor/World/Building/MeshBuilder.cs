using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Builds small, smooth detail meshes (sprinklers, camera domes, speakers) that the box and 12-sided cylinder
    /// primitives of <see cref="ModelUtility"/> cannot do: surfaces of revolution with real smooth shading
    /// (<see cref="AddLathe"/>) and flat strips bent along a path (<see cref="AddStrip"/>). One builder makes one mesh
    /// with one material. Triangle winding is derived from the intended surface normal, so a profile only has to say
    /// which way it faces. A mesh made in memory is not stored by a prefab (it would come back empty), so
    /// <see cref="Attach"/> saves every mesh as an asset in <see cref="MeshFolder"/> (rewritten in place, same GUID).
    /// </summary>
    internal sealed class MeshBuilder
    {
        /// <summary>A point of a lathe profile: radius, height and whether the surface has a hard edge here.</summary>
        internal readonly struct ProfilePoint
        {
            public readonly float R;
            public readonly float Y;
            public readonly bool Hard;

            public ProfilePoint(float r, float y, bool hard = false)
            {
                R = r;
                Y = y;
                Hard = hard;
            }
        }

        public const string MeshFolder = "Assets/_Project/Art/Meshes/Details";

        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        public static ProfilePoint P(float r, float y, bool hard = false) => new ProfilePoint(r, y, hard);

        /// <summary>
        /// A surface of revolution around local +Y. Walk the profile so that the visible side is on your right
        /// (r to the right, y up): up the outside of a cylinder, outward along an underside that faces down, inward
        /// along a top that faces up. <paramref name="smooth"/> false gives flat facets (a hex nut with 6 segments).
        /// <paramref name="rotation"/> and <paramref name="offset"/> place the whole piece.
        /// </summary>
        public void AddLathe(IList<ProfilePoint> profile, int segments, bool smooth, Vector3 offset, Quaternion rotation,
            float startDegrees = 0f, float sweepDegrees = 360f, bool uvByAngle = false)
        {
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            foreach (var point in profile)
            {
                minY = Mathf.Min(minY, point.Y);
                maxY = Mathf.Max(maxY, point.Y);
            }
            float rangeY = Mathf.Max(maxY - minY, 1e-5f);
            int count = profile.Count;
            var segmentNormals = new Vector2[count - 1];
            for (int i = 0; i < count - 1; i++)
            {
                var d = new Vector2(profile[i + 1].R - profile[i].R, profile[i + 1].Y - profile[i].Y);
                segmentNormals[i] = new Vector2(d.y, -d.x).normalized;
            }

            Vector2 PointNormal(int point, int segment)
            {
                if (!smooth || profile[point].Hard || point == 0 || point == count - 1) return segmentNormals[segment];
                return (segmentNormals[point - 1] + segmentNormals[point]).normalized;
            }

            for (int i = 0; i < count - 1; i++)
            {
                for (int k = 0; k < segments; k++)
                {
                    float a0 = (startDegrees + sweepDegrees * k / segments) * Mathf.Deg2Rad;
                    float a1 = (startDegrees + sweepDegrees * (k + 1) / segments) * Mathf.Deg2Rad;
                    float mid = (a0 + a1) / 2f;
                    var n0 = PointNormal(i, i);
                    var n1 = PointNormal(i + 1, i);

                    Vector3 Position(int point, float angle) =>
                        offset + rotation * new Vector3(profile[point].R * Mathf.Cos(angle), profile[point].Y, profile[point].R * Mathf.Sin(angle));

                    Vector3 Normal(Vector2 n, float angle) =>
                        rotation * new Vector3(n.x * Mathf.Cos(angle), n.y, n.x * Mathf.Sin(angle));

                    var p00 = Position(i, a0);
                    var p01 = Position(i, a1);
                    var p10 = Position(i + 1, a0);
                    var p11 = Position(i + 1, a1);
                    float angle0 = smooth ? a0 : mid;
                    float angle1 = smooth ? a1 : mid;
                    Vector2? uv00 = null, uv10 = null, uv11 = null, uv01 = null;
                    if (uvByAngle)
                    {
                        float u0 = k / (float)segments;
                        float u1 = (k + 1) / (float)segments;
                        float v0 = (profile[i].Y - minY) / rangeY;
                        float v1 = (profile[i + 1].Y - minY) / rangeY;
                        uv00 = new Vector2(u0, v0);
                        uv10 = new Vector2(u0, v1);
                        uv11 = new Vector2(u1, v1);
                        uv01 = new Vector2(u1, v0);
                    }
                    AddTriangle(p00, Normal(n0, angle0), p10, Normal(n1, angle0), p11, Normal(n1, angle1), uv00, uv10, uv11);
                    AddTriangle(p00, Normal(n0, angle0), p11, Normal(n1, angle1), p01, Normal(n0, angle1), uv00, uv11, uv01);
                }
            }
        }

        /// <summary>
        /// A strip of rectangular section bent along <paramref name="path"/>: <paramref name="width"/> across
        /// <paramref name="side"/> (a fixed direction the path never turns about) and <paramref name="thickness"/> in the
        /// direction perpendicular to both the path and the side. Flat shaded, closed at both ends.
        /// </summary>
        public void AddStrip(IList<Vector3> path, Vector3 side, float width, float thickness, Vector3 offset, Quaternion rotation)
        {
            side = side.normalized;
            var corners = new Vector3[path.Count][];
            var thick = new Vector3[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                var tangent = ((i < path.Count - 1 ? path[i + 1] : path[i]) - (i > 0 ? path[i - 1] : path[i])).normalized;
                thick[i] = Vector3.Cross(side, tangent).normalized;
                var p = path[i];
                corners[i] = new[]
                {
                    p + thick[i] * thickness / 2f + side * width / 2f,
                    p + thick[i] * thickness / 2f - side * width / 2f,
                    p - thick[i] * thickness / 2f - side * width / 2f,
                    p - thick[i] * thickness / 2f + side * width / 2f,
                };
            }

            Vector3 T(Vector3 v) => offset + rotation * v;
            Vector3 N(Vector3 v) => rotation * v;
            for (int i = 0; i < path.Count - 1; i++)
            {
                for (int c = 0; c < 4; c++)
                {
                    int n = (c + 1) % 4;
                    var outward = (corners[i][c] + corners[i][n] + corners[i + 1][c] + corners[i + 1][n]) / 4f
                                  - (path[i] + path[i + 1]) / 2f;
                    AddFlatQuad(T(corners[i][c]), T(corners[i][n]), T(corners[i + 1][n]), T(corners[i + 1][c]), N(outward));
                }
            }
            var start = path[1] - path[0];
            var end = path[path.Count - 1] - path[path.Count - 2];
            AddFlatQuad(T(corners[0][0]), T(corners[0][1]), T(corners[0][2]), T(corners[0][3]), N(-start));
            int last = path.Count - 1;
            AddFlatQuad(T(corners[last][0]), T(corners[last][1]), T(corners[last][2]), T(corners[last][3]), N(end));
        }

        /// <summary>
        /// A vertical prism: <paramref name="outline"/> is a convex polygon in the XZ plane (x, z), listed counter-clockwise seen
        /// from above with z pointing up the page, extruded from <paramref name="y0"/> to <paramref name="y1"/>. Flat shaded;
        /// for panels with chamfered edges.
        /// </summary>
        public void AddPrism(IList<Vector2> outline, float y0, float y1, Vector3 offset)
        {
            int count = outline.Count;
            for (int i = 0; i < count; i++)
            {
                var p = outline[i];
                var q = outline[(i + 1) % count];
                var edge = q - p;
                var outward = new Vector3(edge.y, 0f, -edge.x);
                AddPanel(offset + new Vector3(q.x, y0, q.y), offset + new Vector3(p.x, y0, p.y), offset + new Vector3(p.x, y1, p.y), offset + new Vector3(q.x, y1, q.y), outward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }
            for (int i = 1; i < count - 1; i++)
            {
                AddFlatTriangle(offset + new Vector3(outline[0].x, y1, outline[0].y), offset + new Vector3(outline[i + 1].x, y1, outline[i + 1].y),
                    offset + new Vector3(outline[i].x, y1, outline[i].y), Vector3.up);
                AddFlatTriangle(offset + new Vector3(outline[0].x, y0, outline[0].y), offset + new Vector3(outline[i].x, y0, outline[i].y),
                    offset + new Vector3(outline[i + 1].x, y0, outline[i + 1].y), Vector3.down);
            }
        }

        /// <summary>An axis-aligned box between two corners (flat shaded); the top and bottom faces can be left out.</summary>
        public void AddBox(Vector3 min, Vector3 max, bool top = true, bool bottom = true)
        {
            if (bottom) AddFlatQuad(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), new Vector3(min.x, min.y, max.z), Vector3.down);
            if (top) AddFlatQuad(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z), Vector3.up);
            AddFlatQuad(new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, min.z), new Vector3(min.x, max.y, max.z), new Vector3(min.x, min.y, max.z), Vector3.left);
            AddFlatQuad(new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), new Vector3(max.x, min.y, max.z), Vector3.right);
            AddFlatQuad(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, min.z), Vector3.back);
            AddFlatQuad(new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, max.z), new Vector3(min.x, max.y, max.z), Vector3.forward);
        }

        /// <summary>
        /// A slab with rounded corners and a bevelled top edge, for the plates of switches and sockets: it lies in the XY plane,
        /// its back at z = 0 (offset by <paramref name="center"/>) and its face at z = <paramref name="depth"/>, facing +Z.
        /// The back is open (it sits against a wall) unless <paramref name="closeBack"/> is set, for slabs seen from behind (monitors).
        /// </summary>
        public void AddRoundedSlab(Vector3 center, float width, float height, float radius, float depth, float bevel, int steps = 5, bool closeBack = false)
        {
            var outer = RoundedLoop(width, height, radius, steps, 0f);
            var inner = RoundedLoop(width, height, radius, steps, bevel);
            float sideTop = depth - bevel;
            var faceCentre = center + new Vector3(0f, 0f, depth);
            for (int i = 0; i < outer.Count; i++)
            {
                int j = (i + 1) % outer.Count;
                var a = outer[i];
                var b = outer[j];
                var radial = new Vector3((a.x + b.x) / 2f, (a.y + b.y) / 2f, 0f).normalized;
                var p0 = center + new Vector3(a.x, a.y, 0f);
                var p1 = center + new Vector3(b.x, b.y, 0f);
                var p2 = center + new Vector3(b.x, b.y, sideTop);
                var p3 = center + new Vector3(a.x, a.y, sideTop);
                AddFlatQuad(p0, p1, p2, p3, radial);
                var q2 = center + new Vector3(inner[j].x, inner[j].y, depth);
                var q3 = center + new Vector3(inner[i].x, inner[i].y, depth);
                AddFlatQuad(p3, p2, q2, q3, radial + Vector3.forward);
                AddTriangle(faceCentre, Vector3.forward, q3, Vector3.forward, q2, Vector3.forward);
                if (closeBack) AddTriangle(center, Vector3.back, p0, Vector3.back, p1, Vector3.back);
            }
        }

        private static List<Vector2> RoundedLoop(float width, float height, float radius, int steps, float inset)
        {
            var points = new List<Vector2>();
            float cornerRadius = Mathf.Max(radius - inset, 0.0005f);
            float cx = width / 2f - radius;
            float cy = height / 2f - radius;
            var centres = new[] { new Vector2(cx, cy), new Vector2(-cx, cy), new Vector2(-cx, -cy), new Vector2(cx, -cy) };
            for (int corner = 0; corner < 4; corner++)
            {
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (corner * 90f + 90f * i / steps) * Mathf.Deg2Rad;
                    points.Add(centres[corner] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cornerRadius);
                }
            }
            return points;
        }

        /// <summary>
        /// A closed convex outline in the (u, v) plane, counter-clockwise, with every corner rounded by <paramref name="radius"/>
        /// (a radius of 0 keeps it sharp), for <see cref="AddExtrusion"/>. <paramref name="steps"/> is the number of facets per corner.
        /// </summary>
        public static List<Vector2> RoundedRectLoop(float u0, float u1, float v0, float v1, float radius, int steps = 2) =>
            RoundedRectLoop(u0, u1, v0, v1, new[] { radius, radius, radius, radius }, steps);

        /// <summary>As above with a radius for each corner: lower-left, lower-right, upper-right, upper-left.</summary>
        public static List<Vector2> RoundedRectLoop(float u0, float u1, float v0, float v1, float[] radii, int steps = 2)
        {
            var centres = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
            var signs = new[] { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) };
            var points = new List<Vector2>();
            for (int corner = 0; corner < 4; corner++)
            {
                float r = radii[corner];
                if (r <= 0f)
                {
                    points.Add(centres[corner]);
                    continue;
                }
                var centre = centres[corner] + new Vector2(signs[corner].x * r, signs[corner].y * r);
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (180f + corner * 90f + 90f * i / steps) * Mathf.Deg2Rad;
                    points.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
                }
            }
            return points;
        }

        /// <summary>
        /// A straight bar of constant convex cross-section <paramref name="loop"/> (counter-clockwise in the plane spanned by
        /// <paramref name="u"/> and <paramref name="v"/>, both unit vectors perpendicular to the run) from
        /// <paramref name="start"/> to <paramref name="end"/>. Each end is cut by the plane through its end point whose normal
        /// is <paramref name="startOutward"/> / <paramref name="endOutward"/> (pointing away from the bar): a square cut for
        /// -direction / +direction, a mitre for the bisector of a joint. Corners flatter than about 40 degrees are shaded smooth.
        /// Leave a cap out where two bars meet in a joint.
        /// </summary>
        public void AddExtrusion(IList<Vector2> loop, Vector3 start, Vector3 end, Vector3 u, Vector3 v,
            Vector3 startOutward, Vector3 endOutward, bool capStart = true, bool capEnd = true)
        {
            int count = loop.Count;
            var along = end - start;
            var direction = along.normalized;
            var a = new Vector3[count];
            var b = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var section = u * loop[i].x + v * loop[i].y;
                a[i] = OnPlane(start + section, direction, start, startOutward);
                b[i] = OnPlane(end + section, direction, end, endOutward);
            }

            var edgeNormal = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var edge = loop[(i + 1) % count] - loop[i];
                edgeNormal[i] = new Vector2(edge.y, -edge.x).normalized;
            }
            Vector3 Normal(Vector2 n) => (u * n.x + v * n.y).normalized;
            Vector3 VertexNormal(int vertex, int edge)
            {
                var previous = edgeNormal[(vertex + count - 1) % count];
                var next = edgeNormal[vertex];
                return Vector2.Dot(previous, next) > 0.77f ? Normal(previous + next) : Normal(edgeNormal[edge]);
            }
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                var ni = VertexNormal(i, i);
                var nj = VertexNormal(j, i);
                AddTriangle(a[i], ni, b[i], ni, b[j], nj);
                AddTriangle(a[i], ni, b[j], nj, a[j], nj);
            }
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 0 ? !capStart : !capEnd) continue;
                var ring = pass == 0 ? a : b;
                var outward = (pass == 0 ? startOutward : endOutward).normalized;
                for (int i = 1; i < count - 1; i++)
                {
                    AddTriangle(ring[0], outward, ring[i], outward, ring[i + 1], outward);
                }
            }
        }

        /// <summary>A flat round disc closing the end of a tube, facing <paramref name="outward"/>.</summary>
        public void AddDisc(Vector3 centre, Vector3 outward, float radius, int sides)
        {
            var normal = outward.normalized;
            var a = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var b = Vector3.Cross(normal, a);
            for (int k = 0; k < sides; k++)
            {
                float t0 = 2f * Mathf.PI * k / sides;
                float t1 = 2f * Mathf.PI * (k + 1) / sides;
                AddTriangle(centre, normal, centre + (a * Mathf.Cos(t0) + b * Mathf.Sin(t0)) * radius, normal,
                    centre + (a * Mathf.Cos(t1) + b * Mathf.Sin(t1)) * radius, normal);
            }
        }

        /// <summary>
        /// A flat disc whose UVs map the whole disc onto the texture square (centre at 0.5, 0.5), for dial faces: <paramref name="right"/>
        /// and <paramref name="up"/> are the directions of the texture's u and v, the disc faces <paramref name="outward"/>.
        /// </summary>
        public void AddDiscUv(Vector3 centre, Vector3 right, Vector3 up, Vector3 outward, float radius, int sides)
        {
            var normal = outward.normalized;
            Vector2 Uv(float angle) => new Vector2(0.5f + 0.5f * Mathf.Cos(angle), 0.5f + 0.5f * Mathf.Sin(angle));
            for (int k = 0; k < sides; k++)
            {
                float t0 = 2f * Mathf.PI * k / sides;
                float t1 = 2f * Mathf.PI * (k + 1) / sides;
                AddTriangle(centre, normal, centre + (right * Mathf.Cos(t0) + up * Mathf.Sin(t0)) * radius, normal,
                    centre + (right * Mathf.Cos(t1) + up * Mathf.Sin(t1)) * radius, normal, new Vector2(0.5f, 0.5f), Uv(t0), Uv(t1));
            }
        }

        /// <summary>Copies everything <paramref name="other"/> has built into this mesh, moved and turned (a part built in its own frame).</summary>
        public void Append(MeshBuilder other, Vector3 offset, Quaternion rotation)
        {
            int first = vertices.Count;
            foreach (var vertex in other.vertices) vertices.Add(offset + rotation * vertex);
            foreach (var normal in other.normals) normals.Add(rotation * normal);
            uvs.AddRange(other.uvs);
            foreach (int index in other.triangles) triangles.Add(first + index);
        }

        private static Vector3 OnPlane(Vector3 point, Vector3 direction, Vector3 planePoint, Vector3 normal)
        {
            float slope = Vector3.Dot(normal, direction);
            if (Mathf.Abs(slope) < 1e-4f) return point;
            return point + direction * (Vector3.Dot(normal, planePoint - point) / slope);
        }

        /// <summary>A single flat triangle facing <paramref name="normal"/>, for pictograms.</summary>
        public void AddFlatTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal) =>
            AddTriangle(a, normal.normalized, b, normal.normalized, c, normal.normalized);

        /// <summary>
        /// A round tube (hose) of <paramref name="radius"/> bent along <paramref name="path"/>, smooth shaded. The cross-section
        /// is carried along the path without twisting, so the tube keeps its full width round every bend. Open ends, or with
        /// <paramref name="closed"/> a ring: the path then lists every point once and the last joins the first.
        /// </summary>
        public void AddTube(IList<Vector3> path, float radius, int sides, Vector3 offset, Quaternion rotation, bool closed = false)
        {
            int count = path.Count;
            var ring = new Vector3[count][];
            var direction = new Vector3[count][];
            var n1 = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Vector3 next = closed ? path[(i + 1) % count] : (i < count - 1 ? path[i + 1] : path[i]);
                Vector3 previous = closed ? path[(i + count - 1) % count] : (i > 0 ? path[i - 1] : path[i]);
                var tangent = (next - previous).normalized;
                if (i == 0)
                {
                    var reference = Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right;
                    n1 = Vector3.Cross(reference, tangent).normalized;
                }
                else
                {
                    n1 = (n1 - tangent * Vector3.Dot(n1, tangent)).normalized;
                }
                var n2 = Vector3.Cross(tangent, n1);
                ring[i] = new Vector3[sides];
                direction[i] = new Vector3[sides];
                for (int k = 0; k < sides; k++)
                {
                    float angle = 2f * Mathf.PI * k / sides;
                    var d = n1 * Mathf.Cos(angle) + n2 * Mathf.Sin(angle);
                    direction[i][k] = rotation * d;
                    ring[i][k] = offset + rotation * (path[i] + d * radius);
                }
            }
            int segments = closed ? count : count - 1;
            for (int i = 0; i < segments; i++)
            {
                int j = (i + 1) % count;
                for (int k = 0; k < sides; k++)
                {
                    int n = (k + 1) % sides;
                    AddTriangle(ring[i][k], direction[i][k], ring[j][k], direction[j][k], ring[j][n], direction[j][n]);
                    AddTriangle(ring[i][k], direction[i][k], ring[j][n], direction[j][n], ring[i][n], direction[i][n]);
                }
            }
        }

        /// <summary>A flat four-cornered panel facing <paramref name="outward"/> with its own UVs (a, b, c, d in order round the edge).</summary>
        public void AddPanel(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            var normal = outward.normalized;
            AddTriangle(a, normal, b, normal, c, normal, uvA, uvB, uvC);
            AddTriangle(a, normal, c, normal, d, normal, uvA, uvC, uvD);
        }

        /// <summary>
        /// A slab with rounded corners and a bevelled top edge standing on the XZ plane (for keys and cases): its base at
        /// <paramref name="baseCenter"/> (the middle of the bottom face), <paramref name="height"/> up (+Y).
        /// </summary>
        public void AddRoundedBlock(Vector3 baseCenter, float sizeX, float sizeZ, float radius, float height, float bevel, int steps = 3)
        {
            var outer = RoundedLoop(sizeX, sizeZ, radius, steps, 0f);
            var inner = RoundedLoop(sizeX, sizeZ, radius, steps, bevel);
            float sideTop = height - bevel;
            var topCentre = baseCenter + new Vector3(0f, height, 0f);
            for (int i = 0; i < outer.Count; i++)
            {
                int j = (i + 1) % outer.Count;
                var a = outer[i];
                var b = outer[j];
                var radial = new Vector3((a.x + b.x) / 2f, 0f, (a.y + b.y) / 2f).normalized;
                var p0 = baseCenter + new Vector3(a.x, 0f, a.y);
                var p1 = baseCenter + new Vector3(b.x, 0f, b.y);
                var p2 = baseCenter + new Vector3(b.x, sideTop, b.y);
                var p3 = baseCenter + new Vector3(a.x, sideTop, a.y);
                AddFlatQuad(p0, p1, p2, p3, radial);
                var q2 = baseCenter + new Vector3(inner[j].x, height, inner[j].y);
                var q3 = baseCenter + new Vector3(inner[i].x, height, inner[i].y);
                AddFlatQuad(p3, p2, q2, q3, radial + Vector3.up);
                AddTriangle(topCentre, Vector3.up, q3, Vector3.up, q2, Vector3.up);
            }
        }

        /// <summary>
        /// A smooth skin over a row of convex cross-section rings (a mouse body): each ring is an array of points, all rings
        /// have the same count, neighbours are joined with normals pointing away from each ring's centre; the ends are closed.
        /// </summary>
        public void AddLoft(IList<Vector3[]> rings, Vector3 offset)
        {
            Vector3 Centre(Vector3[] ring)
            {
                var sum = Vector3.zero;
                foreach (var point in ring) sum += point;
                return sum / ring.Length;
            }

            for (int i = 0; i < rings.Count - 1; i++)
            {
                var ca = Centre(rings[i]);
                var cb = Centre(rings[i + 1]);
                int n = rings[i].Length;
                for (int k = 0; k < n; k++)
                {
                    int m = (k + 1) % n;
                    var a = offset + rings[i][k];
                    var b = offset + rings[i][m];
                    var c = offset + rings[i + 1][m];
                    var d = offset + rings[i + 1][k];
                    var na = (rings[i][k] - ca).normalized;
                    var nb = (rings[i][m] - ca).normalized;
                    var nc = (rings[i + 1][m] - cb).normalized;
                    var nd = (rings[i + 1][k] - cb).normalized;
                    AddTriangle(a, na, d, nd, c, nc);
                    AddTriangle(a, na, c, nc, b, nb);
                }
            }
            CloseRing(rings[0], offset, (Centre(rings[0]) - Centre(rings[1])).normalized);
            CloseRing(rings[rings.Count - 1], offset, (Centre(rings[rings.Count - 1]) - Centre(rings[rings.Count - 2])).normalized);
        }

        private void CloseRing(Vector3[] ring, Vector3 offset, Vector3 normal)
        {
            var centre = Vector3.zero;
            foreach (var point in ring) centre += point;
            centre = offset + centre / ring.Length;
            for (int k = 0; k < ring.Length; k++)
            {
                AddTriangle(centre, normal, offset + ring[k], normal, offset + ring[(k + 1) % ring.Length], normal);
            }
        }

        /// <summary>
        /// Joins a square loop of half-size <paramref name="halfOuter"/> at height <paramref name="yOuter"/> to one of
        /// <paramref name="halfInner"/> at <paramref name="yInner"/> with four sloped faces (a flat ring when the heights are
        /// equal, a pyramid when the inner half-size is 0). The faces look down; with <paramref name="solidInside"/> the
        /// solid is inside the loop (an inverted frustum), otherwise outside it (a bevelled opening).
        /// </summary>
        public void AddSquareBand(float yOuter, float halfOuter, float yInner, float halfInner, Vector3 offset, bool solidInside = true)
        {
            float across = halfOuter - halfInner;
            float drop = yOuter - yInner;
            foreach (var direction in new[] { new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, -1f) })
            {
                var perpendicular = new Vector3(-direction.z, 0f, direction.x);
                var outerA = offset + direction * halfOuter - perpendicular * halfOuter + Vector3.up * yOuter;
                var outerB = offset + direction * halfOuter + perpendicular * halfOuter + Vector3.up * yOuter;
                var innerB = offset + direction * halfInner + perpendicular * halfInner + Vector3.up * yInner;
                var innerA = offset + direction * halfInner - perpendicular * halfInner + Vector3.up * yInner;
                var hint = (solidInside ? direction : -direction) * drop + Vector3.down * Mathf.Max(across, 0.0001f);
                AddFlatQuad(outerA, outerB, innerB, innerA, hint);
            }
        }

        private void AddFlatQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            var normal = outward.normalized;
            AddTriangle(a, normal, b, normal, c, normal);
            AddTriangle(a, normal, c, normal, d, normal);
        }

        /// <summary>
        /// A smooth height-field surface facing +Z over the rectangle spanned by <paramref name="xs"/> and <paramref name="ys"/> (ascending sample positions):
        /// the vertex at (x, y) is at z = <paramref name="height"/>(x, y), with smooth normals from the slope. Used for corrugated panels.
        /// </summary>
        public void AddSurface(System.Func<float, float, float> height, IList<float> xs, IList<float> ys)
        {
            const float h = 0.0004f;
            Vector3 Point(int i, int j) => new Vector3(xs[i], ys[j], height(xs[i], ys[j]));
            Vector3 Normal(int i, int j)
            {
                float x = xs[i];
                float y = ys[j];
                float dx = (height(x + h, y) - height(x - h, y)) / (2f * h);
                float dy = (height(x, y + h) - height(x, y - h)) / (2f * h);
                return new Vector3(-dx, -dy, 1f).normalized;
            }
            for (int j = 0; j < ys.Count - 1; j++)
            {
                for (int i = 0; i < xs.Count - 1; i++)
                {
                    var a = Point(i, j);
                    var b = Point(i + 1, j);
                    var c = Point(i + 1, j + 1);
                    var d = Point(i, j + 1);
                    var na = Normal(i, j);
                    var nb = Normal(i + 1, j);
                    var nc = Normal(i + 1, j + 1);
                    var nd = Normal(i, j + 1);
                    AddTriangle(a, na, b, nb, c, nc);
                    AddTriangle(a, na, c, nc, d, nd);
                }
            }
        }

        private void AddTriangle(Vector3 a, Vector3 na, Vector3 b, Vector3 nb, Vector3 c, Vector3 nc,
            Vector2? uvA = null, Vector2? uvB = null, Vector2? uvC = null)
        {
            var geometric = Vector3.Cross(b - a, c - a);
            if (geometric.sqrMagnitude < 1e-14f) return;
            // Unity draws the side from which the corners run clockwise, whose normal is cross(b - a, c - a).
            if (Vector3.Dot(geometric, na + nb + nc) < 0f)
            {
                (b, c) = (c, b);
                (nb, nc) = (nc, nb);
                (uvB, uvC) = (uvC, uvB);
            }
            int first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(na);
            normals.Add(nb);
            normals.Add(nc);
            uvs.Add(uvA ?? new Vector2(a.x, a.z) * 10f);
            uvs.Add(uvB ?? new Vector2(b.x, b.z) * 10f);
            uvs.Add(uvC ?? new Vector2(c.x, c.z) * 10f);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
        }

        /// <summary>Mesh assets written since <see cref="TrackWrites"/> started; null outside it.</summary>
        private static HashSet<string> written;

        /// <summary>
        /// Runs <paramref name="build"/> and then deletes every mesh asset in <see cref="MeshFolder"/> it did not write (parts that were
        /// renamed or removed), so the folder holds exactly what the prefabs use. Nothing is deleted if the build throws.
        /// </summary>
        internal static void TrackWrites(System.Action build)
        {
            written = new HashSet<string>();
            try
            {
                build();
                foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { MeshFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (written.Contains(path)) continue;
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log($"Deleted {path}: no part is built from it any more.");
                }
            }
            finally
            {
                written = null;
            }
        }

        private static Mesh SaveMesh(Mesh mesh, string assetName)
        {
            Directory.CreateDirectory(MeshFolder);
            string path = $"{MeshFolder}/{assetName}.asset";
            written?.Add(path);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Turns the geometry into a mesh in memory (32-bit indices, so a rack full of servers fits one mesh).</summary>
        public Mesh BuildMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Puts the mesh on a new child of <paramref name="parent"/> with the shared material of <paramref name="surface"/>.</summary>
        public Transform Attach(string name, Transform parent, SurfaceMaterial surface)
        {
            var mesh = SaveMesh(BuildMesh(name), $"{parent.root.name}_{name}");

            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterial = Surfaces.Get(surface);
            return part.transform;
        }
    }
}
