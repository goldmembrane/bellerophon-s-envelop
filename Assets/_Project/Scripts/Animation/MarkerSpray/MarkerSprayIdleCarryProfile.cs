using System;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [Serializable]
    public struct MarkerSprayBoneRotation
    {
        [SerializeField] private string path;
        [SerializeField] private Quaternion localRotation;

        public MarkerSprayBoneRotation(string path, Quaternion localRotation)
        {
            this.path = path;
            this.localRotation = localRotation;
        }

        public string Path => path;
        public Quaternion LocalRotation => localRotation;
    }

    public sealed class MarkerSprayIdleCarryProfile : ScriptableObject
    {
        [SerializeField] private GameObject sprayPrefab;
        [SerializeField] private string rightHandPath;
        [SerializeField] private Vector3 positionOffsetInHandSpace;
        [SerializeField] private Quaternion rotationOffsetFromHand = Quaternion.identity;
        [SerializeField] private Vector3 holderScale = Vector3.one;
        [SerializeField] private Vector3 modelLocalPosition;
        [SerializeField] private Quaternion modelLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private MarkerSprayBoneRotation[] sourceRightArmPose =
            Array.Empty<MarkerSprayBoneRotation>();
        [SerializeField] private MarkerSprayBoneRotation[] rightArmPose =
            Array.Empty<MarkerSprayBoneRotation>();

        public GameObject SprayPrefab => sprayPrefab;
        public string RightHandPath => rightHandPath;
        public Vector3 PositionOffsetInHandSpace => positionOffsetInHandSpace;
        public Quaternion RotationOffsetFromHand => rotationOffsetFromHand;
        public Vector3 HolderScale => holderScale;
        public Vector3 ModelLocalPosition => modelLocalPosition;
        public Quaternion ModelLocalRotation => modelLocalRotation;
        public Vector3 ModelLocalScale => modelLocalScale;
        public MarkerSprayBoneRotation[] SourceRightArmPose => sourceRightArmPose;
        public MarkerSprayBoneRotation[] RightArmPose => rightArmPose;

        public void Configure(
            GameObject prefab,
            string handPath,
            Vector3 positionOffset,
            Quaternion rotationOffset,
            Vector3 scale,
            Vector3 localModelPosition,
            Quaternion localModelRotation,
            Vector3 localModelScale,
            MarkerSprayBoneRotation[] sourcePose,
            MarkerSprayBoneRotation[] pose)
        {
            sprayPrefab = prefab;
            rightHandPath = handPath;
            positionOffsetInHandSpace = positionOffset;
            rotationOffsetFromHand = rotationOffset;
            holderScale = scale;
            modelLocalPosition = localModelPosition;
            modelLocalRotation = localModelRotation;
            modelLocalScale = localModelScale;
            sourceRightArmPose = sourcePose ?? Array.Empty<MarkerSprayBoneRotation>();
            rightArmPose = pose ?? Array.Empty<MarkerSprayBoneRotation>();
        }
    }
}
