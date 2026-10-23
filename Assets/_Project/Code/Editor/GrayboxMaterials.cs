using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// The surface kinds rooms and props are built from. Each one is a single shared material asset, so
    /// final textures (task 4.6) and datacenter profiles (task 4.4) change one material, not every mesh.
    /// Only append new values; the names are part of the asset paths.
    /// </summary>
    public enum SurfaceMaterial
    {
        Floor,
        FloorVent,
        Wall,
        Ceiling,
        Trim,
        Metal,
        Furniture,
        Upholstery,
        Screen,
        LightPanel,
        Whiteboard,
        SafetyRed,
        CeilingGrid,
        Trunking,
        PowerCable,
        DataCable,
        DataTray,
        PipeWater,
        PipeGas,
    }

    /// <summary>
    /// Creates the shared surface materials in <c>Art/Materials</c> as plain URP Lit greys (plus the
    /// colours real installations use: red fire water, green water, yellow gas, yellow data trays).
    /// Existing materials are never touched, except by an explicit rebuild.
    /// </summary>
    internal static class GrayboxMaterials
    {
        public const string Folder = "Assets/_Project/Art/Materials";

        private readonly struct Look
        {
            public readonly Color Color;
            public readonly float Metallic;
            public readonly float Smoothness;
            public readonly Color Emission;

            public Look(float grey, float metallic, float smoothness) : this(new Color(grey, grey, grey), metallic, smoothness) { }

            public Look(Color color, float metallic, float smoothness, Color emission = default)
            {
                Color = color;
                Metallic = metallic;
                Smoothness = smoothness;
                Emission = emission;
            }
        }

        // Different grey values keep surfaces apart in the graybox; the real look comes in phase 4.
        private static readonly Dictionary<SurfaceMaterial, Look> Looks = new Dictionary<SurfaceMaterial, Look>
        {
            { SurfaceMaterial.Floor, new Look(0.42f, 0f, 0.35f) },
            { SurfaceMaterial.FloorVent, new Look(0.28f, 0.5f, 0.4f) },
            { SurfaceMaterial.Wall, new Look(0.72f, 0f, 0.2f) },
            { SurfaceMaterial.Ceiling, new Look(0.82f, 0f, 0.05f) },
            { SurfaceMaterial.Trim, new Look(0.5f, 0.3f, 0.4f) },
            { SurfaceMaterial.Metal, new Look(0.55f, 0.8f, 0.5f) },
            { SurfaceMaterial.Furniture, new Look(0.62f, 0f, 0.3f) },
            { SurfaceMaterial.Upholstery, new Look(0.18f, 0f, 0.1f) },
            { SurfaceMaterial.Screen, new Look(0.04f, 0f, 0.85f) },
            // Fluorescent tubes: emissive so the fitting reads as lit; the light itself is a Light component.
            { SurfaceMaterial.LightPanel, new Look(Color.white, 0f, 0.5f, new Color(1f, 0.96f, 0.9f) * 1.5f) },
            { SurfaceMaterial.Whiteboard, new Look(0.9f, 0f, 0.7f) },
            { SurfaceMaterial.SafetyRed, new Look(new Color(0.7f, 0.08f, 0.06f), 0.1f, 0.6f) },
            { SurfaceMaterial.CeilingGrid, new Look(0.9f, 0.4f, 0.4f) },
            { SurfaceMaterial.Trunking, new Look(0.93f, 0f, 0.35f) },
            { SurfaceMaterial.PowerCable, new Look(0.06f, 0f, 0.3f) },
            { SurfaceMaterial.DataCable, new Look(new Color(0.12f, 0.3f, 0.75f), 0f, 0.4f) },
            { SurfaceMaterial.DataTray, new Look(new Color(0.9f, 0.72f, 0.1f), 0.3f, 0.5f) },
            { SurfaceMaterial.PipeWater, new Look(new Color(0.15f, 0.5f, 0.25f), 0.2f, 0.6f) },
            { SurfaceMaterial.PipeGas, new Look(new Color(0.92f, 0.78f, 0.1f), 0.2f, 0.6f) },
        };

        public static string PathOf(SurfaceMaterial surface) => $"{Folder}/M_{surface}.mat";

        /// <summary>Loads the material, creating it first if it does not exist yet.</summary>
        public static Material Get(SurfaceMaterial surface)
        {
            string path = PathOf(surface);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = $"M_{surface}" };
            ApplyLook(material, Looks[surface]);
            Directory.CreateDirectory(Folder);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Resets every material to its graybox look, keeping the asset (and its GUID). Rebuild only.</summary>
        public static void ResetAll()
        {
            foreach (var pair in Looks)
            {
                var material = Get(pair.Key);
                ApplyLook(material, pair.Value);
                EditorUtility.SetDirty(material);
            }
        }

        private static void ApplyLook(Material material, Look look)
        {
            material.SetColor("_BaseColor", look.Color);
            material.SetFloat("_Metallic", look.Metallic);
            material.SetFloat("_Smoothness", look.Smoothness);
            if (look.Emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", look.Emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
