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
        // Every fitting is a cold white LED (8000 K): the horror look of the game. The light and the emissive diffuser of the fitting
        // (SurfaceMaterial.LightPanel) use this one colour, so a fitting always glows in the colour it lights with. Every light of the
        // level gets a small unique shift of its own at runtime (LightingDirector).
        internal static readonly Color FixtureColor = KelvinColor(8000f);

        // Total intensity of one fitting by zone: what its lights add up to (a long fitting spreads it over several lights).
        internal const float AisleLightIntensity = 4f;
        internal const float CorridorLightIntensity = 4f;
        internal const float LiftLightIntensity = 6f;
        internal const float RoomLightIntensity = 6f;
        internal const float HallLightIntensity = 8f;

        // A flat diffuse emitter (panel, batten, tube) shines about like a cosine: wide, with the edge fading smoothly.
        private const float FlatSpotAngle = 170f;
        private const float FlatSpotInnerAngle = 40f;

        /// <summary>
        /// Gives every fitting directly under <paramref name="group"/> the total intensity <paramref name="total"/>: the lights of one fitting
        /// share it in the proportions they have. Used where a zone needs a different level than the fitting's default (corridors).
        /// </summary>
        internal static void SetFixtureIntensity(Transform group, float total)
        {
            foreach (Transform fitting in group)
            {
                var lights = fitting.GetComponentsInChildren<Light>(true);
                float sum = 0f;
                foreach (var light in lights) sum += light.intensity;
                if (sum <= 0f) continue;
                foreach (var light in lights) light.intensity *= total / sum;
            }
        }

        /// <summary>The colour of a black body at <paramref name="kelvin"/> (1000-12000 K), the usual curve fit, normalised to a brightest channel of 1.</summary>
        internal static Color KelvinColor(float kelvin)
        {
            float t = Mathf.Clamp(kelvin, 1000f, 12000f) / 100f;
            float r = t <= 66f ? 255f : 329.698727446f * Mathf.Pow(t - 60f, -0.1332047592f);
            float g = t <= 66f ? 99.4708025861f * Mathf.Log(t) - 161.1195681661f : 288.1221695283f * Mathf.Pow(t - 60f, -0.0755148492f);
            float b = t >= 66f ? 255f : t <= 19f ? 0f : 138.5177312231f * Mathf.Log(t - 10f) - 305.0447927307f;
            return new Color(Mathf.Clamp01(r / 255f), Mathf.Clamp01(g / 255f), Mathf.Clamp01(b / 255f));
        }

        /// <summary>A downward spot light.</summary>
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
            light.lightmapBakeType = LightmapBakeType.Realtime;
            return light;
        }

        /// <summary>
        /// The light of a flat or long diffuse fitting: lights spread along its length (Z), one per 0.6 m (at least one), each with a wide,
        /// softly fading cone, sharing <paramref name="total"/>. A panel of 0.6 m is one light; a 1.5 m batten is three, so its pool of light is
        /// long like the fitting and not round.
        /// </summary>
        internal static void AddStripLight(Transform root, Vector3 centre, float length, float range, float total, Color color)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(length / 0.6f));
            float step = length / count;
            for (int i = 0; i < count; i++)
            {
                var position = centre + new Vector3(0f, 0f, (i - (count - 1) / 2f) * step);
                AddSpotLight(root, position, FlatSpotAngle, FlatSpotInnerAngle, range, total / count, color);
            }
        }
    }
}
