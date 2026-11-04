using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Tools/Cold Shutdown/Apply Performance Settings: the two render pipeline settings that make the world cheaper to draw, written to the
    /// assets so they do not depend on anybody's Inspector. Safe to run again.
    /// <list type="bullet">
    /// <item>Screen space ambient occlusion keeps working but draws the scene's depth (not depth and normals) and at half resolution:
    /// it needs a pass over every mesh of the scene, and with depth and normals that pass was twice as heavy.</item>
    /// <item>GPU occlusion culling (part of the GPU Resident Drawer, which is already on): meshes hidden behind walls and racks are not drawn.
    /// The world is built at run time, so baked occlusion data is not an option.</item>
    /// </list>
    /// The frame cap is code (<c>FrameRateCap</c>), the racks' level of detail is on the rack (<c>Rack.AddLevelOfDetail</c>).
    /// </summary>
    public static class PerformanceSettingsTool
    {
        public const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        public const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";

        /// <summary>The values of <c>ScreenSpaceAmbientOcclusionSettings.DepthSource</c>.</summary>
        private const int DepthSource = 0;

        [MenuItem("Tools/Cold Shutdown/Apply Performance Settings")]
        public static void Apply()
        {
            var feature = FindAmbientOcclusion();
            if (feature != null)
            {
                var serialized = new SerializedObject(feature);
                serialized.FindProperty("m_Settings.Source").intValue = DepthSource;
                serialized.FindProperty("m_Settings.Downsample").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feature);
            }
            else
            {
                Debug.LogWarning($"No ScreenSpaceAmbientOcclusion feature in {RendererPath}; nothing to change there.");
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PipelinePath);
            if (pipeline != null)
            {
                var serialized = new SerializedObject(pipeline);
                serialized.FindProperty("m_GPUResidentDrawerMode").intValue = 1;
                serialized.FindProperty("m_GPUResidentDrawerEnableOcclusionCullingInCameras").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }
            else
            {
                Debug.LogError($"{PipelinePath} is missing.");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Performance settings applied: ambient occlusion from depth at half resolution, GPU occlusion culling on.");
        }

        /// <summary>The ambient occlusion feature of the PC renderer, or null.</summary>
        internal static Object FindAmbientOcclusion() =>
            AssetDatabase.LoadAllAssetsAtPath(RendererPath).FirstOrDefault(asset => asset != null && asset.name == "ScreenSpaceAmbientOcclusion");
    }
}
