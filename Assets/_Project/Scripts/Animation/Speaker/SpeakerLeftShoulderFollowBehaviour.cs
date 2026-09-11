using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SpeakerLeftShoulderFollowBehaviour : MonoBehaviour
    {
        // Keeps visible source motion without treating the handle as a fixed IK target.
        public const float LeftArmMotionWeight = 0.35f;
        public const float LeftForeArmMotionWeight = 0.25f;
        public const float LeftHandMotionWeight = 0.15f;
        public const float PalmRightMaximumDeviationDegrees = 10f;

        [SerializeField] private SpeakerIdleCarryProfile profile;

        private Transform leftShoulder;
        private GameObject speakerHolder;
        private GameObject instantiatedPrefab;
        private Transform speakerModel;

        public SpeakerIdleCarryProfile Profile => profile;
        public GameObject SpeakerHolder => speakerHolder;
        public Transform SpeakerModel => speakerModel;
        public static bool EditModeSpeakerTransformLocked => false;
        public static bool LeftHandGripLocked => false;

        public void Configure(SpeakerIdleCarryProfile carryProfile)
        {
            profile = carryProfile;
            leftShoulder = null;
            RefreshPreview();
        }

        public void RefreshPreview()
        {
            if (!isActiveAndEnabled || profile == null) return;
            ResolveShoulder();
            ApplyLeftArmPose();
            EnsureSpeakerInstance();
            ApplySpeakerPose();
        }

        public Vector3 ExpectedWorldPosition()
        {
            ResolveShoulder();
            return leftShoulder.TransformPoint(profile.PositionOffsetInShoulderSpace);
        }

        public Quaternion ExpectedWorldRotation()
        {
            ResolveShoulder();
            return leftShoulder.rotation * profile.RotationOffsetFromShoulder;
        }

        public Vector3 HandleWorldPosition()
        {
            EnsureSpeakerInstance();
            return speakerHolder.transform.TransformPoint(profile.HandlePointInHolderSpace);
        }

        public Vector3 HandleWorldAxis()
        {
            EnsureSpeakerInstance();
            return speakerHolder.transform.TransformDirection(
                profile.HandleAxisInHolderSpace).normalized;
        }

        private void OnEnable()
        {
            leftShoulder = null;
            RefreshPreview();
        }

        private void OnDisable()
        {
            DestroySpeakerInstance();
        }

        private void LateUpdate()
        {
            if (profile == null) return;
            ResolveShoulder();
            if (!Application.isPlaying) return;
            ApplyAnimatedLeftArmOffset();
            EnsureSpeakerInstance();
            ApplySpeakerPose();
        }

        private void ApplyAnimatedLeftArmOffset()
        {
            foreach (SpeakerBoneRotation carryPose in profile.LeftArmPose)
            {
                Transform bone = transform.Find(carryPose.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        "Speaker left-arm pose path could not be resolved: " + carryPose.Path);
                if (!TryFindSourcePose(carryPose.Path, out Quaternion sourceRotation))
                    throw new MissingReferenceException(
                        "Speaker source left-arm pose path could not be resolved: " +
                        carryPose.Path);

                Quaternion animatedDelta =
                    Quaternion.Inverse(sourceRotation) * bone.localRotation;
                float motionWeight = MotionWeightForPath(carryPose.Path);
                Quaternion retainedMotion = Quaternion.SlerpUnclamped(
                    Quaternion.identity, animatedDelta, motionWeight);
                bone.localRotation = carryPose.LocalRotation * retainedMotion;
            }
            ConstrainPalmToPlayerRight();
        }

        private static float MotionWeightForPath(string path)
        {
            if (path.EndsWith("/LeftHand", System.StringComparison.Ordinal))
                return LeftHandMotionWeight;
            if (path.EndsWith("/LeftForeArm", System.StringComparison.Ordinal))
                return LeftForeArmMotionWeight;
            return LeftArmMotionWeight;
        }

        private void ConstrainPalmToPlayerRight()
        {
            Transform hand = FindDescendant("LeftHand");
            Transform index = FindDescendant("LeftIndexProximal");
            Transform middle = FindDescendant("LeftMiddleProximal");
            Transform little = FindDescendant("LeftLittleProximal");
            Vector3 fingerDirection = (middle.position - hand.position).normalized;
            Vector3 palmWidth = (little.position - index.position).normalized;
            Vector3 palmNormal = Vector3.Cross(fingerDirection, palmWidth).normalized;
            Vector3 playerRight = transform.right.normalized;
            float angle = Vector3.Angle(palmNormal, playerRight);
            if (angle <= PalmRightMaximumDeviationDegrees) return;

            Quaternion fullCorrection = Quaternion.FromToRotation(palmNormal, playerRight);
            float correctionFraction =
                (angle - PalmRightMaximumDeviationDegrees) / angle;
            hand.rotation = Quaternion.SlerpUnclamped(
                Quaternion.identity, fullCorrection, correctionFraction) * hand.rotation;
        }

        private Transform FindDescendant(string boneName)
        {
            foreach (Transform descendant in GetComponentsInChildren<Transform>(true))
                if (descendant.name == boneName) return descendant;
            throw new MissingReferenceException(
                "Speaker left-arm bone could not be resolved: " + boneName);
        }

        private bool TryFindSourcePose(string path, out Quaternion localRotation)
        {
            foreach (SpeakerBoneRotation sourcePose in profile.SourceLeftArmPose)
            {
                if (sourcePose.Path != path) continue;
                localRotation = sourcePose.LocalRotation;
                return true;
            }
            localRotation = Quaternion.identity;
            return false;
        }

        private void ApplyLeftArmPose()
        {
            foreach (SpeakerBoneRotation bonePose in profile.LeftArmPose)
            {
                Transform bone = transform.Find(bonePose.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        "Speaker left-arm pose path could not be resolved: " + bonePose.Path);
                bone.localRotation = bonePose.LocalRotation;
            }
        }

        private void ApplySpeakerPose()
        {
            speakerHolder.transform.SetPositionAndRotation(
                ExpectedWorldPosition(), ExpectedWorldRotation());
            speakerHolder.transform.localScale = profile.HolderScale;
            ApplyModelPose();
        }

        private void ApplyModelPose()
        {
            if (speakerModel == null)
                throw new MissingReferenceException("PortableSpeaker model instance is missing.");
            speakerModel.SetLocalPositionAndRotation(
                profile.ModelLocalPosition, profile.ModelLocalRotation);
            speakerModel.localScale = profile.ModelLocalScale;
        }

        private void ResolveShoulder()
        {
            if (profile == null)
                throw new MissingReferenceException("Speaker carry profile is missing.");
            if (leftShoulder == null)
                leftShoulder = transform.Find(profile.LeftShoulderPath);
            if (leftShoulder == null)
                throw new MissingReferenceException(
                    "Speaker left-shoulder path could not be resolved: " +
                    profile.LeftShoulderPath);
        }

        private void EnsureSpeakerInstance()
        {
            if (profile == null || profile.SpeakerPrefab == null)
                throw new MissingReferenceException("Speaker model prefab is missing.");
            if (speakerHolder != null && instantiatedPrefab == profile.SpeakerPrefab)
            {
                if (speakerModel == null)
                    speakerModel = speakerHolder.transform.Find("PortableSpeaker_Model");
                return;
            }
            DestroySpeakerInstance();
            speakerHolder = new GameObject("Speaker_Prop");
            speakerHolder.transform.SetParent(transform, false);
            instantiatedPrefab = profile.SpeakerPrefab;
            GameObject model = Instantiate(profile.SpeakerPrefab, speakerHolder.transform, false);
            model.name = "PortableSpeaker_Model";
            speakerModel = model.transform;
            if (!Application.isPlaying)
                SetHideFlagsRecursively(speakerHolder, HideFlags.DontSaveInEditor);
        }

        private void DestroySpeakerInstance()
        {
            if (speakerHolder == null) return;
            if (Application.isPlaying) Destroy(speakerHolder);
            else DestroyImmediate(speakerHolder);
            speakerHolder = null;
            instantiatedPrefab = null;
            speakerModel = null;
        }

        private static void SetHideFlagsRecursively(GameObject root, HideFlags flags)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.hideFlags = flags;
        }
    }
}
