using System.Collections.Generic;
using System.Linq;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Tests.EditMode.Player
{
    /// <summary>Checks the player prefab built by Tools/Cold Shutdown/Create Player Prefab and Update Player Prefab.</summary>
    public class PlayerPrefabTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        private readonly List<GameObject> instances = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in instances)
            {
                Object.DestroyImmediate(instance);
            }
            instances.Clear();
        }

        [Test]
        public void Player_HasAVisibleBodyWithAllBonesConnected()
        {
            var player = Instantiate();
            var body = player.transform.Find("Body");
            Assert.IsNotNull(body, "The player has no Body. Run Tools/Cold Shutdown/Update Player Prefab.");

            var animator = body.GetComponent<PlayerBodyAnimator>();
            Assert.IsNotNull(animator);

            var serialized = new SerializedObject(animator);
            foreach (string bone in new[]
            {
                "controller", "pelvis", "chest", "thighLeft", "thighRight", "shinLeft", "shinRight",
                "footLeft", "footRight", "upperArmLeft", "upperArmRight", "forearmLeft", "forearmRight",
            })
            {
                Assert.IsNotNull(serialized.FindProperty(bone).objectReferenceValue, $"{bone} is not connected");
            }
            Assert.AreSame(player.GetComponent<FirstPersonController>(), serialized.FindProperty("controller").objectReferenceValue);
        }

        [Test]
        public void TheBody_HasNoColliders_SoItNeverBlocksThePlayerOrTheInteractionRay()
        {
            var body = Instantiate().transform.Find("Body");
            Assert.IsEmpty(body.GetComponentsInChildren<Collider>(true));
        }

        [Test]
        public void TheHeadAndTheChest_OnlyCastShadows_SoTheyNeverFillTheCamera()
        {
            var renderers = Instantiate().transform.Find("Body").GetComponentsInChildren<MeshRenderer>(true);
            foreach (string name in new[] { "Mesh_Head", "Mesh_Torso" })
            {
                var renderer = renderers.Single(candidate => candidate.name == name);
                Assert.AreEqual(ShadowCastingMode.ShadowsOnly, renderer.shadowCastingMode, $"{name} is drawn to the camera. Run Tools/Cold Shutdown/Update Player Prefab.");
            }
        }

        [Test]
        public void TheLegsAndTheArms_AreDrawnToTheCamera()
        {
            var renderers = Instantiate().transform.Find("Body").GetComponentsInChildren<MeshRenderer>(true);
            foreach (string name in new[] { "Mesh_Thigh", "Mesh_Shin", "Mesh_Foot", "Mesh_UpperArm", "Mesh_Forearm" })
            {
                Assert.IsTrue(renderers.Where(candidate => candidate.name == name).All(candidate => candidate.shadowCastingMode == ShadowCastingMode.On), name);
            }
        }

        [Test]
        public void TheBody_FitsThePlayersCapsule()
        {
            var player = Instantiate();
            var bounds = player.transform.Find("Body").GetComponentsInChildren<MeshRenderer>(true)
                .Select(renderer => renderer.bounds)
                .Aggregate((a, b) => { a.Encapsulate(b); return a; });

            var capsule = player.GetComponent<CharacterController>();
            Assert.That(bounds.min.y, Is.EqualTo(0f).Within(0.02f), "the soles are on the floor");
            Assert.That(bounds.max.y, Is.EqualTo(capsule.height).Within(0.08f), "the head reaches the top of the capsule");
            Assert.LessOrEqual(Mathf.Max(bounds.extents.x, bounds.extents.z), capsule.radius + 0.35f, "narrow enough to walk through the doors");
        }

        [Test]
        public void ThePlayer_HasTheCurrentTuning()
        {
            var player = Instantiate();
            var controller = new SerializedObject(player.GetComponent<FirstPersonController>());
            var headBob = new SerializedObject(player.GetComponentInChildren<HeadBob>());

            // 1.3 m by default; the Mixamo setup matches it to the crouch animation.
            Assert.That(controller.FindProperty("crouchingHeight").floatValue, Is.InRange(1.0f, 1.5f), "gentle crouch, not down to the knees");
            Assert.That(controller.FindProperty("jumpHeight").floatValue, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(headBob.FindProperty("verticalAmplitude").floatValue, Is.LessThanOrEqualTo(0.02f), "calm walking bob");
            Assert.That(headBob.FindProperty("sprintFactor").floatValue, Is.LessThanOrEqualTo(1f), "the sprint bob is not bigger than the walking bob");
        }

        [Test]
        public void ThePlayerInput_KnowsTheJumpKeys()
        {
            var reader = new SerializedObject(Instantiate().GetComponent<PlayerInputReader>());
            Assert.IsNotNull(reader.FindProperty("jump"), "no jump action");
        }

        private GameObject Instantiate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"{PrefabPath} is missing");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instances.Add(instance);
            return instance;
        }
    }
}
