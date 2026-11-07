using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Shared parts of the meet-me room's racks (the optical distribution frames, the copper patch rack, the cross-connect cabinet) and of its overhead cabling: the open
    /// rack frame with its two finger managers, the 1U panels (LC adapters, SFP cages, Cat6A jacks), the duplex LC and RJ45 plugs with their boots, the vertical cord
    /// bundles built from the real cords of every plugged connector, and the hangers and the pipe the room builds from them.
    /// </summary>
    public static partial class Props
    {
        /// <summary>Footprint and height of every rack and cabinet of the meet-me room (the 19 inch frame with a finger manager either side).</summary>
        internal const float MeetRackWidth = 0.8f;

        internal const float MeetRackDepth = 0.6f;
        internal const float MeetRackHeight = 2.2f;

        private const float MeetPlinth = 0.06f;
        private const float FibreUnit = 0.04445f;
        private const float FibreFirstUnit = 0.14f;
        private const int FibreUnits = 43;

        /// <summary>Front plane of the patch panels (rack-local Z).</summary>
        private const float FibrePanelFront = 0.222f;

        /// <summary>Length of a duplex LC plug with its boots, from the front of the adapter to the end of the boots.</summary>
        private const float FibrePlugLength = 0.0245f;

        /// <summary>Rack-local X (either side) and Z of the middle of the cord bundles; the cords of the plugs run up out of the frame there.</summary>
        internal const float FibreBundleX = 0.327f;

        internal const float FibreBundleZ = 0.235f;

        /// <summary>Where the cords of every rack end: the floor of the cable tray over the racks (the same tray as in the hall), and its width.</summary>
        internal const float FibreTrayBottom = 2.64f;

        internal const float FibreTrayWidth = 0.30f;

        // ---------------------------------------------------------------- the rack frame

        /// <summary>
        /// The open 19 inch frame of the meet-me racks, 0.8 x 0.6 x 2.2 m: plinth, four posts, a top ring whose front bar is solid (the cable ways are open behind it, upwards), a thin back sheet
        /// and the two front mounting rails the panels are screwed to. The front faces +Z.
        /// </summary>
        private static void BuildMeetFrame(RackParts p)
        {
            const float half = MeetRackWidth / 2f;
            const float d = MeetRackDepth / 2f;
            const float post = 0.03f;
            float top = MeetRackHeight - 0.04f;
            var m = SurfaceMaterial.RackBlack;
            p.Box(m, -half, 0f, -d, half, MeetPlinth, d);
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    float x0 = sx < 0 ? -half : half - post;
                    float z0 = sz < 0 ? -d : d - post;
                    p.Box(m, x0, MeetPlinth, z0, x0 + post, top, z0 + post);
                }
            }
            p.Box(m, -half, top, -d, half, MeetRackHeight, -d + post);
            // The front bar of the top ring is solid: the cord bundles leave behind it.
            p.Box(m, -half, top, d - post, half, MeetRackHeight, d);
            p.Box(m, -half, top, -d + post, -half + post, MeetRackHeight, d - post);
            p.Box(m, half - post, top, -d + post, half, MeetRackHeight, d - post);
            p.Box(m, -half + post, MeetPlinth, -d, half - post, top, -d + 0.006f);
            foreach (int s in new[] { -1, 1 })
            {
                float rx0 = s < 0 ? -0.2763f : 0.2255f;
                float rx1 = s < 0 ? -0.2255f : 0.2763f;
                // The front mounting rail (the cage-nut strip) stands on the plinth and is tied to the front bar of the top ring, so the frame is one piece.
                p.Box(m, rx0, MeetPlinth, 0.20f, rx1, top, 0.215f);
                p.Box(m, rx0, top, 0.20f, rx1, MeetRackHeight, d - post);
                // The finger manager hangs from the top ring's side bar and stands on the plinth.
                p.Box(m, Mathf.Min(s * 0.285f, s * half), top - 0.04f, 0.10f, Mathf.Max(s * 0.285f, s * half), MeetRackHeight, 0.19f);
                p.Box(m, Mathf.Min(s * 0.285f, s * 0.37f), MeetPlinth, 0.10f, Mathf.Max(s * 0.285f, s * 0.37f), 0.10f, 0.215f);
            }
        }

        /// <summary>Corner radius of the door leaves and of the frame openings they sit in; the leaf stands <see cref="DoorProud"/> in front of its frame, with a 3 mm seam round it.</summary>
        private const float DoorCorner = 0.03f;

        private const float DoorProud = 0.006f;

        /// <summary>
        /// The frame round a door opening (x0..x1, y0..y1) inside the outer rectangle (ox0..ox1, oy0..oy1): four bars and a fillet in each corner, so the opening has the
        /// rounded corners of the leaf (<see cref="DoorCorner"/>) that fills it.
        /// </summary>
        private static void AddDoorSurround(MeshBuilder b, float ox0, float ox1, float oy0, float oy1, float x0, float x1, float y0, float y1, float z0, float z1, float radius = DoorCorner)
        {
            b.AddBox(new Vector3(ox0, oy0, z0), new Vector3(x0, oy1, z1));
            b.AddBox(new Vector3(x1, oy0, z0), new Vector3(ox1, oy1, z1));
            b.AddBox(new Vector3(x0, oy0, z0), new Vector3(x1, y0, z1));
            b.AddBox(new Vector3(x0, y1, z0), new Vector3(x1, oy1, z1));
            // A radius of 0 leaves the opening's corners sharp, as on the open racks.
            AddOpeningFillets(b, x0, x1, y0, y1, z0, z1, radius);
        }

        /// <summary>The four corner pieces that round the corners of the rectangular opening x0..x1, y0..y1 (a square between each corner and its arc), from z0 to z1.</summary>
        private static void AddOpeningFillets(MeshBuilder b, float x0, float x1, float y0, float y1, float z0, float z1, float radius)
        {
            if (radius <= 0f) return;
            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sy in new[] { -1, 1 })
                {
                    // The corner of the opening and the direction into the opening; the filler is the square between the corner and the arc.
                    float cx = sx < 0 ? x0 : x1;
                    float cy = sy < 0 ? y0 : y1;
                    float dx = -sx;
                    float dy = -sy;
                    var loop = new List<Vector2> { new Vector2(cx, cy), new Vector2(cx + dx * radius, cy) };
                    for (int i = 1; i < 6; i++)
                    {
                        float angle = (270f - 90f * i / 6f) * Mathf.Deg2Rad;
                        loop.Add(new Vector2(cx + dx * radius * (1f + Mathf.Cos(angle)), cy + dy * radius * (1f + Mathf.Sin(angle))));
                    }
                    loop.Add(new Vector2(cx, cy + dy * radius));
                    if (dx * dy < 0f) loop.Reverse();
                    b.AddExtrusion(loop, new Vector3(0f, 0f, z0), new Vector3(0f, 0f, z1), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
                }
            }
        }

        /// <summary>The two stiles of a door leaf (x0..x1, y0..y1) either side of its window or panels (inner0..inner1): full height, the outer corners rounded by <paramref name="radius"/>.</summary>
        private static void AddDoorStiles(MeshBuilder b, float x0, float x1, float y0, float y1, float inner0, float inner1, float z0, float z1, float radius = DoorCorner)
        {
            b.AddExtrusion(RoundedRectLoop(x0, inner0, y0, y1, new[] { radius, 0f, 0f, radius }, 4), new Vector3(0f, 0f, z0), new Vector3(0f, 0f, z1), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
            b.AddExtrusion(RoundedRectLoop(inner1, x1, y0, y1, new[] { 0f, radius, radius, 0f }, 4), new Vector3(0f, 0f, z0), new Vector3(0f, 0f, z1), Vector3.right, Vector3.up, Vector3.back, Vector3.forward);
        }

        /// <summary>
        /// A vertical finger manager at one side of the frame (<paramref name="side"/> -1 west, +1 east): a back plate, an outer wall and a finger across
        /// the channel at every rack unit, so every row of patch cords has its own slot.
        /// </summary>
        private static void BuildFingerManager(RackParts p, int side)
        {
            float xi = side * 0.285f;
            float xo = side * 0.37f;
            float x0 = Mathf.Min(xi, xo);
            float x1 = Mathf.Max(xi, xo);
            var m = SurfaceMaterial.BlackHousing;
            p.Box(m, x0, 0.10f, 0.10f, x1, 2.12f, 0.108f);
            float wall = side < 0 ? x0 : x1 - 0.008f;
            p.Box(m, wall, 0.10f, 0.10f, wall + 0.008f, 2.12f, 0.215f);
            for (int i = 0; i < FibreUnits; i++)
            {
                float y = FibreFirstUnit + i * FibreUnit;
                p.Box(m, x0, y, 0.195f, x1, y + 0.012f, 0.21f);
            }
        }

        // ---------------------------------------------------------------- cord bundles

        private static void AddBezier(List<Vector3> path, Vector3 a, Vector3 b, Vector3 c, Vector3 d, int steps, bool includeStart)
        {
            for (int k = includeStart ? 0 : 1; k <= steps; k++)
            {
                float t = k / (float)steps;
                float u = 1f - t;
                path.Add(u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d);
            }
        }

        /// <summary>The cross-section slots of a bundle of round cords laid hexagonally (<paramref name="pitch"/> between centres): the middle first, then ring by ring.</summary>
        private static Vector2[] BundleLattice(float pitch)
        {
            var list = new List<Vector2>();
            for (int j = -6; j <= 6; j++)
            {
                for (int i = -7; i <= 7; i++) list.Add(new Vector2((i + j * 0.5f) * pitch, j * 0.866f * pitch));
            }
            float unit = pitch * pitch / 100f;
            list.Sort((a, b) =>
            {
                int order = Mathf.RoundToInt(a.sqrMagnitude / unit).CompareTo(Mathf.RoundToInt(b.sqrMagnitude / unit));
                return order != 0 ? order : Mathf.Atan2(a.y, a.x).CompareTo(Mathf.Atan2(b.y, b.x));
            });
            return list.GetRange(0, 127).ToArray();
        }

        /// <summary>
        /// The way a whole bundle goes from the top of its rack to the tray over the rack's middle: a smooth S in the depth direction (rack-local Z, Y), two arcs of
        /// <see cref="FibreBendRadius"/> (never tighter), from the bundle line (z <see cref="FibreBundleZ"/>) to the landing line in the tray (z <see cref="FibreTrayLandZ"/>).
        /// Each point is the position along the bundle's axis and the unit tangent; cords are offset sideways along the normal, so they stay parallel.
        /// </summary>
        private static readonly (float z, float y, float tz, float ty)[] FibreBend = BuildFibreBend();

        /// <summary>Height at which the bundles leave the straight climb and begin the S.</summary>
        internal static readonly float FibreBendStart = FibreTrayBottom + 0.004f - 2f * FibreBendRadius * Mathf.Sin(FibreBendAngle()) - 0.02f;

        private const float FibreBendRadius = 0.20f;
        private const float FibreTrayLandZ = 0f;

        private static float FibreBendAngle() => Mathf.Acos(1f - (FibreBundleZ - FibreTrayLandZ) / (2f * FibreBendRadius));

        private static (float z, float y, float tz, float ty)[] BuildFibreBend()
        {
            const int steps = 10;
            float r = FibreBendRadius;
            float theta = FibreBendAngle();
            float y0 = FibreTrayBottom + 0.004f - 2f * r * Mathf.Sin(theta) - 0.02f;
            var list = new List<(float, float, float, float)>();
            // First arc: turning towards the tray line (centre at -Z of the start).
            for (int k = 0; k <= steps; k++)
            {
                float phi = theta * k / steps;
                list.Add((FibreBundleZ - r + r * Mathf.Cos(phi), y0 + r * Mathf.Sin(phi), -Mathf.Sin(phi), Mathf.Cos(phi)));
            }
            float ez = FibreBundleZ - 2f * r * (1f - Mathf.Cos(theta));
            float ey = y0 + 2f * r * Mathf.Sin(theta);
            // Second arc: back to vertical.
            for (int k = steps - 1; k >= 0; k--)
            {
                float psi = theta * k / steps;
                list.Add((ez + r - r * Mathf.Cos(psi), ey - r * Mathf.Sin(psi), -Mathf.Sin(psi), Mathf.Cos(psi)));
            }
            return list.ToArray();
        }

        /// <summary>
        /// The vertical bundle at one side of a rack (<c>side</c> -1 west, +1 east). Every plugged connector sends its own cords into it: each cord leaves the boot
        /// straight forward, bends sideways (a smooth bend of a few centimetres' radius, never a right angle) and runs along its row to the manager, bends up into its own
        /// slot of the bundle and climbs, parallel to the others, over the rack roof, then in a smooth S (<see cref="FibreBend"/>) to the tray over the middle of the rack.
        /// The cords of one patch-panel unit never share a height: each runs at its own level (and, when a unit has more cords than fit in its height, in a second layer in
        /// front), the cord from the plug nearest the manager lowest and in the layer nearest the panel. The first (lowest) cords take the middle slots, later ones the
        /// outer rings, so the bundle grows as it climbs. Cords are collected by <see cref="Route"/> and laid by <see cref="Flush"/>.
        /// </summary>
        private sealed class CordBundle
        {
            private readonly struct Cord
            {
                public Cord(SurfaceMaterial material, Vector3 tip)
                {
                    Material = material;
                    Tip = tip;
                }

                public SurfaceMaterial Material { get; }

                public Vector3 Tip { get; }
            }

            private readonly RackParts parts;
            private readonly int side;
            private readonly Vector2[] lattice;
            private readonly List<Cord> cords = new List<Cord>();

            public CordBundle(RackParts parts, int side, float top, float pitch, float cordRadius)
            {
                this.parts = parts;
                this.side = side;
                CordRadius = cordRadius;
                lattice = BundleLattice(pitch);
            }

            public float CordRadius { get; }

            public bool Full => cords.Count >= lattice.Length - 1;

            /// <summary>Radius of the whole bundle at its top.</summary>
            public float Radius => cords.Count == 0 ? 0f : lattice[Mathf.Min(cords.Count, lattice.Length) - 1].magnitude + CordRadius;

            public void Route(SurfaceMaterial material, Vector3 tip)
            {
                if (cords.Count < lattice.Length) cords.Add(new Cord(material, tip));
            }

            /// <summary>Lays every collected cord.</summary>
            public void Flush()
            {
                float manager = side * FibreBundleX;
                float pitch = CordRadius * 2.6f;
                int capacity = Mathf.Max(1, Mathf.FloorToInt((FibreUnit - 0.010f) / pitch) + 1);
                var order = new List<Cord>(cords);
                order.Sort((a, b) =>
                {
                    int unit = UnitOf(a.Tip.y).CompareTo(UnitOf(b.Tip.y));
                    if (unit != 0) return unit;
                    int near = Mathf.Abs(a.Tip.x - manager).CompareTo(Mathf.Abs(b.Tip.x - manager));
                    return near != 0 ? near : a.Tip.y.CompareTo(b.Tip.y);
                });
                var ranks = new Dictionary<int, int>();
                for (int i = 0; i < order.Count; i++)
                {
                    int unit = UnitOf(order[i].Tip.y);
                    ranks.TryGetValue(unit, out int rank);
                    ranks[unit] = rank + 1;
                    float level = FibreFirstUnit + unit * FibreUnit + 0.0045f + (rank % capacity) * pitch;
                    int layer = rank / capacity;
                    Lay(order[i], lattice[i], level, layer, pitch);
                }
                cords.Clear();
                foreach (var cord in order) cords.Add(cord);
            }

            private static int UnitOf(float y) => Mathf.FloorToInt((y - FibreFirstUnit) / FibreUnit);

            private void Lay(Cord cord, Vector2 slot, float level, int layer, float pitch)
            {
                var tip = cord.Tip;
                float sx = side * FibreBundleX + slot.x;
                float sz = FibreBundleZ + slot.y;
                float laneZ = tip.z + 0.020f + layer * pitch * 1.15f;
                float total = Mathf.Abs(sx - tip.x);
                float first = Mathf.Min(0.06f, total * 0.45f);
                float last = Mathf.Min(0.045f, total * 0.35f);
                var path = new List<Vector3>();
                var e1 = new Vector3(tip.x + side * first, level, laneZ);
                AddBezier(path, tip, tip + new Vector3(0f, 0f, (laneZ - tip.z) * 0.8f), e1 - new Vector3(side * first * 0.5f, 0f, 0f), e1, 8, true);
                var e2 = new Vector3(sx - side * last, level, laneZ);
                path.Add(e2);
                var f = new Vector3(sx, level + 0.05f, sz);
                AddBezier(path, e2, e2 + new Vector3(side * last * 0.7f, 0f, 0f), f - new Vector3(0f, 0.03f, 0f), f, 6, false);
                // Straight up over the rack roof, then the S to the tray: the cord keeps its place in the bundle, measured along the normal of the S.
                path.Add(new Vector3(sx, FibreBend[0].y, sz));
                for (int k = 1; k < FibreBend.Length; k++)
                {
                    var (z, y, tz, ty) = FibreBend[k];
                    path.Add(new Vector3(sx, y + slot.y * -tz, z + slot.y * ty));
                }
                var last3 = FibreBend[FibreBend.Length - 1];
                path.Add(new Vector3(sx, FibreTrayBottom + 0.012f, last3.z + slot.y));
                parts[cord.Material].AddTube(path, CordRadius, 5, Vector3.zero, Quaternion.identity);
            }
        }

        private static CordBundle[] NewBundles(RackParts p, float top, float pitch, float cordRadius) =>
            new[] { new CordBundle(p, -1, top, pitch, cordRadius), new CordBundle(p, 1, top, pitch, cordRadius) };

        /// <summary>Lays the cords of both bundles of a rack and ties the straight climbs with black hook-and-loop straps every 35 cm (a band with a rim, so the cords show through it).</summary>
        private static void FinishBundles(RackParts p, CordBundle[] bundles)
        {
            foreach (var bundle in bundles) bundle.Flush();
            AddStraps(p, bundles[0], -1);
            AddStraps(p, bundles[1], 1);
            AddConduit(p, bundles[0], -1);
            AddConduit(p, bundles[1], 1);
        }

        /// <summary>
        /// The conduit round a bundle (green like the hall's data drops, galvanised flange rings), from the rack roof along the bundle's S (<see cref="FibreBend"/>) to the floor of the tray over
        /// the rack's middle, so the cords go into a pipe and arrive exactly at the middle of the tray.
        /// </summary>
        private static void AddConduit(RackParts p, CordBundle bundle, int side)
        {
            if (bundle.Radius <= 0f) return;
            float r = bundle.Radius + 0.012f;
            float x = side * FibreBundleX;
            var path = new List<Vector3> { new Vector3(x, MeetRackHeight, FibreBundleZ), new Vector3(x, FibreBend[0].y, FibreBundleZ) };
            for (int k = 1; k < FibreBend.Length; k++) path.Add(new Vector3(x, FibreBend[k].y, FibreBend[k].z));
            path.Add(new Vector3(x, FibreTrayBottom, FibreBend[FibreBend.Length - 1].z));
            var steel = p[SurfaceMaterial.GalvanisedSteel];
            p[SurfaceMaterial.ConduitGreen].AddTube(path, r, 16, Vector3.zero, Quaternion.identity);
            var ring = new[] { P(r, 0f, true), P(r + 0.014f, 0f, true), P(r + 0.014f, 0.008f, true), P(r, 0.008f, true) };
            steel.AddLathe(ring, 20, true, new Vector3(x, MeetRackHeight, FibreBundleZ), Quaternion.identity);
            steel.AddLathe(ring, 20, true, new Vector3(x, FibreTrayBottom - 0.008f, FibreBend[FibreBend.Length - 1].z), Quaternion.identity);
        }

        private static void AddStraps(RackParts p, CordBundle bundle, int side)
        {
            if (bundle.Radius <= 0f) return;
            float top = FibreBendStart;
            for (float y = 0.35f; y < top - 0.03f; y += 0.35f)
            {
                float fraction = Mathf.Clamp01((y - 0.14f) / (top - 0.14f));
                float r = bundle.CordRadius + (bundle.Radius - bundle.CordRadius) * Mathf.Sqrt(fraction) + 0.004f;
                p[SurfaceMaterial.BlackHousing].AddLathe(new[] { P(r, 0f, true), P(r, 0.02f, true), P(r - 0.004f, 0.02f, true), P(r - 0.004f, 0f, true) }, 18, true,
                    new Vector3(side * FibreBundleX, y, FibreBundleZ), Quaternion.identity);
            }
        }

        private static float FibreSlotY(int slot) => FibreFirstUnit + slot * FibreUnit;

        // ---------------------------------------------------------------- patch panels

        /// <summary>The faceplate of a 1U panel: ears with screws, the body behind it and a white label strip.</summary>
        private static void AddPanelFace(RackParts p, int slot, SurfaceMaterial plate)
        {
            float y = FibreSlotY(slot);
            p.Box(plate, -0.2413f, y + 0.001f, 0.215f, 0.2413f, y + FibreUnit - 0.001f, FibrePanelFront);
            p.Box(SurfaceMaterial.ServerBlack, -0.225f, y + 0.003f, 0.11f, 0.225f, y + FibreUnit - 0.003f, 0.215f);
            foreach (int s in new[] { -1, 1 })
            {
                foreach (float dy in new[] { 0.011f, 0.0335f })
                {
                    p.Disc(SurfaceMaterial.ServerSilver, s * 0.2335f, y + dy, FibrePanelFront + 0.0001f, 0.0028f, 8);
                }
            }
            p.Box(SurfaceMaterial.DetectorWhite, -0.215f, y + 0.003f, FibrePanelFront, -0.145f, y + 0.0095f, FibrePanelFront + 0.0006f);
        }

        /// <summary>A duplex LC adapter in a blue housing: a bar round a 13.8 x 8.8 mm opening, 5.5 mm deep.</summary>
        private static void AddAdapter(RackParts p, float cx, float yc, float front)
        {
            var blue = SurfaceMaterial.SupportBlue;
            float z1 = front + 0.0055f;
            p.Box(blue, cx - 0.0102f, yc - 0.0068f, front, cx - 0.0069f, yc + 0.0068f, z1);
            p.Box(blue, cx + 0.0069f, yc - 0.0068f, front, cx + 0.0102f, yc + 0.0068f, z1);
            p.Box(blue, cx - 0.0069f, yc + 0.0044f, front, cx + 0.0069f, yc + 0.0068f, z1);
            p.Box(blue, cx - 0.0069f, yc - 0.0068f, front, cx + 0.0069f, yc - 0.0044f, z1);
        }

        /// <summary>What shows in an empty duplex adapter: the dark opening and the two white ceramic sleeves at its back.</summary>
        private static void AddAdapterInside(RackParts p, float cx, float yc, float front)
        {
            p.Cavity(SurfaceMaterial.ServerVoid, cx - 0.0069f, yc - 0.0044f, cx + 0.0069f, yc + 0.0044f, front + 0.0055f, front + 0.0005f);
            foreach (int s in new[] { -1, 1 })
            {
                p.Disc(SurfaceMaterial.DetectorWhite, cx + s * 0.0052f, yc, front + 0.0007f, 0.0022f, 12);
                p.Disc(SurfaceMaterial.ServerVoid, cx + s * 0.0052f, yc, front + 0.0009f, 0.0008f, 8);
            }
        }

        /// <summary>
        /// A duplex LC plug in an adapter or SFP module whose front face is at <paramref name="zFace"/>: two latched housings joined by the clip, the pull tab, the
        /// angled latch levers, and two tapered boots with a strain-relief ring in <paramref name="boot"/>. Returns the tips of the two boots (the cords start there).
        /// </summary>
        private static (Vector3 left, Vector3 right) AddLcDuplexPlug(RackParts p, float cx, float yc, float zFace, SurfaceMaterial boot)
        {
            var white = SurfaceMaterial.DetectorWhite;
            foreach (int s in new[] { -1, 1 })
            {
                float x = cx + s * 0.0052f;
                p.Box(white, x - 0.0030f, yc - 0.0044f, zFace, x + 0.0030f, yc + 0.0044f, zFace + 0.0100f);
                p.Box(white, x - 0.0021f, yc + 0.0052f, zFace + 0.0030f, x + 0.0021f, yc + 0.0060f, zFace + 0.0085f);
                p.Box(SurfaceMaterial.ServerSilver, x - 0.0010f, yc - 0.0010f, zFace + 0.0100f, x + 0.0010f, yc + 0.0010f, zFace + 0.0105f);
                p[boot].AddLathe(new[]
                {
                    P(0.0040f, 0f, true), P(0.0040f, 0.0035f, true), P(0.0046f, 0.0036f, true), P(0.0046f, 0.0046f, true), P(0.0034f, 0.0047f, true),
                    P(0.0030f, 0.0075f), P(0.0023f, 0.0110f, true), P(0.0001f, 0.0110f, true),
                }, 12, true, new Vector3(x, yc, zFace + 0.0135f), Quaternion.Euler(90f, 0f, 0f));
            }
            p.Box(white, cx - 0.0086f, yc + 0.0044f, zFace + 0.0020f, cx + 0.0086f, yc + 0.0052f, zFace + 0.0105f);
            p.Box(white, cx - 0.0086f, yc - 0.0046f, zFace + 0.0105f, cx + 0.0086f, yc + 0.0054f, zFace + 0.0135f);
            float z = zFace + FibrePlugLength;
            return (new Vector3(cx - 0.0052f, yc, z), new Vector3(cx + 0.0052f, yc, z));
        }

        private static SurfaceMaterial FibreCordMaterial(int key) => key % 5 == 0 ? SurfaceMaterial.LockerDoor : SurfaceMaterial.FibreCord;

        /// <summary>Plugs a duplex plug into the adapter at <c>(cx, yc)</c> and sends both its cords into the bundle at that side.</summary>
        private static void PlugAndRoute(RackParts p, CordBundle[] bundles, float cx, float yc, float zFace, int key)
        {
            var cord = FibreCordMaterial(key);
            var (left, right) = AddLcDuplexPlug(p, cx, yc, zFace, cord);
            var bundle = bundles[cx < 0f ? 0 : 1];
            bundle.Route(cord, left);
            bundle.Route(cord, right);
        }

        /// <summary>
        /// A 1U high-density fibre patch panel: 18 blue duplex LC adapters (36 ports) in a black faceplate with ears and screws; every <paramref name="every"/>-th adapter
        /// carries a duplex plug whose two yellow (some aqua) cords run into the bundle of the nearer manager; the others show their dark opening and ceramic sleeves.
        /// </summary>
        private static void AddFibrePanel(RackParts p, int slot, CordBundle[] bundles, int every)
        {
            float yc = FibreSlotY(slot) + 0.0245f;
            AddPanelFace(p, slot, SurfaceMaterial.ServerBlack);
            for (int i = 0; i < 18; i++)
            {
                float cx = (i - 8.5f) * 0.0225f;
                AddAdapter(p, cx, yc, FibrePanelFront);
                int key = slot * 17 + i * 5;
                if (key % every == 0 && !bundles[cx < 0f ? 0 : 1].Full) PlugAndRoute(p, bundles, cx, yc, FibrePanelFront + 0.0055f, key);
                else AddAdapterInside(p, cx, yc, FibrePanelFront);
            }
        }

        /// <summary>
        /// A 1U panel of 48 SFP+ cages (two rows of 24, real holes through a deep faceplate with the cage and edge connector inside, a link LED between the rows). Every
        /// <paramref name="every"/>-th cage holds a transceiver: a silver body with a coloured wire bail latch, a label tab and a black bezel, with a duplex LC plug whose cords
        /// run into the bundle of the nearer manager.
        /// </summary>
        private static void AddSfpPanel(RackParts p, int slot, CordBundle[] bundles, int every)
        {
            float y = FibreSlotY(slot);
            float front = FibrePanelFront;
            float zBack = front - PortDepth;
            var holes = new List<(float xa, float ya, float xb, float yb)>();
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i < 24; i++) SfpHoles(holes, (i - 11.5f) * 0.0155f, y + (row == 0 ? 0.0058f : 0.0267f));
            }
            PlateWithHoles(p, SurfaceMaterial.ServerBlack, -0.2413f, y + 0.001f, 0.2413f, y + FibreUnit - 0.001f, zBack, front, holes);
            p.Box(SurfaceMaterial.ServerBlack, -0.225f, y + 0.003f, 0.11f, 0.225f, y + FibreUnit - 0.003f, zBack);
            foreach (int s in new[] { -1, 1 })
            {
                p.Box(SurfaceMaterial.ServerBlack, s < 0 ? -0.2413f : 0.2263f, y + 0.001f, 0.205f, s < 0 ? -0.2263f : 0.2413f, y + FibreUnit - 0.001f, front + 0.0006f);
                foreach (float dy in new[] { 0.011f, 0.0335f })
                {
                    p.Disc(SurfaceMaterial.ServerSilver, s * 0.2338f, y + dy, front + 0.0007f, 0.0028f, 8);
                }
            }
            p.Box(SurfaceMaterial.DetectorWhite, -0.215f, y + 0.0185f, front, -0.17f, y + 0.0235f, front + 0.0006f);
            var bails = new[] { SurfaceMaterial.SupportBlue, SurfaceMaterial.SafetyYellow, SurfaceMaterial.LatchRed, SurfaceMaterial.ServerSilver };
            for (int row = 0; row < 2; row++)
            {
                float yLow = y + (row == 0 ? 0.0058f : 0.0267f);
                for (int i = 0; i < 24; i++)
                {
                    float cx = (i - 11.5f) * 0.0155f;
                    int key = slot * 29 + row * 24 + i;
                    if (row == 0) LedBox(p, key % 13 == 0 ? SurfaceMaterial.LedAmber : SurfaceMaterial.SafetyGreen, cx, y + 0.0206f, front, 0.0040f, 0.0020f);
                    if (key % every != 0 || bundles[cx < 0f ? 0 : 1].Full)
                    {
                        SfpInside(p, cx, yLow, front, zBack);
                        continue;
                    }
                    float h = SfpHeight;
                    p.Box(SurfaceMaterial.ServerSilver, cx - 0.0063f, yLow + 0.0006f, front, cx + 0.0063f, yLow + h - 0.0006f, front + 0.0105f);
                    p.Box(SurfaceMaterial.ServerBlack, cx - 0.0063f, yLow + 0.0006f, front + 0.0105f, cx + 0.0063f, yLow + h - 0.0006f, front + 0.0125f);
                    p.Box(SurfaceMaterial.DetectorWhite, cx - 0.0040f, yLow + h - 0.0006f, front + 0.0030f, cx + 0.0040f, yLow + h + 0.0003f, front + 0.0100f);
                    p[bails[key % 4]].AddTube(new[]
                    {
                        new Vector3(cx - 0.0052f, yLow + 0.0010f, front + 0.0100f), new Vector3(cx - 0.0052f, yLow - 0.0004f, front + 0.0130f),
                        new Vector3(cx + 0.0052f, yLow - 0.0004f, front + 0.0130f), new Vector3(cx + 0.0052f, yLow + 0.0010f, front + 0.0100f),
                    }, 0.0007f, 5, Vector3.zero, Quaternion.identity);
                    PlugAndRoute(p, bundles, cx, yLow + h / 2f, front + 0.0125f, key);
                }
            }
        }

        /// <summary>
        /// A 1U 24-port Cat6A patch panel: a deep black faceplate with 24 real RJ45 jacks (latch notch, gold contacts), white number strips over each group of six; every
        /// sixth jack holds a plug (clear housing, latch lever, ribbed strain-relief boot) with a blue or white cord running into the bundle of the nearer manager.
        /// </summary>
        private static void AddCopperPanel(RackParts p, int slot, CordBundle[] bundles)
        {
            float y = FibreSlotY(slot);
            float front = FibrePanelFront;
            float zBack = front - PortDepth;
            float yLow = y + 0.0165f;
            var holes = new List<(float xa, float ya, float xb, float yb)>();
            for (int i = 0; i < 24; i++) Rj45Holes(holes, (i - 11.5f) * 0.0172f, yLow);
            PlateWithHoles(p, SurfaceMaterial.ServerBlack, -0.2413f, y + 0.001f, 0.2413f, y + FibreUnit - 0.001f, zBack, front, holes);
            p.Box(SurfaceMaterial.ServerBlack, -0.225f, y + 0.003f, 0.11f, 0.225f, y + FibreUnit - 0.003f, zBack);
            foreach (int s in new[] { -1, 1 })
            {
                p.Box(SurfaceMaterial.ServerBlack, s < 0 ? -0.2413f : 0.2263f, y + 0.001f, 0.205f, s < 0 ? -0.2263f : 0.2413f, y + FibreUnit - 0.001f, front + 0.0006f);
                foreach (float dy in new[] { 0.011f, 0.0335f })
                {
                    p.Disc(SurfaceMaterial.ServerSilver, s * 0.2338f, y + dy, front + 0.0007f, 0.0028f, 8);
                }
            }
            for (int g = 0; g < 4; g++)
            {
                float x0 = (g * 6 - 11.5f) * 0.0172f - 0.0070f;
                p.Box(SurfaceMaterial.DetectorWhite, x0, y + 0.0364f, front, x0 + 5f * 0.0172f + 0.014f, y + 0.0404f, front + 0.0006f);
            }
            for (int i = 0; i < 24; i++)
            {
                float cx = (i - 11.5f) * 0.0172f;
                Rj45Inside(p, cx, yLow, front, zBack);
                int key = slot * 24 + i;
                var bundle = bundles[cx < 0f ? 0 : 1];
                if (key % 6 != 0 || bundle.Full) continue;
                var cord = key % 2 == 0 ? SurfaceMaterial.DataCable : SurfaceMaterial.SpeakerWhite;
                float yMid = yLow + Rj45Height / 2f;
                p.Box(SurfaceMaterial.DetectorWhite, cx - 0.0054f, yMid - 0.0040f, front, cx + 0.0054f, yMid + 0.0040f, front + 0.0140f);
                p.Box(SurfaceMaterial.DetectorWhite, cx - 0.0020f, yMid + 0.0040f, front + 0.0030f, cx + 0.0020f, yMid + 0.0052f, front + 0.0125f);
                p[cord].AddLathe(new[]
                {
                    P(0.0050f, 0f, true), P(0.0050f, 0.0030f, true), P(0.0042f, 0.0035f, true), P(0.0042f, 0.0070f, true), P(0.0046f, 0.0072f, true),
                    P(0.0046f, 0.0082f, true), P(0.0040f, 0.0084f, true), P(0.0038f, 0.0130f), P(0.0035f, 0.0180f, true), P(0.0001f, 0.0180f, true),
                }, 12, true, new Vector3(cx, yMid, front + 0.0140f), Quaternion.Euler(90f, 0f, 0f));
                bundle.Route(cord, new Vector3(cx, yMid, front + 0.0320f));
            }
        }

        /// <summary>A 1U cable manager between two panels: a black bar and, unless <paramref name="fingers"/> is false, a row of cord fingers in front of it.</summary>
        private static void AddHorizontalManager(RackParts p, int slot, bool fingers)
        {
            float y = FibreSlotY(slot);
            p.Box(SurfaceMaterial.ServerBlack, -0.2413f, y + 0.001f, 0.20f, 0.2413f, y + FibreUnit - 0.001f, 0.2235f);
            if (!fingers) return;
            for (int i = 0; i < 15; i++)
            {
                float x = (i - 7) * 0.031f;
                p.Box(SurfaceMaterial.RackBlack, x - 0.0025f, y + 0.005f, 0.2235f, x + 0.0025f, y + FibreUnit - 0.005f, 0.245f);
            }
        }

        // ---------------------------------------------------------------- cross-connect panels

        private static bool JumperPlugged(int slot, int i) => i >= 1 && i <= 16 && (slot * 13 + i) % 4 != 0;

        private static int JumperTarget(int i) => (i - 1 + 5) % 16 + 1;

        private static int JumperSource(int m) => (m - 1 - 5 + 16) % 16 + 1;

        /// <summary>
        /// A 1U LC panel of a cross-connect cabinet: the adapters with a status LED over each (green, a few amber); ports 1-16 of a panel at a slot divisible by four are
        /// patched with duplex jumpers (two smooth cords each, bulging forward in a loop) to a shifted port of the panel two slots above; the two outer ports of every
        /// panel are uplinks with cords into the bundle at their side.
        /// </summary>
        private static void AddCrossConnectPanel(RackParts p, int slot, CordBundle[] bundles)
        {
            float yc = FibreSlotY(slot) + 0.0245f;
            float front = FibrePanelFront;
            float zFace = front + 0.0055f;
            AddPanelFace(p, slot, SurfaceMaterial.ServerBlack);
            bool lower = slot % 4 == 0 && slot + 2 < FibreUnits;
            bool upper = slot % 4 == 2;
            for (int i = 0; i < 18; i++)
            {
                float cx = (i - 8.5f) * 0.0225f;
                AddAdapter(p, cx, yc, front);
                int key = slot * 7 + i;
                LedBox(p, key % 11 == 0 ? SurfaceMaterial.LedAmber : SurfaceMaterial.SafetyGreen, cx, FibreSlotY(slot) + FibreUnit - 0.0055f, front, 0.0030f, 0.0024f);
                bool uplink = i == 0 || i == 17;
                bool plugged = uplink ? (slot + i) % 3 != 0 : lower ? JumperPlugged(slot, i) : upper && JumperPlugged(slot - 2, JumperSource(i));
                if (!plugged)
                {
                    AddAdapterInside(p, cx, yc, front);
                    continue;
                }
                var cord = FibreCordMaterial(key);
                var (left, right) = AddLcDuplexPlug(p, cx, yc, zFace, cord);
                if (uplink)
                {
                    var bundle = bundles[i == 0 ? 0 : 1];
                    bundle.Route(cord, left);
                    bundle.Route(cord, right);
                }
                else if (lower)
                {
                    float targetX = (JumperTarget(i) - 8.5f) * 0.0225f;
                    float dy = 2f * FibreUnit;
                    float lift = 0.034f + 0.006f * (i % 3);
                    foreach (int s in new[] { -1, 1 })
                    {
                        var a = s < 0 ? left : right;
                        var b = new Vector3(targetX + s * 0.0052f, yc + dy, a.z);
                        var path = new List<Vector3>();
                        AddBezier(path, a, a + new Vector3(0f, 0f, lift), b + new Vector3(0f, 0f, lift), b, 12, true);
                        p[cord].AddTube(path, 0.0021f, 5, Vector3.zero, Quaternion.identity);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- overhead cabling (built by the meet-me room)

        /// <summary>
        /// Trapeze hangers (two threaded rods to the ceiling, a strut under the load, as under the cable trays in the hall) for something <paramref name="halfSpan"/> wide
        /// either side of every <c>underside</c> point, running along <c>along</c> (a horizontal unit axis).
        /// </summary>
        internal static void FibreHangers(Transform parent, string name, IEnumerable<(Vector3 underside, Vector3 along)> hangers, float halfSpan, float ceiling)
        {
            var steel = new MeshBuilder();
            foreach (var (underside, along) in hangers)
            {
                InfrastructureModels.Hanger(steel, underside, Vector3.Cross(Vector3.up, along).normalized, along, halfSpan, ceiling);
            }
            steel.Attach(name, parent, SurfaceMaterial.GalvanisedSteel);
        }

        /// <summary>
        /// The vertical galvanised duct over a central unit where the trays of both rows end, a rectangle with rounded corners like the tray (<paramref name="halfX"/> x <paramref name="halfZ"/>
        /// either side of <paramref name="centre"/>): from <paramref name="bottom"/> up through the ceiling, with a flange at its foot and at the height of the trays, and a red fire-stop collar at the ceiling.
        /// </summary>
        internal static void FibreDuct(Transform parent, string name, Vector3 centre, float bottom, float top, float halfX, float halfZ, float trayHeight)
        {
            var duct = new MeshBuilder();
            var collar = new MeshBuilder();
            const float corner = 0.04f;
            void Band(MeshBuilder b, float grow, float y0, float y1) =>
                b.AddExtrusion(RoundedRectLoop(centre.x - halfX - grow, centre.x + halfX + grow, centre.z - halfZ - grow, centre.z + halfZ + grow, corner + grow, 3),
                    new Vector3(0f, y0, 0f), new Vector3(0f, y1, 0f), Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
            Band(duct, 0f, bottom, top);
            // The two flange rings are the same distance above and below the tray (its floor to the top of its sides).
            const float ringGap = 0.06f;
            foreach (float y in new[] { bottom, trayHeight - ringGap - 0.012f, trayHeight + InfrastructureModels.TraySideHeight + ringGap }) Band(duct, 0.014f, y, y + 0.012f);
            Band(collar, 0.035f, top - 0.12f, top);
            var group = ModelUtility.CreateGroup(name, parent);
            duct.Attach($"{name}_Duct", group, SurfaceMaterial.GalvanisedSteel);
            collar.Attach($"{name}_Collar", group, SurfaceMaterial.SafetyRed);
        }
    }
}
