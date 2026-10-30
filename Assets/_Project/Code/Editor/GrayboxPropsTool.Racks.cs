using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// The server rack, 0.6 x 1.2 x 2.0 m, 42 U, after product photos. Only the front is ever seen, so the sides, back and top are plain
    /// steel and everything is modelled on the front: a black cabinet with a rolled top cap, 19" mounting rails and a door with a
    /// coarse semi-transparent hex grille (alpha-blended texture <c>T_RackGrille</c>, drawn on both sides) and a handle.
    ///
    /// There is exactly one rack prop, <c>Rack</c> (<see cref="Rack"/>). Whether it opens is a flag set when it is placed
    /// (<see cref="Rack.Configure"/>, chosen by <see cref="RackPlacement"/>): a closed rack has a flush handle and a fixed door; an openable
    /// one a pulled-out lever handle (visible even when the door is shut), a door that stands ajar and works (<see cref="RackDoor"/>:
    /// E opens it fully, E again shuts it completely), and a door collider. The filling of all 42 U is random per rack: building the props
    /// generates <see cref="ContentVariants"/> random fillings (<see cref="BuildContentLibrary"/>, deterministic) as shared meshes in
    /// <c>Data/RackContents.asset</c>, and a rack picks one by its seed, which costs nothing at runtime and keeps racks batchable.
    ///
    /// Servers (after photos): Dell R740xd (2 U, black, 24 vertical bays with orange rings), HP DL380p Gen8 (2 U, silver ears, 5 x 5 bays
    /// with red latches), Inter-Tech 3 U chassis (black, slotted vents, handles); plus a rack UPS (2 U, only at the bottom), a 1 U PDU (only in
    /// the lower half) and 1 U blanking panels. No switches, routers or cables, and the top unit is always a server. Every server shows
    /// green (and sometimes amber) status LEDs, which shine through the grille. The pivot is on the floor under the middle of the rack,
    /// +Z is the front.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        public const string RackContentsPath = "Assets/_Project/Data/RackContents.asset";
        private const string RackMeshFolder = "Assets/_Project/Art/Meshes/Racks";

        /// <summary>How many random fillings the library holds.</summary>
        public const int ContentVariants = 12;

        /// <summary>Height of one rack unit (1U = 44.45 mm).</summary>
        private const float RackUnit = 0.04445f;

        private const int RackUnits = 42;

        /// <summary>Width of a 19" panel.</summary>
        private const float PanelHalf = 0.2413f;

        /// <summary>Front plane of the servers (the rack's middle is z = 0, its front z = 0.6).</summary>
        private const float ServerFront = 0.50f;

        /// <summary>Size of one tile of the door's hex grille (the lattice pitch) and of the honeycomb vents, in metres.</summary>
        private const float RackHexTile = 0.016f;

        private const float HoneycombTile = 0.0045f;

        /// <summary>One cell of the slotted vent texture of the 3 U chassis.</summary>
        private const float SlotCellWidth = 0.0055f;

        private const float SlotCellHeight = 0.0095f;

        private const float DoorAjarDegrees = 22f;

        /// <summary>First rack unit: just above the floor plate.</summary>
        private const float RackBottom = 0.085f;

        private enum RackItem
        {
            Blank,
            Pdu,
            Ups,
            Dell,
            Hp,
            Chassis,
        }

        private static int Units(RackItem item)
        {
            switch (item)
            {
                case RackItem.Blank:
                case RackItem.Pdu:
                    return 1;
                case RackItem.Chassis:
                    return 3;
                default:
                    return 2;
            }
        }

        private static bool IsServer(RackItem item) => item == RackItem.Dell || item == RackItem.Hp || item == RackItem.Chassis;

        /// <summary>
        /// A random filling of the 42 U, from the bottom: maybe a UPS first, then servers in a random mix with the odd blanking panel and at
        /// most one PDU in the lower half; the top unit is always a server, and it always adds up to 42 U.
        /// </summary>
        private static List<RackItem> RandomContents(System.Random rng)
        {
            for (int attempt = 0; attempt < 10000; attempt++)
            {
                var list = new List<RackItem>();
                int used = 0;
                bool pdu = false;
                if (rng.NextDouble() < 0.7)
                {
                    list.Add(RackItem.Ups);
                    used += Units(RackItem.Ups);
                }
                while (used < RackUnits)
                {
                    int remaining = RackUnits - used;
                    var choices = new List<(RackItem item, double weight)> { (RackItem.Dell, 3), (RackItem.Hp, 3) };
                    if (remaining >= 3) choices.Add((RackItem.Chassis, 2));
                    if (list.Count > 0 && list[list.Count - 1] != RackItem.Blank && remaining > 2) choices.Add((RackItem.Blank, 0.5));
                    if (!pdu && used < 20 && remaining > 2) choices.Add((RackItem.Pdu, 0.5));
                    double roll = rng.NextDouble() * choices.Sum(c => c.weight);
                    var pick = choices[choices.Count - 1].item;
                    foreach (var (item, weight) in choices)
                    {
                        if ((roll -= weight) < 0)
                        {
                            pick = item;
                            break;
                        }
                    }
                    if (Units(pick) > remaining) break;
                    pdu |= pick == RackItem.Pdu;
                    list.Add(pick);
                    used += Units(pick);
                }
                if (used == RackUnits && IsServer(list[list.Count - 1]))
                {
                    return list;
                }
            }
            throw new InvalidOperationException("No random rack filling adds up to 42 U.");
        }

        /// <summary>The meshes of one part of a prop, one builder per surface material.</summary>
        private sealed class RackParts
        {
            private readonly Dictionary<SurfaceMaterial, GrayboxMeshBuilder> builders = new Dictionary<SurfaceMaterial, GrayboxMeshBuilder>();

            public GrayboxMeshBuilder this[SurfaceMaterial material]
            {
                get
                {
                    if (!builders.TryGetValue(material, out var builder))
                    {
                        builders[material] = builder = new GrayboxMeshBuilder();
                    }
                    return builder;
                }
            }

            public void Box(SurfaceMaterial material, float x0, float y0, float z0, float x1, float y1, float z1) =>
                this[material].AddBox(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

            /// <summary>A flat disc on the front face (facing +Z).</summary>
            public void Disc(SurfaceMaterial material, float x, float y, float z, float radius, int sides = 12) =>
                this[material].AddDisc(new Vector3(x, y, z), Vector3.forward, radius, sides);

            /// <summary>A front-facing quad whose texture repeats every <paramref name="tileU"/> x <paramref name="tileV"/> metres.</summary>
            public void TexturedPanel(SurfaceMaterial material, float x0, float y0, float x1, float y1, float z, float tileU, float tileV)
            {
                float u = (x1 - x0) / tileU;
                float v = (y1 - y0) / tileV;
                this[material].AddPanel(new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z),
                    Vector3.forward, new Vector2(0f, 0f), new Vector2(u, 0f), new Vector2(u, v), new Vector2(0f, v));
            }

            /// <summary>A status LED, green; every fourth server also shows an amber one beside it (activity).</summary>
            public void Led(float x, float y, int index, float z = ServerFront)
            {
                Box(SurfaceMaterial.SafetyGreen, x - 0.0025f, y - 0.002f, z, x + 0.0025f, y + 0.002f, z + 0.002f);
                if (index % 4 == 3)
                {
                    Box(SurfaceMaterial.LedAmber, x - 0.0025f, y - 0.007f, z, x + 0.0025f, y - 0.003f, z + 0.002f);
                }
            }

            /// <summary>Puts every material's geometry on a child of <paramref name="parent"/>, one saved mesh each.</summary>
            public void Attach(string prefix, Transform parent)
            {
                foreach (var pair in builders)
                {
                    pair.Value.Attach($"{prefix}_{pair.Key}", parent, pair.Key);
                }
            }

            /// <summary>One mesh with a submesh per material, in memory.</summary>
            public Mesh ToMesh(string name, out Material[] materials)
            {
                var instances = new List<CombineInstance>();
                var list = new List<Material>();
                foreach (var pair in builders)
                {
                    instances.Add(new CombineInstance { mesh = pair.Value.BuildMesh(pair.Key.ToString()) });
                    list.Add(GrayboxMaterials.Get(pair.Key));
                }
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(instances.ToArray(), false, false);
                foreach (var instance in instances) UnityEngine.Object.DestroyImmediate(instance.mesh);
                mesh.RecalculateBounds();
                materials = list.ToArray();
                return mesh;
            }
        }

        /// <summary>Fills a rack with the servers of <paramref name="items"/>, from the bottom.</summary>
        private static void BuildRackContents(RackParts parts, IList<RackItem> items)
        {
            float y = RackBottom;
            for (int i = 0; i < items.Count; i++)
            {
                BuildRackItem(parts, items[i], y, Units(items[i]) * RackUnit - 0.003f, i);
                y += Units(items[i]) * RackUnit;
            }
        }

        /// <summary>
        /// Builds <see cref="ContentVariants"/> random fillings (the same ones every time) as meshes in <see cref="RackMeshFolder"/> and lists
        /// them in the <see cref="RackContentLibrary"/> asset. Existing assets are rewritten in place (same GUIDs).
        /// </summary>
        public static RackContentLibrary BuildContentLibrary()
        {
            System.IO.Directory.CreateDirectory(RackMeshFolder);
            var library = AssetDatabase.LoadAssetAtPath<RackContentLibrary>(RackContentsPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<RackContentLibrary>();
                AssetDatabase.CreateAsset(library, RackContentsPath);
            }

            var variants = new RackContentLibrary.Variant[ContentVariants];
            for (int i = 0; i < ContentVariants; i++)
            {
                var parts = new RackParts();
                BuildRackContents(parts, RandomContents(new System.Random(1000 + i * 7919)));
                var mesh = parts.ToMesh($"RackContent_{i:00}", out var materials);
                string path = $"{RackMeshFolder}/RackContent_{i:00}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(mesh, path);
                    existing = mesh;
                }
                else
                {
                    EditorUtility.CopySerialized(mesh, existing);
                    UnityEngine.Object.DestroyImmediate(mesh);
                    EditorUtility.SetDirty(existing);
                }
                variants[i] = new RackContentLibrary.Variant { mesh = existing, materials = materials };
            }
            library.SetVariants(variants);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        private static void BuildRack(Transform root)
        {
            float width = RoomContract.RackWidth;
            float height = RoomContract.RackHeight;
            float depth = RoomContract.RackDepth;
            float side = width / 2f;
            float back = -depth / 2f;
            const float plinth = 0.06f;
            const float skin = 0.012f;
            const float doorFront = 0.576f;

            var cabinet = new RackParts();
            // Plinth, the rolled top cap, the sides and the floor and ceiling plates; the block behind the servers closes the rest.
            cabinet.Box(SurfaceMaterial.RackBlack, -side + 0.015f, 0f, back + 0.03f, side - 0.015f, plinth, doorFront - 0.02f);
            cabinet.Box(SurfaceMaterial.RackBlack, -side, height - 0.03f, back, side, height, depth / 2f);
            cabinet.Box(SurfaceMaterial.RackBlack, -side, plinth, back, -side + skin, height - 0.03f, doorFront);
            cabinet.Box(SurfaceMaterial.RackBlack, side - skin, plinth, back, side, height - 0.03f, doorFront);
            cabinet.Box(SurfaceMaterial.RackBlack, -side + skin, plinth, 0.40f, side - skin, plinth + 0.01f, doorFront);
            cabinet.Box(SurfaceMaterial.RackBlack, -side + skin, height - 0.04f, 0.40f, side - skin, height - 0.03f, doorFront);
            cabinet.Box(SurfaceMaterial.ServerBlack, -side + skin, plinth + 0.01f, back + 0.01f, side - skin, height - 0.04f, 0.40f);
            // 19" mounting rails behind the ears of the servers.
            foreach (int sign in new[] { -1, 1 })
            {
                float x0 = sign < 0 ? -side + skin : PanelHalf;
                float x1 = sign < 0 ? -PanelHalf : side - skin;
                cabinet.Box(SurfaceMaterial.RackBlack, x0, plinth + 0.01f, ServerFront - 0.03f, x1, height - 0.04f, ServerFront);
            }
            cabinet.Attach("Body", root);

            // The random filling: a shared mesh from the library (the rack swaps it for the one its seed picks).
            var library = BuildContentLibrary();
            var contents = new GameObject("Contents");
            contents.transform.SetParent(root, false);
            var filter = contents.AddComponent<MeshFilter>();
            var meshRenderer = contents.AddComponent<MeshRenderer>();
            filter.sharedMesh = library[0].mesh;
            meshRenderer.sharedMaterials = library[0].materials;

            // The door: hinged on the viewer's right (-X when looking at the front), handle on the left.
            var hinge = new GameObject("DoorHinge").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(-side + 0.002f, 0f, doorFront + 0.012f);
            float leafWidth = width - 0.004f;
            float top = height - 0.03f;
            var door = new RackParts();
            BuildRackDoor(door, leafWidth, plinth, top);
            door.Attach("Door", hinge);

            // Both handles hang on the door, centred on its height; the rack shows the one that fits its flag.
            float hx = leafWidth - 0.065f / 2f;
            float hy = (plinth + top) / 2f;
            var flush = new GameObject("HandleFlush").transform;
            flush.SetParent(hinge, false);
            var flushParts = new RackParts();
            flushParts.Box(SurfaceMaterial.RackBlack, hx - 0.014f, hy - 0.06f, 0.012f, hx + 0.014f, hy + 0.06f, 0.018f);
            flushParts.Box(SurfaceMaterial.ServerBlack, hx - 0.008f, hy - 0.04f, 0.018f, hx + 0.008f, hy + 0.04f, 0.021f);
            flushParts.Attach("HandleFlush", flush);

            var lever = new GameObject("HandleLever").transform;
            lever.SetParent(hinge, false);
            var leverParts = new RackParts();
            // The same black plate and the same grip as the flush handle, but the grip is pulled out: hinged at the bottom of its pocket and
            // standing straight out from the door (like a pressed latch lever turned away from the rack), with an empty pocket behind it.
            leverParts.Box(SurfaceMaterial.RackBlack, hx - 0.014f, hy - 0.06f, 0.012f, hx + 0.014f, hy + 0.06f, 0.018f);
            // Strictly in the rack's own colours: black plate, black pocket, black grip.
            leverParts.Box(SurfaceMaterial.ServerBlack, hx - 0.009f, hy - 0.042f, 0.018f, hx + 0.009f, hy + 0.042f, 0.0195f);
            leverParts.Box(SurfaceMaterial.RackBlack, hx - 0.011f, hy - 0.046f, 0.0195f, hx + 0.011f, hy - 0.038f, 0.026f);
            leverParts.Box(SurfaceMaterial.ServerBlack, hx - 0.008f, hy - 0.0420f, 0.0195f, hx + 0.008f, hy - 0.038f, 0.0195f + 0.085f);
            leverParts.Attach("HandleLever", lever);
            lever.gameObject.SetActive(false);

            // The glow of an openable rack: one very faint quad exactly the size of the door (additive, so it only brightens), swinging with it. Its
            // texture lights the outline and the stiles and rails of the door and leaves the grille panel almost clear. It shows only while the door is shut.
            var glowObject = new GameObject("DoorGlow").transform;
            glowObject.SetParent(hinge, false);
            var glowParts = new RackParts();
            glowParts[SurfaceMaterial.RackGlow].AddPanel(new Vector3(0f, plinth, 0.0125f), new Vector3(leafWidth, plinth, 0.0125f),
                new Vector3(leafWidth, top, 0.0125f), new Vector3(0f, top, 0.0125f), Vector3.forward,
                Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            glowParts.Attach("DoorGlow", glowObject);
            glowObject.gameObject.SetActive(false);
            var glowRenderer = glowObject.GetComponentInChildren<MeshRenderer>(true);
            var glowComponent = glowObject.gameObject.AddComponent<RackGlow>();
            var glowSerialized = new SerializedObject(glowComponent);
            glowSerialized.FindProperty("target").objectReferenceValue = glowRenderer;
            glowSerialized.ApplyModifiedPropertiesWithoutUndo();

            // The door collider and the door logic only work on an openable rack (Rack.Configure switches them).
            AddBoxCollider(hinge, new Vector3(leafWidth / 2f, (plinth + top) / 2f, 0f), new Vector3(leafWidth, top - plinth, 0.024f));
            var doorCollider = hinge.GetComponent<BoxCollider>();
            doorCollider.enabled = false;
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0f), new Vector3(width, height, depth));

            var rackDoor = root.gameObject.AddComponent<RackDoor>();
            var doorSerialized = new SerializedObject(rackDoor);
            doorSerialized.FindProperty("hinge").objectReferenceValue = hinge;
            doorSerialized.FindProperty("ajarAngle").floatValue = DoorAjarDegrees;
            doorSerialized.ApplyModifiedPropertiesWithoutUndo();
            rackDoor.enabled = false;

            var rack = root.gameObject.AddComponent<Rack>();
            var serialized = new SerializedObject(rack);
            serialized.FindProperty("library").objectReferenceValue = library;
            serialized.FindProperty("contentsFilter").objectReferenceValue = filter;
            serialized.FindProperty("contentsRenderer").objectReferenceValue = meshRenderer;
            serialized.FindProperty("door").objectReferenceValue = rackDoor;
            serialized.FindProperty("doorCollider").objectReferenceValue = doorCollider;
            serialized.FindProperty("flushHandle").objectReferenceValue = flush.gameObject;
            serialized.FindProperty("leverHandle").objectReferenceValue = lever.gameObject;
            serialized.FindProperty("glow").objectReferenceValue = glowObject.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The door leaf, authored on its hinge (x runs from the hinge along the leaf, z is the thickness): a 24 mm black frame (hinge
        /// stile, wide handle stile, top and bottom rail) and the grille panel set back in it. The handle is separate.
        /// </summary>
        private static void BuildRackDoor(RackParts door, float leafWidth, float bottom, float top)
        {
            const float half = 0.012f;
            const float hingeStile = 0.045f;
            const float handleStile = 0.065f;
            const float rail = 0.10f;
            var black = SurfaceMaterial.RackBlack;
            door.Box(black, 0f, bottom, -half, hingeStile, top, half);
            door.Box(black, leafWidth - handleStile, bottom, -half, leafWidth, top, half);
            door.Box(black, hingeStile, bottom, -half, leafWidth - handleStile, bottom + rail, half);
            door.Box(black, hingeStile, top - rail, -half, leafWidth - handleStile, top, half);
            // A shallow lip round the grille panel.
            door.Box(SurfaceMaterial.ServerBlack, hingeStile, bottom + rail, -half + 0.002f, hingeStile + 0.004f, top - rail, 0.004f);
            door.Box(SurfaceMaterial.ServerBlack, leafWidth - handleStile - 0.004f, bottom + rail, -half + 0.002f, leafWidth - handleStile, top - rail, 0.004f);

            float x0 = hingeStile + 0.004f;
            float x1 = leafWidth - handleStile - 0.004f;
            door.TexturedPanel(SurfaceMaterial.RackMesh, x0, bottom + rail, x1, top - rail, 0.004f, RackHexTile, RackHexTile * 1.732f);
        }

        private static void BuildRackItem(RackParts p, RackItem item, float y, float h, int index)
        {
            switch (item)
            {
                case RackItem.Blank: BuildBlank(p, y, h); break;
                case RackItem.Pdu: BuildPdu(p, y, h); break;
                case RackItem.Ups: BuildUps(p, y, h, index); break;
                case RackItem.Dell: BuildDellServer(p, y, h, index); break;
                case RackItem.Hp: BuildHpServer(p, y, h, index); break;
                default: BuildChassis(p, y, h, index); break;
            }
        }

        /// <summary>1 U blanking panel: black steel with two screws and a vent strip.</summary>
        private static void BuildBlank(RackParts p, float y, float h)
        {
            float z = ServerFront;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.03f, PanelHalf, y + h, z);
            foreach (int sign in new[] { -1, 1 })
            {
                p.Disc(SurfaceMaterial.ServerSilver, sign * (PanelHalf - 0.0095f), y + h / 2f, z + 0.0005f, 0.0035f, 8);
            }
            p.TexturedPanel(SurfaceMaterial.ServerSlots, -0.15f, y + 0.009f, 0.15f, y + h - 0.009f, z + 0.0005f, SlotCellWidth, SlotCellHeight);
        }

        /// <summary>1 U power strip: eight outlets, a display, a breaker.</summary>
        private static void BuildPdu(RackParts p, float y, float h)
        {
            float z = ServerFront;
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, y, z - 0.03f, PanelHalf, y + h, z + 0.002f);
            for (int i = 0; i < 8; i++)
            {
                float cx = -0.17f + i * 0.0385f;
                p.Box(SurfaceMaterial.Trunking, cx - 0.0145f, y + 0.008f, z + 0.002f, cx + 0.0145f, y + h - 0.008f, z + 0.0045f);
                p.Box(SurfaceMaterial.ServerBlack, cx - 0.0105f, y + 0.0125f, z + 0.0045f, cx + 0.0105f, y + h - 0.0125f, z + 0.0052f);
            }
            p.Box(SurfaceMaterial.ServerBlack, 0.155f, y + 0.008f, z + 0.002f, 0.225f, y + h - 0.008f, z + 0.004f);
            p.Box(SurfaceMaterial.SafetyGreen, 0.162f, y + 0.014f, z + 0.004f, 0.19f, y + h - 0.014f, z + 0.0046f);
            p.Box(SurfaceMaterial.LedRed, 0.2f, y + 0.014f, z + 0.004f, 0.216f, y + h - 0.014f, z + 0.0055f);
        }

        /// <summary>2 U rack UPS: a vented left half, an LCD, four buttons and a status LED.</summary>
        private static void BuildUps(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, y + h, z);
            p.Box(SurfaceMaterial.RackBlack, -PanelHalf, y, z - 0.002f, PanelHalf, y + h, z + 0.002f);
            p.TexturedPanel(SurfaceMaterial.ServerSlots, -0.215f, y + 0.012f, -0.02f, y + h - 0.012f, z + 0.0025f, SlotCellWidth, SlotCellHeight);
            p.Box(SurfaceMaterial.DriveBay, 0.02f, y + 0.02f, z + 0.002f, 0.145f, y + h - 0.02f, z + 0.004f);
            p.Box(SurfaceMaterial.SafetyGreen, 0.026f, y + 0.026f, z + 0.004f, 0.139f, y + h - 0.026f, z + 0.0046f);
            for (int i = 0; i < 4; i++)
            {
                p.Disc(SurfaceMaterial.ServerSilver, 0.162f + (i % 2) * 0.026f, y + 0.058f - (i / 2) * 0.03f, z + 0.002f, 0.0075f, 10);
            }
            p.Led(0.226f, y + h - 0.018f, index, z + 0.002f);
        }

        /// <summary>Dell R740xd: 2 U, 24 vertical 2.5" bays with orange rings and honeycomb vents, a control panel on the right.</summary>
        private static void BuildDellServer(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float top = y + h;
            float left = -PanelHalf + 0.034f;
            float right = PanelHalf - 0.052f;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, top, z - 0.004f);
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.004f, left, top, z);
            p.Box(SurfaceMaterial.ServerBlack, right, y, z - 0.004f, PanelHalf, top, z);
            p.Box(SurfaceMaterial.DriveBay, left, y + 0.003f, z - 0.004f, right, top - 0.003f, z - 0.001f);

            // Left ear: the information tag.
            p.Box(SurfaceMaterial.ServerSilver, -PanelHalf + 0.008f, top - 0.030f, z, -PanelHalf + 0.026f, top - 0.012f, z + 0.0008f);

            float pitch = (right - left - 0.003f) / 24f;
            float bayWidth = pitch - 0.0022f;
            for (int bay = 0; bay < 24; bay++)
            {
                float cx = left + 0.0015f + (bay + 0.5f) * pitch;
                float xa = cx - bayWidth / 2f;
                float xb = cx + bayWidth / 2f;
                p.Box(SurfaceMaterial.DriveBay, xa, y + 0.005f, z - 0.002f, xb, top - 0.005f, z + 0.0025f);
                p.Box(SurfaceMaterial.ServerBlack, xa, top - 0.017f, z + 0.0025f, xb, top - 0.005f, z + 0.0032f);
                p.Disc(SurfaceMaterial.LatchOrange, cx, top - 0.026f, z + 0.0033f, 0.0034f, 10);
                p.TexturedPanel(SurfaceMaterial.ServerHoneycomb, cx - 0.0053f, y + 0.014f, cx + 0.0053f, top - 0.036f, z + 0.0027f, HoneycombTile, HoneycombTile * 1.732f);
                p.Disc(SurfaceMaterial.ServerSilver, cx, y + 0.0095f, z + 0.0027f, 0.0032f, 8);
            }

            // Right control panel: power button, VGA, two USB ports and the LEDs.
            float px = PanelHalf - 0.036f;
            p.Disc(SurfaceMaterial.ServerSilver, px, top - 0.018f, z + 0.0005f, 0.0055f, 12);
            p.Box(SurfaceMaterial.ServerSilver, px - 0.013f, top - 0.050f, z, px + 0.013f, top - 0.036f, z + 0.001f);
            p.Box(SurfaceMaterial.DriveBay, px - 0.010f, top - 0.047f, z + 0.001f, px + 0.010f, top - 0.039f, z + 0.0014f);
            p.Box(SurfaceMaterial.ServerSilver, px - 0.007f, y + 0.016f, z, px + 0.007f, y + 0.023f, z + 0.001f);
            p.Box(SurfaceMaterial.ServerSilver, px - 0.007f, y + 0.005f, z, px + 0.007f, y + 0.012f, z + 0.001f);
            p.Led(px + 0.0235f, top - 0.012f, index);
        }

        /// <summary>HP ProLiant DL380p Gen8: 2 U, silver ears, 25 small-form-factor bays in 5 columns of 5 with red latches.</summary>
        private static void BuildHpServer(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float top = y + h;
            float left = -PanelHalf + 0.040f;
            float right = PanelHalf - 0.040f;
            p.Box(SurfaceMaterial.ServerSilver, -PanelHalf, y, z - 0.10f, PanelHalf, top, z - 0.004f);
            p.Box(SurfaceMaterial.ServerSilver, -PanelHalf, y, z - 0.004f, left, top, z + 0.002f);
            p.Box(SurfaceMaterial.ServerSilver, right, y, z - 0.004f, PanelHalf, top, z + 0.002f);
            p.Box(SurfaceMaterial.ServerBlack, left, y + 0.002f, z - 0.004f, right, top - 0.002f, z - 0.001f);

            // Left ear: VGA port and the Intel sticker.
            float lx = -PanelHalf + 0.020f;
            p.Box(SurfaceMaterial.ServerBlack, lx - 0.009f, top - 0.026f, z + 0.002f, lx + 0.009f, top - 0.016f, z + 0.0028f);
            p.Box(SurfaceMaterial.SupportBlue, lx - 0.008f, y + 0.012f, z + 0.002f, lx + 0.008f, y + 0.026f, z + 0.0026f);

            // The drive cage: five columns, separated by silver cage ribs.
            const int columns = 5;
            const int rows = 5;
            float rib = 0.004f;
            float columnWidth = (right - left - (columns + 1) * rib) / columns;
            float rowPitch = (h - 0.010f) / rows;
            for (int c = 0; c <= columns; c++)
            {
                float rx = left + c * (columnWidth + rib);
                p.Box(SurfaceMaterial.ServerSilver, rx, y + 0.002f, z - 0.002f, rx + rib, top - 0.002f, z + 0.0015f);
            }
            for (int c = 0; c < columns; c++)
            {
                float xa = left + rib + c * (columnWidth + rib);
                float xb = xa + columnWidth;
                for (int r = 0; r < rows; r++)
                {
                    float ya = y + 0.005f + r * rowPitch + 0.001f;
                    float yb = ya + rowPitch - 0.002f;
                    float cy = (ya + yb) / 2f;
                    p.Box(SurfaceMaterial.DriveBay, xa, ya, z - 0.002f, xb, yb, z + 0.003f);
                    p.Disc(SurfaceMaterial.ServerBlack, xa + 0.015f, cy, z + 0.0031f, 0.0045f, 12);
                    p.Box(SurfaceMaterial.LatchRed, xb - 0.0125f, ya + 0.0016f, z + 0.003f, xb - 0.0025f, yb - 0.0016f, z + 0.0042f);
                    p.Box(SurfaceMaterial.ServerBlack, xa + 0.0285f, cy - 0.0004f, z + 0.003f, xb - 0.016f, cy + 0.0004f, z + 0.0034f);
                }
            }

            // Right ear: power button, USB port, the LEDs and the logo plate.
            float rxm = PanelHalf - 0.020f;
            p.Box(SurfaceMaterial.ServerBlack, rxm - 0.006f, top - 0.020f, z + 0.002f, rxm + 0.006f, top - 0.008f, z + 0.0035f);
            p.Box(SurfaceMaterial.DriveBay, rxm - 0.008f, top - 0.036f, z + 0.002f, rxm + 0.008f, top - 0.028f, z + 0.0028f);
            p.Disc(SurfaceMaterial.ServerBlack, rxm, y + 0.017f, z + 0.0021f, 0.0075f, 12);
            p.Led(rxm - 0.0075f, top - 0.0135f, index, z + 0.0035f);
        }

        /// <summary>Inter-Tech 3 U chassis: black, two slotted vent panels, a middle panel with lock, USB and LEDs, and rack handles.</summary>
        private static void BuildChassis(RackParts p, float y, float h, int index)
        {
            float z = ServerFront;
            float top = y + h;
            p.Box(SurfaceMaterial.ServerBlack, -PanelHalf, y, z - 0.10f, PanelHalf, top, z - 0.006f);
            // Ear plates and the handles (a bar standing 30 mm out, joined to the ear at both ends).
            foreach (int sign in new[] { -1, 1 })
            {
                float outer = sign * PanelHalf;
                float inner = sign * (PanelHalf - 0.028f);
                p.Box(SurfaceMaterial.ServerBlack, Mathf.Min(outer, inner), y, z - 0.006f, Mathf.Max(outer, inner), top, z);
                float bar0 = sign * (PanelHalf - 0.020f);
                float bar1 = sign * (PanelHalf - 0.007f);
                p.Box(SurfaceMaterial.RackBlack, Mathf.Min(bar0, bar1), y + 0.004f, z + 0.018f, Mathf.Max(bar0, bar1), top - 0.004f, z + 0.030f);
                p.Box(SurfaceMaterial.RackBlack, Mathf.Min(bar0, bar1), y + 0.004f, z, Mathf.Max(bar0, bar1), y + 0.016f, z + 0.030f);
                p.Box(SurfaceMaterial.RackBlack, Mathf.Min(bar0, bar1), top - 0.016f, z, Mathf.Max(bar0, bar1), top - 0.004f, z + 0.030f);
                // The mounting screws' slots in the ear.
                float sx = sign * (PanelHalf - 0.0045f);
                for (int i = 0; i < 3; i++)
                {
                    float sy = y + h * (0.2f + 0.3f * i);
                    p.Box(SurfaceMaterial.DriveBay, sx - 0.0025f, sy - 0.006f, z + 0.0005f, sx + 0.0025f, sy + 0.006f, z + 0.001f);
                }
            }

            // Drive-bay covers: two black panels standing 3 mm proud, slotted.
            foreach (int sign in new[] { -1, 1 })
            {
                float x0 = sign < 0 ? -0.205f : 0.05f;
                float x1 = sign < 0 ? -0.05f : 0.205f;
                p.Box(SurfaceMaterial.RackBlack, x0, y + 0.006f, z - 0.004f, x1, top - 0.006f, z + 0.003f);
                p.TexturedPanel(SurfaceMaterial.ServerSlots, x0 + 0.01f, y + 0.014f, x1 - 0.01f, top - 0.014f, z + 0.0031f, SlotCellWidth, SlotCellHeight);
            }

            // Middle panel: key lock, two LEDs, two USB ports.
            p.Box(SurfaceMaterial.RackBlack, -0.038f, y + 0.002f, z - 0.004f, 0.038f, top - 0.002f, z + 0.005f);
            p.Disc(SurfaceMaterial.ServerSilver, 0f, top - 0.025f, z + 0.0051f, 0.0095f, 16);
            p.Disc(SurfaceMaterial.ServerBlack, 0f, top - 0.025f, z + 0.0056f, 0.0035f, 8);
            p.Box(SurfaceMaterial.ServerBlack, -0.0095f, top - 0.0465f, z + 0.0051f, 0.0095f, top - 0.0455f, z + 0.0053f);
            p.Led(-0.012f, y + 0.056f, index, z + 0.005f);
            p.Box(SurfaceMaterial.LedAmber, 0.0095f, y + 0.054f, z + 0.005f, 0.0145f, y + 0.058f, z + 0.007f);
            foreach (int sign in new[] { -1, 1 })
            {
                p.Box(SurfaceMaterial.ServerSilver, sign * 0.0155f - 0.0065f, y + 0.026f, z + 0.005f, sign * 0.0155f + 0.0065f, y + 0.0335f, z + 0.0057f);
            }
        }
    }
}
