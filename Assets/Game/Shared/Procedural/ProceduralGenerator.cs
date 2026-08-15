using System;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public abstract class ProceduralGenerator<TDNA, TResult> : IProceduralGenerator<TDNA, TResult>
    {
        protected ProceduralGenerator(GeneratorVersion version)
        {
            Version = version;
        }

        public GeneratorVersion Version { get; }

        public GenerationValidation Validate(TDNA dna, GenerationContext context)
        {
            if (ReferenceEquals(dna, null))
            {
                return GenerationValidation.Invalid("DNA_NULL", "DNA is required.");
            }

            if (context.GeneratorVersion != Version)
            {
                return GenerationValidation.Invalid(
                    "GENERATOR_VERSION_MISMATCH",
                    $"Generator {Version} cannot process context version {context.GeneratorVersion}.");
            }

            return ValidateCore(dna, context);
        }

        public TResult Generate(TDNA dna, GenerationContext context)
        {
            GenerationValidation validation = Validate(dna, context);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"{validation.ErrorCode}: {validation.Message}");
            }

            return GenerateCore(dna, context);
        }

        protected virtual GenerationValidation ValidateCore(TDNA dna, GenerationContext context)
        {
            return GenerationValidation.Valid();
        }

        protected abstract TResult GenerateCore(TDNA dna, GenerationContext context);
    }
}
