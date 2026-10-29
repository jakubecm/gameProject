# Notes for Claude Code

## What this is

School project of two developers. First-person labyrinth game set in a datacenter. Every run, the
datacenter is assembled from prefab rooms on a grid. The player deduces from physical clues (sector,
rack row, temperature, exclusions) which node has "mutated" and physically disconnects it before the
infection reaches the gateway. Hacking minigames on in-world terminals give the clues.
The rogue AI talks to the player only via short text messages on terminals.

- Engine: Unity 6.6 LTS, URP (Forward+), PC (Windows), Input System package
- Team: Developer A = systems (generator, solver, run state, minigames Password + Breach);
  Developer B = rooms, lighting, sound, UI, minigames Log + Patch panel
- Player-facing text is **Czech**. Code, comments, commit messages and file names are **English**.

## Project layout

```
Assets/
  _Project/
    Art/        Models, Materials, Textures
    Audio/      SFX, Music, Mixers
    Code/
      Core/     pure C# (generator, solver, clues) — asmdef ColdShutdown.Core, no UnityEngine references
      Runtime/  MonoBehaviours, level builder, run director, terminals, UI — asmdef ColdShutdown.Runtime
      Editor/   editor tools and generators (menu: Tools/Cold Shutdown/...) — asmdef ColdShutdown.Editor
    Data/       ScriptableObjects, JSON/CSV texts (lore, AI messages, briefing)
    Prefabs/    Rooms/, Props/, Terminals/, Player/
    Scenes/     Main.unity, Sandbox_A.unity, Sandbox_B.unity
    Tests/
      EditMode/ tests for ColdShutdown.Core — asmdef ColdShutdown.Tests.EditMode
        Rooms/  tests of the room prefabs against the room contract — asmdef ColdShutdown.Tests.EditMode.Rooms
                (references Runtime, so the Core tests stay free of it)
  ThirdParty/   downloaded assets, one folder per source (see CREDITS.md)
```

All code and tests must live under `Assets/` — Unity does not compile anything outside it.

## Architecture rules

- **Generator and deduction live in `Core` and never touch `UnityEngine`.** They take a seed and
  return plain data (grid, room types, rogue rack, terminals). `LevelBuilder` in Runtime turns that
  data into a scene.
- **Randomness:** use `System.Random(seed)` passed in explicitly. Never `UnityEngine.Random` in Core,
  otherwise the same seed will not produce the same map.
- **Every generated map must pass the solver** (reachable + clues narrow candidates to exactly one rack).
  Keep the "1000 seeds" EditMode test green.
- Rooms: every room prefab follows the room contract below (same footprint, an opening on all four
  sides); the builder closes unused openings with wall plugs.
- **Map data:** `MapGenerator.Generate(seed, width, height)` returns a `MapData` (Core). Cells are
  `(column, row)`, column 0 / row 0 is the south-west corner, North = +row = +Z, East = +column = +X.
  Every cell is a room; `MapData.IsConnected(column, row, Direction)` says which sides are open.
  Connections are symmetric. Room types, entry, gateway and the rogue rack are added on top of this
  later (tasks 2.1+); the generator itself must stay deterministic for a given seed and size.
- Systems communicate through C# events (e.g. `OnClueFound`, `OnInfectionStep`), not direct references
  between unrelated components.
- Texts (lore, AI messages, briefing) are data in `Assets/_Project/Data`, never hard-coded strings.
- Lighting is realtime (Forward+). No baked lightmaps — the map is generated at runtime.

## Player

The player is one prefab, `Prefabs/Player/Player.prefab`, built by `Tools/Cold Shutdown/Create Player Prefab`
(`PlayerPrefabTool`). Every scene uses that prefab; `Create Sandbox Scene` places it, and
`Use Player Prefab In Sandbox Scenes` swaps it into older sandbox scenes. Code is in `Code/Runtime/Player/`,
tests in `Tests/EditMode/Player/` (asmdef `ColdShutdown.Tests.EditMode.Player`).

- **Structure:** `Player` (CharacterController, `PlayerInputReader`, `FirstPersonController`, AudioSource,
  `FootstepAudio`) → `CameraPivot` (pitch, eye height) → `Main Camera` (`HeadBob`, `Interactor`,
  `InteractionPrompt`). Capsule 1.8 m tall, radius 0.3 m, steps up to 0.35 m, slopes up to 45°.
- **Input:** `PlayerInputReader` turns Input System actions into a `PlayerCommand` (move, look, crouch,
  sprint); the controller only sees the command, so tests drive it with `FirstPersonController.Simulate`.
  The actions are serialized on the prefab and can be rebound in the Inspector. While the cursor is free
  (Escape; a click locks it again) or `InputEnabled` is false, the player stands still — terminals and
  menus (tasks 2.4, 3.2) set `InputEnabled = false` instead of disabling components.
