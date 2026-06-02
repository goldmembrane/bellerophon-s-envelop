using System;

namespace Bellerophon.Core.Session
{
    public readonly struct CrewState
    {
        public CrewState(int survivorCount, int deadCount)
        {
            if (survivorCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(survivorCount), "Survivor count cannot be negative.");
            }

            if (deadCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(deadCount), "Dead count cannot be negative.");
            }

            SurvivorCount = survivorCount;
            DeadCount = deadCount;
        }

        public int SurvivorCount { get; }

        public int DeadCount { get; }
    }
}
