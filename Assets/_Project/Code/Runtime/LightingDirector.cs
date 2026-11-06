using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ColdShutdown.Runtime
{
    /// <summary>
    /// The one lighting setup of the game: one fixed setting, the best the code has (no quality levels and no adapting to the
    /// graphics card; optimising comes later). The room fittings are cheap realtime spots (no shadows
    /// anywhere in the game, nothing flickers); this director adds what makes them look real:
    /// - the lights themselves are left exactly as the fittings set them: one colour temperature, no changes while the game runs;
    /// - no reflection probe and nothing else that depends on where the player stands: a probe that is redrawn as the player walks changed
    ///   the brightness of the whole scene;
    /// - a global post-processing volume (filmic tonemapping, bloom on the emissive fittings, a little contrast, vignette, grain) and
    ///   SMAA with dithering on the game cameras (dark gradients do not band);
    /// It installs itself in every scene when Play starts; there is nothing to wire and no setting for the player.
    /// </summary>
    [DisallowMultipleComponent]
    public class LightingDirector : MonoBehaviour
    {
        private const float CameraCheckSeconds = 2f;

        private readonly HashSet<Camera> configuredCameras = new HashSet<Camera>();
        private readonly Camera[] cameraBuffer = new Camera[16];

        private VolumeProfile profile;
        private float nextCameraCheck;

        public static LightingDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (GraphicsSettings.currentRenderPipeline == null || FindAnyObjectByType<LightingDirector>() != null) return;
            new GameObject("Lighting Director").AddComponent<LightingDirector>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateVolume();
            // A camera is set up before its first frame is drawn, not at the next periodic check: a camera that was switched off (the player's,
            // while the menu is shown) is not listed by Camera.GetAllCameras, and would show a flat picture without the post-processing.
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (IsGameCamera(camera) && configuredCameras.Add(camera)) Configure(camera);
        }

        private void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            if (Instance == this) Instance = null;
            if (profile != null) Destroy(profile);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextCameraCheck)
            {
                nextCameraCheck = Time.unscaledTime + CameraCheckSeconds;
                ConfigureCameras();
            }
        }

        // ---- cameras and the post-processing volume ----

        private void CreateVolume()
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Lighting Volume (runtime)";

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.Override(TonemappingMode.ACES);

            // The fittings' emission is above 1, so only they glow.
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.3f);
            bloom.scatter.Override(0.65f);
            bloom.highQualityFiltering.Override(false);

            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(0.3f);
            color.contrast.Override(10f);
            color.saturation.Override(-8f);

            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.4f);

            var grain = profile.Add<FilmGrain>();
            grain.intensity.Override(0.12f);
            grain.response.Override(0.8f);

            var volumeObject = new GameObject("Post Volume");
            volumeObject.transform.SetParent(transform, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.sharedProfile = profile;
        }

        private void ConfigureCameras()
        {
            int count = Camera.GetAllCameras(cameraBuffer);
            for (int i = 0; i < count; i++)
            {
                var camera = cameraBuffer[i];
                if (!IsGameCamera(camera) || !configuredCameras.Add(camera)) continue;

                Configure(camera);
            }
        }

        /// <summary>Post-processing, SMAA and dithering on a game camera. Safe to call on a switched-off camera, before it is first shown.</summary>
        public static void Configure(Camera camera)
        {
            if (camera == null || GraphicsSettings.currentRenderPipeline == null) return;

            if (!camera.TryGetComponent(out UniversalAdditionalCameraData data)) data = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.dithering = true;
        }

        private static bool IsGameCamera(Camera camera) => camera != null && camera.cameraType == CameraType.Game && camera.targetTexture == null;
    }
}
