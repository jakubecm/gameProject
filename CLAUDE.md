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
  Data/          WorldMap.asset (the map), RoomDefinition_<Type>.asset, HallKit.asset, RackContents.asset, DesktopTheme_Office.asset;
                 texts as JSON: Lore/ (knowledge base), Web/ (the in-game web), Minigames/ (password words)
  Prefabs/       Rooms/ (shell, wall plug, Room_<Type>[_Layout<n>], Room_Hall = baked hall, Hall/ kit pieces), Props/<Prop>, Player/, Terminals/
  Scenes/        Main.unity, Sandbox_A.unity (player test), Sandbox_B.unity (rooms preview), Sandbox_C.unity (terminals)
  Tests/EditMode/        Core tests: map text, room rules, hall plan, contracts (asmdef ColdShutdown.Tests.EditMode)
  Tests/EditMode/Rooms/  prefab/hall/door/rack/level tests (asmdef ColdShutdown.Tests.EditMode.Rooms, references Runtime)
  Tests/EditMode/Player/ player tests (asmdef ColdShutdown.Tests.EditMode.Player)
  Tests/EditMode/Terminals/  desktop, programs, news site, terminal prefab (asmdef ColdShutdown.Tests.EditMode.Terminals)
  Tests/PlayMode/Terminals/  plays Sandbox_C with a simulated mouse (asmdef ColdShutdown.Tests.PlayMode.Terminals)
  Tests/PlayMode/Performance/ measures what Sandbox_B costs to draw, only with COLDSHUTDOWN_PERF_OUT set (asmdef ColdShutdown.Tests.PlayMode.Performance)
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
| Terminals | Runtime/Terminals: `Terminal` (walk-up), `Desktop`, `BrowserApp`, `LoreApp`, `PasswordApp`; Core: `PasswordPuzzle`; see Terminals |
| Racks | Runtime: `Rack` (`Configure(openable, seed)`, `AddLevelOfDetail`: the filling is only drawn near the player), `RackDoor` (on the door hinge; F opens/shuts only when looking at the door; `OpenChanged`), `RackGlow`, `RackPlacement` (which racks open: one per row, never the first or last rack of a run), `RackContentLibrary` |
| Doors | Runtime: `AutoSlidingDoor` (photo cells, readers, `IsLocked`), `AisleDoor` (derived, single leaf) |
| CCTV | Runtime: `SecurityCamera`, `SecurityMonitor`, `SecurityMonitorWall` (tours all cameras, new shots on all monitors every 8 s, never the same shot twice (`SecurityCamera.ShotKey`); one render of every feed at start; then live (2 feeds per frame) only while the player is in the room, frozen when they leave; Play mode) |
| Frame rate | Runtime: `FrameRateCap` (60 frames per second, vertical sync off; `TargetFrameRate` for the settings menu) |
| Clue displays | Runtime: `AnalogGauge` (needle follows `Value` 0-1) |
| Interaction | Runtime: `IInteractable`, `Interactable`, `Interactor` (on camera), `InteractionPrompt`, `InteractableColorToggle` (test) |
| Game flow and menus | Runtime/UI: `GameFlow` (main menu → playing ⇄ paused; swaps the menu camera and the player; Escape = pause), `UiScreen` pages built from code (`MainMenuScreen` (every row placed and centred by the real ink of its glyphs: `UiKit.Ink`/`Stack`, measured in `UiScreen.Arrange`; gaps `TitleGap`/`ItemGap`), `PauseScreen`, `SettingsScreen` (volume and mute, not connected yet)), `MenuCameraRig` (constant-speed shots through the map: one hall move, one slider through an opening door into the power room (else the security office; only added when its run is free of walls), drone arcs, corridor dollies; motion carries over the cuts), `UiKit` (widgets, one look). `GameFlow` sits in `Main` (made by Create Main Scene / Create Preview Scene) |
| Coffee machine | Runtime: `CoffeeMachine` (E brews, cup fills; E again drinks, the coffee runs over the rim as the cup tips; no sound yet), `CoffeeMachineUse` (the interactable, off while busy), `CoffeeLiquidMesh` (the surface is the exact cut of a world-level plane through the mug interior; volume by slices; `Capacity` = what a tilted mug holds before it runs over the rim; the last film draws back to the rim) |
| Lighting | Runtime: `LightingDirector` (post volume, SMAA, set on every camera before its first frame; installs itself), `EmissionLevels` (Core: the two glow levels); Editor: `World/WorldLighting` (ambient, pipeline settings). ROOMS.md, Lighting |
| Debug | Runtime: `DebugMenu` (F1: FPS, rebuild, map as text; Editor and development builds only) |
| Player | Runtime/Player: see Player |
| World (editor) | `World/`: see Project layout; `WorldTool` = Create Preview Scene, `MainSceneTool`, `WorldBatch` (batch entry) |
| Other editor tools | `PlayerPrefabTool`, `SandboxSceneTool` (Sandbox_A) |

