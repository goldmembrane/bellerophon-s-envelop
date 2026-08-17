using UnityEngine;

namespace Bellerophon.Enemies.Fuga
{
    [DisallowMultipleComponent]
    public sealed class FugaConsumeMotionDriver : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform leftWingRoot;
        [SerializeField] private Transform rightWingRoot;
        [SerializeField] private Transform upperLipRoot;
        [SerializeField] private Transform lowerLipRoot;
        [SerializeField] private Vector3 forwardWorld = Vector3.forward;
        [SerializeField] private float loopDuration = 2f;
        [SerializeField] private float wingbeatFrequency = 0.7f;
        [SerializeField] private float bodyTiltDegrees = 30f;
        [SerializeField] private float mouthOpenDegrees = 60f;
        [SerializeField] private float forwardDistanceMeters = 0.08f;

        public Rigidbody Body => body;
        public Transform VisualRoot => visualRoot;
        public Transform LeftWingRoot => leftWingRoot;
        public Transform RightWingRoot => rightWingRoot;
        public Transform UpperLipRoot => upperLipRoot;
        public Transform LowerLipRoot => lowerLipRoot;
        public Vector3 ForwardWorld => forwardWorld;
        public float LoopDuration => loopDuration;
        public float WingbeatFrequency => wingbeatFrequency;
        public float BodyTiltDegrees => bodyTiltDegrees;
        public float MouthOpenDegrees => mouthOpenDegrees;
        public float ForwardDistanceMeters => forwardDistanceMeters;
        public float CurrentLoopTime => currentLoopTime;
        public float CurrentBodyTiltDegrees => currentBodyTiltDegrees;
        public float CurrentForwardOffsetMeters => currentForwardOffsetMeters;
        public float CurrentMouthWeight => currentMouthWeight;
        public float CurrentUpperLipAngleDegrees => currentUpperLipAngleDegrees;
        public float CurrentLowerLipAngleDegrees => currentLowerLipAngleDegrees;
        public int CompletedLoopCount => completedLoopCount;
        public int CompletedWingbeatCount => completedWingbeatCount;
        public float LastLoopPeakBodyTiltDegrees => lastLoopPeakBodyTiltDegrees;
        public float LastLoopPeakForwardOffsetMeters => lastLoopPeakForwardOffsetMeters;
        public float LastLoopPeakMouthWeight => lastLoopPeakMouthWeight;
        public float LastLoopPeakUpperLipAngleDegrees => lastLoopPeakUpperLipAngleDegrees;
        public float LastLoopPeakLowerLipAngleDegrees => lastLoopPeakLowerLipAngleDegrees;
        public int InvalidLoopPeakCount => invalidLoopPeakCount;
        public int InvalidLoopReturnCount => invalidLoopReturnCount;

        private const float FirstTiltEndTime = 0.35f;
        private const float MouthOpenEndTime = 0.80f;
        private const float BitePeakTime = 1.00f;
        private const float BiteEndTime = 1.05f;
        private const float ReturnEndTime = 1.65f;
        private const float WingUpstrokeDegrees = 44f;
        private const float WingDownstrokeDegrees = -40f;

        private Vector3 initialBodyPosition;
        private Quaternion initialVisualRotation;
        private Quaternion initialLeftWingRotation;
        private Quaternion initialRightWingRotation;
        private Quaternion initialUpperLipRotation;
        private Quaternion initialLowerLipRotation;
        private bool initialPoseCaptured;
        private float elapsedSeconds;
        private float currentLoopTime;
        private float currentBodyTiltDegrees;
        private float currentForwardOffsetMeters;
        private float currentMouthWeight;
        private float currentUpperLipAngleDegrees;
        private float currentLowerLipAngleDegrees;
        private int completedLoopCount;
        private int completedWingbeatCount;
        private float loopPeakBodyTiltDegrees;
        private float loopPeakForwardOffsetMeters;
        private float loopPeakMouthWeight;
        private float loopPeakUpperLipAngleDegrees;
        private float loopPeakLowerLipAngleDegrees;
        private float lastLoopPeakBodyTiltDegrees;
        private float lastLoopPeakForwardOffsetMeters;
        private float lastLoopPeakMouthWeight;
        private float lastLoopPeakUpperLipAngleDegrees;
        private float lastLoopPeakLowerLipAngleDegrees;
        private int invalidLoopPeakCount;
        private int invalidLoopReturnCount;

