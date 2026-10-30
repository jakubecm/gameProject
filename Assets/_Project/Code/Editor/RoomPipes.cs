using System.Collections.Generic;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Service pipes in the look of painted installation pipework (product photos): smooth round tubes that turn in tight
    /// elbows with a collar at both ends of the bend, flat strap clamps with two screws, and compression ball valves with a
    /// hex nut at each end, a stem nut and a steel lever with a coloured grip. Water is green, gas yellow, fire water red:
    /// the grip takes the colour of the pipe it sits on. Sizes follow the pipe radius, so one valve model fits every pipe.
    /// </summary>
    internal static class RoomPipes
    {
        private const int Sides = 20;

        /// <summary>The three pipe sizes (outer radius, m): the fire water main, the narrow service pipe of corridors, and the wide supply pipe of the technical rooms.</summary>
        public const float FireRadius = 0.0305f;
        public const float NarrowRadius = 0.0135f;
        public const float WideRadius = 0.0275f;

        /// <summary>The wide supply pipe is thermally insulated and clad in aluminium (as in the plant room photos), the others are bare painted steel.</summary>
        public static bool IsInsulated(float radius) => Mathf.Approximately(radius, WideRadius);

        /// <summary>
        /// A pipe along <paramref name="path"/> (points of its axis). Every inner corner becomes a bend of
        /// <paramref name="bendRadius"/> (2.2 pipe radii by default, less if the legs are short); the two ends are closed flat.
        /// A bare pipe gets a collar at each end of a bend. The wide pipe is aluminium-clad: seam rings at the bends and bands in
        /// the colour of the medium (<paramref name="surface"/>) near both ends of every straight length and at least every 1.2 m.
        /// </summary>
        public static void Pipe(Transform parent, string name, IList<Vector3> path, float radius, SurfaceMaterial surface, float bendRadius = 0f)
        {
            bool clad = IsInsulated(radius);
            if (bendRadius <= 0f) bendRadius = radius * 2.2f;
            var points = new List<Vector3> { path[0] };
            var rings = new List<(Vector3 from, Vector3 to)>();
            var straights = new List<(Vector3 from, Vector3 to)>();
            var legStart = path[0];
            for (int i = 1; i < path.Count - 1; i++)
            {
                var d1 = (path[i] - path[i - 1]).normalized;
                var d2 = (path[i + 1] - path[i]).normalized;
                float turn = Vector3.Angle(d1, d2) * Mathf.Deg2Rad;
                if (turn < 0.02f)
                {
                    points.Add(path[i]);
                    continue;
                }
                float limit = Mathf.Min((path[i] - path[i - 1]).magnitude, (path[i + 1] - path[i]).magnitude) * 0.5f;
                float tangent = Mathf.Min(bendRadius * Mathf.Tan(turn / 2f), limit);
                float arc = tangent / Mathf.Tan(turn / 2f);
                var a = path[i] - d1 * tangent;
                var b = path[i] + d2 * tangent;
                var centre = a + (d2 - d1 * Vector3.Dot(d1, d2)).normalized * arc;
                for (int k = 0; k <= 8; k++)
                {
                    points.Add(centre + Vector3.Slerp(a - centre, b - centre, k / 8f));
                }
                straights.Add((legStart, a));
                legStart = b;
                if (clad)
                {
                    rings.Add((a - d1 * 0.006f, a + d1 * 0.006f));
                    rings.Add((b - d2 * 0.006f, b + d2 * 0.006f));
                }
                else
                {
                    rings.Add((a - d1 * radius * 0.9f, a + d1 * radius * 0.3f));
                    rings.Add((b - d2 * radius * 0.3f, b + d2 * radius * 0.9f));
                }
            }
            points.Add(path[path.Count - 1]);
            straights.Add((legStart, path[path.Count - 1]));

            var builder = new GrayboxMeshBuilder();
            builder.AddTube(points, radius, Sides, Vector3.zero, Quaternion.identity);
            builder.AddDisc(points[0], points[0] - points[1], radius, Sides);
            builder.AddDisc(points[points.Count - 1], points[points.Count - 1] - points[points.Count - 2], radius, Sides);
            float ringRadius = radius * (clad ? 1.035f : 1.2f);
            foreach (var (from, to) in rings)
            {
                builder.AddTube(new[] { from, to }, ringRadius, Sides, Vector3.zero, Quaternion.identity);
                builder.AddDisc(from, from - to, ringRadius, Sides);
                builder.AddDisc(to, to - from, ringRadius, Sides);
            }
            if (!clad)
            {
                builder.Attach(name, parent, surface);
                return;
            }

            builder.Attach(name, parent, SurfaceMaterial.AluminiumClad);
            var bands = new GrayboxMeshBuilder();
            foreach (var (from, to) in straights)
            {
                float length = Vector3.Distance(from, to);
                if (length < 0.3f) continue;
                var direction = (to - from).normalized;
                int count = Mathf.Max(2, Mathf.CeilToInt(length / 1.2f) + 1);
                for (int k = 0; k < count; k++)
                {
                    float at = Mathf.Lerp(0.14f, length - 0.14f, k / (float)(count - 1));
                    var start = from + direction * (at - 0.025f);
                    var end = from + direction * (at + 0.025f);
                    bands.AddTube(new[] { start, end }, radius * 1.055f, Sides, Vector3.zero, Quaternion.identity);
                    bands.AddDisc(start, -direction, radius * 1.055f, Sides);
                    bands.AddDisc(end, direction, radius * 1.055f, Sides);
                }
            }
            bands.Attach($"{name}_Bands", parent, surface);
        }

        public static void Pipe(Transform parent, string name, Vector3 from, Vector3 to, float radius, SurfaceMaterial surface) =>
            Pipe(parent, name, new[] { from, to }, radius, surface);

        /// <summary>
        /// A strap clamp on a pipe: a band round it, a neck and a flat plate against the wall with two screw heads, the plate
        /// across the pipe. <paramref name="axisPoint"/> is the pipe axis, <paramref name="normal"/> points off the wall,
        /// <paramref name="gap"/> is the distance from the pipe surface to the wall. The band goes into <paramref name="strap"/>
        /// (galvanised steel on every pipe), the screws into <paramref name="screws"/>.
        /// </summary>
        public static void Clamp(GrayboxMeshBuilder strap, GrayboxMeshBuilder screws, Vector3 axisPoint, Vector3 axis, Vector3 normal, float radius, float gap)
        {
            float outer = radius + 0.003f;
            float wall = -(radius + gap);
            var band = new GrayboxMeshBuilder();
            band.AddLathe(new[] { P(radius + 0.0003f, -0.011f, true), P(outer, -0.011f, true), P(outer, 0.011f, true), P(radius + 0.0003f, 0.011f, true) },
                24, true, Vector3.zero, Quaternion.identity);
            band.AddBox(new Vector3(-0.03f, -0.011f, wall), new Vector3(0.03f, 0.011f, wall + 0.004f));
            band.AddBox(new Vector3(-0.011f, -0.011f, wall), new Vector3(0.011f, 0.011f, -radius * 0.7f));
            var heads = new GrayboxMeshBuilder();
            foreach (int side in new[] { -1, 1 })
            {
                heads.AddLathe(new[] { P(0.0055f, 0f, true), P(0.0055f, 0.0015f), P(0.004f, 0.0032f, true), P(0f, 0.0035f, true) },
                    14, true, new Vector3(side * 0.021f, 0f, wall + 0.004f), Quaternion.Euler(90f, 0f, 0f));
            }
            var turn = Quaternion.LookRotation(normal, axis);
            strap.Append(band, axisPoint, turn);
            screws.Append(heads, axisPoint, turn);
        }

        /// <summary>
        /// A compact compression ball valve (product photo) on a pipe of <paramref name="radius"/>, centred on <paramref name="position"/>
        /// of its axis, no wider than a little over the pipe itself: a smooth satin nickel barrel body, a round compression nut at
        /// each end that the pipe runs into, a slim neck with the packing nut, and a zinc-plated lever bar under a rounded,
        /// bevelled plastic grip (dark graphite on every pipe, unlike the green, yellow and red pipes) lying along the pipe (open). <paramref name="stem"/> is the direction the stem points (away from
        /// the wall, at right angles to <paramref name="axis"/>). Everything scales with the pipe radius.
        /// </summary>
        public static void BallValve(Transform parent, string name, Vector3 position, Vector3 axis, Vector3 stem, float radius)
        {
            float r = radius;
            const int round = 32;
            var nickel = new GrayboxMeshBuilder();
            var steel = new GrayboxMeshBuilder();
            var plastic = new GrayboxMeshBuilder();
            var toZ = Quaternion.Euler(90f, 0f, 0f);

            nickel.AddLathe(new[]
            {
                P(1.0f * r, -1.4f * r, true), P(1.1f * r, -1.25f * r), P(1.17f * r, -0.9f * r), P(1.2f * r, -0.4f * r), P(1.2f * r, 0.4f * r),
                P(1.17f * r, 0.9f * r), P(1.1f * r, 1.25f * r), P(1.0f * r, 1.4f * r, true),
            }, round, true, Vector3.zero, Quaternion.identity);
            var nut = new[]
            {
                P(1.0f * r, 1.3f * r, true), P(1.22f * r, 1.34f * r), P(1.3f * r, 1.5f * r), P(1.3f * r, 2.3f * r), P(1.22f * r, 2.5f * r), P(1.05f * r, 2.6f * r), P(1.0f * r, 2.6f * r, true),
            };
            nickel.AddLathe(nut, round, true, Vector3.zero, Quaternion.identity);
            nickel.AddLathe(Mirrored(nut), round, true, Vector3.zero, Quaternion.identity);

            // Neck and packing nut, pointing along local +Z.
            nickel.AddLathe(new[] { P(0.9f * r, 0.9f * r, true), P(0.62f * r, 1.3f * r), P(0.58f * r, 1.6f * r), P(0.58f * r, 1.7f * r, true) },
                round, true, Vector3.zero, toZ);
            nickel.AddLathe(new[]
            {
                P(0.42f * r, 1.78f * r, true), P(0.66f * r, 1.78f * r), P(0.7f * r, 1.9f * r), P(0.7f * r, 2.05f * r), P(0.62f * r, 2.18f * r), P(0.35f * r, 2.2f * r, true),
                P(0.0001f, 2.2f * r, true),
            }, round, true, Vector3.zero, toZ);

            // Lever: a round plate under the packing nut and a flat bar running to the grip.
            steel.AddExtrusion(RoundedRectLoop(-0.85f * r, 0.85f * r, -0.85f * r, 0.85f * r, 0.5f * r, 4),
                new Vector3(0f, 0f, 1.6f * r), new Vector3(0f, 0f, 1.78f * r), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            steel.AddExtrusion(RoundedRectLoop(-0.38f * r, 0.38f * r, 0.2f * r, 3.0f * r, 0.19f * r, 3),
                new Vector3(0f, 0f, 1.6f * r), new Vector3(0f, 0f, 1.78f * r), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

            // Grip: a long slab with a fully rounded tip and softly rounded edges (two stacked layers).
            plastic.AddExtrusion(RoundedRectLoop(-0.62f * r, 0.62f * r, 1.5f * r, 6.2f * r, new[] { 0.3f * r, 0.3f * r, 0.62f * r, 0.62f * r }, 8),
                new Vector3(0f, 0f, 1.5f * r), new Vector3(0f, 0f, 1.86f * r), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            plastic.AddExtrusion(RoundedRectLoop(-0.5f * r, 0.5f * r, 1.62f * r, 6.08f * r, new[] { 0.2f * r, 0.2f * r, 0.5f * r, 0.5f * r }, 8),
                new Vector3(0f, 0f, 1.86f * r), new Vector3(0f, 0f, 1.98f * r), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);

            var turn = Quaternion.LookRotation(stem, axis);
            Place(nickel, position, turn, $"{name}_Body", parent, SurfaceMaterial.ValveNickel);
            Place(steel, position, turn, $"{name}_Bracket", parent, SurfaceMaterial.ValveSteel);
            Place(plastic, position, turn, $"{name}_Grip", parent, SurfaceMaterial.ValveGrip);
        }

        private static void Place(GrayboxMeshBuilder part, Vector3 position, Quaternion turn, string name, Transform parent, SurfaceMaterial surface)
        {
            var placed = new GrayboxMeshBuilder();
            placed.Append(part, position, turn);
            placed.Attach(name, parent, surface);
        }

        /// <summary>The profile of a part on the +Y side of a valve, reflected to the -Y side with its order reversed so it still faces outwards.</summary>
        private static ProfilePoint[] Mirrored(ProfilePoint[] profile)
        {
            var result = new ProfilePoint[profile.Length];
            for (int i = 0; i < profile.Length; i++)
            {
                var point = profile[profile.Length - 1 - i];
                result[i] = P(point.R, -point.Y, point.Hard);
            }
            return result;
        }
    }
}
