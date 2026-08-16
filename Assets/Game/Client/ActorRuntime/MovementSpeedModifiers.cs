using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public readonly struct MovementModifier
    {
        public MovementModifier(float multiplier, float additive = 0f, string label = null)
        { Multiplier = Mathf.Max(0f, multiplier); Additive = additive; Label = label ?? string.Empty; }
        public float Multiplier { get; }
        public float Additive { get; }
        public string Label { get; }
    }

    public readonly struct ResolvedMovementSpeed
    {
        public ResolvedMovementSpeed(float baseSpeed, float additive, float multiplier)
        { BaseSpeed = baseSpeed; Additive = additive; Multiplier = multiplier; FinalSpeed = Mathf.Max(0f, baseSpeed + additive) * multiplier; }
        public float BaseSpeed { get; }
        public float Additive { get; }
        public float Multiplier { get; }
        public float FinalSpeed { get; }
    }

    public sealed class MovementSpeedModifiers
    {
        private readonly Dictionary<object, MovementModifier> _modifiers = new Dictionary<object, MovementModifier>();

        public int Count => _modifiers.Count;

        public void Set(object source, float multiplier)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Set(source, new MovementModifier(multiplier));
        }

        public void Set(object source, in MovementModifier modifier)
        { if (source == null) throw new ArgumentNullException(nameof(source)); _modifiers[source] = modifier; }

        public bool Remove(object source) => source != null && _modifiers.Remove(source);

        public float Resolve(float baseSpeed)
        {
            return ResolveDetailed(baseSpeed).FinalSpeed;
        }

        public ResolvedMovementSpeed ResolveDetailed(float baseSpeed)
        {
            float multiplier = 1f; float additive = 0f;
            foreach (MovementModifier modifier in _modifiers.Values)
            { multiplier *= modifier.Multiplier; additive += modifier.Additive; }
            return new ResolvedMovementSpeed(Mathf.Max(0f, baseSpeed), additive, multiplier);
        }
    }
}
