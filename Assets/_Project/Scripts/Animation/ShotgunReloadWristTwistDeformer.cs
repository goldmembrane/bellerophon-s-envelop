using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    /// <summary>
    /// Distributes only the axial LeftHand rotation across a deformation-only
    /// helper bone. Existing animated bones are read but never modified.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class ShotgunReloadWristTwistDeformer : MonoBehaviour
    {
        [SerializeField] private Transform leftForeArm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform twistBone;
        [SerializeField] private Quaternion bindHandLocalRotation = Quaternion.identity;
        [SerializeField] private Quaternion bindTwistLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 twistAxisInForeArm = Vector3.right;
        [SerializeField, Range(0f, 1f)] private float twistFraction = 0.5f;

        public void Configure(
            Transform foreArm,
            Transform hand,
            Transform helper,
            Vector3 axisInForeArm,
            float fraction)
        {
            leftForeArm = foreArm;
            leftHand = hand;
            twistBone = helper;
            bindHandLocalRotation = hand != null
                ? hand.localRotation
                : Quaternion.identity;
            bindTwistLocalRotation = helper != null
                ? helper.localRotation
                : Quaternion.identity;
            twistAxisInForeArm = axisInForeArm.sqrMagnitude > 0.000001f
                ? axisInForeArm.normalized
                : Vector3.right;
            twistFraction = Mathf.Clamp01(fraction);
        }

        public void EvaluateNow()
        {
            if (leftForeArm == null ||
                leftHand == null ||
                twistBone == null ||
                leftHand.parent != leftForeArm ||
                twistBone.parent != leftForeArm)
            {
                return;
            }

            Quaternion relative =
                leftHand.localRotation * Quaternion.Inverse(bindHandLocalRotation);
            Quaternion twist = ExtractTwist(relative, twistAxisInForeArm);
            twistBone.localRotation =
                Quaternion.SlerpUnclamped(
                    Quaternion.identity,
                    twist,
                    twistFraction) *
                bindTwistLocalRotation;
        }

        private void LateUpdate()
        {
            EvaluateNow();
        }

        private static Quaternion ExtractTwist(
            Quaternion rotation,
            Vector3 normalizedAxis)
        {
            Vector3 vector = new Vector3(rotation.x, rotation.y, rotation.z);
            Vector3 projected = Vector3.Project(vector, normalizedAxis);
            Quaternion twist = new Quaternion(
                projected.x,
                projected.y,
                projected.z,
                rotation.w);
            float magnitude = Mathf.Sqrt(
                twist.x * twist.x +
                twist.y * twist.y +
                twist.z * twist.z +
                twist.w * twist.w);
            if (magnitude <= 0.000001f)
            {
                return Quaternion.identity;
            }

            float inverse = 1f / magnitude;
            twist.x *= inverse;
            twist.y *= inverse;
            twist.z *= inverse;
            twist.w *= inverse;
            if (twist.w < 0f)
            {
                twist.x = -twist.x;
                twist.y = -twist.y;
                twist.z = -twist.z;
                twist.w = -twist.w;
            }

            return twist;
        }
    }
}