Terminals (computers with a desktop, browser, knowledge base and the Password minigame) are built: see Terminals below.
Performance numbers, what was done and how to measure again: PERFORMANCE.md.
Not built yet (planned): run state, clues/deduction, the other three minigames, infection steps, AI messages, settings wiring, audio.
Systems talk through C# events (e.g. `OnClueFound`, `OnInfectionStep`), not direct references.

## Architecture rules

- `Core` never references `UnityEngine`; it returns plain data that Runtime turns into a scene.
- Texts (lore, AI messages, briefing) are data in `Assets/_Project/Data`, never hard-coded strings.
- Lighting is realtime (Forward+), no baked lightmaps; **no shadows anywhere** (no code, no shadow maps), **the room lights never flicker** (the rack door glow keeps its slow breathing); one fixed lighting setting, nothing in the settings screen.
- Contract numbers live only in `RoomContract` / `HallContract`; tools and docs read them from there.
- Generated assets (props, rooms, hall, meshes, generated textures, materials, Sandbox_B) are outputs of the code: commit
  them with their `.meta` (scenes and prefabs reference them by GUID), never edit them by hand.
- Everything a room is made of is generated by the editor tools; **change the tools, not the generated prefabs,
  materials or meshes** (they are rebuilt in place and hand edits are lost).

## Editor tools (Tools/Cold Shutdown/...)

- **Create Preview Scene** (`WorldTool`) – the one tool for the world. Checks the map, then rebuilds from code: every
  generated texture, the materials, every model, every room layout, the hall kit; deletes props, rooms, definitions and
  meshes no code makes any more; bakes the hall of the map (`Room_Hall`); sets the lighting pipeline (`WorldLighting`: no shadows, ambient occlusion, quality level) and the room look of the scenes (ambient, no fog); rewires `Main`; builds `Sandbox_B` (left open).
  Run it after any change to the map or to world code. Batch: `-executeMethod ColdShutdown.Editor.WorldBatch.Build`.
  The room shell and wall plug are only created when missing (rooms are prefab variants of the shell).
- **Create Main Scene** – `Main.unity`: player, `LevelBuilder` (builds the map at Play), debug menu.
- **Create Terminal Prefab / Create Terminal Sandbox** – the terminal prefab (also rebuilt by Create Preview Scene) and `Sandbox_C`.
- **Apply Performance Settings** – ambient occlusion from depth at half resolution and GPU occlusion culling in the PC render pipeline assets
  (see PERFORMANCE.md; the test `PerformanceTests` fails until it has been run).
- **Update Desktop Art** – icons, wallpapers and the news photos of the desktop theme (see Terminals).
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

## Terminals (computers)

Interactable computers with a desktop (task 2.4, with the first programs). Code in `Code/Runtime/Terminals/`, tests in
`Tests/EditMode/Terminals/` and `Tests/PlayMode/Terminals/`.

**Two kinds of computer.** The **office workstation** (the `Workstation` prop at the desks, described below) is built: a desktop with a
browser, a knowledge base and the Password minigame. The **terminal** is a different thing that comes later, with a different interface (not
built, not this desktop). The code and the prefab are still named `Terminal` (`Terminal`, `TerminalTool`, `Prefabs/Terminals/`, the
`TerminalSlot` markers in the rooms): today that is the workstation's desktop. When the terminal is built, give the two their own names.

