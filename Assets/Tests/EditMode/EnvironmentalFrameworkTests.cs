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

        [Test]
        public void StarVisibility_RevealsHighlightsBeforeFullNightAndHidesDuringDay()
        {
            float earlyDusk = new WorldTimeSnapshot(18f).StarVisibility;
            float lateDusk = new WorldTimeSnapshot(19.5f).StarVisibility;
            Assert.That(new WorldTimeSnapshot(12f).StarVisibility, Is.Zero);
            Assert.That(earlyDusk, Is.GreaterThan(0f).And.LessThan(lateDusk));
            Assert.That(new WorldTimeSnapshot(22f).StarVisibility, Is.EqualTo(1f));
        }

        [Test]
        public void StarVisibility_DawnReducesPopulationBeforeRemovingAllStars()
        {
            float earlyDawn = new WorldTimeSnapshot(5f).StarVisibility;
            float lateDawn = new WorldTimeSnapshot(6.5f).StarVisibility;
            Assert.That(earlyDawn, Is.GreaterThan(lateDawn));
            Assert.That(lateDawn, Is.GreaterThan(0f));
            Assert.That(new WorldTimeSnapshot(7.1f).StarVisibility, Is.Zero);
        }

        [Test]
        public void StarDensity_LocalLuminosityScalesContinuouslyUpToThirtyTimes()
        {
            Assert.That(ProceduralStarFieldSystem.ResolveDensityMultiplier(0.2f), Is.EqualTo(30f));
            Assert.That(ProceduralStarFieldSystem.ResolveDensityMultiplier(0.5f), Is.InRange(1.01f, 29.99f));
            Assert.That(ProceduralStarFieldSystem.ResolveDensityMultiplier(0.75f), Is.EqualTo(1f));
        }

        [Test]
        public void CelestialOrbit_OneSolarDayUsesDistinctPhysicalAngularRates()
        {
            CelestialOrbitSnapshot start = CelestialOrbitModel.Evaluate(new WorldTimeSnapshot(0d));
            CelestialOrbitSnapshot nextDay = CelestialOrbitModel.Evaluate(new WorldTimeSnapshot(24d));
            float stellarAdvance = Mathf.DeltaAngle(start.SiderealAngle, nextDay.SiderealAngle);
            float solarAdvance = Mathf.DeltaAngle(start.SolarHourAngle, nextDay.SolarHourAngle);
            float lunarAdvance = Mathf.DeltaAngle(start.LunarHourAngle, nextDay.LunarHourAngle);
            Assert.That(stellarAdvance, Is.EqualTo(0.9856f).Within(0.002f));
            Assert.That(solarAdvance, Is.EqualTo(0f).Within(0.002f));
            Assert.That(lunarAdvance, Is.EqualTo(-12.19f).Within(0.03f));
        }

        [Test]
        public void ShaderBudget_QualityLayersIncreaseWithoutRemovingBaseLighting()
        {
            ProceduralShaderBudget low = ProceduralShaderManager.ResolveBudget(ProceduralShaderQuality.Low);
            ProceduralShaderBudget high = ProceduralShaderManager.ResolveBudget(ProceduralShaderQuality.High);
            ProceduralShaderBudget ultra = ProceduralShaderManager.ResolveBudget(ProceduralShaderQuality.Ultra);
            Assert.That(low.Has(ProceduralShaderLayer.BaseLighting), Is.True);
            Assert.That(low.Has(ProceduralShaderLayer.StylizedReflection), Is.False);
            Assert.That(high.Has(ProceduralShaderLayer.StylizedReflection), Is.True);
            Assert.That(ultra.DiffuseBands, Is.GreaterThan(high.DiffuseBands));
            Assert.That(ultra.ReflectionStrength, Is.GreaterThan(high.ReflectionStrength));
        }

        [Test]
        public void LightingPalette_AdjacentTimesBlendWithoutAbruptColorJumps()
        {
            ProceduralLightingPaletteSample before = ProceduralShaderManager.EvaluatePalette(17.49f);
            ProceduralLightingPaletteSample after = ProceduralShaderManager.EvaluatePalette(17.51f);
            Assert.That(ColorDistance(before.WorldTint, after.WorldTint), Is.LessThan(0.02f));
            Assert.That(ColorDistance(before.ShadowColor, after.ShadowColor), Is.LessThan(0.02f));
            Assert.That(Mathf.Abs(before.Exposure - after.Exposure), Is.LessThan(0.02f));
        }

        [Test]
        public void LightingPalette_MidnightWrapRemainsContinuous()
        {
            ProceduralLightingPaletteSample before = ProceduralShaderManager.EvaluatePalette(23.99f);
            ProceduralLightingPaletteSample after = ProceduralShaderManager.EvaluatePalette(0.01f);
            Assert.That(ColorDistance(before.WorldTint, after.WorldTint), Is.LessThan(0.02f));
        }

        private static float ColorDistance(Color first, Color second)
        {
            Vector4 delta = (Vector4)first - (Vector4)second;
            return delta.magnitude;
        }
    }
}