- **Movements and default controls:**

  | Movement | Keyboard / mouse | Gamepad | Behaviour |
  |---|---|---|---|
  | Walk | WASD, arrows | left stick | 2.6 m/s, accelerates at 14 m/s², stops at 18 m/s², slides along walls |
  | Look | mouse | right stick | 0.08°/pixel, 140°/s; pitch limited to ±85°; optional invert Y |
  | Sprint | Left Shift (hold) | left stick click | 4.4 m/s, only walking forward and upright; 6 s of stamina, refills in 5 s after a 1 s pause; once empty, sprint again from 30 % |
  | Crouch | C or Left Ctrl (hold) | right face button | body 1.8 → 1.05 m in 0.2 s, eyes 1.66 → 0.91 m, 1.3 m/s; stays down while there is no headroom; wins over sprint |
  | Fall / land | — | — | gravity 18 m/s², `Landed` with the impact speed, short camera dip |

  Crouch and sprint can each be switched to toggle mode (`PlayerInputReader.ButtonMode`).
- **Head bob** (`HeadBob` on the camera): a dip on every footstep, sideways sway with slight roll once per
  two steps, 60 % when crouching, up to 140 % when sprinting, +5° field of view while sprinting, landing
  dip. It moves the camera, never the view direction. `BobEnabled` is the settings switch (task 5.6).
- **Footsteps:** `StrideModel` is the one walking rhythm: step length 0.85 m walking, 0.6 m crouching,
  1.15 m sprinting. `FirstPersonController.Footstep` fires on every foot contact with position, speed,
  crouch/sprint flags and the ground collider (its physics material picks the surface sound, task 4.10).
  Head bob dips exactly on these steps. `FootstepAudio` plays assigned clips (none yet: silent).
- **Events for other systems:** `Footstep`, `Landed`, `CrouchChanged`, `SprintChanged`; state for UI and
  sound: `IsGrounded`, `IsCrouching`, `IsSprinting`, `Stamina` (0–1), `IsExhausted`, `HorizontalSpeed`.
- **Settings hooks (task 5.6):** `PlayerInputReader.MouseSensitivity`, `StickSensitivity`, `InvertY`,
  `Crouching` / `Sprinting` (hold or toggle), `HeadBob.BobEnabled`.
- All numbers are serialized fields on the prefab; tune them there in Play mode ("walking feels right").

## Room contract

The numbers live in one place: `RoomContract` in `Core` (`Assets/_Project/Code/Core/RoomContract.cs`).
Generator, level builder and editor tools read them from there; this section only documents them.
Change both together.

- **Cell:** 12 × 12 m, ceiling 3 m. Room pivot = center of the floor, floor at y = 0. The generator
  places a room at `(column * 12, 0, row * 12)`. +Z is north.
