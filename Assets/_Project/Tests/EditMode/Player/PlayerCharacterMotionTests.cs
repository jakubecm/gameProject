using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Player
{
    /// <summary>
    /// Runs the real Animator on the real player prefab (EditMode: the animator is stepped by hand) and measures
    /// the character: the foot that is on the floor must not slide over it, and the head must stay under the camera.
    /// Skipped until Tools/Cold Shutdown/Set Up Mixamo Player Body has been run.
    /// </summary>
    public class PlayerCharacterMotionTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const float FrameTime = 1f / 60f;

        private readonly List<GameObject> created = new List<GameObject>();
        private FirstPersonController controller;
        private PlayerCharacterAnimator character;
        private Animator animator;
        private Transform leftFoot;
        private Transform rightFoot;

        [SetUp]
        public void SetUp()
        {
            var floor = new GameObject("Floor") { transform = { position = new Vector3(0f, -0.5f, 0f) } };
            floor.AddComponent<BoxCollider>().size = new Vector3(400f, 1f, 400f);
            created.Add(floor);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            created.Add(player);
            Physics.SyncTransforms();

            controller = player.GetComponent<FirstPersonController>();
            character = player.GetComponentInChildren<PlayerCharacterAnimator>(true);
            Assume.That(character, Is.Not.Null, "The Mixamo body has not been set up.");

            animator = character.GetComponentInChildren<Animator>();
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Assume.That(leftFoot != null && rightFoot != null, "The humanoid feet could not be found in EditMode.");
            animator.Rebind();
            animator.Update(0f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
            {
                Object.DestroyImmediate(go);
            }
            created.Clear();
        }

        [Test]
        public void Walking_DoesNotSlideTheFeet()
        {
            AssertFeetStayPlanted(new PlayerCommand { Move = Vector2.up }, "walking");
        }

        [Test]
        public void Sprinting_DoesNotSlideTheFeet()
        {
            AssertFeetStayPlanted(new PlayerCommand { Move = Vector2.up, Sprint = true }, "sprinting");
        }

        [Test]
        public void TheHead_StaysUnderTheCamera_WhileWalkingAndSprinting()
        {
            foreach (var command in new[] { new PlayerCommand { Move = Vector2.up }, new PlayerCommand { Move = Vector2.up, Sprint = true } })
            {
                Run(command, 90);
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                for (int i = 0; i < 90; i++)
                {
                    Step(command);
                    var seen = controller.transform.InverseTransformPoint(head.position);
                    Assert.That(seen.x, Is.EqualTo(0f).Within(0.01f), "sideways");
                    Assert.That(seen.z, Is.EqualTo(-0.12f).Within(0.01f), "forwards");
                }
            }
        }

        private void AssertFeetStayPlanted(PlayerCommand command, string what)
        {
            Run(command, 120); // reaches full speed and settles into the walking or running cycle

            var feet = new[] { leftFoot, rightFoot };
            var heights = new List<float>[] { new List<float>(), new List<float>() };
            var distances = new List<float>[] { new List<float>(), new List<float>() };
            for (int i = 0; i < 180; i++)
            {
                Step(command);
                for (int f = 0; f < 2; f++)
                {
                    heights[f].Add(feet[f].position.y);
                    distances[f].Add(feet[f].position.z);
                }
            }

            float bodyPerFrame = controller.HorizontalSpeed * FrameTime;
            var slides = new List<float>();
            for (int f = 0; f < 2; f++)
            {
                float lowest = heights[f].Min();
                for (int i = 1; i < heights[f].Count; i++)
                {
                    // A foot counts as down while it is within 1.5 cm of its lowest point.
                    if (heights[f][i] < lowest + 0.015f && heights[f][i - 1] < lowest + 0.015f)
                    {
                        slides.Add(Mathf.Abs(distances[f][i] - distances[f][i - 1]));
                    }
                }
            }

            Assert.IsNotEmpty(slides, "no foot was ever planted while " + what);
            slides.Sort();
            float median = slides[slides.Count / 2];
            float worst = slides[(int)(slides.Count * 0.95f)];
            TestContext.WriteLine($"{what}: body moves {bodyPerFrame * 100f:0.0} cm per frame; the planted foot slides {median * 100f:0.0} cm (median), {worst * 100f:0.0} cm (95th percentile) per frame.");

            // The planted foot may move a little (the cycles are blended), but nowhere near the body's own speed.
            Assert.Less(median, bodyPerFrame * 0.25f, $"the planted foot slides while {what}");
        }

        private void Run(PlayerCommand command, int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                Step(command);
            }
        }

        // One frame: the controller moves the player, the parameters are set as the character script does in Update,
        // the animator is stepped by hand, then the script's LateUpdate runs (head, floor).
        private void Step(PlayerCommand command)
        {
            controller.Simulate(command, FrameTime);

            float speed = controller.HorizontalSpeed;
            animator.SetFloat("Speed", speed);
            animator.SetFloat("MotionSpeed", character.MotionSpeed(speed, false));
            animator.SetFloat("CrouchMotionSpeed", character.CrouchMotionSpeed(speed, false));
            animator.SetBool("Grounded", controller.IsGrounded);
            animator.SetFloat("Crouch", controller.CrouchAmount);
            animator.Update(FrameTime);

            typeof(PlayerCharacterAnimator).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(character, null);
        }
    }
}
