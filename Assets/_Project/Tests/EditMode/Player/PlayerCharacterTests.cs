using System.Linq;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ColdShutdown.Tests.EditMode.Player
{
    /// <summary>
    /// The downloaded character on the player (Tools/Cold Shutdown/Set Up Mixamo Player Body). The prefab
    /// checks are skipped until that tool has been run, and the playback rate rule is always checked.
    /// </summary>
    public class PlayerCharacterTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

        private GameObject instance;

        [TearDown]
        public void TearDown()
        {
            if (instance != null) Object.DestroyImmediate(instance);
        }

        [Test]
        public void MotionSpeed_KeepsTheFeetFromSliding()
        {
            var host = new GameObject("Character", typeof(PlayerCharacterAnimator));
            try
            {
                var animator = host.GetComponent<PlayerCharacterAnimator>();

                Assert.That(animator.MotionSpeed(0f, false), Is.EqualTo(1f).Within(1e-4f), "standing still");
                Assert.That(animator.MotionSpeed(1.45f, false), Is.EqualTo(1f).Within(1e-3f), "exactly the walking animation");
                Assert.That(animator.MotionSpeed(2.6f, false), Is.EqualTo(1f).Within(1e-3f), "between walking and running: the blend moves at that speed");
                Assert.That(animator.MotionSpeed(0.725f, false), Is.EqualTo(0.5f).Within(1e-3f), "half the walking speed: half the playback rate");
                Assert.That(animator.MotionSpeed(7.2f, false), Is.EqualTo(2f).Within(1e-3f), "twice the running speed: twice the rate");
                Assert.That(animator.MotionSpeed(1.45f, true), Is.EqualTo(-1f).Within(1e-3f), "backwards plays the animation in reverse");

                Assert.That(animator.CrouchMotionSpeed(0f, false), Is.EqualTo(1f).Within(1e-4f));
                Assert.That(animator.CrouchMotionSpeed(1.1f, false), Is.EqualTo(1f).Within(1e-3f), "the crouch clip moves at 1.1 m/s");
                Assert.That(animator.CrouchMotionSpeed(0.55f, false), Is.EqualTo(0.5f).Within(1e-3f));
                Assert.That(animator.CrouchMotionSpeed(1.1f, true), Is.EqualTo(-1f).Within(1e-3f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void TheHead_StaysBehindTheCamera_WhateverTheAnimationDoes()
        {
            // A player with a character whose head the "animation" keeps throwing forward, sideways and up.
            var player = new GameObject("Player", typeof(CharacterController), typeof(FirstPersonController));
            var holder = new GameObject("Character", typeof(PlayerCharacterAnimator)).transform;
            holder.SetParent(player.transform, false);
            var head = new GameObject("Head").transform;
            head.SetParent(holder, false);
            try
            {
                var serialized = new SerializedObject(holder.GetComponent<PlayerCharacterAnimator>());
                serialized.FindProperty("controller").objectReferenceValue = player.GetComponent<FirstPersonController>();
                serialized.FindProperty("headBone").objectReferenceValue = head;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var lateUpdate = typeof(PlayerCharacterAnimator).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                foreach (var animated in new[] { new Vector3(0.4f, 1.6f, 0.9f), new Vector3(-0.2f, 1.5f, -0.3f), new Vector3(0f, 1.66f, 0f) })
                {
                    head.position = player.transform.TransformPoint(animated);
                    lateUpdate.Invoke(holder.GetComponent<PlayerCharacterAnimator>(), null);

                    var seen = player.transform.InverseTransformPoint(head.position);
                    Assert.That(seen.x, Is.EqualTo(0f).Within(1e-3f), $"sideways, animated head {animated}");
                    Assert.That(seen.z, Is.EqualTo(-0.12f).Within(1e-3f), $"forwards, animated head {animated}");
                }
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void TheCharacter_IsWiredUp_WhenItHasBeenSetUp()
        {
            var character = FindCharacter();
            Assume.That(character, Is.Not.Null, "The Mixamo body has not been set up (Tools/Cold Shutdown/Set Up Mixamo Player Body).");

            var animator = character.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "no Animator on the character");
            Assert.IsNotNull(animator.avatar);
            Assert.IsTrue(animator.avatar.isHuman, "the character is not a Humanoid");
            Assert.IsNotNull(animator.runtimeAnimatorController, "no Animator Controller");
            Assert.IsFalse(animator.applyRootMotion, "root motion would carry the character away from the player");
            Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, animator.cullingMode);

            var serialized = new SerializedObject(character.GetComponent<PlayerCharacterAnimator>());
            Assert.IsNotNull(serialized.FindProperty("controller").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("headBone").objectReferenceValue, "no head bone to hide");
        }

        [Test]
        public void TheCharacter_HasNoColliders_AndItsHairDoesNotHangInFrontOfTheCamera()
        {
            var character = FindCharacter();
            Assume.That(character, Is.Not.Null, "The Mixamo body has not been set up.");

            Assert.IsEmpty(character.GetComponentsInChildren<Collider>(true), "a collider on the body would push the player");
            foreach (var renderer in character.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string lower = renderer.name.ToLowerInvariant();
                if (lower.Contains("hair") || lower.Contains("lash"))
                {
                    Assert.AreEqual(ShadowCastingMode.ShadowsOnly, renderer.shadowCastingMode, renderer.name);
                }
            }
        }

        [Test]
        public void TheController_HasAllTheParametersTheScriptSets()
        {
            var character = FindCharacter();
            Assume.That(character, Is.Not.Null, "The Mixamo body has not been set up.");

            var controller = character.GetComponentInChildren<Animator>().runtimeAnimatorController;
            var names = controller.animationClips.Select(clip => clip.name).ToList();
            Assert.Contains("Walking", names);

            var parameters = ((UnityEditor.Animations.AnimatorController)controller).parameters.Select(parameter => parameter.name).ToList();
            foreach (string expected in new[] { "Speed", "MotionSpeed", "CrouchMotionSpeed", "Grounded", "Crouch", "Jump" })
            {
                Assert.Contains(expected, parameters);
            }
        }

        [Test]
        public void TheMannequin_IsSwitchedOff_WhenTheCharacterIsThere()
        {
            var character = FindCharacter();
            Assume.That(character, Is.Not.Null, "The Mixamo body has not been set up.");

            var mannequin = instance.transform.Find("Body");
            Assert.IsNotNull(mannequin, "the mannequin is kept in the prefab as a fallback");
            Assert.IsFalse(mannequin.gameObject.activeSelf, "two bodies would be drawn");
        }

        private Transform FindCharacter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return instance.transform.Find("Character");
        }
    }
}
