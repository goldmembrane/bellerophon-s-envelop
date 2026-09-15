using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bellerophon.PlayerAnimation
{
    [Serializable]
    public struct LightsaberOffIdleBoneRotation
    {
        [SerializeField] private string path;
        [SerializeField] private Quaternion localRotation;

        public LightsaberOffIdleBoneRotation(string bonePath, Quaternion rotation)
        {
            path = bonePath;
            localRotation = rotation;
        }

        public string Path => path;
        public Quaternion LocalRotation => localRotation;
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class LightsaberOffIdleCarryPoseBehaviour : MonoBehaviour
    {
        // Use the established carried-item retention weights: the authored grip
        // stays dominant while exact source locomotion still contributes sway.
        public const float RightArmMotionWeight = 0.05f;
        public const float RightForeArmMotionWeight = 0.03f;
        public const float RightHandMotionWeight = 0.01f;
        public const float RightFingerMotionWeight = 0f;

        [SerializeField] private List<LightsaberOffIdleBoneRotation> authoredPose =
            new List<LightsaberOffIdleBoneRotation>();
        [SerializeField] private List<LightsaberOffIdleBoneRotation> sourceIdlePose =
            new List<LightsaberOffIdleBoneRotation>();

        public IReadOnlyList<LightsaberOffIdleBoneRotation> AuthoredPose => authoredPose;
        public IReadOnlyList<LightsaberOffIdleBoneRotation> SourceIdlePose => sourceIdlePose;

        public void Configure(
            IEnumerable<LightsaberOffIdleBoneRotation> authored,
            IEnumerable<LightsaberOffIdleBoneRotation> source)
        {
            authoredPose = new List<LightsaberOffIdleBoneRotation>(authored);
            sourceIdlePose = new List<LightsaberOffIdleBoneRotation>(source);
        }

        public void RestoreAuthoredPose()
        {
            foreach (LightsaberOffIdleBoneRotation authored in authoredPose)
            {
                Transform bone = transform.Find(authored.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        name + " lightsaber carry bone is missing: " + authored.Path);
                bone.localRotation = authored.LocalRotation;
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying) return;
            ApplyAnimatedPose();
        }

        private void OnAnimatorMove()
        {
            if (!Application.isPlaying) return;
            ApplyAnimatedPose();
        }

        private void ApplyAnimatedPose()
        {
            if (authoredPose.Count != sourceIdlePose.Count || authoredPose.Count == 0)
                throw new InvalidOperationException(
                    name + " lightsaber carry pose data is incomplete.");

            for (int index = 0; index < authoredPose.Count; index++)
            {
                LightsaberOffIdleBoneRotation authored = authoredPose[index];
                LightsaberOffIdleBoneRotation source = sourceIdlePose[index];
                if (!string.Equals(authored.Path, source.Path, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        name + " lightsaber carry pose paths differ at " + index + ".");

                Transform bone = transform.Find(authored.Path);
                if (bone == null)
                    throw new MissingReferenceException(
                        name + " lightsaber carry bone is missing: " + authored.Path);

                Quaternion animatedDelta =
                    Quaternion.Inverse(source.LocalRotation) * bone.localRotation;
                Quaternion retainedMotion = Quaternion.SlerpUnclamped(
                    Quaternion.identity,
                    animatedDelta,
                    MotionWeight(authored.Path));
                bone.localRotation = authored.LocalRotation * retainedMotion;
            }
        }

        private static float MotionWeight(string path)
        {
            if (path.IndexOf("/RightHand/", StringComparison.Ordinal) >= 0)
                return RightFingerMotionWeight;
            if (path.EndsWith("/RightHand", StringComparison.Ordinal))
                return RightHandMotionWeight;
            if (path.EndsWith("/RightForeArm", StringComparison.Ordinal))
                return RightForeArmMotionWeight;
            return RightArmMotionWeight;
        }
    }
}
