using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// The settings the realtime lighting needs outside the code: the room look of a scene (ambient, no fog) and the PC render pipeline
    /// (shadows off, ambient occlusion) and quality level (no realtime reflection probes). Run by Create Preview Scene, so the
    /// numbers live here and nowhere else. What the lights do at runtime is <see cref="ColdShutdown.Runtime.LightingDirector"/>.
    /// </summary>
    internal static class WorldLighting
    {
        private const string PipelineAssetPath = "Assets/Settings/PC_RPAsset.asset";
        private const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string QualitySettingsPath = "ProjectSettings/QualitySettings.asset";
        private const string QualityLevelName = "PC";

        /// <summary>
        /// The rooms are lit only by their own light fittings. Without baked bounce light, a trilight ambient stands in for it: surfaces
        /// facing down (the ceiling) get the light the floor reflects. There is no fog.
        /// </summary>
        internal static void ApplyRoomLook()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.07f, 0.08f, 0.10f);
            RenderSettings.ambientEquatorColor = new Color(0.14f, 0.155f, 0.185f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.30f, 0.34f);

            // No fog: it blends far lit surfaces into the dark, so the lights ahead looked dimmer than the ones around the player.
            RenderSettings.fog = false;
        }

        /// <summary>
        /// One pipeline setting for every graphics card. No shadows at all (no shadow maps are allocated); ambient occlusion at full
        /// resolution; realtime reflection probes off in the PC quality level (there is no probe). Returns false if an asset is missing.
        /// </summary>
        internal static bool ApplyPipeline()
        {
            bool ok = true;

            var pipeline = AssetDatabase.LoadMainAssetAtPath(PipelineAssetPath);
            if (pipeline == null) ok = Missing(PipelineAssetPath);
            else
            {
                var serialized = new SerializedObject(pipeline);
                Set(serialized, "m_MainLightShadowsSupported", 0);
                Set(serialized, "m_AdditionalLightShadowsSupported", 0);
                Set(serialized, "m_SoftShadowsSupported", 0);
                Save(serialized, pipeline);
            }

            Object occlusion = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(RendererPath))
            {
                if (asset != null && asset.name == "ScreenSpaceAmbientOcclusion") occlusion = asset;
            }
            if (occlusion == null) ok = Missing($"{RendererPath} (ScreenSpaceAmbientOcclusion)");
            else
            {
                var serialized = new SerializedObject(occlusion);
                Set(serialized, "m_Settings.Downsample", 0);
                Set(serialized, "m_Settings.Intensity", 0.5f);
                Set(serialized, "m_Settings.Radius", 0.3f);
                Set(serialized, "m_Settings.DirectLightingStrength", 0.25f);
                Save(serialized, occlusion);
            }

            var quality = AssetDatabase.LoadMainAssetAtPath(QualitySettingsPath);
            if (quality == null) ok = Missing(QualitySettingsPath);
            else
            {
                var serialized = new SerializedObject(quality);
                var levels = serialized.FindProperty("m_QualitySettings");
                for (int i = 0; levels != null && i < levels.arraySize; i++)
                {
                    var level = levels.GetArrayElementAtIndex(i);
                    if (level.FindPropertyRelative("name").stringValue != QualityLevelName) continue;
                    Set(level, "realtimeReflectionProbes", 0);
                    Set(level, "shadows", 0);
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            return ok;
        }

        private static bool Missing(string what)
        {
            Debug.LogWarning($"Lighting setup skipped: {what} not found.");
            return false;
        }

        private static void Save(SerializedObject serialized, Object asset)
        {
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void Set(SerializedObject serialized, string path, float value) => Write(serialized.FindProperty(path), path, value);

        private static void Set(SerializedProperty parent, string path, float value) => Write(parent.FindPropertyRelative(path), path, value);

        private static void Write(SerializedProperty property, string path, float value)
        {
            if (property == null)
            {
                Debug.LogWarning($"Lighting setup: property {path} does not exist in this Unity version.");
                return;
            }
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float: property.floatValue = value; break;
                case SerializedPropertyType.Boolean: property.boolValue = value != 0f; break;
                default: property.intValue = Mathf.RoundToInt(value); break;
            }
        }
    }
}
