using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Player
{
    public class LegSolverTests
    {
        private const float Thigh = PlayerBodyDimensions.ThighLength;
        private const float Shin = PlayerBodyDimensions.ShinLength;

        [Test]
        public void TheSolvedPose_ReachesTheTarget()
        {
            foreach (float forward in new[] { -0.4f, -0.2f, 0f, 0.15f, 0.425f })
            {
                foreach (float down in new[] { 0.35f, 0.5f, 0.7f, 0.87f })
                {
                    var pose = LegSolver.Solve(Thigh, Shin, forward, down);
                    var ankle = LegSolver.AnklePosition(Thigh, Shin, pose);

                    Assert.That(ankle.x, Is.EqualTo(forward).Within(0.002f), $"forward for ({forward}, {down})");
                    Assert.That(ankle.y, Is.EqualTo(down).Within(0.002f), $"down for ({forward}, {down})");
                }
            }
        }

        [Test]
        public void TheKneeAlwaysBendsForward_AndAStraightLegHasNoBend()
        {
            foreach (float down in new[] { 0.4f, 0.6f, 0.8f, 0.95f })
            {
                var pose = LegSolver.Solve(Thigh, Shin, 0f, down);
                Assert.GreaterOrEqual(pose.KneeFlexion, 0f);
                Assert.Greater(pose.ThighAngle, 0f, $"the thigh points forward for a foot straight below the hip at {down}");
                Assert.Less(pose.ShinAngle, pose.ThighAngle, "the shin is bent back relative to the thigh");
            }

            Assert.That(LegSolver.Solve(Thigh, Shin, 0f, Thigh + Shin).KneeFlexion, Is.LessThan(8f));
        }

        [Test]
        public void ALowerHip_BendsTheKneeMore()
        {
            float standing = LegSolver.Solve(Thigh, Shin, 0f, 0.87f).KneeFlexion;
            float crouched = LegSolver.Solve(Thigh, Shin, 0f, 0.37f).KneeFlexion;
            Assert.Greater(crouched, standing + 60f);
        }

        [Test]
        public void ATargetOutOfReach_IsReachedAsFarAsPossibleAlongItsDirection()
        {
            var pose = LegSolver.Solve(Thigh, Shin, 0.5f, 1.5f);
            var ankle = LegSolver.AnklePosition(Thigh, Shin, pose);

            Assert.That(ankle.magnitude, Is.EqualTo(Thigh + Shin).Within(0.01f));
            Assert.That(Mathf.Atan2(ankle.x, ankle.y), Is.EqualTo(Mathf.Atan2(0.5f, 1.5f)).Within(0.01f), "same direction");
            Assert.IsFalse(float.IsNaN(pose.ThighAngle));
        }

        [Test]
        public void ATargetAtTheHip_DoesNotBreakTheSolver()
        {
            var pose = LegSolver.Solve(Thigh, Shin, 0f, 0f);
            Assert.IsFalse(float.IsNaN(pose.ThighAngle) || float.IsNaN(pose.KneeFlexion));
        }

        [Test]
        public void TheWalkingFoot_StaysPlantedOnTheFloor_WhileTheBodyMoves()
        {
            // Over one stance half the foot slides back by exactly one step length, the distance the body advanced.
            const float step = 0.85f;
            var start = WalkCycle.Foot(0f, step / 2f, 0.1f);
            var end = WalkCycle.Foot(0.4999f, step / 2f, 0.1f);

            Assert.That(start.x - end.x, Is.EqualTo(step).Within(0.01f));
            Assert.That(start.y, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(end.y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void TheWalkingFoot_LiftsInTheMiddleOfTheSwing_AndLandsInFront()
        {
            var middle = WalkCycle.Foot(0.75f, 0.425f, 0.1f);
            var land = WalkCycle.Foot(0.9999f, 0.425f, 0.1f);

            Assert.That(middle.y, Is.EqualTo(0.1f).Within(1e-4f));
            Assert.That(middle.x, Is.EqualTo(0f).Within(0.01f), "halfway through the swing the foot is under the hip");
            Assert.That(land.x, Is.EqualTo(0.425f).Within(0.01f), "heel strike in front of the body");
            Assert.That(land.y, Is.EqualTo(0f).Within(0.002f));
        }

        [Test]
        public void TheFeet_LandOnWholePhases_LeftOnEvenAndRightOnOdd()
        {
            // At a whole phase one foot has just landed (start of its stance, at the front) and the other has just left.
            for (int phase = 0; phase < 6; phase++)
            {
                float left = WalkCycle.CycleOf(phase, false);
                float right = WalkCycle.CycleOf(phase, true);
                float landing = phase % 2 == 0 ? left : right;
                float leaving = phase % 2 == 0 ? right : left;

                Assert.That(landing, Is.EqualTo(0f).Within(1e-4f), $"phase {phase}");
                Assert.That(leaving, Is.EqualTo(0.5f).Within(1e-4f), $"phase {phase}");
            }
        }

        [Test]
        public void TheTwoFeet_AreAlwaysHalfACycleApart()
        {
            for (float phase = 0f; phase < 4f; phase += 0.13f)
            {
                float difference = Mathf.Abs(WalkCycle.CycleOf(phase, true) - WalkCycle.CycleOf(phase, false));
                Assert.That(difference, Is.EqualTo(0.5f).Within(1e-3f), $"phase {phase}");
            }
        }

        [Test]
        public void AWalkingLegAtFullStride_CanReachItsPositionStanding()
        {
            // The longest reach in the walking cycle must fit the leg, or the foot would not be planted.
            float down = PlayerBodyDimensions.StandingHipHeight - PlayerBodyDimensions.AnkleHeight;
            var pose = LegSolver.Solve(Thigh, Shin, 0.425f, down);
            var ankle = LegSolver.AnklePosition(Thigh, Shin, pose);

            Assert.That(ankle.x, Is.EqualTo(0.425f).Within(0.002f));
            Assert.That(ankle.y, Is.EqualTo(down).Within(0.002f));
        }
    }
}
