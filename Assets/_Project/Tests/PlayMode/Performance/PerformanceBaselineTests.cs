#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ColdShutdown.Runtime;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ColdShutdown.Tests.PlayMode.Performance
{
    /// <summary>
    /// A measurement, not a check: plays Sandbox_B (the whole world), puts the camera at a few places and logs what the renderer was asked to
    /// draw (triangles, draw calls, batches) and how long a frame took with the frame cap lifted. It does nothing unless the environment
    /// variable <c>COLDSHUTDOWN_PERF_OUT</c> names a CSV file to append to, so it never slows or fails the normal test run. Run it before and
    /// after a performance change and keep both files (see PERFORMANCE.md). Needs a GPU: run Unity without -nographics.
    /// </summary>
    public class PerformanceBaselineTests
    {
        public const string OutputVariable = "COLDSHUTDOWN_PERF_OUT";

        private const int WarmupFrames = 25;
        private const int MeasuredFrames = 90;
        private const int Rounds = 3;
        private const float EyeHeight = 1.66f;
        private const int Width = 1920;
        private const int Height = 1080;

        [UnityTest]
        public IEnumerator SandboxB_RenderLoad_AtFixedViewpoints()
        {
            string output = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(output)) Assert.Ignore($"Set {OutputVariable} to a CSV path to measure.");
            string label = Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_LABEL") ?? "run";

            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Sandbox_B.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            // What the game asks for, read before the cap is lifted for the measurement.
            int configuredFrameRate = Application.targetFrameRate;
            int configuredVSync = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            // COLDSHUTDOWN_PERF_NO_LOD=1 takes the racks' level of detail away again, to measure how the world drew without it.
            if (Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_NO_LOD") == "1")
            {
                foreach (var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(group);
                yield return null;
            }

            var camera = Camera.main;
            Assert.IsNotNull(camera, "Sandbox_B has a player camera");
            // A batch run draws nothing by itself, so the camera is rendered by hand into a 1080p target; reading a pixel back waits for the GPU,
            // which makes the time of a frame the time of the CPU and the GPU together.
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.DefaultHDR);
            var pixel = new Texture2D(1, 1, TextureFormat.RGBAHalf, false);
            camera.enabled = false;
            camera.targetTexture = target;
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (behaviour is FirstPersonController || behaviour is PlayerInputReader || behaviour is HeadBob) behaviour.enabled = false;
            }
            var controller = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            if (controller != null) controller.enabled = false;

            var rows = new List<string>();
            var meshes = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            long sceneTriangles = meshes.Where(filter => filter.sharedMesh != null && filter.gameObject.activeInHierarchy).Sum(filter => (long)filter.sharedMesh.triangles.Length / 3);
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            rows.Add(Row(label, "scene (mesh triangles of every active renderer)", 0, 0, sceneTriangles, 0, 0, 0, configuredFrameRate, configuredVSync, renderers.Length));

            var builder = UnityEngine.Object.FindAnyObjectByType<LevelBuilder>();
            var (startPosition, startYaw) = builder.StartPose;
            var views = new List<(string name, Vector3 position, float yaw)> { ("lift start", startPosition, startYaw) };

            // Free spots inside the hall (the player's capsule fits), spread over its floor.
            var hall = GameObject.Find("Room_Hall");
            if (hall != null)
            {
                var bounds = new Bounds(hall.transform.position, Vector3.zero);
                foreach (var renderer in hall.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
                var free = new List<Vector3>();
                for (float x = bounds.min.x + 1f; x < bounds.max.x - 1f; x += 1.5f)
                {
                    for (float z = bounds.min.z + 1f; z < bounds.max.z - 1f; z += 1.5f)
                    {
                        var feet = new Vector3(x, 0.05f, z);
                        if (!Physics.CheckCapsule(feet + Vector3.up * 0.3f, feet + Vector3.up * 1.5f, 0.35f)) free.Add(feet);
                    }
                }
                Assert.GreaterOrEqual(free.Count, 4, "the hall has walkable floor");
                for (int i = 0; i < 3; i++)
                {
                    var spot = free[(int)((i + 0.5f) / 3f * free.Count)];
                    views.Add(($"hall spot {i + 1} looking north", spot, 0f));
                    views.Add(($"hall spot {i + 1} looking east", spot, 90f));
                }
            }

            // Three rounds over all the views, the medians are reported: a laptop's clocks move, one pass is too noisy to compare.
            var samples = views.ToDictionary(view => view.name, view => new List<(double ms, double worst, double triangles, double draws, double setPass)>());
            for (int round = 0; round < Rounds; round++)
            {
                foreach (var (name, position, yaw) in views)
                {
                    camera.transform.SetPositionAndRotation(position + Vector3.up * EyeHeight, Quaternion.Euler(0f, yaw, 0f));
                    for (int i = 0; i < WarmupFrames; i++)
                    {
                        RenderFrame(camera, target, pixel);
                        yield return null;
                    }

                    using (var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count"))
                    using (var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
                    using (var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
                    {
                        double total = 0, worst = 0, triangleSum = 0, drawSum = 0, setPassSum = 0;
                        for (int i = 0; i < MeasuredFrames; i++)
                        {
                            var clock = System.Diagnostics.Stopwatch.StartNew();
                            RenderFrame(camera, target, pixel);
                            double ms = clock.Elapsed.TotalMilliseconds;
                            yield return null;
                            total += ms;
                            worst = Math.Max(worst, ms);
                            triangleSum += triangles.LastValue;
                            drawSum += drawCalls.LastValue;
                            setPassSum += setPass.LastValue;
                        }
                        samples[name].Add((total / MeasuredFrames, worst, triangleSum / MeasuredFrames, drawSum / MeasuredFrames, setPassSum / MeasuredFrames));
                    }
                }
            }
            foreach (var (name, _, _) in views)
            {
                var list = samples[name];
                rows.Add(Row(label, name, Median(list.Select(x => x.ms)), list.Max(x => x.worst), (long)Median(list.Select(x => x.triangles)),
                    (long)Median(list.Select(x => x.draws)), 0, (long)Median(list.Select(x => x.setPass)), configuredFrameRate, configuredVSync, renderers.Length));
            }

            camera.targetTexture = null;
            target.Release();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            if (!File.Exists(output)) File.WriteAllText(output, "label,view,avg_frame_ms,worst_frame_ms,triangles,draw_calls,batches,setpass_calls,configured_target_fps,configured_vsync,renderers\n");
            File.AppendAllLines(output, rows);
            foreach (string row in rows) Debug.Log("PERF " + row);
        }

        /// <summary>
        /// Walks the camera through the hall at walking speed (down the longest free lane, once looking along it and once looking at the racks beside
        /// it) and logs how even the frames are: an average frame rate hides single slow frames, and those are what is seen as stutter.
        /// Needs <c>COLDSHUTDOWN_PERF_WALK_OUT</c>; a row per pass: percentiles, frames over twice the median, and what the garbage collector was given.
        /// </summary>
        [UnityTest]
        public IEnumerator SandboxB_WalkingThroughTheHall_FrameTimeSpikes()
        {
            string output = Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_WALK_OUT");
            if (string.IsNullOrEmpty(output)) Assert.Ignore("Set COLDSHUTDOWN_PERF_WALK_OUT to a CSV path to measure.");
            string label = Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_LABEL") ?? "run";

            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Sandbox_B.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            if (Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_NO_LOD") == "1")
            {
                foreach (var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(group);
                yield return null;
            }

            var camera = Camera.main;
            foreach (var behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (behaviour is FirstPersonController || behaviour is PlayerInputReader || behaviour is HeadBob) behaviour.enabled = false;
            }
            var controller = UnityEngine.Object.FindAnyObjectByType<CharacterController>();
            if (controller != null) controller.enabled = false;
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.DefaultHDR);
            var pixel = new Texture2D(1, 1, TextureFormat.RGBAHalf, false);
            camera.enabled = false;
            camera.targetTexture = target;

            // COLDSHUTDOWN_PERF_LOD_HEIGHT sets the racks' level of detail threshold (see Rack.ContentsScreenHeight) to try other distances.
            string lodHeight = Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_LOD_HEIGHT");
            if (!string.IsNullOrEmpty(lodHeight))
            {
                float height = float.Parse(lodHeight, CultureInfo.InvariantCulture);
                foreach (var group in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    var levels = group.GetLODs();
                    levels[0].screenRelativeTransitionHeight = height;
                    group.SetLODs(levels);
                }
            }

            // COLDSHUTDOWN_PERF_HIDE takes a part of the world away to see what it costs: contents (the racks' fillings), lights, or racks.
            string hide = Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_HIDE");
            if (hide == "contents")
            {
                foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (renderer.name == "Contents") renderer.enabled = false;
            }
            else if (hide == "lights")
            {
                foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) light.enabled = false;
            }
            else if (hide == "racks")
            {
                foreach (var rack in UnityEngine.Object.FindObjectsByType<Rack>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) rack.gameObject.SetActive(false);
            }
            yield return null;

            // The longest run of free floor in the hall along z.
            var hall = GameObject.Find("Room_Hall");
            var bounds = new Bounds(hall.transform.position, Vector3.zero);
            foreach (var renderer in hall.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            float bestX = 0f, bestStart = 0f, bestLength = 0f;
            for (float x = bounds.min.x + 1f; x < bounds.max.x - 1f; x += 0.5f)
            {
                float runStart = 0f, run = 0f;
                for (float z = bounds.min.z + 1f; z < bounds.max.z - 1f; z += 0.5f)
                {
                    var feet = new Vector3(x, 0.05f, z);
                    if (!Physics.CheckCapsule(feet + Vector3.up * 0.3f, feet + Vector3.up * 1.5f, 0.35f))
                    {
                        if (run == 0f) runStart = z;
                        run += 0.5f;
                        if (run > bestLength)
                        {
                            bestLength = run;
                            bestStart = runStart;
                            bestX = x;
                        }
                    }
                    else
                    {
                        run = 0f;
                    }
                }
            }
            Assert.Greater(bestLength, 6f, "the hall has a lane to walk down");

            var rows = new List<string>();
            foreach (var (name, yaw) in new[] { ("looking along the lane", 0f), ("looking at the racks beside", 90f) })
            {
                var frameTimes = new List<double>();
                double gcSum = 0, gcMax = 0;
                using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
                {
                    const float speed = 2.6f;
                    float distance = 0f;
                    int frames = 0;
                    while (distance < bestLength)
                    {
                        var clock = System.Diagnostics.Stopwatch.StartNew();
                        camera.transform.SetPositionAndRotation(new Vector3(bestX, EyeHeight, bestStart + distance), Quaternion.Euler(0f, yaw, 0f));
                        RenderFrame(camera, target, pixel);
                        yield return null;
                        // The time of the whole frame, scripts included: the wait for the next frame is part of what is measured.
                        double ms = Time.unscaledDeltaTime * 1000.0;
                        if (frames > 20)
                        {
                            frameTimes.Add(Math.Max(ms, clock.Elapsed.TotalMilliseconds));
                            gcSum += gc.LastValue;
                            gcMax = Math.Max(gcMax, gc.LastValue);
                        }
                        frames++;
                        distance += speed / 60f;
                    }
                }
                var sorted = frameTimes.OrderBy(x => x).ToList();
                double median = sorted[sorted.Count / 2];
                double Percentile(double q) => sorted[Math.Min(sorted.Count - 1, (int)(q * sorted.Count))];
                int hitches = frameTimes.Count(x => x > median * 2.0 && x > 12.0);
                rows.Add(string.Join(",", label, name, frameTimes.Count, median.ToString("0.00", CultureInfo.InvariantCulture),
                    Percentile(0.95).ToString("0.00", CultureInfo.InvariantCulture), Percentile(0.99).ToString("0.00", CultureInfo.InvariantCulture),
                    sorted.Last().ToString("0.00", CultureInfo.InvariantCulture), hitches,
                    (gcSum / frameTimes.Count / 1024.0).ToString("0.00", CultureInfo.InvariantCulture), (gcMax / 1024.0).ToString("0.0", CultureInfo.InvariantCulture)));
            }

            camera.targetTexture = null;
            target.Release();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            if (!File.Exists(output)) File.WriteAllText(output, "label,pass,frames,median_ms,p95_ms,p99_ms,max_ms,hitch_frames,gc_kb_per_frame,gc_kb_worst_frame\n");
            File.AppendAllLines(output, rows);
            foreach (string row in rows) Debug.Log("WALK " + row);
        }

        /// <summary>
        /// Walks the real player (CharacterController and all) at a fixed 60 frames per second along the hall's lane, sliding along the racks and into
        /// every fire extinguisher, and logs how the player moved: stutter that is not a slow frame is the player jumping (stepping onto something) or
        /// stopping and starting (catching on something). Needs <c>COLDSHUTDOWN_PERF_MOVE_OUT</c>; runs without a GPU.
        /// </summary>
        [UnityTest]
        public IEnumerator SandboxB_PlayerMovement_IsEvenAlongRacksAndAtExtinguishers()
        {
            string output = Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_MOVE_OUT");
            if (string.IsNullOrEmpty(output)) Assert.Ignore("Set COLDSHUTDOWN_PERF_MOVE_OUT to a CSV path to measure.");
            string label = Environment.GetEnvironmentVariable("COLDSHUTDOWN_PERF_LABEL") ?? "run";

            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Scenes/Sandbox_B.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var player = UnityEngine.Object.FindAnyObjectByType<FirstPersonController>();
            Assert.IsNotNull(player);
            var body = player.GetComponent<CharacterController>();
            foreach (var behaviour in player.GetComponentsInChildren<MonoBehaviour>()) if (behaviour is PlayerInputReader || behaviour is FirstPersonController) behaviour.enabled = false;

            var hall = GameObject.Find("Room_Hall");
            var bounds = new Bounds(hall.transform.position, Vector3.zero);
            foreach (var renderer in hall.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            float bestX = 0f, bestStart = 0f, bestLength = 0f;
            for (float x = bounds.min.x + 1f; x < bounds.max.x - 1f; x += 0.5f)
            {
                float runStart = 0f, run = 0f;
                for (float z = bounds.min.z + 1f; z < bounds.max.z - 1f; z += 0.5f)
                {
                    var feet = new Vector3(x, 0.05f, z);
                    if (!Physics.CheckCapsule(feet + Vector3.up * 0.3f, feet + Vector3.up * 1.5f, 0.35f))
                    {
                        if (run == 0f) runStart = z;
                        run += 0.5f;
                        if (run > bestLength)
                        {
                            bestLength = run;
                            bestStart = runStart;
                            bestX = x;
                        }
                    }
                    else
                    {
                        run = 0f;
                    }
                }
            }

            var rows = new List<string>();
            var scenarios = new List<(string name, Vector3 start, float yaw, Vector2 move, int frames)>
            {
                ("lane, straight", new Vector3(bestX, 0.05f, bestStart), 0f, new Vector2(0f, 1f), 160),
                ("lane, pressing into the racks on the right", new Vector3(bestX, 0.05f, bestStart), 0f, new Vector2(1f, 1f), 160),
                ("lane, pressing into the racks on the left", new Vector3(bestX, 0.05f, bestStart), 0f, new Vector2(-1f, 1f), 160),
            };
            foreach (var extinguisher in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.name.StartsWith("FireExtinguisher")).Take(6))
            {
                var away = hall.transform.position - extinguisher.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                var from = extinguisher.position + away.normalized * 2.5f;
                from.y = 0.05f;
                float yaw = Quaternion.LookRotation(-away.normalized).eulerAngles.y;
                scenarios.Add(($"into {extinguisher.name} ({extinguisher.position.x:0.0},{extinguisher.position.z:0.0})", from, yaw, new Vector2(0f, 1f), 160));
            }

            foreach (var (name, start, yaw, move, frames) in scenarios)
            {
                body.enabled = false;
                player.transform.position = start;
                body.enabled = true;
                Physics.SyncTransforms();
                player.SetView(yaw, 0f);
                yield return null;

                var positions = new List<Vector3>();
                for (int i = 0; i < frames; i++)
                {
                    player.Simulate(new PlayerCommand { Move = move }, 1f / 60f);
                    positions.Add(player.transform.position);
                    yield return null;
                }

                // After the first second the player is up to speed: look at the steps from there.
                // What is under the player at the end, and when the player was lifted, what it stepped onto.
                string ground = "";
                var feet = player.transform.position;
                if (Physics.Raycast(feet + Vector3.up * 0.5f, Vector3.down, out var hit, 1f))
                {
                    var t = hit.collider.transform;
                    string path = t.name;
                    for (int up = 0; up < 3 && t.parent != null; up++) { t = t.parent; path = t.name + "/" + path; }
                    ground = $"{path} top {hit.collider.bounds.max.y:0.00} size {hit.collider.bounds.size.x:0.00}x{hit.collider.bounds.size.y:0.00}x{hit.collider.bounds.size.z:0.00}";
                }
                // The frame with the biggest step: where the player was, where it was put, and what stands around those places.
                string lifted = "";
                int worst = 0;
                float biggest = 0f;
                for (int k = 61; k < positions.Count; k++)
                {
                    float step = (positions[k] - positions[k - 1]).magnitude;
                    if (step > biggest)
                    {
                        biggest = step;
                        worst = k;
                    }
                }
                if (biggest > 0.1f)
                {
                    string Near(Vector3 at) => string.Join(" ; ", Physics.OverlapCapsule(at + Vector3.up * 0.3f, at + Vector3.up * 1.5f, 0.45f)
                        .Select(c => $"{c.transform.parent?.name}/{c.name}[{c.bounds.min.y:0.00}..{c.bounds.max.y:0.00}]").Distinct().Take(6));
                    lifted = $"frame {worst}: from ({positions[worst - 1].x:0.00},{positions[worst - 1].y:0.00},{positions[worst - 1].z:0.00}) to ({positions[worst].x:0.00},{positions[worst].y:0.00},{positions[worst].z:0.00}); before: {Near(positions[worst - 1])}; after: {Near(positions[worst])}";
                }
                var steps = new List<double>();
                var heights = new List<double>();
                for (int i = 61; i < positions.Count; i++)
                {
                    var delta = positions[i] - positions[i - 1];
                    steps.Add(new Vector2(delta.x, delta.z).magnitude * 60.0);
                    heights.Add(positions[i].y);
                }
                double top = steps.Max();
                int stops = steps.Count(x => x < top * 0.5);
                int jumps = 0;
                for (int i = 1; i < heights.Count; i++) if (Math.Abs(heights[i] - heights[i - 1]) > 0.004) jumps++;
                rows.Add(string.Join(",", label, "\"" + name + "\"", steps.Average().ToString("0.00", CultureInfo.InvariantCulture), steps.Min().ToString("0.00", CultureInfo.InvariantCulture),
                    top.ToString("0.00", CultureInfo.InvariantCulture), stops, (heights.Max() - heights.Min()).ToString("0.000", CultureInfo.InvariantCulture), jumps, "\"" + ground + "\"", "\"" + lifted + "\""));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            if (!File.Exists(output)) File.WriteAllText(output, "label,scenario,avg_speed_mps,min_speed_mps,max_speed_mps,frames_below_half_speed,height_range_m,height_jumps_over_4mm,ground_at_end,low_colliders_where_lifted\n");
            File.AppendAllLines(output, rows);
            foreach (string row in rows) Debug.Log("MOVE " + row);
        }

        private static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(value => value).ToList();
            return sorted[sorted.Count / 2];
        }

        private static void RenderFrame(Camera camera, RenderTexture target, Texture2D pixel)
        {
            camera.Render();
            RenderTexture.active = target;
            pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            RenderTexture.active = null;
        }

        private static string Row(string label, string view, double avgMs, double worstMs, long triangles, long drawCalls, long batches, long setPass, int targetFps, int vSync, int renderers) =>
            string.Join(",", label, view, avgMs.ToString("0.00", CultureInfo.InvariantCulture), worstMs.ToString("0.00", CultureInfo.InvariantCulture),
                triangles, drawCalls, batches, setPass, targetFps, vSync, renderers);
    }
}
#endif
