using System;
using MyGameWorld.Shared.EntityModel;

namespace MyGameWorld.Shared.NpcCognition
{
    [Serializable]
    public sealed class NpcDNA
    {
        public NpcDNA(EntityDNA entity, IntelligenceDNA intelligence, PersonalityDNA personality)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Intelligence = intelligence ?? throw new ArgumentNullException(nameof(intelligence));
            Personality = personality ?? throw new ArgumentNullException(nameof(personality));
        }

        public EntityDNA Entity { get; }

        public IntelligenceDNA Intelligence { get; }

        public PersonalityDNA Personality { get; }
    }
}