- **Walls:** 0.2 m thick, inside the cell (neighbouring rooms' walls stand back to back), so the
  walkable interior is 11.6 × 11.6 m.
- **Openings:** one in the middle of each side, 2.4 m wide and 2.6 m high, always in the same place.
  A separate wall-plug prefab fills exactly one opening. `RoomContract.GetOpeningPose(Direction)` returns
  the pose of an opening (middle of the wall thickness, on the floor, facing into the room, room-local);
  a wall plug placed with that pose fills it exactly. `LevelBuilder` (task 1.5) uses it for the plugs.
- **Small rooms are strictly square:** 6 x 6 m on the wall center lines (`SmallRoomHalfWidth` 2.9 m, 5.8 x 5.8 m
  inside), by the south door of the cell. Every room except the office is a small room.
- **Keep-clear walkway:** a 2.4 m wide walkway from every door side of a room to its center must stay empty,
  from the floor up to the opening height (`RoomContract.KeepClearHeight` = 2.6 m): the whole cross for big rooms,
  only its arms for a corridor, and only `SmallRoomKeepClearDepth` (2.2 m) in from the door for a small room.
  Ceiling fittings above it (lights, detectors, cable trays over a door) are allowed.
- **Door pockets:** beside every door side of a room with automatic doors the wall stays free from 1.2 to
  `DoorPocketEnd` (2.7 m) along it, 0.3 m deep and up to 2.95 m: the open leaves, the operator box and the reader go there.
- **Rotation:** the generator may rotate rooms by 0/90/180/270° (yaw around +Y). Everything inside a
  prefab rotates with it.
- **Slots:** racks and terminals stand on marker components (`RackSlot`, `TerminalSlot`) placed in the
  room prefab. The pivot is on the floor, centered under the object, and the slot's forward axis
  points into the room (the way the rack front / terminal screen faces). The generator decides which
  rack slots get a rack.
- **Racks:** 0.6 × 1.2 × 2.0 m (W × D × H). Racks stand only in the hall pods and the security office (at most two;
  the old server-aisle room is gone).
- **Room types:** `RoomType` in `Core`. Values are explicit; only append new ones, never reuse 1 (the removed server
  aisle). Corridor shapes: `Corridor` (cross), `CorridorStraight` (N-S), `CorridorT` (E, S, W), `CorridorCorner`
  (E, S), `CorridorDeadEnd` (S). The one big room (whole cell, at most 2 doors, only on its south and west sides
  (`RoomRules.BigRoomDoorSides`), so the walkways meet in the middle): `Office`. Small rooms (6 x 6 m by their south
  door, exactly 1 door, at the end of a corridor): `SmallOffice`, `SecurityOffice`, `PowerRoom`, `HvacRoom`,
  `StorageRoom`. Plus `Hall` and the lift end `Elevator` (13, a corridor tile, `IsCorridor`). `RoomDefinition` (ScriptableObject in Runtime) links a room
  type to its layouts.
- **Layouts:** every room type has `RoomRules.RoomLayouts` (4) fixed layouts, the hall one
  (`RoomRules.LayoutCount`). Corridor layouts differ in the arm that carries the fire kit (`RoomRules.FireArmSide`):
  the corner has 2 (the second its mirror image), the T 4 (fire kit in a side arm or in the stem, and their mirrors), the
  others 1 (a turn reaches every arm); `RoomRules.IsMirroredCorridor` says which are mirrors. Layout 1 is `Room_<Type>.prefab`, the others `Room_<Type>_Layout<n>.prefab`;
  `RoomDefinition.Prefab` is layout 1, `GetLayout(i)` any layout. Layouts 1 and 2 are two designs, 3 and 4 their
  mirror images (`RoomLayouts`/`LayoutMirror` in Editor: small rooms mirrored east-west, the office across the
  diagonal, which swaps its two door sides). `RoomLayout.Generate` gives every `PlacedRoom` a `Layout`.
- **Room rules** (`RoomRules`, Core): `DoorSides(type)` (local sides that may have a door, as a N/E/S/W bit mask:
  corridors their arms, small rooms S, big rooms S + W, the hall all),
  `MaxDoors`, `IsSmallRoom`, `IsCorridor`, `HasAutomaticDoors` (every non-corridor), `FittingTurns(type, connected)` (the quarter turns with which a type fits a cell: corridors exactly,
  every turn for the cross, rooms if every connected side is a door side)
  and `DoorBetween(a, b)` (who owns the door in an opening: the hall piece if one side is hall, otherwise the room,
  between two rooms the west/south one).
- **Small rooms:** the room spans from the south wall to `SmallRoomBackWall` (z = 0) and between the side walls at
  ±`SmallRoomHalfWidth`; full-height walls (`Structure/BackWall`, `Structure/SideWall_W/E`, added by Create Graybox
  Rooms) close it, the rest of the cell is built up.
- **Lift end (`Elevator`):** a corridor tile like the dead end (south arm only) whose closed north end is the
  landing of the passenger lift, also the evacuation lift: no room in between, the lift opens straight onto the
  corridor (numbers in `RoomContract.Lift*`). Structure from Create Graybox Rooms: the landing wall across the corridor
  at `LiftWallFace` (z = 1.5, where a dead end's crossing ends, `Structure/LiftWall_*`) and the block behind the shaft
  (`Structure/ShaftBack`). The `PassengerLift` prop on it: a 1.1 x 2.1 m two-panel centre-opening door (landing and car
  panels move together, `AutoSlidingDoor`: photo cells in front of the door and 0.35 m into the car), the car for 6 to 8
  people, 1.6 x 1.5 m inside and 2.3 m high, with mirror, handrails, lit ceiling and operating panel, and on the landing
  the call buttons, floor indicator, **exit sign over the door**, evacuation lift sign and firefighters' switch. Dressed
  like a dead end (fire kit in its arm); its grille and switch sit in the middle of the crossing's west wall.
  `RoomLayout.Generate` places exactly one, in the dead end nearest the middle of the map (`RoomLayout.LiftCell`), and
  `LevelBuilder` puts the player in the middle of the car facing the door (`RoomContract.LiftStartPose`,
  `LevelBuilder.StartPose`). Without a dead end there is no lift and the player starts in the middle of the map.
- Terminal size is not fixed yet (decide when the terminal model exists, task 2.10).

## Room prefabs

- `Prefabs/Rooms/RoomShell.prefab` (floor, ceiling, four walls with openings, corner pillars) and
  `WallPlug.prefab` are built from ProBuilder boxes by `Tools/Cold Shutdown/Create Room Shell`, with every
  size taken from `RoomContract`. The wall plug's pivot is the opening pose.
- `Room_<RoomType>[_Layout<n>].prefab` are **prefab variants** of the shell, so a change to the shell reaches every
  room (except `Room_Hall`, see Server hall). `Tools/Cold Shutdown/Create Graybox Rooms` makes every layout of every
  `RoomType` plus `Data/RoomDefinition_<RoomType>.asset`, and adds only the room's **game logic** (per layout, from
  `RoomLayouts`): the `TerminalSlot` in the offices and the security office, the security office's video wall
  (2 x 4 screens on the back wall) and its two `RackSlot`s, and the walls of small rooms.
- **Logic and look are separate.** Slots sit directly in the room; everything that is only looks
  (furniture, fittings, building services, partition walls) sits in the room's `Dressing` child, built by
  `Tools/Cold Shutdown/Dress Graybox Rooms`. The same tool gives the shell its door frames
  (`Shell/Wall_<Dir>/DoorFrame`), its suspended ceiling (`Shell/SuspendedCeiling`) and its surface
  materials, and switches the suspended ceiling off (and lays the epoxy floor) in the technical rooms (power room,
  cooling plant, storage). Code never
  looks inside `Dressing`; the generator and level builder only use the slots. Racks and terminals are not dressing:
  they are placed on the slots at runtime (tasks 2.9, 2.10).
- **Suspended ceiling:** an exposed 600 mm T-bar grid hanging 12 mm below the 3 m ceiling plane (the slab
  above is the tile surface), so the room height does not change. A tile is centered on the room center (grid lines
  at odd multiples of 0.3 m), which keeps the grid identical under 90° rotations and puts a tile row in the middle of
  every corridor arm. Every ceiling fitting sits in the grid: 600 fittings, diffusers, detectors, sprinklers and
  domes on a tile center (multiples of 0.6 m), 1200 fittings across two tiles (centered on a grid line along their
  length).
- **Lighting** (few lights, GPU budget, no dark corners): lay-in LED panels `LedPanel600` / `LedPanel1200` in
  suspended ceilings, surface `LedBatten`s in the technical rooms, pendant LED high-bays in the hall, LED strips
  (`AisleLight`) inside the contained aisles; each with one realtime spot light pointing down, no shadows (task 1.10
  keeps shadows for one or two lights). Office 5-7 x 1200 (+1 x 600), small office 3 x 1200 + 1 x 600, security office
  4 x 600, corridors one 600 in the middle of every arm plus the crossing (a 1200 washes out the walls of a 3 m
  corridor), technical rooms 3-4 battens; at most 10 per room. Emergency lights and exit signs
  are emissive only. Bounce light is not baked; `Sandbox_B` uses a trilight ambient (brighter from below) to stand in
  for it, the real scene lighting is decided in task 1.10.
- **Building services** (built by `RoomInfrastructure`), all running floor-to-ceiling, wall-to-wall or into
  the ceiling, so every run leads somewhere and rooms stay independent modules: **no trunking, conduit, pipe or tray
  ever ends in mid-air**; exposed outlets sit in a box fed by a conduit from the ceiling (`RoomInfrastructure.WiredOutlet`).
  Colours: fire water red, chilled water / water green, gas yellow. What shows: offices hide everything above the
  suspended ceiling and in the combined parapet trunking (power + data); corridors show risers and outlet boxes but no
  trays, breaker boards or ducts; technical rooms show what belongs to them; the hall never has fire water.
  - pipes: risers clamped to the walls every metre, with rosettes and valves; fire water riser with a D25 hose
    cabinet in corridors; chilled-water mains, pumps and dial thermometers in the cooling plant;
  - power: **all electrical trunking uses one profile** (`GrayboxPropsTool.TrunkingWidth` x `TrunkingDepth`,
    120 x 60 mm); flush 230 V type-E and RJ45 outlets, floor boxes, cable conduits; exposed power trays in the power room;
  - ventilation: **large square grilles at the floor (`FloorVentGrille`, 0.6 x 0.6 m, just big enough to crawl into)
    and small grilles under the ceiling (`WallVentGrille`, 0.6 x 0.3 m)**, both `GrayboxPropsTool.GrilleGap` (0.15 m) from
    the floor / the ceiling (the girders in the hall), the same in rooms, corridors and the hall; **exactly one grille per
    corridor cell, always the floor one, in the middle of its wall** (the arm opposite the fire arm, or the wall that
    closes it), never near the fire kit; ceiling diffusers in offices and the security office; the
    exposed supply duct only in the cooling plant and the hall;
  - fire safety and security (common practice: NFPA 13 / EN 12845 light hazard, EN 54): sprinklers at most ~4 m apart
    and 2.3 m from a wall (corridors one row on the axis, in line with the lights; 4 per small room, 9 in the office),
    one smoke detector per corridor cell or small room (3 in the office), a manual call point and an extinguisher by
    the exit, ceiling loudspeakers (`CeilingSpeaker`) in suspended ceilings, in **every room and corridor except the hall**
    (the hall is inert gas only). In a corridor the fire kit (fire-water riser, hose cabinet, extinguisher, call point)
    stands in the **fire arm**, which the layout turns towards a room, so it is always by a room's entrance, off the
    through-corridor (`RoomLayout` picks turn and layout, `RoomLayout.FireArmOptions`). Light switches (`LightSwitch`,
    European square 80 x 80 mm) as few as possible but everywhere: one beside each door of a room, one per hall door,
    one per corridor cell, centered above the corridor's grille. Cameras: corridors by the central light, two at a
    crossing or T, one elsewhere; one per small room; two in the office.
- **Doors:** every connected opening into a room gets an automatic bi-parting sliding glass door
  (`AutoSlidingDoor` prop, Runtime component `AutoSlidingDoor`): photo cells on both sides open it for any
  `CharacterController`, an RFID reader with a status LED on both sides (green unlocked, red locked), and an exit sign
  on the operator box **on the owner's (room's) side only**, showing the way out into the corridor; the corridor side
  never has one (fire code). Its pivot is the opening pose, the leaves slide on the owner's side. `IsLocked`
  is for game logic; everything starts unlocked (the preview has every door open-able). The hall's door side piece
  and the aisle ends of the pods carry their own doors (`AisleDoor`, 1.2 x 2.0 m). **Every door works**: there are no
  decorative doors (no hinged doors on partitions, no roller shutters); the one exception is the storage room's
  `FreightLift` (goods lift landing with solid steel telescopic doors, only looks, never opens), which has yellow
  keep-clear hatching in front of it. The lift end's `PassengerLift` door does work.
