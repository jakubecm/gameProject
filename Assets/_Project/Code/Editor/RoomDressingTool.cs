using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Dresses the graybox rooms. The shell gets surface materials, door frames and a suspended ceiling
    /// (600 mm grid at the 3 m ceiling plane with a tile centered on the room center, so the room height does not
    /// change and a corridor's lights sit in its middle); each layout of each room (<see cref="RoomLayouts"/>) gets a
    /// <c>Dressing</c> group with furniture, light fittings, fire safety and the building services, and a
    /// <c>SecurityCameras</c> group. Every cable, conduit, pipe and duct run starts and ends in the floor, the ceiling, a
    /// wall or a device, so it never stops in mid-air and rooms stay independent modules.
    ///
    /// Which services show follows the kind of space: offices hide them above a suspended ceiling and in parapet
    /// trunking; corridors show pipe risers, outlet boxes and one ventilation grille per cell, but no ducts, data trays or
    /// breaker boards; the technical rooms (power room, cooling plant, storage) have no suspended ceiling (the dresser
    /// switches the shell's off), an epoxy floor and show what belongs to them.
    ///
    /// Fire safety and security follow common practice (NFPA 13 / EN 12845 light hazard, EN 54 detection): sprinklers at
    /// most about 4 m apart and 2.3 m from a wall, in a corridor one row on the axis of the lights; one smoke detector per
    /// corridor cell or small room, three in the office; an extinguisher and a manual call point by the exit; ceiling
    /// loudspeakers (voice alarm) in the suspended ceilings; one camera per room or corridor, two at a T-junction or a
    /// crossing. Every room has a light switch beside its door. The hall
    /// (see <see cref="HallKitTool"/>) has no water at all.
    ///
    /// Furniture follows what a room is for and always leaves room to walk: desks keep 1 m behind them for the chair and
    /// about 0.9 m beyond it. Only the security office has lockers and a bench, only the storage room has shelving.
    /// Plant keeps its service clearances: 1 m between machines, at least 0.6 m to the walls (0.8 m behind transformers).
    ///
    /// Dressing is looks only; the game logic (rack and terminal slots) stays outside it. Everything stays clear of
    /// the openings, the keep-clear walkway (up to the opening height), the door pockets beside every opening of a
    /// room with automatic doors (<see cref="RoomContract.DoorPocketEnd"/>), the rack slots and the space in front of
    /// terminal slots. Ceiling fittings sit in the centre of a grid tile (multiples of 0.6 m from the room center) or,
    /// for 1200 mm fittings, across two tiles centered on a grid line.
    ///
    /// Every room type has exactly one layout. The dressers below place everything through <see cref="Put"/> and friends.
    ///
    /// <see cref="RebuildAll"/> (run by Create Preview Scene) throws the generated logic, dressing, props and material settings
    /// away and builds them again.
    /// </summary>
    public static class RoomDressingTool
    {
        public const string DressingName = "Dressing";
        public const string SuspendedCeilingName = "SuspendedCeiling";

        /// <summary>
        /// Group of the CCTV cameras in a room. Cameras are not dressing: the security office shows their feeds,
        /// so they sit in their own group next to <c>Dressing</c>.
        /// </summary>
        public const string SecurityCamerasName = "SecurityCameras";

        /// <summary>
        /// Size of a suspended-ceiling tile. A tile is centered on the room center, so grid lines run at odd multiples of
        /// half a tile and tile centers at multiples of a tile.
        /// </summary>
        public const float CeilingTile = 0.6f;

        /// <summary>Half width of the corridor arms (the keep-clear cross is 1.2 m either side).</summary>
        public const float CorridorHalfWidth = 1.5f;

        // Heights of the trays and ducts under a bare ceiling: above the 2.6 m door head, below the 3 m slab.
        private const float TrayBottom = 2.62f;
        private const float TrayWidth = 0.3f;

        // Socket heights: flush wall outlets and the desk-height parapet trunking in the office.
        private const float OutletHeight = 0.3f;
        private const float ParapetHeight = 0.88f;

        /// <summary>Center height of the small grilles under a 3 m ceiling (<see cref="GrayboxPropsTool.GrilleGap"/> below it).</summary>
        private static float HighGrilleHeight => GrayboxPropsTool.HighGrilleCenter(RoomContract.CeilingHeight);

        private delegate void Dresser(Transform dressing, Transform cameras);

        private static readonly Dictionary<RoomType, Dresser> Dressers = new Dictionary<RoomType, Dresser>
        {
            { RoomType.Corridor, (d, c) => DressCorridor(d, c, RoomType.Corridor, layoutIndex) },
            { RoomType.CorridorStraight, (d, c) => DressCorridor(d, c, RoomType.CorridorStraight, layoutIndex) },
            { RoomType.CorridorT, (d, c) => DressCorridor(d, c, RoomType.CorridorT, layoutIndex) },
            { RoomType.CorridorCorner, (d, c) => DressCorridor(d, c, RoomType.CorridorCorner, layoutIndex) },
            { RoomType.CorridorDeadEnd, (d, c) => DressCorridor(d, c, RoomType.CorridorDeadEnd, layoutIndex) },
            { RoomType.SmallOffice, DressSmallOffice },
            { RoomType.SecurityOffice, DressSecurityOffice },
            { RoomType.PowerRoom, DressPowerRoom },
            { RoomType.HvacRoom, DressHvacRoom },
            { RoomType.StorageRoom, DressStorageRoom },
            { RoomType.Elevator, (d, c) => DressCorridor(d, c, RoomType.Elevator, layoutIndex) },
        };

        /// <summary>Technical rooms: no suspended ceiling, epoxy floor, services exposed.</summary>
        public static bool IsTechnicalRoom(RoomType type) =>
            type == RoomType.PowerRoom || type == RoomType.HvacRoom || type == RoomType.StorageRoom;

        private static float Wall => RoomContract.InteriorHalfSize;

        /// <summary>Inner face of a small room's side walls.</summary>
        private static float Side => RoomContract.SmallRoomHalfWidth;

        private static float Ceiling => RoomContract.CeilingHeight;

        /// <summary>The layout being dressed (corridor layouts differ in their fire arm, see <see cref="RoomRules.FireArmSide"/>).</summary>
        private static int layoutIndex;

        /// <summary>The whole rebuild without asking: props, materials, every layout's logic and dressing, definitions.</summary>
        internal static void RebuildAll()
        {
            GrayboxMaterials.ResetAll();
            GrayboxPropsTool.BuildProps(true);
            GrayboxRoomsTool.CreateGrayboxRooms();
            foreach (var (type, layout) in RoomLayouts.All())
            {
                GrayboxRoomsTool.RebuildLogic(type, layout);
            }
            DressAll(true);
        }

        private static void DressAll(bool rebuild)
        {
            EditPrefab(RoomShellTool.ShellPath, root => DressShell(root, rebuild));
            EditPrefab(RoomShellTool.WallPlugPath, root => AssignDefaultSurfaces(root));

            foreach (var (type, layout) in RoomLayouts.All())
            {
                if (!Dressers.TryGetValue(type, out var dress)) continue;
                bool technical = IsTechnicalRoom(type);
                EditPrefab(RoomLayouts.PrefabPath(type, layout), root =>
                    ConfigureShell(root, technical) | DressRoom(root, dress, layout, rebuild));
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// A technical room shows its services under the bare slab: the shell's suspended ceiling is switched off in the
        /// variant and the floor gets the hall's epoxy. Other rooms keep the shell as it is.
        /// </summary>
        private static bool ConfigureShell(GameObject root, bool technical)
        {
            var shell = root.transform.Find("Shell");
            if (shell == null) return false;
            bool changed = false;
            var ceiling = shell.Find(SuspendedCeilingName);
            if (ceiling != null && ceiling.gameObject.activeSelf == technical)
            {
                ceiling.gameObject.SetActive(!technical);
                changed = true;
            }
            var floor = shell.Find("Floor");
            var mesh = floor != null ? floor.GetComponent<ProBuilderMesh>() : null;
            if (technical && mesh != null && mesh.GetComponent<MeshRenderer>().sharedMaterial != GrayboxMaterials.Get(SurfaceMaterial.HallFloor))
            {
                GrayboxUtility.SetSurface(mesh, SurfaceMaterial.HallFloor);
                changed = true;
            }
            return changed;
        }

        /// <summary>Opens a prefab, lets <paramref name="edit"/> change it and saves it only if something changed.</summary>
        private static void EditPrefab(string path, Func<GameObject, bool> edit)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogError($"{path} not found. Run Tools/Cold Shutdown/Create Preview Scene first.");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (edit(root))
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"Dressed {path}.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool DressShell(GameObject root, bool rebuild)
        {
            bool changed = AssignDefaultSurfaces(root);
            var shell = root.transform.Find("Shell");

            foreach (Direction side in Enum.GetValues(typeof(Direction)))
            {
                var wall = shell.Find($"Wall_{side}");
                changed |= Replace(wall, nameof(GrayboxProp.DoorFrame), rebuild,
                    // The wall group stands on the opening pose, which is the door frame's pivot.
                    () => GrayboxPropsTool.Place(GrayboxProp.DoorFrame, wall, Vector3.zero));
            }

            changed |= Replace(shell, SuspendedCeilingName, rebuild, () => BuildSuspendedCeiling(shell));
            return changed;
        }

        /// <summary>
        /// Builds the room's <c>Dressing</c> and <c>SecurityCameras</c> groups if the dressing is missing; on a rebuild
        /// both are thrown away first.
        /// </summary>
        private static bool DressRoom(GameObject root, Dresser dress, int layout, bool rebuild)
        {
            var existing = root.transform.Find(DressingName);
            if (existing != null && !rebuild) return false;
            foreach (var name in new[] { DressingName, SecurityCamerasName })
            {
                Transform child;
                while ((child = root.transform.Find(name)) != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            layoutIndex = layout;
            try
            {
                var dressing = GrayboxUtility.CreateGroup(DressingName, root.transform);
                var cameras = GrayboxUtility.CreateGroup(SecurityCamerasName, root.transform);
                dress(dressing, cameras);
            }
            finally
            {
                layoutIndex = 0;
            }
            return true;
        }

        /// <summary>Builds the named child if it is missing; on a rebuild the old one is removed first.</summary>
        private static bool Replace(Transform parent, string childName, bool rebuild, Action build)
        {
            var existing = parent.Find(childName);
            if (existing != null && !rebuild)
            {
                return false;
            }
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            build();
            return true;
        }

        /// <summary>Gives meshes that still use ProBuilder's default material the surface their name implies.</summary>
        private static bool AssignDefaultSurfaces(GameObject root)
        {
            bool changed = false;
            foreach (var mesh in root.GetComponentsInChildren<ProBuilderMesh>())
            {
                var materials = mesh.GetComponent<MeshRenderer>().sharedMaterials;
                if (materials.Length == 0 || materials.Any(m => m != BuiltinMaterials.defaultMaterial))
                {
                    continue;
                }

                var surface = mesh.name == "Floor" ? SurfaceMaterial.Floor
                    : mesh.name == "Ceiling" ? SurfaceMaterial.Ceiling
                    : SurfaceMaterial.Wall;
                GrayboxUtility.SetSurface(mesh, surface);
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Exposed T-bar grid of a lay-in ceiling: main and cross tees every 600 mm, with a tile centered on the room
        /// center (so a corridor arm has a tile row in its middle), and a wall angle around the edge. The ceiling slab
        /// above is the tile surface, so the grid hangs only a centimetre below the 3 m ceiling plane and every room keeps
        /// its height. The grid is symmetric, so it lines up whichever way the generator turns the room.
        /// </summary>
        private static void BuildSuspendedCeiling(Transform shell)
        {
            var grid = GrayboxUtility.CreateGroup(SuspendedCeilingName, shell);
            const float bar = 0.024f;
            float depth = GrayboxPropsTool.CeilingGridDepth;
            float bottom = Ceiling - depth;
            int lines = Mathf.FloorToInt(Wall / CeilingTile - 0.5f);

            for (int i = -lines - 1; i <= lines; i++)
            {
                float at = (i + 0.5f) * CeilingTile;
                GrayboxUtility.CreateBoxBetween($"Tee_X_{i}", grid, new Vector3(-Wall, bottom, at - bar / 2f), new Vector3(Wall, Ceiling, at + bar / 2f), SurfaceMaterial.CeilingGrid);
                GrayboxUtility.CreateBoxBetween($"Tee_Z_{i}", grid, new Vector3(at - bar / 2f, bottom, -Wall), new Vector3(at + bar / 2f, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            }

            // The tiles: white mineral-fibre, 576 mm between the tees, with a stepped (tegular) edge; one mesh for the room.
            var tiles = new GrayboxMeshBuilder();
            float limit = Wall - bar;
            float half = CeilingTile / 2f - bar / 2f;
            const float inset = 0.012f;
            for (int i = -10; i <= 10; i++)
            {
                for (int j = -10; j <= 10; j++)
                {
                    float cx = i * CeilingTile;
                    float cz = j * CeilingTile;
                    float x0 = Mathf.Max(cx - half, -limit);
                    float x1 = Mathf.Min(cx + half, limit);
                    float z0 = Mathf.Max(cz - half, -limit);
                    float z1 = Mathf.Min(cz + half, limit);
                    if (x1 - x0 < 0.05f || z1 - z0 < 0.05f) continue;
                    tiles.AddBox(new Vector3(x0, Ceiling - 0.0080f, z0), new Vector3(x1, Ceiling, z1), false);
                    // The face sits behind the step; the sides that a wall angle covers are not stepped.
                    float fx0 = Mathf.Abs(x0 - (cx - half)) < 0.001f ? x0 + inset : x0;
                    float fx1 = Mathf.Abs(x1 - (cx + half)) < 0.001f ? x1 - inset : x1;
                    float fz0 = Mathf.Abs(z0 - (cz - half)) < 0.001f ? z0 + inset : z0;
                    float fz1 = Mathf.Abs(z1 - (cz + half)) < 0.001f ? z1 - inset : z1;
                    if (fx1 - fx0 > 0.05f && fz1 - fz0 > 0.05f)
                    {
                        tiles.AddBox(new Vector3(fx0, Ceiling - depth + 0.0008f, fz0), new Vector3(fx1, Ceiling - 0.0080f, fz1), false);
                    }
                }
            }
            tiles.Attach("Tiles", grid, SurfaceMaterial.CeilingTile);

            GrayboxUtility.CreateBoxBetween("WallAngle_N", grid, new Vector3(-Wall, bottom, Wall - bar), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_S", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(Wall, Ceiling, -Wall + bar), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_E", grid, new Vector3(Wall - bar, bottom, -Wall), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_W", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(-Wall + bar, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
        }

        // ---------------------------------------------------------------- placement

        /// <summary>Places a prop at a point of the room (room-local).</summary>
        private static Transform Put(GrayboxProp prop, Transform parent, float x, float y, float z, float yaw = 0f, string name = null) =>
            GrayboxPropsTool.Place(prop, parent, new Vector3(x, y, z), yaw, name);

        private static Transform Put(GrayboxProp prop, Transform parent, Vector3 position, float yaw = 0f, string name = null) =>
            Put(prop, parent, position.x, position.y, position.z, yaw, name);

        /// <summary>Places a ceiling prop at each (x, z); positions are grid-tile centers (multiples of 0.6 m).</summary>
        private static void PutOnCeiling(GrayboxProp prop, Transform parent, params (float x, float z)[] positions)
        {
            foreach (var (x, z) in positions)
            {
                var at = (new Vector3(x, Ceiling, z));
                GrayboxPropsTool.Place(prop, parent, at, 0f, $"{prop}_{at.x:0.0}_{at.z:0.0}");
            }
        }

        /// <summary>A 1200 mm fitting (long side along Z along Z) at each (x, z).</summary>
        private static void PutLongFitting(GrayboxProp prop, Transform parent, params (float x, float z)[] positions)
        {
            foreach (var (x, z) in positions)
            {
                var at = (new Vector3(x, Ceiling, z));
                GrayboxPropsTool.Place(prop, parent, at, 0f, $"Light_{at.x:0.0}_{at.z:0.0}");
            }
        }

        private static void Box(string name, Transform parent, Vector3 a, Vector3 b, SurfaceMaterial surface, bool collider = false) =>
            GrayboxUtility.CreateBoxBetween(name, parent, a, b, surface, collider);

        private static Vector3 Riser(Transform parent, string name, Vector3 wallPoint, Vector3 normal, float radius, SurfaceMaterial surface,
            float valveHeight = -1f) =>
            RoomInfrastructure.Riser(parent, name, wallPoint, normal, radius, surface, valveHeight);

        private static void Outlet(Transform parent, string name, Vector3 wallPoint, Vector3 normal) =>
            RoomInfrastructure.WiredOutlet(parent, name, wallPoint, normal);

        private static void Trunking(Transform parent, string name, Vector3 from, Vector3 to, Vector3 normal) =>
            RoomInfrastructure.Trunking(parent, name, from, to, normal);

        private static void Trunking(Transform parent, string name, Vector3[] path, Vector3 normal) =>
            RoomInfrastructure.Trunking(parent, name, path, normal);

        private static void PipePath(string name, Transform parent, Vector3[] path, float radius, SurfaceMaterial surface) =>
            RoomPipes.Pipe(parent, name, path, radius, surface);

        private static void Pipe(string name, Transform parent, Vector3 from, Vector3 to, float radius, SurfaceMaterial surface) =>
            RoomPipes.Pipe(parent, name, from, to, radius, surface);

        /// <summary>A cable tray between two points of the room; <paramref name="rodsAt"/> are positions along its run.</summary>
        private static void Tray(Transform parent, string name, Vector3 from, Vector3 to, SurfaceMaterial tray, SurfaceMaterial cables, float[] rodsAt)
        {
            bool alongX = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.z - from.z);
            var a = from;
            var b = to;
            bool mirroredAlongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
            var rods = rodsAt.Select(r =>
            {
                var p = (alongX ? new Vector3(r, 0f, from.z) : new Vector3(from.x, 0f, r));
                return mirroredAlongX ? p.x : p.z;
            }).ToArray();
            RoomInfrastructure.Tray(parent, name, a, b, TrayWidth, TrayBottom, tray, cables, rods);
        }

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
            var paint = new GrayboxMeshBuilder();
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
            var group = GrayboxUtility.CreateGroup(name, parent);
            paint.Attach("Paint", group, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>A painted floor line between two points of the room that differ along one axis.</summary>
        private static void Marking(Transform parent, string name, Vector3 from, Vector3 to)
        {
            const float width = 0.05f;
            var a = from;
            var b = to;
            var across = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z) ? Vector3.forward : Vector3.right;
            GrayboxUtility.CreateBoxBetween(name, parent, a - across * (width / 2f), b + across * (width / 2f) + Vector3.up * 0.002f, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>A yellow line around the floor rectangle (x0, z0)-(x1, z1) of the room.</summary>
        private static void Outline(Transform parent, string name, float x0, float z0, float x1, float z1)
        {
            Marking(parent, $"{name}_S", new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0));
            Marking(parent, $"{name}_N", new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1));
            Marking(parent, $"{name}_W", new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1));
            Marking(parent, $"{name}_E", new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1));
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
            var group = GrayboxUtility.CreateGroup(name, parent);
            var paint = new GrayboxMeshBuilder();
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

        // ---------------------------------------------------------------- corridors

        /// <summary>
        /// A corridor 3 m wide with arms where <see cref="RoomRules.DoorSides"/> says (cross, T, straight, corner or
        /// dead end), between partition blocks (technical rooms behind them); a missing arm is walled up. In the suspended
        /// ceiling, on the corridor axis: a 600 mm LED panel in the middle of the crossing and of every arm, and between
        /// them the sprinklers (1.8 and 4.8 m from the center, so they are 3.0 to 3.6 m apart all along the corridor). One
        /// smoke detector and one loudspeaker per cell, one camera in the middle of a crossing, T-junction or corner (none in straight corridors and dead ends).
        ///
        /// The services are spread over the arms, starting with the fire arm of the layout
        /// (<see cref="RoomRules.FireArmSide"/>, which the layout turns towards a room): the fire-water riser with its hose
        /// cabinet, an extinguisher and a call point stand there, by the room's entrance and off the through-corridor. The
        /// next arm has the water and gas risers and an outlet box fed from the ceiling.
        /// The cell's one ventilation grille (a large square one at the floor; no duct shows) and its light switch above
        /// it share the middle of one wall, as far from the fire kit as the cell allows: the arm opposite the fire arm, or
        /// the wall that closes it. Mirrored layouts (<see cref="RoomRules.IsMirroredCorridor"/>) mirror every arm and go
        /// round the arms the other way.
        ///
        /// The lift end (<see cref="RoomType.Elevator"/>) is a dead end whose closed north end is the landing of the
        /// passenger lift (structure from <see cref="GrayboxRoomsTool"/>, the <see cref="GrayboxProp.PassengerLift"/> on it); its
        /// grille and switch go to the middle of the west wall of the crossing instead.
        /// </summary>
        private static void DressCorridor(Transform dressing, Transform cameras, RoomType type, int layout)
        {
            int arms = RoomRules.DoorSides(type);
            bool Has(Direction side) => (arms & RoomRules.Mask(side)) != 0;
            var fireSide = RoomRules.FireArmSide(type, layout);
            bool flip = RoomRules.IsMirroredCorridor(type, layout);

            var architecture = GrayboxUtility.CreateGroup("Architecture", dressing);
            float extent = Wall - CorridorHalfWidth;
            float center = CorridorHalfWidth + extent / 2f;
            const float skirtingHeight = 0.1f;
            const float skirtingDepth = 0.015f;

            foreach (int sx in new[] { -1, 1 })
            {
                foreach (int sz in new[] { -1, 1 })
                {
                    string corner = Corner(sx, sz);
                    GrayboxUtility.CreateStandingBox($"Partition_{corner}", architecture, new Vector3(sx * center, 0f, sz * center),
                        new Vector3(extent, Ceiling, extent), SurfaceMaterial.Wall);

                    // Skirting on the partition faces that line an arm.
                    float faceX = sx * (CorridorHalfWidth - skirtingDepth / 2f);
                    float faceZ = sz * (CorridorHalfWidth - skirtingDepth / 2f);
                    if (Has(sz > 0 ? Direction.North : Direction.South))
                    {
                        GrayboxUtility.CreateStandingBox($"Skirting_{corner}_X", architecture, new Vector3(faceX, 0f, sz * center),
                            new Vector3(skirtingDepth, skirtingHeight, extent), SurfaceMaterial.Trim, false);
                    }
                    if (Has(sx > 0 ? Direction.East : Direction.West))
                    {
                        GrayboxUtility.CreateStandingBox($"Skirting_{corner}_Z", architecture, new Vector3(sx * center, 0f, faceZ),
                            new Vector3(extent, skirtingHeight, skirtingDepth), SurfaceMaterial.Trim, false);
                    }
                }
            }

            // Walled-up arms: a block between the partitions, its face closing the crossing.
            var fills = new Dictionary<Direction, Transform>();
            bool lift = type == RoomType.Elevator;
            foreach (Direction side in Enum.GetValues(typeof(Direction)))
            {
                if (Has(side)) continue;
                if (lift && side == Direction.North)
                {
                    // The landing wall closes the crossing here; skirting beside the lift door's frame.
                    float from = RoomContract.LiftDoorWidth / 2f + 0.1f;
                    foreach (int sx in new[] { -1, 1 })
                    {
                        GrayboxUtility.CreateBoxBetween(sx < 0 ? "Skirting_Lift_W" : "Skirting_Lift_E", architecture,
                            new Vector3(sx * from, 0f, RoomContract.LiftWallFace - skirtingDepth), new Vector3(sx * CorridorHalfWidth, skirtingHeight, RoomContract.LiftWallFace), SurfaceMaterial.Trim);
                    }
                    var landing = GrayboxUtility.CreateGroup("Lift", dressing);
                    GrayboxPropsTool.Place(GrayboxProp.PassengerLift, landing, new Vector3(0f, 0f, RoomContract.LiftWallFace), 180f, "PassengerLift");
                    continue;
                }
                var fill = Arm(architecture, $"Fill_{side}", side);
                GrayboxUtility.CreateStandingBox("Block", fill, new Vector3(0f, 0f, center), new Vector3(2f * CorridorHalfWidth, Ceiling, extent), SurfaceMaterial.Wall);
                GrayboxUtility.CreateStandingBox("Skirting", fill, new Vector3(0f, 0f, CorridorHalfWidth - skirtingDepth / 2f),
                    new Vector3(2f * CorridorHalfWidth, skirtingHeight, skirtingDepth), SurfaceMaterial.Trim, false);
                fills.Add(side, fill);
            }

            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            // One light per arm (3.6 m from the middle); only a straight corridor also has one in the middle, so the lights stay
            // 3.6 m apart all along it.
            if (type == RoomType.CorridorStraight)
            {
                GrayboxPropsTool.Place(GrayboxProp.LedPanel600, ceiling, new Vector3(0f, Ceiling, 0f), 0f, "Light_Center");
            }
            PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (0.6f, -0.6f));
            // Where a corner or a dead end closes the crossing on two sides, the arms' sprinklers do not reach its far
            // walls: one more on the axis.
            if (type == RoomType.CorridorCorner || type == RoomType.CorridorDeadEnd || lift)
            {
                PutOnCeiling(GrayboxProp.Sprinkler, ceiling, (0f, 0.6f));
            }
            // A dead end (and the lift end) has its arm light 3.6 m from the middle; a second one 3.0 m nearer the closed end
            // spreads the two evenly along the axis.
            if (type == RoomType.CorridorDeadEnd || lift)
            {
                PutOnCeiling(GrayboxProp.LedPanel600, ceiling, (0f, -0.6f));
            }
            // A crossing or T-junction has its camera in the middle: the smoke detector goes diagonally across from the
            // loudspeaker, right next to the camera.
            bool crossOrT = type == RoomType.Corridor || type == RoomType.CorridorT;
            if (crossOrT)
            {
                GrayboxPropsTool.Place(GrayboxProp.SmokeDetector, ceiling, new Vector3(-0.6f, Ceiling, 0.6f), 0f, "SmokeDetector");
            }
            PlaceCorridorCameras(cameras, type);

            // The arms in order from the fire arm, clockwise (anticlockwise when mirrored).
            var armList = new List<Direction>();
            for (int i = 0; i < DirectionExtensions.Count; i++)
            {
                var side = (Direction)(((int)fireSide + (flip ? -i : i) + 2 * DirectionExtensions.Count) % DirectionExtensions.Count);
                if (Has(side)) armList.Add(side);
            }
            for (int i = 0; i < armList.Count; i++)
            {
                DressCorridorArm(Arm(dressing, $"Arm_{armList[i]}", armList[i]), i, i == armList.Count - 1, flip, !crossOrT);
            }

            // The grille and the light switch: the middle of the arm opposite the fire arm (on the face the services leave
            // free), or the middle of the wall that closes it.
            var opposite = fireSide.Opposite();
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            if (lift)
            {
                PlaceGrilleAndSwitch(fills[Direction.West], 0f, CorridorHalfWidth, 180f);
            }
            else if (Has(opposite))
            {
                var arm = dressing.Find($"Arm_{opposite}");
                float x = (flip ? -1f : 1f) * CorridorHalfWidth;
                float yaw = flip ? 90f : 270f;
                PlaceGrilleAndSwitch(arm, x, CorridorArmMiddle, yaw);
            }
            else
            {
                PlaceGrilleAndSwitch(fills[opposite], 0f, CorridorHalfWidth, 180f);
            }

            void PlaceGrilleAndSwitch(Transform wall, float x, float z, float yaw)
            {
                var at = GrayboxUtility.CreateGroup("WallMiddle", wall);
                var grille = GrayboxPropsTool.Place(GrayboxProp.FloorVentGrille, at, new Vector3(x, 0f, z), yaw, "ReturnGrille");
                grille.SetParent(air, true);
                var light = GrayboxPropsTool.Place(GrayboxProp.LightSwitch, at, new Vector3(x, GrayboxPropsTool.LightSwitchHeight, z), yaw, "LightSwitch");
                light.SetParent(safety, true);
                UnityEngine.Object.DestroyImmediate(at.gameObject);
            }
        }

        /// <summary>Distance from the room center to the middle of an arm's partition face (from the crossing to the cell wall).</summary>
        private static float CorridorArmMiddle => (CorridorHalfWidth + Wall) / 2f;

        /// <summary>
        /// One corridor arm, authored as the north arm (it runs from z = 1.5 to the wall; the partition faces are at
        /// x = ±1.5) and mirrored east-west if <paramref name="flip"/>. <paramref name="index"/> counts the arms from the
        /// fire arm: the fire kit in arm 0, the water and gas risers and the outlets (all on the west face, so the east face
        /// stays free for a grille) in arm 1.
        /// </summary>
        private static void DressCorridorArm(Transform arm, int index, bool last, bool flip, bool detectorInArm)
        {
            float s = flip ? -1f : 1f;
            float face = CorridorHalfWidth;
            float westFacing = flip ? 90f : 270f;
            float eastFacing = flip ? 270f : 90f;
            var fromEastFace = Vector3.left * s;
            var fromWestFace = Vector3.right * s;

            var ceiling = GrayboxUtility.CreateGroup("Ceiling", arm);
            // A 600 panel in the middle tile row: in a 3 m corridor a 1200 panel would wash out the walls next to it.
            GrayboxPropsTool.Place(GrayboxProp.LedPanel600, ceiling, new Vector3(0f, Ceiling, 3.6f), 0f, "Light");
            foreach (float z in new[] { 1.8f, 4.8f })
            {
                GrayboxPropsTool.Place(GrayboxProp.Sprinkler, ceiling, new Vector3(0f, Ceiling, z), 0f, $"Sprinkler_{z:0.0}");
            }

            var pipes = GrayboxUtility.CreateGroup("Pipes", arm);
            var power = GrayboxUtility.CreateGroup("PowerAndData", arm);
            var safety = GrayboxUtility.CreateGroup("Safety", arm);
            if (index == 0)
            {
                // Fire water: riser clamped to the east partition, hose cabinet beside it, fed by a branch.
                var fire = RoomInfrastructure.Riser(pipes, "FireWaterRiser", new Vector3(s * face, 0f, 4.5f), fromEastFace, RoomPipes.FireRadius, SurfaceMaterial.SafetyRed);
                const float hydrantZ = 3.3f;
                GrayboxPropsTool.Place(GrayboxProp.HydrantCabinet, safety, new Vector3(s * face, 0f, hydrantZ), westFacing);
                RoomPipes.Pipe(pipes, "HydrantBranch", fire + Vector3.up * 1.25f, new Vector3(fire.x, 1.25f, hydrantZ + 0.325f), RoomPipes.FireRadius, SurfaceMaterial.SafetyRed);
                GrayboxPropsTool.Place(GrayboxProp.FireExtinguisher, safety, new Vector3(-s * face, 0f, 3.6f), eastFacing);
                GrayboxPropsTool.Place(GrayboxProp.FireCallPoint, safety, new Vector3(-s * face, GrayboxPropsTool.CallPointHeight, 4.3f), eastFacing);
                if (detectorInArm)
                {
                    GrayboxPropsTool.Place(GrayboxProp.SmokeDetector, ceiling, new Vector3(0f, Ceiling, 2.4f), 0f, "SmokeDetector");
                }
            }
            else if (index == 1)
            {
                RoomInfrastructure.Riser(pipes, "WaterRiser", new Vector3(-s * face, 0f, 4.6f), fromWestFace, RoomPipes.NarrowRadius, SurfaceMaterial.PipeWater, 1.2f);
                RoomInfrastructure.Riser(pipes, "GasRiser", new Vector3(-s * face, 0f, 2.3f), fromWestFace, RoomPipes.NarrowRadius, SurfaceMaterial.PipeGas, 1.4f);
                RoomInfrastructure.WiredOutlet(power, "Outlets", new Vector3(-s * face, 0f, 3.45f), fromWestFace);
            }
        }

        /// <summary>
        /// One ceiling dome in the middle of a crossing, a T-junction or a corner, looking along the arms it watches
        /// (both arms of a corner). Straight corridors, dead ends and the lift end have no camera.
        /// </summary>
        private static void PlaceCorridorCameras(Transform cameras, RoomType type)
        {
            float yaw;
            switch (type)
            {
                case RoomType.Corridor: yaw = 0f; break;
                case RoomType.CorridorT: yaw = 180f; break;
                case RoomType.CorridorCorner: yaw = 135f; break;
                default: return;
            }
            GrayboxPropsTool.Place(GrayboxProp.SecurityDome, cameras, new Vector3(0f, Ceiling, 0f), yaw, "SecurityDome");
        }

        /// <summary>A group turned so that content authored for the north side ends up on <paramref name="side"/>.</summary>
        private static Transform Arm(Transform parent, string name, Direction side)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            group.localRotation = Quaternion.Euler(0f, (int)side * RoomContract.RotationStepDegrees, 0f);
            return group;
        }

        // ---------------------------------------------------------------- small rooms (6 x 6 m by the south door)

        /// <summary>
        /// What every small room has, placed the same way: four sprinklers (2.4 m apart across, 3 m along the room, at most
        /// 2.2 m from a wall), one smoke detector, a ceiling dome in the corner by the door looking in, the light switch on
        /// the west wall beside the door, and the extinguisher and the call point on
        /// the east wall by the door (west if <paramref name="extinguisherWest"/>).
        /// </summary>
        private static void SmallRoomBasics(Transform dressing, Transform cameras, (float x, float z) detector, bool extinguisherWest = false, bool switchEast = false)
        {
            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            var ceiling = dressing.Find("Ceiling") ?? GrayboxUtility.CreateGroup("Ceiling", dressing);
            // At the corners of the 3 x 3 tiles around the room's middle tile (every other tile), symmetric about the center (0, -3).
            PutOnCeiling(GrayboxProp.Sprinkler, ceiling, (-1.2f, -1.8f), (1.2f, -1.8f), (-1.2f, -4.2f), (1.2f, -4.2f));
            PutOnCeiling(GrayboxProp.SmokeDetector, ceiling, detector);
            Put(GrayboxProp.SecurityDome, cameras, 2.4f, Ceiling, -5.4f, 315f, "SecurityDome");
            if (switchEast) Put(GrayboxProp.LightSwitch, safety, Side, GrayboxPropsTool.LightSwitchHeight, -5.7f, 270f, "LightSwitch");
            else Put(GrayboxProp.LightSwitch, safety, -Side, GrayboxPropsTool.LightSwitchHeight, -5.6f, 90f, "LightSwitch");
            float x = extinguisherWest ? -Side : Side;
            float yaw = extinguisherWest ? 90f : 270f;
            Put(GrayboxProp.FireExtinguisher, safety, x, 0f, -5.3f, yaw);
            Put(GrayboxProp.FireCallPoint, safety, x, GrayboxPropsTool.CallPointHeight, -5.3f, yaw);
        }

        /// <summary>
        /// The suspended ceiling of the small offices and the security office, built like the storage room's: two 1x1 (600 mm)
        /// LED panels on the axis of the air unit (the door axis), in line with the sprinkler rows, the air diffuser in the middle of the room and, diagonally beside it, one loudspeaker and the smoke detector
        /// (<see cref="SmallRoomDetector"/>). The camera sits in the right-hand corner by the door
        /// (<see cref="SmallRoomBasics"/>).
        /// </summary>
        private static void SmallRoomCeiling(Transform ceiling)
        {
            PutOnCeiling(GrayboxProp.LedPanel600, ceiling, (0f, -1.8f), (0f, -4.2f));
            PutOnCeiling(GrayboxProp.CeilingDiffuser, ceiling, (0f, -3.0f));
            PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (0.6f, -2.4f));
        }

        private static readonly (float x, float z) SmallRoomDetector = (-0.6f, -3.6f);

        /// <summary>
        /// Small office, 6 x 6 m, with the office props and still room to walk. Back (north) wall, opposite the door: the wall TV centred in the free
        /// part of the wall (between the west wall and the coffee corner) and the meeting table centred on the same axis, turned 90 degrees (its short end
        /// towards the TV, a few centimetres from the wall), with a chair on each long side; the coffee
        /// corner (kitchenette, fridge in the corner) in the north-east corner. West wall: the desk with two monitors, keyboard, mouse and a photo frame, as near
        /// the door wall as the lockers of the security office (0.95 m from it), beside the terminal in the corner. East wall, the same distance from the door wall:
        /// two filing cabinets and the printer with its socket (a few centimetres between them). The walkway from the door stays free.
        /// </summary>
        private static void DressSmallOffice(Transform dressing, Transform cameras)
        {
            var furniture = GrayboxUtility.CreateGroup("Furniture", dressing);
            var details = GrayboxUtility.CreateGroup("Details", dressing);
            var power = GrayboxUtility.CreateGroup("PowerAndData", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            float back = RoomContract.SmallRoomBackWall;

            // North-east corner: the coffee corner, the fridge in the corner.
            const float counterX = 1.15f;
            Kitchenette(furniture, details, power, new Vector3(counterX, 0f, back - 0.3f), 180f);
            Put(GrayboxProp.Fridge, furniture, 2.45f, 0f, back - 0.345f, 180f);

            // The free wall: from the west wall to where the kitchenette's cabinets start (counter 1.8 m wide, centred on counterX).
            float freeFrom = -Side;
            float freeTo = counterX - 0.9f;
            float axis = (freeFrom + freeTo) / 2f;
            Put(GrayboxProp.WallTv, details, axis, 0f, back, 180f);
            // The table (1.8 x 0.9 m) is turned 90 degrees: its short end stands 5 cm from the wall under the TV.
            const float tableGap = 0.05f;
            MeetingTable(furniture, details, new Vector3(axis, 0f, back - tableGap - 0.9f), 90f, 2);

            // West wall: the desk with two monitors, 0.95 m from the door wall (the terminal's space is in the corner, 0.15 m clear of it).
            var desk = new Vector3(-Side + 0.4f, 0f, -Side * 2f + SecurityLockerGap + 0.8f);
            PlaceDesk(furniture, details, power, desk, 90f, "W1", true, false);
            var top = desk + Vector3.up * GrayboxPropsTool.DeskHeight;
            var turn = Quaternion.Euler(0f, 90f, 0f);
            Put(GrayboxProp.PhotoFrame, details, top + turn * new Vector3(0.66f, 0f, -0.2f), 78f, "PhotoFrame_W1");
            Put(GrayboxProp.Mug, details, top + turn * new Vector3(-0.66f, 0f, 0.15f), 90f, "Mug_W1");
            DressTerminalOutlets(power, -Side, -4.97f, 90f);

            // East wall, from the same distance: two filing cabinets, a small gap, the printer and its socket.
            float start = -Side * 2f + SecurityLockerGap;
            CabinetRow(furniture, details, "E", new[] { start + 0.225f, start + 0.675f }, z => new Vector3(Side - 0.3f, 0f, z), 270f);
            // A few centimetres between the cabinets and the printer, as in a real office.
            const float printerGap = 0.04f;
            float printerZ = start + 0.9f + printerGap + 0.3f;
            Put(GrayboxProp.Printer, furniture, Side - 0.325f, 0f, printerZ, 270f);
            Put(GrayboxProp.PowerSocket, power, Side, OutletHeight, printerZ + 0.375f, 270f, "PowerSocket_Printer");

            SmallRoomCeiling(ceiling);
            SmallRoomBasics(dressing, cameras, SmallRoomDetector, false, true);
        }

        /// <summary>Distance from the door wall's inner face to the nearest edge of the security office's lockers (and of the office's cabinets and desk): 0.95 m.</summary>
        private const float SecurityLockerGap = 0.95f;

        /// <summary>
        /// Security office, 6 x 6 m: the video wall of 2 x 4 screens across the whole back wall (<see cref="GrayboxRoomsTool"/>), two
        /// desks with two monitors each facing it, the compute and JBOD rack slots on a side wall and the terminal by the
        /// door; nothing else but lockers and a bench on the east wall
        /// on the west wall (the racks and the terminal then stand on the east wall). Few lights, so the screens stay
        /// readable.
        /// </summary>
        private static void DressSecurityOffice(Transform dressing, Transform cameras)
        {
            var furniture = GrayboxUtility.CreateGroup("Furniture", dressing);
            var details = GrayboxUtility.CreateGroup("Details", dressing);
            var power = GrayboxUtility.CreateGroup("PowerAndData", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            float back = RoomContract.SmallRoomBackWall;

            // The two consoles stand 2 mm apart (their tops are 1.6 m wide).
            foreach (var (x, suffix) in new[] { (-0.801f, "1"), (0.801f, "2") })
            {
                PlaceDesk(furniture, details, power, new Vector3(x, 0f, -1.9f), 180f, $"Console{suffix}", true, true);
            }
            Put(GrayboxProp.PaperStack, details, -0.2f, GrayboxPropsTool.DeskHeight, -2.1f, 350f, "ShiftLog");

            // Power for the screens: trunking behind each column of screens, up into the ceiling.
            foreach (float x in GrayboxRoomsTool.VideoWallColumns)
            {
                Trunking(power, $"ScreenTrunking_{x:0.00}", new Vector3(x, 2.42f, back), new Vector3(x, Ceiling, back), Vector3.back);
            }

            // Outlets for the two racks, beside them on their wall.
            float rackSide = -1f;
            float rackYaw = 90f;
            Put(GrayboxProp.PowerSocket, power, rackSide * Side, OutletHeight, -3.45f, rackYaw, "PowerSocket_Racks");
            Put(GrayboxProp.DataSocket, power, rackSide * Side, OutletHeight, -3.2f, rackYaw, "DataSocket_Racks");
            DressTerminalOutlets(power, rackSide * Side, -4.97f, rackYaw);

            // The changing corner on the east wall, as near the door wall as the extinguisher corner allows (0.3 m clear of it): two lockers and the bench in front.
            foreach (float z in new[] { -3.95f, -4.55f })
            {
                Put(GrayboxProp.Locker, furniture, Side - 0.25f, 0f, z, 270f, $"Locker_{z:0.0}");
            }
            Put(GrayboxProp.LockerBench, furniture, 1.9f, 0f, -4.25f, 90f);

            SmallRoomCeiling(ceiling);
            SmallRoomBasics(dressing, cameras, SmallRoomDetector, false, true);
        }

        /// <summary>
        /// A desk with its PC (tower under the desk, one or two monitors with keyboard and mouse) and a chair, and with
        /// <paramref name="floorBox"/> a small floor box under its back, between its side panels (desks away from the
        /// walls). <paramref name="yaw"/> points from the desk to the chair: 0 puts the person on the +Z side, 90 on +X.
        /// </summary>
        private static void PlaceDesk(Transform furniture, Transform details, Transform power, Vector3 position, float yaw, string suffix, bool dual, bool floorBox)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(GrayboxProp.Desk, furniture, position, yaw, $"Desk_{suffix}");
            var screen = dual ? GrayboxProp.DualWorkstation : GrayboxProp.Workstation;
            Put(screen, details, position + turn * new Vector3(0f, GrayboxPropsTool.DeskHeight, -0.05f), yaw, $"{screen}_{suffix}");
            Put(GrayboxProp.PcTower, furniture, position + turn * new Vector3(0.55f, 0f, 0f), yaw, $"PcTower_{suffix}");
            Put(GrayboxProp.OfficeChair, furniture, position + turn * new Vector3(0f, 0f, 0.78f), yaw + 180f, $"OfficeChair_{suffix}");
            if (floorBox)
            {
                Put(GrayboxProp.FloorBox, power, position + turn * new Vector3(-0.45f, 0f, -0.2f), yaw, $"FloorBox_{suffix}");
            }
        }

        /// <summary>
        /// Four desks in two back-to-back pairs at right angles to the north wall: the pairs stand along Z at
        /// <paramref name="z0"/> and <paramref name="z1"/>, the west desk of a pair faces west, the east desk east. The
        /// pair by the wall is fed by the parapet trunking, the other by floor boxes; a photo, a mug or papers on some
        /// desks (never binders or a laptop).
        /// </summary>
        private static void DeskCluster(Transform furniture, Transform details, Transform power, float x, float z0, float z1, string name, bool[] dual)
        {
            int i = 0;
            foreach (float z in new[] { z0, z1 })
            {
                foreach (int side in new[] { -1, 1 })
                {
                    var position = new Vector3(x + side * 0.4015f, 0f, z);
                    float yaw = side < 0 ? 270f : 90f;
                    PlaceDesk(furniture, details, power, position, yaw, $"{name}{i + 1}", dual[i], z == z0);
                    var turn = Quaternion.Euler(0f, yaw, 0f);
                    var top = position + Vector3.up * GrayboxPropsTool.DeskHeight;
                    switch (i)
                    {
                        case 0:
                            Put(GrayboxProp.PhotoFrame, details, top + turn * new Vector3(-0.6f, 0f, -0.2f), yaw + 15f, $"PhotoFrame_{name}{i + 1}");
                            break;
                        case 1:
                            Put(GrayboxProp.Mug, details, top + turn * new Vector3(0.5f, 0f, 0.15f), yaw, $"Mug_{name}{i + 1}");
                            break;
                        case 2:
                            Put(GrayboxProp.PaperStack, details, top + turn * new Vector3(-0.5f, 0f, 0.1f), yaw + 8f, $"PaperStack_{name}{i + 1}");
                            break;
                        default:
                            Put(GrayboxProp.PhotoFrame, details, top + turn * new Vector3(0.6f, 0f, -0.2f), yaw - 12f, $"PhotoFrame_{name}{i + 1}");
                            break;
                    }
                    i++;
                }
            }
        }

        /// <summary>
        /// Combined power and data trunking at desk height along a north-facing wall (at <paramref name="wallZ"/>): it
        /// starts at the west wall, runs east to <paramref name="riseX"/> and turns up into the ceiling there in a mitred corner, so both ends
        /// are closed. A power and a data outlet at each of <paramref name="outlets"/>.
        /// </summary>
        private static void ParapetTrunking(Transform power, float fromX, float riseX, float wallZ, float[] outlets)
        {
            var south = new Vector3(0f, 0f, -1f);
            float front = wallZ - GrayboxPropsTool.TrunkingDepth;
            Trunking(power, "ParapetTrunking", new[]
            {
                new Vector3(fromX, ParapetHeight, wallZ), new Vector3(riseX, ParapetHeight, wallZ), new Vector3(riseX, Ceiling, wallZ),
            }, south);
            foreach (float x in outlets)
            {
                Put(GrayboxProp.PowerSocket, power, x - 0.12f, ParapetHeight, front, 180f, $"PowerSocket_Parapet_{x:0.0}");
                Put(GrayboxProp.DataSocket, power, x + 0.12f, ParapetHeight, front, 180f, $"DataSocket_Parapet_{x:0.0}");
            }
        }

        /// <summary>
        /// The kitchenette of a coffee corner against a wall (<paramref name="yaw"/> turns its front away from the wall):
        /// the closed counter with its sink and tap, the coffee machine and mugs on the worktop, and a socket above it.
        /// </summary>
        private static void Kitchenette(Transform furniture, Transform details, Transform power, Vector3 position, float yaw)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(GrayboxProp.CoffeeCounter, furniture, position, yaw);
            Put(GrayboxProp.CoffeeMachine, details, position + turn * GrayboxPropsTool.CounterMachinePosition, yaw);
            Put(GrayboxProp.Mug, details, position + turn * new Vector3(-0.09f, GrayboxPropsTool.CounterHeight, -0.245f), yaw, "Mug_Counter1");
            Put(GrayboxProp.Mug, details, position + turn * new Vector3(0.09f, GrayboxPropsTool.CounterHeight, -0.245f), yaw, "Mug_Counter2");
            Put(GrayboxProp.PowerSocket, power, position + turn * new Vector3(-0.55f, 1.15f, -0.3f), yaw, "PowerSocket_Coffee");
        }

        /// <summary>A row of filing cabinets against a wall; every other one has papers on top.</summary>
        private static void CabinetRow(Transform furniture, Transform details, string name, float[] along, Func<float, Vector3> position, float yaw)
        {
            for (int i = 0; i < along.Length; i++)
            {
                var at = position(along[i]);
                Put(GrayboxProp.FilingCabinet, furniture, at, yaw, $"FilingCabinet_{name}{i + 1}");
                if (i % 2 == 1)
                {
                    Put(GrayboxProp.PaperStack, details, at + Vector3.up * 1.32f, yaw + 80f, $"PaperStack_{name}{i + 1}");
                }
            }
        }

        /// <summary>
        /// A meeting table (1.8 x 0.9 m, long side along X turned by <paramref name="yaw"/>) with
        /// <paramref name="chairsPerSide"/> chairs along each long side only, so there is room to walk around its ends;
        /// a floor box under it, papers and a mug on it.
        /// </summary>
        private static void MeetingTable(Transform furniture, Transform details, Vector3 center, float yaw, int chairsPerSide, string name = "Meeting")
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(GrayboxProp.MeetingTable, furniture, center, yaw, $"MeetingTable_{name}");
            float spacing = chairsPerSide == 3 ? 0.6f : 0.9f;
            int chair = 1;
            for (int i = 0; i < chairsPerSide; i++)
            {
                float along = (i - (chairsPerSide - 1) / 2f) * spacing;
                Put(GrayboxProp.OfficeChair, furniture, center + turn * new Vector3(along, 0f, 0.85f), yaw + 180f, $"OfficeChair_{name}{chair++}");
                Put(GrayboxProp.OfficeChair, furniture, center + turn * new Vector3(along, 0f, -0.85f), yaw, $"OfficeChair_{name}{chair++}");
            }
            Put(GrayboxProp.PaperStack, details, center + turn * new Vector3(-0.4f, GrayboxPropsTool.DeskHeight, -0.1f), yaw + 10f, $"PaperStack_{name}");
            Put(GrayboxProp.Mug, details, center + turn * new Vector3(0.35f, GrayboxPropsTool.DeskHeight, 0.15f), yaw, $"Mug_{name}");
        }

        /// <summary>
        /// Power and data outlets on the wall beside the terminal slot (see <see cref="RoomLayouts.Terminal"/>): the power
        /// outlet at (<paramref name="x"/>, <paramref name="z"/>) on a wall facing <paramref name="yaw"/>, the data outlet
        /// 0.25 m along the wall.
        /// </summary>
        private static void DressTerminalOutlets(Transform power, float x, float z, float yaw)
        {
            var along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var at = new Vector3(x, OutletHeight, z);
            Put(GrayboxProp.PowerSocket, power, at, yaw, "PowerSocket_Terminal");
            Put(GrayboxProp.DataSocket, power, at + along * 0.25f, yaw, "DataSocket_Terminal");
        }

        private static (float x, float z)[] Lattice(float[] xs, float[] zs)
        {
            var points = new List<(float, float)>();
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    points.Add((x, z));
                }
            }
            return points.ToArray();
        }

        // ---------------------------------------------------------------- technical rooms (6 x 6 m, one door)

        /// <summary>
        /// The ceiling of the technical rooms (power room, cooling plant, storage room), the same in all three: two LED
        /// battens on the sprinkler axes (x = ±1.2 m, along the door axis, centered between the two sprinkler rows), and on the
        /// wall opposite the door two ventilation shaft grilles high up on those same axes. Nothing runs across the ceiling.
        /// The smoke detector sits in the exact middle of the room (<see cref="TechnicalDetector"/>).
        /// </summary>
        private static void TechnicalCeiling(Transform ceiling, Transform air)
        {
            foreach (float x in new[] { -1.2f, 1.2f })
            {
                Put(GrayboxProp.LedBatten, ceiling, x, Ceiling, -3.0f, 0f, $"Light_{x:0.0}_-3.0");
                Put(GrayboxProp.WallVentGrille, air, x, HighGrilleHeight, RoomContract.SmallRoomBackWall, 180f, $"ShaftGrille_{(x < 0 ? "W" : "E")}");
            }
        }

        /// <summary>The middle of a small room (room-local), where the technical rooms have their smoke detector.</summary>
        private static readonly (float x, float z) TechnicalDetector = (0f, -3.0f);

        /// <summary>
        /// Electrical switch room, 6 x 6 m, filled with what belongs there and nothing else. Two cast-resin transformers
        /// stand along the side walls in the north half (0.8 m behind them, 1.8 m between their fronts), each behind a
        /// yellow keep-out line (no floor grilles in this room; the two shaft grilles are high on the back wall). On the back wall between
        /// them the panel of kW meters for all hall sections. In the
        /// south half, 1 m in front of the transformers: two switchgear sections on the west wall and two UPS
        /// cabinets on the east wall. An exposed power tray across the room. LED battens, sprinklers, a detector, a CO2 extinguisher.
        /// </summary>
        private static void DressPowerRoom(Transform dressing, Transform cameras)
        {
            var plant = GrayboxUtility.CreateGroup("Plant", dressing);
            var power = GrayboxUtility.CreateGroup("Power", dressing);
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            var markings = GrayboxUtility.CreateGroup("Markings", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);

            const float transformerZ = -1.6f;
            Put(GrayboxProp.Transformer, plant, -1.5f, 0f, transformerZ, 90f, "Transformer_W");
            Put(GrayboxProp.Transformer, plant, 1.5f, 0f, transformerZ, 270f, "Transformer_E");
            // Yellow lines, one exact mesh: the aisle lines (1.4 m either side of the middle, from the door as in the cooling plant) run into the keep-out lines
            // 0.5 m round both enclosures (the front line along z = -3.1 m, the side line at x = 0.4 m), closed by the walls; nothing overlaps, every corner is square.
            var lines = new List<(Vector3, Vector3)>();
            foreach (int side in new[] { -1, 1 })
            {
                lines.Add((new Vector3(side * 1.4f, 0f, -2f * Side), new Vector3(side * 1.4f, 0f, -3.1f)));
                lines.Add((new Vector3(side * Side, 0f, -3.1f), new Vector3(side * 0.4f, 0f, -3.1f)));
                lines.Add((new Vector3(side * 0.4f, 0f, -3.1f), new Vector3(side * 0.4f, 0f, RoomContract.SmallRoomBackWall)));
            }
            PaintedLines(markings, "KeepOut", lines, -Side, Side, -2f * Side, RoomContract.SmallRoomBackWall);
            TechnicalCeiling(ceiling, air);

            Put(GrayboxProp.AnalogMeterPanel, plant, 0f, 0f, RoomContract.SmallRoomBackWall, 180f, "MeterPanel");

            foreach (float z in new[] { -4.0f, -4.8f })
            {
                Put(GrayboxProp.SwitchgearCabinet, plant, -Side + 0.3f, 0f, z, 90f, $"Switchgear_{z:0.0}");
            }
            // Their north edge is 0.5 m from the transformers' front line (z = -3.1), as the switchgear's and the transformers' are.
            foreach (float z in new[] { -3.9f, -4.5f })
            {
                Put(GrayboxProp.EquipmentCabinet, plant, Side - 0.4f, 0f, z, 270f, $"UpsCabinet_{z:0.0}");
            }
            

            SmallRoomBasics(dressing, cameras, TechnicalDetector);
        }

        /// <summary>
        /// Cooling plant (HVAC room, 6 x 6 m), a chilled-water plant for the data centre that fills the room except for the 2.4 m aisle
        /// from the door to the thermometer wall. Everything is piped into the two large plate heat exchangers at the back end of the
        /// side walls, and every pipe runs along the walls or rises from the floor or ceiling or into a wall, none crosses the room. On one
        /// side the pump set (two base-mounted pumps with blue motors, their discharge into the exchanger's flow line along the wall and
        /// the return line down to their suction flanges); on the other the buffer tank, the air handling unit and the expansion vessel,
        /// tapped into the second exchanger's flow and return lines. The six thermometers on the back wall run into the exchangers' end
        /// faces. The only gauges are the large ones on that wall and the pumps' thermometers. The pumps are on the east side.
        /// Nothing stands in the aisle or the door pockets, nor in the extinguisher corner.
        /// </summary>
        private static void DressHvacRoom(Transform dressing, Transform cameras)
        {
            var plant = GrayboxUtility.CreateGroup("Plant", dressing);
            var pipes = GrayboxUtility.CreateGroup("Pipes", dressing);
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            var markings = GrayboxUtility.CreateGroup("Markings", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);

            // Yellow lines 1.4 m either side of the middle (clear of the door frame) along the aisle from the door to the thermometer wall: everything between them stays clear.
            foreach (int side in new[] { -1, 1 })
            {
                Marking(markings, $"KeepClear_{(side < 0 ? "W" : "E")}", new Vector3(side * 1.4f, 0f, -2f * Side), new Vector3(side * 1.4f, 0f, RoomContract.SmallRoomBackWall));
            }

            const float s = 1f;                               // the side of the pumps: east
            float pumpSideYaw = s > 0f ? 270f : 90f;          // faces the aisle from that side
            float otherSideYaw = s > 0f ? 90f : 270f;
            float wallLine = Side - RoomPipes.WideRadius - 0.04f;
            float wallP = s * wallLine;                       // axis of the wall pipes on the pump side
            float wallO = -s * wallLine;                      // ... and on the other side
            float exchangerX = wallLine - 0.5f;
            const float laneFlow = 2.62f;
            const float laneReturn = 2.40f;
            const float flowZ = -2.00f;                       // where the exchangers' secondary flow and return pipes rise
            const float returnZ = -2.30f;
            float wide = RoomPipes.WideRadius;
            var towardsAisleP = Vector3.left * s;
            var towardsAisleO = Vector3.right * s;

            Put(GrayboxProp.CoolingMeterPanel, plant, 0f, 0f, RoomContract.SmallRoomBackWall, 180f, "MeterPanel");
            Put(s > 0f ? GrayboxProp.PlateHeatExchanger : GrayboxProp.PlateHeatExchangerMirrored, plant, s * exchangerX, 0f, -1.55f, pumpSideYaw, "HeatExchanger_A");
            Put(s > 0f ? GrayboxProp.PlateHeatExchangerMirrored : GrayboxProp.PlateHeatExchanger, plant, -s * exchangerX, 0f, -1.55f, otherSideYaw, "HeatExchanger_B");

            // Pump side: two pumps with the motors to the aisle and the suction flanges to the wall.
            float[] pumpZ = { -3.4f, -4.2f };
            float pumpX = s * 2.4f;
            float flangeX = s * (2.4f + 0.285f);
            for (int i = 0; i < 2; i++)
            {
                float z = pumpZ[i];
                Put(GrayboxProp.CirculationPump, plant, pumpX, 0f, z, s > 0f ? 180f : 0f, $"Pump_{i + 1}");
                // Discharge up to the flow lane, to the wall line (it ends on the header that runs along the wall to the exchanger).
                PipePath($"Discharge_{i + 1}", pipes, new[] { new Vector3(pumpX, 0.7f, z), new Vector3(pumpX, laneFlow, z), new Vector3(wallP, laneFlow, z) }, wide, SurfaceMaterial.PipeWater);
                RoomPipes.BallValve(pipes, $"DischargeValve_{i + 1}", new Vector3(pumpX, 1.05f, z), Vector3.up, towardsAisleP, wide);
                Put(GrayboxProp.PipeGauge, pipes, new Vector3(pumpX, 1.5f, z) + towardsAisleP * wide, pumpSideYaw, $"Thermometer_{i + 1}");
            }
            // Flow header along the wall from the exchanger to the far pump; return header back from the exchanger, down to the suctions.
            PipePath("FlowHeader_A", pipes, new[] { new Vector3(wallP, laneFlow, flowZ), new Vector3(wallP, laneFlow, pumpZ[1]) }, wide, SurfaceMaterial.PipeWater);
            PipePath("ReturnHeader_A", pipes, new[]
            {
                new Vector3(wallP, laneReturn, returnZ), new Vector3(wallP, laneReturn, pumpZ[1]), new Vector3(wallP, 0.34f, pumpZ[1]), new Vector3(flangeX, 0.34f, pumpZ[1]),
            }, wide, SurfaceMaterial.PipeWater);
            PipePath("ReturnBranch_A", pipes, new[] { new Vector3(wallP, laneReturn, pumpZ[0]), new Vector3(wallP, 0.34f, pumpZ[0]), new Vector3(flangeX, 0.34f, pumpZ[0]) }, wide, SurfaceMaterial.PipeWater);
            WallClamps("FlowClamps_A", pipes, wallP, laneFlow, flowZ, pumpZ[1], towardsAisleP);
            WallClamps("ReturnClamps_A", pipes, wallP, laneReturn, returnZ, pumpZ[1], towardsAisleP);

            // The other side: buffer tank, air handling unit and expansion vessel tapped into the second exchanger's lines.
            float tankX = -s * 2.28f;
            const float tankZ = -3.12f;
            Put(GrayboxProp.BufferTank, plant, tankX, 0f, tankZ, otherSideYaw, "BufferTank");
            var otherTurn = Quaternion.Euler(0f, otherSideYaw, 0f);
            var tankFlow = new Vector3(tankX, 0f, tankZ) + otherTurn * new Vector3(-0.12f, 0f, 0f);
            var tankReturn = new Vector3(tankX, 0f, tankZ) + otherTurn * new Vector3(0.12f, 0f, 0f);
            PipePath("TankFlow", pipes, new[] { new Vector3(tankFlow.x, 2.23f, tankFlow.z), new Vector3(tankFlow.x, laneFlow, tankFlow.z), new Vector3(wallO, laneFlow, tankFlow.z) }, wide, SurfaceMaterial.PipeWater);
            RoomPipes.BallValve(pipes, "TankFlowValve", new Vector3(tankFlow.x, 2.43f, tankFlow.z), Vector3.up, towardsAisleO, wide);
            PipePath("TankReturn", pipes, new[] { new Vector3(tankReturn.x, 2.23f, tankReturn.z), new Vector3(tankReturn.x, laneReturn, tankReturn.z), new Vector3(wallO, laneReturn, tankReturn.z) }, wide, SurfaceMaterial.PipeWater);

            const float handlerZ = -3.95f;
            float handlerX = -s * 2.48f;
            Put(GrayboxProp.AirHandlerUnit, plant, handlerX, 0f, handlerZ, otherSideYaw, "AirHandlerUnit");
            var handlerFlow = new Vector3(handlerX, 0f, handlerZ) + otherTurn * new Vector3(-0.2f, 0f, -0.23f);
            var handlerReturn = new Vector3(handlerX, 0f, handlerZ) + otherTurn * new Vector3(0.2f, 0f, -0.23f);
            PipePath("HandlerFlow", pipes, new[] { new Vector3(handlerFlow.x, 1.99f, handlerFlow.z), new Vector3(handlerFlow.x, laneFlow, handlerFlow.z), new Vector3(wallO, laneFlow, handlerFlow.z) }, wide, SurfaceMaterial.PipeWater);
            RoomPipes.BallValve(pipes, "HandlerFlowValve", new Vector3(handlerFlow.x, 2.3f, handlerFlow.z), Vector3.up, towardsAisleO, wide);
            PipePath("HandlerReturn", pipes, new[] { new Vector3(handlerReturn.x, 1.99f, handlerReturn.z), new Vector3(handlerReturn.x, laneReturn, handlerReturn.z), new Vector3(wallO, laneReturn, handlerReturn.z) }, wide, SurfaceMaterial.PipeWater);

            const float vesselZ = -4.57f;
            float vesselX = -s * 2.35f;
            Put(GrayboxProp.ExpansionVessel, plant, vesselX, 0f, vesselZ, otherSideYaw, "ExpansionVessel");
            var nozzle = new Vector3(vesselX, 0.5f, vesselZ) + otherTurn * new Vector3(0f, 0f, -0.28f);
            PipePath("VesselReturn", pipes, new[] { new Vector3(wallO, laneReturn, vesselZ), new Vector3(wallO, 0.5f, vesselZ), new Vector3(nozzle.x, 0.5f, vesselZ) }, wide, SurfaceMaterial.PipeWater);

            // Flow and return headers along the wall on the other side.
            PipePath("FlowHeader_B", pipes, new[] { new Vector3(wallO, laneFlow, flowZ), new Vector3(wallO, laneFlow, handlerFlow.z) }, wide, SurfaceMaterial.PipeWater);
            PipePath("ReturnHeader_B", pipes, new[] { new Vector3(wallO, laneReturn, returnZ), new Vector3(wallO, laneReturn, vesselZ) }, wide, SurfaceMaterial.PipeWater);
            WallClamps("FlowClamps_B", pipes, wallO, laneFlow, flowZ, handlerFlow.z, towardsAisleO);
            WallClamps("ReturnClamps_B", pipes, wallO, laneReturn, returnZ, vesselZ, towardsAisleO);

            TechnicalCeiling(ceiling, air);
            SmallRoomBasics(dressing, cameras, TechnicalDetector);
        }

        /// <summary>Blue pipe clamps every metre along a horizontal run on the wall (x = <paramref name="wallX"/>, between two z), at height <paramref name="y"/>.</summary>
        private static void WallClamps(string name, Transform parent, float wallX, float y, float fromZ, float toZ, Vector3 normal)
        {
            var strap = new GrayboxMeshBuilder();
            var screws = new GrayboxMeshBuilder();
            float direction = Mathf.Sign(toZ - fromZ);
            for (float z = fromZ + direction * 0.5f; direction * (toZ - z) > 0.3f; z += direction * 1.0f)
            {
                RoomPipes.Clamp(strap, screws, new Vector3(wallX, y, z), Vector3.forward, normal, RoomPipes.WideRadius, 0.04f);
            }
            strap.Attach($"{name}_Clamps", parent, SurfaceMaterial.SupportBlue);
            screws.Attach($"{name}_Screws", parent, SurfaceMaterial.Chrome);
        }

        /// <summary>
        /// Storage room, 6 x 6 m, filled with galvanised shelving that carries cardboard boxes, every shelf
        /// reachable from an aisle at least 0.9 m wide: a row along the back wall, a row along the west wall, a
        /// back-to-back double row in the middle (along Z) and one unit on the east wall.
        /// The east wall has the landing of the goods lift (only looks: its solid doors never open) with yellow keep-clear
        /// hatching on the floor in front.
        /// Two shaft grilles high on the back wall, two LED battens, sprinklers, a detector in the middle, an extinguisher.
        /// </summary>
        private static void DressStorageRoom(Transform dressing, Transform cameras)
        {
            var shelving = GrayboxUtility.CreateGroup("Shelving", dressing);
            var details = GrayboxUtility.CreateGroup("Details", dressing);
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            var markings = GrayboxUtility.CreateGroup("Markings", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            const float half = 0.2f;
            float back = RoomContract.SmallRoomBackWall;

            int index = 0;
            void Shelf(float x, float z, float yaw)
            {
                ShelfWithItems(shelving, details, new Vector3(x, 0f, z), yaw, index++);
            }

            foreach (float x in new[] { -2f, -1f, 0f, 1f, 2f })
            {
                Shelf(x, back - half, 180f);
            }
            foreach (float z in new[] { -0.9f, -1.9f, -2.9f, -3.9f, -4.9f })
            {
                Shelf(-Side + half, z, 90f);
            }
            Shelf(Side - half, -0.9f, 270f);
            // The double row is centred in the free space between the west row and the start of the keep-clear hatching.
            float rowX = (-Side + 2f * half + FreightHatchStartX) / 2f;
            foreach (float z in new[] { -1.9f, -2.9f })
            {
                Shelf(rowX - half, z, 270f);
                Shelf(rowX + half, z, 90f);
            }
            

            // The goods lift on the east wall and the space in front of it that must stay clear for loading.
            var lift = GrayboxUtility.CreateGroup("FreightLift", dressing);
            Put(GrayboxProp.FreightLift, lift, Side, 0f, FreightLiftZ, 270f, "FreightLift");
            float hatch = GrayboxPropsTool.FreightDoorWidth / 2f + 0.15f;
            Hatching(markings, "KeepClear_FreightLift", FreightHatchStartX, FreightLiftZ - hatch, Side, FreightLiftZ + hatch);

            TechnicalCeiling(ceiling, air);
            SmallRoomBasics(dressing, cameras, TechnicalDetector);
        }

        /// <summary>Room-local Z of the middle of the goods lift door in the storage room (on the east wall).</summary>
        private const float FreightLiftZ = -3.55f;

        /// <summary>Room-local X where the goods lift's keep-clear hatching starts (its edge furthest from the wall).</summary>
        private const float FreightHatchStartX = 1.6f;

        /// <summary>
        /// A galvanised shelving unit (1 m wide) with cardboard boxes on its boards: every board carries one or two of the four box
        /// kinds, chosen by a fixed pattern from the unit's index so neighbouring units differ.
        /// </summary>
        private static void ShelfWithItems(Transform shelving, Transform details, Vector3 position, float yaw, int index)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(GrayboxProp.Shelf, shelving, position, yaw, $"Shelf_{index + 1}");
            var boards = GrayboxPropsTool.ShelfBoardTops;
            var kinds = new[] { GrayboxProp.BoxCartonCube, GrayboxProp.BoxLidFlat, GrayboxProp.BoxLidCube, GrayboxProp.BoxCartonFlat };
            for (int board = 0; board < boards.Length; board++)
            {
                // Two boxes or one box (left, middle or right), depending on unit and board.
                int pattern = (index * 3 + board * 5) % 6;
                if (board == 0 && index % 2 == 0)
                {
                    Put(kinds[(index + board) % 4], details, position + turn * new Vector3(-0.25f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_A");
                    Put(kinds[(index + board + 1) % 4], details, position + turn * new Vector3(0.27f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_B");
                    continue;
                }
                // x of the first and the second box: two boxes side by side, or just one somewhere along the board.
                float[] first = { -0.24f, -0.24f, -0.08f, -0.08f, 0.27f, 0.0f };
                Put(kinds[(index + board * 2 + pattern) % 4], details, position + turn * new Vector3(first[pattern], boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_A");
                if (pattern == 0 || pattern == 2)
                {
                    Put(kinds[(index + board + pattern + 1) % 4], details, position + turn * new Vector3(0.27f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_B");
                }
                else if (pattern == 4)
                {
                    Put(kinds[(index + board + 3) % 4], details, position + turn * new Vector3(-0.2f, boards[board], 0f), yaw, $"Box_{index + 1}_{board + 1}_B");
                }
            }
        }

        private static string Corner(int sx, int sz) => $"{(sz > 0 ? "N" : "S")}{(sx > 0 ? "E" : "W")}";
    }
}
