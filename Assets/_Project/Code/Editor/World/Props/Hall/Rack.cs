using System;
using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Width of a 19" panel.</summary>
        private const float PanelHalf = 0.2413f;

        /// <summary>Depth of the door's collider in front of the body's (the door leaf from z = 0.576 to the rack front at 0.6).</summary>
        private const float DoorThickness = 0.024f;

        private const float DoorAjarDegrees = 22f;

        /// <summary>The meshes of one part of a prop, one builder per surface material.</summary>
        private sealed class RackParts
        {
            private readonly Dictionary<SurfaceMaterial, MeshBuilder> builders = new Dictionary<SurfaceMaterial, MeshBuilder>();

            public MeshBuilder this[SurfaceMaterial material]
            {
                get
                {
                    if (!builders.TryGetValue(material, out var builder))
                    {
                        builders[material] = builder = new MeshBuilder();
                    }
                    return builder;
                }
            }

            public void Box(SurfaceMaterial material, float x0, float y0, float z0, float x1, float y1, float z1) =>
                this[material].AddBox(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1));

            /// <summary>A rectangular ring (four bars of width <paramref name="t"/>) round the box x0..x1, y0..y1, from z0 to z1.</summary>
            public void Frame(SurfaceMaterial material, float x0, float y0, float x1, float y1, float z0, float z1, float t)
            {
                Box(material, x0, y0, z0, x0 + t, y1, z1);
                Box(material, x1 - t, y0, z0, x1, y1, z1);
                Box(material, x0 + t, y0, z0, x1 - t, y0 + t, z1);
                Box(material, x0 + t, y1 - t, z0, x1 - t, y1, z1);
            }

            /// <summary>
            /// The inside of a hole in a front plate: four walls and a back wall facing into the opening (seen through it), from <paramref name="zFront"/>
            /// back to <paramref name="zBack"/>.
            /// </summary>
            public void Cavity(SurfaceMaterial material, float xa, float ya, float xb, float yb, float zFront, float zBack)
            {
                var m = this[material];
                m.AddPanel(new Vector3(xa, ya, zBack), new Vector3(xb, ya, zBack), new Vector3(xb, yb, zBack), new Vector3(xa, yb, zBack), Vector3.forward,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                m.AddPanel(new Vector3(xa, ya, zFront), new Vector3(xa, ya, zBack), new Vector3(xa, yb, zBack), new Vector3(xa, yb, zFront), Vector3.right,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                m.AddPanel(new Vector3(xb, ya, zFront), new Vector3(xb, ya, zBack), new Vector3(xb, yb, zBack), new Vector3(xb, yb, zFront), Vector3.left,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                m.AddPanel(new Vector3(xa, ya, zFront), new Vector3(xb, ya, zFront), new Vector3(xb, ya, zBack), new Vector3(xa, ya, zBack), Vector3.up,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
                m.AddPanel(new Vector3(xa, yb, zFront), new Vector3(xb, yb, zFront), new Vector3(xb, yb, zBack), new Vector3(xa, yb, zBack), Vector3.down,
                    Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
            }

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
                    list.Add(Surfaces.Get(pair.Key));
                }
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(instances.ToArray(), false, false);
                foreach (var instance in instances) UnityEngine.Object.DestroyImmediate(instance.mesh);
                mesh.RecalculateBounds();
                materials = list.ToArray();
                return mesh;
            }
        }

        [Model(Prop.Rack)]
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

            // The door collider and the door logic only work on an openable rack (Rack.Configure switches them). The door logic sits on the
            // hinge with the door's own collider, and the body's collider stops behind the door, so only looking at the door offers to open it.
            AddBoxCollider(hinge, new Vector3(leafWidth / 2f, (plinth + top) / 2f, 0f), new Vector3(leafWidth, top - plinth, DoorThickness));
            var doorCollider = hinge.GetComponent<BoxCollider>();
            doorCollider.enabled = false;
            float bodyFront = depth / 2f - DoorThickness;
            AddBoxCollider(root, new Vector3(0f, height / 2f, (back + bodyFront) / 2f), new Vector3(width, height, bodyFront - back));

            var rackDoor = hinge.gameObject.AddComponent<RackDoor>();
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
    }
}
