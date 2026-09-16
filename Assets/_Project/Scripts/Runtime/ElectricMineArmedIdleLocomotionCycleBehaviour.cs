using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    public sealed class ElectricMineArmedIdleLocomotionCycleBehaviour : StateMachineBehaviour
    {
        public const int MotionCount = 7;
        public const float SecondsPerMotion = 1f;
        public const string MoveXParameter = "MoveX";
        public const string MoveYParameter = "MoveY";
        public const string DiagonalBlendParameter = "ForwardSidestepBlend";

        private static readonly Vector2[] MotionPositions =
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(1f, 0f),
            new Vector2(0.70710677f, 0.70710677f),
            new Vector2(0f, 2f),
            new Vector2(0f, -2f)
        };

        private static readonly string[] MotionNames =
        {
            "Idle",
            "WalkForward",
            "WalkBackward",
            "Sidestep",
            "WalkDiagonal",
            "RunForward",
            "Jump"
        };

        private static readonly Dictionary<int, float> StartTimes =
            new Dictionary<int, float>();
        private static readonly Dictionary<int, int> LastPhases =
            new Dictionary<int, int>();

        [SerializeField] private string[] gripBonePaths = Array.Empty<string>();
        [SerializeField] private Quaternion[] gripLocalRotations =
            Array.Empty<Quaternion>();

        [NonSerialized] private readonly Dictionary<int, Transform[]> resolvedBones =
            new Dictionary<int, Transform[]>();

        public int GripBoneCount => gripBonePaths?.Length ?? 0;

        public void ConfigureGrip(string[] paths, Quaternion[] rotations)
        {
            if (paths == null || rotations == null || paths.Length != rotations.Length ||
                paths.Length == 0)
                throw new ArgumentException("Electric mine armed grip data is incomplete.");
            gripBonePaths = (string[])paths.Clone();
            gripLocalRotations = (Quaternion[])rotations.Clone();
            resolvedBones.Clear();
        }

        public float MaximumGripDeviation(Animator animator)
        {
            Transform[] bones = ResolveBones(animator);
            float maximum = 0f;
            for (int index = 0; index < bones.Length; index++)
                maximum = Mathf.Max(maximum,
                    Quaternion.Angle(bones[index].localRotation,
                        gripLocalRotations[index]));
            return maximum;
        }

        public static string MotionName(int phase)
        {
            if (phase < 0 || phase >= MotionNames.Length)
                throw new ArgumentOutOfRangeException(nameof(phase));
            return MotionNames[phase];
        }

        public static Vector2 MotionPosition(int phase)
        {
            if (phase < 0 || phase >= MotionPositions.Length)
                throw new ArgumentOutOfRangeException(nameof(phase));
            return MotionPositions[phase];
        }

        public static bool TryGetSequenceState(
            Animator animator,
            out int absolutePhase,
            out int phase,
            out float phaseElapsed)
        {
            absolutePhase = 0;
            phase = 0;
            phaseElapsed = 0f;
            if (animator == null ||
                !StartTimes.TryGetValue(animator.GetInstanceID(), out float start))
                return false;
            float elapsed = Mathf.Max(0f, Time.time - start);
            absolutePhase = Mathf.FloorToInt(elapsed / SecondsPerMotion);
            phase = absolutePhase % MotionCount;
            phaseElapsed = elapsed - absolutePhase * SecondsPerMotion;
            return true;
        }

        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            int id = animator.GetInstanceID();
            if (!StartTimes.ContainsKey(id))
                StartTimes[id] = Time.time;
            int phase = CurrentPhase(animator);
            LastPhases[id] = phase;
            ApplyPhase(animator, phase);
        }

        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            int id = animator.GetInstanceID();
            if (!StartTimes.ContainsKey(id))
                StartTimes[id] = Time.time;
            int phase = CurrentPhase(animator);
            ApplyPhase(animator, phase);
            if (!LastPhases.TryGetValue(id, out int previous) || previous == phase)
            {
                LastPhases[id] = phase;
                return;
            }

            LastPhases[id] = phase;
            animator.Play(stateInfo.fullPathHash, layerIndex, 0f);
        }

        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            int id = animator.GetInstanceID();
            if (animator.gameObject.activeInHierarchy)
                return;
            StartTimes.Remove(id);
            LastPhases.Remove(id);
            resolvedBones.Remove(id);
        }

        private static int CurrentPhase(Animator animator)
        {
            float elapsed = Mathf.Max(
                0f,
                Time.time - StartTimes[animator.GetInstanceID()]);
            return Mathf.FloorToInt(elapsed / SecondsPerMotion) % MotionCount;
        }

        private static void ApplyPhase(Animator animator, int phase)
        {
            Vector2 position = MotionPositions[phase];
            animator.SetFloat(MoveXParameter, position.x);
            animator.SetFloat(MoveYParameter, position.y);
            animator.SetFloat(DiagonalBlendParameter, 0.5f);
        }

        private Transform[] ResolveBones(Animator animator)
        {
            if (gripBonePaths == null || gripLocalRotations == null ||
                gripBonePaths.Length == 0 ||
                gripBonePaths.Length != gripLocalRotations.Length)
                throw new InvalidOperationException(
                    "Electric mine armed grip pose was not configured.");
            int id = animator.GetInstanceID();
            if (resolvedBones.TryGetValue(id, out Transform[] cached) &&
                cached.All(item => item != null))
                return cached;
            var bones = new Transform[gripBonePaths.Length];
            for (int index = 0; index < bones.Length; index++)
            {
                bones[index] = animator.transform.Find(gripBonePaths[index]);
                if (bones[index] == null)
                    throw new MissingReferenceException(
                        "Electric mine armed grip bone is missing: " +
                        gripBonePaths[index]);
            }
            resolvedBones[id] = bones;
            return bones;
        }
    }
}
