using System;
using System.Collections.Generic;
using MyGameWorld.Client.ActorRuntime;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    public interface IPlayerSensoryFeedback
    {
        void PresentVision(IReadOnlyList<IVisionTarget> targets);
        void PresentHearing(HeardSound sound);
        void PresentTouch(TouchEvent contact);
        void PresentSmell(IReadOnlyList<DetectedScent> scents);
        void PresentTaste(TasteStimulus taste);
        void PresentProprioception(ProprioceptionSnapshot state);
    }

    public sealed class PlayerSensoryPresentation : IDisposable
    {
        private readonly IVisionSensor _vision; private readonly IHearingSensor _hearing;
        private readonly ITouchSensor _touch; private readonly ISmellSensor _smell;
        private readonly ITasteSensor _taste; private readonly IProprioceptionSensor _proprioception;
        private readonly IPlayerSensoryFeedback _feedback;

        public PlayerSensoryPresentation(Actor actor, IPlayerSensoryFeedback feedback)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            if (!actor.Sensors.TryGet(out _vision) || !actor.Sensors.TryGet(out _hearing) ||
                !actor.Sensors.TryGet(out _touch) || !actor.Sensors.TryGet(out _smell) ||
                !actor.Sensors.TryGet(out _taste) || !actor.Sensors.TryGet(out _proprioception))
                throw new InvalidOperationException("Player sensory presentation requires all six Actor sensors.");
            _vision.PerceptionUpdated += OnVision; _hearing.SoundHeard += OnHearing; _touch.ContactSensed += OnTouch;
            _smell.PerceptionUpdated += OnSmell; _taste.TasteSensed += OnTaste; _proprioception.Sampled += OnProprioception;
        }

        public void Dispose()
        {
            _vision.PerceptionUpdated -= OnVision; _hearing.SoundHeard -= OnHearing; _touch.ContactSensed -= OnTouch;
            _smell.PerceptionUpdated -= OnSmell; _taste.TasteSensed -= OnTaste; _proprioception.Sampled -= OnProprioception;
        }
        private void OnVision(IReadOnlyList<IVisionTarget> value) => _feedback.PresentVision(value);
        private void OnHearing(HeardSound value) => _feedback.PresentHearing(value);
        private void OnTouch(TouchEvent value) => _feedback.PresentTouch(value);
        private void OnSmell(IReadOnlyList<DetectedScent> value) => _feedback.PresentSmell(value);
        private void OnTaste(TasteStimulus value) => _feedback.PresentTaste(value);
        private void OnProprioception(ProprioceptionSnapshot value) => _feedback.PresentProprioception(value);
    }

    [DisallowMultipleComponent]
    public sealed class PlayerSensoryPresentationSystem : MonoBehaviour
    {
        private PlayerSensoryPresentation _presentation;
        public void Initialize(Actor actor, IPlayerSensoryFeedback feedback)
        {
            if (_presentation != null) throw new InvalidOperationException("Player sensory presentation is already initialized.");
            _presentation = new PlayerSensoryPresentation(actor, feedback);
        }
        private void OnDestroy() { _presentation?.Dispose(); _presentation = null; }
    }
}
