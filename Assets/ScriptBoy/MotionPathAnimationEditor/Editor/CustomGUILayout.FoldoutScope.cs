using UnityEditor;
using UnityEngine;


namespace ScriptBoy.MotionPathAnimEditor
{
    static partial class CustomGUILayout
    {
        public class FoldoutScope : GUI.Scope
        {
            public FoldoutScope(string title, out bool foldout)
            {
                foldout = FoldoutState.GetState(title);
                EditorGUI.BeginChangeCheck();
                OpenScope(ref foldout, title);
                if (EditorGUI.EndChangeCheck())
                {
                    FoldoutState.SetState(title, foldout);
                }
            }

            public void OpenScope(ref bool foldout, string title)
            {
                foldout = EditorGUILayout.Foldout(foldout, title);
                EditorGUI.indentLevel++;
            }

            protected override void CloseScope()
            {
                EditorGUI.indentLevel--;
            }
        }
    }
}