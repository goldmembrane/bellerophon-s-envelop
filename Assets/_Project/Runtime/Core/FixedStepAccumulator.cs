using System;

namespace Bellerophon.Core
{
    public sealed class FixedStepAccumulator
    {
        private float accumulatedSeconds;

        public FixedStepAccumulator(float stepSeconds)
        {
            if (stepSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(stepSeconds), "Step seconds must be positive.");
            }

            StepSeconds = stepSeconds;
        }

        public float StepSeconds { get; }

        public int ConsumeSteps(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            accumulatedSeconds += deltaSeconds;
            var steps = 0;

            while (accumulatedSeconds >= StepSeconds)
            {
                accumulatedSeconds -= StepSeconds;
                steps++;
            }

            return steps;
        }
    }
}