        public void Configure(
            Rigidbody configuredBody,
            Transform configuredVisualRoot,
            Transform configuredLeftWingRoot,
            Transform configuredRightWingRoot,
            Transform configuredUpperLipRoot,
            Transform configuredLowerLipRoot,
            Vector3 configuredForwardWorld,
            float configuredLoopDuration,
            float configuredWingbeatFrequency,
            float configuredBodyTiltDegrees,
            float configuredMouthOpenDegrees,
            float configuredForwardDistanceMeters)
        {
            body = configuredBody;
            visualRoot = configuredVisualRoot;
            leftWingRoot = configuredLeftWingRoot;
            rightWingRoot = configuredRightWingRoot;
            upperLipRoot = configuredUpperLipRoot;
            lowerLipRoot = configuredLowerLipRoot;
            forwardWorld = configuredForwardWorld.sqrMagnitude > 0.000001f
                ? configuredForwardWorld.normalized
                : Vector3.forward;
            loopDuration = Mathf.Max(0.02f, configuredLoopDuration);
            wingbeatFrequency = Mathf.Max(0.01f, configuredWingbeatFrequency);
            bodyTiltDegrees = Mathf.Abs(configuredBodyTiltDegrees);
            mouthOpenDegrees = Mathf.Abs(configuredMouthOpenDegrees);
            forwardDistanceMeters = Mathf.Max(0f, configuredForwardDistanceMeters);
            initialPoseCaptured = false;
            CaptureInitialPose();
            ResetReviewState();
            if (Application.isPlaying)
            {
                ApplyVisualPose(0f);
            }
        }

        private void OnEnable()
        {
            initialPoseCaptured = false;
            CaptureInitialPose();
            ResetReviewState();
            ApplyVisualPose(0f);
        }

        private void OnDisable()
        {
            if (!initialPoseCaptured)
            {
                return;
            }

            if (visualRoot != null) visualRoot.localRotation = initialVisualRotation;
            if (leftWingRoot != null) leftWingRoot.localRotation = initialLeftWingRotation;
            if (rightWingRoot != null) rightWingRoot.localRotation = initialRightWingRotation;
            if (upperLipRoot != null) upperLipRoot.localRotation = initialUpperLipRotation;
            if (lowerLipRoot != null) lowerLipRoot.localRotation = initialLowerLipRotation;
            currentUpperLipAngleDegrees = 0f;
            currentLowerLipAngleDegrees = 0f;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || body == null || visualRoot == null ||
                leftWingRoot == null || rightWingRoot == null || upperLipRoot == null || lowerLipRoot == null)
            {
                return;
            }

            CaptureInitialPose();
            var previousLoopIndex = Mathf.FloorToInt(elapsedSeconds / loopDuration);
            elapsedSeconds += Time.fixedDeltaTime;
            var currentLoopIndex = Mathf.FloorToInt(elapsedSeconds / loopDuration);
            if (currentLoopIndex > previousLoopIndex)
            {
                CompleteLoop();
            }

