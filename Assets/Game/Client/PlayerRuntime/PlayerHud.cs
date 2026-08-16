using System;
using MyGameWorld.Client.ActorRuntime;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    public readonly struct PlayerHudState
    {
        public PlayerHudState(bool crosshairVisible, string interactionPrompt, bool movementDebugVisible,
            ProprioceptiveMovementState movementState, PlayerCameraModeId? cameraMode)
        {
            CrosshairVisible = crosshairVisible; InteractionPrompt = interactionPrompt ?? string.Empty;
            MovementDebugVisible = movementDebugVisible; MovementState = movementState; CameraMode = cameraMode;
        }
        public bool CrosshairVisible { get; }
        public string InteractionPrompt { get; }
        public bool MovementDebugVisible { get; }
        public ProprioceptiveMovementState MovementState { get; }
        public PlayerCameraModeId? CameraMode { get; }
    }

    public interface IPlayerHudView
    {
        void Render(in PlayerHudState state);
    }

    public sealed class PlayerHudPresenter : IDisposable
    {
        private readonly IProprioceptionSensor _proprioception;
        private readonly PlayerCameraModeController _cameraModes;
        private readonly IPlayerHudView _view;
        private bool _crosshairVisible = true;
        private bool _movementDebugVisible;
        private string _interactionPrompt = string.Empty;
        private ProprioceptiveMovementState _movementState;
        private PlayerCameraModeId? _cameraMode;

        public PlayerHudPresenter(IProprioceptionSensor proprioception, PlayerCameraModeController cameraModes,
            IPlayerHudView view)
        {
            _proprioception = proprioception ?? throw new ArgumentNullException(nameof(proprioception));
            _cameraModes = cameraModes ?? throw new ArgumentNullException(nameof(cameraModes));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _proprioception.Sampled += OnProprioceptionSampled;
            _cameraModes.ModeChanged += OnCameraModeChanged;
            if (_cameraModes.ActiveMode != null) _cameraMode = _cameraModes.ActiveMode.Id;
            Publish();
        }

        public void SetInteractionPrompt(string prompt) { _interactionPrompt = prompt ?? string.Empty; Publish(); }
        public void SetCrosshairVisible(bool visible) { _crosshairVisible = visible; Publish(); }
        public void SetMovementDebugVisible(bool visible) { _movementDebugVisible = visible; Publish(); }
        public void Dispose()
        {
            _proprioception.Sampled -= OnProprioceptionSampled;
            _cameraModes.ModeChanged -= OnCameraModeChanged;
        }
        private void OnProprioceptionSampled(ProprioceptionSnapshot snapshot) { _movementState = snapshot.MovementState; Publish(); }
        private void OnCameraModeChanged(PlayerCameraModeId mode) { _cameraMode = mode; Publish(); }
        private void Publish()
        {
            PlayerHudState state = new PlayerHudState(_crosshairVisible, _interactionPrompt, _movementDebugVisible,
                _movementState, _cameraMode); _view.Render(in state);
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerHudView : MonoBehaviour, IPlayerHudView
    {
        private PlayerHudState _state;
        private GUIContent _prompt = new GUIContent();
        private GUIContent _movement = new GUIContent();
        private GUIContent _cameraMode = new GUIContent();
        public void Render(in PlayerHudState state)
        {
            _state = state; _prompt.text = state.InteractionPrompt;
            _movement.text = $"Movement: {state.MovementState}";
            _cameraMode.text = state.CameraMode.HasValue ? $"Camera: {state.CameraMode.Value}" : string.Empty;
        }
        private void OnGUI()
        {
            if (_state.CrosshairVisible) GUI.Label(new Rect(Screen.width * 0.5f - 8f, Screen.height * 0.5f - 12f, 16f, 24f), "+");
            if (!string.IsNullOrEmpty(_prompt.text)) GUI.Label(new Rect(Screen.width * 0.5f - 160f, Screen.height * 0.65f, 320f, 28f), _prompt);
            if (_state.MovementDebugVisible) GUI.Label(new Rect(12f, 12f, 240f, 24f), _movement);
            if (!string.IsNullOrEmpty(_cameraMode.text)) GUI.Label(new Rect(12f, 38f, 240f, 24f), _cameraMode);
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerHudSystem : MonoBehaviour
    {
        private PlayerHudPresenter _presenter;
        public PlayerHudPresenter Presenter => _presenter;
        public void Initialize(Actor actor, PlayerCameraSystem cameraSystem, IPlayerHudView view)
        {
            if (_presenter != null) throw new InvalidOperationException("Player HUD is already initialized.");
            if (actor == null || !actor.Sensors.TryGet(out IProprioceptionSensor proprioception))
                throw new InvalidOperationException("Player HUD requires an Actor with proprioception.");
            if (cameraSystem == null || !cameraSystem.IsInitialized) throw new InvalidOperationException("Player HUD requires an initialized camera system.");
            _presenter = new PlayerHudPresenter(proprioception, cameraSystem.Modes, view);
        }
        private void OnDestroy() { _presenter?.Dispose(); _presenter = null; }
    }
}