- **Props** live in `Prefabs/Props/<Name>.prefab` (`GrayboxProp` enum, built by
  `Tools/Cold Shutdown/Create Graybox Props`, split over `GrayboxPropsTool*.cs`: rooms, hall/CCTV, doors/safety,
  racks, office, technical) and rooms hold instances of them, so replacing a graybox prop with a final model
  (task 4.7) is one prefab edit. Pivot rules: floor props on the floor, centered, +Z = the side people use; wall
  props on the floor at the wall surface, +Z away from the wall; small wall fittings (sockets, call points, grilles,
  readers, wall domes) at the center of their back plate; ceiling props on the ceiling surface, hanging down, long
  side along Z; desk-top props on the desk surface; doors on the opening pose. Props have one `BoxCollider` (or none,
  if out of reach) instead of a mesh collider per part.
- **Racks:** `RackCompute`, `RackGpu`, `RackJbod`, `RackNetwork`: closed sides, back and top, the front filled with
  equipment and the odd blanking panel. Placed on slots at runtime (task 2.9); only the preview scene places them now.
- **Where furniture may stand:** shelving (`Shelf`), crates and binders only in the storage room; lockers and the
  locker bench only in the security office; desks carry monitors, keyboards, mice, papers, mugs and photos, never
  binders; plant only in its own room (transformers, switchgear, UPS in the power room; plate heat exchangers, pumps,
  buffer tanks in the cooling plant). There are no service carts, laptops or cable drums. Plant keeps 1 m between
  machines, at least 0.6 m to walls (0.8 m behind transformers). Desks keep room for the chair and to walk past
  (0.4 m free behind every chair); floor boxes lie under the back of the desk they feed. The security office holds
  only two dual-monitor desks, two racks, the video wall, the terminal and either lockers + bench or a coffee corner.
  `RoomPrefabTests` check these rules and that no two props stand in each other.
