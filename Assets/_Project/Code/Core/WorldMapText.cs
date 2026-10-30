using System;
using System.Collections.Generic;

namespace ColdShutdown.Core
{
    /// <summary>
    /// Reads the one world map from text: one line per row of cells, north at the top, one character per cell (spaces between
    /// them are optional, lines starting with <c>#</c> are comments). <c>.</c> is an empty cell; the others are
    /// <see cref="RoomTypeExtensions.Symbol"/>: <c>C</c> corridor, <c>D</c> corridor with a door into the hall next to it, <c>E</c> lift
    /// end (the start), <c>O</c> office, <c>G</c> security office, <c>P</c> power room, <c>V</c> cooling plant, <c>S</c> storage room,
    /// <c>H</c> hall.
    ///
    /// Everything else follows from the neighbours, so a map is only the arrangement of rooms:
    /// - neighbouring corridor cells connect; a corridor's shape (cross, T, straight, corner, dead end) and turn come from its
    ///   connections, and its layout puts the fire kit in an arm that leads into a room or the hall;
    /// - a room and the lift end have one door, into a neighbouring corridor (north first, then south, west, east if several touch it);
    /// - the hall is one rectangle of <c>H</c> cells; only a <c>D</c> corridor opens into it (a door in the hall wall); rooms never do;
    /// - exactly one lift end; every room must be reachable from it.
    /// </summary>
    public static class WorldMapText
    {
        /// <summary>The outcome of <see cref="Parse"/>: the layout (null if there are errors), errors and warnings in plain words.</summary>
        public sealed class Result
        {
            public RoomLayout Layout { get; internal set; }

            public List<string> Errors { get; } = new List<string>();

            public List<string> Warnings { get; } = new List<string>();

            public bool IsValid => Layout != null && Errors.Count == 0;
        }

        private static readonly RoomType[] CorridorShapes =
        {
            RoomType.CorridorCross, RoomType.CorridorT, RoomType.CorridorStraight, RoomType.CorridorCorner, RoomType.CorridorDeadEnd,
        };

        // The side a room's door takes when several corridors touch it.
        private static readonly Direction[] DoorPreference = { Direction.North, Direction.South, Direction.West, Direction.East };

        /// <summary>The map character of a corridor cell that has a door into the hall next to it.</summary>
        public const char HallDoorCorridor = 'D';

        private enum Kind { None, Corridor, Lift, Room, Hall }

        /// <summary>The room type of a map character, or null for <c>.</c>; <c>C</c> gives the cross (the real shape comes later).</summary>
        public static RoomType? TypeOf(char symbol)
        {
            if (symbol == HallDoorCorridor) return RoomType.CorridorCross;
            foreach (RoomType type in Enum.GetValues(typeof(RoomType)))
            {
                if (type.Symbol() == symbol) return RoomRules.IsCorridor(type) && type != RoomType.LiftEnd ? RoomType.CorridorCross : type;
            }
            return null;
        }

        public static Result Parse(string text)
        {
            var result = new Result();
            var lines = new List<string>();
            foreach (var raw in (text ?? string.Empty).Split('\n'))
            {
                var line = raw.Replace(" ", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty);
                if (line.Length == 0 || line[0] == '#') continue;
                lines.Add(line);
            }
            if (lines.Count == 0)
            {
                result.Errors.Add("The map is empty.");
                return result;
            }

            int height = lines.Count;
            int width = 0;
            foreach (var line in lines) width = Math.Max(width, line.Length);
            var types = new RoomType?[width * height];
            var kinds = new Kind[width * height];
            var hallDoors = new bool[width * height];
            for (int row = 0; row < height; row++)
            {
                var line = lines[height - 1 - row];
                for (int column = 0; column < line.Length; column++)
                {
                    char symbol = line[column];
                    if (symbol == '.') continue;
                    var type = TypeOf(symbol);
                    if (!type.HasValue)
                    {
                        result.Errors.Add($"Unknown character '{symbol}' at {Cell(column, row)}.");
                        continue;
                    }
                    types[row * width + column] = type;
                    hallDoors[row * width + column] = symbol == HallDoorCorridor;
                    kinds[row * width + column] = type == RoomType.CorridorCross ? Kind.Corridor : type == RoomType.LiftEnd ? Kind.Lift
                        : type == RoomType.Hall ? Kind.Hall : Kind.Room;
                }
            }

            Kind KindAt(int column, int row) => column >= 0 && row >= 0 && column < width && row < height ? kinds[row * width + column] : Kind.None;
            var map = new MapData(width, height);

            // Corridor to corridor, hall to hall, and a D corridor to the hall.
            bool HallDoorAt(int column, int row) => KindAt(column, row) == Kind.Corridor && hallDoors[row * width + column];
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    var here = KindAt(column, row);
                    foreach (var side in new[] { Direction.East, Direction.North })
                    {
                        int otherColumn = column + side.ColumnOffset(), otherRow = row + side.RowOffset();
                        var there = KindAt(otherColumn, otherRow);
                        bool connect = (here == Kind.Corridor && there == Kind.Corridor) || (here == Kind.Hall && there == Kind.Hall)
                            || (here == Kind.Hall && HallDoorAt(otherColumn, otherRow)) || (there == Kind.Hall && HallDoorAt(column, row));
                        if (connect) map.Connect(column, row, side);
                    }
                }
            }

