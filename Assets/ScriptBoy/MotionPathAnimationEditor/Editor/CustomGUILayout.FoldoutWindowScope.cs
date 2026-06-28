using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static partial class CustomGUILayout
    {
        public class FoldoutWindowScope : GUI.Scope
        {
            public FoldoutWindowScope(string title)
            {
                bool foldout = FoldoutState.GetState(title);
                EditorGUI.BeginChangeCheck();
                Open(ref foldout, title);
                if (EditorGUI.EndChangeCheck())
                {
                    FoldoutState.SetState(title, foldout);
                }
            }

            public FoldoutWindowScope(string title, string key, out bool foldout)
            {
                foldout = FoldoutState.GetState(key);
                Open(ref foldout, title);
                FoldoutState.SetState(key, foldout);
            }

            public FoldoutWindowScope(string title, out bool foldout)
            {
                foldout = FoldoutState.GetState(title);
                Open(ref foldout, title);
                FoldoutState.SetState(title, foldout);
            }

            public FoldoutWindowScope(ref bool foldout, string title)
            {
                Open(ref foldout, title);
            }

            public void Open(ref bool foldout, string title)
            {
                GUILayout.BeginVertical("", GUIStyles.foldoutWindow);

                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(title));
                Rect rect = GUILayoutUtility.GetRect(size.x, size.y + 5);
                Rect rect2 = rect;
                rect2.x += 2;
                rect2.width -= 4;
                EditorGUI.LabelField(rect2, "", GUIStyles.foldoutWindowBar);
                foldout = EditorGUI.Foldout(rect, foldout, title, GUIStyles.foldoutWindowFoldout);
                GUILayout.BeginVertical("", foldout ? GUIStyles.foldoutWindowOpen : GUIStyles.foldoutWindowClose);
            }

            protected override void CloseScope()
            {
                GUILayout.EndVertical();
                GUILayout.EndVertical();
            }
        }
    }
}