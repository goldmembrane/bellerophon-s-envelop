using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static partial class CustomGUILayout
    {
        static readonly GUIContent s_EmptyContent = new GUIContent("");



        public static void PropertyField(SerializedProperty serializedProperty, GUIContent lable, float lableWidth, float fieldWidth)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(lable, GUILayout.MaxWidth(lableWidth));

            EditorGUILayout.PropertyField(serializedProperty, s_EmptyContent,
                GUILayout.MaxWidth(fieldWidth), GUILayout.ExpandWidth(true));

            EditorGUILayout.EndHorizontal();
        }

        public static void PropertyFieldNoLable(SerializedProperty serializedProperty)
        {
            EditorGUILayout.PropertyField(serializedProperty, s_EmptyContent);
        }

        public static void ToggleLeft(SerializedProperty serializedProperty, GUIContent lable)
        {
            serializedProperty.boolValue = EditorGUILayout.ToggleLeft(lable, serializedProperty.boolValue);
        }

        public static bool ButtonNoStretch(string text)
        {
            return GUILayout.Button(text, GUIStyles.buttonNoStretch);
        }

        public static MinMaxGradient MinMaxGradientField(string lable, MinMaxGradient gradient)
        {
            using (new FoldoutScope(lable, out bool editMode))
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                s_EmptyContent.text = lable;
                float indent = EditorGUI.indentLevel * 15 + GUI.skin.label.CalcSize(s_EmptyContent).x;
                s_EmptyContent.text = "";
                rect.x += indent;
                rect.width -= indent;
                GUI.DrawTexture(rect, gradient.staticVerticalMap);

                Event @event = Event.current;
                if (@event.type == EventType.MouseDown && @event.button == 0 && rect.Contains(@event.mousePosition))
                {
                    FoldoutState.SetState(lable, !editMode);
                }

                if (editMode)
                {
                    gradient.minColor = EditorGUILayout.ColorField("Min", gradient.minColor);
                    gradient.maxColor = EditorGUILayout.ColorField("Max", gradient.maxColor);
                }
            }
            return gradient;
        }

        public static bool ToggleButton(string lable, bool value)
        {
            value = GUILayout.Toggle(value, new GUIContent(lable), GUIStyles.toggleButton);
            return value;
        }
    }
}