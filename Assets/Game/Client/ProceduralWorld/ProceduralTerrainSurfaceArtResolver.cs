using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public static class ProceduralTerrainSurfaceArtResolver
    {
        public static Color[] Resolve(TerrainChunkData data, int styleVersion)
        {
            Color[] colors = new Color[data.Colors.Length];
            bool flatTopology = data.Vertices.Length == data.Triangles.Length;
            if (!flatTopology)
            {
                for (int index = 0; index < colors.Length; index++) colors[index] = Convert(data.Colors[index]);
                return colors;
            }

            for (int triangle = 0; triangle < data.Triangles.Length; triangle += 3)
            {
                WorldColor first = data.Colors[triangle];
                WorldColor second = data.Colors[triangle + 1];
                WorldColor third = data.Colors[triangle + 2];
                Color average = new Color(
                    (first.Red + second.Red + third.Red) / 3f,
                    (first.Green + second.Green + third.Green) / 3f,
                    (first.Blue + second.Blue + third.Blue) / 3f,
                    1f);
                int face = triangle / 3;
                int cellsPerAxis = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(data.TriangleCount * 0.5f)));
                int cell = face / 2;
                int globalCellX = (data.ChunkX * cellsPerAxis) + (cell % cellsPerAxis);
                int globalCellZ = (data.ChunkZ * cellsPerAxis) + (cell / cellsPerAxis);
                Color resolved = GradeFace(average, data.Normals[triangle], globalCellX / 4, globalCellZ / 4, styleVersion);
                colors[triangle] = resolved;
                colors[triangle + 1] = resolved;
                colors[triangle + 2] = resolved;
            }
            return colors;
        }

        private static Color GradeFace(Color source, WorldVector3 normal, int regionX, int regionZ, int styleVersion)
        {
            Color.RGBToHSV(source, out float hue, out float saturation, out float value);
            uint hash = Hash(unchecked((uint)regionX), unchecked((uint)regionZ), 0x54455252u, unchecked((uint)styleVersion));
            float variation = ((hash & 1023u) / 1023f - 0.5f) * 0.035f;
            float orientation = Mathf.Clamp((normal.Y - 0.78f) * 0.08f, -0.025f, 0.025f);
            saturation = Mathf.Clamp01(saturation * 1.06f);
            value = Mathf.Clamp01(value + variation + orientation);
            value = Mathf.Round(value * 24f) / 24f;
            Color result = Color.HSVToRGB(hue, saturation, value);
            result.a = 1f;
            return result;
        }

        private static uint Hash(uint first, uint second, uint third, uint fourth)
        {
            uint value = 2166136261u;
            value = (value ^ first) * 16777619u;
            value = (value ^ second) * 16777619u;
            value = (value ^ third) * 16777619u;
            return (value ^ fourth) * 16777619u;
        }

        private static Color Convert(WorldColor value) => new Color(value.Red, value.Green, value.Blue, 1f);
    }
}
