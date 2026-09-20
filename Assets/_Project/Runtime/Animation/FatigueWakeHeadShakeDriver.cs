using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10000)]
    public sealed class FatigueWakeHeadShakeDriver : MonoBehaviour
    {
        public const float DownPitchDegrees = 10f;
        public const float SideYawDegrees = 20f;
        public const float FirstShakeSeconds = 0.4f;
        public const float SecondShakeSeconds = 0.3f;
        public const float ThirdShakeSeconds = 0.15f;
        public const float FourthShakeSeconds = 0.15f;
        public const float TotalShakeSeconds = 1f;

        private const float NeckShare = 0.35f;
        private const float HeadShare = 0.65f;
        private const float BoundaryBlendSeconds = 0.04f;

        [SerializeField] private Animator animator;
        [SerializeField] private Transform neck;
        [SerializeField] private Transform head;

        public float CurrentPitchDegrees { get; private set; }
        public float CurrentYawDegrees { get; private set; }
        public float CurrentPhaseElapsed { get; private set; }
        public bool IsActive { get; private set; }

        public bool IsConfigured =>
            animator != null && neck != null && head != null;

        public Animator ConfiguredAnimator => animator;
        public Transform ConfiguredNeck => neck;
        public Transform ConfiguredHead => head;

        public void Configure(
            Animator configuredAnimator,
            Transform configuredNeck,
            Transform configuredHead)
        {
            animator = configuredAnimator;
            neck = configuredNeck;
            head = configuredHead;
        }

        private void LateUpdate()
        {
            IsActive = false;
            CurrentPitchDegrees = 0f;
            CurrentYawDegrees = 0f;
            CurrentPhaseElapsed = 0f;
            if (!IsConfigured ||
                !FatigueHeadShakeLocomotionCycleBehaviour.TryGetSequenceState(
                    animator,
                    out _,
                    out int phase,
                    out float phaseElapsed) ||
                phase != FatigueHeadShakeLocomotionCycleBehaviour.HeadShakePhase)
                return;

            float time = Mathf.Clamp(phaseElapsed, 0f, TotalShakeSeconds);
            float envelope = BoundaryEnvelope(time);
            float pitch = DownPitchDegrees * envelope;
            float yaw = EvaluateYawDegrees(time) * envelope;
            Quaternion offset =
                Quaternion.AngleAxis(yaw, transform.up) *
                Quaternion.AngleAxis(pitch, transform.right);
            neck.rotation = Quaternion.Slerp(
                Quaternion.identity,
                offset,
                NeckShare) * neck.rotation;
            head.rotation = Quaternion.Slerp(
                Quaternion.identity,
                offset,
                HeadShare) * head.rotation;

            IsActive = true;
            CurrentPitchDegrees = pitch;
            CurrentYawDegrees = yaw;
            CurrentPhaseElapsed = time;
        }

        public static float EvaluateYawDegrees(float phaseElapsed)
        {
            float time = Mathf.Clamp(phaseElapsed, 0f, TotalShakeSeconds);
            float start = 0f;
            float[] durations =
            {
                FirstShakeSeconds,
                SecondShakeSeconds,
                ThirdShakeSeconds,
                FourthShakeSeconds
            };
            for (int index = 0; index < durations.Length; index++)
            {
                float end = start + durations[index];
                if (time <= end || index == durations.Length - 1)
                    return EvaluateSingleShake(
                        Mathf.InverseLerp(start, end, time));
                start = end;
            }
            return 0f;
        }

        public static int ShakeIndex(float phaseElapsed)
        {
            float time = Mathf.Clamp(
                phaseElapsed,
                0f,
                TotalShakeSeconds - Mathf.Epsilon);
            if (time < FirstShakeSeconds)
                return 0;
            if (time < FirstShakeSeconds + SecondShakeSeconds)
                return 1;
            if (time < FirstShakeSeconds + SecondShakeSeconds + ThirdShakeSeconds)
                return 2;
            return 3;
        }

        private static float EvaluateSingleShake(float normalized)
        {
            float t = Mathf.Clamp01(normalized);
            if (t <= 0.25f)
                return Mathf.Lerp(
                    0f,
                    -SideYawDegrees,
                    Smooth(t / 0.25f));
            if (t <= 0.75f)
                return Mathf.Lerp(
                    -SideYawDegrees,
                    SideYawDegrees,
                    Smooth((t - 0.25f) / 0.5f));
            return Mathf.Lerp(
                SideYawDegrees,
                0f,
                Smooth((t - 0.75f) / 0.25f));
        }

        private static float BoundaryEnvelope(float time)
        {
            float enter = Mathf.SmoothStep(
                0f,
                1f,
                time / BoundaryBlendSeconds);
            float exit = 1f - Mathf.SmoothStep(
                0f,
                1f,
                (time - (TotalShakeSeconds - BoundaryBlendSeconds)) /
                BoundaryBlendSeconds);
            return Mathf.Min(enter, exit);
        }

        private static float Smooth(float value) =>
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
    }
}
