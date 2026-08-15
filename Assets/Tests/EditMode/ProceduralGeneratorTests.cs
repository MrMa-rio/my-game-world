using System;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ProceduralGeneratorTests
    {
        [Test]
        public void Generate_SameContext_ProducesSameResult()
        {
            SampleGenerator generator = new SampleGenerator();
            GenerationContext context = CreateContext(1);

            Assert.That(generator.Generate("dna", context), Is.EqualTo(generator.Generate("dna", context)));
        }

        [Test]
        public void Generate_MismatchedVersion_Throws()
        {
            SampleGenerator generator = new SampleGenerator();

            Assert.Throws<InvalidOperationException>(() => generator.Generate("dna", CreateContext(2)));
        }

        private static GenerationContext CreateContext(ushort version)
        {
            return new GenerationContext(99, new GeneratorVersion(version), new AssetCatalogVersion(1));
        }

        private sealed class SampleGenerator : ProceduralGenerator<string, int>
        {
            public SampleGenerator()
                : base(new GeneratorVersion(1))
            {
            }

            protected override int GenerateCore(string dna, GenerationContext context)
            {
                return context.CreateRandom().NextInt(1000);
            }
        }
    }
}
