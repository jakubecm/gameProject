using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Every model rooms are built from. One value = one prefab <c>Prefabs/Props/&lt;Name&gt;.prefab</c>, built by the method marked
    /// <c>[Model(Prop.Name)]</c> in <c>World/Props/&lt;Category&gt;/&lt;Name&gt;.cs</c>. Adding a model: a value here and one file with its builder.
    /// The values are not serialized anywhere, so they may be reordered; the names are the prefab file names. Reference: PROPS.md.
    /// </summary>
    public enum Prop
    {
        // Structure
        DoorFrame,
        RoomDoor,
        AisleDoor,
        PassengerLift,
        FreightLift,

        // Lighting
        LedPanel,
        LedBatten,
        PendantLight,
        PendantLightGirder,
        AisleLight,
        ExitSign,

        // Safety and security
        Sprinkler,
        SmokeDetector,
        CeilingSpeaker,
        FireExtinguisher,
        FireCallPoint,
        HoseCabinet,
        GasNozzle,
        CameraDome,
        CameraPendant,

        // Building services
        PowerSocket,
        DataSocket,
        FloorBox,
        LightSwitch,
        WallVentGrille,
        FloorVentGrille,
        CeilingDiffuser,
        PerforatedFloorTile,
        PipeThermometer,

        // Office and security office
        Desk,
        OfficeChair,
        Workstation,
        PcTower,
        MeetingTable,
        FilingCabinet,
        Printer,
        WallTv,
        VideoScreen,
        CoffeeCounter,
        CoffeeMachine,
        Fridge,
        Mug,
        PaperStack,
        PhotoFrame,
        Locker,
        LockerBench,

        // Storage room
        Shelf,
        BoxLidCube,
        BoxLidFlat,
        BoxCartonCube,
        BoxCartonFlat,

        // Power room
        Transformer,
        Switchgear,
        UpsCabinet,
        PowerMeterPanel,

        // Cooling plant
        HeatExchanger,
        HeatExchangerMirrored,
        CirculationPump,
        BufferTank,
        AirHandler,
        ExpansionVessel,
        CoolingMeterPanel,

        // Server hall
        Rack,
        HallSign,
    }

    /// <summary>Marks the static method <c>void Build(Transform root)</c> that builds a <see cref="Prop"/>.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ModelAttribute : Attribute
    {
        public ModelAttribute(Prop prop) => Prop = prop;

        public Prop Prop { get; }
    }

    /// <summary>
    /// Builds the models as prefabs with real-world dimensions; rooms hold instances of them, so replacing a model with a final
    /// one (task 4.7) is one prefab edit. Built and rebuilt in place (same GUIDs) by Tools/Cold Shutdown/Create Preview Scene.
    ///
    /// Pivot conventions (local +Z is "forward"):
    /// - floor props: pivot on the floor, centered under the prop; forward is the side people use
    ///   (desk and cabinet fronts, the way a seated person faces, the open side of a shelf);
    /// - wall props: pivot on the floor at the wall surface; forward points away from the wall;
    /// - small wall fittings (sockets): pivot at the center of the back plate; forward away from the wall;
    /// - ceiling props: pivot on the ceiling surface, hanging down; the long side runs along Z;
    /// - desk-top props: pivot on the desk surface; forward is the way the screen faces;
    /// - doors and the door frame: pivot is the opening pose (<see cref="RoomContract.GetOpeningPose"/>).
    /// </summary>
    public static partial class Props
    {
        public const string PropsFolder = "Assets/_Project/Prefabs/Props";

        public const float DeskHeight = 0.75f;

        /// <summary>
        /// Cross-section of every electrical trunking run (width across the wall, depth off the wall), so the
        /// trunking leaving a distribution panel and the trunking along walls are the same product.
        /// </summary>
        public const float TrunkingWidth = 0.12f;
        public const float TrunkingDepth = 0.06f;

        /// <summary>Height of the suspended-ceiling T-bars below the ceiling plane; ceiling fittings cover it.</summary>
        public const float CeilingGridDepth = 0.012f;

        private static Dictionary<Prop, Action<Transform>> builders;

        /// <summary>The builder of every prop, found by its <see cref="ModelAttribute"/>; exactly one per prop.</summary>
        private static Dictionary<Prop, Action<Transform>> Builders
        {
            get
            {
                if (builders != null) return builders;
                var found = new Dictionary<Prop, Action<Transform>>();
                foreach (var method in typeof(Props).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var model = method.GetCustomAttribute<ModelAttribute>();
                    if (model == null) continue;
                    if (found.ContainsKey(model.Prop)) throw new InvalidOperationException($"{model.Prop} has two builders.");
                    found.Add(model.Prop, (Action<Transform>)Delegate.CreateDelegate(typeof(Action<Transform>), method));
                }
                var missing = Enum.GetValues(typeof(Prop)).Cast<Prop>().Where(prop => !found.ContainsKey(prop)).ToList();
                if (missing.Count > 0) throw new InvalidOperationException($"No [Model] builder for {string.Join(", ", missing)}.");
                return builders = found;
            }
        }

        public static string PathOf(Prop prop) => $"{PropsFolder}/{prop}.prefab";

        /// <summary>Builds the props whose prefab is missing.</summary>
        public static void CreateMissingProps() => BuildProps(false);

        /// <summary>Builds every prop prefab; <paramref name="replace"/> rebuilds existing ones in place (same GUID).</summary>
        internal static void BuildProps(bool replace)
        {
            foreach (Prop prop in Enum.GetValues(typeof(Prop)))
            {
                string path = PathOf(prop);
                if (!replace && RoomShell.SkipExisting(path))
                {
                    continue;
                }

                var root = new GameObject(prop.ToString());
                Builders[prop](root.transform);
                RoomShell.SaveAsPrefab(root, path);
                Debug.Log($"Prop saved to {path}.");
            }
        }

        /// <summary>
        /// Deletes the prefabs in <see cref="PropsFolder"/> that no <see cref="Prop"/> builds any more (removed or renamed props),
        /// so a rebuild leaves only what the tools make.
        /// </summary>
        internal static void DeleteObsoleteProps()
        {
            var current = new HashSet<string>();
            foreach (Prop prop in Enum.GetValues(typeof(Prop))) current.Add(PathOf(prop));
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PropsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (current.Contains(path) || System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') != PropsFolder) continue;
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Deleted {path}: no prop is built from it any more.");
            }
        }

        /// <summary>Places an instance of a prop prefab (created first if missing) under <paramref name="parent"/>.</summary>
        internal static Transform Place(Prop prop, Transform parent, Vector3 localPosition, float yawDegrees = 0f, string name = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(prop));
            if (prefab == null)
            {
                CreateMissingProps();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(prop));
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name ?? prop.ToString();
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);
            return instance.transform;
        }

        /// <summary>One box collider over the whole prop, instead of a mesh collider on every small part.</summary>
        internal static void AddBoxCollider(Transform root, Vector3 center, Vector3 size)
        {
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }
    }
}
