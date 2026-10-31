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
        /// <summary>
        /// 55" display on a wall bracket. The screen is a quad (UVs 0-1), so <see cref="SecurityMonitor"/> can put a
        /// camera feed on it.
        /// </summary>
        [Model(Prop.VideoScreen)]
        private static void BuildVideoScreen(Transform root)
        {
            ModelUtility.CreateBox("Bracket", root, new Vector3(0f, 0f, 0.02f), new Vector3(0.4f, 0.3f, 0.04f), SurfaceMaterial.Metal, false);
            ModelUtility.CreateBox("Bezel", root, new Vector3(0f, 0f, 0.0625f), new Vector3(1.23f, 0.71f, 0.045f), SurfaceMaterial.Upholstery, false);

            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Screen";
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            screen.transform.SetParent(root, false);
            screen.transform.localPosition = new Vector3(0f, 0f, 0.0855f);
            // A quad faces -Z; turned around it faces away from the wall.
            screen.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            screen.transform.localScale = new Vector3(1.2f, 0.675f, 1f);
            var renderer = screen.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Surfaces.Get(SurfaceMaterial.Screen);
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            var monitor = root.gameObject.AddComponent<SecurityMonitor>();
            var serialized = new SerializedObject(monitor);
            serialized.FindProperty("screen").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
