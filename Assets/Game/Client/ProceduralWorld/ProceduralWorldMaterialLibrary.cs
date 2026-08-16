using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public sealed class ProceduralWorldMaterialLibrary : IDisposable
    {
        private readonly List<Material> _materials = new List<Material>();
        private readonly Shader _shader;

        public ProceduralWorldMaterialLibrary()
        {
            Shader.SetGlobalVector("_WorldTimeTint", Vector4.one);
            _shader = Shader.Find("MyGameWorld/Procedural World/Vertex Color Lit");
            if (_shader == null)
            {
                throw new InvalidOperationException("Procedural world shader was not found.");
            }

            Terrain = Create("Terrain", new Color(0.8f, 0.84f, 0.76f));
            Wireframe = Create("Wireframe", new Color(0.06f, 0.08f, 0.07f));
            Trunk = Create("Trunk", new Color(0.34f, 0.18f, 0.08f));
            Leaves = Create("Leaves", new Color(0.18f, 0.58f, 0.22f));
            LeavesLight = Create("Leaves Light", new Color(0.32f, 0.72f, 0.28f));
            Rock = Create("Rock", new Color(0.43f, 0.45f, 0.42f));
            RockLight = Create("Rock Light", new Color(0.55f, 0.56f, 0.50f));
            RockDark = Create("Rock Dark", new Color(0.30f, 0.33f, 0.31f));
            Marker = Create("Scale Marker", new Color(0.95f, 0.48f, 0.08f));
            FlowerStem = Create("Flower Stem", new Color(0.16f, 0.46f, 0.15f));
            FlowerPetal = Create("Flower Petal", new Color(0.88f, 0.34f, 0.48f));
            FlowerPetalLight = Create("Flower Petal Light", new Color(1f, 0.72f, 0.26f));
            MushroomStem = Create("Mushroom Stem", new Color(0.76f, 0.68f, 0.52f));
            MushroomCap = Create("Mushroom Cap", new Color(0.67f, 0.18f, 0.11f));
            MushroomCapLight = Create("Mushroom Cap Light", new Color(0.92f, 0.42f, 0.18f));
            Water = Create("Water", new Color(0.18f, 0.58f, 0.82f));
            Lava = Create("Lava", new Color(1f, 0.24f, 0.035f));
            Branch = Create("Branch", new Color(0.34f, 0.18f, 0.08f));
            EnvironmentVfx = Create("Environmental VFX", Color.white);
            SetSurfaceResponse(Terrain, 0.14f, 0.20f);
            SetSurfaceResponse(Trunk, 0.08f, 0.16f); SetSurfaceResponse(Branch, 0.08f, 0.16f);
            SetSurfaceResponse(Leaves, 0.12f, 0.24f); SetSurfaceResponse(LeavesLight, 0.14f, 0.28f);
            SetSurfaceResponse(Rock, 0.22f, 0.38f); SetSurfaceResponse(RockLight, 0.24f, 0.42f); SetSurfaceResponse(RockDark, 0.18f, 0.34f);
            SetSurfaceResponse(Water, 0.86f, 0.88f); SetSurfaceResponse(Lava, 0.38f, 0.58f);
            SetSurfaceResponse(FlowerPetal, 0.20f, 0.40f); SetSurfaceResponse(FlowerPetalLight, 0.24f, 0.46f);
            SetSurfaceResponse(MushroomCap, 0.28f, 0.48f); SetSurfaceResponse(MushroomCapLight, 0.32f, 0.52f);
            SetWindResponse(Trunk, PhysicalResponseCatalog.Resolve(PhysicalResponseZone.Trunk).ShaderResponse, 0.25f);
            SetWindResponse(Branch, PhysicalResponseCatalog.Resolve(PhysicalResponseZone.LargeBranch).ShaderResponse, 0.18f);
            float leafResponse = PhysicalResponseCatalog.Resolve(PhysicalResponseZone.Leaves).ShaderResponse;
            SetWindResponse(Leaves, leafResponse, 0.05f); SetWindResponse(LeavesLight, leafResponse, 0.05f);
            float flexibleResponse = PhysicalResponseCatalog.Resolve(PhysicalResponseZone.FlexibleSurface).ShaderResponse;
            SetWindResponse(FlowerStem, flexibleResponse * 0.55f, 0.02f);
            SetWindResponse(FlowerPetal, flexibleResponse, 0f); SetWindResponse(FlowerPetalLight, flexibleResponse, 0f);
            TreeMaterials = new[] { Trunk, Leaves, LeavesLight, Branch };
            BushMaterials = new[] { Leaves, LeavesLight };
            BushLowMaterials = new[] { Leaves };
            RockMaterials = new[] { Rock, RockLight, RockDark };
            MarkerMaterials = new[] { Marker };
            FlowerMaterials = new[] { FlowerStem, FlowerPetal, FlowerPetalLight };
            MushroomMaterials = new[] { MushroomStem, MushroomCap, MushroomCapLight };
        }

        public Material Terrain { get; }

        public Material Wireframe { get; }

        public Material Trunk { get; }

        public Material Leaves { get; }

        public Material LeavesLight { get; }

        public Material Rock { get; }
        public Material RockLight { get; }
        public Material RockDark { get; }

        public Material Marker { get; }
        public Material FlowerStem { get; }
        public Material FlowerPetal { get; }
        public Material FlowerPetalLight { get; }
        public Material MushroomStem { get; }
        public Material MushroomCap { get; }
        public Material MushroomCapLight { get; }
        public Material Water { get; }
        public Material Lava { get; }
        public Material Branch { get; }
        public Material EnvironmentVfx { get; }
        public Material[] TreeMaterials { get; }
        public Material[] BushMaterials { get; }
        public Material[] BushLowMaterials { get; }
        public Material[] RockMaterials { get; }
        public Material[] MarkerMaterials { get; }
        public Material[] FlowerMaterials { get; }
        public Material[] MushroomMaterials { get; }

        public void Dispose()
        {
            for (int index = 0; index < _materials.Count; index++)
            {
                if (_materials[index] != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(_materials[index]);
                    else UnityEngine.Object.DestroyImmediate(_materials[index]);
                }
            }

            _materials.Clear();
        }

        private Material Create(string name, Color tint)
        {
            Material material = new Material(_shader)
            {
                name = $"Procedural {name} Material",
                enableInstancing = true
            };
            material.SetColor("_BaseColor", tint);
            material.SetColor("_InstanceColor", Color.white);
            _materials.Add(material);
            return material;
        }

        private static void SetWindResponse(Material material, float response, float heightStart)
        {
            material.SetFloat("_WindResponse", response); material.SetFloat("_WindHeightStart", heightStart);
        }

        private static void SetSurfaceResponse(Material material, float reflection, float smoothness)
        {
            material.SetFloat("_ReflectionStrength", reflection); material.SetFloat("_SurfaceSmoothness", smoothness);
        }
    }
}
