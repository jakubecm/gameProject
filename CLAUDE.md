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
  `InteractionPrompt`); plus a `Body` child (`PlayerBodyAnimator`, see below). Capsule 1.8 m tall, radius
  0.3 m, steps up to 0.35 m, slopes up to 45°.
- **Input:** `PlayerInputReader` turns Input System actions into a `PlayerCommand` (move, look, crouch,
  sprint, jump); the controller only sees the command, so tests drive it with `FirstPersonController.Simulate`.
  The actions are serialized on the prefab and can be rebound in the Inspector. While the cursor is free
  (Escape; a click locks it again) or `InputEnabled` is false, the player stands still — terminals and
  menus (tasks 2.4, 3.2) set `InputEnabled = false` instead of disabling components.
- **Movements and default controls:**

  | Movement | Keyboard / mouse | Gamepad | Behaviour |
  |---|---|---|---|
  | Walk | WASD, arrows | left stick | 2.6 m/s, accelerates at 14 m/s², stops at 18 m/s², slides along walls |
  | Look | mouse | right stick | 0.08°/pixel, 140°/s; pitch limited to ±85°; optional invert Y |
  | Sprint | Left Shift (hold) | left stick click | 4.4 m/s, only walking forward and upright; 6 s of stamina, refills in 5 s after a 1 s pause; once empty, sprint again from 30 % |
  | Crouch | C or Left Ctrl (hold) | right face button | body 1.8 → 1.3 m in 0.3 s, eased in and out, eyes 1.66 → 1.16 m, 1.3 m/s; stays down while there is no headroom; wins over sprint |
  | Jump | Space | bottom face button | feet rise 0.6 m (below desk height, so desks are no platform); not while crouching; jump pressed 0.12 s before landing still counts (buffer), and 0.1 s after walking off an edge (coyote time); speed is kept in the air, steering has 35 % of the ground acceleration; a sprint carries through the jump; a ceiling ends the rise |
  | Fall / land | — | — | gravity 18 m/s², `Landed` with the impact speed, short camera dip |

  Crouch and sprint can each be switched to toggle mode (`PlayerInputReader.ButtonMode`).
- **Head bob** (`HeadBob` on the camera): a dip on every footstep (1.8 cm), sideways sway (1.2 cm) with slight
  roll (0.2°) once per two steps, 60 % when crouching, 85 % when sprinting (the steps already come faster, a
  bigger bob felt like a rollercoaster), +3° field of view while sprinting, landing dip. It moves the camera,
  never the view direction. `BobEnabled` is the settings switch (task 5.6).
- **Footsteps:** `StrideModel` is the one walking rhythm: step length 0.85 m walking, 0.6 m crouching,
  1.15 m sprinting. `FirstPersonController.Footstep` fires on every foot contact with position, speed,
  crouch/sprint flags and the ground collider (its physics material picks the surface sound, task 4.10).
  Head bob dips exactly on these steps. `FootstepAudio` plays assigned clips (none yet: silent).
- **Events for other systems:** `Footstep`, `Landed`, `Jumped`, `CrouchChanged`, `SprintChanged`; state for UI and
  sound: `IsGrounded`, `IsCrouching`, `IsSprinting`, `Stamina` (0–1), `IsExhausted`, `HorizontalSpeed`,
  `VerticalSpeed`, `LocalVelocity`, `CrouchAmount`.
- **Body** (`Body` child, `PlayerBodyAnimator`): a mannequin of cylinders, spheres and boxes in a bone hierarchy
  (`Pelvis` → `Chest` → arms, `Thigh_L/R` → `Shin` → `Foot`), built by `PlayerBodyBuilder` with sizes from
  `PlayerBodyDimensions` (legs 2 × 0.5 m, 1.8 m tall), so it is visible when looking down. No Animator and no clips:
  the pose is computed every frame from the controller. Legs: `WalkCycle` gives each foot's path from the stride
  phase (the foot on the floor slides back under the body at exactly the walking speed, so it does not slip; the
  left foot lands on even phases, the right on odd ones, with the footstep sound and the head bob), `LegSolver`
  is a two-bone solver that turns the wanted ankle position into hip and knee angles. Hips hang under the eyes,
  so they follow the crouch; the torso leans when sprinting and crouching; legs tuck in the air; arms swing
  against the legs; hips dip after landing; the soles are snapped to the floor under the player (the
  CharacterController floats a skin width above it). The head and the chest only cast shadows: the camera is inside the head, 14 cm above the chest, which would fill half the screen when looking down. The player sees legs and arms.
  The body has **no colliders**. A final model can later be rigged to the same bones, or the animator replaced.
