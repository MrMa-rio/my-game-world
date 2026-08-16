using System;
using MyGameWorld.Client.EntityRuntime;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public sealed class ActorContext
    {
        public ActorContext(Actor actor, WorldEntity entity)
        {
            Actor = actor != null ? actor : throw new ArgumentNullException(nameof(actor));
            Entity = entity != null ? entity : throw new ArgumentNullException(nameof(entity));
            if (!entity.IsInitialized) throw new InvalidOperationException("Actor context requires an initialized world entity.");
        }

        public Actor Actor { get; }
        public WorldEntity Entity { get; }
        public WorldPresence Presence => Entity.Presence;
        public Transform Transform => Entity.transform;
    }
}
