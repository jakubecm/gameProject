using System.Collections.Generic;
using ColdShutdown.Core;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    public static partial class Props
    {
        public const float HallSignSize = 0.8f;

        /// <summary>
        /// A cube sign hanging on two rods from the roof deck. Each of the four faces has a panel and a world-space
        /// text with the same code (<see cref="ColdShutdown.Runtime.HallSign"/>), so it reads from every aisle. Tile signs are white,
        /// perimeter header signs (column letter or row number) yellow.
        /// </summary>
        private static void BuildHallSign(Transform root, SurfaceMaterial face)
        {
            float size = HallSignSize;
            float top = -HallSignDrop;
            float center = top - size / 2f;
            // Two steel rods from round ceiling plates down to eye bolts on the housing's top.
            var rods = new MeshBuilder();
            foreach (int side in new[] { -1, 1 })
            {
                float z = side * 0.3f;
                rods.AddTube(new[] { new Vector3(0f, top + 0.02f, z), new Vector3(0f, -0.004f, z) }, 0.006f, 8, Vector3.zero, Quaternion.identity);
                rods.AddLathe(new[] { P(0.035f, -0.006f, true), P(0.035f, 0f, true), P(0.0001f, 0f, true) }, 20, true, new Vector3(0f, 0f, z), Quaternion.identity);
                rods.AddLathe(new[] { P(0.0095f, -0.018f, true), P(0.0095f, -0.006f, true), P(0.0001f, -0.006f, true) }, 6, false, new Vector3(0f, 0f, z), Quaternion.identity);
                rods.AddLathe(new[] { P(0.016f, top, true), P(0.016f, top + 0.008f, true), P(0.0001f, top + 0.008f, true) }, 16, true, new Vector3(0f, 0f, z), Quaternion.identity);
            }
            rods.Attach("Rods", root, SurfaceMaterial.Chrome);
            ModelUtility.CreateBox("Housing", root, new Vector3(0f, center, 0f), Vector3.one * size, SurfaceMaterial.Upholstery, false);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var normals = new[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            string[] names = { "N", "E", "S", "W" };
            var texts = new Text[normals.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                var normal = normals[i];
                var panelSize = new Vector3(Mathf.Abs(normal.z) > 0f ? size - 0.08f : 0.01f, size - 0.08f, Mathf.Abs(normal.x) > 0f ? size - 0.08f : 0.01f);
                ModelUtility.CreateBox($"Panel_{names[i]}", root, new Vector3(0f, center, 0f) + normal * (size / 2f + 0.005f), panelSize, face, false);
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

        [Model(Prop.HallSign)]
        private static void BuildHallSign(Transform root) => BuildHallSign(root, SurfaceMaterial.SignFace);
    }
}
