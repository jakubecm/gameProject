using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>Checks that Main.unity (made by Tools/Cold Shutdown/Create Main Scene) is wired up and can build a level.</summary>
    public class MainSceneTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        private Scene scene;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), $"{ScenePath} is missing. Run Tools/Cold Shutdown/Create Main Scene.");
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.CloseScene(scene, true);
        }

        [Test]
        public void Scene_HasAWiredLevelBuilderAndPlayer()
        {
            var builder = FindInScene<LevelBuilder>();
            Assert.IsNotNull(builder, "No LevelBuilder in Main");
            Assert.IsNotNull(builder.Player, "The LevelBuilder has no player");
            Assert.IsNotNull(builder.Player.GetComponent<FirstPersonController>(), "The player is not the player prefab");
            Assert.IsNotNull(FindInScene<DebugMenu>(), "No DebugMenu in Main");
        }

        [Test]
        public void Scene_BuildsALevelAroundThePlayer()
        {
            var builder = FindInScene<LevelBuilder>();
            Assert.IsTrue(builder.Build(123));

            Assert.IsTrue(builder.HasLevel);
            Assert.AreEqual(builder.Width * builder.Height, builder.Layout.Rooms.Count);

            var (column, row) = builder.StartCell;
            Assert.AreEqual(column * 12f, builder.Player.position.x, 0.001f);
            Assert.AreEqual(row * 12f, builder.Player.position.z, 0.001f);
            builder.Clear();
        }

        private T FindInScene<T>() where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }
    }
}
