using System;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    public static class AvatarMorphologyApplier
    {
        public static void Apply(Animator animator, AvatarStyleRecipe style)
        {
            if (animator == null) throw new ArgumentNullException(nameof(animator));
            if (!animator.isHuman) return;
            ScaleUniform(animator.GetBoneTransform(HumanBodyBones.Head), style.HeadScale);
            ScaleWidth(animator.GetBoneTransform(HumanBodyBones.Chest), style.TorsoWidth);
            ScaleWidth(animator.GetBoneTransform(HumanBodyBones.UpperChest), style.TorsoWidth);
            ScaleWidth(animator.GetBoneTransform(HumanBodyBones.Hips), style.HipWidth);
        }

        private static void ScaleUniform(Transform bone, float scale)
        {
            if (bone != null) bone.localScale = Vector3.Scale(bone.localScale, Vector3.one * scale);
        }

        private static void ScaleWidth(Transform bone, float width)
        {
            if (bone != null) bone.localScale = Vector3.Scale(bone.localScale, new Vector3(width, 1f, width));
        }
    }
}
