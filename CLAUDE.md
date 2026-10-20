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
    Prefabs/    Rooms/, Props/, Terminals/
    Scenes/     Main.unity, Sandbox_A.unity, Sandbox_B.unity
    Tests/
      EditMode/ tests for ColdShutdown.Core — asmdef ColdShutdown.Tests.EditMode
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
- Systems communicate through C# events (e.g. `OnClueFound`, `OnInfectionStep`), not direct references
  between unrelated components.
- Texts (lore, AI messages, briefing) are data in `Assets/_Project/Data`, never hard-coded strings.
- Lighting is realtime (Forward+). No baked lightmaps — the map is generated at runtime.

## Room contract

The numbers live in one place: `RoomContract` in `Core` (`Assets/_Project/Code/Core/RoomContract.cs`).
Generator, level builder and editor tools read them from there; this section only documents them.
Change both together.

- **Cell:** 12 × 12 m, ceiling 3 m. Room pivot = center of the floor, floor at y = 0. The generator
  places a room at `(column * 12, 0, row * 12)`. +Z is north.
- **Walls:** 0.2 m thick, inside the cell (neighbouring rooms' walls stand back to back), so the
  walkable interior is 11.6 × 11.6 m.
- **Openings:** one in the middle of each side, 2.4 m wide and 2.6 m high, always in the same place.
  A separate wall-plug prefab fills exactly one opening.
- **Keep-clear cross:** a 2.4 m wide walkway from every opening to the center must stay empty.
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
