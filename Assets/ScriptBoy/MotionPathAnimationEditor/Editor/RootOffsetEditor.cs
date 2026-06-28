using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    [UnityEditor.CustomEditor(typeof(RootOffset))]
    class RootOffsetEditor : CustomEditor
    {
        [SerializeField] private SerializedProperty m_PositionProp;
        [SerializeField] private SerializedProperty m_RotationProp;

        public void OnGUI()
        {

            serializedObject.Update();
            using (new CustomGUILayout.FoldoutWindowScope("Root Offset", out bool open))
            {
                if (open)
                {
                    if (Settings.pathSpace == Space.World)
                    {
                        EditorGUILayout.HelpBox("It doesn't work when PathSpace == World!", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(m_PositionProp);
                        EditorGUILayout.PropertyField(m_RotationProp);

                        EditorGUILayout.Space();

                        GUILayout.BeginHorizontal();
                        OnButtonsGUI();
                        GUILayout.EndHorizontal();
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();

        }

        private void OnButtonsGUI()
        {
            if (GUILayout.Button("Reset"))
            {
                m_PositionProp.vector3Value = Vector3.zero;
                m_RotationProp.vector3Value = Vector3.zero;
            }

            var rootMotionPath = AnimEditor.rootMotionPath;
            if (rootMotionPath != null && rootMotionPath.hasCurveData)
            {
                if (GUILayout.Button("Calculate Offset"))
                {
                    float time = AnimEditor.animationWindow.time;
                    Vector3 offset = AnimEditor.root.position - rootMotionPath.GetPositionAtTime(time);
                    m_PositionProp.vector3Value = offset;
                }
            }
        }
    }
}