- **The office workstation is the `Workstation` prop** (two monitors, keyboard, mouse at a desk; the colleague's model, PROPS.md), so every desk
  in the rooms (office 1, security office 2) is a computer. `TerminalTool` gives the prop prefab `Prefabs/Props/Workstation.prefab` the
  `Terminal` component, a view point and two world-space canvases laid exactly over the monitors' pictures (the mesh part
  `Monitor_Picture`): the desktop, clickable, on `Monitor_Left` (the right-hand monitor seen from the chair, beside the mouse;
  constant `TerminalTool.ActiveMonitor`), and on `Monitor_Right` a second canvas (`Terminal.secondScreen`) that is black, shows the
  same wallpaper only while the computer is **on** (not while booting or shutting down) and goes black again when it goes off.
  The prop's own box collider is what the interaction ray hits. **Create Preview Scene** rebuilds the models (which start the
  workstation prefab from scratch) and then rewires it (`TerminalTool.BuildTerminalPrefab`); `Create Terminal Prefab` only rewires.
  `Prefabs/Terminals/Terminal.prefab` is a desk with a workstation on it, for `Create Terminal Sandbox` (`Scenes/Sandbox_C.unity`,
  two computers on a plain floor, both off). The `TerminalSlot` markers in the rooms are not used for a computer for now.

- **Walk-up:** `Terminal` (on the computer) is the `IInteractable` itself; its prompt is "Turn on computer" while it is off,
  "Use computer" otherwise. `Interact` switches it on if it is off and `Enter`s: the camera glides (0.6 s, eased, real
  time) to the `ViewPoint`, FOV 45, `PlayerInputReader.InputEnabled = false` (the player stands still), the cursor is
  free, `Interactor`, `InteractionPrompt` (the centre dot goes with it) and `HeadBob` are switched off, a hint "Esc - step away from the computer"
  shows. `Esc` (or `Leave`) glides back and gives everything back. State: `TerminalState` Idle / Entering / InUse /
  Leaving; `Advance(deltaTime)` moves the camera (tests call it by hand). Events: `Entered`, `Exited`, `PowerChanged`,
  `MinigameCompleted(MinigameResult)` (the clue system, task 2.2, listens to the last one).
- **The screen** is a world-space uGUI canvas (1280 x 720 units, stretched to the monitor picture: about 0.53 x 0.31 m, turned with
  the angled monitor) with a `GraphicRaycaster`; the terminal sets the player's camera as its event camera, so the desktop is clicked with the mouse. An `EventSystem` with
  `InputSystemUIInputModule` is created at run time if the scene has none (navigation events off, so WASD does nothing there).
  **The canvas is turned 180 degrees around Y** in the prefab: a canvas is seen correctly from its back, so facing the viewer
  mirrors everything, and Unity's UI ignores clicks on graphics that face away from the camera (`ignoreReversedGraphics`).
  A test checks that the canvas's forward axis points the way the view point looks.
- **Mouse pointer:** while seated, `Terminal.UpdatePointer` draws a pointer (`Desktop.SetPointer`, a white arrow on the top layer of the
  desktop) where the real mouse is over the screen and hides the system cursor there; beside the screen the system cursor shows.
- **Power** (`Desktop.Power`, `PowerState`: Off, Booting, On, ShuttingDown): `startsOn` on the terminal says how it starts
  (the prefab: off). Off = black screen; clicking it, or pressing E on the computer, boots (3 s boot screen with a bar, then
  the desktop). The Start button opens a menu with "Shut down": every window closes (so nothing keeps the game paused), a
  1.2 s "Shutting down..." screen, then black again. While off and seated, the black screen says "Click to power on".
- **Desktop** (`Desktop`, built in code, look from `DesktopTheme` asset `Data/DesktopTheme_Office.asset`: a modern office
  look; change colours and sizes there): wallpaper, icons (one click opens), draggable windows with a close button that come to
  the front when pressed, a taskbar with a button per window and a clock (night shift, starts 02:13). Programs implement
  `ITerminalApp` (`Build`, `Closed`, `Suspended`, `Resumed`) and are added with `Desktop.AddApp`. UI helpers: `UiFactory`
  (legacy `Text` with the built-in font; TextMeshPro would need its resources imported first).
