# Notes for Claude Code

## The game

School project of two developers. First-person game set in an underground datacenter. The player deduces from
physical clues (hall node, rack row, temperature, power load, exclusions) which rack has "mutated" and physically
disconnects it before the infection reaches the gateway. **The focus is on the minigames** played on in-world
terminals; they give the clues. The rogue AI talks to the player only via short text messages on terminals.

- **There is exactly one map**, hand-designed by Developer B: a text grid of rooms (`Data/WorldMap.asset`), every room
  one 12 x 12 m cell, the hall one room over a rectangle of cells. Models are the building blocks of rooms; each room
  type has one prepared layout (template); the map arranges the rooms. Everything is generated from code by editor tools.
- Engine: Unity 6.6 LTS, URP (Forward+), PC (Windows), Input System package.
- Team: Developer A = systems (run state, clues/deduction, minigames Password + Breach, player);
  Developer B = map, rooms, props, lighting, sound, UI, minigames Log + Patch panel.
- Player-facing text always English. Code, comments, commits and file names are **English**.
- Room and prop reference: **`ROOMS.md`** (every room, tile, layout and placement rule) and **`PROPS.md`** (every
  model). Read the one you need instead of the tool code.

## Project layout

```
Assets/_Project/
  Code/Core/     pure C#, no UnityEngine (asmdef ColdShutdown.Core): grid and map data, room/hall contracts and rules
  Code/Runtime/  MonoBehaviours (asmdef ColdShutdown.Runtime); Player/ = first-person player
  Code/Editor/   editor tools, menu Tools/Cold Shutdown/... (asmdef ColdShutdown.Editor)
    World/         everything the world is built from: WorldTool (the one tool), MainSceneTool, WorldBatch
      Props/<Category>/<Model>.cs   one file per model; Props.cs = Prop enum + registry; Shared/ = cross-category helpers
      Rooms/<Type>.cs               one file per room type (template: slots, structure, dressing); Rooms.cs = pipeline
      Hall/HallBuilder.cs           hall kit (tile, sides, corners) and the baked hall
      Building/                     MeshBuilder, ModelUtility, Surfaces (materials + generated textures), pipes, ducts, trays
    PlayerPrefabTool.cs, SandboxSceneTool.cs   player and Sandbox_A (not world code)
  Art/           Materials (M_<SurfaceMaterial>), Textures (T_*), Meshes/Details + Meshes/Racks (generated, commit them), Models
  Audio/         SFX, Music, Mixers (empty so far)
  Data/          WorldMap.asset (the map), RoomDefinition_<Type>.asset, HallKit.asset, RackContents.asset; later texts
  Prefabs/       Rooms/ (shell, wall plug, Room_<Type>[_Layout<n>], Room_Hall = baked hall, Hall/ kit pieces), Props/<Prop>, Player/, Terminals/
  Scenes/        Main.unity, Sandbox_A.unity (player test), Sandbox_B.unity (rooms preview)
  Tests/EditMode/        Core tests: map text, room rules, hall plan, contracts (asmdef ColdShutdown.Tests.EditMode)
  Tests/EditMode/Rooms/  prefab/hall/door/rack/level tests (asmdef ColdShutdown.Tests.EditMode.Rooms, references Runtime)
  Tests/EditMode/Player/ player tests (asmdef ColdShutdown.Tests.EditMode.Player)
Assets/ThirdParty/<Source>/   downloaded assets, one folder per source, listed in CREDITS.md
```

All code and tests live under `Assets/` (Unity compiles nothing outside it).

## Where the logic lives

