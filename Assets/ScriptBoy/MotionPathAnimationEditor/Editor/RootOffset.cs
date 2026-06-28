using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    class RootOffset : UnityEditor.ScriptableSingleton<RootOffset>
    {
        public static Matrix4x4 matrix => Matrix4x4.TRS(instance.position, instance.rotation, Vector3.one);

        [SerializeField] private Vector3 m_Position;
        [SerializeField] private Vector3 m_Rotation;

        public Vector3 position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public Quaternion rotation
        {
            get => Quaternion.Euler(m_Rotation);
            set => m_Rotation = value.eulerAngles;
        }

        private void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
        }
    }
}