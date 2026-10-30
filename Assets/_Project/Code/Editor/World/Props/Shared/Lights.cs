using System;
using System.Collections.Generic;
using ColdShutdown.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static ColdShutdown.Editor.MeshBuilder;

namespace ColdShutdown.Editor
{
    /// <summary>The light colour and the realtime spot every light fitting carries.</summary>
    public static partial class Props
    {
        // LED panels and battens: 4000 K, no shadows (task 1.10 allows shadows on one or two lights only).
        private static readonly Color LedPanelColor = new Color(1f, 0.95f, 0.88f);

        /// <summary>A downward spot light without shadows (task 1.10 allows shadows on one or two lights only).</summary>
        internal static Light AddSpotLight(Transform root, Vector3 position, float angle, float innerAngle, float range, float intensity, Color color)
        {
            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = position;
            lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = angle;
            light.innerSpotAngle = innerAngle;
            light.range = range;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            return light;
        }
    }
}