| Area | Files |
|---|---|
| The map | Core: `WorldMapText` (text → layout, validation), `MapData` (cells `(column, row)`, column 0/row 0 = south-west, North = +Z, East = +X, symmetric connections), `Direction`; Runtime: `WorldMap` (the asset) |
| Room geometry contract | Core: `RoomContract` (cell 12 m, ceiling 3 m, walls, openings, small room, lift, rack size, `GetOpeningPose`, `LiftStartPose`) |
| Room types and rules | Core: `RoomType` (explicit values, append only, never reuse 1 or 2; `Symbol` = map character), `RoomRules` (door sides, layouts, fire arm, fitting turns, door owner) |
| Room placement per cell | Core: `RoomLayout` / `PlacedRoom` (type + quarter turns + layout, empty cells allowed), `RoomLayout.FireArmOptions` |
| Server hall | Core: `HallContract` (numbers), `HallPlan` (side/corner kinds, node codes A1...); Runtime: `HallKit` (pieces), `HallSign` |
| Building the level | Runtime: `LevelBuilder` (`Build()` the world map: rooms, wall plugs, doors, the baked hall, racks, player in the lift; event `LevelBuilt`; same code in Main at Play and in Sandbox_B in the editor), `RoomDefinition` (type → layout prefabs) |
| Slots | Runtime: `RackSlot`, `TerminalSlot` (pivot on floor, forward = into the room), `SlotGizmos`, `SlotPoseExtensions` |
| Racks | Runtime: `Rack` (`Configure(openable, seed)`), `RackDoor` (on the door hinge; F opens/shuts only when looking at the door; `OpenChanged`), `RackGlow`, `RackPlacement` (which racks open: one per row, never the first or last rack of a run), `RackContentLibrary` |
| Doors | Runtime: `AutoSlidingDoor` (photo cells, readers, `IsLocked`), `AisleDoor` (derived, single leaf) |
| CCTV | Runtime: `SecurityCamera`, `SecurityMonitor`, `SecurityMonitorWall` (pages feeds, only near the player, Play mode) |
| Clue displays | Runtime: `AnalogGauge` (needle follows `Value` 0-1) |
| Interaction | Runtime: `IInteractable`, `Interactable`, `Interactor` (on camera), `InteractionPrompt`, `InteractableColorToggle` (test) |
| Debug | Runtime: `DebugMenu` (F1: FPS, rebuild, map as text; Editor and development builds only) |
| Player | Runtime/Player: see Player |
| World (editor) | `World/`: see Project layout; `WorldTool` = Create Preview Scene, `MainSceneTool`, `WorldBatch` (batch entry) |
| Other editor tools | `PlayerPrefabTool`, `SandboxSceneTool` (Sandbox_A) |

Not built yet (planned): run state, clues/deduction, terminals and the four minigames, infection steps, AI
messages, UI, audio. Systems talk through C# events (e.g. `OnClueFound`, `OnInfectionStep`), not direct references.

## Architecture rules

- `Core` never references `UnityEngine`; it returns plain data that Runtime turns into a scene.
- Texts (lore, AI messages, briefing) are data in `Assets/_Project/Data`, never hard-coded strings.
- Lighting is realtime (Forward+), no baked lightmaps; lights have no shadows except one or two chosen later (task 1.10).
- Contract numbers live only in `RoomContract` / `HallContract`; tools and docs read them from there.
- Generated assets (props, rooms, hall, meshes, generated textures, materials, Sandbox_B) are outputs of the code: commit
  them with their `.meta` (scenes and prefabs reference them by GUID), never edit them by hand.
- Everything a room is made of is generated by the editor tools; **change the tools, not the generated prefabs,
  materials or meshes** (they are rebuilt in place and hand edits are lost).

## Editor tools (Tools/Cold Shutdown/...)

- **Create Preview Scene** (`WorldTool`) – the one tool for the world. Checks the map, then rebuilds from code: every
  generated texture, the materials, every model, every room layout, the hall kit; deletes props, rooms, definitions and
  meshes no code makes any more; bakes the hall of the map (`Room_Hall`); rewires `Main`; builds `Sandbox_B` (left open).
  Run it after any change to the map or to world code. Batch: `-executeMethod ColdShutdown.Editor.WorldBatch.Build`.
  The room shell and wall plug are only created when missing (rooms are prefab variants of the shell).
- **Create Main Scene** – `Main.unity`: player, `LevelBuilder` (builds the map at Play), debug menu.
- **Create Sandbox Scene** – `Sandbox_A`: floor, light, player, two interaction test cubes.
- **Player tools** – Create Player Prefab, Update Player Prefab, Use Player Prefab In Sandbox Scenes, Set Up Mixamo Player Body.
- Generated mesh assets (`Art/Meshes/Details`) are named `<prefab root>_<part name>`: part names must be unique within a
  prefab, or parts overwrite each other's mesh.

## Working with the world (AI-friendly)

- **A model:** read/edit only `World/Props/<Category>/<Model>.cs` (+ its `<Category>Parts.cs` if it uses one). New model =
  a `Prop` value + one file with `[Model(Prop.X)]`. Reference: PROPS.md.
