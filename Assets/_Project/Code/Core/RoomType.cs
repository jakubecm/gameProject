namespace ColdShutdown.Core
{
    /// <summary>
    /// Kind of room a cell can hold. Values are explicit so serialized assets stay valid
    /// when types are added; only ever append new values.
    /// </summary>
    public enum RoomType
    {
        Corridor = 0,
        ServerAisle = 1,
        Office = 2,
    }
}
