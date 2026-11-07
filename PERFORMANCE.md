# Performance

What it costs to draw the world, what was done about it, and how to measure it again. Raw numbers: `perf/sandbox_b_2026-10-31.csv`.

## What was wrong (2026-10-31, `Sandbox_B`, which holds the whole world)

- 25.5 million mesh triangles in 10 000 renderers, 16 500 objects, 1 400 colliders, 121 realtime spot lights (no shadows), nothing static.
- **338 server racks were 24 of those 25.5 million triangles** (about 71 000 each): the random filling of a rack (servers, cables, ports) is one
  big mesh, and it was drawn for every rack in view, however far away.
- Screen space ambient occlusion was set to "depth and normals", which draws every mesh of the scene once more before the real pass.
- The frame rate was not capped and vertical sync was off, so a laptop rendered flat out until it ran hot and slowed itself down (stutter).

## What was done

| Fix | Where | Effect |
|---|---|---|
| Rack level of detail | `Rack.AddLevelOfDetail` (called when the rack prefab is built and again from `Rack.Awake`, so the Scene view saves the work too): the filling is drawn only while the rack fills more than 30 % of the screen's height (about 7 m; `Rack.ContentsScreenHeight`, 0.5), the body and the door always | by far the biggest win |
| Cheaper ambient occlusion | `Tools/Cold Shutdown/Apply Performance Settings` (`PerformanceSettingsTool`): from the depth (not depth and normals), at half resolution | about 1.4 ms per frame |
| GPU occlusion culling | the same tool: `GPU Resident Drawer` was already on, this turns on its occlusion culling (no baking: the world is built at run time) | about 1.3 ms per frame |
| Frame cap | `FrameRateCap`: 60 frames per second, vertical sync off (`FrameRateCap.TargetFrameRate`, 0 or less removes the cap) | steady frame times, cooler laptop |

## Measured (1920 x 1080 off-screen, RTX 4060 laptop, Unity batch mode, medians of three rounds over seven viewpoints)

Seven viewpoints: the lift start, and three free spots in the hall, each looking north and east. Frame time is the CPU and the GPU together (a pixel is
read back after every frame); the frame cap is lifted for the measurement.

| | 1. Before | 2. + rack LOD | 3. + cheaper SSAO | 4. + occlusion culling (all) |
|---|---|---|---|---|
| Frame time, mean of the viewpoints | 31.7 ms | 14.5 ms | 13.1 ms | **11.8 ms** |
| Worst frame | 69.9 ms | 23.4 ms | 22.2 ms | **20.6 ms** |
| Triangles drawn, mean | 20.0 M | 4.6 M | 4.6 M | not countable (see below) |
| Draw calls, mean | 2 205 | 2 202 | 2 202 | not comparable (see below) |
| Viewpoints within 16.7 ms (60 fps) | 1 of 7 | 6 of 7 | 6 of 7 | **7 of 7** |
| Target frame rate set by the game | none (unlimited), vsync off | 60, vsync off | 60, vsync off | 60, vsync off |

(The target frame rate of columns 2 to 4 is the code that was in the project at that time; in column 1 the cap was only lifted again for the measurement. Before
the fix the game had no cap: `Application.targetFrameRate` was -1.)

Per viewpoint (frame time, rendered triangles):

| Viewpoint | Before | After all four |
|---|---|---|
| lift start | 62.9 ms, 49.9 M | 9.7 ms |
| hall spot 1 north | 20.9 ms, 10.9 M | 9.2 ms |
| hall spot 1 east | 45.4 ms, 30.8 M | 16.6 ms |
| hall spot 2 north | 29.3 ms, 19.0 M | 10.7 ms |
| hall spot 2 east | 23.0 ms, 12.5 M | 14.3 ms |
| hall spot 3 north | 24.6 ms, 13.7 M | 9.3 ms |
| hall spot 3 east | 15.4 ms, 3.6 M | 12.5 ms |

### How to read the numbers

- **Triangle and draw call counters stop being usable in column 4.** With GPU occlusion culling the draws are issued by the GPU, so the counters of the
  profiler read about 0 triangles and a higher number of draw calls. Compare frame times there, not those two rows. Columns 1 to 3 are comparable.
