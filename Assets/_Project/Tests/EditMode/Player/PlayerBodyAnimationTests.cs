using System.Collections.Generic;
using System.Reflection;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Player
{
    /// <summary>
    /// Walks the real player prefab over a floor and checks the body: the feet stay on the floor, the foot
    /// that is down does not slide, and the legs stay inside the body. Time.deltaTime is not running in
    /// EditMode, so the animation blends are set directly; the poses themselves come from the stride phase.
    /// </summary>
    public class PlayerBodyAnimationTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
        private const float FrameTime = 1f / 60f;
        private const float AnkleHeight = PlayerBodyDimensions.AnkleHeight;

        private readonly List<GameObject> created = new List<GameObject>();
        private FirstPersonController controller;
        private PlayerBodyAnimator animator;
        private Transform footLeft;
        private Transform footRight;

        [SetUp]
        public void SetUp()
        {
            var floor = new GameObject("Floor") { transform = { position = new Vector3(0f, -0.5f, 0f) } };
            floor.AddComponent<BoxCollider>().size = new Vector3(200f, 1f, 200f);
            created.Add(floor);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            created.Add(player);
            Physics.SyncTransforms();

            controller = player.GetComponent<FirstPersonController>();
            animator = player.GetComponentInChildren<PlayerBodyAnimator>(true);
            footLeft = player.transform.Find("Body/Pelvis/Thigh_L/Shin/Foot");
            footRight = player.transform.Find("Body/Pelvis/Thigh_R/Shin/Foot");
            Assert.IsNotNull(footLeft, "Thigh_L/Shin/Foot is missing");
            Assert.IsNotNull(footRight, "Thigh_R/Shin/Foot is missing");
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
        public void Walking_KeepsTheFeetOnTheFloor_AndThePlantedFootDoesNotSlide()
        {
            Walk(new PlayerCommand { Move = Vector2.up }, 1.5f);

            float previousLeft = footLeft.position.z;
            float previousRight = footRight.position.z;
            bool leftWasPlanted = false;
            bool rightWasPlanted = false;
            float worstSlide = 0f;
            for (int frame = 0; frame < 150; frame++)
            {
                Step(new PlayerCommand { Move = Vector2.up });
                float leftHeight = footLeft.position.y;
                float rightHeight = footRight.position.y;

                Assert.GreaterOrEqual(leftHeight, AnkleHeight - 0.02f, $"left foot under the floor at frame {frame}");
                Assert.GreaterOrEqual(rightHeight, AnkleHeight - 0.02f, $"right foot under the floor at frame {frame}");
                Assert.LessOrEqual(Mathf.Min(leftHeight, rightHeight), AnkleHeight + 0.02f, $"both feet are in the air at frame {frame}");

                bool leftPlanted = leftHeight < AnkleHeight + 0.0005f;
                bool rightPlanted = rightHeight < AnkleHeight + 0.0005f;
                if (leftPlanted && leftWasPlanted) worstSlide = Mathf.Max(worstSlide, Mathf.Abs(footLeft.position.z - previousLeft));
                if (rightPlanted && rightWasPlanted) worstSlide = Mathf.Max(worstSlide, Mathf.Abs(footRight.position.z - previousRight));

                previousLeft = footLeft.position.z;
                previousRight = footRight.position.z;
                leftWasPlanted = leftPlanted;
                rightWasPlanted = rightPlanted;
            }

            // The body moves 4 cm per frame; a foot that is down must stay put in the world (within 5 mm per frame).
            Assert.Less(worstSlide, 0.005f, "the planted foot slides over the floor");
        }

        [Test]
        public void Walking_LiftsEachFoot_InTurn()
        {
            Walk(new PlayerCommand { Move = Vector2.up }, 1.5f);

            float highestLeft = 0f;
            float highestRight = 0f;
            for (int frame = 0; frame < 120; frame++)
            {
                Step(new PlayerCommand { Move = Vector2.up });
                highestLeft = Mathf.Max(highestLeft, footLeft.position.y);
                highestRight = Mathf.Max(highestRight, footRight.position.y);
            }

            Assert.Greater(highestLeft, AnkleHeight + 0.05f, "the left foot steps");
            Assert.Greater(highestRight, AnkleHeight + 0.05f, "the right foot steps");
        }

        [Test]
        public void Standing_PutsBothFeetFlatOnTheFloor()
        {
            for (int frame = 0; frame < 30; frame++)
            {
                Step(default);
            }

            Assert.That(footLeft.position.y, Is.EqualTo(AnkleHeight).Within(0.01f));
            Assert.That(footRight.position.y, Is.EqualTo(AnkleHeight).Within(0.01f));
            Assert.That(Mathf.Abs(footLeft.position.x - footRight.position.x), Is.GreaterThan(0.1f), "the feet are apart");
        }

        [Test]
        public void Crouching_KeepsTheFeetOnTheFloor_AndLowersTheHips()
        {
            Step(default);
            float standingHips = animator.transform.Find("Pelvis").position.y;

            for (int frame = 0; frame < 40; frame++)
            {
                Step(new PlayerCommand { Crouch = true });
            }

            Assert.That(footLeft.position.y, Is.EqualTo(AnkleHeight).Within(0.02f));
            Assert.That(footRight.position.y, Is.EqualTo(AnkleHeight).Within(0.02f));
            Assert.Less(animator.transform.Find("Pelvis").position.y, standingHips - 0.35f, "the hips drop with the eyes");
        }

        [Test]
        public void TheLegsNeverBreakAndTheBodyNeverLeavesThePlayer_WhileSprintingAndCrouchWalking()
        {
            foreach (var command in new[]
            {
                new PlayerCommand { Move = Vector2.up, Sprint = true },
                new PlayerCommand { Move = Vector2.up, Crouch = true },
                new PlayerCommand { Move = Vector2.down },
                new PlayerCommand { Move = Vector2.right },
            })
            {
                Walk(command, 0.7f);
                for (int frame = 0; frame < 60; frame++)
                {
                    Step(command);
                    foreach (var foot in new[] { footLeft, footRight })
                    {
                        Assert.IsFalse(float.IsNaN(foot.position.x) || float.IsNaN(foot.position.y) || float.IsNaN(foot.position.z), "NaN in a foot position");
                        Assert.GreaterOrEqual(foot.position.y, AnkleHeight - 0.03f);
                        Assert.LessOrEqual(Vector3.Distance(foot.position, controller.transform.position), 1.2f, "a foot far from the body");
                    }
                }
            }
        }

        // Runs the controller for some seconds without looking at the body.
        private void Walk(PlayerCommand command, float seconds)
        {
            int frames = Mathf.RoundToInt(seconds / FrameTime);
            for (int i = 0; i < frames; i++)
            {
                controller.Simulate(command, FrameTime);
            }
        }

        // One frame: the controller moves, then the body animates (as LateUpdate does).
        private void Step(PlayerCommand command)
        {
            controller.Simulate(command, FrameTime);
            SetBlends(moveBlend: controller.HorizontalSpeed > 0.1f ? 1f : 0f, airBlend: controller.IsGrounded ? 0f : 1f);
            typeof(PlayerBodyAnimator).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animator, null);
        }

        // Time.deltaTime is zero here, so the blends that normally follow it are put where they would end up.
        private void SetBlends(float moveBlend, float airBlend)
        {
            var type = typeof(PlayerBodyAnimator);
            type.GetField("moveBlend", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(animator, moveBlend);
            type.GetField("airBlend", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(animator, airBlend);
        }
    }
}
