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
    /// interactor and its prompt, the visible body) and swaps it into the sandbox scenes in place of the
    /// throwaway sandbox player. Update Player Prefab brings an existing prefab up to date without
    /// replacing values that were tuned by hand.
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
        /// Brings the existing player prefab up to date: adds the body if it is missing, and moves tuning values
        /// that still have an old default to the new default. A value someone changed by hand is left alone.
        /// </summary>
        [MenuItem("Tools/Cold Shutdown/Update Player Prefab")]
        public static void UpdatePlayerPrefab()
        {
            if (!File.Exists(PrefabPath))
            {
                CreatePlayerPrefab();
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                bool changed = false;
                if (root.transform.Find("Body") == null)
                {
                    PlayerBodyBuilder.AddBody(root);
                    Debug.Log("Player prefab: added the Body.");
                    changed = true;
                }

                changed |= MigrateTuning(root);
                changed |= ResetBodyPosition(root);
                changed |= HideTheChestFromTheCamera(root);

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    Debug.Log($"Player prefab updated: {PrefabPath}.");
                }
                else
                {
                    Debug.Log("Player prefab is already up to date.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>The first version of the body drew its chest to the camera, which filled the screen when looking down.</summary>
        private static bool HideTheChestFromTheCamera(GameObject root)
        {
            var body = root.transform.Find("Body");
            if (body == null) return false;

            bool changed = false;
            foreach (var renderer in body.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name != "Mesh_Torso" || renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) continue;

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                Debug.Log("Player prefab: the chest is no longer drawn to the camera.");
                changed = true;
            }
            return changed;
        }

        /// <summary>Puts the body back to its standard position; a first version of the body had a fixed height offset.</summary>
        private static bool ResetBodyPosition(GameObject root)
        {
            var body = root.transform.Find("Body");
            if (body == null) return false;

            var wanted = PlayerBodyBuilder.BodyPosition(root);
            if ((body.localPosition - wanted).sqrMagnitude < 1e-8f) return false;

            body.localPosition = wanted;
            Debug.Log($"Player prefab: Body position set to {wanted}.");
            return true;
        }

        /// <summary>Old default values from before the jump, the calmer head bob and the gentler crouch.</summary>
        private static bool MigrateTuning(GameObject root)
        {
            bool changed = false;

            var controller = root.GetComponent<FirstPersonController>();
            if (controller != null)
            {
                var serialized = new SerializedObject(controller);
                changed |= MigrateFloat(serialized, "crouchingHeight", 1.05f, 1.3f);
                changed |= MigrateFloat(serialized, "crouchTime", 0.2f, 0.3f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var headBob = root.GetComponentInChildren<HeadBob>();
            if (headBob != null)
            {
                var serialized = new SerializedObject(headBob);
                changed |= MigrateFloat(serialized, "verticalAmplitude", 0.035f, 0.018f);
                changed |= MigrateFloat(serialized, "lateralAmplitude", 0.022f, 0.012f);
                changed |= MigrateFloat(serialized, "rollAmplitude", 0.35f, 0.2f);
                changed |= MigrateFloat(serialized, "sprintFactor", 1.4f, 0.85f);
                changed |= MigrateFloat(serialized, "sprintFovIncrease", 5f, 3f);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return changed;
        }

        private static bool MigrateFloat(SerializedObject serialized, string field, float oldDefault, float newDefault)
        {
            var property = serialized.FindProperty(field);
            if (property == null || !Mathf.Approximately(property.floatValue, oldDefault)) return false;

            property.floatValue = newDefault;
            Debug.Log($"Player prefab: {serialized.targetObject.GetType().Name}.{field} {oldDefault} -> {newDefault}.");
            return true;
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

            PlayerBodyBuilder.AddBody(player);

            return player;
        }
    }
}
