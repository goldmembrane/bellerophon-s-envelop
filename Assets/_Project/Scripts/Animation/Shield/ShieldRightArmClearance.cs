using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class ShieldRightArmClearance : MonoBehaviour
    {
        [SerializeField] private Transform transporter;
        [SerializeField] private Transform spine;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform rightHand;
        [SerializeField] private float maximumForearmCenterRightOffsetMeters = 0.22f;
        [SerializeField] private float forearmCenterDownwardOffsetMeters = 0.05f;

        public float MaximumForearmCenterRightOffsetMeters => maximumForearmCenterRightOffsetMeters;
        public float ForearmCenterDownwardOffsetMeters => forearmCenterDownwardOffsetMeters;
        public float LastAppliedDownwardDisplacementMeters { get; private set; }
        public float LastLoweringRotationErrorDegrees { get; private set; }

        public void Configure(Transform transporterRoot, Transform spineBone, Transform shoulder, Transform upperArm,
            Transform forearm, Transform hand, float maximumRightOffsetMeters, float downwardOffsetMeters)
        {
            transporter = transporterRoot;
            spine = spineBone;
            rightShoulder = shoulder;
            rightArm = upperArm;
            rightForearm = forearm;
            rightHand = hand;
            maximumForearmCenterRightOffsetMeters = maximumRightOffsetMeters;
            forearmCenterDownwardOffsetMeters = downwardOffsetMeters;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || transporter == null || spine == null || rightShoulder == null || rightArm == null ||
                rightForearm == null || rightHand == null) return;

            LastAppliedDownwardDisplacementMeters = 0f;
            LastLoweringRotationErrorDegrees = 0f;
            ConstrainRightOffsetImmediately();

            LowerArmWithoutChangingPose();
        }

        public void ConstrainRightOffsetImmediately()
        {
            if (transporter == null || spine == null || rightArm == null || rightForearm == null || rightHand == null)
                return;
            Vector3 right = transporter.right.normalized;
            Vector3 center = Vector3.Lerp(rightForearm.position, rightHand.position, 0.5f);
            float currentRightOffset = Vector3.Dot(center - spine.position, right);
            if (currentRightOffset > maximumForearmCenterRightOffsetMeters)
                ConstrainRightOffset(center, right);
        }

        private void ConstrainRightOffset(Vector3 center, Vector3 right)
        {
            Vector3 fromPivot = center - rightArm.position;
            float desiredRightComponent = maximumForearmCenterRightOffsetMeters -
                Vector3.Dot(rightArm.position - spine.position, right);
            float radius = fromPivot.magnitude;
            desiredRightComponent = Mathf.Clamp(desiredRightComponent, -radius, radius);
            Vector3 perpendicular = fromPivot - right * Vector3.Dot(fromPivot, right);
            Vector3 perpendicularDirection = perpendicular.sqrMagnitude > 0.000001f
                ? perpendicular.normalized
                : transporter.forward.normalized;
            float desiredPerpendicularMagnitude = Mathf.Sqrt(Mathf.Max(0f,
                radius * radius - desiredRightComponent * desiredRightComponent));
            Vector3 desiredFromPivot = right * desiredRightComponent +
                perpendicularDirection * desiredPerpendicularMagnitude;
            rightArm.rotation = Quaternion.FromToRotation(fromPivot, desiredFromPivot) * rightArm.rotation;
        }

        private void LowerArmWithoutChangingPose()
        {
            Vector3 up = transporter.up.normalized;
            Vector3 centerBeforeLowering = Vector3.Lerp(rightForearm.position, rightHand.position, 0.5f);
            Quaternion shoulderRotation = rightShoulder.localRotation;
            Quaternion armRotation = rightArm.localRotation;
            Quaternion forearmRotation = rightForearm.localRotation;
            Quaternion handRotation = rightHand.localRotation;

            // Move the complete arm chain as one rigid pose after the inward upper-arm correction.
            rightShoulder.position -= up * forearmCenterDownwardOffsetMeters;

            Vector3 centerAfterLowering = Vector3.Lerp(rightForearm.position, rightHand.position, 0.5f);
            LastAppliedDownwardDisplacementMeters =
                Vector3.Dot(centerBeforeLowering - centerAfterLowering, up);
            LastLoweringRotationErrorDegrees = Mathf.Max(
                Quaternion.Angle(shoulderRotation, rightShoulder.localRotation),
                Quaternion.Angle(armRotation, rightArm.localRotation),
                Quaternion.Angle(forearmRotation, rightForearm.localRotation),
                Quaternion.Angle(handRotation, rightHand.localRotation));
        }
    }
}
