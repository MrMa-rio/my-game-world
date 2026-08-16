using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class DevelopmentFreeCamera : MonoBehaviour
    {
        [SerializeField, Min(0.1f)]
        private float _movementSpeed = 55f;

        [SerializeField, Min(1f)]
        private float _fastMultiplier = 3.5f;

        [SerializeField, Min(0.01f)]
        private float _lookSensitivity = 0.12f;

        [SerializeField, Min(0.1f)]
        private float _scrollSpeedStep = 5f;

        private ProceduralWorldSandbox _sandbox;
        private float _pitch;
        private float _yaw;

        public float MovementSpeed => _movementSpeed;

        private void Start()
        {
            Vector3 angles = transform.eulerAngles;
            _pitch = NormalizeAngle(angles.x);
            _yaw = angles.y;
            _sandbox = FindAnyObjectByType<ProceduralWorldSandbox>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null)
            {
                return;
            }

            Vector3 input = Vector3.zero;
            input.z = ReadAxis(keyboard.sKey, keyboard.wKey);
            input.x = ReadAxis(keyboard.aKey, keyboard.dKey);
            input.y = ReadAxis(keyboard.qKey, keyboard.eKey);
            float speed = _movementSpeed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                speed *= _fastMultiplier;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            transform.position += transform.TransformDirection(input) * speed * Time.unscaledDeltaTime;

            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                if (mouse.rightButton.isPressed)
                {
                    Vector2 look = mouse.delta.ReadValue() * _lookSensitivity;
                    _yaw += look.x;
                    _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
                    transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                }

                if (mouse.rightButton.wasReleasedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _movementSpeed = Mathf.Clamp(
                        _movementSpeed + (Mathf.Sign(scroll) * _scrollSpeedStep),
                        1f,
                        300f);
                }
            }

            if (_sandbox == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                _sandbox.ToggleHud();
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                _sandbox.ToggleWireframe();
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                _sandbox.RegenerateSameSeed();
            }

            if (keyboard.f4Key.wasPressedThisFrame)
            {
                _sandbox.GenerateNextSeed();
            }

            if (keyboard.f5Key.wasPressedThisFrame) _sandbox.CycleWindStrength();
            if (keyboard.f6Key.wasPressedThisFrame) _sandbox.CycleEnvironmentalBiome();
            if (keyboard.f7Key.wasPressedThisFrame) _sandbox.CycleVfxDensity();
            if (keyboard.f8Key.wasPressedThisFrame) _sandbox.AdvanceWorldTime();
            if (keyboard.f9Key.wasPressedThisFrame) _sandbox.ToggleWorldTimePause();
            if (keyboard.f10Key.wasPressedThisFrame) _sandbox.SpawnShootingStar();
            if (keyboard.f11Key.wasPressedThisFrame) _sandbox.SpawnMeteor();
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
