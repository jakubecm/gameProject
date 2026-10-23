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

        private static float Ceiling => RoomContract.CeilingHeight;

        /// <summary>
        /// A vertical pipe from the floor to the ceiling (a riser), clamped to the wall every metre, with
        /// rosettes where it passes the floor and the suspended ceiling, and an optional valve.
        /// </summary>
        /// <param name="wallPoint">Point on the wall surface at floor level behind the pipe.</param>
        /// <param name="normal">Wall normal pointing into the room (a unit axis).</param>
        /// <returns>The pipe axis position at floor level, for branches.</returns>
        public static Vector3 Riser(Transform parent, string name, Vector3 wallPoint, Vector3 normal, float radius,
            SurfaceMaterial surface, float valveHeight = -1f, SurfaceMaterial handle = SurfaceMaterial.Metal)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            var axis = wallPoint + normal * (radius + PipeWallGap);
            var across = Vector3.Cross(Vector3.up, normal);

            GrayboxUtility.CreatePipe("Pipe", group, axis, axis + Vector3.up * Ceiling, radius, surface);
            GrayboxUtility.CreateCylinder("Rosette_Floor", group, axis + Vector3.up * 0.005f, radius + 0.02f, 0.01f, SurfaceMaterial.Metal);
            GrayboxUtility.CreateCylinder("Rosette_Ceiling", group, axis + Vector3.up * (Ceiling - 0.005f), radius + 0.02f, 0.01f, SurfaceMaterial.Metal);

            for (float y = 0.5f; y < Ceiling; y += 1f)
            {
                GrayboxUtility.CreateCylinder($"Clamp_{y:0.0}", group, axis + Vector3.up * y, radius + 0.006f, 0.03f, SurfaceMaterial.Metal);
                GrayboxUtility.CreateBoxBetween($"Bracket_{y:0.0}", group,
                    wallPoint + Vector3.up * (y - 0.015f) - across * 0.015f,
                    axis - normal * radius + Vector3.up * (y + 0.015f) + across * 0.015f, SurfaceMaterial.Metal);
            }

            if (valveHeight > 0f)
            {
                var valve = axis + Vector3.up * valveHeight;
                GrayboxUtility.CreateCylinder("Valve", group, valve, radius + 0.015f, 0.09f, SurfaceMaterial.Metal);
                GrayboxUtility.CreateBoxBetween("Valve_Handle", group,
                    valve + Vector3.up * 0.06f - across * 0.01f,
                    valve + Vector3.up * 0.075f + across * 0.01f + normal * (radius + 0.12f), handle);
            }
            return axis;
        }

        /// <summary>
        /// A run of electrical trunking along a wall surface, between two points on its center line.
        /// Uses the one standard profile, so every cable run on a wall matches the distribution panels.
        /// </summary>
        public static void Trunking(Transform parent, string name, Vector3 from, Vector3 to, Vector3 normal)
        {
            var direction = (to - from).normalized;
            var across = Vector3.Cross(direction, normal);
            across = new Vector3(Mathf.Abs(across.x), Mathf.Abs(across.y), Mathf.Abs(across.z));
            float half = GrayboxPropsTool.TrunkingWidth / 2f;
            GrayboxUtility.CreateBoxBetween(name, parent, from - across * half,
                to + across * half + normal * GrayboxPropsTool.TrunkingDepth, SurfaceMaterial.Trunking);
        }

        /// <summary>
        /// A cable tray hung from the ceiling on threaded rods and trapeze bars, with its cables in it.
        /// <paramref name="from"/> and <paramref name="to"/> are the ends of its center line (only X and Z
        /// are used); <paramref name="rodsAt"/> are the positions of the hangers along the run.
        /// </summary>
        public static void Tray(Transform parent, string name, Vector3 from, Vector3 to, float width, float bottom,
            SurfaceMaterial tray, SurfaceMaterial cables, IEnumerable<float> rodsAt)
        {
            const float plate = 0.01f;
            const float rail = 0.01f;
            const float railHeight = 0.08f;

            var group = GrayboxUtility.CreateGroup(name, parent);
            bool alongZ = Mathf.Abs(to.z - from.z) > Mathf.Abs(to.x - from.x);
            var across = alongZ ? Vector3.right : Vector3.forward;
            from.y = to.y = bottom;
            float half = width / 2f;

            GrayboxUtility.CreateBoxBetween("Bottom", group, from - across * half, to + across * half + Vector3.up * plate, tray);
            GrayboxUtility.CreateBoxBetween("Rail_A", group, from - across * half, to - across * (half - rail) + Vector3.up * railHeight, tray);
            GrayboxUtility.CreateBoxBetween("Rail_B", group, from + across * (half - rail), to + across * half + Vector3.up * railHeight, tray);
            GrayboxUtility.CreateBoxBetween("Cables", group, from - across * (half - 0.025f) + Vector3.up * plate,
                to + across * (half - 0.025f) + Vector3.up * (plate + 0.045f), cables);

            foreach (float at in rodsAt)
            {
                var center = alongZ ? new Vector3(from.x, bottom, at) : new Vector3(at, bottom, from.z);
                var along = alongZ ? Vector3.forward : Vector3.right;
                GrayboxUtility.CreateBoxBetween($"Trapeze_{at:0.0}", group, center - across * (half + 0.05f) - along * 0.02f - Vector3.up * 0.02f,
                    center + across * (half + 0.05f) + along * 0.02f, SurfaceMaterial.Metal);
                foreach (int side in new[] { -1, 1 })
                {
                    var rod = center + across * side * (half + 0.03f);
                    GrayboxUtility.CreateBoxBetween($"Rod_{at:0.0}_{(side < 0 ? "A" : "B")}", group, rod - across * 0.006f - along * 0.006f - Vector3.up * 0.02f,
                        new Vector3(rod.x, Ceiling, rod.z) + across * 0.006f + along * 0.006f, SurfaceMaterial.Metal);
                }
            }
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
