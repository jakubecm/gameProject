using System.Linq;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Rooms
{
    /// <summary>The cheap-to-draw settings (see PERFORMANCE.md): rack level of detail, frame cap, ambient occlusion and occlusion culling.</summary>
    public class PerformanceTests
    {
        private const string RackPath = "Assets/_Project/Prefabs/Props/Rack.prefab";
        private const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";

        private GameObject rack;
        private int frameRate;
        private int vSync;

        [SetUp]
        public void SetUp()
        {
            frameRate = Application.targetFrameRate;
            vSync = QualitySettings.vSyncCount;
        }

        [TearDown]
        public void TearDown()
        {
            if (rack != null) Object.DestroyImmediate(rack);
            Application.targetFrameRate = frameRate;
            QualitySettings.vSyncCount = vSync;
            FrameRateCap.TargetFrameRate = FrameRateCap.DefaultTargetFrameRate;
            Application.targetFrameRate = frameRate;
            QualitySettings.vSyncCount = vSync;
        }

        [Test]
        public void ARack_OnlyDrawsItsFilling_WhileItIsNear()
        {
            rack = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RackPath));
            var component = rack.GetComponent<Rack>();
            component.AddLevelOfDetail();
            component.AddLevelOfDetail();

            var groups = rack.GetComponents<LODGroup>();
            Assert.AreEqual(1, groups.Length, "asking twice does not add a second group");
            var levels = groups[0].GetLODs();
            Assert.AreEqual(2, levels.Length);
            Assert.AreEqual(Rack.ContentsScreenHeight, levels[0].screenRelativeTransitionHeight, 1e-4f);
            Assert.AreEqual(1, levels[0].renderers.Length, "only the filling has a detailed level");
            Assert.AreEqual("Contents", levels[0].renderers[0].name);
            Assert.IsEmpty(levels[1].renderers, "and nothing is drawn of it beyond");

            var always = rack.GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.name != "Contents").ToList();
            Assert.IsNotEmpty(always, "the body and the door are not in the group, so they stay");
            foreach (var renderer in always) Assert.IsFalse(levels.Any(level => level.renderers.Contains(renderer)), renderer.name);
        }

        [Test]
        public void TheFrameRateCap_SetsTheTargetFrameRate_AndTurnsVerticalSyncOff()
        {
            QualitySettings.vSyncCount = 1;
            FrameRateCap.TargetFrameRate = 60;
            Assert.AreEqual(60, Application.targetFrameRate);
            Assert.AreEqual(0, QualitySettings.vSyncCount, "vertical sync would override the cap");

            FrameRateCap.TargetFrameRate = 0;
            Assert.AreEqual(-1, Application.targetFrameRate, "0 means no cap");
            Assert.AreEqual(60, FrameRateCap.DefaultTargetFrameRate);
        }

        [Test]
        public void TheRenderPipeline_UsesTheCheaperAmbientOcclusion_AndOcclusionCulling()
        {
            var feature = AssetDatabase.LoadAllAssetsAtPath(RendererPath).FirstOrDefault(asset => asset != null && asset.name == "ScreenSpaceAmbientOcclusion");
            Assert.IsNotNull(feature, "the PC renderer has the ambient occlusion feature");
            var occlusion = new SerializedObject(feature);
            Assert.AreEqual(0, occlusion.FindProperty("m_Settings.Source").intValue, "from the depth, not depth and normals: Tools/Cold Shutdown/Apply Performance Settings");

            var pipeline = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(PipelinePath));
            Assert.AreEqual(1, pipeline.FindProperty("m_GPUResidentDrawerMode").intValue, "the GPU resident drawer is on");
            Assert.IsTrue(pipeline.FindProperty("m_GPUResidentDrawerEnableOcclusionCullingInCameras").boolValue, "with GPU occlusion culling");
        }
    }
}
