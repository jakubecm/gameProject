namespace ColdShutdown.Editor
{
    /// <summary>
    /// Command-line entry point of Create Preview Scene, for building without the Editor UI:
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod ColdShutdown.Editor.WorldBatch.Build</c>.
    /// </summary>
    public static class WorldBatch
    {
        public static void Build()
        {
            if (!WorldTool.RebuildEverything()) throw new System.Exception("The world map is not valid, see the log.");
        }
    }
}
