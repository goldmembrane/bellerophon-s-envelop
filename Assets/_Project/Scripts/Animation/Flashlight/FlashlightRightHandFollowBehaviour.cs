using System;
using System.Collections.Generic;
using Bellerophon.PlayerHands;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [Serializable]
    public struct FlashlightBoneRotation
    {
        [SerializeField] private string path;
        [SerializeField] private Quaternion localRotation;

        public FlashlightBoneRotation(string bonePath, Quaternion rotation)
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
    public sealed class FlashlightRightHandFollowBehaviour : MonoBehaviour
    {
        // Match the established carried-item locomotion retention: the authored
        // arm remains dominant while exact source clips contribute visible sway.
        public const float RightArmMotionWeight = 0.05f;
        public const float RightForeArmMotionWeight = 0.03f;
        public const float RightHandMotionWeight = 0.01f;

        [SerializeField] private GameObject flashlightPrefab;
        [SerializeField] private PlayerHandGripPose fistGripPose;
        [SerializeField] private string rightHandPath;
        [SerializeField] private Vector3 holderLocalPosition;
        [SerializeField] private Quaternion holderLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 modelLocalPosition;
        [SerializeField] private Quaternion modelLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private List<FlashlightBoneRotation> authoredRightArmPose =
            new List<FlashlightBoneRotation>();
        [SerializeField] private List<FlashlightBoneRotation> sourceRightArmPose =
            new List<FlashlightBoneRotation>();

        private Transform rightHand;
        private GameObject holder;
        private Transform model;
        private GameObject instantiatedPrefab;

        public GameObject FlashlightPrefab => flashlightPrefab;
        public PlayerHandGripPose FistGripPose => fistGripPose;
        public string RightHandPath => rightHandPath;
        public Vector3 HolderLocalPosition => holderLocalPosition;
        public Quaternion HolderLocalRotation => holderLocalRotation;
        public Vector3 ModelLocalPosition => modelLocalPosition;
        public Quaternion ModelLocalRotation => modelLocalRotation;
        public Vector3 ModelLocalScale => modelLocalScale;
        public Transform RightHand => rightHand;
        public GameObject Holder => holder;
        public Transform Model => model;
        public IReadOnlyList<FlashlightBoneRotation> AuthoredRightArmPose =>
            authoredRightArmPose;
        public IReadOnlyList<FlashlightBoneRotation> SourceRightArmPose =>
            sourceRightArmPose;

        public void Configure(
            GameObject sourcePrefab,
            PlayerHandGripPose gripPose,
            string handPath,
            Vector3 propLocalPosition,
            Quaternion propLocalRotation,
            Vector3 sourceModelLocalPosition,
            Quaternion sourceModelLocalRotation,
            Vector3 scaledModelLocalScale)
        {
            flashlightPrefab = sourcePrefab;
            fistGripPose = gripPose;
            rightHandPath = handPath;
            holderLocalPosition = propLocalPosition;
            holderLocalRotation = propLocalRotation;
            modelLocalPosition = sourceModelLocalPosition;
            modelLocalRotation = sourceModelLocalRotation;
            modelLocalScale = scaledModelLocalScale;
            rightHand = null;
            RefreshPreview();
        }

        public void ConfigureLocomotionPose(
            IEnumerable<FlashlightBoneRotation> authoredPose,
            IEnumerable<FlashlightBoneRotation> sourcePose)
        {
            authoredRightArmPose = new List<FlashlightBoneRotation>(authoredPose);
            sourceRightArmPose = new List<FlashlightBoneRotation>(sourcePose);
        }

        public void RefreshPreview()
        {
            if (!isActiveAndEnabled || flashlightPrefab == null || fistGripPose == null ||
                string.IsNullOrEmpty(rightHandPath)) return;
            ResolveHand();
            if (Application.isPlaying) ApplyAnimatedRightArmOffset();
            else ApplyAuthoredRightArmPose();
            ApplyFistPose();
            EnsureInstance();
            ApplyPropPose();
        }

        private void OnEnable()
        {
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

        private void ResolveHand()
        {
            if (rightHand == null) rightHand = transform.Find(rightHandPath);
            if (rightHand == null)
                throw new MissingReferenceException(
                    name + " right hand is missing: " + rightHandPath);
        }

        private void ApplyFistPose()
        {
            foreach (PlayerHandGripPose.JointPose joint in fistGripPose.Joints)
            {
                if (!joint.BoneName.StartsWith("Right", System.StringComparison.Ordinal))
                    continue;
                Transform bone = FindDescendant(rightHand, joint.BoneName);
                if (bone == null)
                    throw new MissingReferenceException(
                        name + " grip bone is missing: " + joint.BoneName);
                bone.localRotation = joint.LocalRotation;
            }
        }

        private void ApplyAnimatedRightArmOffset()
        {
            if (authoredRightArmPose.Count == 0 && sourceRightArmPose.Count == 0) return;
            if (authoredRightArmPose.Count != sourceRightArmPose.Count)
                throw new InvalidOperationException(
                    name + " flashlight locomotion pose list lengths differ.");

            for (int index = 0; index < authoredRightArmPose.Count; index++)
            {
                FlashlightBoneRotation authored = authoredRightArmPose[index];
                FlashlightBoneRotation source = sourceRightArmPose[index];
                if (authored.Path != source.Path)
                    throw new InvalidOperationException(
                        name + " flashlight locomotion pose paths differ at " + index + ".");
                Transform bone = transform.Find(authored.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        name + " flashlight locomotion bone is missing: " + authored.Path);
                Quaternion animatedDelta =
                    Quaternion.Inverse(source.LocalRotation) * bone.localRotation;
                Quaternion retainedMotion = Quaternion.SlerpUnclamped(
                    Quaternion.identity,
                    animatedDelta,
                    MotionWeightForPath(authored.Path));
                bone.localRotation = authored.LocalRotation * retainedMotion;
            }
        }

        private void ApplyAuthoredRightArmPose()
        {
            foreach (FlashlightBoneRotation authored in authoredRightArmPose)
            {
                Transform bone = transform.Find(authored.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        name + " flashlight authored-pose bone is missing: " + authored.Path);
                bone.localRotation = authored.LocalRotation;
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
            if (holder != null && model != null && instantiatedPrefab == flashlightPrefab) return;
            DestroyInstance();
            holder = new GameObject("Flashlight_Prop");
            holder.transform.SetParent(rightHand, false);
            instantiatedPrefab = flashlightPrefab;
            GameObject modelObject = Instantiate(flashlightPrefab, holder.transform, false);
            modelObject.name = "Flashlight_Model";
            model = modelObject.transform;
            if (!Application.isPlaying)
                SetHideFlagsRecursively(holder, HideFlags.DontSaveInEditor);
        }

        private void ApplyPropPose()
        {
            holder.transform.localPosition = holderLocalPosition;
            holder.transform.localRotation = holderLocalRotation;
            holder.transform.localScale = Vector3.one;
            model.localPosition = modelLocalPosition;
            model.localRotation = modelLocalRotation;
            model.localScale = modelLocalScale;
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
