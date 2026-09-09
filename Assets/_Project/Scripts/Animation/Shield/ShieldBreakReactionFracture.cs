using System;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DefaultExecutionOrder(12000)]
    [DisallowMultipleComponent]
    public sealed class ShieldBreakReactionFracture : MonoBehaviour
    {
        [SerializeField] private ShieldStateMotion stateMotion;
        [SerializeField] private Transform intactShield;
        [SerializeField] private Renderer[] intactRenderers = Array.Empty<Renderer>();
        [SerializeField] private Transform storageParent;
        [SerializeField] private Transform detachedFragmentRoot;
        [SerializeField] private Transform[] fragmentPivots = Array.Empty<Transform>();
        [SerializeField] private Vector3[] fragmentCenters = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] directions = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] rotationAxes = Array.Empty<Vector3>();
        [SerializeField] private float[] rotationDegrees = Array.Empty<float>();
        [SerializeField] private float[] distanceScales = Array.Empty<float>();
        [SerializeField] private float startDelaySeconds = 0.05f;
        [SerializeField] private float cycleDurationSeconds = 2.866667f;
        [SerializeField] private float travelExtentLocal;
        [SerializeField] private float travelMultiplier = 1.9f;
        [SerializeField] private float gravityBaseMultiplier = 0.18f;
        [SerializeField] private float gravityProgressMultiplier = 0.62f;
        [SerializeField] private float linearSpeedMultiplier = 10f;
        [SerializeField] private float fragmentVisibleDurationSeconds = 1f;

        private int observedCycleCount;
        private bool detached;
        private bool fragmentsHidden;
        private float previousCycleTimeSeconds;

        public int FragmentCount => fragmentPivots.Length;
        public float StartDelaySeconds => startDelaySeconds;
        public float FractureDurationSeconds => cycleDurationSeconds - startDelaySeconds;
        public float CycleDurationSeconds => cycleDurationSeconds;
        public float CurrentCycleTimeSeconds { get; private set; }
        public float CurrentFractureProgress { get; private set; }
        public float LastDetachCycleTimeSeconds { get; private set; } = -1f;
        public float LastPreDetachCycleTimeSeconds { get; private set; } = -1f;
        public int DetachmentCount { get; private set; }
        public int ResetCount { get; private set; }
        public bool IsDetached => detached;
        public bool IntactShieldVisible => intactRenderers.Length > 0 && intactRenderers.AllVisible();
        public bool DetachedRootVisible => detachedFragmentRoot != null && detachedFragmentRoot.gameObject.activeSelf;
        public bool DetachedRootHasNoParent => detachedFragmentRoot != null && detachedFragmentRoot.parent == null;
        public float LastMaximumFragmentScaleError { get; private set; }
        public float LinearSpeedMultiplier => linearSpeedMultiplier;
        public float FragmentVisibleDurationSeconds => fragmentVisibleDurationSeconds;
        public float FragmentHideCycleTimeSeconds => startDelaySeconds + fragmentVisibleDurationSeconds;
        public bool FragmentsHidden => fragmentsHidden;
        public int HideCount { get; private set; }
        public float LastHideCycleTimeSeconds { get; private set; } = -1f;
        public float LastPreHideCycleTimeSeconds { get; private set; } = -1f;
        public float LastMinimumFragmentSpeedLocalUnitsPerSecond { get; private set; }
        public float LastMaximumFragmentSpeedLocalUnitsPerSecond { get; private set; }
        public Vector3 LastDetachedAnchorPosition { get; private set; }
        public Quaternion LastDetachedAnchorRotation { get; private set; } = Quaternion.identity;
        public float DetachedAnchorPositionError => detachedFragmentRoot == null || !detached
            ? 0f
            : Vector3.Distance(detachedFragmentRoot.position, LastDetachedAnchorPosition);
        public float DetachedAnchorRotationErrorDegrees => detachedFragmentRoot == null || !detached
            ? 0f
            : Quaternion.Angle(detachedFragmentRoot.rotation, LastDetachedAnchorRotation);
        public Transform DetachedFragmentRoot => detachedFragmentRoot;
        public Transform[] FragmentPivots => fragmentPivots;

        public void Configure(ShieldStateMotion motion, Transform shield, Renderer[] shieldRenderers,
            Transform fragmentStorageParent, Transform fragmentRoot, Transform[] pivots,
            Vector3[] centers, Vector3[] approvedDirections, Vector3[] approvedRotationAxes,
            float[] approvedRotationDegrees, float[] approvedDistanceScales,
            float delaySeconds, float totalCycleSeconds, float extentLocal)
        {
            int count = pivots?.Length ?? 0;
            if (count == 0 || (centers?.Length ?? 0) != count ||
                (approvedDirections?.Length ?? 0) != count ||
                (approvedRotationAxes?.Length ?? 0) != count ||
                (approvedRotationDegrees?.Length ?? 0) != count ||
                (approvedDistanceScales?.Length ?? 0) != count)
                throw new ArgumentException("Approved Shield fragment arrays must have matching non-zero lengths.");
            if (delaySeconds < 0f || totalCycleSeconds <= delaySeconds)
                throw new ArgumentException("Approved Shield fracture timing is invalid.");

            stateMotion = motion;
            intactShield = shield;
            intactRenderers = shieldRenderers ?? Array.Empty<Renderer>();
            storageParent = fragmentStorageParent;
            detachedFragmentRoot = fragmentRoot;
            fragmentPivots = pivots;
            fragmentCenters = centers;
            directions = approvedDirections;
            rotationAxes = approvedRotationAxes;
            rotationDegrees = approvedRotationDegrees;
            distanceScales = approvedDistanceScales;
            startDelaySeconds = delaySeconds;
            cycleDurationSeconds = totalCycleSeconds;
            travelExtentLocal = extentLocal;
            travelMultiplier = 1.9f;
            gravityBaseMultiplier = 0.18f;
            gravityProgressMultiplier = 0.62f;
            linearSpeedMultiplier = 10f;
            fragmentVisibleDurationSeconds = 1f;
            observedCycleCount = stateMotion == null ? 0 : stateMotion.CompletedCycleCount;
            previousCycleTimeSeconds = 0f;
            ResetFractureState(false);
        }

        private void OnEnable()
        {
            observedCycleCount = stateMotion == null ? 0 : stateMotion.CompletedCycleCount;
            previousCycleTimeSeconds = 0f;
            ResetFractureState(false);
        }

        private void OnDisable()
        {
            if (detachedFragmentRoot != null) detachedFragmentRoot.gameObject.SetActive(false);
            SetIntactRendererVisibility(true);
            detached = false;
            fragmentsHidden = false;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || stateMotion == null || intactShield == null ||
                detachedFragmentRoot == null || fragmentPivots.Length == 0)
                return;

            int cycleCount = stateMotion.CompletedCycleCount;
            CurrentCycleTimeSeconds = stateMotion.LastCycleProgress * cycleDurationSeconds;
            if (cycleCount != observedCycleCount)
            {
                observedCycleCount = cycleCount;
                ResetFractureState(true);
            }

            if (CurrentCycleTimeSeconds < startDelaySeconds)
            {
                if (detached) ResetFractureState(true);
                CurrentFractureProgress = 0f;
                previousCycleTimeSeconds = CurrentCycleTimeSeconds;
                return;
            }

            if (!detached)
            {
                LastPreDetachCycleTimeSeconds = previousCycleTimeSeconds;
                DetachAtCurrentShieldPose();
            }
            CurrentFractureProgress = Mathf.Clamp01(
                (CurrentCycleTimeSeconds - startDelaySeconds) / FractureDurationSeconds);
            ApplyApprovedFragmentMotion(CurrentFractureProgress);
            if (!fragmentsHidden &&
                CurrentCycleTimeSeconds >= startDelaySeconds + fragmentVisibleDurationSeconds)
            {
                detachedFragmentRoot.gameObject.SetActive(false);
                fragmentsHidden = true;
                LastPreHideCycleTimeSeconds = previousCycleTimeSeconds;
                LastHideCycleTimeSeconds = CurrentCycleTimeSeconds;
                HideCount++;
            }
            previousCycleTimeSeconds = CurrentCycleTimeSeconds;
        }

        private void DetachAtCurrentShieldPose()
        {
            Vector3 worldScale = intactShield.lossyScale;
            detachedFragmentRoot.SetParent(null, false);
            detachedFragmentRoot.SetPositionAndRotation(intactShield.position, intactShield.rotation);
            detachedFragmentRoot.localScale = worldScale;
            LastDetachedAnchorPosition = detachedFragmentRoot.position;
            LastDetachedAnchorRotation = detachedFragmentRoot.rotation;
            detachedFragmentRoot.gameObject.SetActive(true);
            SetIntactRendererVisibility(false);
            detached = true;
            fragmentsHidden = false;
            LastDetachCycleTimeSeconds = CurrentCycleTimeSeconds;
            DetachmentCount++;
        }

        private void ApplyApprovedFragmentMotion(float progress)
        {
            float easedRotation = progress * progress * (3f - 2f * progress);
            LastMaximumFragmentScaleError = 0f;
            LastMinimumFragmentSpeedLocalUnitsPerSecond = float.PositiveInfinity;
            LastMaximumFragmentSpeedLocalUnitsPerSecond = 0f;
            for (int index = 0; index < fragmentPivots.Length; index++)
            {
                Vector3 approvedFinalDisplacement = CalculateApprovedFinalDisplacement(index);
                Vector3 linearVelocity = approvedFinalDisplacement *
                    (linearSpeedMultiplier / FractureDurationSeconds);
                Transform fragment = fragmentPivots[index];
                fragment.localPosition = fragmentCenters[index] + approvedFinalDisplacement *
                    (linearSpeedMultiplier * progress);
                fragment.localRotation = Quaternion.AngleAxis(rotationDegrees[index] * easedRotation,
                    rotationAxes[index]);
                LastMinimumFragmentSpeedLocalUnitsPerSecond = Mathf.Min(
                    LastMinimumFragmentSpeedLocalUnitsPerSecond, linearVelocity.magnitude);
                LastMaximumFragmentSpeedLocalUnitsPerSecond = Mathf.Max(
                    LastMaximumFragmentSpeedLocalUnitsPerSecond, linearVelocity.magnitude);
                LastMaximumFragmentScaleError = Mathf.Max(LastMaximumFragmentScaleError,
                    Vector3.Distance(fragment.localScale, Vector3.one));
            }
        }

        private void ResetFractureState(bool countReset)
        {
            if (detachedFragmentRoot != null)
            {
                detachedFragmentRoot.gameObject.SetActive(false);
                if (storageParent != null) detachedFragmentRoot.SetParent(storageParent, false);
                detachedFragmentRoot.localPosition = Vector3.zero;
                detachedFragmentRoot.localRotation = Quaternion.identity;
                detachedFragmentRoot.localScale = Vector3.one;
            }
            for (int index = 0; index < fragmentPivots.Length; index++)
            {
                if (fragmentPivots[index] == null) continue;
                fragmentPivots[index].localPosition = fragmentCenters[index];
                fragmentPivots[index].localRotation = Quaternion.identity;
                fragmentPivots[index].localScale = Vector3.one;
            }
            SetIntactRendererVisibility(true);
            CurrentCycleTimeSeconds = 0f;
            CurrentFractureProgress = 0f;
            previousCycleTimeSeconds = 0f;
            detached = false;
            fragmentsHidden = false;
            if (countReset) ResetCount++;
        }

        public float CalculateMaximumApprovedPoseError()
        {
            if (!detached || fragmentPivots.Length == 0) return 0f;
            float progress = CurrentFractureProgress;
            float easedRotation = progress * progress * (3f - 2f * progress);
            float maximum = 0f;
            for (int index = 0; index < fragmentPivots.Length; index++)
            {
                Vector3 expectedPosition = fragmentCenters[index] + CalculateApprovedFinalDisplacement(index) *
                    (linearSpeedMultiplier * progress);
                Quaternion expectedRotation = Quaternion.AngleAxis(rotationDegrees[index] * easedRotation,
                    rotationAxes[index]);
                maximum = Mathf.Max(maximum,
                    Vector3.Distance(fragmentPivots[index].localPosition, expectedPosition),
                    Quaternion.Angle(fragmentPivots[index].localRotation, expectedRotation));
            }
            return maximum;
        }

        public Vector3 CalculateFragmentLinearVelocityLocal(int index)
        {
            if (index < 0 || index >= fragmentPivots.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return CalculateApprovedFinalDisplacement(index) *
                (linearSpeedMultiplier / FractureDurationSeconds);
        }

        private Vector3 CalculateApprovedFinalDisplacement(int index)
        {
            Vector3 directionalTravel = directions[index] * travelExtentLocal *
                travelMultiplier * distanceScales[index];
            Vector3 approvedFinalGravity = -Vector3.forward * travelExtentLocal *
                (gravityBaseMultiplier + gravityProgressMultiplier);
            return directionalTravel + approvedFinalGravity;
        }

        private void SetIntactRendererVisibility(bool visible)
        {
            for (int index = 0; index < intactRenderers.Length; index++)
                if (intactRenderers[index] != null) intactRenderers[index].enabled = visible;
        }
    }

    internal static class ShieldRendererArrayExtensions
    {
        internal static bool AllVisible(this Renderer[] renderers)
        {
            for (int index = 0; index < renderers.Length; index++)
                if (renderers[index] == null || !renderers[index].enabled) return false;
            return true;
        }
    }
}
