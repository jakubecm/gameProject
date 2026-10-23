using System.Collections.Generic;
using System.IO;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ColdShutdown.Editor
{
    /// <summary>
    /// Creates the player prefab (first-person controller, input, head bob, footsteps, camera with the
    /// interactor and its prompt) and swaps it into the sandbox scenes in place of the throwaway
    /// sandbox player.
    /// </summary>
    public static class PlayerPrefabTool
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        private const float StandingHeight = 1.8f;
        private const float Radius = 0.3f;
        private const float EyeHeight = 1.66f;

        private static readonly string[] SandboxScenes =
        {
            "Assets/_Project/Scenes/Sandbox_A.unity",
            "Assets/_Project/Scenes/Sandbox_B.unity",
        };

        [MenuItem("Tools/Cold Shutdown/Create Player Prefab")]
        public static void CreatePlayerPrefab()
        {
            if (File.Exists(PrefabPath))
            {
                Debug.Log($"{PrefabPath} already exists, skipped. Delete it to regenerate.");
                return;
            }

            var player = BuildPlayer();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
            Object.DestroyImmediate(player);
            Debug.Log($"Player prefab saved to {PrefabPath}.");
        }

        /// <summary>
        /// Replaces every sandbox player (a "Player" with a CharacterController but no
        /// <see cref="FirstPersonController"/>) in the sandbox scenes with the player prefab, keeping its
        /// position and facing.
        /// </summary>
        [MenuItem("Tools/Cold Shutdown/Use Player Prefab In Sandbox Scenes")]
        public static void UsePlayerPrefabInSandboxScenes()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            foreach (string scenePath in SandboxScenes)
            {
                if (File.Exists(scenePath))
                {
                    UsePlayerPrefabInScene(scenePath);
                }
            }
        }

        /// <summary>Replaces the sandbox players in one scene with the player prefab and saves the scene.</summary>
        public static void UsePlayerPrefabInScene(string scenePath)
        {
            CreatePlayerPrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var oldPlayers = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Player" && root.GetComponent<CharacterController>() != null && root.GetComponent<FirstPersonController>() == null)
                {
                    oldPlayers.Add(root);
                }
            }

            foreach (var old in oldPlayers)
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                player.transform.SetPositionAndRotation(old.transform.position, Quaternion.Euler(0f, old.transform.eulerAngles.y, 0f));
                Object.DestroyImmediate(old);
            }

            if (oldPlayers.Count > 0)
            {
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"{scenePath}: replaced {oldPlayers.Count} sandbox player(s) with {PrefabPath}.");
        }

        private static GameObject BuildPlayer()
        {
            var player = new GameObject("Player");

            var body = player.AddComponent<CharacterController>();
            body.height = StandingHeight;
            body.radius = Radius;
            body.center = new Vector3(0f, StandingHeight / 2f, 0f);
            // Steps up kerbs and floor tiles, not onto desks.
            body.stepOffset = 0.35f;
            body.slopeLimit = 45f;
            body.skinWidth = 0.03f;
            body.minMoveDistance = 0f;

            var input = player.AddComponent<PlayerInputReader>();
            input.ResetToDefaultBindings();

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, EyeHeight, 0f);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(pivot, false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.fieldOfView = 70f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<HeadBob>();
            cameraObject.AddComponent<Interactor>();
            cameraObject.AddComponent<InteractionPrompt>();

            var controller = player.AddComponent<FirstPersonController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("cameraPivot").objectReferenceValue = pivot;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var audioSource = player.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            player.AddComponent<FootstepAudio>();

            return player;
        }
    }
}