- **Mixamo body (the real character):** a downloaded, rigged human (Adobe Mixamo, see `CREDITS.md`) in
  `Assets/ThirdParty/PlayerBrian/`: `character.fbx` (with its skin and textures, ~55 MB) plus one
  `Ch01_nonPBR@<Name>.fbx` per animation, downloaded from Mixamo "In Place" and "Without Skin" (a few hundred KB
  each; with the skin each was 55 MB). Clips used, by file name: `Idle`, `Walking`, `Running`, `Male Crouch Pose` (or
  `Crouch Idle`), `Crouched Walking`, `Jump`; only `Walking` is required. `Tools/Cold Shutdown/Set Up Mixamo Player
  Body` (`MixamoPlayerTool`) sets the models up as Humanoid, extracts the textures (1024 px), converts the materials to
  URP Lit in `ThirdParty/PlayerBrian/Materials`, builds `Art/Animation/PlayerBody.controller` from the clips it finds,
  adds a `Character` child to the player prefab and switches the procedural mannequin (`Body`) off (it stays in the
  prefab as a fallback). Import settings of the clips: root height and rotation are baked into the pose, the forward
  travel is **not** (it stays root motion, ignored because Apply Root Motion is off; baking it makes the body walk away
  from the camera). Run it again after adding animations; if a new FBX is not imported yet, focus Unity first.
- **Animator Controller:** Idle ↔ Locomotion (blend tree of Walking and Running by `Speed`, thresholds = the clips' real
  speeds) ↔ Crouch (blend tree of the crouch pose and Crouched Walking, while `Crouch` > 0.5); Jump from any state
  by the `Jump` trigger, back to Idle or Locomotion after landing. Parameters: `Speed`, `MotionSpeed`,
  `CrouchMotionSpeed`, `Grounded`, `Crouch`, `Jump`. A movement whose animation is missing shows the standing one.
- **`PlayerCharacterAnimator`** (on `Character`) sets the parameters from the controller. The walk and run
  animations play at the rate that keeps the feet from sliding (`walkClipSpeed`, `runClipSpeed`, `crouchClipSpeed`
  on the component; the tool takes them from the clips, in-place clips fall back to defaults: tune them in Play mode
  if the feet slide); walking backwards plays them in reverse. The head bone is scaled to nothing, hair and
  eyelashes only cast shadows, the character stands 12 cm behind the camera, is scaled so its eyes are at the
  camera height, and its soles are snapped to the floor (`FloorSnap`). The setup measures the crouch pose and sets the
  player's crouched capsule height so the camera drops as low as the animation's head does.
- **Updating an existing prefab:** `Tools/Cold Shutdown/Update Player Prefab` adds the body if it is missing and
  moves tuning values that still have their old default to the new default (crouch height and time, head bob);
  a value changed by hand is left alone. `Create Player Prefab` only creates a missing prefab.
- **Settings hooks (task 5.6):** `PlayerInputReader.MouseSensitivity`, `StickSensitivity`, `InvertY`,
  `Crouching` / `Sprinting` (hold or toggle), `HeadBob.BobEnabled`.
- All numbers are serialized fields on the prefab; tune them there in Play mode ("walking feels right"). The
  defaults in code only apply to a new prefab; an existing prefab keeps its values (see Update Player Prefab).
- Tests: `FirstPersonControllerTests` (movement, crouch easing, jump rules), `LegSolverTests` (solver and walk
  cycle math), `PlayerBodyAnimationTests` (walks the real prefab: feet on the floor, planted foot does not slide),
  `PlayerPrefabTests` (body, bones, no colliders, tuning).

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
- **Keep-clear cross:** a 2.4 m wide walkway from every opening to the center must stay empty, from the
  floor up to the opening height (`RoomContract.KeepClearHeight` = 2.6 m). Ceiling fittings above it
  (lights, detectors, cable trays over a door) are allowed. Agreed as a clarification for realistic ceilings.
- **Rotation:** the generator may rotate rooms by 0/90/180/270° (yaw around +Y). Everything inside a
  prefab rotates with it.
- **Slots:** racks and terminals stand on marker components (`RackSlot`, `TerminalSlot`) placed in the
  room prefab. The pivot is on the floor, centered under the object, and the slot's forward axis
  points into the room (the way the rack front / terminal screen faces). The generator decides which
  rack slots get a rack.
- **Racks:** 0.6 × 1.2 × 2.0 m (W × D × H).
- **Server aisle:** 12 rack slots — 6 on the west wall and 6 on the east wall, 3 on each side of the
  opening. Along the wall, slot centers are 2.4, 3.6 and 4.8 m from the room center (1.2 m pitch,
  mirrored on both sides). Rack centers stand 5.2 m from the center on X, backs against the wall.
  West racks face east (yaw 90), east racks face west (yaw 270). `RoomContract.GetServerAisleRackSlots()`
  returns these poses so editor tools use the same layout.
