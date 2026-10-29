using UnityEditor;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Command-line entry points for rebuilding the graybox content without opening the Editor UI:
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod ColdShutdown.Editor.RoomBatchTools.RebuildRoomsAndPreviews</c>.
    /// </summary>
    public static class RoomBatchTools
    {
        /// <summary>
        /// Rebuilds every prop, the hall kit, every room layout (logic and dressing), the room definitions, both
        /// preview scenes and the main scene (so its level builder knows every room type). Destructive like Rebuild Graybox
        /// Rooms and Rebuild Hall Kit; for graybox output only.
        /// </summary>
        public static void RebuildRoomsAndPreviews()
        {
            RoomShellTool.CreateRoomShell();
            RoomDressingTool.RebuildAll();
            HallKitTool.RebuildHallKit();
            AssetDatabase.SaveAssets();
            RoomPreviewSceneTool.CreateRoomPreviewScene();
            RoomPreviewSceneTool.CreateTestPreviewScene();
            MainSceneTool.CreateMainScene();
            AssetDatabase.SaveAssets();
        }
    }
}