- **Desktop art** (wallpapers and icons): `Tools/Cold Shutdown/Update Desktop Art` (`DesktopArtTool`). It draws placeholder icons
  (a globe, an open book, a padlock; `DesktopArtPainter`, signed distance shapes) into `Art/Terminal/Icons/Icon_browser.png`,
  `Icon_knowledge.png` and `Icon_security.png`, and four placeholder wallpapers (`placeholder_*.png`, 1280 x 720) into
  `Art/Terminal/Wallpapers/` if there are none, sets the import settings (icons uncompressed, wallpapers at most 2048 px)
  and fills `DesktopTheme.icons` and `DesktopTheme.wallpapers`. **To use real pictures:** overwrite an icon file with a better
  picture of the same name (the theme keeps pointing at it), and put wallpaper photos into `Assets/ThirdParty/Wallpapers/`
  (or `Art/Terminal/Wallpapers/`), then run the tool again; the `placeholder_*` files are ignored as soon as there is any
  other wallpaper. Add a row to `CREDITS.md` for every photo. Each computer shows one wallpaper of the theme: random every
  time the game starts (`Terminal.wallpaperIndex` = -1; computers in one scene take the pictures one after another from a
  random start, so two never look the same until the pictures run out) or a fixed one (0, 1, ...). The picture is cut to fit 16:9 without
  stretching (`Desktop.CoverRect`) and a dark veil (`DesktopTheme.wallpaperTint`) keeps the icons readable. A program without
  an icon picture gets a coloured tile with a letter. The icon also shows in the window's title bar and the taskbar.
- **Texts, all English:** captions of the desktop's own controls are constants in `DesktopTexts`. The game's texts are data
  files: `Data/Lore/knowledge_base.json` (`{articles:[{id, category, title, date, text}]}`, `LoreLibrary`),
  `Data/Web/web_pages.json` (`{home, newsNav, pages}`, `WebLibrary`, see "Browser" below) and `Data/Minigames/password_words.json` (`{words:[...]}`, upper case letters
  only). **The documents and the news are drafts** (they start with "DRAFT"); the team writes the real ones (task 4.2). The
  corporation name is not decided, so the texts avoid it. A test checks that no page links to a page that does not exist.
- **Browser** (`BrowserApp`): an address bar, Back and Home, and a scrolling page. Start page "start.local" links to the news
  site **AAC News** and to "Grid Status". `kind` of a page is `portal`, `list` or `article`; an article pauses the game, the
  other pages do not. A page without a `layout` is plain (title, date, text, link buttons named `Link_<target>`).
