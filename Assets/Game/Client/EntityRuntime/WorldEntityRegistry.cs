using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Core;

namespace MyGameWorld.Client.EntityRuntime
{
    public interface IWorldEntityRegistry
    {
        int Count { get; }
        void Register(WorldEntity entity);
        void Unregister(WorldEntity entity);
        bool TryGet(EntityId entityId, out WorldEntity entity);
    }

    public sealed class WorldEntityRegistry : IWorldEntityRegistry
    {
        private readonly Dictionary<EntityId, WorldEntity> _entities = new Dictionary<EntityId, WorldEntity>();
        public int Count => _entities.Count;

        public void Register(WorldEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (!entity.IsInitialized) throw new InvalidOperationException("An entity must be initialized before registration.");
            if (_entities.ContainsKey(entity.EntityId)) throw new InvalidOperationException($"Entity {entity.EntityId} is already registered.");
            _entities.Add(entity.EntityId, entity);
        }

        public void Unregister(WorldEntity entity)
        {
            if (entity == null || !entity.IsInitialized) return;
            WorldEntity registered;
            if (_entities.TryGetValue(entity.EntityId, out registered) && ReferenceEquals(registered, entity))
                _entities.Remove(entity.EntityId);
        }

        public bool TryGet(EntityId entityId, out WorldEntity entity) => _entities.TryGetValue(entityId, out entity);
    }
}