- The triangles drawn are more than the scene's 25.5 M because the ambient occlusion pass draws the scene a second time (the lift start sees nearly
  everything: 49.9 M = twice the scene). That is also why "depth and normals" cost so much.
- This is noisy on a laptop (clocks move): the same view of the same build measured 24 and 32 ms on average in two runs of the "before" state. The
  *order* of the fixes is solid (rack LOD >> SSAO about equal to occlusion culling); the exact milliseconds are not. The medians over three rounds
  soften that, they do not remove it.
- Measured without the Editor's own cost (Scene view, gizmos, inspector) and without gameplay scripts. Expect the Editor to add on top.
- The first, single-pass measurement of the "before" state read a mean of 24 ms and 34 ms at the lift start (this table: 31.7 and 62.9 ms), the same build.

## Stutter next to the racks (second round)

The user still saw stutter next to racks at an average of 60 fps. Walking the camera through the hall at walking speed (`SandboxB_WalkingThroughTheHall_FrameTimeSpikes`,
1080p, three-quarters of a minute of frames, medians) showed why: the *average* was fine but the racks beside the lane cost 16.9 ms a frame, over the 16.7 ms
that 60 fps allows, so frames were missing their slot (a rack fills many pixels, and every one is lit by every light that reaches it).

| Walking the hall | Rack LOD at 0.3 (about 10 m) | at 0.5 (about 7 m, now) | at 0.8 | no rack contents at all | no lights at all |
|---|---|---|---|---|---|
| lane, looking along it | 11.8 ms | **5.2 ms** | 5.3 ms | 5.2 ms | 5.0 ms |
| looking at the racks beside | 16.9 ms (p99 21.5, worst 22.7) | **7.1 ms** (p99 8.6, worst 9.4) | 7.3 ms | 7.1 ms | 6.5 ms |

- The racks' contents are nearly all of what is left of the cost, and so are the lights: either one removed brings the frame down to about 7 ms. They multiply.
  The filling is therefore drawn only within about 7 m now. Nothing was seen to pop in the measurement (the filling sits behind the closed door grille).
- No hitch frames (a frame over twice the median) were found in the walk, and the garbage collector gets about 0.7 KB per frame (one frame in the 700 about 53 KB):
  the stutter was not garbage collection.
- The player's movement was checked too (`SandboxB_PlayerMovement_IsEvenAlongRacksAndAtExtinguishers`): straight down the lane the speed is a constant
  2.60 m/s, sliding along the racks it is constant, no height jumps. The fire extinguisher has no collider, so walking close to it does not touch anything.
  The few runs that did show the player being pushed (0.35 m) were starts inside a partition wall, not a real path.
- Not found, and not measurable without the Editor: a hitch that happens once the first time something comes into view (the Editor compiles a shader
  variant or uploads a texture then). If the stutter only happens the first time at each place, that is it, and a build will not show it.

## What is left (not done, in the order of what it would give)

1. Hall racks and props are still separate renderers (about 10 000): merging static geometry per room, or LODs for the rest of the props, would cut the work
   of the CPU. Nothing is marked static.
2. 121 realtime spot lights in Forward+: shorter ranges, or fewer fittings per room, helps the pixel shader cost.
3. The rack fillings are 12 meshes of about 16 MB each, with read/write on: switching it off saves memory.
4. Windows: make sure the Unity Editor runs on the RTX (Settings > System > Display > Graphics > add Unity.exe > High performance) and the laptop is plugged in
   with the power mode set to best performance. In the Editor, close the Scene view and turn off Gizmos while playing (Maximize On Play).

## Measuring again

The PlayMode test `PerformanceBaselineTests` plays `Sandbox_B`, renders seven viewpoints by hand, and appends rows to a CSV. It does nothing unless
`COLDSHUTDOWN_PERF_OUT` is set, so the normal test run ignores it. It needs a GPU (no `-nographics`) and the Editor closed:

```
set COLDSHUTDOWN_PERF_OUT=C:\temp\perf.csv
set COLDSHUTDOWN_PERF_LABEL=my change
"<path-to-Unity.exe>" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter PerformanceBaselineTests -testResults results.xml
```

`COLDSHUTDOWN_PERF_NO_LOD=1` takes the racks' level of detail away again, to measure the world without it. Run it more than once and compare medians.