            // Rooms and the lift end: one door into a neighbouring corridor.
            int lifts = 0;
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    var here = KindAt(column, row);
                    if (here != Kind.Room && here != Kind.Lift) continue;
                    if (here == Kind.Lift) lifts++;
                    var corridors = new List<Direction>();
                    foreach (var side in DoorPreference)
                    {
                        if (KindAt(column + side.ColumnOffset(), row + side.RowOffset()) == Kind.Corridor) corridors.Add(side);
                    }
                    string name = types[row * width + column].Value.ToString();
                    if (corridors.Count == 0)
                    {
                        result.Errors.Add($"{name} at {Cell(column, row)} has no corridor next to it.");
                        continue;
                    }
                    if (corridors.Count > 1)
                    {
                        result.Warnings.Add($"{name} at {Cell(column, row)} touches {corridors.Count} corridors; its door faces {corridors[0]}.");
                    }
                    map.Connect(column, row, corridors[0]);
                }
            }
            if (lifts != 1) result.Errors.Add($"The map needs exactly one lift end (E), it has {lifts}.");

            CheckHall(result, kinds, width, height);

            // Shapes, turns and layouts.
            var rooms = new PlacedRoom?[width * height];
            bool IsRoom(int column, int row)
            {
                var kind = KindAt(column, row);
                return kind == Kind.Room || kind == Kind.Hall;
            }
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    var kind = KindAt(column, row);
                    if (kind == Kind.None) continue;
                    var type = types[row * width + column].Value;
                    int connected = RoomLayout.ConnectedSides(map, column, row);
                    if (kind == Kind.Hall)
                    {
                        rooms[row * width + column] = new PlacedRoom(column, row, RoomType.Hall, 0);
                        continue;
                    }
                    if (kind == Kind.Corridor)
                    {
                        if (connected == 0)
                        {
                            result.Errors.Add($"Corridor at {Cell(column, row)} leads nowhere.");
                            continue;
                        }
                        type = Array.Find(CorridorShapes, shape => RoomRules.FittingTurns(shape, connected).Count > 0);
                        var options = RoomLayout.FireArmOptions(map, column, row, type, IsRoom);
                        var (turns, layout) = options.Count > 0 ? options[0] : (RoomRules.FittingTurns(type, connected)[0], 0);
                        rooms[row * width + column] = new PlacedRoom(column, row, type, turns, layout);
                        continue;
                    }
                    if (connected == 0) continue;
                    rooms[row * width + column] = new PlacedRoom(column, row, type, RoomRules.FittingTurns(type, connected)[0]);
                }
            }
            if (result.Errors.Count > 0) return result;

            var layoutResult = new RoomLayout(map, rooms);
            var (startColumn, startRow) = layoutResult.StartCell;
            int reachable = map.CountReachableCells(startColumn, startRow);
            if (reachable < layoutResult.Rooms.Count)
            {
                result.Errors.Add($"Only {reachable} of {layoutResult.Rooms.Count} cells can be reached from the lift end.");
                return result;
            }
            result.Layout = layoutResult;
            return result;
        }

        /// <summary>The hall, if any, must be one filled rectangle of <c>H</c>.</summary>
        private static void CheckHall(Result result, Kind[] kinds, int width, int height)
        {
            int minColumn = int.MaxValue, minRow = int.MaxValue, maxColumn = -1, maxRow = -1;
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    if (kinds[row * width + column] != Kind.Hall) continue;
                    minColumn = Math.Min(minColumn, column);
                    minRow = Math.Min(minRow, row);
                    maxColumn = Math.Max(maxColumn, column);
                    maxRow = Math.Max(maxRow, row);
                }
            }
            if (maxColumn < 0) return;
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    if (kinds[row * width + column] != Kind.Hall)
                    {
                        result.Errors.Add($"The hall must be one rectangle of H; {Cell(column, row)} is missing.");
                        return;
                    }
                }
            }
        }

        /// <summary>A cell as the designer reads it: column from the west, row from the south, both from 0.</summary>
        private static string Cell(int column, int row) => $"cell ({column}, {row})";
    }
}
