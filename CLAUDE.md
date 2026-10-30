# Notes for Claude Code

## The game

School project of two developers. First-person game set in an underground datacenter. The player deduces from
physical clues (hall node, rack row, temperature, power load, exclusions) which rack has "mutated" and physically
disconnects it before the infection reaches the gateway. **The focus is on the minigames** played on in-world
terminals; they give the clues. The rogue AI talks to the player only via short text messages on terminals.

- **There is exactly one map**, hand-designed by Developer B as a fixed layout of rooms on the 12 m grid. No
  procedural maps. Rooms and props are built by editor tools; the map is assembled from them.
- Engine: Unity 6.6 LTS, URP (Forward+), PC (Windows), Input System package.
- Team: Developer A = systems (run state, clues/deduction, minigames Password + Breach, player);
  Developer B = map, rooms, props, lighting, sound, UI, minigames Log + Patch panel.
- Player-facing text is **Czech** (exit signs stay English). Code, comments, commits and file names are **English**.
- Room and prop reference: **`ROOMS.md`** (every room, tile, layout and placement rule) and **`PROPS.md`** (every
  model). Read the one you need instead of the tool code.

## Project layout

```
Assets/_Project/
  Code/Core/     pure C#, no UnityEngine (asmdef ColdShutdown.Core): grid and map data, room/hall contracts and rules
  Code/Runtime/  MonoBehaviours (asmdef ColdShutdown.Runtime); Player/ = first-person player
  Code/Editor/   editor tools, menu Tools/Cold Shutdown/... (asmdef ColdShutdown.Editor)
  Art/           Materials (M_<SurfaceMaterial>), Textures (T_*), Meshes/Details + Meshes/Racks (generated, commit them), Models
  Audio/         SFX, Music, Mixers (empty so far)
  Data/          RoomDefinition_<Type>.asset, HallKit.asset, RackContents.asset; later texts (lore, AI messages, briefing)
  Prefabs/       Rooms/ (shell, wall plug, Room_<Type>[_Layout<n>], Hall/ pieces), Props/<GrayboxProp>, Player/, Terminals/
  Scenes/        Main.unity, Sandbox_A.unity (player test), Sandbox_B.unity (rooms preview)
  Tests/EditMode/        Core tests (asmdef ColdShutdown.Tests.EditMode)
  Tests/EditMode/Rooms/  prefab/hall/door/rack/level tests (asmdef ColdShutdown.Tests.EditMode.Rooms, references Runtime)
  Tests/EditMode/Player/ player tests (asmdef ColdShutdown.Tests.EditMode.Player)
Assets/ThirdParty/<Source>/   downloaded assets, one folder per source, listed in CREDITS.md
```

All code and tests live under `Assets/` (Unity compiles nothing outside it).

## Where the logic lives

| Area | Files |
|---|---|
| Grid, directions, map data | Core: `Direction`, `MapData` (cells `(column, row)`, column 0/row 0 = south-west, North = +Z, East = +X, symmetric connections), `MapData.ToText` |
| Room geometry contract | Core: `RoomContract` (cell 12 m, ceiling 3 m, walls, openings, small room, lift, rack size, `GetOpeningPose`, `LiftStartPose`) |
| Room types and rules | Core: `RoomType` (explicit values, append only, never reuse 1 or 2), `RoomRules` (door sides, layouts, fire arm, fitting turns, door owner) |
| Room placement per cell | Core: `RoomLayout` / `PlacedRoom` (type + quarter turns + layout), `RoomLayout.FireArmOptions`, `LiftCell` |
| Server hall | Core: `HallContract` (numbers), `HallPlan` (side/corner kinds, node codes A1...); Runtime: `HallKit` (assembles pieces), `HallSign` |
| Building a level | Runtime: `LevelBuilder` (instantiates prefabs per cell, wall plugs, doors, hall pieces, `StartPose`, event `LevelBuilt`), `RoomDefinition` (type → layout prefabs) |
| Slots | Runtime: `RackSlot`, `TerminalSlot` (pivot on floor, forward = into the room), `SlotGizmos`, `SlotPoseExtensions` |
| Racks | Runtime: `Rack` (`Configure(openable, seed)`), `RackDoor` (F opens/shuts, `OpenChanged`), `RackGlow`, `RackPlacement` (which racks open), `RackContentLibrary` |
| Doors | Runtime: `AutoSlidingDoor` (photo cells, readers, `IsLocked`), `AisleDoor` (derived, single leaf) |
| CCTV | Runtime: `SecurityCamera`, `SecurityMonitor`, `SecurityMonitorWall` (pages feeds, only near the player, Play mode) |
| Clue displays | Runtime: `AnalogGauge` (needle follows `Value` 0-1) |
| Interaction | Runtime: `IInteractable`, `Interactable`, `Interactor` (on camera), `InteractionPrompt`, `InteractableColorToggle` (test) |
| Debug | Runtime: `DebugMenu` (F1: FPS, map as text; Editor and development builds only) |
| Player | Runtime/Player: see Player |
| Rooms and props (editor) | `RoomPreviewSceneTool` (the one Sandbox_B tool), `RoomShellTool`, `GrayboxRoomsTool` (room logic: slots, small-room walls, lift wall, video wall), `RoomLayouts` (slot poses), `RoomDressingTool` (all dressing), `RoomInfrastructure`, `RoomPipes`, `InfrastructureModels` (ducts, trays, hangers), `HallKitTool`, `GrayboxPropsTool*.cs` (one partial per prop family), `GrayboxMeshBuilder`, `GrayboxMaterials` (materials + generated textures), `GrayboxUtility` |
| Scenes (editor) | `MainSceneTool` (Main.unity), `SandboxSceneTool` (Sandbox_A), `RoomBatchTools` (batch entry points) |

