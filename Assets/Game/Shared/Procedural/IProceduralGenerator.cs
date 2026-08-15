using MyGameWorld.Shared.Core;

namespace MyGameWorld.Shared.Procedural
{
    public interface IProceduralGenerator<TDNA, TResult>
    {
        GeneratorVersion Version { get; }

        GenerationValidation Validate(TDNA dna, GenerationContext context);

        TResult Generate(TDNA dna, GenerationContext context);
    }
}
