using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Builds the visible building services in a room: pipe risers, electrical trunking and cable trays.
    /// Every run starts and ends in the floor, the ceiling or a wall, so it always leads somewhere and a
    /// room stays a self-contained module. All runs are axis-aligned; positions are room-local.
    /// </summary>
    internal static class RoomInfrastructure
    {
        /// <summary>Gap between a pipe and the wall it is clamped to.</summary>
        private const float PipeWallGap = 0.04f;

        /// <summary>
        /// A vertical pipe from the floor to the ceiling (a riser), clamped to the wall every metre, with
        /// rosettes where it passes the floor and the suspended ceiling, and an optional valve.
        /// </summary>
        /// <param name="wallPoint">Point on the wall surface at floor level behind the pipe.</param>
        /// <param name="normal">Wall normal pointing into the room (a unit axis).</param>
        /// <param name="ceiling">Height the riser runs up to (the 3 m room ceiling unless given).</param>
        /// <returns>The pipe axis position at floor level, for branches.</returns>
        public static Vector3 Riser(Transform parent, string name, Vector3 wallPoint, Vector3 normal, float radius,
            SurfaceMaterial surface, float valveHeight = -1f,
            float ceiling = RoomContract.CeilingHeight)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            var axis = wallPoint + normal * (radius + PipeWallGap);

            RoomPipes.Pipe(group, $"{name}_Pipe", axis, axis + Vector3.up * ceiling, radius, surface);
            AddRosette(group, $"{name}_RosetteFloor", axis, radius, false);
            AddRosette(group, $"{name}_RosetteCeiling", axis + Vector3.up * (ceiling - 0.0112f), radius, true);

            var strap = new GrayboxMeshBuilder();
            var screws = new GrayboxMeshBuilder();
            for (float y = 0.5f; y < ceiling; y += 1f)
            {
                if (valveHeight > 0f && Mathf.Abs(y - valveHeight) < 4f * radius) continue;
                RoomPipes.Clamp(strap, screws, axis + Vector3.up * y, Vector3.up, normal, radius, PipeWallGap);
            }
            strap.Attach($"{name}_Clamps", group, RoomPipes.IsInsulated(radius) ? SurfaceMaterial.SupportBlue : SurfaceMaterial.PipeClamp);
            screws.Attach($"{name}_Screws", group, SurfaceMaterial.Chrome);

            if (valveHeight > 0f)
            {
                RoomPipes.BallValve(group, $"{name}_Valve", axis + Vector3.up * valveHeight, Vector3.up, normal, radius);
            }
            return axis;
        }

        /// <summary>A stepped chrome flange round a pipe where it passes the floor (<paramref name="hanging"/> false) or the ceiling.</summary>
        public static void AddRosette(Transform parent, string name, Vector3 axis, float radius, bool hanging)
        {
            float outer = radius + 0.025f;
            var rosette = new GrayboxMeshBuilder();
            rosette.AddLathe(hanging
                ? new[] { GrayboxMeshBuilder.P(radius, -0.012f, true), GrayboxMeshBuilder.P(outer - 0.004f, -0.012f, true), GrayboxMeshBuilder.P(outer, -0.008f), GrayboxMeshBuilder.P(outer, 0f, true) }
                : new[] { GrayboxMeshBuilder.P(outer, 0f, true), GrayboxMeshBuilder.P(outer, 0.008f), GrayboxMeshBuilder.P(outer - 0.004f, 0.012f, true), GrayboxMeshBuilder.P(radius, 0.012f, true) },
                32, true, axis, Quaternion.identity);
            rosette.Attach(name, parent, SurfaceMaterial.Chrome);
        }

        /// <summary>A straight run of electrical trunking between two points on its center line, cut square at both ends.</summary>
        public static void Trunking(Transform parent, string name, Vector3 from, Vector3 to, Vector3 normal) =>
            Trunking(parent, name, new[] { from, to }, normal);

        /// <summary>
        /// Electrical trunking along a wall surface following <paramref name="path"/> (the points of its center line,
        /// <paramref name="normal"/> pointing off the wall). Uses the one standard profile (<see cref="GrayboxPropsTool.TrunkingWidth"/>
        /// x <see cref="GrayboxPropsTool.TrunkingDepth"/>), so every cable run on a wall matches the distribution panels. It is
        /// white PVC made of a trough with a back plate, two walls with an inward lip, and a lid that sits on the lips; all the
        /// outer edges are slightly rounded. The two ends of the whole run are square; where the run turns, the two lengths
        /// are cut at 45 degrees (a mitre) and meet without a gap, like a fitted corner.
        /// </summary>
        public static void Trunking(Transform parent, string name, IList<Vector3> path, Vector3 normal)
        {
            const float wall = 0.004f;
            const float lipWidth = 0.006f;
            const float lipThickness = 0.004f;
            const float lidThickness = 0.004f;
            const float lidInset = 0.007f;

            float half = GrayboxPropsTool.TrunkingWidth / 2f;
            float depth = GrayboxPropsTool.TrunkingDepth;
            float wallTop = depth - lidThickness;
            var pieces = new List<List<Vector2>>
            {
                GrayboxMeshBuilder.RoundedRectLoop(-half + wall, half - wall, 0f, wall, 0f),
                GrayboxMeshBuilder.RoundedRectLoop(-half, -half + wall, 0f, wallTop, new[] { 0.003f, 0f, 0f, 0.003f }),
                GrayboxMeshBuilder.RoundedRectLoop(half - wall, half, 0f, wallTop, new[] { 0f, 0.003f, 0.003f, 0f }),
                GrayboxMeshBuilder.RoundedRectLoop(-half + wall, -half + wall + lipWidth, wallTop - lipThickness, wallTop, 0f),
                GrayboxMeshBuilder.RoundedRectLoop(half - wall - lipWidth, half - wall, wallTop - lipThickness, wallTop, 0f),
                GrayboxMeshBuilder.RoundedRectLoop(-half + lidInset, half - lidInset, wallTop, depth, 0.0015f),
            };

            normal = normal.normalized;
            var builder = new GrayboxMeshBuilder();
            int runs = path.Count - 1;
            for (int i = 0; i < runs; i++)
            {
                var direction = (path[i + 1] - path[i]).normalized;
                var startOutward = -direction;
                var endOutward = direction;
                if (i > 0) startOutward = -(direction + (path[i] - path[i - 1]).normalized).normalized;
                if (i < runs - 1) endOutward = (direction + (path[i + 2] - path[i + 1]).normalized).normalized;
                var across = Vector3.Cross(direction, normal).normalized;
                foreach (var piece in pieces)
                {
                    builder.AddExtrusion(piece, path[i], path[i + 1], across, normal, startOutward, endOutward, i == 0, i == runs - 1);
                }
            }
            builder.Attach(name, parent, SurfaceMaterial.Trunking);
        }

        /// <summary>
        /// A perforated cable tray hung from the ceiling on threaded rods and strut trapezes, with its cables in it
        /// (<see cref="InfrastructureModels.CableTray"/>). <paramref name="from"/> and <paramref name="to"/> are the ends of its
        /// center line (only X and Z are used); <paramref name="rodsAt"/> are the positions of the hangers along the run, whose rods reach
        /// up to <paramref name="ceiling"/>. The tray is always galvanised steel; <paramref name="tray"/> is kept for the callers' sake.
        /// </summary>
        public static void Tray(Transform parent, string name, Vector3 from, Vector3 to, float width, float bottom,
            SurfaceMaterial tray, SurfaceMaterial cables, IEnumerable<float> rodsAt, float ceiling = RoomContract.CeilingHeight) =>
            InfrastructureModels.CableTray(parent, name, from, to, width, bottom, cables, rodsAt, ceiling);

        /// <summary>
        /// A thin cable conduit from a device straight up to <paramref name="top"/> (a ceiling or a beam), built like the corridor risers
        /// (<see cref="Riser"/>): a smooth painted tube, a stepped chrome rosette where it enters the ceiling and flat strap clamps with two screws every
        /// metre on the wall. <paramref name="from"/> is the conduit axis at the device, <paramref name="normal"/> the wall normal into the room (a unit axis).
        /// </summary>
        public static void Conduit(Transform parent, string name, Vector3 from, float top, Vector3 normal, SurfaceMaterial surface = SurfaceMaterial.Trunking, bool rosette = true)
        {
            const float radius = 0.0125f;
            const float gap = 0.004f;
            var group = GrayboxUtility.CreateGroup(name, parent);
            RoomPipes.Pipe(group, $"{name}_Pipe", from, new Vector3(from.x, top, from.z), radius, surface);
            if (rosette) AddRosette(group, $"{name}_Rosette", new Vector3(from.x, top - 0.0112f, from.z), radius, true);
            var strap = new GrayboxMeshBuilder();
            var screws = new GrayboxMeshBuilder();
            for (float y = Mathf.Ceil(from.y + 0.4f); y < top - 0.15f; y += 1f)
            {
                RoomPipes.Clamp(strap, screws, new Vector3(from.x, y, from.z), Vector3.up, normal, radius, gap);
            }
            strap.Attach($"{name}_Clamps", group, SurfaceMaterial.PipeClamp);
            screws.Attach($"{name}_Screws", group, SurfaceMaterial.Chrome);
        }

        /// <summary>
        /// A flush-mounted 230 V socket and RJ45 outlet side by side in a wall: their plates sit directly on the wall surface
        /// and stand a few millimetres proud, with no box, conduit or cable outside the wall. <paramref name="wallPoint"/>
        /// is the point on the wall surface at floor level behind their middle, <paramref name="normal"/> the wall normal
        /// into the room (a unit axis).
        /// </summary>
        public static void WiredOutlet(Transform parent, string name, Vector3 wallPoint, Vector3 normal, float height = 0.3f,
            float ceiling = RoomContract.CeilingHeight)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            var along = Vector3.Cross(Vector3.up, normal);
            var center = wallPoint + Vector3.up * height;
            float yaw = Mathf.Atan2(normal.x, normal.z) * Mathf.Rad2Deg;
            GrayboxPropsTool.Place(GrayboxProp.PowerSocket, group, center - along * 0.05f, yaw);
            GrayboxPropsTool.Place(GrayboxProp.DataSocket, group, center + along * 0.05f, yaw);
        }

        /// <summary>Hanger positions every <paramref name="step"/> from <paramref name="first"/> up to <paramref name="last"/>.</summary>
        public static IEnumerable<float> Every(float first, float last, float step)
        {
            for (float at = first; at <= last + 0.001f; at += step)
            {
                yield return at;
            }
        }
    }
}
