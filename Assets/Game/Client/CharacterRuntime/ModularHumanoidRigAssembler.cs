using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    public readonly struct ModularRigAssemblyResult
    {
        public ModularRigAssemblyResult(Animator animator, int reboundRenderers, int mappedBones, int disabledAnimators)
        { Animator = animator; ReboundRenderers = reboundRenderers; MappedBones = mappedBones; DisabledAnimators = disabledAnimators; }
        public Animator Animator { get; }
        public int ReboundRenderers { get; }
        public int MappedBones { get; }
        public int DisabledAnimators { get; }
        public bool IsValid => Animator != null;
    }

    public static class ModularHumanoidRigAssembler
    {
        public static ModularRigAssemblyResult Consolidate(RuntimeAvatar avatar)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            Animator[] candidates = avatar.GetComponentsInChildren<Animator>(true);
            Animator canonical = SelectCanonicalAnimator(candidates);
            if (canonical == null) return default;

            Dictionary<string, Transform> canonicalBones = BuildCanonicalBoneMap(canonical.transform);
            SkinnedMeshRenderer[] renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int reboundRenderers = 0;
            int mappedBones = 0;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                SkinnedMeshRenderer renderer = renderers[rendererIndex];
                Transform[] sourceBones = renderer.bones;
                if (sourceBones == null || sourceBones.Length == 0) continue;
                Transform[] sharedBones = new Transform[sourceBones.Length];
                int rendererMappedBones = 0;
                for (int boneIndex = 0; boneIndex < sourceBones.Length; boneIndex++)
                {
                    Transform source = sourceBones[boneIndex];
                    if (source != null && canonicalBones.TryGetValue(Normalize(source.name), out Transform shared))
                    {
                        sharedBones[boneIndex] = shared;
                        rendererMappedBones++;
                    }
                    else
                    {
                        sharedBones[boneIndex] = source;
                    }
                }

                // Only switch a renderer when almost its complete skeleton can be resolved.
                // This prevents partially compatible accessories from becoming distorted.
                if (rendererMappedBones < Mathf.CeilToInt(sourceBones.Length * 0.85f)) continue;
                renderer.bones = sharedBones;
                if (renderer.rootBone != null && canonicalBones.TryGetValue(Normalize(renderer.rootBone.name), out Transform sharedRoot))
                    renderer.rootBone = sharedRoot;
                rendererMappedBones = Mathf.Min(rendererMappedBones, sourceBones.Length);
                mappedBones += rendererMappedBones;
                reboundRenderers++;
            }

            int disabledAnimators = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                Animator candidate = candidates[index];
                if (candidate == null || candidate == canonical) continue;
                candidate.enabled = false;
                candidate.runtimeAnimatorController = null;
                disabledAnimators++;
            }
            return new ModularRigAssemblyResult(canonical, reboundRenderers, mappedBones, disabledAnimators);
        }

        private static Animator SelectCanonicalAnimator(Animator[] candidates)
        {
            Animator best = null;
            int bestScore = int.MinValue;
            for (int index = 0; index < candidates.Length; index++)
            {
                Animator candidate = candidates[index];
                if (candidate == null || candidate.avatar == null || !candidate.avatar.isValid || !candidate.avatar.isHuman) continue;
                int score = candidate.GetComponentsInChildren<Transform>(true).Length;
                string name = candidate.transform.name.ToLowerInvariant();
                if (name.Contains("body")) score += 10000;
                if (name.Contains("armor")) score -= 1000;
                if (score > bestScore) { best = candidate; bestScore = score; }
            }
            return best;
        }

        private static Dictionary<string, Transform> BuildCanonicalBoneMap(Transform root)
        {
            Dictionary<string, Transform> bones = new Dictionary<string, Transform>(StringComparer.Ordinal);
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                string key = Normalize(transforms[index].name);
                if (!bones.ContainsKey(key)) bones.Add(key, transforms[index]);
            }
            return bones;
        }

        private static string Normalize(string value)
            => value.ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty);
    }
}
