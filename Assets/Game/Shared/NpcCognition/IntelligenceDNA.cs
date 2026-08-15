using System;

namespace MyGameWorld.Shared.NpcCognition
{
    [Serializable]
    public sealed class IntelligenceDNA
    {
        public IntelligenceDNA(
            byte overallLevel,
            NormalizedTrait perception,
            NormalizedTrait memory,
            NormalizedTrait reasoning,
            NormalizedTrait language,
            NormalizedTrait social,
            NormalizedTrait planning)
        {
            if (overallLevel > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(overallLevel), "Overall intelligence must be between 0 and 10.");
            }

            OverallLevel = overallLevel;
            Perception = perception;
            Memory = memory;
            Reasoning = reasoning;
            Language = language;
            Social = social;
            Planning = planning;
        }

        public byte OverallLevel { get; }

        public NormalizedTrait Perception { get; }

        public NormalizedTrait Memory { get; }

        public NormalizedTrait Reasoning { get; }

        public NormalizedTrait Language { get; }

        public NormalizedTrait Social { get; }

        public NormalizedTrait Planning { get; }
    }
}
