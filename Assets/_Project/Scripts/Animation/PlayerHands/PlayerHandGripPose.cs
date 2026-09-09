using System;
using UnityEngine;

namespace Bellerophon.PlayerHands
{
    /// <summary>Authored local finger rotations and rigid item offsets shared by rig-driven consumers.</summary>
    public sealed class PlayerHandGripPose : ScriptableObject
    {
        [Serializable]
        public struct JointPose
        {
            public string BoneName;
            public Quaternion LocalRotation;
        }

        public Vector3 LeftItemPosition;
        public Quaternion LeftItemRotation;
        public Vector3 RightItemPosition;
        public Quaternion RightItemRotation;
        public JointPose[] Joints;

        public Quaternion RotationFor(string boneName)
        {
            foreach (JointPose joint in Joints)
                if (joint.BoneName == boneName) return joint.LocalRotation;
            throw new InvalidOperationException("Missing authored finger joint: " + boneName);
        }
    }
}
