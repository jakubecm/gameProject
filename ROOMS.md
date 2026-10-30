# Rooms

Every room, corridor piece and hall tile of the map, with its layouts and placement rules. Metres; room-local
coordinates: origin = cell center on the floor, +Z = north (the unturned prefab), +X = east. Models: `PROPS.md`.
Numbers used by code live in `RoomContract` / `HallContract` (Core); built by `GrayboxRoomsTool` (logic),
`RoomDressingTool` (dressing) and `HallKitTool` (hall), all run by Tools/Cold Shutdown/Create Preview Scene.

## Cell contract (all pieces)

- Cell 12 x 12 m, ceiling 3 m (hall 6 m). Placed at `(column * 12, 0, row * 12)`; turned only by 0/90/180/270°.
- Walls 0.2 m inside the cell (neighbours stand back to back) → interior 11.6 x 11.6 m.
- One opening in the middle of each side, 2.4 x 2.6 m. `RoomContract.GetOpeningPose(side)` = its pose (wall middle,
  floor, facing in). Unconnected openings get `WallPlug` and the room hides that `DoorFrame`.
- Keep-clear walkway: 2.4 m wide from every door to the room center, floor to 2.6 m (corridors: their arms; small
  rooms: only 2.2 m in from the door). Ceiling fittings may hang above it.
- Door pockets: beside every door of a room with automatic doors the wall stays free 1.2-2.7 m from the opening axis,
  0.3 m deep, up to 2.95 m (door leaves, operator, reader).
- Nothing sticks out of the interior (small rooms: their 5.8 x 5.8 m square); props never intersect.
- Racks 0.6 x 1.2 x 2.0 m (W x D x H), only in the hall pods and the security office.

## Prefab structure

- `RoomShell.prefab` (ProBuilder): floor, ceiling, four walls with openings, corner pillars. Every `Room_<Type>` is a
  prefab variant of it (except `Room_Hall`). Shell extras added by dressing: `Shell/Wall_<Dir>/DoorFrame`,
  `Shell/SuspendedCeiling`, surface materials.
- Room children: `Structure` (small-room walls, lift wall), slots (`TerminalSlot`, `RackSlots/RackSlot_NN`),
  `VideoWall` (security office) = game logic; `Dressing` = looks only (code never reads it); `SecurityCameras`.
- Racks and terminals are not in prefabs: they are placed on the slots at runtime / in the preview scenes.
- `RoomDefinition_<Type>.asset` links a type to its layout prefabs (`Prefab` = layout 1, `GetLayout(i)`).

## Room types (`RoomType`, explicit values, append only; 1 and 2 are retired)

| Value | Type | Kind | Door sides (unturned) | Layouts | Symbol |
|---|---|---|---|---|---|
| 0 | `Corridor` | corridor cross | N E S W | 1 | C |
| 8 | `CorridorStraight` | corridor | N S | 1 | C |
| 9 | `CorridorT` | corridor | E S W | 4 | C |
| 10 | `CorridorCorner` | corridor | E S | 2 | C |
| 11 | `CorridorDeadEnd` | corridor | S | 1 | D |
| 13 | `Elevator` | corridor (lift end) | S | 1 | E |
| 5 | `SmallOffice` | small room | S | 1 | o |
| 4 | `SecurityOffice` | small room | S | 1 | G |
| 6 | `PowerRoom` | small room, technical | S | 1 | P |
| 7 | `HvacRoom` | small room, technical (cooling plant) | S | 1 | V |
| 12 | `StorageRoom` | small room, technical | S | 1 | S |
| 3 | `Hall` | hall tile | any | 1 | H |

Prefabs: layout 1 = `Room_<Type>.prefab`, others `Room_<Type>_Layout<n>.prefab` (only corner 2, T 2-4).

## Map rules (`RoomRules`)

- `FittingTurns(type, connected)`: corridors fit exactly their arms; the cross every turn; rooms if every connected
  side is a door side. Small rooms have exactly 1 door, at the end of a corridor (they replace a dead end).
