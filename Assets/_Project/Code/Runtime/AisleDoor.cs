namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The automatic sliding door at either end of a contained cold aisle in the server hall. It moves like a room door
    /// (<see cref="AutoSlidingDoor"/>: photo cells, reader, lock) but is its own type with its own prop (a black-framed glass door in the
    /// aisle enclosure), so aisle doors and room doors can be found, locked and changed apart (the access rules of the aisles are game logic of task 2.x).
    /// </summary>
    public class AisleDoor : AutoSlidingDoor
    {
    }
}
