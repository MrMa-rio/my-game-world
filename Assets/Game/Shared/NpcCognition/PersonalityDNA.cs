using System;

namespace MyGameWorld.Shared.NpcCognition
{
    [Serializable]
    public sealed class PersonalityDNA
    {
        public PersonalityDNA(
            NormalizedTrait curiosity,
            NormalizedTrait aggression,
            NormalizedTrait courage,
            NormalizedTrait empathy,
            NormalizedTrait loyalty,
            NormalizedTrait patience,
            NormalizedTrait greed,
            NormalizedTrait sociability)
        {
            Curiosity = curiosity;
            Aggression = aggression;
            Courage = courage;
            Empathy = empathy;
            Loyalty = loyalty;
            Patience = patience;
            Greed = greed;
            Sociability = sociability;
        }

        public NormalizedTrait Curiosity { get; }

        public NormalizedTrait Aggression { get; }

        public NormalizedTrait Courage { get; }

        public NormalizedTrait Empathy { get; }

        public NormalizedTrait Loyalty { get; }

        public NormalizedTrait Patience { get; }

        public NormalizedTrait Greed { get; }

        public NormalizedTrait Sociability { get; }
    }
}
