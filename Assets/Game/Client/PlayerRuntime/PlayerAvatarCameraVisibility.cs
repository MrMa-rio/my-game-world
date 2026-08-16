using System.Collections.Generic;
using MyGameWorld.Client.CharacterRuntime;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGameWorld.Client.PlayerRuntime
{
    [DisallowMultipleComponent]
    public sealed class PlayerAvatarCameraVisibility : MonoBehaviour
    {
        private readonly List<RendererState> _renderers = new List<RendererState>();
        private PlayerCameraSystem _cameraSystem;

        public void Initialize(RuntimeAvatar avatar, PlayerCameraSystem cameraSystem)
        {
            _cameraSystem = cameraSystem;
            Renderer[] renderers = avatar.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                _renderers.Add(new RendererState(renderers[index], renderers[index].shadowCastingMode));
            }

            _cameraSystem.Modes.ModeChanged += OnModeChanged;
            if (_cameraSystem.Modes.ActiveMode != null)
            {
                OnModeChanged(_cameraSystem.Modes.ActiveMode.Id);
            }
        }

        private void OnModeChanged(PlayerCameraModeId mode)
        {
            bool firstPerson = mode == PlayerCameraModeId.FirstPerson;
            for (int index = 0; index < _renderers.Count; index++)
            {
                RendererState state = _renderers[index];
                if (state.Renderer != null)
                {
                    state.Renderer.shadowCastingMode = firstPerson ? ShadowCastingMode.ShadowsOnly : state.OriginalMode;
                }
            }
        }

        private void OnDestroy()
        {
            if (_cameraSystem?.Modes != null)
            {
                _cameraSystem.Modes.ModeChanged -= OnModeChanged;
            }
        }

        private readonly struct RendererState
        {
            public RendererState(Renderer renderer, ShadowCastingMode originalMode)
            {
                Renderer = renderer;
                OriginalMode = originalMode;
            }

            public Renderer Renderer { get; }
            public ShadowCastingMode OriginalMode { get; }
        }
    }
}
