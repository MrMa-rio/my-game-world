using MyGameWorld.Client.CharacterRuntime;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ProceduralGaitMathTests
    {
        [Test]
        public void Evaluate_OppositeLegs_ProducesAlternatingHipMotion()
        {
            ProceduralGaitPose pose = ProceduralGaitMath.Evaluate(0f, 1f, 0f);
            Assert.That(pose.LeftHip, Is.GreaterThan(0f));
            Assert.That(pose.RightHip, Is.LessThan(0f));
        }

        [Test]
        public void Evaluate_Running_FlexesElbowsMoreThanWalking()
        {
            ProceduralGaitPose walk = ProceduralGaitMath.Evaluate(0.25f, 1f, 0f);
            ProceduralGaitPose run = ProceduralGaitMath.Evaluate(0.25f, 1f, 1f);
            Assert.That(run.LeftElbow, Is.GreaterThan(walk.LeftElbow + 40f));
            Assert.That(run.RightElbow, Is.GreaterThan(walk.RightElbow + 40f));
        }

        [Test]
        public void Evaluate_SwingPhase_FlexesKneeForFootClearance()
        {
            ProceduralGaitPose stance = ProceduralGaitMath.Evaluate(0.3f, 1f, 0f);
            ProceduralGaitPose swing = ProceduralGaitMath.Evaluate(0.74f, 1f, 0f);
            Assert.That(swing.LeftKnee, Is.GreaterThan(stance.LeftKnee + 30f));
        }

        [Test]
        public void ResolveCycleFrequency_Running_IsFasterButBounded()
        {
            float walking = ProceduralGaitMath.ResolveCycleFrequency(1f, 0f);
            float running = ProceduralGaitMath.ResolveCycleFrequency(1f, 1f);
            Assert.That(walking, Is.InRange(1f, 1.3f));
            Assert.That(running, Is.InRange(1.8f, 2.1f));
            Assert.That(running, Is.GreaterThan(walking));
        }
    }
}