- **Room types:** `RoomType` in `Core`. Values are explicit; only append new ones.
  `RoomDefinition` (ScriptableObject in Runtime) links a room type to its prefab.
- Terminal size is not fixed yet (decide when the terminal model exists, task 2.10).

## Room prefabs

- `Prefabs/Rooms/RoomShell.prefab` (floor, ceiling, four walls with openings, corner pillars) and
  `WallPlug.prefab` are built from ProBuilder boxes by `Tools/Cold Shutdown/Create Room Shell`, with every
  size taken from `RoomContract`. The wall plug's pivot is the opening pose.
- `Room_<RoomType>.prefab` are **prefab variants** of the shell, so a change to the shell reaches every
  room. `Tools/Cold Shutdown/Create Graybox Rooms` makes one for every `RoomType` plus
  `Data/RoomDefinition_<RoomType>.asset`, and adds only the room's **game logic**: the 12 `RackSlot`s in
  the server aisle, the `TerminalSlot` in the office.
- **Logic and look are separate.** Slots sit directly in the room; everything that is only looks
  (furniture, fittings, building services, partition walls) sits in the room's `Dressing` child, built by
  `Tools/Cold Shutdown/Dress Graybox Rooms`. The same tool gives the shell its door frames
  (`Shell/Wall_<Dir>/DoorFrame`), its suspended ceiling (`Shell/SuspendedCeiling`) and its surface
  materials. Code never looks inside `Dressing`; the generator and level builder only use the slots. Racks
  and terminals are not dressing: they are placed on the slots at runtime (tasks 2.9, 2.10).
- **Suspended ceiling:** an exposed 600 mm T-bar grid hanging 12 mm below the 3 m ceiling plane (the slab
  above is the tile surface), so the room height does not change. Grid lines run at multiples of 0.6 m from
  the room center, which keeps the grid identical under 90° rotations. Every ceiling fitting sits in the
  grid: 600 fittings, detectors, sprinklers and nozzles on a tile center (odd multiples of 0.3 m), 1200
  fittings across two tiles (centered on a grid line along their length).
- **Lighting:** lay-in louvered fluorescent fittings (`Troffer600` 4 x T5, `Troffer1200` 3 x T5), each with
  its own realtime spot light pointing down, no shadows (task 1.10 keeps shadows for one or two lights).
  The intensity is set once in the prop. No room has a central light. Office: 4 x 4 grid of 600 fittings;
  server aisle: 1200 fittings over the rack fronts and the aisle; corridor: 1200 fittings across the arms.
  Bounce light is not baked; `Sandbox_B` uses a trilight ambient (brighter from below) to stand in for it,
  the real scene lighting is decided in task 1.10.
- **Building services** (built by `RoomInfrastructure`), all running floor-to-ceiling, wall-to-wall or into
  the ceiling, so every run leads somewhere and rooms stay independent modules:
  - pipes: risers clamped to the walls every metre, with rosettes at floor and ceiling and valves; fire water
    red (with a D25 hose cabinet), water green, gas yellow. Sprinklers are fed from above the ceiling;
  - power: distribution boards with trunking up into the ceiling, power trays over the rack rows, flush
    230 V outlets, desk-height parapet trunking in the office. **All electrical trunking uses one profile**
    (`GrayboxPropsTool.TrunkingWidth` x `TrunkingDepth`, 120 x 60 mm);
  - data: yellow wire trays over the rack rows plus a cross-connect tray between the rows, RJ45 outlets.
  - Fire safety: smoke detectors in every room, sprinklers in the corridor and office, inert-gas nozzles
    (no water) in the server aisle, extinguishers.
- **Props** live in `Prefabs/Props/<Name>.prefab` (`GrayboxProp` enum, built by
  `Tools/Cold Shutdown/Create Graybox Props`) and rooms hold instances of them, so replacing a graybox
  prop with a final model (task 4.7) is one prefab edit. Pivot rules: floor props on the floor, centered,
  +Z = the side people use; wall props on the floor at the wall surface, +Z away from the wall; sockets at
  the center of their back plate; ceiling props on the ceiling surface, hanging down, long side along Z;
  desk-top props on the desk surface. Props have one `BoxCollider` (or none, if out of reach) instead of a
  mesh collider per part.
- **Surface materials** are shared assets `Art/Materials/M_<SurfaceMaterial>.mat` (`SurfaceMaterial` enum),
  plain URP Lit greys plus the real service colours (fire red, water green, gas yellow, yellow data trays,
  blue data cables, emissive tubes). Final textures (task 4.6) go into these materials and datacenter
  profiles (task 4.4) swap them, so no mesh needs to be re-assigned. Agree on any new slot with the other
  developer.
