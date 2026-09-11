using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class HoloSprayRightHandFollowBehaviour : MonoBehaviour
    {
        // Preserve authored grip while retaining a controlled share of source locomotion sway.
        public const float RightArmMotionWeight = 0.05f;
        public const float RightForeArmMotionWeight = 0.03f;
        public const float RightHandMotionWeight = 0.01f;
        public const float FingerMotionWeight = 0f;
        public const float PalmLeftMaximumDeviationDegrees = 10f;
        public const float SprayRotationFollowWeight = 0.45f;

        [SerializeField] private HoloSprayIdleCarryProfile profile;

        private Transform rightHand;
        private GameObject sprayHolder;
        private GameObject instantiatedPrefab;
        private Transform sprayModel;

        public HoloSprayIdleCarryProfile Profile => profile;
        public GameObject SprayHolder => sprayHolder;
        public Transform SprayModel => sprayModel;

        public void Configure(HoloSprayIdleCarryProfile carryProfile)
        {
            profile = carryProfile;
            rightHand = null;
            RefreshPreview();
        }

        public void RefreshPreview()
        {
            if (!isActiveAndEnabled || profile == null) return;
            ResolveHand();
            ApplyRightArmPose();
            EnsureSprayInstance();
            ApplySprayPose();
        }

        public Vector3 ExpectedWorldPosition()
        {
            ResolveHand();
            return rightHand.TransformPoint(profile.PositionOffsetInHandSpace);
        }

        public Quaternion ExpectedWorldRotation()
        {
            ResolveHand();
            Quaternion handRotation =
                rightHand.rotation * profile.RotationOffsetFromHand;
            if (!Application.isPlaying) return handRotation;
            Quaternion uprightForward = Quaternion.LookRotation(
                transform.forward.normalized, transform.up.normalized);
            return Quaternion.SlerpUnclamped(
                uprightForward, handRotation, SprayRotationFollowWeight);
        }

        private void OnEnable()
        {
            rightHand = null;
            RefreshPreview();
        }

        private void OnDisable()
        {
            DestroySprayInstance();
        }

        private void LateUpdate()
        {
            ApplyRuntimePose();
        }

        private void OnAnimatorMove()
        {
            ApplyRuntimePose();
        }

        private void ApplyRuntimePose()
        {
            if (profile == null || !Application.isPlaying) return;
            ResolveHand();
            ApplyAnimatedRightArmOffset();
            EnsureSprayInstance();
            ApplySprayPose();
        }

        private void ApplyAnimatedRightArmOffset()
        {
            foreach (HoloSprayBoneRotation carryPose in profile.RightArmPose)
            {
                Transform bone = transform.Find(carryPose.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        "HoloSpray right-arm pose path could not be resolved: " +
                        carryPose.Path);
                if (!TryFindSourcePose(carryPose.Path, out Quaternion sourceRotation))
                    throw new MissingReferenceException(
                        "HoloSpray source right-arm pose path could not be resolved: " +
                        carryPose.Path);

                Quaternion animatedDelta =
                    Quaternion.Inverse(sourceRotation) * bone.localRotation;
                Quaternion retainedMotion = Quaternion.SlerpUnclamped(
                    Quaternion.identity,
                    animatedDelta,
                    MotionWeightForPath(carryPose.Path));
                bone.localRotation = carryPose.LocalRotation * retainedMotion;
            }
            ConstrainPalmToPlayerLeft();
        }

        private static float MotionWeightForPath(string path)
        {
            if (path.IndexOf("/RightHand/", System.StringComparison.Ordinal) >= 0)
                return FingerMotionWeight;
            if (path.EndsWith("/RightHand", System.StringComparison.Ordinal))
                return RightHandMotionWeight;
            if (path.EndsWith("/RightForeArm", System.StringComparison.Ordinal))
                return RightForeArmMotionWeight;
            return RightArmMotionWeight;
        }

        private bool TryFindSourcePose(string path, out Quaternion localRotation)
        {
            foreach (HoloSprayBoneRotation sourcePose in profile.SourceRightArmPose)
            {
                if (sourcePose.Path != path) continue;
                localRotation = sourcePose.LocalRotation;
                return true;
            }
            localRotation = Quaternion.identity;
            return false;
        }

        private void ConstrainPalmToPlayerLeft()
        {
            Transform hand = FindDescendant("RightHand");
            Transform index = FindDescendant("RightIndexProximal");
            Transform middle = FindDescendant("RightMiddleProximal");
            Transform little = FindDescendant("RightLittleProximal");
            Vector3 playerLeft = -transform.right.normalized;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                Vector3 fingerDirection =
                    (middle.position - hand.position).normalized;
                Vector3 palmWidth = (little.position - index.position).normalized;
                Vector3 palmNormal =
                    Vector3.Cross(palmWidth, fingerDirection).normalized;
                if (Vector3.Angle(palmNormal, playerLeft) <=
                    PalmLeftMaximumDeviationDegrees) return;
                hand.rotation = Quaternion.FromToRotation(
                    palmNormal, playerLeft) * hand.rotation;
            }
        }

        private Transform FindDescendant(string boneName)
        {
            foreach (Transform descendant in GetComponentsInChildren<Transform>(true))
                if (descendant.name == boneName) return descendant;
            throw new MissingReferenceException(
                "HoloSpray right-arm bone could not be resolved: " + boneName);
        }

        private void ApplyRightArmPose()
        {
            foreach (HoloSprayBoneRotation pose in profile.RightArmPose)
            {
                Transform bone = transform.Find(pose.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        "HoloSpray right-arm pose path could not be resolved: " + pose.Path);
                bone.localRotation = pose.LocalRotation;
            }
        }

        private void ApplySprayPose()
        {
            sprayHolder.transform.SetPositionAndRotation(
                ExpectedWorldPosition(), ExpectedWorldRotation());
            sprayHolder.transform.localScale = profile.HolderScale;
            sprayModel.SetLocalPositionAndRotation(
                profile.ModelLocalPosition, profile.ModelLocalRotation);
            sprayModel.localScale = profile.ModelLocalScale;
        }

        private void ResolveHand()
        {
            if (profile == null)
                throw new MissingReferenceException("HoloSpray carry profile is missing.");
            if (rightHand == null) rightHand = transform.Find(profile.RightHandPath);
            if (rightHand == null)
                throw new MissingReferenceException(
                    "HoloSpray right-hand path could not be resolved: " +
                    profile.RightHandPath);
        }

        private void EnsureSprayInstance()
        {
            if (profile == null || profile.SprayPrefab == null)
                throw new MissingReferenceException("HoloSpray model prefab is missing.");
            if (sprayHolder != null && instantiatedPrefab == profile.SprayPrefab)
            {
                if (sprayModel == null)
                    sprayModel = sprayHolder.transform.Find("HolographicSpray_Model");
                return;
            }
            DestroySprayInstance();
            sprayHolder = new GameObject("HoloSpray_Prop");
            sprayHolder.transform.SetParent(transform, false);
            instantiatedPrefab = profile.SprayPrefab;
            GameObject model = Instantiate(profile.SprayPrefab, sprayHolder.transform, false);
            model.name = "HolographicSpray_Model";
            sprayModel = model.transform;
            if (!Application.isPlaying)
                SetHideFlagsRecursively(sprayHolder, HideFlags.DontSaveInEditor);
        }

        private void DestroySprayInstance()
        {
            if (sprayHolder == null) return;
            if (Application.isPlaying) Destroy(sprayHolder);
            else DestroyImmediate(sprayHolder);
            sprayHolder = null;
            instantiatedPrefab = null;
            sprayModel = null;
        }

        private static void SetHideFlagsRecursively(GameObject root, HideFlags flags)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.hideFlags = flags;
        }
    }
}