- **Analog meters:** `AnalogMeterPanel` (6 gauges, in the power room and the cooling plant), `PipeGauge`, the heat
  exchanger thermometer and the switchgear ammeters carry `AnalogGauge`
  (Runtime): the needle follows `Value` (0-1), set by the run logic as a physical clue (load per hall section in the
  power room, coolant temperature and air flow in the cooling plant).
- **Surface materials** are shared assets `Art/Materials/M_<SurfaceMaterial>.mat` (`SurfaceMaterial` enum),
  plain URP Lit greys plus the real service colours (fire red, water green, gas yellow, yellow data trays,
  blue data cables, emissive panels, green and red LEDs, see-through wire mesh). Final textures (task 4.6) go into
  these materials and datacenter profiles (task 4.4) swap them, so no mesh needs to be re-assigned. Agree on any new
  slot with the other developer (`WireMesh` and `LedRed` were added with the hall rework).
- Current graybox layouts (the Czech brief `ROOMSANDPROPS.md` describes them for the team): corridors as before but with
  one grille per cell and no visible duct; **office** design 1 glazed meeting room NE / desk cluster NW / coffee E /
  desks SE / huddle + terminal SW, design 2 open meeting NW / coffee NE / desk bench SE / terminal SW; **small office**
  desks + coffee or meeting table + desks; **security office** desks facing the video wall, racks on a side wall,
  lockers or coffee; **power room** two transformers along the side walls, meter panel between them, switchgear and
  UPS or boards in the south half; **cooling plant** two plate heat exchangers, two pumps, a buffer tank, meter panel,
  risers and the supply duct; **storage room** shelving everywhere with aisles and the freight lift on the east wall;
  **lift end** a dead end closed by the passenger lift.