            completedWingbeatCount = Mathf.FloorToInt(elapsedSeconds * wingbeatFrequency);
            currentLoopTime = Mathf.Repeat(elapsedSeconds, loopDuration);
            ApplyVisualPose(currentLoopTime);
            ApplyRigidbodyForwardMotion();
            loopPeakBodyTiltDegrees = Mathf.Max(loopPeakBodyTiltDegrees, currentBodyTiltDegrees);
            loopPeakForwardOffsetMeters = Mathf.Max(loopPeakForwardOffsetMeters, currentForwardOffsetMeters);
            loopPeakMouthWeight = Mathf.Max(loopPeakMouthWeight, currentMouthWeight);
            loopPeakUpperLipAngleDegrees = Mathf.Max(
                loopPeakUpperLipAngleDegrees,
                Quaternion.Angle(initialUpperLipRotation, upperLipRoot.localRotation));
            loopPeakLowerLipAngleDegrees = Mathf.Max(
                loopPeakLowerLipAngleDegrees,
                Quaternion.Angle(initialLowerLipRotation, lowerLipRoot.localRotation));
        }

        private void CaptureInitialPose()
        {
            if (initialPoseCaptured || body == null || visualRoot == null || leftWingRoot == null ||
                rightWingRoot == null || upperLipRoot == null || lowerLipRoot == null)
            {
                return;
            }

            initialBodyPosition = body.position;
            initialVisualRotation = visualRoot.localRotation;
            initialLeftWingRotation = leftWingRoot.localRotation;
            initialRightWingRotation = rightWingRoot.localRotation;
            initialUpperLipRotation = upperLipRoot.localRotation;
            initialLowerLipRotation = lowerLipRoot.localRotation;
            initialPoseCaptured = true;
        }

        private void ResetReviewState()
        {
            elapsedSeconds = 0f;
            currentLoopTime = 0f;
            completedLoopCount = 0;
            completedWingbeatCount = 0;
            loopPeakBodyTiltDegrees = 0f;
            loopPeakForwardOffsetMeters = 0f;
            loopPeakMouthWeight = 0f;
            loopPeakUpperLipAngleDegrees = 0f;
            loopPeakLowerLipAngleDegrees = 0f;
            lastLoopPeakBodyTiltDegrees = 0f;
            lastLoopPeakForwardOffsetMeters = 0f;
            lastLoopPeakMouthWeight = 0f;
            lastLoopPeakUpperLipAngleDegrees = 0f;
            lastLoopPeakLowerLipAngleDegrees = 0f;
            invalidLoopPeakCount = 0;
            invalidLoopReturnCount = 0;
        }

        private void CompleteLoop()
        {
            completedLoopCount++;
            lastLoopPeakBodyTiltDegrees = loopPeakBodyTiltDegrees;
            lastLoopPeakForwardOffsetMeters = loopPeakForwardOffsetMeters;
            lastLoopPeakMouthWeight = loopPeakMouthWeight;
            lastLoopPeakUpperLipAngleDegrees = loopPeakUpperLipAngleDegrees;
            lastLoopPeakLowerLipAngleDegrees = loopPeakLowerLipAngleDegrees;
            if (Mathf.Abs(lastLoopPeakBodyTiltDegrees - bodyTiltDegrees) > 0.05f ||
                Mathf.Abs(lastLoopPeakForwardOffsetMeters - forwardDistanceMeters) > 0.0005f ||
                Mathf.Abs(lastLoopPeakMouthWeight - 100f) > 0.05f ||
                Mathf.Abs(lastLoopPeakUpperLipAngleDegrees - mouthOpenDegrees * 0.5f) > 0.05f ||
                Mathf.Abs(lastLoopPeakLowerLipAngleDegrees - mouthOpenDegrees * 0.5f) > 0.05f)
            {
                invalidLoopPeakCount++;
            }

            if (currentBodyTiltDegrees > 0.05f || currentForwardOffsetMeters > 0.0005f || currentMouthWeight > 0.05f ||
                Quaternion.Angle(initialUpperLipRotation, upperLipRoot.localRotation) > 0.05f ||
                Quaternion.Angle(initialLowerLipRotation, lowerLipRoot.localRotation) > 0.05f)
            {
                invalidLoopReturnCount++;
            }

            loopPeakBodyTiltDegrees = 0f;
            loopPeakForwardOffsetMeters = 0f;
            loopPeakMouthWeight = 0f;
            loopPeakUpperLipAngleDegrees = 0f;
            loopPeakLowerLipAngleDegrees = 0f;
        }

        private void ApplyVisualPose(float time)
        {
            currentBodyTiltDegrees = EvaluateBodyTilt(time);
            currentForwardOffsetMeters = EvaluateForwardOffset(time);
            currentMouthWeight = EvaluateMouthWeight(time);
            if (visualRoot != null)
            {
                visualRoot.localRotation = initialVisualRotation *
                                           Quaternion.AngleAxis(currentBodyTiltDegrees, Vector3.right);
            }

            var wingMidpoint = (WingUpstrokeDegrees + WingDownstrokeDegrees) * 0.5f;
            var wingAmplitude = (WingUpstrokeDegrees - WingDownstrokeDegrees) * 0.5f;
            var wingAngle = wingMidpoint + wingAmplitude *
                Mathf.Cos(elapsedSeconds * wingbeatFrequency * Mathf.PI * 2f);
            if (leftWingRoot != null)
            {
                leftWingRoot.localRotation = initialLeftWingRotation * Quaternion.AngleAxis(wingAngle, Vector3.right);
            }

            if (rightWingRoot != null)
            {
                rightWingRoot.localRotation = initialRightWingRotation * Quaternion.AngleAxis(wingAngle, Vector3.right);
            }

            SetLipPose(currentMouthWeight);
        }

        private void ApplyRigidbodyForwardMotion()
        {
            var targetPosition = initialBodyPosition + forwardWorld * currentForwardOffsetMeters;
            body.linearVelocity = (targetPosition - body.position) / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
            body.angularVelocity = Vector3.zero;
        }

        private void SetLipPose(float weight)
        {
            if (visualRoot == null || upperLipRoot == null || lowerLipRoot == null)
            {
                return;
            }

            var progress = Mathf.Clamp01(weight / 100f);
            var eachLipAngle = mouthOpenDegrees * 0.5f * progress;
            var modelRightWorld = visualRoot.TransformDirection(Vector3.right).normalized;
            var upperAxisInParent = upperLipRoot.parent != null
                ? upperLipRoot.parent.InverseTransformDirection(modelRightWorld).normalized
                : modelRightWorld;
            var lowerAxisInParent = lowerLipRoot.parent != null
                ? lowerLipRoot.parent.InverseTransformDirection(modelRightWorld).normalized
                : modelRightWorld;
            upperLipRoot.localRotation = Quaternion.AngleAxis(-eachLipAngle, upperAxisInParent) * initialUpperLipRotation;
            lowerLipRoot.localRotation = Quaternion.AngleAxis(eachLipAngle, lowerAxisInParent) * initialLowerLipRotation;
            currentUpperLipAngleDegrees = eachLipAngle;
            currentLowerLipAngleDegrees = eachLipAngle;
        }

        private float EvaluateBodyTilt(float time)
        {
            if (time < FirstTiltEndTime) return SmoothSegment(time, 0f, FirstTiltEndTime, 0f, 12f);
            if (time < MouthOpenEndTime) return SmoothSegment(time, FirstTiltEndTime, MouthOpenEndTime, 12f, 20f);
            if (time < BitePeakTime) return SmoothSegment(time, MouthOpenEndTime, BitePeakTime, 20f, bodyTiltDegrees);
            if (time <= BiteEndTime) return bodyTiltDegrees;
            if (time < ReturnEndTime) return SmoothSegment(time, BiteEndTime, ReturnEndTime, bodyTiltDegrees, 0f);
            return 0f;
        }

        private float EvaluateForwardOffset(float time)
        {
            if (time < FirstTiltEndTime) return 0f;
            if (time < MouthOpenEndTime) return SmoothSegment(time, FirstTiltEndTime, MouthOpenEndTime, 0f, 0.02f);
            if (time < BitePeakTime) return SmoothSegment(time, MouthOpenEndTime, BitePeakTime, 0.02f, forwardDistanceMeters);
            if (time <= BiteEndTime) return forwardDistanceMeters;
            if (time < ReturnEndTime) return SmoothSegment(time, BiteEndTime, ReturnEndTime, forwardDistanceMeters, 0f);
            return 0f;
        }

        private static float EvaluateMouthWeight(float time)
        {
            if (time < FirstTiltEndTime) return 0f;
            if (time < MouthOpenEndTime) return SmoothSegment(time, FirstTiltEndTime, MouthOpenEndTime, 0f, 100f);
            if (time < BitePeakTime) return SmoothSegment(time, MouthOpenEndTime, BitePeakTime, 100f, 0f);
            return 0f;
        }

        private static float SmoothSegment(float time, float startTime, float endTime, float startValue, float endValue)
        {
            var progress = Mathf.InverseLerp(startTime, endTime, time);
            progress = progress * progress * (3f - 2f * progress);
            return Mathf.LerpUnclamped(startValue, endValue, progress);
        }
    }
}
