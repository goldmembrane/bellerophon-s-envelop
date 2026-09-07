using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DefaultExecutionOrder(10800)]
    [DisallowMultipleComponent]
    public sealed class ShieldHeadForward : MonoBehaviour
    {
        [SerializeField] private Transform transporter;
        [SerializeField] private Transform head;
        [SerializeField] private Vector3 localFaceAxis = Vector3.forward;
        [SerializeField] private Vector3 localUpAxis = Vector3.up;

        public float LastForwardAlignment { get; private set; }
        public float LastUpAlignment { get; private set; }

        public void Configure(Transform transporterRoot, Transform headBone, Vector3 faceAxis, Vector3 upAxis)
        {
            transporter = transporterRoot;
            head = headBone;
            localFaceAxis = faceAxis.normalized;
            localUpAxis = upAxis.normalized;
            AlignImmediately();
        }

        public void AlignImmediately()
        {
            if (transporter == null || head == null) return;
            Vector3 currentFace = head.TransformDirection(localFaceAxis).normalized;
            Vector3 currentUp = head.TransformDirection(localUpAxis).normalized;
            Quaternion currentBasis = Quaternion.LookRotation(currentFace, currentUp);
            Quaternion desiredBasis = Quaternion.LookRotation(transporter.forward, transporter.up);
            head.rotation = desiredBasis * Quaternion.Inverse(currentBasis) * head.rotation;
            LastForwardAlignment = Vector3.Dot(head.TransformDirection(localFaceAxis).normalized,
                transporter.forward.normalized);
            LastUpAlignment = Vector3.Dot(head.TransformDirection(localUpAxis).normalized,
                transporter.up.normalized);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            AlignImmediately();
        }
    }
}