- **A room:** read/edit only `World/Rooms/<Type>.cs` (its `[Template]`: slots, structure, dressing; shared helpers in
  `RoomParts.cs`, `SmallRooms.cs`, `Corridor.cs`). New room type = `RoomType` value + door sides + symbol + one template file.
- **The map:** edit the text of `Data/WorldMap.asset` (format in ROOMS.md).
- **Textures:** generated ones are functions in `Surfaces` (rewritten every build); drawn ones are PNGs in `Art/Textures`.
- After any of these: Create Preview Scene, then the EditMode tests.

## Player

One prefab, `Prefabs/Player/Player.prefab` (`PlayerPrefabTool`); every scene uses it. Code in `Code/Runtime/Player/`.
(The body, jump and Mixamo character are on `main`; merge it before working on the player.)

- **Structure:** `Player` (CharacterController 1.8 m / r 0.3 m, step 0.35 m, slope 45°; `PlayerInputReader`,
  `FirstPersonController`, AudioSource, `FootstepAudio`) → `CameraPivot` (pitch, eye 1.66 m) → `Main Camera` (`HeadBob`,
  `Interactor`, `InteractionPrompt`); `Body` (procedural mannequin, `PlayerBodyAnimator`, fallback) and `Character`
  (Mixamo model, `PlayerCharacterAnimator`, `FloorSnap`). No colliders on the body.
- **Input:** `PlayerInputReader` → `PlayerCommand` (move, look, crouch, sprint, jump); tests drive
  `FirstPersonController.Simulate`. While the cursor is free (Escape) or `InputEnabled` is false the player stands
  still; terminals and menus set `InputEnabled = false`.
- **Controls:** walk WASD (2.6 m/s); look mouse (pitch ±85°); sprint Left Shift (4.4 m/s, 6 s stamina); crouch C/Left
  Ctrl (1.8 → 1.3 m, 1.3 m/s); jump Space (0.6 m, buffer + coyote time). Crouch/sprint can be toggle mode.
- **Events/state:** `Footstep`, `Landed`, `Jumped`, `CrouchChanged`, `SprintChanged`; `IsGrounded`, `IsCrouching`,
  `IsSprinting`, `Stamina`, `IsExhausted`, `HorizontalSpeed`, `VerticalSpeed`. `StrideModel` = the walking rhythm
  (footsteps, head bob and leg cycle share it).
- **Settings hooks (task 5.6):** `MouseSensitivity`, `StickSensitivity`, `InvertY`, hold/toggle modes, `HeadBob.BobEnabled`.
- Mixamo files: `Assets/ThirdParty/PlayerBrian/` (Git LFS), animator `Art/Animation/PlayerBody.controller`.
- All tuning values are serialized on the prefab; an existing prefab keeps its values (Update Player Prefab migrates defaults).

## Map and level

- One map (`Data/WorldMap.asset`, text, north at the top; ROOMS.md). Every cell holds one room or corridor piece, turned by
  quarter turns; corridor shapes, turns, fire-arm layouts and doors follow from the neighbours; unconnected openings get
  wall plugs. Rooms connect only through corridors; the hall is one room over a rectangle of cells, entered from `D` corridors.
- The player starts in the passenger lift car (`RoomContract.LiftStartPose`, `LevelBuilder.StartPose`).
- `Level Builder / Level / Cell_<column>_<row> / Room_<Type>` + `WallPlug_<Side>` + `Door_<Side>`, `Room_Hall` (at its
  south-west cell), `Racks`; the cell never turns, only the room inside it.

## Working rules for Claude

- Do **not** hand-edit `.unity`, `.prefab` or `.asset` YAML unless asked; change or add an editor tool and tell the
  user which menu item to run.
- Never modify `Library/`, `Temp/`, `Logs/`, `UserSettings/` or generated `.csproj`/`.sln` files.
- New files under `Assets/`: remind the user to focus Unity so it writes the `.meta` files, and to commit them.
- New third-party asset → `Assets/ThirdParty/<Source>/` + a row in `CREDITS.md`.
- A change to rooms, props or the map also updates `ROOMS.md` / `PROPS.md` (the map shown in ROOMS.md is the one in `WorldMap.asset`).
- Keep changes small and focused; one feature per branch.

## Commands

EditMode tests (the Editor must be closed):

```
"<path-to-Unity>" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./TestResults.xml -logFile -
```

Windows: `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe`; macOS:
`/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity`.
