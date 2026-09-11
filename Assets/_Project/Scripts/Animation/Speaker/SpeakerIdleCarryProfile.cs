using System;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [Serializable]
    public struct SpeakerBoneRotation
    {
        [SerializeField] private string path;
        [SerializeField] private Quaternion localRotation;

        public SpeakerBoneRotation(string path, Quaternion localRotation)
        {
            this.path = path;
            this.localRotation = localRotation;
        }

        public string Path => path;
        public Quaternion LocalRotation => localRotation;
    }

    public sealed class SpeakerIdleCarryProfile : ScriptableObject
    {
        private static readonly Vector3 CapturedModelLocalPosition =
            new Vector3(0f, 0.108f, -0.223f);
        private static readonly Quaternion CapturedModelLocalRotation =
            Quaternion.Euler(-90f, 0f, 0f);
        private static readonly Vector3 CapturedModelLocalScale =
            new Vector3(100f, 50f, 70f);

        [SerializeField] private GameObject speakerPrefab;
        [SerializeField] private string leftShoulderPath;
        [SerializeField] private Vector3 positionOffsetInShoulderSpace;
        [SerializeField] private Quaternion rotationOffsetFromShoulder = Quaternion.identity;
        [SerializeField] private Vector3 holderScale = Vector3.one;
        [SerializeField] private Vector3 modelLocalPosition;
        [SerializeField] private Quaternion modelLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private bool hasAuthoredModelTransform;
        [SerializeField] private Vector3 handlePointInHolderSpace;
        [SerializeField] private Vector3 handleAxisInHolderSpace = Vector3.right;
        [SerializeField] private SpeakerBoneRotation[] sourceLeftArmPose =
            Array.Empty<SpeakerBoneRotation>();
        [SerializeField] private SpeakerBoneRotation[] leftArmPose =
            Array.Empty<SpeakerBoneRotation>();

        public GameObject SpeakerPrefab => speakerPrefab;
        public string LeftShoulderPath => leftShoulderPath;
        public Vector3 PositionOffsetInShoulderSpace => positionOffsetInShoulderSpace;
        public Quaternion RotationOffsetFromShoulder => rotationOffsetFromShoulder;
        public Vector3 HolderScale => holderScale;
        public Vector3 ModelLocalPosition => hasAuthoredModelTransform
            ? modelLocalPosition
            : CapturedModelLocalPosition;
        public Quaternion ModelLocalRotation => hasAuthoredModelTransform
            ? modelLocalRotation
            : CapturedModelLocalRotation;
        public Vector3 ModelLocalScale => hasAuthoredModelTransform
            ? modelLocalScale
            : CapturedModelLocalScale;
        public Vector3 HandlePointInHolderSpace => handlePointInHolderSpace;
        public Vector3 HandleAxisInHolderSpace => handleAxisInHolderSpace;
        public SpeakerBoneRotation[] SourceLeftArmPose => sourceLeftArmPose;
        public SpeakerBoneRotation[] LeftArmPose => leftArmPose;

        public void Configure(
            GameObject prefab,
            string shoulderPath,
            Vector3 positionOffset,
            Quaternion rotationOffset,
            Vector3 scale,
            Vector3 localModelPosition,
            Quaternion localModelRotation,
            Vector3 localModelScale,
            Vector3 handlePoint,
            Vector3 handleAxis,
            SpeakerBoneRotation[] sourcePose,
            SpeakerBoneRotation[] pose)
        {
            speakerPrefab = prefab;
            leftShoulderPath = shoulderPath;
            positionOffsetInShoulderSpace = positionOffset;
            rotationOffsetFromShoulder = rotationOffset;
            holderScale = scale;
            modelLocalPosition = localModelPosition;
            modelLocalRotation = localModelRotation;
            modelLocalScale = localModelScale;
            hasAuthoredModelTransform = true;
            handlePointInHolderSpace = handlePoint;
            handleAxisInHolderSpace = handleAxis;
            sourceLeftArmPose = sourcePose ?? Array.Empty<SpeakerBoneRotation>();
            leftArmPose = pose ?? Array.Empty<SpeakerBoneRotation>();
        }
    }
}
