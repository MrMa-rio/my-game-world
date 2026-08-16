using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    public readonly struct ProceduralGaitPose
    {
        public ProceduralGaitPose(float leftHip, float rightHip, float leftKnee, float rightKnee,
            float leftAnkle, float rightAnkle, float leftShoulder, float rightShoulder,
            float leftElbow, float rightElbow, float shoulderAbduction, float pelvisHeight,
            float pelvisLateral, float pelvisRoll, float torsoYaw, float bodyLean)
        {
            LeftHip = leftHip; RightHip = rightHip; LeftKnee = leftKnee; RightKnee = rightKnee;
            LeftAnkle = leftAnkle; RightAnkle = rightAnkle; LeftShoulder = leftShoulder;
            RightShoulder = rightShoulder; LeftElbow = leftElbow; RightElbow = rightElbow;
            ShoulderAbduction = shoulderAbduction; PelvisHeight = pelvisHeight;
            PelvisLateral = pelvisLateral; PelvisRoll = pelvisRoll; TorsoYaw = torsoYaw; BodyLean = bodyLean;
        }

        public float LeftHip { get; }
        public float RightHip { get; }
        public float LeftKnee { get; }
        public float RightKnee { get; }
        public float LeftAnkle { get; }
        public float RightAnkle { get; }
        public float LeftShoulder { get; }
        public float RightShoulder { get; }
        public float LeftElbow { get; }
        public float RightElbow { get; }
        public float ShoulderAbduction { get; }
        public float PelvisHeight { get; }
        public float PelvisLateral { get; }
        public float PelvisRoll { get; }
        public float TorsoYaw { get; }
        public float BodyLean { get; }
    }

    public static class ProceduralGaitMath
    {
        public static float ResolveCycleFrequency(float normalizedSpeed, float runWeight)
        {
            float speed = Mathf.Clamp01(normalizedSpeed);
            float walkCycles = Mathf.Lerp(0.72f, 1.25f, Smooth01(speed));
            float runCycles = Mathf.Lerp(1.35f, 2.05f, Smooth01(speed));
            return Mathf.Lerp(walkCycles, runCycles, Mathf.Clamp01(runWeight));
        }

        public static ProceduralGaitPose Evaluate(float phase, float normalizedSpeed, float runWeight)
        {
            float speed = Mathf.Clamp01(normalizedSpeed);
            float run = Mathf.Clamp01(runWeight);
            float amplitude = Mathf.Lerp(0.55f, 1f, speed);
            float leftPhase = Mathf.Repeat(phase, 1f);
            float rightPhase = Mathf.Repeat(phase + 0.5f, 1f);
            float leftHip = HipAngle(leftPhase, run) * amplitude;
            float rightHip = HipAngle(rightPhase, run) * amplitude;
            float leftKnee = KneeFlexion(leftPhase, run) * amplitude;
            float rightKnee = KneeFlexion(rightPhase, run) * amplitude;
            float leftAnkle = AnkleAngle(leftPhase, run) * amplitude;
            float rightAnkle = AnkleAngle(rightPhase, run) * amplitude;

            // Arms counterbalance the opposite leg. Running shortens the arm pendulum
            // with progressive elbow flexion instead of merely increasing swing angle.
            float leftShoulder = -rightHip * Mathf.Lerp(0.72f, 0.88f, run);
            float rightShoulder = -leftHip * Mathf.Lerp(0.72f, 0.88f, run);
            float elbowBase = Mathf.Lerp(6f, 56f, run);
            float leftElbow = elbowBase + PositivePulse(rightPhase, 0.02f, 0.5f) * Mathf.Lerp(4f, 14f, run) * amplitude;
            float rightElbow = elbowBase + PositivePulse(leftPhase, 0.02f, 0.5f) * Mathf.Lerp(4f, 14f, run) * amplitude;
            float doubleStep = Mathf.Cos(phase * Mathf.PI * 4f);
            float singleStep = Mathf.Sin(phase * Mathf.PI * 2f);

            return new ProceduralGaitPose(leftHip, rightHip, leftKnee, rightKnee,
                leftAnkle, rightAnkle, leftShoulder, rightShoulder, leftElbow, rightElbow,
                Mathf.Lerp(1.5f, 4.5f, run),
                -doubleStep * Mathf.Lerp(0.014f, 0.035f, run) * amplitude,
                singleStep * Mathf.Lerp(0.012f, 0.02f, run) * amplitude,
                singleStep * Mathf.Lerp(2.2f, 3.4f, run) * amplitude,
                -singleStep * Mathf.Lerp(3.2f, 5.5f, run) * amplitude,
                Mathf.Lerp(1.5f, 8f, run) * speed);
        }

        public static float Damp(float current, float target, float response, float deltaTime)
            => Mathf.Lerp(current, target, 1f - Mathf.Exp(-Mathf.Max(0f, response) * Mathf.Max(0f, deltaTime)));

        private static float HipAngle(float phase, float run)
            => Mathf.Cos(phase * Mathf.PI * 2f) * Mathf.Lerp(19f, 31f, run) +
               Mathf.Sin(phase * Mathf.PI * 4f + 0.35f) * Mathf.Lerp(1.5f, 3.5f, run);

        private static float KneeFlexion(float phase, float run)
        {
            float loading = PositivePulse(phase, 0.02f, Mathf.Lerp(0.18f, 0.12f, run)) * Mathf.Lerp(10f, 18f, run);
            float swing = PositivePulse(phase, Mathf.Lerp(0.57f, 0.42f, run),
                Mathf.Lerp(0.91f, 0.82f, run)) * Mathf.Lerp(46f, 68f, run);
            return loading + swing;
        }

        private static float AnkleAngle(float phase, float run)
        {
            float pushOff = PositivePulse(phase, Mathf.Lerp(0.42f, 0.3f, run), Mathf.Lerp(0.64f, 0.52f, run));
            float clearance = PositivePulse(phase, Mathf.Lerp(0.62f, 0.5f, run), Mathf.Lerp(0.91f, 0.84f, run));
            return pushOff * Mathf.Lerp(-15f, -23f, run) + clearance * Mathf.Lerp(9f, 14f, run);
        }

        private static float PositivePulse(float value, float start, float end)
        {
            if (value <= start || value >= end) return 0f;
            return Mathf.Sin((value - start) / (end - start) * Mathf.PI);
        }

        private static float Smooth01(float value)
        {
            float clamped = Mathf.Clamp01(value);
            return clamped * clamped * (3f - 2f * clamped);
        }
    }
}
