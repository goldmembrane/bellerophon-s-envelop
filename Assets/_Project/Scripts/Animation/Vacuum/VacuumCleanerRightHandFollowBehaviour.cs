using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [DisallowMultipleComponent]
    public sealed class VacuumCleanerRightHandFollowBehaviour : MonoBehaviour
    {
        [SerializeField] private string rightHandPath;
        [SerializeField] private string vacuumCleanerPath;
        [SerializeField] private Vector3 positionOffsetInHandSpace;
        [SerializeField] private Quaternion rotationOffsetFromHand = Quaternion.identity;
        [SerializeField] private Vector3 preservedLocalScale = Vector3.one;

        private Transform rightHand;
        private Transform vacuumCleaner;

        public string RightHandPath => rightHandPath;
        public string VacuumCleanerPath => vacuumCleanerPath;
        public Vector3 PositionOffsetInHandSpace => positionOffsetInHandSpace;
        public Quaternion RotationOffsetFromHand => rotationOffsetFromHand;
        public Vector3 PreservedLocalScale => preservedLocalScale;

        public void Configure(
            string handPath,
            string cleanerPath,
            Transform hand,
            Transform cleaner)
        {
            rightHandPath = handPath;
            vacuumCleanerPath = cleanerPath;
            rightHand = hand;
            vacuumCleaner = cleaner;
            positionOffsetInHandSpace = hand.InverseTransformPoint(cleaner.position);
            rotationOffsetFromHand = Quaternion.Inverse(hand.rotation) * cleaner.rotation;
            preservedLocalScale = cleaner.localScale;
        }

        public Vector3 ExpectedWorldPosition()
        {
            ResolveReferences();
            return rightHand.TransformPoint(positionOffsetInHandSpace);
        }

        public Quaternion ExpectedWorldRotation()
        {
            ResolveReferences();
            return rightHand.rotation * rotationOffsetFromHand;
        }

        private void OnEnable()
        {
            rightHand = null;
            vacuumCleaner = null;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResolveReferences();
            vacuumCleaner.SetPositionAndRotation(
                rightHand.TransformPoint(positionOffsetInHandSpace),
                rightHand.rotation * rotationOffsetFromHand);
            vacuumCleaner.localScale = preservedLocalScale;
        }

        private void ResolveReferences()
        {
            if (rightHand == null)
            {
                rightHand = transform.Find(rightHandPath);
            }
            if (vacuumCleaner == null)
            {
                vacuumCleaner = transform.Find(vacuumCleanerPath);
            }
            if (rightHand == null || vacuumCleaner == null)
            {
                throw new MissingReferenceException(
                    "Vacuum cleaner right-hand follow paths could not be resolved. " +
                    "Hand=" + rightHandPath + ", Cleaner=" + vacuumCleanerPath + ".");
            }
        }
    }
}