- `MaxDoors`: corridors/hall = their door sides, rooms 1. `HasAutomaticDoors`: every non-corridor.
- `DoorBetween(a, b)`: who owns the door in an opening: the hall if one side is hall, otherwise the room; between two
  rooms the west/south one. The owner gets `Door_<Side>` (an `AutoSlidingDoor`), leaves slide on its side.
- Every corridor gets the turn + layout whose **fire arm** leads into a room (`RoomLayout.FireArmOptions`), so the fire
  kit always stands by a room's entrance, off the through-corridor.
- Exactly one lift end (`Elevator`) per map; the player starts in its car facing the door
  (`RoomContract.LiftStartPose`: x 0, z = 1.84 + 0.75, yaw 180).
- Fire arm per layout (`FireArmSide`): corner L1 East, L2 South (mirror); T L1 South (stem), L2 West, L3 South
  (mirror of L1), L4 East (mirror of L2); dead end and lift end South. Mirrored layouts (`IsMirroredCorridor`: corner
  L2, T L2-4) mirror every arm and walk the arms anticlockwise.

## Shared fittings

- **Suspended ceiling** (offices, security office, corridors): 600 mm exposed T-bar grid 12 mm under the 3 m plane,
  a tile centered on the room center (grid lines at odd multiples of 0.3 m); 600 fittings on tile centers. Off in
  technical rooms, which get the epoxy floor (`HallFloor`).
- **Lights:** realtime spot per fitting, no shadows, at most 10 per room. Offices: 2 x `LedPanel600`; corridors one
  per arm; technical rooms 2 x `LedBatten`; hall `PendantLight`s.
- **Services:** every pipe/trunking/conduit/tray starts and ends in a wall, floor, ceiling or device. Colours: fire
  water red, water green, gas yellow. Trunking one profile 120 x 60 mm. Outlets flush (`PowerSocket`, `DataSocket`
  at 0.3 m). Offices hide services; corridors show risers and outlets only; technical rooms show their own; the hall
  has no water and no outlets.
- **Ventilation:** `FloorVentGrille` 0.6 x 0.6 m 0.15 m above floor, `WallVentGrille` 0.6 x 0.3 m 0.15 m below ceiling;
  offices use `CeilingDiffuser`.
- **Safety (NFPA 13 / EN 12845 / EN 54):** sprinklers ≤ ~4 m apart, ≤ 2.3 m from walls; one smoke detector per
  corridor cell / small room; extinguisher + call point (1.4 m) by the exit; `CeilingSpeaker` in every suspended
  ceiling (none in the hall); light switch 80 x 80 mm at 1.05 m, one per room door / corridor cell / hall door.
- **Doors:** every connected opening into a room has an `AutoSlidingDoor` (glass, bi-parting, photo cells, reader with
  LED both sides, exit sign on the room side only). All unlocked at start (`IsLocked` is game logic). No decorative
  doors; the only non-working door is the storage room's `FreightLift`.

## Corridors (`DressCorridor`)

- 3 m wide arms (half width 1.5) between four `Partition_<NE..SW>` blocks (full height, `Wall`), skirting on arm faces;
  a missing arm is a `Fill_<Side>` block with skirting.
- Every arm (authored as north, `Arm_<Side>`): `LedPanel600` at 3.6 m, `Sprinkler` at 1.8 and 4.8 m (on the axis).
- Cell ceiling: `CeilingSpeaker` (0.6, -0.6). Straight: extra light at the center. Corner, dead end, lift end: extra
  sprinkler (0, 0.6). Dead end, lift end: second light (0, -0.6). Cross and T: `SmokeDetector` (-0.6, 0.6); others:
  detector in the fire arm at 2.4 m.
