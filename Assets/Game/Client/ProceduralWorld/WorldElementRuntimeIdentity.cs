using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class WorldElementRuntimeIdentity : MonoBehaviour
    {
        [SerializeField] private long _elementId;
        [SerializeField] private long _zoneId;
        [SerializeField] private long _seed;
        [SerializeField] private WorldElementKind _kind;
        [SerializeField] private ushort _generatorVersion;

        public long ElementId => _elementId;
        public long ZoneId => _zoneId;
        public long Seed => _seed;
        public WorldElementKind Kind => _kind;
        public ushort GeneratorVersion => _generatorVersion;

        public void Initialize(WorldElementDNA dna)
        {
            _elementId = dna.ElementId.Value;
            _zoneId = dna.ZoneId.Value;
            _seed = dna.Seed;
            _kind = dna.ElementKind;
            _generatorVersion = dna.GeneratorVersion.Value;
        }
    }
}
