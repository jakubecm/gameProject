using System.Collections.Generic;
using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Player
{
    /// <summary>
    /// Drives the <see cref="FirstPersonController"/> frame by frame through
    /// <see cref="FirstPersonController.Simulate"/> on a flat test floor, without input devices.
    /// </summary>
    public class FirstPersonControllerTests
    {
        private const float FrameTime = 1f / 60f;

        private readonly List<GameObject> created = new List<GameObject>();
        private FirstPersonController controller;

        [SetUp]
        public void SetUp()
        {
            CreateBox("Floor", new Vector3(0f, -0.5f, 0f), new Vector3(200f, 1f, 200f));

            var player = Track(new GameObject("Player"));
            var body = player.AddComponent<CharacterController>();
            body.radius = 0.3f;
            body.stepOffset = 0.35f;
            body.skinWidth = 0.03f;
            body.minMoveDistance = 0f;
            new GameObject("CameraPivot").transform.SetParent(player.transform, false);
            controller = player.AddComponent<FirstPersonController>();
            Physics.SyncTransforms();
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
        public void Walking_ReachesWalkingSpeedAndStaysOnTheFloor()
        {
            Run(new PlayerCommand { Move = Vector2.up }, 1f);

            var position = controller.transform.position;
            Assert.That(position.z, Is.InRange(2.0f, controller.WalkSpeed));
            Assert.That(position.y, Is.EqualTo(0f).Within(0.05f));
            Assert.That(controller.HorizontalSpeed, Is.EqualTo(controller.WalkSpeed).Within(0.05f));
            Assert.IsTrue(controller.IsGrounded);
        }

        [Test]
        public void Releasing_TheInput_StopsThePlayer()
        {
            Run(new PlayerCommand { Move = Vector2.up }, 1f);
            Run(default, 0.5f);

            Assert.That(controller.HorizontalSpeed, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Walls_StopThePlayer()
        {
            CreateBox("Wall", new Vector3(0f, 1.5f, 1.5f), new Vector3(4f, 3f, 0.2f));

            Run(new PlayerCommand { Move = Vector2.up }, 2f);

            float wallFace = 1.4f;
            Assert.That(controller.transform.position.z, Is.LessThanOrEqualTo(wallFace - 0.3f + 0.01f));
            Assert.That(controller.HorizontalSpeed, Is.LessThan(0.05f));
        }

        [Test]
        public void Sprinting_IsFasterThanWalking()
        {
            var changes = new List<bool>();
            controller.SprintChanged += changes.Add;

            Run(new PlayerCommand { Move = Vector2.up, Sprint = true }, 1f);

            Assert.IsTrue(controller.IsSprinting);
            Assert.That(controller.HorizontalSpeed, Is.EqualTo(controller.SprintSpeed).Within(0.05f));
            CollectionAssert.AreEqual(new[] { true }, changes);
        }

        [Test]
        public void Sprinting_WorksOnlyWhenWalkingForward()
        {
            Run(new PlayerCommand { Move = Vector2.down, Sprint = true }, 1f);
            Assert.IsFalse(controller.IsSprinting, "backwards");
            Assert.That(controller.HorizontalSpeed, Is.EqualTo(controller.WalkSpeed).Within(0.05f));

            Run(new PlayerCommand { Move = Vector2.right, Sprint = true }, 1f);
            Assert.IsFalse(controller.IsSprinting, "sideways");
        }

        [Test]
        public void Crouching_WinsOverSprinting()
        {
            Run(new PlayerCommand { Move = Vector2.up, Sprint = true, Crouch = true }, 1f);

            Assert.IsTrue(controller.IsCrouching);
            Assert.IsFalse(controller.IsSprinting);
            Assert.That(controller.HorizontalSpeed, Is.LessThan(controller.WalkSpeed));
        }

        [Test]
        public void Stamina_RunsOutAndRecovers()
        {
            Run(new PlayerCommand { Move = Vector2.up, Sprint = true }, 7f);
            Assert.IsTrue(controller.IsExhausted);
            Assert.IsFalse(controller.IsSprinting, "out of stamina");
            Assert.That(controller.Stamina, Is.EqualTo(0f).Within(0.001f));
            Assert.That(controller.HorizontalSpeed, Is.EqualTo(controller.WalkSpeed).Within(0.05f), "back to walking");

            // Holding sprint while exhausted does not sprint, even after a short rest.
            Run(new PlayerCommand { Move = Vector2.up, Sprint = true }, 1.5f);
            Assert.IsFalse(controller.IsSprinting);

            Run(default, 2.5f);
            Assert.IsFalse(controller.IsExhausted, "rested enough to sprint again");
            Run(new PlayerCommand { Move = Vector2.up, Sprint = true }, 0.5f);
            Assert.IsTrue(controller.IsSprinting);

            Run(default, 7f);
            Assert.That(controller.Stamina, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Sprinting_TakesLongerAndMarkedFootsteps()
        {
            var steps = new List<FootstepEvent>();
            Run(new PlayerCommand { Move = Vector2.up, Sprint = true }, 0.5f);
            controller.Footstep += steps.Add;
            float start = controller.transform.position.z;

            Run(new PlayerCommand { Move = Vector2.up, Sprint = true }, 2f);
            float run = controller.transform.position.z - start;

            Assert.That(steps.Count, Is.EqualTo(Mathf.FloorToInt(run / 1.15f)).Within(1));
            Assert.IsTrue(steps.TrueForAll(step => step.Sprinting));
        }

        [Test]
        public void Crouching_LowersTheBodyAndTheEyes()
        {
            float standingEyes = EyesAfter(default);
            Run(new PlayerCommand { Crouch = true }, 0.5f);

            Assert.IsTrue(controller.IsCrouching);
            Assert.That(controller.Height, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(controller.GetComponent<CharacterController>().height, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(controller.CameraPivot.localPosition.y, Is.LessThan(standingEyes - 0.6f));
            Assert.That(controller.CrouchAmount, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Crouching_IsSlowerThanWalking()
        {
            Run(new PlayerCommand { Crouch = true }, 0.5f);
            Run(new PlayerCommand { Move = Vector2.up, Crouch = true }, 1f);

            Assert.That(controller.HorizontalSpeed, Is.LessThan(controller.WalkSpeed * 0.6f));
            Assert.That(controller.HorizontalSpeed, Is.GreaterThan(0.5f));
        }

        [Test]
        public void StandingUp_WaitsUntilThereIsHeadroom()
        {
            Run(new PlayerCommand { Crouch = true }, 0.5f);
            CreateBox("LowCeiling", new Vector3(0f, 1.5f, 0f), new Vector3(4f, 0.2f, 4f));

            Run(default, 0.5f);
            Assert.IsTrue(controller.IsCrouching, "stood up into a 1.4 m ceiling");
            Assert.That(controller.Height, Is.LessThan(1.4f));

            // Walk out from under it without holding crouch: the player stands up on the way out.
            Run(new PlayerCommand { Move = Vector2.up }, 3f);
            Assert.IsFalse(controller.IsCrouching);
            Assert.That(controller.Height, Is.EqualTo(1.8f).Within(0.001f));
        }

        [Test]
        public void Looking_ClampsThePitchAndTurnsTheBody()
        {
            Run(new PlayerCommand { Look = new Vector2(0f, 500f) }, FrameTime);
            Assert.That(controller.Pitch, Is.EqualTo(-85f).Within(0.001f));

            Run(new PlayerCommand { Look = new Vector2(0f, -1000f) }, FrameTime);
            Assert.That(controller.Pitch, Is.EqualTo(85f).Within(0.001f));

            Run(new PlayerCommand { Look = new Vector2(90f, 0f) }, FrameTime);
            Run(new PlayerCommand { Move = Vector2.up }, 1f);
            Assert.That(controller.transform.position.x, Is.GreaterThan(2f), "forward after turning right should be +X");
            Assert.That(Mathf.Abs(controller.transform.position.z), Is.LessThan(0.01f));
        }

        [Test]
        public void Footsteps_FollowTheDistanceWalked()
        {
            var steps = new List<FootstepEvent>();
            controller.Footstep += steps.Add;

            Run(new PlayerCommand { Move = Vector2.up }, 2f);
            float walked = controller.transform.position.z;
            Assert.That(steps.Count, Is.EqualTo(Mathf.FloorToInt(walked / 0.85f)).Within(1));
            Assert.IsNotNull(steps[0].Ground, "a footstep should know the floor under it");
            Assert.IsFalse(steps[0].Crouching);

            int whileWalking = steps.Count;
            Run(default, 1f);
            Assert.That(steps.Count, Is.LessThanOrEqualTo(whileWalking + 1), "no footsteps while standing still");
        }

        [Test]
        public void Falling_RaisesLandedOnce()
        {
            controller.transform.position = new Vector3(0f, 2f, 0f);
            Physics.SyncTransforms();
            var impacts = new List<float>();
            controller.Landed += impacts.Add;

            Run(default, 1.5f);

            Assert.AreEqual(1, impacts.Count);
            Assert.That(impacts[0], Is.GreaterThan(5f));
            Assert.IsTrue(controller.IsGrounded);
        }

        private float EyesAfter(PlayerCommand command)
        {
            Run(command, FrameTime);
            return controller.CameraPivot.localPosition.y;
        }

        private void Run(PlayerCommand command, float seconds)
        {
            int frames = Mathf.Max(1, Mathf.RoundToInt(seconds / FrameTime));
            for (int i = 0; i < frames; i++)
            {
                // Look is a per-frame delta, so it is applied once, not every frame of the run.
                var frame = command;
                if (i > 0) frame.Look = Vector2.zero;
                controller.Simulate(frame, FrameTime);
            }
        }

        private void CreateBox(string name, Vector3 center, Vector3 size)
        {
            var box = Track(new GameObject(name));
            box.transform.position = center;
            box.AddComponent<BoxCollider>().size = size;
            Physics.SyncTransforms();
        }

        private GameObject Track(GameObject go)
        {
            created.Add(go);
            return go;
        }
    }
}