- Camera (`SecurityDome` at the center): cross yaw 0, T yaw 180, corner yaw 135; none in straight, dead end, lift end.
- Arms are counted from the fire arm (clockwise, anticlockwise if mirrored):
  - arm 0 (fire arm): `FireWaterRiser` (red, east face, z 4.5) with branch into `HydrantCabinet` (z 3.3),
    `FireExtinguisher` (west face, z 3.6), `FireCallPoint` (z 4.3);
  - arm 1: `WaterRiser` (green, z 4.6), `GasRiser` (yellow, z 2.3), `PowerSocket` + `DataSocket` pair (z 3.45), all on
    the west face;
  - other arms: only light and sprinklers.
- One `FloorVentGrille` per cell with the `LightSwitch` above it, in the middle of the arm opposite the fire arm (or
  of the block closing it); never near the fire kit. No visible duct, tray or breaker board.

## Lift end (`Elevator`)

A dead end whose closed north end is the lift landing; no room in between. Structure: `LiftWall_W/E` + `LiftWall_Lintel`
at z = 1.5 (`LiftWallFace`) with the 1.1 x 2.1 m door opening, `ShaftBack` block behind the shaft. `PassengerLift`
prop on the landing (car 1.6 x 1.5 x 2.3 m, working door). Dressed like a dead end (fire kit in its arm); its grille and
switch sit in the middle of the west wall of the crossing.

## Small rooms (all five)

- Strict 6 x 6 m square by the south door: interior x ±2.9 (`SmallRoomHalfWidth`), z -5.8 to 0 (`SmallRoomBackWall`);
  `Structure/BackWall`, `SideWall_W/E` full height; the rest of the cell is built up.
- Basics (`SmallRoomBasics`): 4 `Sprinkler` at (±1.2, -1.8 / -4.2); 1 `SmokeDetector`; `SecurityDome` in the
  south-east corner (2.4, -5.4, yaw 315); `FireExtinguisher` + `FireCallPoint` on the east wall at z -5.3;
  `LightSwitch` on the east wall at z -5.7 (offices) or the west wall at z -5.6 (technical rooms).
- Office ceiling (`SmallRoomCeiling`): `LedPanel600` at (0, -1.8) and (0, -4.2), `CeilingDiffuser` (0, -3),
  `CeilingSpeaker` (0.6, -2.4), detector (-0.6, -3.6).
- Technical ceiling (`TechnicalCeiling`): bare slab, `LedBatten` at (±1.2, -3), detector (0, -3), two
  `WallVentGrille` shaft grilles high on the back wall at x ±1.2. Nothing runs across the ceiling.
- Machines: ≥ 1 m between them, ≥ 0.6 m to walls (0.8 m behind transformers). Desks: 0.4 m free behind every chair.

### Small office (`SmallOffice`)

- Terminal slot: south-west corner, (-2.5, -5.4) facing east; terminal outlets (power + data) on the west wall at z -4.97.
- North-east corner: coffee corner = `CoffeeCounter` (center x 1.15, back to the north wall) with `CoffeeMachine`,
  2 `Mug`, `PowerSocket` above; `Fridge` in the corner (2.45, -0.35).
- Back wall: `WallTv` centred in the free part of the wall (x -1.33), `MeetingTable` on the same axis turned 90°, its
  short end 5 cm from the wall, 4 `OfficeChair` (2 per long side), `PaperStack`, `Mug`.
- West wall: `Desk` with `DualWorkstation`, `PcTower`, `OfficeChair`, `PhotoFrame`, `Mug`, 0.95 m from the door wall.
- East wall (same 0.95 m start): 2 `FilingCabinet` (papers on the second), 4 cm gap, `Printer` + its `PowerSocket`.

### Security office (`SecurityOffice`)

- Back wall: `VideoWall` (game logic, `SecurityMonitorWall`) of 8 `VideoScreen` (2 rows at 1.3 / 2.05 m x 4 columns
  at x ±0.625 / ±1.875), trunking behind each column up to the ceiling.
- Two consoles facing the video wall: `Desk` + `DualWorkstation` + `PcTower` + `OfficeChair` + `FloorBox` at
  (±0.801, -1.9) (2 mm apart); `PaperStack` "ShiftLog" between.
