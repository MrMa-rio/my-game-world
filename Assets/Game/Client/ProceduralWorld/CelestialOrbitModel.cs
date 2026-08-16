using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public readonly struct CelestialOrbitSnapshot
    {
        public CelestialOrbitSnapshot(Quaternion stellarRotation, Quaternion sunRotation, Quaternion moonRotation,
            float siderealAngle, float solarHourAngle, float lunarHourAngle)
        {
            StellarRotation = stellarRotation; SunRotation = sunRotation; MoonRotation = moonRotation;
            SiderealAngle = siderealAngle; SolarHourAngle = solarHourAngle; LunarHourAngle = lunarHourAngle;
        }
        public Quaternion StellarRotation { get; }
        public Quaternion SunRotation { get; }
        public Quaternion MoonRotation { get; }
        public float SiderealAngle { get; }
        public float SolarHourAngle { get; }
        public float LunarHourAngle { get; }
    }

    public static class CelestialOrbitModel
    {
        public const double SolarDayHours = 24d;
        public const double SiderealDayHours = 23.9344696d;
        public const double TropicalYearDays = 365.2422d;
        public const double LunarSiderealPeriodDays = 27.321662d;
        public const double LunarSynodicPeriodDays = 29.530589d;
        public const float AxialTiltDegrees = 23.4f;
        public const float LunarOrbitInclinationDegrees = 5.145f;
        private const float WorldNorthAzimuth = -28f;

        public static CelestialOrbitSnapshot Evaluate(WorldTimeSnapshot time)
        {
            double totalDays = time.TotalHours / SolarDayHours;
            float siderealAngle = RepeatDegrees(time.TotalHours / SiderealDayHours * 360d);
            float solarLongitude = RepeatDegrees(totalDays / TropicalYearDays * 360d);
            float lunarLongitude = RepeatDegrees(180d + totalDays / LunarSiderealPeriodDays * 360d);
            float solarHourAngle = RepeatDegrees(siderealAngle - solarLongitude);
            float lunarHourAngle = RepeatDegrees(siderealAngle - lunarLongitude);
            float solarDeclination = AxialTiltDegrees * Mathf.Sin(solarLongitude * Mathf.Deg2Rad);
            float lunarLatitude = LunarOrbitInclinationDegrees * Mathf.Sin(lunarLongitude * Mathf.Deg2Rad);

            Quaternion referenceFrame = Quaternion.Euler(0f, WorldNorthAzimuth, AxialTiltDegrees);
            Quaternion stellarRotation = referenceFrame * Quaternion.AngleAxis(siderealAngle - 90f, Vector3.right);
            Quaternion sunRotation = referenceFrame * Quaternion.AngleAxis(solarHourAngle - 90f, Vector3.right) *
                Quaternion.AngleAxis(solarDeclination, Vector3.forward);
            Quaternion moonRotation = referenceFrame * Quaternion.AngleAxis(lunarHourAngle - 90f, Vector3.right) *
                Quaternion.AngleAxis(lunarLatitude, Vector3.forward);
            return new CelestialOrbitSnapshot(stellarRotation, sunRotation, moonRotation, siderealAngle, solarHourAngle, lunarHourAngle);
        }

        private static float RepeatDegrees(double value)
        {
            double repeated = value % 360d; if (repeated < 0d) repeated += 360d; return (float)repeated;
        }
    }
}
