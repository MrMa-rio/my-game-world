using System;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    public readonly struct TasteStimulus
    {
        public TasteStimulus(int flavorId, float intensity, float toxicity, float nutrition, GameObject source = null)
        {
            FlavorId = flavorId; Intensity = Mathf.Max(0f, intensity); Toxicity = Mathf.Max(0f, toxicity);
            Nutrition = Mathf.Max(0f, nutrition); Source = source;
        }
        public int FlavorId { get; }
        public float Intensity { get; }
        public float Toxicity { get; }
        public float Nutrition { get; }
        public GameObject Source { get; }
    }

    public interface ITasteSensor : IActorSensor
    {
        TasteStimulus LastTaste { get; }
        event Action<TasteStimulus> TasteSensed;
        bool Sense(in TasteStimulus stimulus);
    }

    [DisallowMultipleComponent]
    public sealed class TasteSensor : ActorSensor, ITasteSensor
    {
        public TasteStimulus LastTaste { get; private set; }
        public event Action<TasteStimulus> TasteSensed;

        protected override void OnInitialized()
        {
            if (TickMode != SensorTickMode.EventDriven)
                throw new InvalidOperationException("TasteSensor must be event-driven.");
        }

        protected override void Sample() { }

        public bool Sense(in TasteStimulus stimulus)
        {
            if (!IsInitialized || !IsEnabled || !Context.Actor.State.CanAct) return false;
            LastTaste = stimulus; TasteSensed?.Invoke(stimulus); return true;
        }
    }
}
