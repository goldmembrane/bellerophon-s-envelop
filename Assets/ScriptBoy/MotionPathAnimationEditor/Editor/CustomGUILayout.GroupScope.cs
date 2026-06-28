using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static partial class CustomGUILayout
    {
        public class GroupScope : GUI.Scope
        {
            public GroupScope(string groupName)
            {
                GUILayout.Label(groupName, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
            }

            protected override void CloseScope()
            {
                EditorGUI.indentLevel--;
                GUILayout.Space(5);
            }
        }
    }
}