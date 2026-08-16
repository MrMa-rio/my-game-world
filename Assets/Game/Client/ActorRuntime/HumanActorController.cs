using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MyGameWorld.Client.ActorRuntime
{
    public readonly struct HumanInputSnapshot
    {
        public HumanInputSnapshot(Vector2 move, Vector2 look, bool run, bool jumpPressed, bool interactPressed, bool changeCameraPressed = false)
        { Move = move; Look = look; Run = run; JumpPressed = jumpPressed; InteractPressed = interactPressed; ChangeCameraPressed = changeCameraPressed; }
        public Vector2 Move { get; }
        public Vector2 Look { get; }
        public bool Run { get; }
        public bool JumpPressed { get; }
        public bool InteractPressed { get; }
        public bool ChangeCameraPressed { get; }
    }

    [DisallowMultipleComponent]
    public sealed class HumanActorController : ActorController
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _moveActionName = "Move";
        [SerializeField] private string _lookActionName = "Look";
        [SerializeField] private string _runActionName = "Sprint";
        [SerializeField] private string _jumpActionName = "Jump";
        [SerializeField] private string _interactActionName = "Interact";
        [SerializeField] private string _changeCameraActionName = "ChangeCamera";

        private InputActionAsset _runtimeActions;
        private InputActionMap _actionMap;
        private InputAction _move;
        private InputAction _look;
        private InputAction _run;
        private InputAction _jump;
        private InputAction _interact;
        private InputAction _changeCamera;
        private ulong _sequence;

        public void Configure(InputActionAsset inputActions, string actionMapName = "Player")
        {
            if (IsBound) throw new InvalidOperationException("Input configuration cannot change while the controller is bound.");
            _inputActions = inputActions != null ? inputActions : throw new ArgumentNullException(nameof(inputActions));
            _actionMapName = string.IsNullOrWhiteSpace(actionMapName) ? throw new ArgumentException("Action map name is required.") : actionMapName;
        }

        protected override void OnBound()
        {
            if (_inputActions == null) throw new InvalidOperationException("Human controller requires an InputActionAsset.");
            _runtimeActions = Instantiate(_inputActions);
            _actionMap = _runtimeActions.FindActionMap(_actionMapName, true);
            _move = _actionMap.FindAction(_moveActionName, true);
            _look = _actionMap.FindAction(_lookActionName, true);
            _run = _actionMap.FindAction(_runActionName, true);
            _jump = _actionMap.FindAction(_jumpActionName, true);
            _interact = _actionMap.FindAction(_interactActionName, true);
            _changeCamera = _actionMap.FindAction(_changeCameraActionName, false);
            _actionMap.Enable();
        }

        protected override void OnUnbinding()
        {
            if (_actionMap != null) _actionMap.Disable();
            _actionMap = null; _move = null; _look = null; _run = null; _jump = null; _interact = null; _changeCamera = null;
            if (_runtimeActions != null)
            {
                if (Application.isPlaying) Destroy(_runtimeActions); else DestroyImmediate(_runtimeActions);
                _runtimeActions = null;
            }
        }

        private void Update()
        {
            if (!IsBound || _actionMap == null || !_actionMap.enabled) return;
            ProcessInput(new HumanInputSnapshot(_move.ReadValue<Vector2>(), _look.ReadValue<Vector2>(), _run.IsPressed(),
                _jump.WasPressedThisFrame(), _interact.WasPressedThisFrame(), _changeCamera != null && _changeCamera.WasPressedThisFrame()));
        }

        public void ProcessInput(in HumanInputSnapshot input)
        {
            if (!IsBound) throw new InvalidOperationException("Human controller must be bound before processing input.");
            MoveIntent move = new MoveIntent(NextSequence(), input.Move); Context.Actor.Intents.Submit(in move);
            RunIntent run = new RunIntent(NextSequence(), input.Run); Context.Actor.Intents.Submit(in run);
            if (input.Look.sqrMagnitude > 0f)
            {
                LookIntent look = new LookIntent(NextSequence(), input.Look); Context.Actor.Intents.Submit(in look);
            }
            if (input.JumpPressed)
            {
                JumpIntent jump = new JumpIntent(NextSequence()); Context.Actor.Intents.Submit(in jump);
            }
            if (input.InteractPressed)
            {
                InteractIntent interact = new InteractIntent(NextSequence()); Context.Actor.Intents.Submit(in interact);
            }
            if (input.ChangeCameraPressed)
            {
                ChangeCameraIntent changeCamera = new ChangeCameraIntent(NextSequence()); Context.Actor.Intents.Submit(in changeCamera);
            }
        }

        private ulong NextSequence() => ++_sequence;
    }
}
