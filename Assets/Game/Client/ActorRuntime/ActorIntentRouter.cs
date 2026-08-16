using System;
using System.Collections.Generic;

namespace MyGameWorld.Client.ActorRuntime
{
    public enum IntentDispatchResult : byte
    {
        Accepted = 1,
        ActorUnavailable = 2,
        NoHandler = 3,
        HandlerUnavailable = 4
    }

    public interface IActorIntentHandler<TIntent> where TIntent : struct, IActorIntent
    {
        void HandleIntent(in TIntent intent);
    }

    public sealed class ActorIntentRouter
    {
        private interface IIntentRoute
        {
            IActorCapability Owner { get; }
        }

        private sealed class IntentRoute<TIntent> : IIntentRoute where TIntent : struct, IActorIntent
        {
            public IntentRoute(IActorIntentHandler<TIntent> handler, IActorCapability owner)
            { Handler = handler; Owner = owner; }
            public IActorIntentHandler<TIntent> Handler { get; }
            public IActorCapability Owner { get; }
        }

        private readonly Actor _actor;
        private readonly Dictionary<Type, IIntentRoute> _routes = new Dictionary<Type, IIntentRoute>();

        public ActorIntentRouter(Actor actor) => _actor = actor != null ? actor : throw new ArgumentNullException(nameof(actor));
        public int HandlerCount => _routes.Count;

        public void Register<TIntent>(IActorIntentHandler<TIntent> handler, IActorCapability owner)
            where TIntent : struct, IActorIntent
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            Type intentType = typeof(TIntent);
            if (_routes.ContainsKey(intentType)) throw new InvalidOperationException($"Intent {intentType.Name} already has a handler.");
            _routes.Add(intentType, new IntentRoute<TIntent>(handler, owner));
        }

        public IntentDispatchResult Submit<TIntent>(in TIntent intent) where TIntent : struct, IActorIntent
        {
            if (!_actor.State.CanAct) return IntentDispatchResult.ActorUnavailable;
            IIntentRoute route;
            if (!_routes.TryGetValue(typeof(TIntent), out route)) return IntentDispatchResult.NoHandler;
            IntentRoute<TIntent> typedRoute = (IntentRoute<TIntent>)route;
            if (!typedRoute.Owner.CanExecute) return IntentDispatchResult.HandlerUnavailable;
            typedRoute.Handler.HandleIntent(in intent);
            return IntentDispatchResult.Accepted;
        }

        public void UnregisterOwner(IActorCapability owner)
        {
            if (owner == null || _routes.Count == 0) return;
            List<Type> remove = null;
            foreach (KeyValuePair<Type, IIntentRoute> pair in _routes)
            {
                if (!ReferenceEquals(pair.Value.Owner, owner)) continue;
                if (remove == null) remove = new List<Type>();
                remove.Add(pair.Key);
            }
            if (remove == null) return;
            for (int index = 0; index < remove.Count; index++) _routes.Remove(remove[index]);
        }
    }
}
