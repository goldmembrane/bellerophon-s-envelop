using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{

    partial class AnimEditorWindow
    {
        class MotionPathListRenderer : ReorderableList
        {
            List<MotionPath> m_List;
            int m_PrevCount;

            List<string> m_CopyData = null;


            public void UpdateList(List<MotionPath> motionPaths)
            {

                list = m_List  = motionPaths;
            }

 
            public MotionPathListRenderer(List<MotionPath> list) : base(list, typeof(MotionPath))
            {
                m_List = list;
                //headerHeight = 50;
                //elementHeight = 22;
                onAddCallback += OnAddCallback;
                onCanAddCallback += OnCanAddCallback;
                drawHeaderCallback += DrawHeaderCallback;
                drawElementCallback += DrawElementCallback;
                drawElementBackgroundCallback += DrawElementBackgroundCallback;
            }

            private void DrawElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
            {
                if (m_List.Count <= 1)
                {
                    rect.y -= 4;
                    rect.height += 7;
                }
                else if (index == 0)
                {

                    rect.y -= 4;
                    rect.height += 4;

                }
                else if (index == m_List.Count - 1)
                {
                    rect.y += 1;
                    rect.height += 2;
                }
                else
                {
                    rect.y += 1;
                    rect.height -= 1;
                }


                //  rect.x++;
                // rect.width--;
                GUI.DrawTexture(rect, isActive ? Textures.itemRowActive : Textures.itemRowNormal);
            }

            private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
            {

                //rect.y -= 2;
                //rect.height -= 4;

                var motionPath = m_List[index];

                if (motionPath.isMissing)
                {
                    Color contentColor = GUI.contentColor;
                    GUI.contentColor = Color.yellow;
                    GUI.Label(rect, motionPath.fullName + " (Missing)");
                    GUI.contentColor = contentColor;
                    return;
                }
                GUI.Label(rect, Settings.showPathFullName ? motionPath.fullName : motionPath.name);

                float s = rect.height - 2;
                rect = new Rect(rect.x + rect.width - s * 3, rect.y, s, s);

                if (Settings.showPathEditButton)
                {
                    //if (motionPath.HasCurveData) MotionPathToggleButtons.loop.Draw(rect, m_List, index);

                 //   Rect r = new Rect(rect.x - 60, rect.y, rect.width + 60, rect.height);
                  //  r = RectUtility.Shrink(r, 0, 3);

                  //  GUI.Button(RectUtility.DockLeft(r, r.width / 2), "Look");
                  //  GUI.Button(RectUtility.DockRight(r, r.width / 2), "Loop");
            

                    rect.x += s;
                    //if (motionPath.HasCurveData)
                        MotionPathToggleButtons.edit.Draw(rect, m_List, index);
                    rect.x += s;
                }
                else
                {
                    rect.x += s;
                    //if (motionPath.HasCurveData) MotionPathToggleButtons.loop.Draw(rect, m_List, index);
                    rect.x += s;
                }

                MotionPathToggleButtons.active.Draw(rect, m_List, index);
            }

            private void DrawHeaderCallback(Rect rect)
            {
                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Copy"), false, Copy);
                    if (CanPaste())
                    {
                        menu.AddItem(new GUIContent("Paste"), false, Paste);
                    }
                    menu.AddItem(new GUIContent("Clear"), false, Clear);
                    menu.ShowAsContext();
                }


                int count = m_List.Count;
                if (m_PrevCount != count)
                {
                    m_PrevCount = count;
                    AnimEditorWindow.RepaintWindow();
                }



                Rect r = rect;
                r.x -= 5;
                r.width += 10;
                GUI.DrawTexture(r, Textures.listHeader);

                GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
                style.alignment = TextAnchor.MiddleCenter;
                GUI.Label(rect, "Motion Path List", style);


                /*
                rect.width = 100;

                if (GUI.Button(rect, "Copy"))
                {

                }
                rect.x += rect.width;
                if (GUI.Button(rect, "Paste"))
                {

                }
       */
            }

            void Copy()
            {
                m_CopyData = new List<string>();
                foreach (var item in m_List)
                {
                    m_CopyData.Add(item.transformPath);
                }
            }

            void Paste()
            {
                Undo.RecordObject(AnimEditor.instance.motionPathClip, "MotionPathList.Paste");
                foreach (var item in m_CopyData)
                {
                    if (m_List.Exists((m) => m.transformPath == item)) continue;
                    m_List.Add(new MotionPath(item));
                }
            }

            bool CanPaste()
            {
                if(m_CopyData != null)
                foreach (var item in m_CopyData)
                {
                    if (m_List.TrueForAll((m) => m.transformPath != item)) return false;
                }

                return false;
            }

            void Clear()
            {
                Undo.RecordObject(AnimEditor.instance.motionPathClip, "MotionPathList.Clear");
                m_List.Clear();
            }

            private bool OnCanAddCallback(ReorderableList list)
            {
                foreach (var transform in Selection.transforms)
                {
                    if (CanBeAdded(transform))
                    {
                        return true;
                    }
                }
                return false;
            }

            private void OnAddCallback(ReorderableList list)
            {
                foreach (var transform in Selection.GetFiltered<Transform>(SelectionMode.ExcludePrefab))
                {
                    if (CanBeAdded(transform))
                    {
                        m_List.Add(new MotionPath(transform));
                    }
                }
            }

            bool CanBeAdded(Transform transform)
            {
  

                if (m_List.Exists(e => e.transform == transform)) return false;
                Transform root = AnimEditor.root;
                if (root == null) return false;
                if (transform == root) return true;
                if (transform.IsChildOf(root)) return true;

   
                return false;
            }
        }

        static class MotionPathToggleButtons
        {
            public static MotionPathToggleButton active { get; }
            public static MotionPathToggleButton edit { get; }
            public static MotionPathToggleButton loop { get; }

            static MotionPathToggleButtons()
            {
                active =  new MotionPathToggleButton(GUIContents.activePath, GUIStyles.toggleVisibility, (e) => e.active, (e, v) => e.active = v);
                edit = new MotionPathToggleButton(GUIContents.editPath, GUIStyles.toggleEdit, (e) => e.editable, (e, v) => e.editable = v);
                loop = new MotionPathToggleButton(GUIContents.loopPath, GUIStyles.toggleLoop, (e) => e.loop, (e, v) => e.loop = v);
            }
        }

        class MotionPathToggleButton
        {
            GUIContent m_Content;
            GUIStyle m_Style;
            Func<MotionPath, bool> m_GetValue;
            Action<MotionPath, bool> m_SetValue;

            public MotionPathToggleButton(GUIContent content, GUIStyle style, Func<MotionPath, bool> getValue, Action<MotionPath, bool> setValue)
            {
                m_Content = content;
                m_Style = style;
                m_GetValue = getValue;
                m_SetValue = setValue;
            }

            public void Draw(Rect rect, List<MotionPath> list, int index)
            {
                MotionPath motionPath = list[index];

                bool value = m_GetValue(motionPath);
                rect = ApplyMargin(rect, 4);
                int button = Event.current.button;
                EditorGUI.BeginChangeCheck();
                value = GUI.Toggle(rect, value, m_Content, m_Style);
                if (EditorGUI.EndChangeCheck())
                {
                    if (button == 1)
                    {
                        bool oldValue = m_GetValue(motionPath);
                        m_SetValue(motionPath, false);
                        bool all = oldValue && list.TrueForAll((e) => !m_GetValue(e));
                        foreach (var e in list) m_SetValue(e, all);
                        value = true;
                    }
                    m_SetValue(motionPath, value);
                }
            }

            Rect ApplyMargin(Rect rect, float margin)
            {
                return new Rect(rect.x + margin, rect.y + margin, rect.width - margin, rect.height - margin);
            }
        }
    }
}