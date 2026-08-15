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
            _shader = Shader.Find("MyGameWorld/Procedural World/Vertex Color Lit");
            if (_shader == null)
            {
                throw new InvalidOperationException("Procedural world shader was not found.");
            }

            Terrain = Create("Terrain", Color.white);
            Wireframe = Create("Wireframe", new Color(0.06f, 0.08f, 0.07f));
            Trunk = Create("Trunk", new Color(0.34f, 0.18f, 0.08f));
            Leaves = Create("Leaves", new Color(0.18f, 0.58f, 0.22f));
            LeavesLight = Create("Leaves Light", new Color(0.32f, 0.72f, 0.28f));
            Rock = Create("Rock", new Color(0.42f, 0.45f, 0.43f));
            Marker = Create("Scale Marker", new Color(0.95f, 0.48f, 0.08f));
            TreeMaterials = new[] { Trunk, Leaves, LeavesLight };
            BushMaterials = new[] { Leaves, LeavesLight };
            BushLowMaterials = new[] { Leaves };
            RockMaterials = new[] { Rock };
            MarkerMaterials = new[] { Marker };
        }

        public Material Terrain { get; }

        public Material Wireframe { get; }

        public Material Trunk { get; }

        public Material Leaves { get; }

        public Material LeavesLight { get; }

        public Material Rock { get; }

        public Material Marker { get; }
        public Material[] TreeMaterials { get; }
        public Material[] BushMaterials { get; }
        public Material[] BushLowMaterials { get; }
        public Material[] RockMaterials { get; }
        public Material[] MarkerMaterials { get; }

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
    }
}