- West wall: 2 `RackSlot` (compute rack, JBOD) at z -3.95 / -4.55, fronts east (racks placed on them; one openable);
  `PowerSocket` + `DataSocket` for the racks; terminal slot (-2.5, -5.4) facing east with its outlets.
- East wall: 2 `Locker` (z -3.95 / -4.55, centred on the racks) and `LockerBench` (1.9, -4.25).
- Only these things: no coffee corner, no other furniture.

### Power room (`PowerRoom`)

- 2 `Transformer` along the side walls at (±1.5, -1.6), fronts facing each other.
- `AnalogMeterPanel` on the back wall between them (kW per hall section, clue).
- West wall: 2 `SwitchgearCabinet` (z -4.0 / -4.8). East wall: 2 `EquipmentCabinet` UPS (z -3.9 / -4.5).
- Yellow painted lines (one mesh): aisle lines at x ±1.4 from the door to z -3.1, keep-out line z -3.1 and side lines
  x ±0.4 round the transformers. No floor grilles, no tray across the room.

### Cooling plant (`HvacRoom`)

- Free aisle between yellow lines at x ±1.4 from the door to the back wall; nothing in it, the door pockets or the
  extinguisher corner. No pipe crosses the room: runs along the walls at 2.62 m (flow) and 2.40 m (return) or rise
  from floor/ceiling.
