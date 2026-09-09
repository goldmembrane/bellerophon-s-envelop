using System;
using System.Collections;
using UnityEngine;

namespace Bellerophon.PlayerConsumables
{
    /// <summary>Authors rig controls only. Animation Rigging owns all skeleton rotations.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-10000)]
    public sealed class ConsumableRiggedSharedMotion : MonoBehaviour
    {
        public const float Duration = 5f;
        public const float MinimumReachReserve = 0.045f;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform leftArm, leftForeArm, leftHand;
        [SerializeField] private Transform rightShoulder, rightArm, rightForeArm, rightHand;
        [SerializeField] private Transform leftArmControl, leftHandTarget, contactSocket, rightHandTarget;
        [SerializeField] private Transform rightElbowTarget, rightForearmControl, batteryGrip, batteryPlugTip, batteryPlugBase;
        [SerializeField] private Quaternion leftStartRotation, rightStartRotation, rightContactRotation;
        [SerializeField] private Quaternion leftForeArmRestRotation, leftHandRestRotation, rightHandRestRotation;
        // Model-space rigid offsets derive from the imported bones and actual battery vertices.
        [SerializeField] private Vector3 handOffsetInForearm, plugOffsetInHand, rightStartElbowPosition;
        // Three existing spine joints share torso reach. These are rig controls, never bone writers.
        [SerializeField] private Transform[] torsoControls, rightSeedControls;
        [SerializeField] private Transform rightShoulderHint;
        [SerializeField] private Vector3[] torsoRestPositions;
        [SerializeField] private Quaternion[] torsoRestRotations, rightSeedRestRotations;
        [SerializeField] private Vector3 leftArmRestPosition, rightShoulderRestPosition, rightArmRestPosition, socketOffsetInLeftArm;
        [SerializeField] private float torsoTurnDegrees = -30f;
        [SerializeField] private float leftRestElbowAngle;
        [SerializeField] private float leftRaiseDegrees = -90f;
        [SerializeField] private Vector3 readyOffset = new Vector3(0.120f, 0.040f, -0.025f);
        [SerializeField] private Vector3 liftOffset = new Vector3(0.050f, 0.110f, -0.025f);
        [SerializeField] private Vector3 detachedOffset = new Vector3(0.060f, 0.115f, -0.030f);
        private float automaticTime;
        public float CurrentSampleTime { get; private set; }
        public bool ManualReview => false;
        public Transform ModelRoot => modelRoot;
        public Transform LeftArm => leftArm;
        public Transform LeftForeArm => leftForeArm;
        public Transform LeftHand => leftHand;
        public Transform RightShoulder => rightShoulder;
        public Transform RightArm => rightArm;
        public Transform RightForeArm => rightForeArm;
        public Transform RightHand => rightHand;
        public Transform LeftHandTarget => leftHandTarget;
        public Transform ContactSocket => contactSocket;
        public Transform RightHandTarget => rightHandTarget;
        public Transform RightElbowTarget => rightElbowTarget;
        public Transform BatteryGrip => batteryGrip;
        public Transform BatteryPlugTip => batteryPlugTip;
        public Transform BatteryPlugBase => batteryPlugBase;
        public float LeftRestElbowAngle => leftRestElbowAngle;
        public Quaternion LeftForeArmRestRotation => leftForeArmRestRotation;
        public Quaternion LeftHandRestRotation => leftHandRestRotation;
        public Quaternion RightHandRestRotation => rightHandRestRotation;
        public static event Action<ConsumableRiggedSharedMotion> PoseEvaluated;

