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
    /// crossing, none in the meeting room. Every room has a light switch beside its door. The hall
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
    /// Each room type has two designs; layouts 3 and 4 are their mirror images (<see cref="LayoutMirror"/>). The
    /// dressers below are written for the design and place everything through <see cref="Put"/> and friends, which
    /// apply the mirror.
    ///
    /// Dress Graybox Rooms only adds what is missing, so hand edits survive. Rebuild Graybox Rooms throws the generated
    /// logic, dressing, props and material settings away and builds them again.
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

        private const float EmergencyLightHeight = 2.45f;

        /// <summary>Center height of the small grilles under a 3 m ceiling (<see cref="GrayboxPropsTool.GrilleGap"/> below it).</summary>
        private static float HighGrilleHeight => GrayboxPropsTool.HighGrilleCenter(RoomContract.CeilingHeight);

        private delegate void Dresser(Transform dressing, Transform cameras, int design);

        private static readonly Dictionary<RoomType, Dresser> Dressers = new Dictionary<RoomType, Dresser>
        {
            { RoomType.Corridor, (d, c, _) => DressCorridor(d, c, RoomType.Corridor, layoutIndex) },
            { RoomType.CorridorStraight, (d, c, _) => DressCorridor(d, c, RoomType.CorridorStraight, layoutIndex) },
            { RoomType.CorridorT, (d, c, _) => DressCorridor(d, c, RoomType.CorridorT, layoutIndex) },
            { RoomType.CorridorCorner, (d, c, _) => DressCorridor(d, c, RoomType.CorridorCorner, layoutIndex) },
            { RoomType.CorridorDeadEnd, (d, c, _) => DressCorridor(d, c, RoomType.CorridorDeadEnd, layoutIndex) },
            { RoomType.Office, DressOffice },
            { RoomType.SmallOffice, DressSmallOffice },
            { RoomType.SecurityOffice, DressSecurityOffice },
            { RoomType.PowerRoom, DressPowerRoom },
            { RoomType.HvacRoom, DressHvacRoom },
            { RoomType.StorageRoom, DressStorageRoom },
            { RoomType.Elevator, (d, c, _) => DressCorridor(d, c, RoomType.Elevator, layoutIndex) },
        };

        /// <summary>Technical rooms: no suspended ceiling, epoxy floor, services exposed.</summary>
        public static bool IsTechnicalRoom(RoomType type) =>
            type == RoomType.PowerRoom || type == RoomType.HvacRoom || type == RoomType.StorageRoom;

        private static float Wall => RoomContract.InteriorHalfSize;

        /// <summary>Inner face of a small room's side walls.</summary>
        private static float Side => RoomContract.SmallRoomHalfWidth;

        private static float Ceiling => RoomContract.CeilingHeight;

        /// <summary>The mirror of the layout being dressed; every placement below goes through it.</summary>
        private static LayoutMirror mirror = LayoutMirror.None;

        /// <summary>The layout being dressed (corridor layouts differ in their fire arm, see <see cref="RoomRules.FireArmSide"/>).</summary>
        private static int layoutIndex;

        [MenuItem("Tools/Cold Shutdown/Dress Graybox Rooms")]
        public static void DressGrayboxRooms()
        {
            GrayboxPropsTool.CreateGrayboxProps();
            DressAll(false);
        }

        [MenuItem("Tools/Cold Shutdown/Rebuild Graybox Rooms")]
        public static void RebuildGrayboxRooms()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Cold Shutdown",
                    "Rebuild all graybox props, surface materials, door frames, suspended ceilings, room logic (slots, walls of " +
                    "small rooms, video wall) and Dressing groups of every room layout? Hand edits in them are lost.", "Rebuild", "Cancel"))
            {
                return;
            }
            RebuildAll();
        }

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
                    ConfigureShell(root, technical) | DressRoom(root, dress, RoomLayouts.Mirror(type, layout), layout, rebuild));
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
                Debug.LogError($"{path} not found. Run Create Room Shell and Create Graybox Rooms first.");
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
        private static bool DressRoom(GameObject root, Dresser dress, LayoutMirror layoutMirror, int layout, bool rebuild)
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

            mirror = layoutMirror;
            layoutIndex = layout;
            try
            {
                var dressing = GrayboxUtility.CreateGroup(DressingName, root.transform);
                var cameras = GrayboxUtility.CreateGroup(SecurityCamerasName, root.transform);
                dress(dressing, cameras, RoomLayouts.Design(layout));
            }
            finally
            {
                mirror = LayoutMirror.None;
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

            GrayboxUtility.CreateBoxBetween("WallAngle_N", grid, new Vector3(-Wall, bottom, Wall - bar), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_S", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(Wall, Ceiling, -Wall + bar), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_E", grid, new Vector3(Wall - bar, bottom, -Wall), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            GrayboxUtility.CreateBoxBetween("WallAngle_W", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(-Wall + bar, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
        }

        // ---------------------------------------------------------------- placement through the layout mirror

        /// <summary>Places a prop at a point of the design (room-local), mirrored for the layout.</summary>
        private static Transform Put(GrayboxProp prop, Transform parent, float x, float y, float z, float yaw = 0f, string name = null) =>
            GrayboxPropsTool.Place(prop, parent, mirror.Point(new Vector3(x, y, z)), mirror.Yaw(yaw), name);

        private static Transform Put(GrayboxProp prop, Transform parent, Vector3 position, float yaw = 0f, string name = null) =>
            Put(prop, parent, position.x, position.y, position.z, yaw, name);

        /// <summary>Places a ceiling prop at each (x, z); positions are grid-tile centers (multiples of 0.6 m).</summary>
        private static void PutOnCeiling(GrayboxProp prop, Transform parent, params (float x, float z)[] positions)
        {
            foreach (var (x, z) in positions)
            {
                var at = mirror.Point(new Vector3(x, Ceiling, z));
                GrayboxPropsTool.Place(prop, parent, at, 0f, $"{prop}_{at.x:0.0}_{at.z:0.0}");
            }
        }

        /// <summary>A 1200 mm fitting (long side along Z in the design) at each (x, z).</summary>
        private static void PutLongFitting(GrayboxProp prop, Transform parent, params (float x, float z)[] positions)
        {
            foreach (var (x, z) in positions)
            {
                var at = mirror.Point(new Vector3(x, Ceiling, z));
                GrayboxPropsTool.Place(prop, parent, at, mirror.Yaw(0f), $"Light_{at.x:0.0}_{at.z:0.0}");
            }
        }

        private static void Box(string name, Transform parent, Vector3 a, Vector3 b, SurfaceMaterial surface, bool collider = false) =>
            GrayboxUtility.CreateBoxBetween(name, parent, mirror.Point(a), mirror.Point(b), surface, collider);

        private static Vector3 Riser(Transform parent, string name, Vector3 wallPoint, Vector3 normal, float radius, SurfaceMaterial surface,
            float valveHeight = -1f, SurfaceMaterial handle = SurfaceMaterial.Metal) =>
            RoomInfrastructure.Riser(parent, name, mirror.Point(wallPoint), mirror.Point(normal), radius, surface, valveHeight, handle);

        private static void Outlet(Transform parent, string name, Vector3 wallPoint, Vector3 normal) =>
            RoomInfrastructure.WiredOutlet(parent, name, mirror.Point(wallPoint), mirror.Point(normal));

        private static void Trunking(Transform parent, string name, Vector3 from, Vector3 to, Vector3 normal) =>
            RoomInfrastructure.Trunking(parent, name, mirror.Point(from), mirror.Point(to), mirror.Point(normal));

        private static void Pipe(string name, Transform parent, Vector3 from, Vector3 to, float radius, SurfaceMaterial surface) =>
            GrayboxUtility.CreatePipe(name, parent, mirror.Point(from), mirror.Point(to), radius, surface);

        /// <summary>A cable tray between two points of the design; <paramref name="rodsAt"/> are positions along its run.</summary>
        private static void Tray(Transform parent, string name, Vector3 from, Vector3 to, SurfaceMaterial tray, SurfaceMaterial cables, float[] rodsAt)
        {
            bool alongX = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.z - from.z);
            var a = mirror.Point(from);
            var b = mirror.Point(to);
            bool mirroredAlongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
            var rods = rodsAt.Select(r =>
            {
                var p = mirror.Point(alongX ? new Vector3(r, 0f, from.z) : new Vector3(from.x, 0f, r));
                return mirroredAlongX ? p.x : p.z;
            }).ToArray();
            RoomInfrastructure.Tray(parent, name, a, b, TrayWidth, TrayBottom, tray, cables, rods);
        }

        /// <summary>A painted floor line between two points of the design that differ along one axis.</summary>
        private static void Marking(Transform parent, string name, Vector3 from, Vector3 to)
        {
            const float width = 0.05f;
            var a = mirror.Point(from);
            var b = mirror.Point(to);
            var across = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z) ? Vector3.forward : Vector3.right;
            GrayboxUtility.CreateBoxBetween(name, parent, a - across * (width / 2f), b + across * (width / 2f) + Vector3.up * 0.002f, SurfaceMaterial.SafetyYellow);
        }

        /// <summary>A yellow line around the floor rectangle (x0, z0)-(x1, z1) of the design.</summary>
        private static void Outline(Transform parent, string name, float x0, float z0, float x1, float z1)
        {
            Marking(parent, $"{name}_S", new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0));
            Marking(parent, $"{name}_N", new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1));
            Marking(parent, $"{name}_W", new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1));
            Marking(parent, $"{name}_E", new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1));
        }

        /// <summary>
        /// Yellow keep-clear hatching over the floor rectangle (x0, z0)-(x1, z1) of the design: an outline and diagonal
        /// stripes 0.4 m apart, the space that must never be blocked (in front of the storage room's gate).
        /// </summary>
        private static void Hatching(Transform parent, string name, float x0, float z0, float x1, float z1)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            Outline(group, "Edge", x0, z0, x1, z1);
            var a = mirror.Point(new Vector3(x0, 0f, z0));
            var b = mirror.Point(new Vector3(x1, 0f, z1));
            var min = Vector3.Min(a, b);
            var max = Vector3.Max(a, b);
            float width = max.x - min.x;
            float depth = max.z - min.z;
            const float step = 0.4f;
            int index = 0;
            // Stripes at 45 degrees, each clipped to the rectangle by its length: from the left edge or the bottom edge.
            for (float start = -width + step / 2f; start < depth; start += step)
            {
                // A stripe is the line z - min.z = (x - min.x) + start.
                float xFrom = Mathf.Max(0f, -start);
                float xTo = Mathf.Min(width, depth - start);
                if (xTo - xFrom < 0.15f) continue;
                float length = (xTo - xFrom) * Mathf.Sqrt(2f);
                var center = new Vector3(min.x + (xFrom + xTo) / 2f, 0.001f, min.z + (xFrom + xTo) / 2f + start);
                var stripe = GrayboxUtility.CreateBox($"Stripe_{index++}", group, center, new Vector3(0.1f, 0.002f, length - 0.1f), SurfaceMaterial.SafetyYellow, false);
                stripe.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
        }

        // ---------------------------------------------------------------- corridors

        /// <summary>
        /// A corridor 3 m wide with arms where <see cref="RoomRules.DoorSides"/> says (cross, T, straight, corner or
        /// dead end), between partition blocks (technical rooms behind them); a missing arm is walled up. In the suspended
        /// ceiling, on the corridor axis: a 600 mm LED panel in the middle of the crossing and of every arm, and between
        /// them the sprinklers (1.8 and 4.8 m from the center, so they are 3.0 to 3.6 m apart all along the corridor). One
        /// smoke detector and one loudspeaker per cell, cameras by the central light (two at a T-junction or a crossing,
        /// one elsewhere).
        ///
        /// The services are spread over the arms, starting with the fire arm of the layout
        /// (<see cref="RoomRules.FireArmSide"/>, which the layout turns towards a room): the fire-water riser with its hose
        /// cabinet, an extinguisher and a call point stand there, by the room's entrance and off the through-corridor. The
        /// next arm has the water and gas risers and an outlet box fed from the ceiling, the last one the emergency light.
        /// The cell's one ventilation grille (a large square one at the floor; no duct shows) and its light switch above
        /// it share the middle of one wall, as far from the fire kit as the cell allows: the arm opposite the fire arm, or
        /// the wall that closes it. Mirrored layouts (<see cref="RoomRules.IsMirroredCorridor"/>) mirror every arm and go
        /// round the arms the other way.
        ///
        /// The lift end (<see cref="RoomType.Elevator"/>) is a dead end whose closed north end is the landing of the
        /// passenger lift (structure from Create Graybox Rooms, the <see cref="GrayboxProp.PassengerLift"/> on it); its
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
            GrayboxPropsTool.Place(GrayboxProp.LedPanel600, ceiling, new Vector3(0f, Ceiling, 0f), 0f, "Light_Center");
            PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (0.6f, -0.6f));
            // Where a corner or a dead end closes the crossing on two sides, the arms' sprinklers do not reach its far
            // walls: one more on the axis.
            if (type == RoomType.CorridorCorner || type == RoomType.CorridorDeadEnd || lift)
            {
                PutOnCeiling(GrayboxProp.Sprinkler, ceiling, (0f, 0.6f));
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
                DressCorridorArm(Arm(dressing, $"Arm_{armList[i]}", armList[i]), i, i == armList.Count - 1, flip);
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
        /// stays free for a grille) in arm 1; the last arm gets the emergency light.
        /// </summary>
        private static void DressCorridorArm(Transform arm, int index, bool last, bool flip)
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
                var fire = RoomInfrastructure.Riser(pipes, "FireWaterRiser", new Vector3(s * face, 0f, 4.5f), fromEastFace, 0.045f, SurfaceMaterial.SafetyRed);
                const float hydrantZ = 3.3f;
                GrayboxPropsTool.Place(GrayboxProp.HydrantCabinet, safety, new Vector3(s * face, 0f, hydrantZ), westFacing);
                GrayboxUtility.CreatePipe("HydrantBranch", pipes, fire + Vector3.up * 1.25f, new Vector3(fire.x, 1.25f, hydrantZ + 0.325f), 0.025f, SurfaceMaterial.SafetyRed);
                GrayboxPropsTool.Place(GrayboxProp.FireExtinguisher, safety, new Vector3(-s * face, 0f, 3.6f), eastFacing);
                GrayboxPropsTool.Place(GrayboxProp.FireCallPoint, safety, new Vector3(-s * face, GrayboxPropsTool.CallPointHeight, 4.3f), eastFacing);
                GrayboxPropsTool.Place(GrayboxProp.SmokeDetector, ceiling, new Vector3(0f, Ceiling, 2.4f), 0f, "SmokeDetector");
            }
            else if (index == 1)
            {
                RoomInfrastructure.Riser(pipes, "WaterRiser", new Vector3(-s * face, 0f, 4.6f), fromWestFace, 0.025f, SurfaceMaterial.PipeWater, 1.2f, SurfaceMaterial.PipeWater);
                RoomInfrastructure.Riser(pipes, "GasRiser", new Vector3(-s * face, 0f, 2.3f), fromWestFace, 0.02f, SurfaceMaterial.PipeGas, 1.4f, SurfaceMaterial.PipeGas);
                RoomInfrastructure.WiredOutlet(power, "Outlets", new Vector3(-s * face, 0f, 3.45f), fromWestFace);
            }
            if (last)
            {
                GrayboxPropsTool.Place(GrayboxProp.EmergencyLight, safety, new Vector3(-s * face, EmergencyLightHeight, 5.2f), eastFacing);
            }
        }

        /// <summary>
        /// Ceiling domes by the central light, on tile centers: two at a crossing or a T-junction (each looking along two
        /// arms), one in every other corridor shape.
        /// </summary>
        private static void PlaceCorridorCameras(Transform cameras, RoomType type)
        {
            void Dome(float x, float z, float yaw, string name) =>
                GrayboxPropsTool.Place(GrayboxProp.SecurityDome, cameras, new Vector3(x, Ceiling, z), yaw, name);

            switch (type)
            {
                case RoomType.Corridor:
                    Dome(0.6f, 0.6f, 225f, "SecurityDome_NE");
                    Dome(-0.6f, -0.6f, 45f, "SecurityDome_SW");
                    break;
                case RoomType.CorridorT:
                    Dome(-0.6f, 0.6f, 135f, "SecurityDome_NW");
                    Dome(0.6f, 0.6f, 225f, "SecurityDome_NE");
                    break;
                case RoomType.CorridorStraight:
                    Dome(0.6f, 0.6f, 180f, "SecurityDome");
                    break;
                default:
                    Dome(-0.6f, 0.6f, type == RoomType.CorridorCorner ? 135f : 180f, "SecurityDome");
                    break;
            }
        }

        /// <summary>A group turned so that content authored for the north side ends up on <paramref name="side"/>.</summary>
        private static Transform Arm(Transform parent, string name, Direction side)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            group.localRotation = Quaternion.Euler(0f, (int)side * RoomContract.RotationStepDegrees, 0f);
            return group;
        }

        // ---------------------------------------------------------------- office (big room, doors south and west)

        /// <summary>
        /// Full-size office, doors on its south and west sides (the walkways meet in the middle). Two designs; layouts 3
        /// and 4 are mirrored across the diagonal, which swaps the two door sides.
        /// Design 1: a glazed meeting room in the north-east (table for six, wall TV and whiteboard at the same height);
        /// a cluster of four desks at right angles to the north wall on the parapet trunking with a printer and filing
        /// cabinets beside it; the coffee corner (kitchenette, fridge, standing table) on the east wall; two desks back to
        /// back in the south-east with filing cabinets on the south wall; a huddle table, the terminal and a printer in
        /// the south-west.
        /// Design 2: an open meeting area in the north-west (table for six, TV on the north wall, whiteboard on the west
        /// wall); the coffee corner on the north wall in the north-east with filing cabinets on the east wall; a bench of
        /// four desks in the south-east and a touchdown desk on the east wall; the terminal, a printer and filing cabinets
        /// in the south-west.
        /// </summary>
        private static void DressOffice(Transform dressing, Transform cameras, int design)
        {
            var furniture = GrayboxUtility.CreateGroup("Furniture", dressing);
            var details = GrayboxUtility.CreateGroup("Details", dressing);
            var power = GrayboxUtility.CreateGroup("PowerAndData", dressing);
            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);

            if (design == 0)
            {
                // NE: glazed meeting room on the grid lines at 1.5 m, its entrance on the west side by the office.
                const float glass = 1.5f;
                var meeting = GrayboxUtility.CreateGroup("MeetingRoom", furniture);
                GlassWall(meeting, "Glass_W", new Vector3(glass, 0f, glass + 1.0f), new Vector3(glass, 0f, Wall));
                GlassWall(meeting, "Glass_S", new Vector3(glass, 0f, glass), new Vector3(Wall, 0f, glass));
                MeetingTable(meeting, details, new Vector3(3.65f, 0f, 3.65f), 0f, 3);
                Put(GrayboxProp.WallTv, meeting, Wall, 0f, 3.65f, 270f);
                Put(GrayboxProp.Whiteboard, meeting, 3.65f, 0f, Wall, 180f);
                Put(GrayboxProp.PowerSocket, power, Wall, OutletHeight, 2.6f, 270f, "PowerSocket_Meeting");
                Put(GrayboxProp.DataSocket, power, Wall, OutletHeight, 2.85f, 270f, "DataSocket_Meeting");

                // NW: four desks at right angles to the north wall, fed by the parapet trunking; printer and cabinets east of them.
                DeskCluster(furniture, details, power, -3.0f, 3.2f, 4.8f, "NW", new[] { false, true, true, false });
                ParapetTrunking(power, -Wall, -1.4f, Wall, new[] { -3.6f, -2.4f });
                Put(GrayboxProp.Printer, furniture, 0f, 0f, Wall - 0.325f, 180f, "Printer_N");
                Put(GrayboxProp.PowerSocket, power, 0.5f, OutletHeight, Wall, 180f, "PowerSocket_Printer_N");
                CabinetRow(furniture, details, "N", new[] { 0.6f, 1.05f }, x => new Vector3(x, 0f, Wall - 0.3f), 180f);

                // East: the coffee corner, a standing table in front of it.
                Kitchenette(furniture, details, power, new Vector3(Wall - 0.3f, 0f, -0.2f), 270f);
                Put(GrayboxProp.Fridge, furniture, Wall - 0.345f, 0f, 1.05f, 270f);
                StandingTable(furniture, details, new Vector3(3.6f, 0f, 0f));

                // SE: two desks back to back, filing cabinets on the south wall.
                PlaceDesk(furniture, details, power, new Vector3(3.9f, 0f, -2.4f), 0f, "SE1", true, true);
                PlaceDesk(furniture, details, power, new Vector3(3.9f, 0f, -3.2f), 180f, "SE2", false, true);
                CabinetRow(furniture, details, "S", new[] { 3.5f, 3.95f, 4.4f, 4.85f, 5.3f }, x => new Vector3(x, 0f, -Wall + 0.3f), 0f);

                // SW: a huddle table for four by the terminal, a printer on the west wall.
                DressTerminalOutlets(power, -4.55f, -Wall, 0f);
                MeetingTable(furniture, details, new Vector3(-3.6f, 0f, -2.9f), 0f, 2, "Huddle");
                Put(GrayboxProp.Printer, furniture, -Wall + 0.325f, 0f, -4.4f, 90f, "Printer_W");
                Put(GrayboxProp.PowerSocket, power, -Wall, OutletHeight, -3.9f, 90f, "PowerSocket_Printer_W");

                Put(GrayboxProp.FireExtinguisher, safety, -Wall, 0f, -3.2f, 90f);
                Put(GrayboxProp.FireCallPoint, safety, -Wall, GrayboxPropsTool.CallPointHeight, 3.3f, 90f);

                PutLongFitting(GrayboxProp.LedPanel1200, ceiling, (3.6f, 3.9f), (-3.0f, 3.3f), (-3.0f, 4.5f), (0f, 3.3f), (3.6f, -0.3f), (3.6f, -2.7f), (-3.6f, -2.7f));
                PutOnCeiling(GrayboxProp.CeilingDiffuser, ceiling, (-1.8f, 4.8f), (2.4f, 4.8f), (2.4f, -1.8f), (-2.4f, -1.8f));
                PutOnCeiling(GrayboxProp.SmokeDetector, ceiling, (-1.2f, 2.4f), (3.0f, 2.4f), (1.8f, -2.4f));
                PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (-4.2f, 2.4f), (4.8f, 2.4f), (2.4f, -4.8f), (-2.4f, -4.8f));
                Put(GrayboxProp.SecurityDome, cameras, -5.4f, Ceiling, 5.4f, 135f, "SecurityDome_NW");
                Put(GrayboxProp.SecurityDome, cameras, 5.4f, Ceiling, -5.4f, 315f, "SecurityDome_SE");
            }
            else
            {
                // NW: an open meeting area, the TV on the north wall and the whiteboard on the west wall at the same height.
                MeetingTable(furniture, details, new Vector3(-3.0f, 0f, 3.7f), 0f, 3);
                Put(GrayboxProp.WallTv, furniture, -3.0f, 0f, Wall, 180f);
                Put(GrayboxProp.Whiteboard, furniture, -Wall, 0f, 3.7f, 90f);
                Put(GrayboxProp.PowerSocket, power, -2.0f, OutletHeight, Wall, 180f, "PowerSocket_Meeting");
                Put(GrayboxProp.DataSocket, power, -1.75f, OutletHeight, Wall, 180f, "DataSocket_Meeting");

                // NE: the coffee corner on the north wall, a standing table, filing cabinets on the east wall.
                Kitchenette(furniture, details, power, new Vector3(3.1f, 0f, Wall - 0.3f), 180f);
                Put(GrayboxProp.Fridge, furniture, 4.45f, 0f, Wall - 0.345f, 180f);
                StandingTable(furniture, details, new Vector3(3.1f, 0f, 3.6f));
                CabinetRow(furniture, details, "E", new[] { 2.2f, 2.65f, 3.1f, 3.55f }, z => new Vector3(Wall - 0.3f, 0f, z), 270f);

                // SE: a bench of four desks (two pairs back to back), a touchdown desk on the east wall.
                foreach (var (x, suffix) in new[] { (2.6f, "1"), (4.2f, "2") })
                {
                    PlaceDesk(furniture, details, power, new Vector3(x, 0f, -2.2f), 0f, $"Bench{suffix}N", suffix == "1", true);
                    PlaceDesk(furniture, details, power, new Vector3(x, 0f, -3.0f), 180f, $"Bench{suffix}S", suffix == "2", true);
                }
                PlaceDesk(furniture, details, power, new Vector3(Wall - 0.4f, 0f, 0.4f), 270f, "Touchdown", false, false);
                Put(GrayboxProp.PowerSocket, power, Wall, OutletHeight, -0.6f, 270f, "PowerSocket_Touchdown");
                Put(GrayboxProp.DataSocket, power, Wall, OutletHeight, -0.85f, 270f, "DataSocket_Touchdown");

                // SW: the terminal, a printer on the south wall, filing cabinets on the west wall.
                DressTerminalOutlets(power, -4.55f, -Wall, 0f);
                Put(GrayboxProp.Printer, furniture, -5.0f, 0f, -Wall + 0.325f, 0f, "Printer_S");
                Put(GrayboxProp.PowerSocket, power, -4.5f, OutletHeight, -Wall, 0f, "PowerSocket_Printer_S");
                CabinetRow(furniture, details, "W", new[] { -3.4f, -3.85f }, z => new Vector3(-Wall + 0.3f, 0f, z), 90f);

                Put(GrayboxProp.FireExtinguisher, safety, -3.0f, 0f, -Wall, 0f);
                Put(GrayboxProp.FireCallPoint, safety, -Wall, GrayboxPropsTool.CallPointHeight, -3.0f, 90f);

                PutLongFitting(GrayboxProp.LedPanel1200, ceiling, (-3.0f, 3.9f), (3.0f, 3.9f), (3.6f, -2.7f), (-3.6f, -3.9f), (4.8f, 0.3f));
                PutOnCeiling(GrayboxProp.LedPanel600, ceiling, (0f, 2.4f));
                PutOnCeiling(GrayboxProp.CeilingDiffuser, ceiling, (-1.2f, 4.8f), (1.8f, 4.8f), (2.4f, -4.8f), (-1.8f, -4.2f));
                PutOnCeiling(GrayboxProp.SmokeDetector, ceiling, (-1.8f, 1.8f), (1.8f, -1.2f), (-2.4f, -2.4f));
                PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (-4.8f, 2.4f), (2.4f, 2.4f), (1.2f, -4.8f), (-2.4f, -4.8f));
                Put(GrayboxProp.SecurityDome, cameras, -5.4f, Ceiling, -5.4f, 45f, "SecurityDome_SW");
                Put(GrayboxProp.SecurityDome, cameras, 5.4f, Ceiling, 5.4f, 225f, "SecurityDome_NE");
            }

            // Both doors: a light switch beside each (on the side away from the door's reader), an emergency light.
            Put(GrayboxProp.LightSwitch, safety, -2.85f, GrayboxPropsTool.LightSwitchHeight, -Wall, 0f, "LightSwitch_S");
            Put(GrayboxProp.LightSwitch, safety, -Wall, GrayboxPropsTool.LightSwitchHeight, 2.85f, 90f, "LightSwitch_W");
            Put(GrayboxProp.EmergencyLight, safety, -Wall, EmergencyLightHeight, 4.8f, 90f);
            // Sprinklers 4.2 m apart, 1.6 m from the walls (light hazard).
            PutOnCeiling(GrayboxProp.Sprinkler, ceiling, Lattice(new[] { -4.2f, 0f, 4.2f }, new[] { -4.2f, 0f, 4.2f }));
        }

        // ---------------------------------------------------------------- small rooms (6 x 6 m by the south door)

        /// <summary>
        /// What every small room has, placed the same way: four sprinklers (2.4 m apart across, 3 m along the room, at most
        /// 2.2 m from a wall), one smoke detector, a ceiling dome in the corner by the door looking in, the light switch on
        /// the west wall beside the door, an emergency light on the west wall, and the extinguisher and the call point on
        /// the east wall by the door (west if <paramref name="extinguisherWest"/>).
        /// </summary>
        private static void SmallRoomBasics(Transform dressing, Transform cameras, (float x, float z) detector, bool extinguisherWest = false)
        {
            var safety = GrayboxUtility.CreateGroup("Safety", dressing);
            var ceiling = dressing.Find("Ceiling") ?? GrayboxUtility.CreateGroup("Ceiling", dressing);
            PutOnCeiling(GrayboxProp.Sprinkler, ceiling, (-1.2f, -0.6f), (1.2f, -0.6f), (-1.2f, -3.6f), (1.2f, -3.6f));
            PutOnCeiling(GrayboxProp.SmokeDetector, ceiling, detector);
            Put(GrayboxProp.SecurityDome, cameras, 2.4f, Ceiling, -5.4f, 315f, "SecurityDome");
            Put(GrayboxProp.LightSwitch, safety, -Side, GrayboxPropsTool.LightSwitchHeight, -5.6f, 90f, "LightSwitch");
            Put(GrayboxProp.EmergencyLight, safety, -Side, EmergencyLightHeight, -3.0f, 90f);
            float x = extinguisherWest ? -Side : Side;
            float yaw = extinguisherWest ? 90f : 270f;
            Put(GrayboxProp.FireExtinguisher, safety, x, 0f, -5.3f, yaw);
            Put(GrayboxProp.FireCallPoint, safety, x, GrayboxPropsTool.CallPointHeight, -5.3f, yaw);
        }

        /// <summary>
        /// Small office, 6 x 6 m. Design 1: two desks facing the west wall, the coffee corner on the back wall with a
        /// standing table, a printer and filing cabinets on the east wall, the terminal on the west wall by the door.
        /// Design 2: a meeting table for four at right angles to the TV on the back wall, two desks facing the east wall,
        /// filing cabinets on the east wall, a printer and the terminal on the west wall.
        /// </summary>
        private static void DressSmallOffice(Transform dressing, Transform cameras, int design)
        {
            var furniture = GrayboxUtility.CreateGroup("Furniture", dressing);
            var details = GrayboxUtility.CreateGroup("Details", dressing);
            var power = GrayboxUtility.CreateGroup("PowerAndData", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            float back = RoomContract.SmallRoomBackWall;

            if (design == 0)
            {
                PlaceDesk(furniture, details, power, new Vector3(-Side + 0.4f, 0f, -0.9f), 90f, "W1", false, false);
                PlaceDesk(furniture, details, power, new Vector3(-Side + 0.4f, 0f, -2.6f), 90f, "W2", true, false);
                Put(GrayboxProp.PowerSocket, power, -Side, OutletHeight, -1.65f, 90f, "PowerSocket_Desks");
                Put(GrayboxProp.DataSocket, power, -Side, OutletHeight, -1.9f, 90f, "DataSocket_Desks");

                Kitchenette(furniture, details, power, new Vector3(1.0f, 0f, back - 0.3f), 180f);
                Put(GrayboxProp.Fridge, furniture, 2.3f, 0f, back - 0.345f, 180f);
                StandingTable(furniture, details, new Vector3(1.0f, 0f, -1.9f));

                Put(GrayboxProp.Printer, furniture, Side - 0.325f, 0f, -3.0f, 270f);
                Put(GrayboxProp.PowerSocket, power, Side, OutletHeight, -2.5f, 270f, "PowerSocket_Printer");
                CabinetRow(furniture, details, "E", new[] { -3.65f, -4.1f }, z => new Vector3(Side - 0.3f, 0f, z), 270f);
                DressTerminalOutlets(power, -Side, -5.2f, 90f);

                PutLongFitting(GrayboxProp.LedPanel1200, ceiling, (-1.8f, -1.5f), (1.2f, -1.5f), (0f, -4.5f));
                PutOnCeiling(GrayboxProp.LedPanel600, ceiling, (1.8f, -3.6f));
                PutOnCeiling(GrayboxProp.CeilingDiffuser, ceiling, (0f, -1.2f));
                PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (0f, -3.0f));
                SmallRoomBasics(dressing, cameras, (0f, -2.4f));
            }
            else
            {
                MeetingTable(furniture, details, new Vector3(-0.9f, 0f, -2.3f), 90f, 2);
                Put(GrayboxProp.WallTv, furniture, -0.9f, 0f, back, 180f);
                Put(GrayboxProp.PowerSocket, power, -0.1f, OutletHeight, back, 180f, "PowerSocket_Tv");
                Put(GrayboxProp.DataSocket, power, 0.15f, OutletHeight, back, 180f, "DataSocket_Tv");

                PlaceDesk(furniture, details, power, new Vector3(Side - 0.4f, 0f, -0.9f), 270f, "E1", true, false);
                PlaceDesk(furniture, details, power, new Vector3(Side - 0.4f, 0f, -2.6f), 270f, "E2", false, false);
                Put(GrayboxProp.PowerSocket, power, Side, OutletHeight, -1.65f, 270f, "PowerSocket_Desks");
                Put(GrayboxProp.DataSocket, power, Side, OutletHeight, -1.9f, 270f, "DataSocket_Desks");
                CabinetRow(furniture, details, "E", new[] { -3.7f, -4.15f }, z => new Vector3(Side - 0.3f, 0f, z), 270f);

                Put(GrayboxProp.Printer, furniture, -Side + 0.325f, 0f, -3.6f, 90f);
                Put(GrayboxProp.PowerSocket, power, -Side, OutletHeight, -3.1f, 90f, "PowerSocket_Printer");
                DressTerminalOutlets(power, -Side, -5.2f, 90f);

                PutLongFitting(GrayboxProp.LedPanel1200, ceiling, (-0.6f, -2.1f), (2.4f, -1.5f), (0f, -4.5f));
                PutOnCeiling(GrayboxProp.LedPanel600, ceiling, (-1.8f, -4.2f));
                PutOnCeiling(GrayboxProp.CeilingDiffuser, ceiling, (0.6f, -1.2f));
                PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (-1.8f, -1.2f));
                SmallRoomBasics(dressing, cameras, (0.6f, -3.0f));
            }
        }

        /// <summary>
        /// Security office, 6 x 6 m: the video wall of 2 x 4 screens across the whole back wall (Create Graybox Rooms), two
        /// desks with two monitors each facing it, the compute and JBOD rack slots on a side wall and the terminal by the
        /// door; nothing else but, in design 1, lockers and a bench on the east wall, and in design 2 the coffee corner
        /// on the west wall (the racks and the terminal then stand on the east wall). Few lights, so the screens stay
        /// readable.
        /// </summary>
        private static void DressSecurityOffice(Transform dressing, Transform cameras, int design)
        {
            var furniture = GrayboxUtility.CreateGroup("Furniture", dressing);
            var details = GrayboxUtility.CreateGroup("Details", dressing);
            var power = GrayboxUtility.CreateGroup("PowerAndData", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);
            float back = RoomContract.SmallRoomBackWall;

            foreach (var (x, suffix) in new[] { (-0.85f, "1"), (0.85f, "2") })
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
            float rackSide = design == 0 ? -1f : 1f;
            float rackYaw = design == 0 ? 90f : 270f;
            Put(GrayboxProp.PowerSocket, power, rackSide * Side, OutletHeight, -3.05f, rackYaw, "PowerSocket_Racks");
            Put(GrayboxProp.DataSocket, power, rackSide * Side, OutletHeight, -2.8f, rackYaw, "DataSocket_Racks");
            DressTerminalOutlets(power, rackSide * Side, design == 0 ? -5.2f : -5.45f, rackYaw);

            if (design == 0)
            {
                // The changing corner: lockers on the east wall, a bench in front of them.
                foreach (float z in new[] { -3.3f, -3.7f, -4.1f })
                {
                    Put(GrayboxProp.Locker, furniture, Side - 0.25f, 0f, z, 270f, $"Locker_{z:0.0}");
                }
                Put(GrayboxProp.LockerBench, furniture, 1.9f, 0f, -3.7f, 90f);
            }
            else
            {
                Kitchenette(furniture, details, power, new Vector3(-Side + 0.3f, 0f, -3.8f), 90f);
            }

            PutOnCeiling(GrayboxProp.LedPanel600, ceiling, (-1.8f, -3.0f), (1.8f, -3.0f), (0f, -1.2f), (0f, -4.8f));
            PutOnCeiling(GrayboxProp.CeilingDiffuser, ceiling, (0f, -3.0f));
            PutOnCeiling(GrayboxProp.CeilingSpeaker, ceiling, (-0.6f, -4.2f));
            SmallRoomBasics(dressing, cameras, (0.6f, -2.4f), design == 1);
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
                    var position = new Vector3(x + side * 0.4f, 0f, z);
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
        /// starts at the west wall, runs east to <paramref name="riseX"/> and rises into the ceiling there, so both ends
        /// are closed. A power and a data outlet at each of <paramref name="outlets"/>.
        /// </summary>
        private static void ParapetTrunking(Transform power, float fromX, float riseX, float wallZ, float[] outlets)
        {
            var south = new Vector3(0f, 0f, -1f);
            float front = wallZ - GrayboxPropsTool.TrunkingDepth;
            Trunking(power, "ParapetTrunking", new Vector3(fromX, ParapetHeight, wallZ), new Vector3(riseX + GrayboxPropsTool.TrunkingWidth / 2f, ParapetHeight, wallZ), south);
            Trunking(power, "ParapetTrunking_Rise", new Vector3(riseX, ParapetHeight, wallZ), new Vector3(riseX, Ceiling, wallZ), south);
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
            Put(GrayboxProp.Mug, details, position + turn * new Vector3(-0.15f, GrayboxPropsTool.CounterHeight, 0.1f), yaw, "Mug_Counter1");
            Put(GrayboxProp.Mug, details, position + turn * new Vector3(0.0f, GrayboxPropsTool.CounterHeight, 0.05f), yaw + 40f, "Mug_Counter2");
            Put(GrayboxProp.PowerSocket, power, position + turn * new Vector3(-0.55f, 1.15f, -0.3f), yaw, "PowerSocket_Coffee");
        }

        /// <summary>The standing table of a coffee corner with two mugs on it.</summary>
        private static void StandingTable(Transform furniture, Transform details, Vector3 position)
        {
            Put(GrayboxProp.CoffeeTable, furniture, position);
            Put(GrayboxProp.Mug, details, position + new Vector3(-0.12f, GrayboxPropsTool.CoffeeTableHeight, 0.08f), 0f, "Mug_Table1");
            Put(GrayboxProp.Mug, details, position + new Vector3(0.14f, GrayboxPropsTool.CoffeeTableHeight, -0.06f), 120f, "Mug_Table2");
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

        /// <summary>
        /// A floor-to-ceiling glass wall between two points on the floor (along X or Z): aluminium head and floor
        /// tracks, mullions about every metre, glass panes with a collider so nobody walks through.
        /// </summary>
        private static void GlassWall(Transform parent, string name, Vector3 from, Vector3 to)
        {
            var group = GrayboxUtility.CreateGroup(name, parent);
            bool alongX = Mathf.Abs(to.x - from.x) > Mathf.Abs(to.z - from.z);
            var across = alongX ? Vector3.forward : Vector3.right;
            float top = Ceiling - GrayboxPropsTool.CeilingGridDepth;
            const float frame = 0.025f;
            Box("Track_Floor", group, from - across * frame, to + across * frame + Vector3.up * 0.04f, SurfaceMaterial.Metal);
            Box("Track_Head", group, from - across * frame + Vector3.up * (top - 0.04f), to + across * frame + Vector3.up * top, SurfaceMaterial.Metal);
            Box("Glass", group, from - across * 0.006f + Vector3.up * 0.04f, to + across * 0.006f + Vector3.up * (top - 0.04f), SurfaceMaterial.Glass, true);
            float length = (to - from).magnitude;
            int panes = Mathf.Max(1, Mathf.RoundToInt(length));
            var along = (to - from).normalized;
            for (int i = 0; i <= panes; i++)
            {
                var at = from + along * (length * i / panes);
                var half = along * 0.02f;
                Box($"Mullion_{i}", group, Clamp(at - half, from, to) - across * frame, Clamp(at + half, from, to) + across * frame + Vector3.up * top, SurfaceMaterial.Metal);
            }
        }

        private static Vector3 Clamp(Vector3 point, Vector3 a, Vector3 b) =>
            new Vector3(Mathf.Clamp(point.x, Mathf.Min(a.x, b.x), Mathf.Max(a.x, b.x)), point.y, Mathf.Clamp(point.z, Mathf.Min(a.z, b.z), Mathf.Max(a.z, b.z)));

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
        /// Electrical switch room, 6 x 6 m, filled with what belongs there and nothing else. Two cast-resin transformers
        /// stand along the side walls in the north half (0.8 m behind them, 1.8 m between their fronts), each behind a
        /// yellow keep-out line, with cross ventilation (a large intake grille low behind the west one, an exhaust grille
        /// high on the east wall). On the back wall between them the panel of analog meters for all hall sections. In the
        /// south half, 1 m in front of the transformers: design 1 two switchgear sections on the west wall and two UPS
        /// cabinets on the east wall; design 2 two distribution boards on the west wall and the switchgear on the east
        /// wall. An exposed power tray across the room. LED battens, sprinklers, a detector, a CO2 extinguisher.
        /// </summary>
        private static void DressPowerRoom(Transform dressing, Transform cameras, int design)
        {
            var plant = GrayboxUtility.CreateGroup("Plant", dressing);
            var power = GrayboxUtility.CreateGroup("Power", dressing);
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            var markings = GrayboxUtility.CreateGroup("Markings", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);

            const float transformerZ = -1.6f;
            Put(GrayboxProp.Transformer, plant, -1.5f, 0f, transformerZ, 90f, "Transformer_W");
            Put(GrayboxProp.Transformer, plant, 1.5f, 0f, transformerZ, 270f, "Transformer_E");
            // Keep-out lines 0.5 m around both enclosures, closed by the walls.
            foreach (int side in new[] { -1, 1 })
            {
                string name = side < 0 ? "W" : "E";
                Marking(markings, $"KeepOut_{name}_Front", new Vector3(side * Side, 0f, -3.1f), new Vector3(side * 0.4f, 0f, -3.1f));
                Marking(markings, $"KeepOut_{name}_Side", new Vector3(side * 0.4f, 0f, -3.1f), new Vector3(side * 0.4f, 0f, 0f));
            }
            Put(GrayboxProp.FloorVentGrille, air, -Side, 0f, transformerZ, 90f, "IntakeGrille");
            Put(GrayboxProp.WallVentGrille, air, Side, HighGrilleHeight, transformerZ, 270f, "ExhaustGrille");

            Put(GrayboxProp.AnalogMeterPanel, plant, 0f, 0f, RoomContract.SmallRoomBackWall, 180f, "MeterPanel");

            if (design == 0)
            {
                foreach (float z in new[] { -4.0f, -4.8f })
                {
                    Put(GrayboxProp.SwitchgearCabinet, plant, -Side + 0.3f, 0f, z, 90f, $"Switchgear_{z:0.0}");
                }
                foreach (float z in new[] { -4.0f, -4.6f })
                {
                    Put(GrayboxProp.EquipmentCabinet, plant, Side - 0.4f, 0f, z, 270f, $"UpsCabinet_{z:0.0}");
                }
                Tray(power, "PowerTray", new Vector3(-Side, 0f, -3.0f), new Vector3(Side, 0f, -3.0f), SurfaceMaterial.Metal, SurfaceMaterial.PowerCable,
                    new[] { -2.4f, -1.2f, 0f, 1.2f, 2.4f });
                foreach (var (x, z) in new[] { (0f, -1.6f), (0f, -4.0f), (-1.8f, -4.0f), (1.8f, -4.0f) })
                {
                    Put(GrayboxProp.LedBatten, ceiling, x, Ceiling, z, 0f, $"Light_{x:0.0}_{z:0.0}");
                }
            }
            else
            {
                foreach (float z in new[] { -4.0f, -4.8f })
                {
                    Put(GrayboxProp.ElectricalPanel, power, -Side, 0f, z, 90f, $"DistributionBoard_{z:0.0}");
                    Put(GrayboxProp.SwitchgearCabinet, plant, Side - 0.3f, 0f, z, 270f, $"Switchgear_{z:0.0}");
                }
                Tray(power, "PowerTray", new Vector3(-Side, 0f, -4.4f), new Vector3(Side, 0f, -4.4f), SurfaceMaterial.Metal, SurfaceMaterial.PowerCable,
                    new[] { -2.4f, -1.2f, 0f, 1.2f, 2.4f });
                foreach (var (x, z) in new[] { (0f, -1.6f), (-0.9f, -4.0f), (0.9f, -4.0f) })
                {
                    Put(GrayboxProp.LedBatten, ceiling, x, Ceiling, z, 0f, $"Light_{x:0.0}_{z:0.0}");
                }
            }

            SmallRoomBasics(dressing, cameras, (0.6f, -2.4f));
        }

        /// <summary>
        /// Cooling plant, 6 x 6 m, filled with what belongs there and nothing else: two plate heat exchangers (chilled
        /// water to the hall's cooling loop, their pipes rising behind them to the ceiling), two circulation pumps with
        /// risers and thermometers, a chilled-water buffer tank, the panel of analog meters (coolant temperatures and air
        /// flow), a pair of chilled-water risers on the wall and the supply duct under the slab across the room, with a
        /// return grille high on the wall. Design 1: exchangers on the back wall, pumps in front of them, the tank by the
        /// west wall; design 2: exchangers along the side walls, the tank on the back wall between them, the pumps by the
        /// side walls further south. Plant keeps 1 m between machines and 0.6 m to the walls.
        /// </summary>
        private static void DressHvacRoom(Transform dressing, Transform cameras, int design)
        {
            var plant = GrayboxUtility.CreateGroup("Plant", dressing);
            var pipes = GrayboxUtility.CreateGroup("Pipes", dressing);
            var air = GrayboxUtility.CreateGroup("Ventilation", dressing);
            var ceiling = GrayboxUtility.CreateGroup("Ceiling", dressing);

            void Pump(float x, float z, float yaw, string name)
            {
                Put(GrayboxProp.CirculationPump, plant, x, 0f, z, yaw, $"Pump_{name}");
                Pipe($"PumpRiser_{name}", pipes, new Vector3(x, 0.7f, z), new Vector3(x, Ceiling, z), 0.04f, SurfaceMaterial.PipeWater);
                var front = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Put(GrayboxProp.PipeGauge, pipes, new Vector3(x, 1.5f, z) + front * 0.04f, yaw, $"Thermometer_{name}");
            }

            float riserZ;
            if (design == 0)
            {
                Put(GrayboxProp.PlateHeatExchanger, plant, -1.4f, 0f, -0.9f, 180f, "HeatExchanger_W");
                Put(GrayboxProp.PlateHeatExchanger, plant, 1.4f, 0f, -0.9f, 180f, "HeatExchanger_E");
                Pump(-1.4f, -2.4f, 180f, "1");
                Pump(1.4f, -2.4f, 180f, "2");
                Put(GrayboxProp.BufferTank, plant, -1.9f, 0f, -4.2f, 90f, "BufferTank");
                Put(GrayboxProp.AnalogMeterPanel, plant, Side, 0f, -3.6f, 270f, "MeterPanel");
                riserZ = -4.6f;
                foreach (var (x, z) in new[] { (0f, -1.6f), (0f, -4.2f), (1.8f, -4.2f) })
                {
                    Put(GrayboxProp.LedBatten, ceiling, x, Ceiling, z, 0f, $"Light_{x:0.0}_{z:0.0}");
                }
            }
            else
            {
                Put(GrayboxProp.PlateHeatExchanger, plant, -2.0f, 0f, -1.3f, 90f, "HeatExchanger_W");
                Put(GrayboxProp.PlateHeatExchanger, plant, 2.0f, 0f, -1.3f, 270f, "HeatExchanger_E");
                Put(GrayboxProp.BufferTank, plant, 0f, 0f, -1.0f, 180f, "BufferTank");
                Pump(-2.1f, -3.4f, 90f, "1");
                Pump(2.1f, -3.4f, 270f, "2");
                Put(GrayboxProp.AnalogMeterPanel, plant, -Side, 0f, -4.6f, 90f, "MeterPanel");
                riserZ = -4.4f;
                foreach (var (x, z) in new[] { (0f, -2.0f), (0f, -4.4f), (1.8f, -4.4f) })
                {
                    Put(GrayboxProp.LedBatten, ceiling, x, Ceiling, z, 0f, $"Light_{x:0.0}_{z:0.0}");
                }
            }

            // Chilled-water flow and return risers on the east wall, floor to ceiling.
            Riser(pipes, "ChilledWater_Flow", new Vector3(Side, 0f, riserZ), Vector3.left, 0.05f, SurfaceMaterial.PipeWater, 1.3f);
            Riser(pipes, "ChilledWater_Return", new Vector3(Side, 0f, riserZ - 0.25f), Vector3.left, 0.05f, SurfaceMaterial.PipeWater, 1.5f);

            // The supply duct comes out of the walls under the slab and runs across the room, grilles in its underside.
            const float ductZ = -3.0f;
            Box("SupplyDuct", air, new Vector3(-Side, TrayBottom, ductZ - 0.25f), new Vector3(Side, Ceiling - 0.02f, ductZ + 0.25f), SurfaceMaterial.Metal);
            foreach (float x in new[] { -1.2f, 1.2f })
            {
                Box($"DuctGrille_{x:0.0}", air, new Vector3(x - 0.2f, TrayBottom - 0.01f, ductZ - 0.15f), new Vector3(x + 0.2f, TrayBottom, ductZ + 0.15f), SurfaceMaterial.FloorVent);
            }
            Put(GrayboxProp.WallVentGrille, air, -Side, HighGrilleHeight, -2.4f, 90f, "ReturnGrille");

            SmallRoomBasics(dressing, cameras, design == 0 ? (0f, -0.6f) : (0.6f, -0.6f));
        }

        /// <summary>
        /// Storage room, 6 x 6 m, filled with steel shelving that carries crates, archive boxes and binders, every shelf
        /// reachable from an aisle at least 0.9 m wide: a row along the back wall, a row along the west wall, a
        /// back-to-back double row in the middle (along Z in design 1, along X in design 2) and one unit on the east wall.
        /// The east wall has the landing of the goods lift (only looks: its solid doors never open) with yellow keep-clear
        /// hatching on the floor in front.
        /// An exhaust grille high on the back wall, LED battens, sprinklers, a detector, an extinguisher.
        /// </summary>
        private static void DressStorageRoom(Transform dressing, Transform cameras, int design)
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

            foreach (float x in new[] { -2.4f, -1.4f, -0.4f, 0.6f, 1.6f })
            {
                Shelf(x, back - half, 180f);
            }
            foreach (float z in new[] { -0.9f, -1.9f, -2.9f, -3.9f, -4.9f })
            {
                Shelf(-Side + half, z, 90f);
            }
            Shelf(Side - half, -1.0f, 270f);
            if (design == 0)
            {
                foreach (float z in new[] { -1.9f, -2.9f })
                {
                    Shelf(-half, z, 270f);
                    Shelf(half, z, 90f);
                }
            }
            else
            {
                foreach (float x in new[] { -0.5f, 0.5f })
                {
                    Shelf(x, -1.9f + half, 0f);
                    Shelf(x, -1.9f - half, 180f);
                }
            }

            // The goods lift on the east wall and the space in front of it that must stay clear for loading.
            var lift = GrayboxUtility.CreateGroup("FreightLift", dressing);
            Put(GrayboxProp.FreightLift, lift, Side, 0f, FreightLiftZ, 270f, "FreightLift");
            float hatch = GrayboxPropsTool.FreightDoorWidth / 2f + 0.15f;
            Hatching(markings, "KeepClear_FreightLift", 1.6f, FreightLiftZ - hatch, Side, FreightLiftZ + hatch);

            Put(GrayboxProp.WallVentGrille, air, 0f, HighGrilleHeight, back, 180f, "ExhaustGrille");
            foreach (var (x, z) in new[] { (-1.5f, -2.0f), (1.5f, -2.0f), (0f, -4.6f) })
            {
                Put(GrayboxProp.LedBatten, ceiling, x, Ceiling, z, 0f, $"Light_{x:0.0}_{z:0.0}");
            }
            SmallRoomBasics(dressing, cameras, (0f, -0.6f));
        }

        /// <summary>Room-local Z of the middle of the goods lift door in the storage room (on the east wall).</summary>
        private const float FreightLiftZ = -3.55f;

        /// <summary>
        /// A steel shelving unit (1 m wide, its archive boxes built in) with more on its boards: crates on the bottom
        /// board, binders or a crate on the middle board, and a crate on top of every third unit.
        /// </summary>
        private static void ShelfWithItems(Transform shelving, Transform details, Vector3 position, float yaw, int index)
        {
            var turn = Quaternion.Euler(0f, yaw, 0f);
            Put(GrayboxProp.Shelf, shelving, position, yaw, $"Shelf_{index + 1}");
            var boards = GrayboxPropsTool.ShelfBoardTops;
            Put(GrayboxProp.StorageCrate, details, position + turn * new Vector3(-0.25f, boards[0], 0f), yaw, $"StorageCrate_{index + 1}_A");
            if (index % 2 == 0)
            {
                Put(GrayboxProp.StorageCrate, details, position + turn * new Vector3(0.24f, boards[0], 0f), yaw, $"StorageCrate_{index + 1}_B");
                Put(GrayboxProp.Binders, details, position + turn * new Vector3(0f, boards[2], 0f), yaw, $"Binders_{index + 1}");
            }
            else
            {
                Put(GrayboxProp.StorageCrate, details, position + turn * new Vector3(0.2f, boards[2], 0f), yaw, $"StorageCrate_{index + 1}_C");
            }
            if (index % 3 == 0)
            {
                Put(GrayboxProp.StorageCrate, details, position + turn * new Vector3(-0.1f, ShelfTop, 0f), yaw, $"StorageCrate_{index + 1}_Top");
            }
        }

        /// <summary>Top of the shelving's highest board.</summary>
        private const float ShelfTop = 1.92f;

        private static string Corner(int sx, int sz) => $"{(sz > 0 ? "N" : "S")}{(sx > 0 ? "E" : "W")}";
    }
}
