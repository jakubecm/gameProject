using ColdShutdown.Core;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>Checks that Main.unity (made by Tools/Cold Shutdown/Create Main Scene) is wired up and can build the world map.</summary>
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
            Assert.IsNotNull(builder.WorldMap, "The LevelBuilder has no world map");
            Assert.IsTrue(builder.Build());

            Assert.IsTrue(builder.HasLevel);
            Assert.Greater(builder.Layout.Rooms.Count, 0);

            // The player starts in the lift car, in the start cell.
            var (column, row) = builder.StartCell;
            Assert.AreEqual(0f, Vector3.Distance(builder.StartPose.position, builder.Player.position), 0.001f);
            Assert.AreEqual(column, RoomContract.ColumnAt(builder.Player.position.x));
            Assert.AreEqual(row, RoomContract.RowAt(builder.Player.position.z));
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