        public void Configure(Transform root, Transform lArm, Transform lForearm, Transform lHand,
            Transform rShoulder, Transform rArm, Transform rForearm, Transform rHand,
            Transform armControl, Transform handReference, Transform socket, Transform rTarget,
            Transform elbowTarget, Transform forearmControl, Transform grip, Transform plugTip,
            Transform plugBase, Quaternion contactForearmRotation, Transform[] spineBones,
            Transform[] spineControls, Transform shoulderHint, Transform[] seedControls)
        {
            modelRoot = root;
            leftArm = lArm; leftForeArm = lForearm; leftHand = lHand;
            rightShoulder = rShoulder; rightArm = rArm; rightForeArm = rForearm; rightHand = rHand;
            leftArmControl = armControl; leftHandTarget = handReference;
            contactSocket = socket; rightHandTarget = rTarget;
            rightElbowTarget = elbowTarget; rightForearmControl = forearmControl;
            batteryGrip = grip; batteryPlugTip = plugTip; batteryPlugBase = plugBase;
            leftStartRotation = Quaternion.Inverse(root.rotation) * lArm.rotation;
            rightStartRotation = Quaternion.Inverse(root.rotation) * rForearm.rotation;
            rightContactRotation = contactForearmRotation;
            leftForeArmRestRotation = lForearm.localRotation;
            leftHandRestRotation = lHand.localRotation;
            rightHandRestRotation = rHand.localRotation;
            leftRestElbowAngle = Vector3.Angle(lArm.position - lForearm.position, lHand.position - lForearm.position);
            handOffsetInForearm = Quaternion.Inverse(rightStartRotation) *
                root.InverseTransformVector(rHand.position - rForearm.position);
            Quaternion handRotation = Quaternion.Inverse(root.rotation) * rHand.rotation;
            plugOffsetInHand = Quaternion.Inverse(handRotation) *
                root.InverseTransformVector(plugTip.position - rHand.position);
            rightStartElbowPosition = root.InverseTransformPoint(rForearm.position);
            // Start with a softly bent elbow rather than the nearly locked original rest arm.
            rightStartRotation = Quaternion.AngleAxis(-18f, Vector3.right) * rightStartRotation;
            torsoControls=spineControls;rightSeedControls=seedControls;rightShoulderHint=shoulderHint;
            torsoRestPositions=new Vector3[3];torsoRestRotations=new Quaternion[3];
            for(int i=0;i<3;i++)
            {torsoRestPositions[i]=root.InverseTransformPoint(spineBones[i].position);torsoRestRotations[i]=Quaternion.Inverse(root.rotation)*spineBones[i].rotation;}
            rightSeedRestRotations=new[]{Quaternion.Inverse(root.rotation)*rShoulder.rotation,Quaternion.Inverse(root.rotation)*rArm.rotation};
            leftArmRestPosition=root.InverseTransformPoint(lArm.position);
            rightShoulderRestPosition=root.InverseTransformPoint(rShoulder.position);rightArmRestPosition=root.InverseTransformPoint(rArm.position);
            socketOffsetInLeftArm=Quaternion.Inverse(leftStartRotation)*root.InverseTransformVector(socket.position-lArm.position);
            automaticTime = 0f;
            ApplyControls(0f);
        }

        private bool Ready => modelRoot != null && leftArmControl != null && contactSocket != null &&
            rightHandTarget != null && rightElbowTarget != null && rightForearmControl != null && batteryPlugTip != null &&
            torsoControls != null && torsoControls.Length == 3 && rightShoulderHint != null;
        private void OnEnable()
        {
            automaticTime = 0f;
            if (Ready) ApplyControls(0f);
#if UNITY_EDITOR
            if (Application.isPlaying) StartCoroutine(ReportRenderedPose());
#endif
        }
        private void Update()
        {
            if (!Application.isPlaying || !Ready) return;
            automaticTime += Time.deltaTime;
            ApplyControls(Mathf.Repeat(automaticTime, Duration));
        }
        private IEnumerator ReportRenderedPose()
        {
            var rendered = new WaitForEndOfFrame();
            while (enabled)
            {
                yield return rendered;
                if (Ready) PoseEvaluated?.Invoke(this);
            }
        }

        public struct AuthoredRigPose
        {
            public Quaternion Spine0,Spine1,Spine2,TorsoDelta,LeftArmRotation,ForearmRotation,HandRotation;
            public Vector3 LeftArmOrigin,ShoulderOrigin,ShoulderHint,Elbow,Hand,Plug;
        }

