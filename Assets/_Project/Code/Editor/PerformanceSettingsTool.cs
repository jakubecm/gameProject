using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Tools/Cold Shutdown/Apply Performance Settings: writes the render pipeline settings of the lighting setup (<see cref="WorldLighting.ApplyPipeline"/>,
    /// which Create Preview Scene runs too, so the numbers live in one place) without rebuilding the world. Of those, the ones that make the world
    /// cheaper to draw (measured in PERFORMANCE.md):
    /// <list type="bullet">
    /// <item>Screen space ambient occlusion draws the scene's depth, not depth and normals: it needs a pass over every mesh of the scene, and
    /// with depth and normals that pass was twice as heavy.</item>
    /// <item>GPU occlusion culling (part of the GPU Resident Drawer, which is already on): meshes hidden behind walls and racks are not drawn.
    /// The world is built at run time, so baked occlusion data is not an option.</item>
    /// </list>
    /// The frame cap is code (<c>FrameRateCap</c>), the racks' level of detail is on the rack (<c>Rack.AddLevelOfDetail</c>).
    /// </summary>
    public static class PerformanceSettingsTool
    {
        public const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        public const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";

        [MenuItem("Tools/Cold Shutdown/Apply Performance Settings")]
        public static void Apply()
        {
            if (WorldLighting.ApplyPipeline()) Debug.Log("Pipeline settings applied: no shadows, ambient occlusion from the depth, GPU occlusion culling on.");
        }
    }
}
