using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public interface IScheduledActorController : IActorController { void TickDecision(float deltaTime); }

    [DisallowMultipleComponent]
    public sealed class ActorDecisionScheduler : MonoBehaviour
    {
        private readonly List<IScheduledActorController> _controllers = new List<IScheduledActorController>();
        public int RegisteredCount => _controllers.Count;
        public void Register(IScheduledActorController controller)
        { if (controller == null) throw new ArgumentNullException(nameof(controller)); if (!_controllers.Contains(controller)) _controllers.Add(controller); }
        public void Unregister(IScheduledActorController controller) => _controllers.Remove(controller);
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            for (int index = _controllers.Count - 1; index >= 0; index--)
            { IScheduledActorController controller = _controllers[index]; if (controller == null) { _controllers.RemoveAt(index); continue; } controller.TickDecision(deltaTime); }
        }
        private void Update() => Tick(Time.deltaTime);
    }

    [DisallowMultipleComponent]
    public sealed class MockAIController : ActorController, IScheduledActorController
    {
        [SerializeField, Min(1f)] private float _runCycleSeconds = 4f;
        [SerializeField, Range(0f, 1f)] private float _runFraction = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _turnVariation = 0.3f;
        private ActorDecisionScheduler _scheduler; private float _phase; private ulong _sequence;
        public void Configure(ActorDecisionScheduler scheduler, float initialPhase = 0f)
        { if (IsBound) throw new InvalidOperationException("Mock AI cannot change while bound."); _scheduler = scheduler; _phase = Mathf.Max(0f, initialPhase); }
        protected override void OnBound()
        { if (_scheduler == null) throw new InvalidOperationException("Mock AI requires an ActorDecisionScheduler."); _scheduler.Register(this); }
        protected override void OnUnbinding() => _scheduler?.Unregister(this);
        public void TickDecision(float deltaTime)
        {
            if (!IsBound || !Context.Actor.State.CanAct) return; _phase += deltaTime;
            float cycle = Mathf.Max(1f, _runCycleSeconds); float normalized = (_phase % cycle) / cycle;
            Vector2 direction = new Vector2(Mathf.Sin(_phase * 0.65f) * _turnVariation, 1f).normalized;
            MoveIntent move = new MoveIntent(++_sequence, direction); Context.Actor.Intents.Submit(in move);
            bool running = normalized >= 1f - _runFraction; RunIntent run = new RunIntent(++_sequence, running); Context.Actor.Intents.Submit(in run);
        }
    }
}
