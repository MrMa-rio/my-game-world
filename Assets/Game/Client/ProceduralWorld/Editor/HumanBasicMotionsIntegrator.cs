using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld.Editor
{
    public static class HumanBasicMotionsIntegrator
    {
        public const string ControllerPath = "Assets/Game/Content/AvatarValidation/HumanBasicMotions.controller";
        private const string AvatarPartsRoot = "Assets/ia assets/avatar-reference/system-g6/normalized/parts";
        private const string MotionsRoot = "Assets/Kevin Iglesias/Human Animations/Animations/Male";

        [MenuItem("My Game World/Integrate Human Basic Motions")]
        public static void Integrate()
        {
            ConfigureProceduralAvatarRigs();
            CreateController();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ProceduralWorldPlayerIntegrator.Integrate();
            Debug.Log("[AvatarAnimation] Human Basic Motions integrated with procedural humanoid avatars.");
        }

        public static void IntegrateFromCommandLine()
        {
            try
            {
                Integrate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ConfigureProceduralAvatarRigs()
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { AvatarPartsRoot });
            if (modelGuids.Length == 0) throw new InvalidOperationException("System G6 avatar models were not found.");
            for (int index = 0; index < modelGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(modelGuids[index]);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                if (importer.animationType == ModelImporterAnimationType.Human &&
                    importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel) continue;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreateController()
        {
            AnimationClip idle = LoadClip($"{MotionsRoot}/Idles/HumanM@Idle01.fbx");
            AnimationClip walk = LoadClip($"{MotionsRoot}/Movement/Walk/HumanM@Walk01_Forward.fbx");
            AnimationClip run = LoadClip($"{MotionsRoot}/Movement/Run/HumanM@Run01_Forward.fbx");
            AnimationClip jump = LoadClip($"{MotionsRoot}/Movement/Jump/HumanM@Jump01 - Begin.fbx");
            AnimationClip fall = LoadClip($"{MotionsRoot}/Movement/Jump/HumanM@Fall01.fbx");
            AnimationClip land = LoadClip($"{MotionsRoot}/Movement/Jump/HumanM@Jump01 - Land.fbx");

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("MovementState", AnimatorControllerParameterType.Int);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Slope", AnimatorControllerParameterType.Float);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState locomotion = machine.AddState("Locomotion");
            BlendTree blendTree = new BlendTree
            {
                name = "Idle Walk Run",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            blendTree.AddChild(idle, 0f);
            blendTree.AddChild(walk, 4f);
            blendTree.AddChild(run, 7f);
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            locomotion.motion = blendTree;
            machine.defaultState = locomotion;

            AnimatorState jumpState = AddState(machine, "Jump", jump);
            AnimatorState fallState = AddState(machine, "Fall", fall);
            AnimatorState landState = AddState(machine, "Land", land);
            AddAnyStateTransition(machine, jumpState, 3);
            AddAnyStateTransition(machine, fallState, 4);
            AddAnyStateTransition(machine, landState, 5);
            AddMovementTransition(jumpState, fallState, 4, 0.08f);
            AddMovementTransition(jumpState, locomotion, 0, 0.12f, true);
            AddMovementTransition(fallState, landState, 5, 0.08f);
            AddMovementTransition(fallState, locomotion, 0, 0.12f, true);
            AnimatorStateTransition landToLocomotion = landState.AddTransition(locomotion);
            landToLocomotion.hasExitTime = true;
            landToLocomotion.exitTime = 0.72f;
            landToLocomotion.duration = 0.16f;
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion)
        {
            AnimatorState state = machine.AddState(name);
            state.motion = motion;
            state.writeDefaultValues = true;
            return state;
        }

        private static void AddAnyStateTransition(AnimatorStateMachine machine, AnimatorState target, int movementState)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(target);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.Equals, movementState, "MovementState");
        }

        private static void AddMovementTransition(AnimatorState source, AnimatorState target, int movementState,
            float duration, bool requireGrounded = false)
        {
            AnimatorStateTransition transition = source.AddTransition(target);
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.AddCondition(AnimatorConditionMode.Equals, movementState, "MovementState");
            if (requireGrounded) transition.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        }

        private static AnimationClip LoadClip(string modelPath)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    return clip;
            }
            throw new InvalidOperationException($"Animation clip was not found at {modelPath}.");
        }
    }
}