Not built yet (planned): run state, clues/deduction, terminals and the four minigames, infection steps, AI
messages, UI, audio. Systems talk through C# events (e.g. `OnClueFound`, `OnInfectionStep`), not direct references.

## Architecture rules

- `Core` never references `UnityEngine`; it returns plain data that Runtime turns into a scene.
- Texts (lore, AI messages, briefing) are data in `Assets/_Project/Data`, never hard-coded strings.
- Lighting is realtime (Forward+), no baked lightmaps; lights have no shadows except one or two chosen later (task 1.10).
- Contract numbers live only in `RoomContract` / `HallContract`; tools and docs read them from there.
- Everything a room is made of is generated by the editor tools; **change the tools, not the generated prefabs,
  materials or meshes** (they are rebuilt in place and hand edits are lost).

## Editor tools (Tools/Cold Shutdown/...)

- **Create Preview Scene** – the one tool for rooms, props and `Sandbox_B`. Reimports textures, rewrites every generated
  texture, resets materials, rebuilds every prop, room layout and the hall kit in place (same GUIDs), deletes prop
  prefabs no `GrayboxProp` builds, then builds `Sandbox_B` (and `TestPreview` if it exists). Run it after any change to
  a room or prop tool. Batch: `-executeMethod ColdShutdown.Editor.RoomBatchTools.BuildPreviewSceneB`
  (`RebuildRoomsAndPreviews` also builds `TestPreview` and `Main`). The room shell and wall plug are only created when
  missing (rooms are prefab variants of the shell).
- **Create Main Scene** – `Main.unity`: player, `LevelBuilder` wired to every `RoomDefinition`, wall plug, door, hall kit, debug menu.
- **Create Sandbox Scene** – `Sandbox_A`: floor, light, player, two interaction test cubes.
- **Player tools** – Create Player Prefab, Update Player Prefab, Use Player Prefab In Sandbox Scenes, Set Up Mixamo Player Body.
- Mesh assets are named `<prefab root>_<part name>`: part names must be unique within a prefab, or parts overwrite
  each other's mesh.

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

- The map is a grid of 12 x 12 m cells; every cell holds one room, corridor piece or hall tile, turned by quarter
  turns; unconnected openings get wall plugs. Rooms connect only through corridors; the hall is one rectangle of
  hall tiles. Layout rules for rooms: `ROOMS.md`.
- The player starts in the passenger lift car (`RoomContract.LiftStartPose`, `LevelBuilder.StartPose`).
- `Level Builder / Level / Cell_<column>_<row> / Room_<Type>` + `WallPlug_<Side>` + `Door_<Side>`; the cell never
  turns, only the room inside it.
- `Sandbox_B` is the hand-made preview: a 2 x 3 hall (A1-B3) with corridors around it and one room of each type.

## Working rules for Claude

- Do **not** hand-edit `.unity`, `.prefab` or `.asset` YAML unless asked; change or add an editor tool and tell the
  user which menu item to run.
- Never modify `Library/`, `Temp/`, `Logs/`, `UserSettings/` or generated `.csproj`/`.sln` files.
- New files under `Assets/`: remind the user to focus Unity so it writes the `.meta` files, and to commit them.
- New third-party asset → `Assets/ThirdParty/<Source>/` + a row in `CREDITS.md`.
- A change to rooms or props also updates `ROOMS.md` / `PROPS.md`.
- Keep changes small and focused; one feature per branch.

## Commands

EditMode tests (the Editor must be closed):

```
"<path-to-Unity>" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults ./TestResults.xml -logFile -
```

Windows: `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe`; macOS:
`/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity`.