- Current graybox layouts: **corridor** = a 3 m wide cross between four partition blocks (skirting, fire
  water riser with hose cabinet, water and gas risers, distribution board with outlet, extinguisher);
  **server aisle** = perforated floor tiles in front of every rack run, power and data trays over the rows,
  data cross-connect, distribution board, gas nozzles, detectors, extinguisher; **office** = two
  workstations on a parapet trunking with outlets (NW), meeting table with whiteboard and outlets (NE),
  filing cabinets and shelving (SE), terminal corner with its outlets kept free (SW), radiator.
- Task 1.9 says "by hand, starting from the generated shells". The three graybox rooms were generated by
  the tool instead; the result is the same kind of editable ProBuilder prefab. From now on the prefabs are
  edited by hand (ProBuilder), not through the tool.
- **The room tools only create what is missing and never replace existing work**, so hand edits are safe:
  existing prefabs, materials and `Dressing` groups are skipped. To regenerate something, delete it first
  (a `Dressing` group, a prop prefab, a material). Regenerating the shell breaks the variants, so delete
  them too. Order on a fresh project: Create Room Shell, Create Graybox Rooms, Dress Graybox Rooms.
- `Tools/Cold Shutdown/Rebuild Graybox Dressing` is the one destructive tool: it rebuilds all props (same
  GUIDs), resets the material settings, and rebuilds door frames, suspended ceilings and `Dressing` groups.
  Use it only while the rooms are still pure graybox output; once someone dresses a room by hand, use the
  additive Dress Graybox Rooms instead.
- New room type (task 3.8): append a value to `RoomType`, run Create Graybox Rooms (prefab, definition,
  slots), then dress the new variant by hand in a `Dressing` child, or add a dresser to `RoomDressingTool`.
- The tests in `Tests/EditMode/Rooms` check every `RoomType` has a prefab and definition, every opening has
  a door frame, the shell's suspended ceiling hangs at the ceiling plane, lights are realtime without shadows, and
  that nothing outside the shell blocks the openings, the keep-clear walkway, a rack slot
  or the space in front of a terminal slot (placeholder 0.8 x 2 x 0.6 m plus 1 m to stand), or sticks out
  of the room interior. Run them after every hand edit of a room.
- `Tools/Cold Shutdown/Create Room Preview Scene` rebuilds `Scenes/Sandbox_B.unity` (Corridor, ServerAisle,
  Office in a row, unused openings plugged, placeholder boxes on the rack slots, sandbox player, no scene
  lights: the rooms are lit by their own fittings) — a hand-made stand-in for the level builder. The
  placeholder racks exist only in that scene.
  `SandboxSceneTool.CreatePlayer()` is `internal` and returns the player so both sandbox scenes share it.

## Level builder and debug menu

- `Scenes/Main.unity` is built by `Tools/Cold Shutdown/Create Main Scene`: the player prefab, a `Level Builder`
  (`LevelBuilder`, Runtime) wired to the three `RoomDefinition` assets and `WallPlug.prefab`, and a `Debug Menu`.
  Press Play and a random map is built around the player (start cell = middle of the map). Default size 6 x 6,
  set on the builder.
- **Data flow:** `MapGenerator.Generate(seed, w, h)` → `MapData` → `RoomLayout.Generate(map, seed, types)` (a
  `PlacedRoom` per cell: type + quarter turns) → `LevelBuilder.Build(seed, w, h)` instantiates. Everything before
  the builder is plain Core data and deterministic. `RoomLayout.Generate` is a **placeholder** (random type and
  turn per cell) until task 2.1 adds quotas, entry, gateway and the rogue rack.
- **Hierarchy:** `Level Builder / Level / Cell_<column>_<row> / Room_<Type>` plus `WallPlug_<Side>` for every
  closed side. A cell object sits at `(column * 12, 0, row * 12)` and never turns; only the room inside turns, so
  plugs stay where the map says the openings are (they use `RoomContract.GetOpeningPose`). The builder only
  plugs openings that are not connected, including the outer edge of the map.
- `LevelBuilder` raises `LevelBuilt` after every build; other systems listen to it instead of referencing the
  builder. `Build` replaces the previous level; `Clear` removes it.
- **F1 debug menu** (`DebugMenu`): seed, map size, Regenerate, random seed, FPS, and the map as text
  (`MapData.ToText`; `@` is the player, `C`/`S`/`O` the room types). Open = player stands still and the cursor
  is free. Active in the Editor and development builds only.
- Tests: `Tests/EditMode/RoomLayoutTests.cs` (Core), `Tests/EditMode/Rooms/LevelBuilderTests.cs` and
  `MainSceneTests.cs` (build real levels from the room prefabs).

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
