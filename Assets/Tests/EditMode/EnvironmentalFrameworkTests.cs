using MyGameWorld.Client.ProceduralWorld;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class EnvironmentalFrameworkTests
    {
        [Test]
        public void SampleWind_SameSeedTimeAndPosition_IsDeterministic()
        {
            WindProfile firstProfile = new WindProfile(); WindProfile secondProfile = new WindProfile();
            WindSystem first = new WindSystem(firstProfile, 829172); WindSystem second = new WindSystem(secondProfile, 829172);
            first.Tick(1.25f); second.Tick(1.25f);
            WindSample a = first.SampleWind(new Vector3(17f, 0f, -31f));
            WindSample b = second.SampleWind(new Vector3(17f, 0f, -31f));
            Assert.That(a.Direction, Is.EqualTo(b.Direction)); Assert.That(a.Strength, Is.EqualTo(b.Strength));
            Assert.That(a.Gust, Is.EqualTo(b.Gust));
        }

        [Test]
        public void SampleWind_DifferentWorldPositions_ReceiveLocalVariation()
        {
            WindSystem wind = new WindSystem(new WindProfile(), 42); wind.Tick(2f);
            WindSample first = wind.SampleWind(Vector3.zero); WindSample second = wind.SampleWind(new Vector3(91f, 0f, 73f));
            Assert.That(second.Strength, Is.Not.EqualTo(first.Strength)); Assert.That(second.Direction, Is.Not.EqualTo(first.Direction));
        }

        [Test]
        public void ResolveVfx_UsesBiomeAndActualSurface()
        {
            Assert.That(EnvironmentalVfxSystem.TryResolveRule(EnvironmentalBiomeKind.Desert, EnvironmentalSurfaceKind.Sand, out EnvironmentalVfxRule sand), Is.True);
            Assert.That(sand.Kind, Is.EqualTo(EnvironmentalVfxKind.SandDust));
            Assert.That(EnvironmentalVfxSystem.TryResolveRule(EnvironmentalBiomeKind.Desert, EnvironmentalSurfaceKind.Water, out _), Is.False);
            Assert.That(EnvironmentalVfxSystem.TryResolveRule(EnvironmentalBiomeKind.Forest, EnvironmentalSurfaceKind.Grass, out EnvironmentalVfxRule forest), Is.True);
            Assert.That(forest.Kind, Is.EqualTo(EnvironmentalVfxKind.DryLeaves));
            Assert.That(EnvironmentalVfxSystem.TryResolveRule(EnvironmentalBiomeKind.Grassland, EnvironmentalSurfaceKind.Grass, out EnvironmentalVfxRule grass), Is.True);
            Assert.That(grass.Kind, Is.EqualTo(EnvironmentalVfxKind.Pollen));
            Assert.That(EnvironmentalVfxSystem.TryResolveRule(EnvironmentalBiomeKind.Snow, EnvironmentalSurfaceKind.Snow, out EnvironmentalVfxRule snow), Is.True);
            Assert.That(snow.Kind, Is.EqualTo(EnvironmentalVfxKind.LooseSnow));
        }

        [Test]
        public void ResolveZones_TreeExposesRootTrunkBranchesAndLeavesWithoutRigidbodies()
        {
            EnvironmentalPhysicalResponseSystem system = new EnvironmentalPhysicalResponseSystem();
            var zones = system.ResolveZones(DecorationKind.Tree);
            Assert.That(zones, Does.Contain(PhysicalResponseZone.Root)); Assert.That(zones, Does.Contain(PhysicalResponseZone.Trunk));
            Assert.That(zones, Does.Contain(PhysicalResponseZone.LargeBranch)); Assert.That(zones, Does.Contain(PhysicalResponseZone.SmallBranch));
            Assert.That(zones, Does.Contain(PhysicalResponseZone.Leaves));
            Assert.That(PhysicalResponseCatalog.Resolve(PhysicalResponseZone.Leaves).ShaderResponse,
                Is.GreaterThan(PhysicalResponseCatalog.Resolve(PhysicalResponseZone.LargeBranch).ShaderResponse));
            Assert.That(PhysicalResponseCatalog.Resolve(PhysicalResponseZone.LargeBranch).ShaderResponse,
                Is.GreaterThan(PhysicalResponseCatalog.Resolve(PhysicalResponseZone.Trunk).ShaderResponse));
        }

        [Test]
        public void VfxRule_IntensityUsesContinuousCurve()
        {
            EnvironmentalVfxRule rule = new EnvironmentalVfxRule(EnvironmentalVfxKind.Pollen, 0.1f, 1f, 1f, 1f, 1f);
            Assert.That(rule.Evaluate(0.1f), Is.EqualTo(0f));
            Assert.That(rule.Evaluate(0.4f), Is.GreaterThan(0f));
            Assert.That(rule.Evaluate(0.7f), Is.GreaterThan(rule.Evaluate(0.4f)));
        }

        [TestCase(2f, DayPhase.DeepNight)]
        [TestCase(6f, DayPhase.Dawn)]
        [TestCase(12f, DayPhase.Day)]
        [TestCase(18f, DayPhase.Dusk)]
        [TestCase(22f, DayPhase.Night)]
        public void WorldTimeSnapshot_HourResolvesExpectedPhase(float hour, DayPhase expected)
        {
            Assert.That(new WorldTimeSnapshot(hour).Phase, Is.EqualTo(expected));
        }

        [Test]
        public void WorldTime_TickWrapsNightToDawnWithoutLosingDayIndex()
        {
            WorldTimeSystem time = new WorldTimeSystem(new WorldTimeProfile());
            time.SetHour(23.9f); long dayBefore = time.Snapshot.DayIndex;
            time.AdvanceHours(1f);
            Assert.That(time.Snapshot.Hour, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(time.Snapshot.DayIndex, Is.EqualTo(dayBefore + 1));
        }
    }
}
