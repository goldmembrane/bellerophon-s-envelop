using Bellerophon.Core;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class FixedStepAccumulatorTests
    {
        [Test]
        public void ConsumeSteps_ReturnsOnlyCompletedSteps()
        {
            var accumulator = new FixedStepAccumulator(0.25f);

            Assert.That(accumulator.ConsumeSteps(0.10f), Is.EqualTo(0));
            Assert.That(accumulator.ConsumeSteps(0.15f), Is.EqualTo(1));
            Assert.That(accumulator.ConsumeSteps(0.75f), Is.EqualTo(3));
        }
    }
}

