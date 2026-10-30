using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using static ColdShutdown.Editor.GrayboxMeshBuilder;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Props of the server hall and the CCTV system. Same pivot conventions as the room props; ceiling props of
    /// the hall hang from the roof deck (or a beam) and have a fixed drop, so they sit at the same height in
    /// every tile.
    /// </summary>
    public static partial class GrayboxPropsTool
    {
        /// <summary>Distance from the roof deck down to the top of a pendant light fitting (4.1 m above the floor).</summary>
        public const float PendantLightDrop = 1.9f;

        /// <summary>Distance from the roof deck down to the top of a hanging sign (4.2 m above the floor).</summary>
        public const float HallSignDrop = 1.8f;

        public const float HallSignSize = 0.8f;

        /// <summary>Length of the pole of a pendant CCTV dome, from its mount to the dome.</summary>
        public const float SecurityPendantDrop = 2.2f;

        // LED high-bay fittings, 4000 K, no shadows.
        private static readonly Color LedColor = new Color(1f, 0.97f, 0.92f);

        /// <summary>
        /// The hall's luminaire: the storage room's LED batten (<see cref="BuildLedBatten"/>, 1.5 m long along Z, its light a wide downward spot without shadows)
        /// hanging <see cref="PendantLightDrop"/> below the roof deck on two steel wire ropes, each with a rounded ceiling plate, a chrome cup with a cable
        /// gripper, a crimped ferrule and a thimble eye on a bracket that sits on the tray, and a round black power cable that curves down from a small junction
        /// box on the roof to a gland on the tray. Pivot on the roof deck, hanging down.
        /// </summary>
        private static void BuildPendantLight(Transform root)
        {
            float top = -PendantLightDrop;
            var lamp = GrayboxUtility.CreateGroup("Lamp", root);
            lamp.localPosition = new Vector3(0f, top, 0f);
            BuildLedBatten(lamp);
            var light = lamp.GetComponentInChildren<Light>();
            light.spotAngle = 140f;
            light.innerSpotAngle = 90f;
            light.range = 10f;
            light.intensity = 7f;
            light.color = LedColor;

            var zinc = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            var rope = new GrayboxMeshBuilder();
            var eyes = new GrayboxMeshBuilder();
            var black = new GrayboxMeshBuilder();
            var box = new GrayboxMeshBuilder();
            foreach (float z in new[] { -0.55f, 0.55f })
            {
                // Ceiling plate and the cup with the cable gripper under it.
                zinc.AddRoundedBlock(new Vector3(0f, -0.006f, z), 0.07f, 0.07f, 0.012f, 0.006f, 0.002f);
                chrome.AddLathe(new[] { P(0.026f, -0.006f, true), P(0.026f, -0.016f), P(0.016f, -0.028f), P(0.0075f, -0.032f, true), P(0.0075f, -0.056f, true), P(0.0035f, -0.058f, true) }, 24, true,
                    new Vector3(0f, 0f, z), Quaternion.identity);
                // The wire rope down to the lamp, a crimped ferrule round its end, and the thimble eye on the bracket.
                float eyeY = top + 0.022f;
                rope.AddTube(new[] { new Vector3(0f, -0.058f, z), new Vector3(0f, eyeY + 0.01f, z) }, 0.0018f, 8, Vector3.zero, Quaternion.identity);
                chrome.AddLathe(new[] { P(0.0042f, eyeY + 0.012f, true), P(0.0062f, eyeY + 0.016f), P(0.0062f, eyeY + 0.046f, true), P(0.0042f, eyeY + 0.050f, true) }, 16, true,
                    new Vector3(0f, 0f, z), Quaternion.identity);
                var ring = new List<Vector3>();
                for (int i = 0; i < 16; i++)
                {
                    float angle = i / 16f * 2f * Mathf.PI;
                    ring.Add(new Vector3(Mathf.Cos(angle) * 0.010f, eyeY + Mathf.Sin(angle) * 0.013f, z));
                }
                eyes.AddTube(ring, 0.0022f, 8, Vector3.zero, Quaternion.identity, true);
                // The bracket: a strap over the tray with the eye bolted on.
                zinc.AddExtrusion(RoundedRectLoop(-0.052f, 0.052f, z - 0.018f, z + 0.018f, 0.006f, 3), new Vector3(0f, top, 0f), new Vector3(0f, top + 0.004f, 0f),
                    Vector3.right, Vector3.forward, Vector3.down, Vector3.up);
                chrome.AddLathe(new[] { P(0.0035f, 0f, true), P(0.0035f, 0.014f, true), P(0.0001f, 0.014f, true) }, 10, true, new Vector3(0f, top + 0.004f, z), Quaternion.identity);
            }

            // Power cable: junction box on the roof, a gentle S down to the gland near the end of the tray.
            const float cableZ = -0.68f;
            box.AddRoundedBlock(new Vector3(0.06f, -0.04f, cableZ), 0.09f, 0.09f, 0.01f, 0.04f, 0.004f);
            black.AddLathe(new[] { P(0.012f, top, true), P(0.012f, top + 0.014f, true), P(0.0075f, top + 0.018f, true), P(0.0075f, top + 0.030f, true) }, 16, true, new Vector3(0f, 0f, cableZ), Quaternion.identity);
            black.AddTube(SmoothPath(new[]
            {
                new Vector3(0f, top + 0.030f, cableZ), new Vector3(0f, top + 0.12f, cableZ), new Vector3(0.03f, top * 0.5f, cableZ), new Vector3(0.06f, -0.16f, cableZ), new Vector3(0.06f, -0.04f, cableZ),
            }, 6), 0.0045f, 10, Vector3.zero, Quaternion.identity);

            zinc.Attach("Plates", root, SurfaceMaterial.ValveSteel);
            chrome.Attach("Cups", root, SurfaceMaterial.Chrome);
            rope.Attach("WireRopes", root, SurfaceMaterial.ValveSteel);
            eyes.Attach("Thimbles", root, SurfaceMaterial.Chrome);
            black.Attach("PowerCable", root, SurfaceMaterial.BlackHousing);
            box.Attach("JunctionBox", root, SurfaceMaterial.ElectricGrey);
        }

        /// <summary>
        /// A cube sign hanging on two rods from the roof deck. Each of the four faces has a panel and a world-space
        /// text with the same code (<see cref="HallSign"/>), so it reads from every aisle. Tile signs are white,
        /// perimeter header signs (column letter or row number) yellow.
        /// </summary>
        private static void BuildHallSign(Transform root, SurfaceMaterial face)
        {
            float size = HallSignSize;
            float top = -HallSignDrop;
            float center = top - size / 2f;
            // Two steel rods from round ceiling plates down to eye bolts on the housing's top.
            var rods = new GrayboxMeshBuilder();
            foreach (int side in new[] { -1, 1 })
            {
                float z = side * 0.3f;
                rods.AddTube(new[] { new Vector3(0f, top + 0.02f, z), new Vector3(0f, -0.004f, z) }, 0.006f, 8, Vector3.zero, Quaternion.identity);
                rods.AddLathe(new[] { P(0.035f, -0.006f, true), P(0.035f, 0f, true), P(0.0001f, 0f, true) }, 20, true, new Vector3(0f, 0f, z), Quaternion.identity);
                rods.AddLathe(new[] { P(0.0095f, -0.018f, true), P(0.0095f, -0.006f, true), P(0.0001f, -0.006f, true) }, 6, false, new Vector3(0f, 0f, z), Quaternion.identity);
                rods.AddLathe(new[] { P(0.016f, top, true), P(0.016f, top + 0.008f, true), P(0.0001f, top + 0.008f, true) }, 16, true, new Vector3(0f, 0f, z), Quaternion.identity);
            }
            rods.Attach("Rods", root, SurfaceMaterial.Chrome);
            GrayboxUtility.CreateBox("Housing", root, new Vector3(0f, center, 0f), Vector3.one * size, SurfaceMaterial.Upholstery, false);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var normals = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            string[] names = { "N", "E", "S", "W" };
            var texts = new Text[normals.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                var normal = normals[i];
                var panelSize = new Vector3(Mathf.Abs(normal.z) > 0f ? size - 0.08f : 0.01f, size - 0.08f, Mathf.Abs(normal.x) > 0f ? size - 0.08f : 0.01f);
                GrayboxUtility.CreateBox($"Panel_{names[i]}", root, new Vector3(0f, center, 0f) + normal * (size / 2f + 0.005f), panelSize, face, false);
                texts[i] = CreateSignText($"Text_{names[i]}", root, new Vector3(0f, center, 0f) + normal * (size / 2f + 0.012f), normal, font, size - 0.1f);
            }

            var sign = root.gameObject.AddComponent<HallSign>();
            var serialized = new SerializedObject(sign);
            var faces = serialized.FindProperty("faces");
            faces.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                faces.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A world-space canvas with one bold, centered text, facing along <paramref name="normal"/>.</summary>
        private static Text CreateSignText(string name, Transform parent, Vector3 position, Vector3 normal, Font font, float width)
        {
            const float units = 400f;
            var canvasObject = new GameObject(name, typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(units, units);
            rect.localPosition = position;
            // A canvas is read from the side its forward axis points away from.
            rect.localRotation = Quaternion.LookRotation(-normal, Vector3.up);
            rect.localScale = Vector3.one * (width / units);

            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(canvasObject.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 220;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.05f, 0.05f, 0.05f);
            text.raycastTarget = false;
            text.text = "--";
            return text;
        }

        /// <summary>Adds the disabled feed camera (looking along +Z, pitched down) and the <see cref="SecurityCamera"/> on the root.</summary>
        private static Camera AddSecurityCamera(Transform root, Vector3 lensPosition, float pitch)
        {
            var lensObject = new GameObject("FeedCamera");
            lensObject.transform.SetParent(root, false);
            lensObject.transform.localPosition = lensPosition;
            lensObject.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            var camera = lensObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 40f;
            camera.allowMSAA = false;
            camera.allowHDR = false;

            var securityCamera = root.gameObject.AddComponent<SecurityCamera>();
            var serialized = new SerializedObject(securityCamera);
            serialized.FindProperty("feedCamera").objectReferenceValue = camera;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return camera;
        }

        /// <summary>
        /// 55" display on a wall bracket. The screen is a quad (UVs 0-1), so <see cref="SecurityMonitor"/> can put a
        /// camera feed on it.
        /// </summary>
        private static void BuildVideoScreen(Transform root)
        {
            GrayboxUtility.CreateBox("Bracket", root, new Vector3(0f, 0f, 0.02f), new Vector3(0.4f, 0.3f, 0.04f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateBox("Bezel", root, new Vector3(0f, 0f, 0.0625f), new Vector3(1.23f, 0.71f, 0.045f), SurfaceMaterial.Upholstery, false);

            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Screen";
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            screen.transform.SetParent(root, false);
            screen.transform.localPosition = new Vector3(0f, 0f, 0.0855f);
            // A quad faces -Z; turned around it faces away from the wall.
            screen.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            screen.transform.localScale = new Vector3(1.2f, 0.675f, 1f);
            var renderer = screen.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GrayboxMaterials.Get(SurfaceMaterial.Screen);
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var monitor = root.gameObject.AddComponent<SecurityMonitor>();
            var serialized = new SerializedObject(monitor);
            serialized.FindProperty("screen").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Floor box 150 x 150 mm (after a product photo), flush with the floor: a dark hammered-pewter square cover plate with
        /// rounded corners, 6 mm proud at its rim, a recessed rectangular lid inside it with two hinge slots along the back edge
        /// (-Z) and a round chrome screw lock in the middle of the front edge (+Z). It lies under the back of the desk it feeds.
        /// </summary>
        private static void BuildFloorBox(Transform root)
        {
            const float size = 0.150f;
            var model = GrayboxUtility.CreateGroup("Model", root);
            // A slab's depth runs along +Z; stand it on the floor (depth up, its +Y edge towards -Z, the back).
            model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var plate = new GrayboxMeshBuilder();
            var lid = new GrayboxMeshBuilder();
            var dark = new GrayboxMeshBuilder();
            var chrome = new GrayboxMeshBuilder();
            plate.AddRoundedSlab(Vector3.zero, size, size, 0.010f, 0.0060f, 0.0015f, 4);
            // Inset lid, a millimetre below the rim, with a thin dark groove round it.
            dark.AddRoundedSlab(new Vector3(0f, 0f, 0.0058f), 0.118f, 0.118f, 0.004f, 0.0004f, 0.0001f, 3);
            lid.AddRoundedSlab(new Vector3(0f, 0f, 0.0058f), 0.114f, 0.114f, 0.003f, 0.0016f, 0.0006f, 3);
            // Hinge slots on the back edge (model +Y), the screw lock on the front edge (model -Y).
            foreach (float x in new[] { -0.040f, 0.040f })
            {
                dark.AddBox(new Vector3(x - 0.012f, 0.0525f, 0.0058f), new Vector3(x + 0.012f, 0.0545f, 0.0064f), true, false);
            }
            chrome.AddLathe(new[] { P(0f, 0.0080f), P(0.0045f, 0.0078f), P(0.0050f, 0.0074f, true), P(0.0050f, 0.0066f, true), P(0f, 0.0066f, true) }, 24, true,
                new Vector3(0f, -0.0455f, 0f), Quaternion.Euler(90f, 0f, 0f));
            plate.Attach("CoverPlate", model, SurfaceMaterial.FloorBoxMetal);
            lid.Attach("Lid", model, SurfaceMaterial.FloorBoxMetal);
            dark.Attach("Groove", model, SurfaceMaterial.BlackHousing);
            chrome.Attach("Lock", model, SurfaceMaterial.Chrome);
        }

        /// <summary>
        /// Illuminated emergency exit sign 360 x 160 mm, 50 mm deep: a white housing with the supplied design on its face (white
        /// "EXIT" on a green rounded panel with a white border), lit from inside (emissive). The face looks along +Z.
        /// </summary>
        private static void BuildExitSign(Transform root)
        {
            const float width = 0.36f;
            const float height = 0.16f;
            const float depth = 0.05f;
            var body = new GrayboxMeshBuilder();
            var face = new GrayboxMeshBuilder();
            body.AddBox(new Vector3(-width / 2f, -height / 2f, 0f), new Vector3(width / 2f, height / 2f, depth - 0.0012f));
            float x = width / 2f - 0.004f;
            float y = height / 2f - 0.004f;
            float z = depth;
            // Viewed from +Z the left is +X, so u runs from +X to -X.
            face.AddPanel(new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y, z), new Vector3(-x, y, z), Vector3.forward,
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            body.Attach("Body", root, SurfaceMaterial.Trunking);
            face.Attach("Face", root, SurfaceMaterial.ExitSignFace);
        }
    }
}
