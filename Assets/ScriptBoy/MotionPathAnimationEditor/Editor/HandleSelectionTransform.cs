using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    class HandleSelectionTransform : UnityEditor.ScriptableSingleton<HandleSelectionTransform>
    {
        [SerializeField] private Vector3 m_Position = Vector3.zero;
        [SerializeField] private Vector3 m_Rotation = Vector3.zero;
        [SerializeField] private Vector3 m_Scale = Vector3.one;

        private Quaternion m_RotationOffset = Quaternion.identity;

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

        public Quaternion rotationOffset
        {
            get => (Tools.pivotRotation == PivotRotation.Local) ? m_RotationOffset : Quaternion.identity;
            set => m_RotationOffset = value.normalized;
        }

        public Vector3 scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }



        private float m_Time;
        private List<Vector3> m_DefaultPositions;

        private void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            m_DefaultPositions = new List<Vector3>();
        }

        public void RefeshTransform(float time)
        {
            position = HandleSelection.center;
            rotation = Quaternion.identity;
            scale = Vector3.one;

            rotationOffset = HandleSelection.rotation;
        }

        public void CheckTimeChanges(float time)
        {
            if (m_Time == time) return;
            m_Time = time;

            UpdateDefaultPositions();
        }

        public void UpdateDefaultPositions()
        {
            Matrix4x4 world2Local = Matrix4x4.TRS(position, rotation, scale).inverse;

            m_DefaultPositions.Clear();
            foreach (var handle in HandleSelection.handles)
            {
                m_DefaultPositions.Add(world2Local.MultiplyPoint(handle.position));
            }
        }

        public void UpdatePositions()
        {
            Matrix4x4 local2World = Matrix4x4.TRS(position, rotation, scale);

            int i = 0;
            foreach (var handle in HandleSelection.handles)
            {
                handle.position = local2World.MultiplyPoint(m_DefaultPositions[i++]);
                handle.hasChanged = true;
            }
        }


        public void DoPositionHandle()
        {
            EditorGUI.BeginChangeCheck();
            var p = Handles.PositionHandle(position, rotation * rotationOffset);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Move");
                position = EditorGridUtility.SnapToGrid(p);
                UpdatePositions();
            }
        }

        public void DoRotationHandle()
        {
            EditorGUI.BeginChangeCheck();
            var r = Handles.DoRotationHandle(rotation * rotationOffset, position) * Quaternion.Inverse(rotationOffset);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Rotate");
                rotation = r;
                UpdatePositions();
            }
        }

        public void DoScaleHandle()
        {
            EditorGUI.BeginChangeCheck();
            var s = Handles.DoScaleHandle(scale, position, rotation * rotationOffset, HandleUtility.GetHandleSize(position));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Scale");
                scale = s;
                UpdatePositions();
            }
        }
    }
}