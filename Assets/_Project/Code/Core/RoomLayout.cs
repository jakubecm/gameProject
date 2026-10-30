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
    /// A map plus the room placed in every cell. Plain data, so the same seed always gives the same layout
    /// and a level builder only has to turn it into objects.
    /// </summary>
    public sealed class RoomLayout
    {
        private readonly PlacedRoom[] rooms;

        public MapData Map { get; }

        /// <summary>Whether the layout has a lift end, where the player starts (<see cref="StartCell"/>).</summary>
        public bool HasLift { get; }

        /// <summary>
        /// The cell the player starts in: the lift end if there is one, otherwise the middle of the map, so every
        /// direction is open to explore.
        /// </summary>
        public (int column, int row) StartCell { get; }

        internal RoomLayout(MapData map, PlacedRoom[] rooms, (int column, int row)? lift)
        {
            Map = map;
            this.rooms = rooms;
            HasLift = lift.HasValue;
            StartCell = lift ?? (map.Width / 2, map.Height / 2);
        }

        public IReadOnlyList<PlacedRoom> Rooms => rooms;

        public PlacedRoom GetRoom(int column, int row)
        {
            if (!Map.InBounds(column, row)) throw new ArgumentOutOfRangeException($"Cell ({column}, {row}) is outside the map.");
            return rooms[row * Map.Width + column];
        }

        /// <summary>
        /// PLACEHOLDER weight of a room type, until task 2.1 (room types, quotas, entry and gateway) replaces
        /// the random choice with real quotas. Only the types that fit a cell compete for it (see
        /// <see cref="RoomRules.FittingTurns"/>): corridor shapes match the connections exactly, small rooms need a
        /// dead end. Hall tiles only make sense as a connected hall, so the
        /// random placeholder never picks them. A type without an entry here counts as 10.
        /// </summary>
        public static int PlaceholderWeight(RoomType type)
        {
            switch (type)
            {
                case RoomType.Corridor:
                case RoomType.CorridorStraight:
                case RoomType.CorridorT:
                case RoomType.CorridorCorner:
                case RoomType.CorridorDeadEnd:
                    return 50;
                case RoomType.SmallOffice: return 8;
                case RoomType.SecurityOffice: return 4;
                case RoomType.PowerRoom: return 4;
                case RoomType.HvacRoom: return 4;
                case RoomType.StorageRoom: return 4;
                case RoomType.Hall:
                case RoomType.Elevator:
                    return 0;
                default: return 10;
            }
        }

        /// <summary>
        /// PLACEHOLDER until task 2.1: every cell gets a random type from <paramref name="availableTypes"/> among
        /// those that fit its connections, weighted by <see cref="PlaceholderWeight"/>, turned so its doors face the
        /// connected sides. If no type fits, the cross corridor is used (the builder plugs its unused arms), or, without
        /// one, any available type. Every room gets one of its type's fixed layouts at random. Deterministic for a seed.
        ///
        /// If the lift end is available, exactly one is placed, where the player starts: in the dead end (one
        /// connection) nearest the middle of the map (<see cref="LiftCell"/>). Corridors then get the turn and layout
        /// whose fire arm (<see cref="RoomRules.FireArmSide"/>) leads into a room, if one of their arms does.
        /// </summary>
        public static RoomLayout Generate(MapData map, int seed, IReadOnlyList<RoomType> availableTypes)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (availableTypes == null || availableTypes.Count == 0) throw new ArgumentException("At least one room type is needed.", nameof(availableTypes));

            // A separate stream from the map's own, so changing how rooms are chosen never changes the map.
            var random = new Random(unchecked(seed * 31 + 17));
            // Layouts have their own stream too, so they never change which types are chosen.
            var layouts = new Random(unchecked(seed * 53 + 29));
            var rooms = new PlacedRoom[map.Width * map.Height];
            var candidates = new List<(RoomType type, List<int> turns)>();
            var lift = Contains(availableTypes, RoomType.Elevator) ? LiftCell(map) : null;
            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    int connected = ConnectedSides(map, column, row);
                    if (lift.HasValue && lift.Value == (column, row))
                    {
                        var liftTurns = RoomRules.FittingTurns(RoomType.Elevator, connected);
                        rooms[row * map.Width + column] = new PlacedRoom(column, row, RoomType.Elevator, liftTurns.Count > 0 ? liftTurns[0] : 0);
                        continue;
                    }
                    candidates.Clear();
                    int totalWeight = 0;
                    foreach (var type in availableTypes)
                    {
                        int weight = PlaceholderWeight(type);
                        if (weight <= 0) continue;
                        var turns = RoomRules.FittingTurns(type, connected);
                        if (turns.Count == 0) continue;
                        candidates.Add((type, turns));
                        totalWeight += weight;
                    }

                    RoomType chosen;
                    int quarterTurns;
                    if (candidates.Count > 0)
                    {
                        int roll = random.Next(totalWeight);
                        var pick = candidates[candidates.Count - 1];
                        foreach (var candidate in candidates)
                        {
                            roll -= PlaceholderWeight(candidate.type);
                            if (roll < 0)
                            {
                                pick = candidate;
                                break;
                            }
                        }
                        chosen = pick.type;
                        quarterTurns = pick.turns[random.Next(pick.turns.Count)];
                    }
                    else
                    {
                        chosen = Contains(availableTypes, RoomType.Corridor) ? RoomType.Corridor : Fallback(availableTypes, random);
                        quarterTurns = random.Next(4);
                    }
                    int layout = layouts.Next(RoomRules.LayoutCount(chosen));
                    rooms[row * map.Width + column] = new PlacedRoom(column, row, chosen, quarterTurns, layout);
                }
            }
            TurnFireArmsToRooms(map, rooms, seed);
            return new RoomLayout(map, rooms, lift);
        }

        /// <summary>
        /// The cell for the lift end: the dead end (one connection) nearest the middle of the map, the first in row
        /// order on a tie; on a map without dead ends a cell without connections, otherwise none.
        /// </summary>
        public static (int column, int row)? LiftCell(MapData map)
        {
            (int column, int row)? best = null;
            double bestDistance = double.MaxValue;
            foreach (int wanted in new[] { 1, 0 })
            {
                for (int row = 0; row < map.Height; row++)
                {
                    for (int column = 0; column < map.Width; column++)
                    {
                        if (RoomRules.BitCount(ConnectedSides(map, column, row)) != wanted) continue;
                        double dx = column - (map.Width - 1) / 2.0;
                        double dy = row - (map.Height - 1) / 2.0;
                        double distance = dx * dx + dy * dy;
                        if (distance < bestDistance - 1e-9)
                        {
                            bestDistance = distance;
                            best = (column, row);
                        }
                    }
                }
                if (best.HasValue) return best;
            }
            return null;
        }

        /// <summary>
        /// Gives every corridor the turn and layout whose fire arm leads into a room (not another corridor), chosen at
        /// random among those that do; a corridor with no room next to it keeps what it has.
        /// </summary>
        private static void TurnFireArmsToRooms(MapData map, PlacedRoom[] rooms, int seed)
        {
            // Its own stream, so the fire arms never change the rooms.
            var random = new Random(unchecked(seed * 71 + 37));
            for (int i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                if (!RoomRules.IsCorridor(room.Type)) continue;
                var options = FireArmOptions(map, room.Column, room.Row, room.Type, (c, r) => !RoomRules.IsCorridor(rooms[r * map.Width + c].Type));
                if (options.Count == 0) continue;
                var (turns, layout) = options[random.Next(options.Count)];
                rooms[i] = new PlacedRoom(room.Column, room.Row, room.Type, turns, layout);
            }
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

        private static bool Contains(IReadOnlyList<RoomType> types, RoomType type)
        {
            foreach (var candidate in types)
            {
                if (candidate == type) return true;
            }
            return false;
        }

        /// <summary>Any available type that may be placed at random, weighted; the last one if none may.</summary>
        private static RoomType Fallback(IReadOnlyList<RoomType> availableTypes, Random random)
        {
            int total = 0;
            foreach (var type in availableTypes) total += Math.Max(0, PlaceholderWeight(type));
            if (total == 0) return availableTypes[availableTypes.Count - 1];
            int roll = random.Next(total);
            foreach (var type in availableTypes)
            {
                roll -= Math.Max(0, PlaceholderWeight(type));
                if (roll < 0) return type;
            }
            return availableTypes[availableTypes.Count - 1];
        }
    }
}
