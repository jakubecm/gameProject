using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        /// <summary>Along the wall on the other side: the reader beside the frame, inside the 3 m wide corridor arm.</summary>
        public const float OuterReaderAlongWall = 1.37f;

        public const float ReaderHeight = 1.1f;

        /// <summary>
        /// Two glass leaves, side by side in the opening, after a product photo of an automatic sliding door: a slim frame of stainless
        /// steel (30 mm stiles, a 50 mm top rail, a 45 mm bottom rail) round one clear glass pane. Each leaf has its own collider.
        /// </summary>
        private static Transform[] BuildLeaves(Transform root, float width, float height, float offset, bool single = false, SurfaceMaterial frameSurface = SurfaceMaterial.StainlessSteel)
        {
            const float frame = 0.03f;
            const float depth = 0.03f;
                        float leafWidth = width / 2f;
            if (single) leafWidth = width;
            var leaves = new Transform[single ? 1 : 2];
            for (int i = 0; i < leaves.Length; i++)
            {
                var leaf = ModelUtility.CreateGroup(single ? "Leaf" : i == 0 ? "Leaf_Left" : "Leaf_Right", root);
                leaf.localPosition = new Vector3(single ? 0f : (i == 0 ? -1f : 1f) * leafWidth / 2f, 0f, offset + depth / 2f);
                var steel = new MeshBuilder();
                var glass = new MeshBuilder();
                float half = leafWidth / 2f;
                float d = depth / 2f;
                // Stiles: the outer one a plain bar, the one where the leaves meet chamfered on both faces (6 mm), so the dividing line
                // shows between the two leaves although they touch.
                const float chamfer = 0.006f;
                var outerStile = i == 0 ? new Vector2(-half, -half + frame) : new Vector2(half - frame, half);
                steel.AddBox(new Vector3(outerStile.x, 0.005f, -d), new Vector3(outerStile.y, height, d));
                var meeting = i == 0
                    ? new[] { new Vector2(half - frame, -d), new Vector2(half - chamfer, -d), new Vector2(half, -d + chamfer), new Vector2(half, d - chamfer), new Vector2(half - chamfer, d), new Vector2(half - frame, d) }
                    : new[] { new Vector2(-half + chamfer, -d), new Vector2(-half + frame, -d), new Vector2(-half + frame, d), new Vector2(-half + chamfer, d), new Vector2(-half, d - chamfer), new Vector2(-half, -d + chamfer) };
                if (single) steel.AddBox(new Vector3(half - frame, 0.005f, -d), new Vector3(half, height, d));
                else steel.AddPrism(meeting, 0.005f, height, Vector3.zero);
                steel.AddBox(new Vector3(-half + frame, height - 0.05f, -d), new Vector3(half - frame, height, d));
                steel.AddBox(new Vector3(-half + frame, 0.005f, -d), new Vector3(half - frame, 0.05f, d));
                glass.AddBox(new Vector3(-half + frame, 0.05f, -0.005f), new Vector3(half - frame, height - 0.05f, 0.005f));
                steel.Attach("Frame", leaf, frameSurface);
                glass.Attach("Glass", leaf, SurfaceMaterial.Glass);
                AddBoxCollider(leaf, new Vector3(0f, height / 2f, 0f), new Vector3(leafWidth, height, depth));
                leaves[i] = leaf;
            }
            return leaves;
        }

        private static void ConfigureDoor(Transform root, Transform[] leaves, float travel, Renderer[] leds, Vector3 sensorCenter, Vector3 sensorSize) =>
            ConfigureDoor<AutoSlidingDoor>(root, leaves, travel, leds, sensorCenter, sensorSize);

        private static void ConfigureDoor<T>(Transform root, Transform[] leaves, float travel, Renderer[] leds, Vector3 sensorCenter, Vector3 sensorSize) where T : AutoSlidingDoor
        {
            var door = root.gameObject.AddComponent<T>();
            var serialized = new SerializedObject(door);
            var leafProperty = serialized.FindProperty("leaves");
            leafProperty.arraySize = leaves.Length;
            for (int i = 0; i < leaves.Length; i++)
            {
                leafProperty.GetArrayElementAtIndex(i).objectReferenceValue = leaves[i];
            }
            serialized.FindProperty("travel").floatValue = travel;
            serialized.FindProperty("sensorCenter").vector3Value = sensorCenter;
            serialized.FindProperty("sensorSize").vector3Value = sensorSize;
            var ledProperty = serialized.FindProperty("readerLeds");
            ledProperty.arraySize = leds.Length;
            for (int i = 0; i < leds.Length; i++)
            {
                ledProperty.GetArrayElementAtIndex(i).objectReferenceValue = leds[i];
            }
            serialized.FindProperty("unlockedLed").objectReferenceValue = Surfaces.Get(SurfaceMaterial.SafetyGreen);
            serialized.FindProperty("lockedLed").objectReferenceValue = Surfaces.Get(SurfaceMaterial.LedRed);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The passenger lift, which is also the evacuation lift, seen from the corridor: a wall prop on the corridor face
        /// of the landing wall at the end of the lift end tile (<see cref="RoomContract.LiftWallFace"/>; +Z into the
        /// corridor). The landing wall and the shaft are room structure; this prop is everything else (sizes from
        /// <see cref="RoomContract"/>):
        /// - a two-panel centre-opening stainless door 1.1 x 2.1 m: the landing panels behind the wall and the car panels
        ///   behind them move together, like a real lift, and open by themselves (<see cref="AutoSlidingDoor"/>) for anyone
        ///   standing in front of the door or just inside the car;
        /// - the car for 6 to 8 people (1.6 x 1.5 m inside, 2.3 m high): stainless walls, a mirror on the back wall,
        ///   handrails, a rubber floor, a lit ceiling and the car operating panel; the player starts every run in it;
        /// - on the landing: a stainless frame, the sill, the call buttons, the floor indicator, the exit sign over the door
        ///   (the lift is the way out), the evacuation lift sign (EN 81-72).
        /// </summary>
        /// <summary>Segments (a top, b top right, c bottom right, d bottom, e bottom left, f top left, g middle) of the seven-segment digits.</summary>
        private static readonly Dictionary<char, string> SevenSegment = new Dictionary<char, string>
        {
            { '0', "abcdef" }, { '1', "bc" }, { '2', "abdeg" }, { '3', "abcdg" }, { '4', "bcfg" },
            { '5', "acdfg" }, { '6', "acdefg" }, { '7', "abc" }, { '8', "abcdefg" }, { '9', "abcdfg" }, { '-', "g" },
        };

        /// <summary>
        /// Seven-segment text (digits and minus) of the given character <paramref name="height"/> in the XY plane around
        /// <paramref name="centre"/>, standing <paramref name="depth"/> proud along +Z. Seen from +Z the text reads towards -X (the
        /// viewer's right), like every sign face in these props.
        /// </summary>
        private static void AddSegmentText(MeshBuilder builder, string text, Vector3 centre, float height, float depth = 0.001f)
        {
            float w = 0.55f * height;
            float gap = 0.25f * height;
            float t = 0.14f * height;
            float total = text.Length * w + (text.Length - 1) * gap;
            for (int i = 0; i < text.Length; i++)
            {
                float cx = centre.x + total / 2f - w / 2f - i * (w + gap);
                float cy = centre.y;
                float left = cx + w / 2f;
                float right = cx - w / 2f;
                float top = cy + height / 2f;
                float bottom = cy - height / 2f;
                void Seg(float x0, float x1, float y0, float y1) =>
                    builder.AddBox(new Vector3(Mathf.Min(x0, x1), y0, centre.z), new Vector3(Mathf.Max(x0, x1), y1, centre.z + depth), true, true);
                foreach (char segment in SevenSegment[text[i]])
                {
                    switch (segment)
                    {
                        case 'a': Seg(right + t * 0.5f, left - t * 0.5f, top - t, top); break;
                        case 'b': Seg(right, right + t, cy + t * 0.5f, top - t * 0.5f); break;
                        case 'c': Seg(right, right + t, bottom + t * 0.5f, cy - t * 0.5f); break;
                        case 'd': Seg(right + t * 0.5f, left - t * 0.5f, bottom, bottom + t); break;
                        case 'e': Seg(left - t, left, bottom + t * 0.5f, cy - t * 0.5f); break;
                        case 'f': Seg(left - t, left, cy + t * 0.5f, top - t * 0.5f); break;
                        case 'g': Seg(right + t * 0.5f, left - t * 0.5f, cy - t / 2f, cy + t / 2f); break;
                    }
                }
            }
        }

        /// <summary>The floor number the lift shows, in the car and on the landing indicator.</summary>
        public const string LiftFloorText = "-4";

        /// <summary>
        /// RFID access reader, 55 x 125 x 20 mm: a black rounded housing with a bevelled edge and a vertical slot near the top
        /// whose light strip shows the lock state (green unlocked, red locked: the door swaps its material). The pivot is the
        /// middle of the back at <paramref name="position"/>, facing yaw.
        /// </summary>
        private static Renderer BuildReader(Transform root, string name, Vector3 position, float yaw)
        {
            const float width = 0.055f;
            const float height = 0.125f;
            const float depth = 0.020f;
            var reader = ModelUtility.CreateGroup(name, root);
            reader.localPosition = position;
            reader.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var body = new MeshBuilder();
            var slot = new MeshBuilder();
            var light = new MeshBuilder();

            body.AddRoundedSlab(Vector3.zero, width, height, 0.007f, depth, 0.0035f, 4);
            slot.AddBox(new Vector3(-0.0036f, 0.014f, depth - 0.0002f), new Vector3(0.0036f, 0.056f, depth + 0.0003f), true, false);
            light.AddBox(new Vector3(-0.0022f, 0.017f, depth + 0.0003f), new Vector3(0.0022f, 0.053f, depth + 0.0007f), true, false);

            body.Attach($"{name}_Body", reader, SurfaceMaterial.ReaderBlack);
            slot.Attach($"{name}_Slot", reader, SurfaceMaterial.Screen);
            var led = light.Attach($"{name}_Led", reader, SurfaceMaterial.SafetyGreen);
            return led.GetComponent<MeshRenderer>();
        }
    }
}
