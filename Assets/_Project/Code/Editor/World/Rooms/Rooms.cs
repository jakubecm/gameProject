using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// What one room type adds to the shell: its game logic (slots and structure) and its look (dressing). Every room file in
    /// <c>World/Rooms</c> declares the template of its type(s) with a method <c>[Template(RoomType.X)] RoomTemplate X(RoomType type)</c>.
    /// </summary>
    internal sealed class RoomTemplate
    {
        /// <summary>Where the terminal stands (pivot on the floor, forward into the room); null for none.</summary>
        public SlotPose? Terminal;

        /// <summary>Rack slots (pivot on the floor, the rack front into the room).</summary>
        public SlotPose[] Racks = Array.Empty<SlotPose>();

        /// <summary>Game logic built into the prefab beside the slots (walls of a small room, the lift shaft, the video wall); may be null.</summary>
        public Action<Transform> Structure;

        /// <summary>Builds the look into the <c>Dressing</c> group and the cameras into <c>SecurityCameras</c>; the int is the layout.</summary>
        public Action<Transform, Transform, int> Dress;
    }

    /// <summary>Marks the static method <c>RoomTemplate X(RoomType type)</c> that describes a room type (one method may serve several types).</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    internal sealed class TemplateAttribute : Attribute
    {
        public TemplateAttribute(RoomType type) => Type = type;

        public RoomType Type { get; }
    }

    /// <summary>
    /// Builds every room prefab <c>Room_&lt;Type&gt;[_Layout&lt;n&gt;]</c> (a prefab variant of the room shell) and its
    /// <see cref="RoomDefinition"/>. One file per room type holds its template; this file holds the pipeline and the shell's look
    /// (surface materials, door frames, suspended ceiling). Room rules and the exact contents of every room: ROOMS.md.
    ///
    /// Logic and look are separate: the slots, <c>Structure</c> and <c>VideoWall</c> are game logic; everything in <c>Dressing</c>
    /// is looks only and code never reads it; cameras sit in <c>SecurityCameras</c>. Every cable, conduit, pipe and duct starts and
    /// ends in the floor, the ceiling, a wall or a device, so rooms stay independent modules. Nothing in the dressing blocks the
    /// openings, the keep-clear walkway, the door pockets, the rack slots or the space in front of a terminal.
    /// </summary>
    public static partial class Rooms
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

        // Socket heights: flush wall outlets and the desk-height parapet trunking in the office.
        private const float OutletHeight = 0.3f;

        /// <summary>Center height of the small grilles under a 3 m ceiling (<see cref="Props.GrilleGap"/> below it).</summary>
        private static float HighGrilleHeight => Props.HighGrilleCenter(RoomContract.CeilingHeight);

        /// <summary>Technical rooms: no suspended ceiling, epoxy floor, services exposed.</summary>
        public static bool IsTechnicalRoom(RoomType type) =>
            type == RoomType.PowerRoom || type == RoomType.CoolingPlant || type == RoomType.StorageRoom;

        private static float Wall => RoomContract.InteriorHalfSize;

        /// <summary>Inner face of a small room's side walls.</summary>
        private static float Side => RoomContract.SmallRoomHalfWidth;

        private static float Ceiling => RoomContract.CeilingHeight;

        private static Dictionary<RoomType, RoomTemplate> templates;

        /// <summary>The template of a room type (every type but the hall has one, found by its <see cref="TemplateAttribute"/>).</summary>
        internal static RoomTemplate TemplateOf(RoomType type)
        {
            if (templates == null)
            {
                var found = new Dictionary<RoomType, RoomTemplate>();
                foreach (var method in typeof(Rooms).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    foreach (var attribute in method.GetCustomAttributes<TemplateAttribute>())
                    {
                        if (found.ContainsKey(attribute.Type)) throw new InvalidOperationException($"{attribute.Type} has two templates.");
                        found.Add(attribute.Type, (RoomTemplate)method.Invoke(null, new object[] { attribute.Type }));
                    }
                }
                templates = found;
            }
            if (!templates.TryGetValue(type, out var template)) throw new InvalidOperationException($"No [Template] for {type}.");
            return template;
        }

        /// <summary>Every layout of every room type built from the shell (not the hall).</summary>
        public static IEnumerable<(RoomType type, int layout)> AllLayouts()
        {
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                if (type == RoomType.Hall) continue;
                for (int layout = 0; layout < RoomRules.LayoutCount(type); layout++)
                {
                    yield return (type, layout);
                }
            }
        }

        /// <summary>
        /// The whole rebuild without asking: material settings, every prop, the shell's look, then every layout of every room type in
        /// one pass each (logic and look, in place, same GUIDs), the definitions, and deletes room prefabs and definitions no type uses.
        /// </summary>
        internal static void RebuildAll()
        {
            Surfaces.ResetAll();
            Props.BuildProps(true);
            EditPrefab(RoomShell.ShellPath, root => DressShell(root, true));
            EditPrefab(RoomShell.WallPlugPath, AssignDefaultSurfaces);
            var shell = AssetDatabase.LoadAssetAtPath<GameObject>(RoomShell.ShellPath);
            if (shell == null)
            {
                Debug.LogError($"{RoomShell.ShellPath} not found.");
                return;
            }
            foreach (var (type, layout) in AllLayouts())
            {
                BuildRoom(shell, type, layout);
            }
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                if (type != RoomType.Hall) SaveDefinition(type);
            }
            DeleteObsoleteRooms();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Builds one layout prefab (a new prefab variant of the shell the first time, in place afterwards): throws its logic and look away and
        /// builds them again from the template.
        /// </summary>
        private static void BuildRoom(GameObject shell, RoomType type, int layout)
        {
            string path = PrefabPath(type, layout);
            var template = TemplateOf(type);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(path) : (GameObject)PrefabUtility.InstantiatePrefab(shell);
            try
            {
                root.name = Path.GetFileNameWithoutExtension(path);
                foreach (var name in new[] { StructureName, VideoWallName, "RackSlots", "TerminalSlot", DressingName, SecurityCamerasName })
                {
                    Transform child;
                    while ((child = root.transform.Find(name)) != null)
                    {
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    }
                }

                template.Structure?.Invoke(root.transform);
                if (template.Terminal.HasValue)
                {
                    var slot = new GameObject("TerminalSlot", typeof(TerminalSlot)).transform;
                    slot.SetParent(root.transform, false);
                    RoomShell.ApplyPose(slot, template.Terminal.Value);
                }
                if (template.Racks.Length > 0)
                {
                    var group = ModelUtility.CreateGroup("RackSlots", root.transform);
                    for (int i = 0; i < template.Racks.Length; i++)
                    {
                        var slot = new GameObject($"RackSlot_{i:00}", typeof(RackSlot)).transform;
                        slot.SetParent(group, false);
                        RoomShell.ApplyPose(slot, template.Racks[i]);
                    }
                }

                ConfigureShell(root, IsTechnicalRoom(type));
                var dressing = ModelUtility.CreateGroup(DressingName, root.transform);
                var cameras = ModelUtility.CreateGroup(SecurityCamerasName, root.transform);
                template.Dress(dressing, cameras, layout);

                if (exists) PrefabUtility.SaveAsPrefabAsset(root, path);
                else RoomShell.SaveAsPrefab(root, path);
                Debug.Log($"Room prefab saved to {path}.");
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>Deletes room prefabs and definitions that no room type (or layout) uses any more, e.g. after a rename.</summary>
        private static void DeleteObsoleteRooms()
        {
            var prefabs = new HashSet<string> { PrefabPath(RoomType.Hall), RoomShell.ShellPath, RoomShell.WallPlugPath };
            foreach (var (type, layout) in AllLayouts()) prefabs.Add(PrefabPath(type, layout));
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { RoomShell.RoomsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (prefabs.Contains(path) || Path.GetDirectoryName(path)?.Replace('\\', '/') != RoomShell.RoomsFolder) continue;
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Deleted {path}: no room type uses it any more.");
            }
            var definitions = new HashSet<string>();
            foreach (RoomType type in Enum.GetValues(typeof(RoomType))) definitions.Add(DefinitionPath(type));
            foreach (string guid in AssetDatabase.FindAssets("t:RoomDefinition", new[] { DefinitionsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (definitions.Contains(path)) continue;
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Deleted {path}: no room type uses it any more.");
            }
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
            if (technical && mesh != null && mesh.GetComponent<MeshRenderer>().sharedMaterial != Surfaces.Get(SurfaceMaterial.HallFloor))
            {
                ModelUtility.SetSurface(mesh, SurfaceMaterial.HallFloor);
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
                changed |= Replace(wall, nameof(Prop.DoorFrame), rebuild,
                    // The wall group stands on the opening pose, which is the door frame's pivot.
                    () => Props.Place(Prop.DoorFrame, wall, Vector3.zero));
            }

            changed |= Replace(shell, SuspendedCeilingName, rebuild, () => BuildSuspendedCeiling(shell));
            return changed;
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
                ModelUtility.SetSurface(mesh, surface);
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Exposed T-bar grid of a lay-in ceiling: main and cross tees every 600 mm, with a tile centered on the room
        /// center (so a corridor arm has a tile row in its middle), and a wall angle around the edge. The ceiling slab
        /// above is the tile surface, so the grid hangs only a centimetre below the 3 m ceiling plane and every room keeps
        /// its height. The grid is symmetric, so it lines up whichever way the map turns the room.
        /// </summary>
        private static void BuildSuspendedCeiling(Transform shell)
        {
            var grid = ModelUtility.CreateGroup(SuspendedCeilingName, shell);
            const float bar = 0.024f;
            float depth = Props.CeilingGridDepth;
            float bottom = Ceiling - depth;
            int lines = Mathf.FloorToInt(Wall / CeilingTile - 0.5f);

            for (int i = -lines - 1; i <= lines; i++)
            {
                float at = (i + 0.5f) * CeilingTile;
                ModelUtility.CreateBoxBetween($"Tee_X_{i}", grid, new Vector3(-Wall, bottom, at - bar / 2f), new Vector3(Wall, Ceiling, at + bar / 2f), SurfaceMaterial.CeilingGrid);
                ModelUtility.CreateBoxBetween($"Tee_Z_{i}", grid, new Vector3(at - bar / 2f, bottom, -Wall), new Vector3(at + bar / 2f, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            }

            // The tiles: white mineral-fibre, 576 mm between the tees, with a stepped (tegular) edge; one mesh for the room.
            var tiles = new MeshBuilder();
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

            ModelUtility.CreateBoxBetween("WallAngle_N", grid, new Vector3(-Wall, bottom, Wall - bar), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            ModelUtility.CreateBoxBetween("WallAngle_S", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(Wall, Ceiling, -Wall + bar), SurfaceMaterial.CeilingGrid);
            ModelUtility.CreateBoxBetween("WallAngle_E", grid, new Vector3(Wall - bar, bottom, -Wall), new Vector3(Wall, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
            ModelUtility.CreateBoxBetween("WallAngle_W", grid, new Vector3(-Wall, bottom, -Wall), new Vector3(-Wall + bar, Ceiling, Wall), SurfaceMaterial.CeilingGrid);
        }

        public const string DefinitionsFolder = "Assets/_Project/Data";

        public static string PrefabPath(RoomType type) => $"{RoomShell.RoomsFolder}/Room_{type}.prefab";

        public static string DefinitionPath(RoomType type) => $"{DefinitionsFolder}/RoomDefinition_{type}.asset";

        public const string StructureName = "Structure";

        /// <summary>Creates the definition if it is missing and points it at the current prefabs of every layout.</summary>
        internal static void SaveDefinition(RoomType type)
        {
            string path = DefinitionPath(type);
            var definition = AssetDatabase.LoadAssetAtPath<RoomDefinition>(path);
            if (definition == null)
            {
                Directory.CreateDirectory(DefinitionsFolder);
                definition = ScriptableObject.CreateInstance<RoomDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("type").intValue = (int)type;
            serialized.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(type, 0));
            var others = serialized.FindProperty("otherLayouts");
            int count = RoomRules.LayoutCount(type) - 1;
            others.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                others.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(type, i + 1));
            }
            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(definition);
                Debug.Log($"Room definition saved to {path}.");
            }
        }


        public static string PrefabPath(RoomType type, int layout) =>
            layout == 0 ? PrefabPath(type) : $"{RoomShell.RoomsFolder}/Room_{type}_Layout{layout + 1}.prefab";

        private static float SmallWall => RoomContract.SmallRoomHalfWidth;
    }
}
