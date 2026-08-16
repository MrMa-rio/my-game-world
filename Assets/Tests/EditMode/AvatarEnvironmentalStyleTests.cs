using MyGameWorld.Client.CharacterRuntime;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class AvatarEnvironmentalStyleTests
    {
        [Test]
        public void Resolve_SameSeedAndEnvironment_ProducesSameRecipe()
        {
            AvatarEnvironmentContext context = new AvatarEnvironmentContext(2, 1, 12f, 0.08f);
            AvatarStyleRecipe first = AvatarEnvironmentalStyleResolver.Resolve(3201, context);
            AvatarStyleRecipe second = AvatarEnvironmentalStyleResolver.Resolve(3201, context);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Resolve_DifferentBiome_ChangesSilhouetteFamily()
        {
            AvatarStyleRecipe forest = AvatarEnvironmentalStyleResolver.Resolve(3201,
                new AvatarEnvironmentContext(2, 1, 12f, 0.08f));
            AvatarStyleRecipe snow = AvatarEnvironmentalStyleResolver.Resolve(3201,
                new AvatarEnvironmentContext(4, 6, 12f, 0.08f));
            Assert.That(forest.Family, Is.EqualTo(AvatarSilhouetteFamily.ForestRanger));
            Assert.That(snow.Family, Is.EqualTo(AvatarSilhouetteFamily.SnowHighlander));
            Assert.That(snow.VisualScale, Is.Not.EqualTo(forest.VisualScale));
        }

        [Test]
        public void Resolve_RockyTemperateSpawn_SelectsSturdySilhouette()
        {
            AvatarStyleRecipe recipe = AvatarEnvironmentalStyleResolver.Resolve(77,
                new AvatarEnvironmentContext(1, 3, 70f, 0.4f));
            Assert.That(recipe.Family, Is.EqualTo(AvatarSilhouetteFamily.RockyHighlander));
            Assert.That(recipe.VisualScale.x, Is.GreaterThan(1.1f));
            Assert.That(recipe.HeadScale, Is.GreaterThan(1.1f));
            Assert.That(recipe.TorsoWidth, Is.GreaterThan(1.1f));
        }
    }
}
