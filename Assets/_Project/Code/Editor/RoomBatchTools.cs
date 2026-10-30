using UnityEditor;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Command-line entry points for rebuilding the graybox content without opening the Editor UI:
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod ColdShutdown.Editor.RoomBatchTools.BuildPreviewSceneB</c>.
    /// In the Editor the same build is Tools/Cold Shutdown/Create Preview Scene.
    /// </summary>
    public static class RoomBatchTools
    {
        /// <summary>Everything <c>Sandbox_B</c> is made of, then the scene (<see cref="RoomPreviewSceneTool.RebuildEverything"/>).</summary>
        public static void BuildPreviewSceneB() => RoomPreviewSceneTool.RebuildEverything();

        /// <summary>
        /// <see cref="BuildPreviewSceneB"/>, then the <c>TestPreview</c> scene even when it does not exist yet, and the main scene (so
        /// its level builder knows every room type). Destructive; for graybox output only.
        /// </summary>
        public static void RebuildRoomsAndPreviews()
        {
            RoomPreviewSceneTool.RebuildEverything();
            RoomPreviewSceneTool.CreateTestPreviewScene();
            MainSceneTool.CreateMainScene();
            AssetDatabase.SaveAssets();
        }
    }
}
