using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class CelestialCycleSystem : IDisposable
    {
        // Noon sun plus the configured daytime ambient sky defines normalized local luminosity 1.0.
        public const float MaximumSolarIlluminationReference = 1.8f;
        private static readonly int SunDirection = Shader.PropertyToID("_CelestialSunDirection");
        private static readonly int MoonDirection = Shader.PropertyToID("_CelestialMoonDirection");
        private static readonly int CelestialTime = Shader.PropertyToID("_CelestialTime");
        private static readonly int DayColor = Shader.PropertyToID("_CelestialDayColor");
        private static readonly int NightColor = Shader.PropertyToID("_CelestialNightColor");
        private static readonly int HorizonColor = Shader.PropertyToID("_CelestialHorizonColor");
        private static readonly int WorldTimeTint = Shader.PropertyToID("_WorldTimeTint");
        private static readonly int WorldTimeRimColor = Shader.PropertyToID("_WorldTimeRimColor");
        private readonly Light _sun; private readonly Light _moon; private readonly bool _ownsSun;
        private readonly Material _skyMaterial; private readonly Material _previousSkybox;

        public CelestialCycleSystem(Transform parent)
        {
            _sun = RenderSettings.sun != null ? RenderSettings.sun : CreateLight("Sun", parent);
            _ownsSun = RenderSettings.sun == null; _sun.name = "Sun"; _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft; _sun.shadowStrength = 0.78f; RenderSettings.sun = _sun;
            _moon = CreateLight("Moon", parent); _moon.type = LightType.Directional; _moon.shadows = LightShadows.Soft; _moon.shadowStrength = 0.34f;
            Shader skyShader = Shader.Find("MyGameWorld/Procedural World/Celestial Sky");
            if (skyShader == null) throw new InvalidOperationException("Celestial sky shader was not found.");
            _previousSkybox = RenderSettings.skybox; _skyMaterial = new Material(skyShader) { name = "Runtime Celestial Sky" };
            _skyMaterial.SetFloat("_Exposure", 1f); RenderSettings.skybox = _skyMaterial;
            RenderSettings.ambientMode = AmbientMode.Trilight;
        }

        public Light Sun => _sun; public Light Moon => _moon;
        public CelestialOrbitSnapshot Orbit { get; private set; }

        public void Apply(WorldTimeSnapshot time)
        {
            Orbit = CelestialOrbitModel.Evaluate(time);
            _sun.transform.rotation = Orbit.SunRotation;
            _moon.transform.rotation = Orbit.MoonRotation;
            Vector3 sunToSky = -_sun.transform.forward; Vector3 moonToSky = -_moon.transform.forward;
            float twilight = Mathf.Max(time.Dawn, time.Dusk);
            _sun.intensity = time.Daylight * Mathf.Lerp(0.32f, 1.28f, time.Daylight);
            _sun.color = Color.Lerp(new Color(1f, 0.48f, 0.2f), new Color(1f, 0.92f, 0.76f), time.Daylight);
            _sun.enabled = _sun.intensity > 0.005f;
            _moon.intensity = time.Night * 0.24f; _moon.color = new Color(0.48f, 0.62f, 1f); _moon.enabled = _moon.intensity > 0.015f;

            Color deepNightZenith = new Color(0.018f, 0.035f, 0.12f);
            Color dayZenith = new Color(0.24f, 0.58f, 0.94f);
            Color dawnZenith = new Color(0.20f, 0.28f, 0.58f);
            Color duskZenith = new Color(0.16f, 0.30f, 0.62f);
            Color deepNightHorizon = new Color(0.045f, 0.10f, 0.24f);
            Color dayHorizon = new Color(0.62f, 0.82f, 0.96f);
            Color dawnHorizon = new Color(1f, 0.48f, 0.30f);
            Color duskHorizon = new Color(1f, 0.25f, 0.075f);
            Color zenith = Color.Lerp(deepNightZenith, dayZenith, time.Daylight);
            Color horizon = Color.Lerp(deepNightHorizon, dayHorizon, time.Daylight);
            zenith = Color.Lerp(zenith, dawnZenith, time.Dawn * 0.82f);
            zenith = Color.Lerp(zenith, duskZenith, time.Dusk * 0.88f);
            horizon = Color.Lerp(horizon, dawnHorizon, time.Dawn);
            horizon = Color.Lerp(horizon, duskHorizon, time.Dusk);
            Shader.SetGlobalVector(SunDirection, new Vector4(sunToSky.x, sunToSky.y, sunToSky.z, 0f));
            Shader.SetGlobalVector(MoonDirection, new Vector4(moonToSky.x, moonToSky.y, moonToSky.z, 0f));
            Shader.SetGlobalVector(CelestialTime, new Vector4(time.Daylight, time.Night, time.Dawn, time.Dusk));
            Shader.SetGlobalColor(DayColor, zenith); Shader.SetGlobalColor(NightColor, zenith); Shader.SetGlobalColor(HorizonColor, horizon);
            _skyMaterial.SetVector("_CelestialSunDirection", new Vector4(sunToSky.x, sunToSky.y, sunToSky.z, 0f));
            _skyMaterial.SetVector("_CelestialMoonDirection", new Vector4(moonToSky.x, moonToSky.y, moonToSky.z, 0f));
            _skyMaterial.SetVector("_CelestialTime", new Vector4(time.Daylight, time.Night, time.Dawn, time.Dusk));
            _skyMaterial.SetColor("_CelestialDayColor", zenith); _skyMaterial.SetColor("_CelestialNightColor", zenith);
            _skyMaterial.SetColor("_CelestialHorizonColor", horizon);
            Color worldTint = Color.Lerp(new Color(0.38f, 0.49f, 0.82f), Color.white, time.Daylight);
            worldTint = Color.Lerp(worldTint, new Color(0.84f, 0.72f, 0.80f), time.Dawn * 0.48f);
            worldTint = Color.Lerp(worldTint, new Color(1f, 0.61f, 0.38f), time.Dusk * 0.52f);
            float exposure = Mathf.Lerp(0.66f, 1f, time.Daylight) + twilight * 0.08f;
            Shader.SetGlobalVector(WorldTimeTint, new Vector4(worldTint.r, worldTint.g, worldTint.b, exposure));
            Color rimColor = Color.Lerp(new Color(0.12f, 0.32f, 0.86f), new Color(0.34f, 0.54f, 0.82f), time.Daylight);
            rimColor = Color.Lerp(rimColor, new Color(1f, 0.34f, 0.10f), time.Dusk * 0.72f);
            rimColor = Color.Lerp(rimColor, new Color(0.62f, 0.48f, 0.94f), time.Dawn * 0.48f);
            float rimStrength = Mathf.Lerp(0.34f, 0.035f, time.Daylight) + twilight * 0.08f;
            Shader.SetGlobalVector(WorldTimeRimColor, new Vector4(rimColor.r, rimColor.g, rimColor.b, rimStrength));

            RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.06f, 0.12f, 0.34f), new Color(0.46f, 0.65f, 0.82f), time.Daylight);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.06f, 0.13f, 0.31f), new Color(0.40f, 0.48f, 0.42f), time.Daylight);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.035f, 0.055f, 0.13f), new Color(0.22f, 0.25f, 0.20f), time.Daylight);
            RenderSettings.ambientIntensity = Mathf.Lerp(0.56f, 1f, time.Daylight);
            Color fogColor = Color.Lerp(deepNightHorizon, new Color(0.61f, 0.75f, 0.82f), time.Daylight);
            fogColor = Color.Lerp(fogColor, dawnHorizon, time.Dawn * 0.42f);
            fogColor = Color.Lerp(fogColor, duskHorizon, time.Dusk * 0.48f);
            RenderSettings.fogColor = fogColor;
        }

        public void Dispose()
        {
            RenderSettings.skybox = _previousSkybox;
            if (_skyMaterial != null) UnityEngine.Object.Destroy(_skyMaterial);
            if (_moon != null) UnityEngine.Object.Destroy(_moon.gameObject);
            if (_ownsSun && _sun != null) UnityEngine.Object.Destroy(_sun.gameObject);
        }

        private static Light CreateLight(string name, Transform parent)
        {
            GameObject root = new GameObject(name); root.transform.SetParent(parent, false); return root.AddComponent<Light>();
        }
    }
}
