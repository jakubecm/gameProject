using System;
using System.Collections.Generic;

namespace ColdShutdown.Core
{
    /// <summary>A room on the grid: its cell, its type and how far it is turned (in quarter turns around +Y).</summary>
    public readonly struct PlacedRoom
    {
        public readonly int Column;
        public readonly int Row;
        public readonly RoomType Type;
        public readonly int QuarterTurns;

        public PlacedRoom(int column, int row, RoomType type, int quarterTurns)
        {
            Column = column;
            Row = row;
            Type = type;
            QuarterTurns = quarterTurns;
        }

        public float YawDegrees => QuarterTurns * RoomContract.RotationStepDegrees;
    }

    /// <summary>
    /// A map plus the room placed in every cell. Plain data, so the same seed always gives the same layout
    /// and a level builder only has to turn it into objects.
    /// </summary>
    public sealed class RoomLayout
    {
        private readonly PlacedRoom[] rooms;

        public MapData Map { get; }

        internal RoomLayout(MapData map, PlacedRoom[] rooms)
        {
            Map = map;
            this.rooms = rooms;
        }

        public IReadOnlyList<PlacedRoom> Rooms => rooms;

        public PlacedRoom GetRoom(int column, int row)
        {
            if (!Map.InBounds(column, row)) throw new ArgumentOutOfRangeException($"Cell ({column}, {row}) is outside the map.");
            return rooms[row * Map.Width + column];
        }

        /// <summary>
        /// PLACEHOLDER weight of a room type, until task 2.1 (room types, quotas, entry and gateway) replaces
        /// the random choice with real quotas. Roughly: half corridors, a third server aisles, a few offices.
        /// A type without an entry here counts as 10.
        /// </summary>
        public static int PlaceholderWeight(RoomType type)
        {
            switch (type)
            {
                case RoomType.Corridor: return 50;
                case RoomType.ServerAisle: return 35;
                case RoomType.Office: return 15;
                default: return 10;
            }
        }

        /// <summary>
        /// PLACEHOLDER until task 2.1: every cell gets a random type from <paramref name="availableTypes"/>,
        /// weighted by <see cref="PlaceholderWeight"/>, and a random quarter turn. Deterministic for a seed.
        /// </summary>
        public static RoomLayout Generate(MapData map, int seed, IReadOnlyList<RoomType> availableTypes)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (availableTypes == null || availableTypes.Count == 0) throw new ArgumentException("At least one room type is needed.", nameof(availableTypes));

            int totalWeight = 0;
            foreach (var type in availableTypes)
            {
                totalWeight += PlaceholderWeight(type);
            }

            // A separate stream from the map's own, so changing how rooms are chosen never changes the map.
            var random = new Random(unchecked(seed * 31 + 17));
            var rooms = new PlacedRoom[map.Width * map.Height];
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    var type = PickType(availableTypes, random.Next(totalWeight));
                    int turns = random.Next(4);
                    rooms[row * map.Width + column] = new PlacedRoom(column, row, type, turns);
                }
            }
            return new RoomLayout(map, rooms);
        }

        private static RoomType PickType(IReadOnlyList<RoomType> availableTypes, int roll)
        {
            foreach (var type in availableTypes)
            {
                roll -= PlaceholderWeight(type);
                if (roll < 0) return type;
            }
            return availableTypes[availableTypes.Count - 1];
        }
    }
}