- `CoolingMeterPanel` on the back wall (6 large dial thermometers, pipes into the exchangers' end faces; clue).
- `PlateHeatExchanger` (east) and `PlateHeatExchangerMirrored` (west) at the back of the side walls (z -1.55).
- East (pump side): 2 `CirculationPump` (x 2.4, z -3.4 / -4.2, motors to the aisle) with discharge ball valves and
  `PipeGauge` thermometers, flow and return headers along the wall to the exchanger, blue clamps.
- West: `BufferTank` (-2.28, -3.12), `AirHandlerUnit` (CRAH, duct to the ceiling, -2.48, -3.95), `ExpansionVessel`
  (-2.35, -4.57), each piped into the west exchanger's flow/return headers (valves on the flows).
- Wide pipes are insulated aluminium with bands in the medium colour (green), blue supports. No switchgear here.

### Storage room (`StorageRoom`)

- 15 `Shelf` units with cardboard boxes (`BoxLidCube`, `BoxLidFlat`, `BoxCartonCube`, `BoxCartonFlat`, 1-2 per board
  by a fixed pattern): 5 along the back wall, 5 along the west wall, 1 on the east wall (z -0.9), a back-to-back
  double row of 2 x 2 in the middle (z -1.9 / -2.9). Every shelf reachable from an aisle ≥ 0.9 m.
- `FreightLift` on the east wall (z -3.55), never opens; yellow keep-clear `Hatching` in front from x 1.6 to the wall.

## Server hall (`Hall`)

A group of hall tiles forming **one rectangle** (no partitions). Node codes: columns letters from the west edge (A, B,
... Z, AA), rows numbers from the south edge ("B3"). Built from `Room_Hall.prefab` + side and corner pieces of
`HallKit.asset` (`HallKit.AddPieces` turns them into place; tiles never turn, never get wall plugs).

- **Tile:** 12 x 12 m, deck at 6 m, no walls between tiles. Epoxy floor; flat concrete ceiling textured as 600 mm
  perforated tiles; steel girders on the cell edges (half profile per tile, 5.2 m), secondary beams every 3 m (5.6 m).
- **Two pods** per tile (centers z ±2.5), each a contained cold aisle along X: 2 rows of 14 `RackSlot` (8.4 m), fronts
  facing across a 1.2 m aisle; every slot holds a rack, so an aisle is entered only by its doors. Hot aisle 1.4 m
  between the pods' backs. Aisle floor: `FloorVentTile`s only. Containment roof: solid black 4 cm panels lying on the
  rack tops (white labels on two). `AisleDoor` at both ends (single black leaf, slides left seen from outside, reader on
  the right rack end). 2 `AisleLight` strips under the roof. Yellow outline 0.3 m round each pod. 56 rack slots/tile.
- **Walkways:** 1.6 m pod end → east/west wall (3.6 m between tiles), 1.5 m pod side → north/south wall (3.4 m between
  tiles). Nothing below 2.6 m in them.
- **Overhead (all above the signs, which reach down to 4.2 m):** return duct over the hot aisle (4.4-4.9 m, 4 grilles in
  its underside); per pod a supply duct (4.4-4.9 m) with one drop at x 0 into the containment roof (collar, rigid duct,
  black flexible connector, boot, roof collar); power tray over the outer row (z ±3.7) with one brown conduit drop
  (x -3) and data tray over the inner row (z ±1.3) with one green drop (x +3), trays at 5.0 m; inert-gas line over each
  pod (5.15 m) with 2 `GasNozzle` (x ±2.1) and ring hangers; 6 `SmokeDetector` on the deck. Ducts brushed stainless,
  flanged every 3-4 m, trapeze hangers; trays one model (galvanised, slotted, cables, M10 rods). All runs go east-west
  edge to edge, so they continue into the neighbour or end in the perimeter wall.
- **Lights:** 8 `PendantLight` (1.9 m below the deck): 4 over the walkways (x ±2.4, z ±5.1), 4 over the cross walkways
  (x ±5.1, z ±2.0, turned 90°).
- **Sign:** one white `HallSignCube` over the hot aisle center with the tile code on all four faces; no other signs.
- **Cameras:** only hanging `SecurityPendant`s from the girder, 2 per open east edge (3 m either side of its middle,
  `HangingCamera` group; `HallKit.ChooseCameras` keeps the east side only). **No cameras on walls or columns.**
- **Sides** (`HallSideKind`, from `HallPlan`): `Open` = hall neighbour: open floor (carries the hanging cameras);
  `Door` = connected room neighbour: 6 m perimeter wall with the opening, `DoorFrame`, `AutoSlidingDoor` (owned by the
  hall), `FireCallPoint`, `LightSwitch`; `Wall` = anything else: solid 6 m wall, `FireCallPoint`, inert-gas riser floor
  → roof. Both have a plinth and a walkway line 0.5 m in front. `Closed` is unused (open floor).
- **Corners** (`HallCornerKind`): `Inner` (free-standing column), `Concave`, `EdgeFirst`, `EdgeSecond`, `Outer`: a quarter
  of the 0.6 m column (cap plate, no base plate) and the walkway-line pieces that turn there.
- **Never in the hall:** water (sprinklers, hose cabinets, extinguishers), outlets, boards, plant, furniture,
  ventilation grilles in the walls, partitions, speakers.

## Preview scenes

- `Sandbox_B` (Create Preview Scene): 7 x 6 cells; 2 x 3 hall (A1-B3) with doors from two corridors; cross, T, corner,
  straight, dead end; one small office, security office, power room, cooling plant, storage room and the lift end
  (player start); racks on every slot (`RackPlacement`: one openable rack in the security office, one per row of every
  aisle); trilight ambient instead of bounce light; no scene lights.
- `TestPreview` (batch `RebuildRoomsAndPreviews`): one long corridor with every layout of every room type, every
  corridor shape and the 2 x 3 hall.

## Tests (run after every room change)

`Tests/EditMode/Rooms`: `RoomPrefabTests` (prefab + definition per type, door frames, ceiling height, light count and
settings, nothing in openings/walkways/pockets/slots/terminal space, inside the interior, safety counts, small rooms
square, technical rooms without ceiling, chairs have room, props do not overlap), `HallKitTests` (walkways, aisle
closed, door path, rack slots, services above signs, no water/outlets, cameras only hanging), `AutoSlidingDoorTests`,
`RackTests`, `LevelBuilderTests`, `MainSceneTests`.