        // Pure authoring/target evaluation. Does not sample an Animator or change a validation target.
        public AuthoredRigPose GetAuthoredRigPose(float time, Quaternion? contactRotationOverride = null)
        {
            Quaternion contactRotation = contactRotationOverride ?? rightContactRotation;
            float rise=Ease(time/.5f);
            var pose=new AuthoredRigPose
            {
                Spine0=Quaternion.AngleAxis(torsoTurnDegrees*rise*.30f,Vector3.up)*torsoRestRotations[0],
                Spine1=Quaternion.AngleAxis(torsoTurnDegrees*rise*.65f,Vector3.up)*torsoRestRotations[1],
                Spine2=Quaternion.AngleAxis(torsoTurnDegrees*rise,Vector3.up)*torsoRestRotations[2],
                LeftArmRotation=Quaternion.AngleAxis(leftRaiseDegrees*rise,Vector3.right)*leftStartRotation
            };
            Vector3 spinePosition=torsoRestPositions[0]+pose.Spine0*Quaternion.Inverse(torsoRestRotations[0])*(torsoRestPositions[1]-torsoRestPositions[0]);
            spinePosition+=pose.Spine1*Quaternion.Inverse(torsoRestRotations[1])*(torsoRestPositions[2]-torsoRestPositions[1]);
            pose.TorsoDelta=pose.Spine2*Quaternion.Inverse(torsoRestRotations[2]);
            Vector3 Moved(Vector3 rest)=>spinePosition+pose.TorsoDelta*(rest-torsoRestPositions[2]);
            pose.LeftArmOrigin=Moved(leftArmRestPosition);pose.ShoulderOrigin=Moved(rightShoulderRestPosition);
            pose.ShoulderHint=Moved(rightArmRestPosition);
            Vector3 contact=pose.LeftArmOrigin+pose.LeftArmRotation*socketOffsetInLeftArm;
            Quaternion startRotation=pose.TorsoDelta*rightStartRotation;
            pose.ForearmRotation=Quaternion.Slerp(startRotation,contactRotation,Ease((time-.5f)/.5f));
            pose.HandRotation=pose.ForearmRotation*rightHandRestRotation;
            Vector3 rigidOffset=pose.HandRotation*plugOffsetInHand+pose.ForearmRotation*handOffsetInForearm;
            Vector3 plugPosition;
            if(time<1f)
            {
                float travel=Ease((time-.5f)/.5f);
                Vector3 endElbow=contact+readyOffset-contactRotation*rightHandRestRotation*plugOffsetInHand-contactRotation*handOffsetInForearm;
                pose.Elbow=Vector3.Lerp(Moved(rightStartElbowPosition),endElbow,travel)+Vector3.forward*(.10f*Mathf.Sin(Mathf.PI*travel));
                plugPosition=pose.Elbow+rigidOffset;
            }
            else if (time < 1.2f) plugPosition = Vector3.Lerp(contact + readyOffset, contact + liftOffset, Ease((time - 1f) / 0.2f));
            else if (time < 1.7f) plugPosition = Vector3.Lerp(contact + liftOffset, contact, Ease((time - 1.2f) / 0.5f));
            else if (time < 3f) plugPosition = contact;
            else if (time < 3.5f) plugPosition = Vector3.Lerp(contact, contact + detachedOffset, Ease((time - 3f) / 0.5f));
            else plugPosition = contact + detachedOffset;

            pose.Plug=plugPosition;pose.Hand=plugPosition-pose.HandRotation*plugOffsetInHand;
            pose.Elbow=plugPosition-rigidOffset;
            return pose;
        }
        private void ApplyControls(float time)
        {
            CurrentSampleTime=time;AuthoredRigPose pose=GetAuthoredRigPose(time);
            torsoControls[0].localRotation=pose.Spine0;torsoControls[1].localRotation=pose.Spine1;torsoControls[2].localRotation=pose.Spine2;
            for(int i=0;i<2;i++)rightSeedControls[i].localRotation=pose.TorsoDelta*rightSeedRestRotations[i];
            rightShoulderHint.localPosition=pose.ShoulderHint;
            leftArmControl.localPosition=pose.LeftArmOrigin;leftArmControl.localRotation=pose.LeftArmRotation;
            // IK positions the elbow; the forearm control transports the unchanged wrist
            // and rigid prop together. No independent downward wrist rotation is used.
            rightElbowTarget.localPosition=pose.Elbow;rightForearmControl.localRotation=pose.ForearmRotation;
            rightHandTarget.localPosition=pose.Hand;rightHandTarget.localRotation=pose.HandRotation;
        }
        private static float Ease(float value) { value = Mathf.Clamp01(value); return value * value * (3f - 2f * value); }
    }
}
