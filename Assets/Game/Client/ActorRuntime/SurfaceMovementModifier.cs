using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [CreateAssetMenu(menuName = "My Game World/Actor/Surface Movement Modifiers")]
    public sealed class SurfaceMovementModifierProfile : ScriptableObject
    {
        [Serializable] private struct Rule { public int SurfaceId; [Range(0f, 2f)] public float Multiplier; }
        [SerializeField] private Rule[] _rules = Array.Empty<Rule>();
        public float Resolve(int surfaceId)
        {
            for (int index = 0; index < _rules.Length; index++) if (_rules[index].SurfaceId == surfaceId) return _rules[index].Multiplier;
            return 1f;
        }
        public void SetRule(int surfaceId, float multiplier)
        {
            for (int index = 0; index < _rules.Length; index++)
                if (_rules[index].SurfaceId == surfaceId) { _rules[index].Multiplier = Mathf.Max(0f, multiplier); return; }
            Array.Resize(ref _rules, _rules.Length + 1); _rules[_rules.Length - 1] = new Rule { SurfaceId = surfaceId, Multiplier = Mathf.Max(0f, multiplier) };
        }
    }

    public interface ISurfaceMovementModifier : IActorCapability { }

    [DisallowMultipleComponent]
    public sealed class SurfaceMovementModifier : ActorCapability, ISurfaceMovementModifier
    {
        [SerializeField] private SurfaceMovementModifierProfile _profile;
        private IWalkCapability _walk; private IEnvironmentContextSensor _environment;
        public void Configure(SurfaceMovementModifierProfile profile)
        { if (IsInitialized) throw new InvalidOperationException("Surface modifier cannot change after initialization."); _profile = profile; }
        protected override void OnInitialized()
        {
            if (_profile == null) throw new InvalidOperationException("Surface movement modifier requires a profile.");
            if (!Context.Actor.Capabilities.TryGet(out _walk) || !Context.Actor.Sensors.TryGet(out _environment))
                throw new InvalidOperationException("Surface movement modifier requires Walk and EnvironmentContextSensor.");
            _environment.ContextChanged += OnContextChanged;
        }
        protected override void OnReleasing()
        { if (_environment != null) _environment.ContextChanged -= OnContextChanged; _walk?.RemoveSpeedModifier(this); }
        private void OnContextChanged(MyGameWorld.Client.EntityRuntime.WorldEnvironmentSnapshot context)
        {
            float multiplier = _profile.Resolve(context.SurfaceId);
            if (Mathf.Approximately(multiplier, 1f)) _walk.RemoveSpeedModifier(this);
            else { MovementModifier modifier = new MovementModifier(multiplier, label: "Surface"); _walk.SetSpeedModifier(this, in modifier); }
        }
    }
}
