using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
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
        public const float SecurityPendantDrop = 1.9f;

        // LED high-bay fittings, 4000 K, no shadows.
        private static readonly Color LedColor = new Color(1f, 0.97f, 0.92f);

        /// <summary>
        /// Linear LED high-bay luminaire 1.3 m long (along Z), hanging on two wires from the roof deck. Its light
        /// is a wide downward spot without shadows.
        /// </summary>
        private static void BuildPendantLight(Transform root)
        {
            const float length = 1.3f;
            float top = -PendantLightDrop;
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateBoxBetween(side < 0 ? "Wire_S" : "Wire_N", root, new Vector3(-0.003f, top, side * 0.5f - 0.003f),
                    new Vector3(0.003f, 0f, side * 0.5f + 0.003f), SurfaceMaterial.Metal);
            }
            GrayboxUtility.CreateBox("Body", root, new Vector3(0f, top - 0.04f, 0f), new Vector3(0.2f, 0.08f, length), SurfaceMaterial.CeilingGrid, false);
            GrayboxUtility.CreateBox("Diffuser", root, new Vector3(0f, top - 0.085f, 0f), new Vector3(0.16f, 0.01f, length - 0.04f), SurfaceMaterial.LightPanel, false);

            AddSpotLight(root, new Vector3(0f, top - 0.1f, 0f), 140f, 90f, 10f, 7f, LedColor);
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
            foreach (int side in new[] { -1, 1 })
            {
                GrayboxUtility.CreateBoxBetween(side < 0 ? "Rod_S" : "Rod_N", root, new Vector3(-0.008f, top, side * 0.3f - 0.008f),
                    new Vector3(0.008f, 0f, side * 0.3f + 0.008f), SurfaceMaterial.Metal);
            }
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

        /// <summary>
        /// Ceiling dome camera for the 3 m rooms: a flat round base on the ceiling and a smoked half-sphere bubble under
        /// it, the camera inside.
        /// </summary>
        private static void BuildSecurityDome(Transform root)
        {
            GrayboxUtility.CreateCylinder("Base", root, new Vector3(0f, -0.01f, 0f), 0.08f, 0.02f, SurfaceMaterial.Trunking);
            GrayboxUtility.CreateDome("Bubble", root, new Vector3(0f, -0.02f, 0f), 0.065f, SurfaceMaterial.Screen);
            AddSecurityCamera(root, new Vector3(0f, -0.05f, 0f), 30f);
        }

        /// <summary>
        /// Wall dome camera without an arm: a round back plate on the wall and the smoked dome on it, looking along +Z
        /// and 25° down. A small wall fitting: the pivot is the center of the back plate. Its cable leaves the plate
        /// upward; the dressers run it in a conduit up to the ceiling.
        /// </summary>
        private static void BuildSecurityWallDome(Transform root)
        {
            var plate = GrayboxUtility.CreateCylinder("Plate", root, new Vector3(0f, 0f, 0.01f), 0.085f, 0.02f, SurfaceMaterial.Trunking);
            plate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var bubble = GrayboxUtility.CreateDome("Bubble", root, new Vector3(0f, 0f, 0.02f), 0.07f, SurfaceMaterial.Screen);
            // The dome bulges down (-Y); turned so it bulges away from the wall (+Z).
            bubble.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            AddSecurityCamera(root, new Vector3(0f, -0.01f, 0.06f), 25f);
        }

        /// <summary>Height above the pivot where the cable of a wall dome leaves its back plate.</summary>
        public const float WallDomeCableHeight = 0.085f;

        /// <summary>
        /// Pendant dome camera for the hall: a junction box clamped to a beam, a pole with the cable inside, and
        /// the dome at its end, so it looks over the racks.
        /// </summary>
        private static void BuildSecurityPendant(Transform root)
        {
            float bottom = -SecurityPendantDrop;
            GrayboxUtility.CreateBox("JunctionBox", root, new Vector3(0f, -0.04f, 0f), new Vector3(0.15f, 0.08f, 0.15f), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreatePipe("Pole", root, new Vector3(0f, -0.08f, 0f), new Vector3(0f, bottom, 0f), 0.025f, SurfaceMaterial.Trunking);
            GrayboxUtility.CreateCylinder("Housing", root, new Vector3(0f, bottom - 0.03f, 0f), 0.09f, 0.06f, SurfaceMaterial.Trunking);
            GrayboxUtility.CreateDome("Bubble", root, new Vector3(0f, bottom - 0.06f, 0f), 0.085f, SurfaceMaterial.Screen);
            AddSecurityCamera(root, new Vector3(0f, bottom - 0.1f, 0f), 35f);
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

        /// <summary>19" equipment cabinet 0.6 x 2.0 x 0.8 m with a glass door (NVR, PoE switches, UPS); the front faces +Z.</summary>
        private static void BuildEquipmentCabinet(Transform root)
        {
            const float width = 0.6f;
            const float height = 2.0f;
            const float depth = 0.8f;
            GrayboxUtility.CreateStandingBox("Body", root, Vector3.zero, new Vector3(width, height, depth), SurfaceMaterial.Upholstery, false);
            float[] units = { 0.3f, 0.55f, 0.9f, 1.05f, 1.2f, 1.5f };
            for (int i = 0; i < units.Length; i++)
            {
                GrayboxUtility.CreateStandingBox($"Unit_{i}", root, new Vector3(0f, units[i], depth / 2f + 0.002f), new Vector3(0.48f, i % 2 == 0 ? 0.13f : 0.044f, 0.004f), i % 3 == 0 ? SurfaceMaterial.Screen : SurfaceMaterial.Metal, false);
            }
            GrayboxUtility.CreateStandingBox("Door", root, new Vector3(0f, 0.05f, depth / 2f + 0.02f), new Vector3(width - 0.02f, height - 0.1f, 0.01f), SurfaceMaterial.Glass, false);
            GrayboxUtility.CreateStandingBox("Handle", root, new Vector3(0.24f, 0.95f, depth / 2f + 0.035f), new Vector3(0.02f, 0.15f, 0.02f), SurfaceMaterial.Metal, false);
            AddBoxCollider(root, new Vector3(0f, height / 2f, 0.02f), new Vector3(width, height, depth + 0.04f));
        }

        /// <summary>
        /// Small floor box 150 x 150 mm flush with the floor (a 5 mm lid with a cable outlet), feeding one desk away from
        /// the walls. It lies under the back of the desk, between its side panels, so the desk never stands on it.
        /// </summary>
        private static void BuildFloorBox(Transform root)
        {
            GrayboxUtility.CreateStandingBox("Lid", root, Vector3.zero, new Vector3(0.15f, 0.005f, 0.15f), SurfaceMaterial.Metal, false);
            GrayboxUtility.CreateStandingBox("CableOutlet", root, new Vector3(0f, 0.005f, -0.06f), new Vector3(0.08f, 0.003f, 0.02f), SurfaceMaterial.Upholstery, false);
        }

        /// <summary>Illuminated emergency exit sign (green pictogram panel), 360 x 160 mm.</summary>
        private static void BuildExitSign(Transform root)
        {
            GrayboxUtility.CreateBox("Body", root, new Vector3(0f, 0f, 0.025f), new Vector3(0.36f, 0.16f, 0.05f), SurfaceMaterial.Trunking, false);
            GrayboxUtility.CreateBox("Face", root, new Vector3(0f, 0f, 0.0515f), new Vector3(0.34f, 0.14f, 0.003f), SurfaceMaterial.SafetyGreen, false);
        }
    }
}
