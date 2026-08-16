using System;
using System.Collections.Generic;
using MyGameWorld.Client.ActorRuntime;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    [DisallowMultipleComponent]
    public sealed class HumanoidMotionAnimation : MonoBehaviour, IActorAnimationSink
    {
        private readonly List<Animator> _animators = new List<Animator>();
        private ActorAnimationState _pendingState;
        private bool _hasState;

        public int AnimatorCount => _animators.Count;
        public int ReboundRendererCount { get; private set; }
        public int MappedBoneCount { get; private set; }
        public int DisabledDuplicateAnimatorCount { get; private set; }
        public bool IsOperational => _animators.Count > 0;

        public bool Initialize(RuntimeAvatar avatar, RuntimeAnimatorController controller)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            if (controller == null) return false;
            _animators.Clear();
            ModularRigAssemblyResult rig = ModularHumanoidRigAssembler.Consolidate(avatar);
            if (!rig.IsValid) return false;
            ReboundRendererCount = rig.ReboundRenderers;
            MappedBoneCount = rig.MappedBones;
            DisabledDuplicateAnimatorCount = rig.DisabledAnimators;
            Animator animator = rig.Animator;
            AvatarMorphologyApplier.Apply(animator, avatar.Style);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.enabled = true;
            _animators.Add(animator);
            return IsOperational;
        }

        public void Apply(in ActorAnimationState state)
        {
            _pendingState = state;
            _hasState = true;
        }

        private void Update()
        {
            if (!_hasState) return;
            for (int index = 0; index < _animators.Count; index++)
            {
                Animator animator = _animators[index];
                if (animator == null) continue;
                animator.SetInteger(MovementStateHash, (int)_pendingState.Movement);
                animator.SetFloat(SpeedHash, _pendingState.PlanarSpeed, 0.14f, Time.deltaTime);
                animator.SetFloat(VerticalSpeedHash, _pendingState.VerticalSpeed, 0.1f, Time.deltaTime);
                animator.SetBool(GroundedHash, _pendingState.Grounded);
                animator.SetFloat(SlopeHash, _pendingState.Slope, 0.18f, Time.deltaTime);
            }
        }

        private static readonly int MovementStateHash = Animator.StringToHash("MovementState");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int SlopeHash = Animator.StringToHash("Slope");
    }
}