- Task 1.9 says "by hand, starting from the generated shells". The graybox rooms were generated by
  the tool instead; the result is the same kind of editable ProBuilder prefab. From now on the prefabs are
  edited by hand (ProBuilder), not through the tool.
- **The room tools only create what is missing and never replace existing work**, so hand edits are safe:
  existing prefabs, materials and `Dressing` groups are skipped. To regenerate something, delete it first
  (a `Dressing` group, a prop prefab, a material). Regenerating the shell breaks the variants, so delete
  them too. Order on a fresh project: Create Room Shell, Create Graybox Rooms, Dress Graybox Rooms.
- `Tools/Cold Shutdown/Rebuild Graybox Rooms` is the one destructive room tool: it rebuilds all props (same
  GUIDs), resets the material settings, and rebuilds every layout's logic (slots, small-room walls, lift wall, video
  wall), door frames, suspended ceilings, `Dressing` and `SecurityCameras` groups. From the command line:
  `-executeMethod ColdShutdown.Editor.RoomBatchTools.RebuildRoomsAndPreviews` (rooms, hall kit, both preview scenes and
  the main scene).
  Use it only while the rooms are still pure graybox output; once someone dresses a room by hand, use the
  additive Dress Graybox Rooms instead.
- New room type (task 3.8): append a value to `RoomType`, give it its door sides in `RoomRules` and its slots in
  `RoomLayouts`, run Create Graybox Rooms (prefabs, definition, slots), then dress the new layouts by hand in a
  `Dressing` child, or add a dresser to `RoomDressingTool`.
