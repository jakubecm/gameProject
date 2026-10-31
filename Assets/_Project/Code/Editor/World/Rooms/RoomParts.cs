using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>Helpers the room templates place things with.</summary>
    public static partial class Rooms
    {
        // ---------------------------------------------------------------- placement

        /// <summary>Places a prop at a point of the room (room-local).</summary>
        private static Transform Put(Prop prop, Transform parent, float x, float y, float z, float yaw = 0f, string name = null) =>
            Props.Place(prop, parent, new Vector3(x, y, z), yaw, name);

        private static Transform Put(Prop prop, Transform parent, Vector3 position, float yaw = 0f, string name = null) =>
            Put(prop, parent, position.x, position.y, position.z, yaw, name);

        /// <summary>Places a ceiling prop at each (x, z); positions are grid-tile centers (multiples of 0.6 m).</summary>
        private static void PutOnCeiling(Prop prop, Transform parent, params (float x, float z)[] positions)
        {
            foreach (var (x, z) in positions)
            {
                var at = (new Vector3(x, Ceiling, z));
                Props.Place(prop, parent, at, 0f, $"{prop}_{at.x:0.0}_{at.z:0.0}");
            }
        }

        private static void Trunking(Transform parent, string name, Vector3 from, Vector3 to, Vector3 normal) =>
            RoomInfrastructure.Trunking(parent, name, from, to, normal);

        private static void Trunking(Transform parent, string name, Vector3[] path, Vector3 normal) =>
            RoomInfrastructure.Trunking(parent, name, path, normal);

        private static void PipePath(string name, Transform parent, Vector3[] path, float radius, SurfaceMaterial surface) =>
            RoomPipes.Pipe(parent, name, path, radius, surface);

        /// <summary>
        /// Painted floor lines (each between two points that differ along one axis) merged into one mesh without overlaps: every line is 5 cm wide and
        /// runs half a width past its ends so corners and junctions are filled, clipped to the room (the walls); the union is cut into boxes on the grid of all
        /// the line edges, so no two pieces share floor area.
        /// </summary>
        private static void PaintedLines(Transform parent, string name, IList<(Vector3 from, Vector3 to)> lines, float minX, float maxX, float minZ, float maxZ)
        {
            const float width = 0.05f;
            const float thickness = 0.002f;
            var rects = new List<Vector4>();
            foreach (var (from, to) in lines)
            {
                bool alongX = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.z - from.z);
                float x0 = Mathf.Min(from.x, to.x);
                float x1 = Mathf.Max(from.x, to.x);
                float z0 = Mathf.Min(from.z, to.z);
                float z1 = Mathf.Max(from.z, to.z);
                if (alongX) rects.Add(new Vector4(Mathf.Max(minX, x0 - width / 2f), z0 - width / 2f, Mathf.Min(maxX, x1 + width / 2f), z0 + width / 2f));
                else rects.Add(new Vector4(x0 - width / 2f, Mathf.Max(minZ, z0 - width / 2f), x0 + width / 2f, Mathf.Min(maxZ, z1 + width / 2f)));
            }
            var xs = new SortedSet<float>();
            var zs = new SortedSet<float>();
            foreach (var r in rects)
            {
                xs.Add(r.x);
                xs.Add(r.z);
                zs.Add(r.y);
                zs.Add(r.w);
            }
            var xl = xs.ToList();
            var zl = zs.ToList();
            var paint = new MeshBuilder();
            for (int j = 0; j < zl.Count - 1; j++)
            {
                float runStart = float.NaN;
                for (int i = 0; i <= xl.Count - 1; i++)
                {
                    bool covered = false;
                    if (i < xl.Count - 1)
                    {
                        float cx = (xl[i] + xl[i + 1]) / 2f;
                        float cz = (zl[j] + zl[j + 1]) / 2f;
                        foreach (var r in rects) covered |= cx > r.x && cx < r.z && cz > r.y && cz < r.w;
                    }
                    if (covered && float.IsNaN(runStart)) runStart = xl[i];
                    if (!covered && !float.IsNaN(runStart))
                    {
                        paint.AddBox(new Vector3(runStart, 0f, zl[j]), new Vector3(xl[i], thickness, zl[j + 1]));
                        runStart = float.NaN;
                    }
                }
            }
            var group = ModelUtility.CreateGroup(name, parent);
            paint.Attach("Paint", group, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>A painted floor line between two points of the room that differ along one axis.</summary>
        private static void Marking(Transform parent, string name, Vector3 from, Vector3 to)
        {
            const float width = 0.05f;
            var a = from;
            var b = to;
            var across = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z) ? Vector3.forward : Vector3.right;
            ModelUtility.CreateBoxBetween(name, parent, a - across * (width / 2f), b + across * (width / 2f) + Vector3.up * 0.002f, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>
        /// Yellow keep-clear hatching over the floor rectangle (x0, z0)-(x1, z1) of the room: an outline and diagonal
        /// stripes, the space that must never be blocked (in front of the storage room's gate).
        /// </summary>
        private static void Hatching(Transform parent, string name, float x0, float z0, float x1, float z1)
        {
            // One mesh of exact pieces, so nothing overlaps and nothing is left open: four border bars that tile the outline without
            // overlapping at the corners, and diagonal stripes clipped to the inside of the border, so every stripe ends flush on it.
            const float bar = 0.05f;
            const float thickness = 0.002f;
            const float period = 0.5f;
            const float stripe = 0.25f;
            var group = ModelUtility.CreateGroup(name, parent);
            var paint = new MeshBuilder();
            float minX = Mathf.Min(x0, x1);
            float maxX = Mathf.Max(x0, x1);
            float minZ = Mathf.Min(z0, z1);
            float maxZ = Mathf.Max(z0, z1);
            paint.AddBox(new Vector3(minX, 0f, minZ), new Vector3(maxX, thickness, minZ + bar));
            paint.AddBox(new Vector3(minX, 0f, maxZ - bar), new Vector3(maxX, thickness, maxZ));
            paint.AddBox(new Vector3(minX, 0f, minZ + bar), new Vector3(minX + bar, thickness, maxZ - bar));
            paint.AddBox(new Vector3(maxX - bar, 0f, minZ + bar), new Vector3(maxX, thickness, maxZ - bar));

            float innerMinX = minX + bar;
            float innerMaxX = maxX - bar;
            float innerMinZ = minZ + bar;
            float innerMaxZ = maxZ - bar;
            // A stripe is the band c0 <= (z - x) <= c0 + stripe, clipped to the inner rectangle (Sutherland-Hodgman, four edges).
            for (float c0 = innerMinZ - innerMaxX; c0 < innerMaxZ - innerMinX; c0 += period)
            {
                var polygon = new List<Vector2>
                {
                    new Vector2(innerMinX - 10f, innerMinX - 10f + c0),
                    new Vector2(innerMaxX + 10f, innerMaxX + 10f + c0),
                    new Vector2(innerMaxX + 10f, innerMaxX + 10f + c0 + stripe),
                    new Vector2(innerMinX - 10f, innerMinX - 10f + c0 + stripe),
                };
                polygon = Clip(polygon, p => p.x - innerMinX);
                polygon = Clip(polygon, p => innerMaxX - p.x);
                polygon = Clip(polygon, p => p.y - innerMinZ);
                polygon = Clip(polygon, p => innerMaxZ - p.y);
                if (polygon.Count < 3 || PolygonArea(polygon) < 0.0004f) continue;
                paint.AddPrism(polygon, 0f, thickness, Vector3.zero);
            }
            paint.Attach("Paint", group, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>Keeps the part of a convex polygon where <paramref name="inside"/> is not negative.</summary>
        private static List<Vector2> Clip(List<Vector2> polygon, System.Func<Vector2, float> inside)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                float da = inside(a);
                float db = inside(b);
                if (da >= 0f) result.Add(a);
                if ((da >= 0f) != (db >= 0f)) result.Add(Vector2.Lerp(a, b, da / (da - db)));
            }
            return result;
        }

        private static float PolygonArea(List<Vector2> polygon)
        {
            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }
            return Mathf.Abs(area) / 2f;
        }

        /// <summary>
        /// A desk with its PC (tower under the desk, two monitors with keyboard and mouse) and a chair, and with
        /// <paramref name="floorBox"/> a small floor box under its back, between its side panels (desks away from the
        /// walls). <paramref name="yaw"/> points from the desk to the chair: 0 puts the person on the +Z side, 90 on +X.
        /// </summary>
        private static void PlaceDesk(Transform furniture, Transform details, Transform power, Vector3 position, float yaw, string suffix, bool floorBox)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(Prop.Desk, furniture, position, yaw, $"Desk_{suffix}");
            var screen = Prop.Workstation;
            Put(screen, details, position + turn * new Vector3(0f, Props.DeskHeight, -0.05f), yaw, $"{screen}_{suffix}");
            Put(Prop.PcTower, furniture, position + turn * new Vector3(0.55f, 0f, 0f), yaw, $"PcTower_{suffix}");
            Put(Prop.OfficeChair, furniture, position + turn * new Vector3(0f, 0f, 0.78f), yaw + 180f, $"OfficeChair_{suffix}");
            if (floorBox)
            {
                Put(Prop.FloorBox, power, position + turn * new Vector3(-0.45f, 0f, -0.2f), yaw, $"FloorBox_{suffix}");
            }
        }

        /// <summary>
        /// Power and data outlets on the wall beside the terminal slot (see the template's <see cref="RoomTemplate.Terminal"/>): the power
        /// outlet at (<paramref name="x"/>, <paramref name="z"/>) on a wall facing <paramref name="yaw"/>, the data outlet
        /// 0.25 m along the wall.
        /// </summary>
        private static void DressTerminalOutlets(Transform power, float x, float z, float yaw)
        {
            var along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var at = new Vector3(x, OutletHeight, z);
            Put(Prop.PowerSocket, power, at, yaw, "PowerSocket_Terminal");
            Put(Prop.DataSocket, power, at + along * 0.25f, yaw, "DataSocket_Terminal");
        }

        private static string Corner(int sx, int sz) => $"{(sz > 0 ? "N" : "S")}{(sx > 0 ? "E" : "W")}";
    }
}
