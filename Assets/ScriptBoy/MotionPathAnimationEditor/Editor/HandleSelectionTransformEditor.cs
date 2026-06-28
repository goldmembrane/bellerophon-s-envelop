using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    [UnityEditor.CustomEditor(typeof(HandleSelectionTransform))]
    class HandleSelectionTransformEditor : CustomEditor
    {
        [SerializeField] private SerializedProperty m_PositionProp;
        [SerializeField] private SerializedProperty m_RotationProp;
        [SerializeField] private SerializedProperty m_ScaleProp;

        public void OnGUI()
        {
            serializedObject.Update();
            using (new CustomGUILayout.FoldoutWindowScope("Transform", out bool open))
            {
                if (open)
                {
                    EditorGUILayout.PropertyField(m_PositionProp);
                    EditorGUILayout.PropertyField(m_RotationProp);
                    EditorGUILayout.PropertyField(m_ScaleProp);
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}