- The tests in `Tests/EditMode/Rooms` check every `RoomType` has a prefab and definition, every opening has
  a door frame, the shell's suspended ceiling hangs at the ceiling plane, lights are realtime without shadows and
  within the light budget, and that nothing outside the shell blocks the openings, the walkways from the doors,
  the door pockets, a rack slot or the space in front of a terminal slot (placeholder 0.8 x 2 x 0.6 m plus 1 m to
  stand), or sticks out of the room interior (the small room's square), for every layout; that every room has
  cameras, sprinklers, detectors, an extinguisher and a light switch, and not too many of them; that small rooms are
  square and technical rooms have no suspended ceiling; that chairs have room behind them and the layouts differ.
  `HallKitTests` assemble hall tiles and check the walkways, the contained aisle, the door path, the partition, the
  rack slots, the services above the signs and that the hall has no water or outlets. `AutoSlidingDoorTests` drive the
  door. Run them after every hand edit of a room.
- `Tools/Cold Shutdown/Create Room Preview Scene` rebuilds `Scenes/Sandbox_B.unity`: a rectangular 3 x 3 hall (nodes
  A1-C3) with two doors to the corridors around it (cross, T-junctions, corner, straight, dead end), and off the
  corridors 2 offices (one with two doors), a small office, the security office, the power room, the cooling plant,
  the storage room and the lift end; unused openings plugged, an automatic door in every room opening, a rack prop on
  every rack slot, the player in the lift, no scene lights. `Create Test Preview Scene` rebuilds
  `Scenes/TestPreview.unity`: one straight corridor with **every layout of every room type** on it (offices, small
  offices and security offices north, power rooms, cooling plants and storage rooms south, layouts 1-4 from west to
  east), the lift end at the west end (the player starts in its car), every corridor shape at its ends and the 3 x 3 hall
  at the east end. Corridors get their fire arm towards a room, as in a level. Hand-made stand-ins for the level
  builder; the racks exist only in these scenes.
  `SandboxSceneTool.CreatePlayer()` is `internal` and returns the player so both sandbox scenes share it.

## Server hall

A server hall is a group of `Hall` cells. Its numbers live in `HallContract` (Core); this section only documents them.

- **Tile:** same 12 x 12 m cell and same openings as a room, but 6 m to the underside of the roof deck and **no
  walls between neighbouring hall tiles**. Hall tiles never turn and never get wall plugs.
- **Layout of a tile:** two pods side by side across the tile (centers at z = ±2.5), each a long contained cold aisle
  running east-west: two straight rows of 14 rack slots (8.4 m), side by side without gaps, fronts facing each other
  across a 1.2 m aisle (two perforated tiles). The aisle is closed on every side: rack backs and sides outside, a
  containment roof at 2.2 m (just above the racks) with fascia panels, automatic sliding doors with readers at both
  ends, two LED strips inside; its floor is perforated tiles only. The rack backs of both pods face the 1.4 m hot
  aisle in the middle. Free walkways: 1.6 m from the pod ends to an east/west perimeter wall (3.6 m cross walkway
  between tiles), 1.5 m to a north/south perimeter wall (3.4 m between tiles). A yellow outline 0.3 m around every
  pod, pedestrian crossings out of the aisle doors, a walkway line 0.5 m in front of the perimeter walls. Building
  columns (0.6 m) only on the cell corners, a quarter in each tile (free-standing only where four hall tiles meet).
  The aisle floor is a steel grating (`FloorVentTile`).
- **Infrastructure only:** no heat exchangers, transformers, switchgear or electrical boards (they are in the cooling
  plant and the power room), no fire water, no outlets, no furniture and **no partitions of any kind**: a hall is
  always one rectangular space. Every rack slot of a pod is filled, so an aisle can only be entered through its doors.
- **Sides** (`HallSideKind`, chosen by `HallPlan` from the map): `Open` = hall neighbour (connected or not: open
  floor); `Door` = room neighbour, connected: 6 m perimeter wall with the standard opening, an automatic door owned by
  the hall and a light switch; `Wall` = anything else: solid perimeter wall with an inert-gas riser. `Closed` is kept
  for serialized kits but never planned (its piece is open floor).
- **Corners** (`HallCornerKind`): `Inner`, `Concave` (inside corner of the perimeter), `EdgeFirst`/`EdgeSecond` (one
  side perimeter), `Outer`; they carry the column quarter, the walkway line where the wall turns or goes on, a wall
  dome camera on free-standing columns and a cable tray riser in outer corners.
- **Kit:** `Tools/Cold Shutdown/Create Hall Kit` (also run by Create Graybox Rooms) builds `Prefabs/Rooms/Room_Hall.prefab`
  (the tile: floor, roof, girders, pod, services, signs, cameras), `Prefabs/Rooms/Hall/HallSide_<Kind>` (authored
  for North) and `HallCorner_<Kind>` (authored for North-East), and `Data/HallKit.asset`. `HallKit.AddPieces` turns them
  into place. Additive like the room tools; `Rebuild Hall Kit` replaces all pieces (same GUIDs).
- **Services** (kept to the minimum, all running east-west so every run continues in the neighbour or ends in the
  perimeter wall), all above the hanging signs (which reach up to 4.2 m): the return air duct over the hot aisle
  (4.4-4.9 m) with grilles in its underside, per pod a supply duct from the cooling plant (4.4-4.9 m) with a drop onto
  the containment roof (every aisle is connected to the central HVAC), a power tray over the outer row and a yellow
  data tray over the inner row (5.0 m) with a single drop each into the pod and a yellow inert-gas line with nozzles
  (5.15 m), smoke detectors on the deck. Perimeter walls carry only large square supply grilles 0.15 m above the
  floor (as in the corridors) and small return grilles 0.15 m below the girders, the gas release station (alarm +
  flooding), manual call points, readers and a light switch by the doors, cameras and the odd riser: **no fire water,
  sprinklers, hose cabinets, extinguishers, outlets, boards or plant in the hall**. Lighting: pendant LED high-bays
  (`PendantLight`, 4.1 m) over the walkways and the cross walkways, realtime, no shadows.
- **Coordinates:** columns are letters from the hall's west edge (A, B, ... Z, AA), rows numbers from its south edge.
  Every tile has one white hanging cube with its code ("B3") on all four faces over its hot aisle, between its two
  aisles; there are no other signs. `HallSign.Code` sets the text by hand.
- `RoomLayout`'s random placeholder never picks `Hall`; `LevelBuilder` assembles hall cells with its `HallKit` once a
  layout places them (task 2.1).

## CCTV and security office

- `SecurityCamera` (Runtime) sits on the camera props, each with a half-sphere bubble: `SecurityDome` (ceiling dome, 3 m rooms), `SecurityWallDome`
  (wall dome without an arm, on hall walls and columns with a conduit to the roof) and `SecurityPendant` (on hall beams).
  Its feed camera stays disabled. Every room has a `SecurityCameras` group next to `Dressing` (added by Dress Graybox
  Rooms); hall pieces have their own.
- `Room_SecurityOffice` is a small room: video wall of 8 screens (2 x 4) along the whole back wall (`VideoWall` group
  with `SecurityMonitorWall`, screens are `SecurityMonitor`), two desks with dual monitors facing it, the two rack
  slots, the terminal and lockers or a coffee corner.
- `SecurityMonitorWall` pages through all cameras (8 s per page) and renders only a couple of feeds per frame, only
  while the main camera is within 20 m. Feeds only show in Play mode.

## Level builder and debug menu

- `Scenes/Main.unity` is built by `Tools/Cold Shutdown/Create Main Scene`: the player prefab, a `Level Builder`
  (`LevelBuilder`, Runtime) wired to every `RoomDefinition` asset, `WallPlug.prefab`, the `AutoSlidingDoor` prop and the
  hall kit, and a `Debug Menu`. Press Play and a random map is built around the player, who starts in the lift
  (`LevelBuilder.StartPose`). Default size 6 x 6, set on the builder.
- **Data flow:** `MapGenerator.Generate(seed, w, h)` → `MapData` → `RoomLayout.Generate(map, seed, types)` (a
  `PlacedRoom` per cell: type + quarter turns + layout) → `LevelBuilder.Build(seed, w, h)` instantiates the layout's
  prefab. Everything before
  the builder is plain Core data and deterministic. `RoomLayout.Generate` is a **placeholder** until task 2.1 adds
  quotas, entry, gateway and the rogue rack: per cell it picks, weighted, among the types that fit the cell's
  connections (`RoomRules.FittingTurns`: the corridor shape that matches, small rooms only in dead ends, big rooms with
  at most two connections) and turns it so its doors face the connections; if nothing fits it falls back to the cross.
  The lift end is not random: exactly one, in the dead end nearest the middle. Then every corridor gets the turn and
  layout whose fire arm leads into a room.
- **Hierarchy:** `Level Builder / Level / Cell_<column>_<row> / Room_<Type>` plus `WallPlug_<Side>` for every
  closed side and `Door_<Side>` for every automatic door the cell owns. A cell object sits at
  `(column * 12, 0, row * 12)` and never turns; only the room inside turns, so plugs and doors stay where the map says
  the openings are (they use `RoomContract.GetOpeningPose`). The builder only plugs openings that are not connected,
  including the outer edge of the map, and hides the room's door frame there (`LevelBuilder.HideDoorFrame`), so a
  plugged opening looks like the rest of the wall.
- `LevelBuilder` raises `LevelBuilt` after every build; other systems listen to it instead of referencing the
  builder. `Build` replaces the previous level; `Clear` removes it.
- **F1 debug menu** (`DebugMenu`): seed, map size, Regenerate, random seed, FPS, and the map as text
  (`MapData.ToText`; `@` is the player, the room types by `RoomType.Symbol`: `C` corridor, `D` dead end, `O` office,
  `o` small office, `G` security office, `P` power room, `V` cooling plant, `S` storage room, `E` lift end, `H` hall). Open = player stands still and
  the cursor is free. Active in the Editor and development builds only.
- Tests: `Tests/EditMode/RoomLayoutTests.cs` and `RoomRulesTests.cs` (Core), `Tests/EditMode/Rooms/LevelBuilderTests.cs`
  and `MainSceneTests.cs` (build real levels from the room prefabs).

## Working rules for Claude

- Do **not** hand-edit `.unity`, `.prefab` or `.asset` YAML unless explicitly asked. Prefer an editor
  script (menu item under `Tools/Cold Shutdown/`) that creates or changes the asset, and tell the user
  to run it.
- Never modify `Library/`, `Temp/`, `Logs/`, `UserSettings/` or generated `.csproj`/`.sln` files.
- When you create new files under `Assets/`, remind the user to focus the Unity Editor so it generates
  the `.meta` files, and that `.meta` files must be committed together with the asset.
- New third-party asset → put it in `Assets/ThirdParty/<Source>/` and add a row to `CREDITS.md`.
- Keep changes small and focused; one feature per branch.

## Commands

Run EditMode tests from the command line (the Editor must be closed for this project):

```
"<path-to-Unity.exe>" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./TestResults.xml -logFile -
```

On Windows the path is usually `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe`.
