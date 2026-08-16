using System.Collections.Generic;
using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    public static class PhysicalResponseCatalog
    {
        public static PhysicalResponseProfile Resolve(PhysicalResponseZone zone)
        {
            switch (zone)
            {
                case PhysicalResponseZone.Root: return new PhysicalResponseProfile(40f, 1f, 0f, 0.95f, 1.5f, 0.2f, 0.4f);
                case PhysicalResponseZone.Trunk: return new PhysicalResponseProfile(20f, 0.94f, 0.1f, 0.86f, 1.1f, 2f, 0.55f);
                case PhysicalResponseZone.LargeBranch: return new PhysicalResponseProfile(3.5f, 0.68f, 0.42f, 0.67f, 0.72f, 2.2f, 0.8f);
                case PhysicalResponseZone.SmallBranch: return new PhysicalResponseProfile(1.2f, 0.38f, 0.7f, 0.54f, 0.46f, 1.5f, 0.9f);
                case PhysicalResponseZone.Leaves: return new PhysicalResponseProfile(0.12f, 0.08f, 1f, 0.34f, 0.12f, 1f, 1f);
                default: return new PhysicalResponseProfile(0.5f, 0.22f, 0.82f, 0.48f, 0.25f, 0.8f, 0.9f);
            }
        }
    }

    public sealed class EnvironmentalPhysicalResponseSystem
    {
        private readonly List<ResponseRegistration> _registrations = new List<ResponseRegistration>();
        private int _cursor;
        public int RegisteredCount => _registrations.Count;

        public void Register(GameObject root, DecorationKind kind)
        {
            if (root == null || !Supports(kind)) return;
            _registrations.Add(new ResponseRegistration(root.transform, kind, CreateZones(kind)));
        }

        public void Unregister(GameObject root)
        {
            if (root == null) return;
            for (int index = _registrations.Count - 1; index >= 0; index--)
                if (_registrations[index].Transform == root.transform) _registrations.RemoveAt(index);
            if (_cursor >= _registrations.Count) _cursor = 0;
        }

        public void ProcessBatch(WindSystem wind, Camera camera, int budget)
        {
            if (wind == null || _registrations.Count == 0) return;
            int count = Mathf.Min(Mathf.Max(1, budget), _registrations.Count);
            for (int processed = 0; processed < count; processed++)
            {
                if (_cursor >= _registrations.Count) _cursor = 0;
                ResponseRegistration registration = _registrations[_cursor++];
                if (registration.Transform == null || !registration.Transform.gameObject.activeInHierarchy) continue;
                float distance = camera != null ? Vector3.Distance(camera.transform.position, registration.Transform.position) : 0f;
                registration.LastLod = distance < 30f ? ProceduralVisualLod.High : distance < 80f ? ProceduralVisualLod.Medium : ProceduralVisualLod.Low;
                registration.LastSample = wind.SampleWind(registration.Transform.position);
            }
        }

        public IReadOnlyList<PhysicalResponseZone> ResolveZones(DecorationKind kind) => CreateZones(kind);

        private static bool Supports(DecorationKind kind) => kind == DecorationKind.Tree || kind == DecorationKind.TreeCluster ||
            kind == DecorationKind.Bush || kind == DecorationKind.BushCluster || kind == DecorationKind.Flower || kind == DecorationKind.FlowerCluster;

        private static PhysicalResponseZone[] CreateZones(DecorationKind kind)
        {
            if (kind == DecorationKind.Tree || kind == DecorationKind.TreeCluster)
                return new[] { PhysicalResponseZone.Root, PhysicalResponseZone.Trunk, PhysicalResponseZone.LargeBranch, PhysicalResponseZone.SmallBranch, PhysicalResponseZone.Leaves };
            return new[] { PhysicalResponseZone.Root, PhysicalResponseZone.FlexibleSurface };
        }

        private sealed class ResponseRegistration
        {
            public ResponseRegistration(Transform transform, DecorationKind kind, PhysicalResponseZone[] zones)
            { Transform = transform; Kind = kind; Zones = zones; }
            public Transform Transform { get; } public DecorationKind Kind { get; } public PhysicalResponseZone[] Zones { get; }
            public WindSample LastSample { get; set; } public ProceduralVisualLod LastLod { get; set; }
        }
    }
}
