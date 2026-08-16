using System;
using System.Collections.Generic;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.Procedural;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    [DisallowMultipleComponent]
    public sealed class ProceduralAvatarAnimation : MonoBehaviour, IActorAnimationSink
    {
        private const float WalkReferenceSpeed = 4f;
        private const float RunReferenceSpeed = 7f;
        private readonly List<BonePose> _bones = new List<BonePose>();
        private ActorAnimationState _state;
        private Vector3 _bindPosition;
        private Quaternion _bindRotation;
        private float _gaitPhase;
        private float _smoothedSpeed;
        private float _movementWeight;
        private float _runWeight;
        private bool _hasState;

        public int AnimatedBoneCount => _bones.Count;
        public int ArmatureCount { get; private set; }
        public float GaitPhase => _gaitPhase;

        public void Initialize(RuntimeAvatar avatar)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            _bindPosition = avatar.transform.localPosition;
            _bindRotation = avatar.transform.localRotation;
            _bones.Clear();
            ArmatureCount = 0;
            SkinnedMeshRenderer[] renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Dictionary<Transform, Dictionary<AvatarBoneRole, BoneCandidate>> armatures =
                new Dictionary<Transform, Dictionary<AvatarBoneRole, BoneCandidate>>();
            HashSet<Transform> visitedBones = new HashSet<Transform>();
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Transform[] rendererBones = renderers[rendererIndex].bones;
                for (int boneIndex = 0; boneIndex < rendererBones.Length; boneIndex++)
                {
                    Transform bone = rendererBones[boneIndex];
                    if (bone == null || !visitedBones.Add(bone)) continue;
                    AddCandidate(avatar.transform, bone, armatures);
                }
            }

            // The technical fallback uses rigid transforms rather than skinned meshes.
            if (armatures.Count == 0)
            {
                Transform[] transforms = avatar.GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
                    AddCandidate(avatar.transform, transforms[index], armatures);
                }
            }

            foreach (KeyValuePair<Transform, Dictionary<AvatarBoneRole, BoneCandidate>> armature in armatures)
            {
                ArmatureCount++;
                foreach (KeyValuePair<AvatarBoneRole, BoneCandidate> candidate in armature.Value)
                {
                    _bones.Add(new BonePose(candidate.Value.Transform, candidate.Key));
                }
            }
        }

        public void Apply(in ActorAnimationState state)
        {
            _state = state;
            _hasState = true;
        }

        private void LateUpdate()
        {
            if (!_hasState) return;
            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            _smoothedSpeed = ProceduralGaitMath.Damp(_smoothedSpeed, _state.Grounded ? _state.PlanarSpeed : 0f, 9f, deltaTime);
            _movementWeight = ProceduralGaitMath.Damp(_movementWeight,
                _state.Grounded && _state.PlanarSpeed > 0.08f ? 1f : 0f, 6f, deltaTime);
            _runWeight = ProceduralGaitMath.Damp(_runWeight,
                _state.Movement == ActorAnimationMovementState.Run ? 1f : 0f, 4.5f, deltaTime);

            float normalizedSpeed = Mathf.Lerp(Mathf.Clamp01(_smoothedSpeed / WalkReferenceSpeed),
                Mathf.Clamp01(_smoothedSpeed / RunReferenceSpeed), _runWeight);
            _gaitPhase = Mathf.Repeat(_gaitPhase +
                ProceduralGaitMath.ResolveCycleFrequency(normalizedSpeed, _runWeight) * deltaTime * _movementWeight, 1f);

            ProceduralGaitPose pose = ProceduralGaitMath.Evaluate(_gaitPhase, normalizedSpeed, _runWeight);
            float poseBlend = 1f - Mathf.Exp(-8f * deltaTime);
            ApplyRootPose(pose, poseBlend);
            for (int index = 0; index < _bones.Count; index++)
            {
                BonePose bone = _bones[index];
                if (bone.Transform == null) continue;
                Quaternion target = bone.BindRotation * ResolveOffset(bone.Role, pose);
                bone.Transform.localRotation = Quaternion.Slerp(bone.Transform.localRotation, target, poseBlend);
            }
        }

        private void ApplyRootPose(ProceduralGaitPose pose, float blend)
        {
            float airborne = _state.Grounded ? 0f : 1f;
            float jumpLean = _state.Movement == ActorAnimationMovementState.Jump ? -4f :
                _state.Movement == ActorAnimationMovementState.Fall ? 4f : 0f;
            Vector3 gaitOffset = new Vector3(pose.PelvisLateral, pose.PelvisHeight, 0f) * _movementWeight;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _bindPosition + gaitOffset, blend);
            Quaternion gaitRotation = Quaternion.Euler(Mathf.Lerp(pose.BodyLean, jumpLean, airborne),
                pose.TorsoYaw * _movementWeight, pose.PelvisRoll * _movementWeight);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _bindRotation * gaitRotation, blend);
        }

        private Quaternion ResolveOffset(AvatarBoneRole role, ProceduralGaitPose pose)
        {
            if (!_state.Grounded) return ResolveAirborneOffset(role);
            float weight = _movementWeight;
            switch (role)
            {
                case AvatarBoneRole.LeftUpperArm: return Quaternion.Euler(pose.LeftShoulder * weight, 0f, -pose.ShoulderAbduction * weight);
                case AvatarBoneRole.RightUpperArm: return Quaternion.Euler(pose.RightShoulder * weight, 0f, pose.ShoulderAbduction * weight);
                case AvatarBoneRole.LeftForearm: return Quaternion.Euler(-pose.LeftElbow * weight, 0f, 0f);
                case AvatarBoneRole.RightForearm: return Quaternion.Euler(-pose.RightElbow * weight, 0f, 0f);
                case AvatarBoneRole.LeftThigh: return Quaternion.Euler(pose.LeftHip * weight, 0f, 0f);
                case AvatarBoneRole.RightThigh: return Quaternion.Euler(pose.RightHip * weight, 0f, 0f);
                case AvatarBoneRole.LeftCalf: return Quaternion.Euler(-pose.LeftKnee * weight, 0f, 0f);
                case AvatarBoneRole.RightCalf: return Quaternion.Euler(-pose.RightKnee * weight, 0f, 0f);
                case AvatarBoneRole.LeftFoot: return Quaternion.Euler(pose.LeftAnkle * weight, 0f, 0f);
                case AvatarBoneRole.RightFoot: return Quaternion.Euler(pose.RightAnkle * weight, 0f, 0f);
                case AvatarBoneRole.Pelvis: return Quaternion.Euler(0f, -pose.TorsoYaw * 0.55f * weight, pose.PelvisRoll * weight);
                case AvatarBoneRole.Spine: return Quaternion.Euler(0f, pose.TorsoYaw * weight, -pose.PelvisRoll * 0.45f * weight);
                case AvatarBoneRole.Head: return Quaternion.Euler(0f, -pose.TorsoYaw * 0.45f * weight, 0f);
                default: return Quaternion.identity;
            }
        }

        private Quaternion ResolveAirborneOffset(AvatarBoneRole role)
        {
            bool rising = _state.Movement == ActorAnimationMovementState.Jump;
            switch (role)
            {
                case AvatarBoneRole.LeftUpperArm: return Quaternion.Euler(rising ? -22f : 2f, 0f, -18f);
                case AvatarBoneRole.RightUpperArm: return Quaternion.Euler(rising ? -22f : 2f, 0f, 18f);
                case AvatarBoneRole.LeftForearm:
                case AvatarBoneRole.RightForearm: return Quaternion.Euler(-32f, 0f, 0f);
                case AvatarBoneRole.LeftThigh: return Quaternion.Euler(rising ? 24f : 10f, 0f, 0f);
                case AvatarBoneRole.RightThigh: return Quaternion.Euler(rising ? 10f : 20f, 0f, 0f);
                case AvatarBoneRole.LeftCalf: return Quaternion.Euler(-38f, 0f, 0f);
                case AvatarBoneRole.RightCalf: return Quaternion.Euler(-28f, 0f, 0f);
                default: return Quaternion.identity;
            }
        }

        private static AvatarBoneRole ResolveRole(string source)
        {
            string name = source.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
            bool left = name.Contains("left") || name.EndsWith(".l") || name.EndsWith("l");
            bool right = name.Contains("right") || name.EndsWith(".r") || name.EndsWith("r");
            if (name.Contains("forearm") || name.Contains("lowerarm")) return left ? AvatarBoneRole.LeftForearm : right ? AvatarBoneRole.RightForearm : AvatarBoneRole.None;
            if (name.Contains("upperarm")) return left ? AvatarBoneRole.LeftUpperArm : right ? AvatarBoneRole.RightUpperArm : AvatarBoneRole.None;
            if (name.Contains("thigh") || name.Contains("upperleg")) return left ? AvatarBoneRole.LeftThigh : right ? AvatarBoneRole.RightThigh : AvatarBoneRole.None;
            if (name.Contains("calf") || name.Contains("lowerleg") || name.Contains("shin")) return left ? AvatarBoneRole.LeftCalf : right ? AvatarBoneRole.RightCalf : AvatarBoneRole.None;
            if (name.Contains("foot") || name.Contains("ankle")) return left ? AvatarBoneRole.LeftFoot : right ? AvatarBoneRole.RightFoot : AvatarBoneRole.None;
            if (name.Contains("pelvis") || name.Contains("hips")) return AvatarBoneRole.Pelvis;
            if (name.Contains("spine") || name.Contains("chest")) return AvatarBoneRole.Spine;
            if (name == "head" || name.EndsWith("head")) return AvatarBoneRole.Head;
            return AvatarBoneRole.None;
        }

        private static void AddCandidate(Transform avatarRoot, Transform bone,
            Dictionary<Transform, Dictionary<AvatarBoneRole, BoneCandidate>> armatures)
        {
            AvatarBoneRole role = ResolveRole(bone.name);
            if (role == AvatarBoneRole.None) return;
            Transform armatureRoot = FindArmatureRoot(avatarRoot, bone);
            if (!armatures.TryGetValue(armatureRoot, out Dictionary<AvatarBoneRole, BoneCandidate> roles))
            {
                roles = new Dictionary<AvatarBoneRole, BoneCandidate>();
                armatures.Add(armatureRoot, roles);
            }

            int score = ResolveCandidateScore(bone, role);
            if (!roles.TryGetValue(role, out BoneCandidate existing) || score > existing.Score)
            {
                roles[role] = new BoneCandidate(bone, score);
            }
        }

        private static Transform FindArmatureRoot(Transform avatarRoot, Transform bone)
        {
            Transform current = bone;
            while (current.parent != null && current.parent != avatarRoot)
            {
                current = current.parent;
            }
            return current;
        }

        private static int ResolveCandidateScore(Transform bone, AvatarBoneRole role)
        {
            int depth = 0;
            for (Transform current = bone; current.parent != null; current = current.parent) depth++;
            if (role != AvatarBoneRole.Spine) return 1000 - depth;
            string name = bone.name.ToLowerInvariant();
            if (name.Contains("chest")) return 3000 + depth;
            if (name.Contains("spine.002") || name.Contains("spine2")) return 2500 + depth;
            if (name.Contains("spine.001") || name.Contains("spine1")) return 2000 + depth;
            return 1000 + depth;
        }

        private enum AvatarBoneRole : byte
        {
            None, Pelvis, LeftUpperArm, RightUpperArm, LeftForearm, RightForearm,
            LeftThigh, RightThigh, LeftCalf, RightCalf, LeftFoot, RightFoot, Spine, Head
        }

        private readonly struct BonePose
        {
            public BonePose(Transform transform, AvatarBoneRole role)
            { Transform = transform; Role = role; BindRotation = transform.localRotation; }
            public Transform Transform { get; }
            public AvatarBoneRole Role { get; }
            public Quaternion BindRotation { get; }
        }

        private readonly struct BoneCandidate
        {
            public BoneCandidate(Transform transform, int score)
            { Transform = transform; Score = score; }
            public Transform Transform { get; }
            public int Score { get; }
        }
    }

    public static class ProceduralAvatarFallback
    {
        public static RuntimeAvatar Create(Transform parent, long seed)
        {
            GameObject root = new GameObject("Procedural Avatar Fallback");
            root.transform.SetParent(parent, false);
            RuntimeAvatar avatar = root.AddComponent<RuntimeAvatar>();
            avatar.Initialize(new CharacterAppearanceDNA(seed, CharacterAppearanceGenerator.Version, new AssetCatalogVersion(1),
                AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame,
                Array.Empty<CharacterPartSelection>(), 8, 8, 8),
                AvatarEnvironmentalStyleResolver.Resolve(seed, default));
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.22f, 0.58f, 0.76f);
            CreatePart(root.transform, "Spine", PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f), new Vector3(0.62f, 0.72f, 0.34f), material);
            CreatePart(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.82f, 0f), new Vector3(0.42f, 0.46f, 0.4f), material);
            CreatePart(root.transform, "UpperArm.L", PrimitiveType.Cube, new Vector3(-0.42f, 1.42f, 0f), new Vector3(0.2f, 0.58f, 0.2f), material);
            CreatePart(root.transform, "UpperArm.R", PrimitiveType.Cube, new Vector3(0.42f, 1.42f, 0f), new Vector3(0.2f, 0.58f, 0.2f), material);
            CreatePart(root.transform, "Thigh.L", PrimitiveType.Cube, new Vector3(-0.18f, 0.56f, 0f), new Vector3(0.24f, 0.78f, 0.26f), material);
            CreatePart(root.transform, "Thigh.R", PrimitiveType.Cube, new Vector3(0.18f, 0.56f, 0f), new Vector3(0.24f, 0.78f, 0.26f), material);
            return avatar;
        }

        private static void CreatePart(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            UnityEngine.Object.Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
