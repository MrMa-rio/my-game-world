using System;
using MyGameWorld.Client.EntityRuntime;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IEnvironmentContextSensor : IActorSensor
    {
        WorldEnvironmentSnapshot Current { get; }
        event Action<WorldEnvironmentSnapshot> ContextChanged;
    }

    [DisallowMultipleComponent]
    public sealed class EnvironmentContextSensor : ActorSensor, IEnvironmentContextSensor
    {
        private IWorldEnvironmentContextProvider _provider;
        public WorldEnvironmentSnapshot Current { get; private set; }
        public event Action<WorldEnvironmentSnapshot> ContextChanged;
        public void Configure(IWorldEnvironmentContextProvider provider)
        {
            if (IsInitialized) throw new InvalidOperationException("Environment context cannot change after initialization.");
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }
        protected override void OnInitialized()
        {
            if (TickMode != SensorTickMode.Interval) throw new InvalidOperationException("Environment context must use interval scheduling.");
            if (_provider == null) throw new InvalidOperationException("Environment context requires a provider.");
        }
        protected override void Sample()
        {
            Current = _provider.Sample(Context.Transform.position, Context.Presence.GlobalPosition);
            ContextChanged?.Invoke(Current);
        }
    }
}
