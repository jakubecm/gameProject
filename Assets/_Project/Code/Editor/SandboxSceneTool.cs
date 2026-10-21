using System.IO;
using ColdShutdown.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Editor
{
    /// <summary>Creates Sandbox_A: a flat floor, a throwaway player and two test cubes for the interaction system.</summary>
    public static class SandboxSceneTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox_A.unity";

        [MenuItem("Tools/Cold Shutdown/Create Sandbox Scene")]
        public static void CreateSandboxScene()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Create Sandbox Scene", $"{ScenePath} already exists. Replace it?", "Replace", "Cancel"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightObject.AddComponent<Light>().type = LightType.Directional;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            CreatePlayer();
            CreateTestCube("Press Cube", new Vector3(-1.5f, 0.5f, 3f), "Použít", 0f);
            CreateTestCube("Hold Cube", new Vector3(1.5f, 0.5f, 3f), "Podržet", 1.5f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"Sandbox scene saved to {ScenePath}. Press Play: look with the mouse, walk with WASD, press E on the left cube, hold E on the right one.");
        }

        private static void CreatePlayer()
        {
            var player = new GameObject("Player");
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<Interactor>();
            cameraObject.AddComponent<InteractionPrompt>();

            var playerController = player.AddComponent<SandboxPlayerController>();
            var serialized = new SerializedObject(playerController);
            serialized.FindProperty("cameraPivot").objectReferenceValue = cameraObject.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateTestCube(string cubeName, Vector3 position, string prompt, float holdDuration)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = cubeName;
            cube.transform.position = position;

            var interactable = cube.AddComponent<Interactable>();
            var serialized = new SerializedObject(interactable);
            serialized.FindProperty("prompt").stringValue = prompt;
            serialized.FindProperty("holdDuration").floatValue = holdDuration;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            cube.AddComponent<InteractableColorToggle>();
        }
    }
}
