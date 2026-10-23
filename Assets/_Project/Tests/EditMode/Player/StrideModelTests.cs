using ColdShutdown.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace ColdShutdown.Tests.EditMode.Player
{
    public class StrideModelTests
    {
        private const float Epsilon = 0.0001f;

        [Test]
        public void Advance_CountsOneFootstepPerStepLength()
        {
            var stride = new StrideModel();
            int steps = 0;
            // 0.125 m per frame over 3 m: exact in floating point, so the count is not off by rounding.
            for (int i = 0; i < 24; i++)
            {
                steps += stride.Advance(0.125f, 0.5f);
            }
            Assert.AreEqual(6, steps);
            Assert.That(stride.Phase, Is.EqualTo(6f).Within(Epsilon));
        }

        [Test]
        public void Advance_WithoutMovement_TakesNoStep()
        {
            var stride = new StrideModel();
            Assert.AreEqual(0, stride.Advance(0f, 0.75f));
            Assert.AreEqual(0, stride.Advance(-1f, 0.75f));
            Assert.AreEqual(0f, stride.Phase);
        }

        [Test]
        public void PlantFoot_MovesToTheNearestFootContact()
        {
            var stride = new StrideModel();
            stride.Advance(1.3f, 1f);
            stride.PlantFoot();
            Assert.AreEqual(1f, stride.Phase);
        }

        [Test]
        public void BobOffset_IsLowestOnAFootstepAndHighestMidStep()
        {
            const float vertical = 0.04f;
            const float lateral = 0.02f;

            var onStep = StrideModel.BobOffset(3f, vertical, lateral);
            var midStep = StrideModel.BobOffset(3.5f, vertical, lateral);
            Assert.That(onStep.y, Is.EqualTo(-vertical).Within(Epsilon));
            Assert.That(midStep.y, Is.EqualTo(vertical).Within(Epsilon));
            Assert.That(onStep.x, Is.EqualTo(0f).Within(Epsilon), "centered when the feet change");
            Assert.That(Mathf.Abs(midStep.x), Is.EqualTo(lateral).Within(Epsilon), "over the standing foot mid-step");
        }

        [Test]
        public void BobOffset_SwaysToAlternateSides()
        {
            var left = StrideModel.BobOffset(0.5f, 0.04f, 0.02f);
            var right = StrideModel.BobOffset(1.5f, 0.04f, 0.02f);
            Assert.That(left.x, Is.EqualTo(-right.x).Within(Epsilon));
        }
    }
}