- **Start page** (`StartPageBuilder`, `layout` "portal"): a dark page with a heading, the page's `text` as subtitle and a tile per
  link (`Tiles/Link_<target>`: coloured badge from the link's `badge` and `color`, label, `note`). Footer text is `DesktopTexts.PortalFooter`.
- **AAC News** (`NewsPageBuilder`, the fictional BBC-style news portal; its own logo and colours, never the real ones): pages
  with `layout` `news_section` or `news_article` get a masthead (orange-red "AAC" badge + "NEWS", `Masthead`), a menu from
  `newsNav` (`Menu/Nav_<target>`, the open section has an `Underline`; an article belongs to the tab of its `section`), and:
  - **section** (`stories: [ids]`): no title (the open tab says it), then `Front` = the lead story (`Card_<id>`, picture 640 x 270, so the headline shows without scrolling, tag line,
    serif headline, summary) next to `Front/Side` (the next three stories with a thumbnail), then `GridRow`s of three small cards;
  - **article** (`Article`): `Tag` (tag and section), `Headline`, `Summary`, `Byline` (name and date), `Photo` (or
    `PhotoPlaceholder`) with `Caption`, `Paragraph_n` (the text split at blank lines) and `Related/Card_<id>` (`related` ids).
  Every story is a whole-card button; headlines use `DesktopTheme.HeadlineFont` (a serif font of the computer, or the font
  assigned to `headlineFont`; drop a free serif TTF such as Source Serif into `Assets/ThirdParty/Fonts/` and assign it to
  the theme asset for the same look on every machine). The browser window is 1120 x 640 so the page is 1056 wide.
  The site is not only about the game's story: sections World, Business, Technology, Climate, Sport (cup final, fictional
  results, marathon, tennis), Culture (festival, film prize, village bakery) and Science (comet, deep-sea survey); the front
  page mixes them. All articles are DRAFTs. `Data/Web/web_pages.json` is generated by a script in the scratchpad of the
  author; edit the JSON by hand now (a story is `layout news_article` with its `section`, listed in that section page's `stories`).
  News page fields: `layout, section, tag, byline, summary, image, caption, stories, related` (see `WebPage`).
  **News pictures:** an article names a picture by `image` (a key). `Update Desktop Art` draws a placeholder photo per key into
  `Art/Web/Images/<key>.png` if missing (`WebImagePainter`, flat drawn scenes, 16:9, 960 x 540: grid, datacenter, ghost, law,
  talks, heat, reservoir, chips, ports, city, football, marathon, tennis, scoreboard, concert, lighthouse, bakery, comet,
  deepsea; add a scene by adding a key and a drawing method) and fills `DesktopTheme.webImages` from `Art/Web/Images` and `Assets/ThirdParty/WebImages`
  (key = file name without extension, lower case; a ThirdParty file wins over a placeholder with the same key; use 16:9
  or any shape, it is cut to fit with `Desktop.CoverRect`). **To use real photos:** put them in `Assets/ThirdParty/WebImages/`
  under the key's name (e.g. `heat.jpg`), run the tool, add a row to `CREDITS.md` per photo (and say if an image is
  AI-generated, as the plan requires). A story whose key has no picture shows a coloured block, so a new key works at once.
  Tests: `DesktopTests` (masthead, menu, cards, article, related, pause), `DesktopArtTests` (photos, theme), `TerminalDataTests`
  (every article has all fields, is on the front page and in its section, and has a placeholder).
- **Seated view:** `Terminal.ViewPosition` puts the camera on the view point's axis, close enough that the screen fills
  `screenFill` (0.9) of the view for the camera's real aspect ratio (`Terminal.ViewDistanceFor`), never farther than the
  view point, so text is as large as the display allows. The prefab's view point is only the farthest position.
- **Knowledge Base** (`LoreApp`, the lore reader of task 3.2): documents listed on the left (incident reports, procedures,
  notices), text on the right (scrolls with the mouse wheel). A document being open pauses the game through `GamePause`
  (time scale 0; several things may hold it, it runs when the last lets go). Leaving the computer, closing the window or
  shutting down releases the pause; sitting down again takes it back. The camera and the interface use real time.
- **Security** (`PasswordApp`, rules in `Core/PasswordPuzzle`, task 2.5): 10 candidate words of the same length, one is the
  password; a pick shows its "match" (letters on the right place), 4 wrong picks lock it ("Try again" makes a new puzzle),
  the puzzle stays when the window is closed and reopened. About half of the candidates look like the password, so answers
  can be used to eliminate words (a player doing that wins ~95 % of the puzzles, see `PasswordPuzzleTests`). The seed is the
  clock's for now. `Completed(MinigameResult)` says how it ended; giving a real clue for it is task 2.2.
- **Tests:** `Tests/PlayMode/Terminals/TerminalClickTests` plays Sandbox_C with a simulated mouse (switch on, sit down, click
  the browser, read an article, back, close, pick a word, shut down, boot again, stand up); it is the test that fails when
  the screen is mirrored. PlayMode tests run from the command line with `-testPlatform PlayMode`.
- **Not done yet:** the other clue minigames (Breach 2.6, Log 3.6, Patch panel 3.7), AI messages on the screen (3.1), the
  lore archive with saving (3.3), a simple extra game, and the final terminal model (2.10, the colleague's: keep the
  `Terminal` component, the `ViewPoint` and the screen canvas, rotated to face the viewer, when replacing the meshes;
  `TerminalTool` finds the monitors and pictures by the names `Monitor_Left`, `Monitor_Right` and `Monitor_Picture`).

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
