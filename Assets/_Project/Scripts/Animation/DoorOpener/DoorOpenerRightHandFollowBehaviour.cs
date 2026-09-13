using System;
using System.Collections.Generic;
using Bellerophon.PlayerHands;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [Serializable]
    public struct DoorOpenerBoneRotation
    {
        [SerializeField] private string path;
        [SerializeField] private Quaternion localRotation;

        public DoorOpenerBoneRotation(string bonePath, Quaternion rotation)
        {
            path = bonePath;
            localRotation = rotation;
        }

        public string Path => path;
        public Quaternion LocalRotation => localRotation;
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class DoorOpenerRightHandFollowBehaviour : MonoBehaviour
    {
        // Retain the authored grip while allowing a controlled share of the
        // exact source locomotion to keep the carried upper body from looking frozen.
        public const float RightArmMotionWeight = 0.05f;
        public const float RightForeArmMotionWeight = 0.03f;
        public const float RightHandMotionWeight = 0.01f;

        [SerializeField] private GameObject devicePrefab;
        [SerializeField] private PlayerHandGripPose gripPose;
        [SerializeField] private string rightForeArmPath;
        [SerializeField] private string rightHandPath;
        [SerializeField] private Quaternion authoredForeArmRotation = Quaternion.identity;
        [SerializeField] private Quaternion authoredHandRotation = Quaternion.identity;
        [SerializeField] private Vector3 holderLocalPosition;
        [SerializeField] private Quaternion holderLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 modelLocalPosition;
        [SerializeField] private Quaternion modelLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private Vector3 antennaLocalAxis = Vector3.up;
        [SerializeField] private Vector3 frontLocalAxis = Vector3.forward;
        [SerializeField] private List<DoorOpenerBoneRotation> authoredRightArmPose =
            new List<DoorOpenerBoneRotation>();
        [SerializeField] private List<DoorOpenerBoneRotation> sourceRightArmPose =
            new List<DoorOpenerBoneRotation>();

        [NonSerialized] private Transform rightForeArm;
        [NonSerialized] private Transform rightHand;
        [NonSerialized] private GameObject holder;
        [NonSerialized] private Transform model;
        [NonSerialized] private GameObject instantiatedPrefab;

        public GameObject DevicePrefab => devicePrefab;
        public PlayerHandGripPose GripPose => gripPose;
        public string RightForeArmPath => rightForeArmPath;
        public string RightHandPath => rightHandPath;
        public Quaternion AuthoredForeArmRotation => authoredForeArmRotation;
        public Quaternion AuthoredHandRotation => authoredHandRotation;
        public Vector3 HolderLocalPosition => holderLocalPosition;
        public Quaternion HolderLocalRotation => holderLocalRotation;
        public Vector3 ModelLocalPosition => modelLocalPosition;
        public Quaternion ModelLocalRotation => modelLocalRotation;
        public Vector3 ModelLocalScale => modelLocalScale;
        public Vector3 AntennaLocalAxis => antennaLocalAxis;
        public Vector3 FrontLocalAxis => frontLocalAxis;
        public IReadOnlyList<DoorOpenerBoneRotation> AuthoredRightArmPose =>
            authoredRightArmPose;
        public IReadOnlyList<DoorOpenerBoneRotation> SourceRightArmPose =>
            sourceRightArmPose;
        public Transform RightHand => rightHand;
        public GameObject Holder => holder;
        public Transform Model => model;

        public void Configure(
            GameObject prefab,
            PlayerHandGripPose authoredGripPose,
            string foreArmPath,
            string handPath,
            Quaternion foreArmRotation,
            Quaternion handRotation,
            Vector3 propLocalPosition,
            Quaternion propLocalRotation,
            Vector3 sourceModelLocalPosition,
            Quaternion sourceModelLocalRotation,
            Vector3 scaledModelLocalScale,
            Vector3 sourceAntennaLocalAxis,
            Vector3 sourceFrontLocalAxis)
        {
            devicePrefab = prefab;
            gripPose = authoredGripPose;
            rightForeArmPath = foreArmPath;
            rightHandPath = handPath;
            authoredForeArmRotation = foreArmRotation;
            authoredHandRotation = handRotation;
            holderLocalPosition = propLocalPosition;
            holderLocalRotation = propLocalRotation;
            modelLocalPosition = sourceModelLocalPosition;
            modelLocalRotation = sourceModelLocalRotation;
            modelLocalScale = scaledModelLocalScale;
            antennaLocalAxis = sourceAntennaLocalAxis.normalized;
            frontLocalAxis = sourceFrontLocalAxis.normalized;
            rightForeArm = null;
            rightHand = null;
            RefreshPreview();
        }

        public void ConfigureLocomotionPose(
            IEnumerable<DoorOpenerBoneRotation> authoredPose,
            IEnumerable<DoorOpenerBoneRotation> sourcePose)
        {
            authoredRightArmPose = new List<DoorOpenerBoneRotation>(authoredPose);
            sourceRightArmPose = new List<DoorOpenerBoneRotation>(sourcePose);
        }

        public Vector3 ExpectedWorldPosition()
        {
            ResolveRig();
            return rightHand.TransformPoint(holderLocalPosition);
        }

        public Quaternion ExpectedWorldRotation()
        {
            ResolveRig();
            return rightHand.rotation * holderLocalRotation;
        }

        public void RefreshPreview()
        {
            if (!isActiveAndEnabled || devicePrefab == null || gripPose == null ||
                string.IsNullOrEmpty(rightForeArmPath) || string.IsNullOrEmpty(rightHandPath))
                return;
            ResolveRig();
            if (Application.isPlaying) ApplyAnimatedRightArmPose();
            else ApplyAuthoredRightArmPose();
            ApplyFingerGrip();
            EnsureInstance();
            ApplyPropPose();
        }

#if UNITY_EDITOR
        public void RefreshAnimatedPreviewForValidation()
        {
            if (!isActiveAndEnabled || devicePrefab == null || gripPose == null ||
                string.IsNullOrEmpty(rightForeArmPath) || string.IsNullOrEmpty(rightHandPath))
                return;
            ResolveRig();
            ApplyAnimatedRightArmPose();
            ApplyFingerGrip();
            EnsureInstance();
            ApplyPropPose();
        }
#endif

        private void OnEnable()
        {
            rightForeArm = null;
            rightHand = null;
            RefreshPreview();
        }

        private void OnDisable()
        {
            DestroyInstance();
        }

        private void LateUpdate()
        {
            RefreshPreview();
        }

        private void OnAnimatorMove()
        {
            RefreshPreview();
        }

        private void ResolveRig()
        {
            if (rightForeArm == null) rightForeArm = transform.Find(rightForeArmPath);
            if (rightHand == null) rightHand = transform.Find(rightHandPath);
            if (rightForeArm == null)
                throw new MissingReferenceException(
                    name + " right forearm is missing: " + rightForeArmPath);
            if (rightHand == null)
                throw new MissingReferenceException(
                    name + " right hand is missing: " + rightHandPath);
        }

        private void ApplyAuthoredRightArmPose()
        {
            if (authoredRightArmPose.Count > 0)
            {
                foreach (DoorOpenerBoneRotation pose in authoredRightArmPose)
                {
                    Transform bone = transform.Find(pose.Path) ??
                        throw new MissingReferenceException(
                            name + " authored carry bone is missing: " + pose.Path);
                    SetLocalRotationIfDifferent(bone, pose.LocalRotation);
                }
                return;
            }
            SetLocalRotationIfDifferent(rightForeArm, authoredForeArmRotation);
            SetLocalRotationIfDifferent(rightHand, authoredHandRotation);
        }

        private void ApplyAnimatedRightArmPose()
        {
            if (authoredRightArmPose.Count == 0 && sourceRightArmPose.Count == 0)
            {
                ApplyAuthoredRightArmPose();
                return;
            }
            if (authoredRightArmPose.Count != sourceRightArmPose.Count)
                throw new InvalidOperationException(
                    name + " DoorOpener locomotion pose list lengths differ.");
            for (int index = 0; index < authoredRightArmPose.Count; index++)
            {
                DoorOpenerBoneRotation authored = authoredRightArmPose[index];
                DoorOpenerBoneRotation source = sourceRightArmPose[index];
                if (authored.Path != source.Path)
                    throw new InvalidOperationException(
                        name + " DoorOpener locomotion pose paths differ at " + index + ".");
                Transform bone = transform.Find(authored.Path) ??
                    throw new MissingReferenceException(
                        name + " animated carry bone is missing: " + authored.Path);
                Quaternion animatedDelta =
                    Quaternion.Inverse(source.LocalRotation) * bone.localRotation;
                Quaternion retainedMotion = Quaternion.SlerpUnclamped(
                    Quaternion.identity,
                    animatedDelta,
                    MotionWeightForPath(authored.Path));
                SetLocalRotationIfDifferent(
                    bone, authored.LocalRotation * retainedMotion);
            }
        }

        private void ApplyFingerGrip()
        {
            foreach (PlayerHandGripPose.JointPose joint in gripPose.Joints)
            {
                if (!joint.BoneName.StartsWith("Right", StringComparison.Ordinal)) continue;
                Transform bone = FindDescendant(rightHand, joint.BoneName);
                if (bone == null)
                    throw new MissingReferenceException(
                        name + " grip bone is missing: " + joint.BoneName);
                SetLocalRotationIfDifferent(bone, joint.LocalRotation);
            }
        }

        private static float MotionWeightForPath(string path)
        {
            if (path.EndsWith("/RightHand", StringComparison.Ordinal))
                return RightHandMotionWeight;
            if (path.EndsWith("/RightForeArm", StringComparison.Ordinal))
                return RightForeArmMotionWeight;
            return RightArmMotionWeight;
        }

        private void EnsureInstance()
        {
            if (holder != null && model != null && instantiatedPrefab == devicePrefab) return;
            DestroyInstance();
            holder = new GameObject("DoorOpener_Prop");
            holder.transform.SetParent(rightHand, false);
            instantiatedPrefab = devicePrefab;
            GameObject modelObject = Instantiate(devicePrefab, holder.transform, false);
            modelObject.name = "DoorOpener_Model";
            model = modelObject.transform;
            if (!Application.isPlaying)
                SetHideFlagsRecursively(holder, HideFlags.DontSaveInEditor);
        }

        private void ApplyPropPose()
        {
            SetLocalPositionIfDifferent(holder.transform, holderLocalPosition);
            SetLocalRotationIfDifferent(holder.transform, holderLocalRotation);
            SetLocalScaleIfDifferent(holder.transform, Vector3.one);
            SetLocalPositionIfDifferent(model, modelLocalPosition);
            SetLocalRotationIfDifferent(model, modelLocalRotation);
            SetLocalScaleIfDifferent(model, modelLocalScale);
        }

        private static void SetLocalPositionIfDifferent(
            Transform target,
            Vector3 value)
        {
            if ((target.localPosition - value).sqrMagnitude > 0.0000000001f)
                target.localPosition = value;
        }

        private static void SetLocalRotationIfDifferent(
            Transform target,
            Quaternion value)
        {
            if (Quaternion.Angle(target.localRotation, value) > 0.0001f)
                target.localRotation = value;
        }

        private static void SetLocalScaleIfDifferent(
            Transform target,
            Vector3 value)
        {
            if ((target.localScale - value).sqrMagnitude > 0.0000000001f)
                target.localScale = value;
        }

        private void DestroyInstance()
        {
            if (holder == null) return;
            if (Application.isPlaying) Destroy(holder);
            else DestroyImmediate(holder);
            holder = null;
            model = null;
            instantiatedPrefab = null;
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                if (item.name == targetName) return item;
            return null;
        }

        private static void SetHideFlagsRecursively(GameObject root, HideFlags flags)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.hideFlags = flags;
        }
    }
}
