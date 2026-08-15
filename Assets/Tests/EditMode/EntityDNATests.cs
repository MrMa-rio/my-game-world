using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.EntityModel;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class EntityDNATests
    {
        [Test]
        public void Equals_SameDeterministicInputs_ReturnsTrue()
        {
            EntityDNA first = CreateDNA();
            EntityDNA second = CreateDNA();

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void EntityId_NonPositiveValue_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new EntityId(0));
        }

        private static EntityDNA CreateDNA()
        {
            return new EntityDNA(
                new EntityId(42),
                new ArchetypeId(7),
                123456789L,
                new GeneratorVersion(1),
                new AssetCatalogVersion(1));
        }
    }
}
