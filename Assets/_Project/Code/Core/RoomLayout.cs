using System;
using System.Collections.Generic;

namespace ColdShutdown.Core
{
    /// <summary>
    /// A room on the grid: its cell, its type, which of the type's fixed layouts it uses (0 to
    /// <see cref="RoomRules.LayoutCount"/> - 1) and how far it is turned (in quarter turns around +Y).
    /// </summary>
    public readonly struct PlacedRoom
    {
        public readonly int Column;
        public readonly int Row;
        public readonly RoomType Type;
        public readonly int QuarterTurns;
        public readonly int Layout;

        public PlacedRoom(int column, int row, RoomType type, int quarterTurns, int layout = 0)
        {
            Column = column;
            Row = row;
            Type = type;
            QuarterTurns = quarterTurns;
            Layout = layout;
        }

        public float YawDegrees => QuarterTurns * RoomContract.RotationStepDegrees;
    }

    /// <summary>
    /// The map plus the room placed in each of its cells (a cell may be empty). Plain data made by <see cref="WorldMapText"/>; a level
    /// builder only has to turn it into objects.
    /// </summary>
    public sealed class RoomLayout
    {
        private readonly PlacedRoom?[] rooms;
        private readonly List<PlacedRoom> placed = new List<PlacedRoom>();

        public MapData Map { get; }

        /// <summary>Whether the layout has a lift end, where the player starts (<see cref="StartCell"/>).</summary>
        public bool HasLift { get; }

        /// <summary>The cell the player starts in: the lift end, otherwise the middle of the map.</summary>
        public (int column, int row) StartCell { get; }

        public RoomLayout(MapData map, PlacedRoom?[] rooms)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            if (rooms == null || rooms.Length != map.Width * map.Height) throw new ArgumentException("One entry per cell is needed.", nameof(rooms));
            this.rooms = rooms;
            StartCell = (map.Width / 2, map.Height / 2);
            foreach (var room in rooms)
            {
                if (!room.HasValue) continue;
                placed.Add(room.Value);
                if (room.Value.Type == RoomType.LiftEnd && !HasLift)
                {
                    HasLift = true;
                    StartCell = (room.Value.Column, room.Value.Row);
                }
            }
        }

        /// <summary>Every placed room, row by row from the south-west corner.</summary>
        public IReadOnlyList<PlacedRoom> Rooms => placed;

        public bool HasRoom(int column, int row) => Map.InBounds(column, row) && rooms[row * Map.Width + column].HasValue;

        public PlacedRoom GetRoom(int column, int row)
        {
            if (!HasRoom(column, row)) throw new ArgumentOutOfRangeException($"Cell ({column}, {row}) has no room.");
            return rooms[row * Map.Width + column].Value;
        }

        /// <summary>
        /// The turns and layouts with which a corridor of <paramref name="type"/> fits its cell and has its fire arm
        /// (<see cref="RoomRules.FireArmSide"/>) towards a connected neighbour for which <paramref name="isRoom"/> holds.
        /// Empty if no arm leads into a room.
        /// </summary>
        public static List<(int turns, int layout)> FireArmOptions(MapData map, int column, int row, RoomType type, Func<int, int, bool> isRoom)
        {
            var options = new List<(int turns, int layout)>();
            foreach (int turns in RoomRules.FittingTurns(type, ConnectedSides(map, column, row)))
            {
                for (int layout = 0; layout < RoomRules.LayoutCount(type); layout++)
                {
                    int fire = RoomRules.Rotate(RoomRules.Mask(RoomRules.FireArmSide(type, layout)), turns);
                    for (int d = 0; d < DirectionExtensions.Count; d++)
                    {
                        var side = (Direction)d;
                        if (fire != RoomRules.Mask(side) || !map.IsConnected(column, row, side)) continue;
                        if (isRoom(column + side.ColumnOffset(), row + side.RowOffset())) options.Add((turns, layout));
                    }
                }
            }
            return options;
        }

        /// <summary>Mask of the connected sides of a cell (see <see cref="RoomRules"/>).</summary>
        public static int ConnectedSides(MapData map, int column, int row)
        {
            int mask = 0;
            for (int d = 0; d < DirectionExtensions.Count; d++)
            {
                if (map.IsConnected(column, row, (Direction)d)) mask |= RoomRules.Mask((Direction)d);
            }
            return mask;
        }
    }
}
