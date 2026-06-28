using System;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    partial class AnimEditorWindow : EditorWindow
    {
        private static AnimEditorWindow s_Instance;

        [SerializeField] private Material m_CurveMaterial;

        private AnimEditor m_AnimEditor;
        private HandleSelectionTransformEditor m_SelectionTransformEditor;
        private RootOffsetEditor m_RootOffsetEditor;
        private MotionPathListRenderer m_ListRenderer;

        private bool m_ShowFullName;
        private bool m_ShowSettings;
        private bool m_EditMode;

        [MenuItem("Tools/ScriptBoy/Motion Path Animation Editor", false, 0)]
        static void OpenWindow()
        {
            GetWindow<AnimEditorWindow>().Show();
        }

        public static void RepaintWindow()
        {
            if (s_Instance != null) s_Instance.Repaint();
        }

        private void Awake()
        {
            titleContent = new GUIContent("Motion Path Anim Editor");
            minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            s_Instance = this;
            BezierCurveRenderer.SetMaterial(m_CurveMaterial);
            m_SelectionTransformEditor = (HandleSelectionTransformEditor)Editor.CreateEditor(HandleSelectionTransform.instance);
            m_RootOffsetEditor = (RootOffsetEditor)Editor.CreateEditor(RootOffset.instance);
            if (m_AnimEditor != null) m_AnimEditor.Destroy();
            m_AnimEditor = new AnimEditor();
            m_AnimEditor.editMode = m_EditMode;

            if (m_AnimEditor.motionPathClip != null)
            {
                m_ListRenderer = new MotionPathListRenderer(m_AnimEditor.motionPaths);
            }
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

         void UndoRedoPerformed()
        {
            Repaint();
        }

        private void OnDisable()
        {
            DestroyImmediate(m_SelectionTransformEditor);
            DestroyImmediate(m_RootOffsetEditor);
            m_AnimEditor.Destroy();
            m_AnimEditor = null;
            Tools.hidden = false;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        private void OnGUI()
        {
            
            //bool wideMode = EditorGUIUtility.wideMode;
            //EditorGUIUtility.wideMode = true;

            if (OnCheckStateGUI())
            {
                Tools.hidden = false;
                return;
            }

            Tools.hidden = m_EditMode;

            OnHeaderGUI();
            OnBodyGUI();

            //EditorGUIUtility.wideMode = wideMode;

            SceneView.RepaintAll();
        }

        private bool OnCheckStateGUI()
        {
            if (!AnimEditor.animationWindow)
            {
                AnimEditor.animationWindow = AnimationWindowWrapper.FindWindow();
            }

            if (!AnimEditor.animationWindow)
            {
                EditorGUILayout.HelpBox("No Animation Window!", MessageType.Error);
                if (GUILayout.Button("Open Animation Window"))
                {
                    AnimEditor.animationWindow = AnimationWindowWrapper.GetWindow();
                }
                return true;
            }

            if (AnimEditor.animationWindow.animationClip == null)
            {
                if (AnimEditor.animationWindow.hasFocus)
                {
                    EditorGUILayout.HelpBox("No Animation Clip!", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox("No Animation Window!", MessageType.Warning);
                    if (GUILayout.Button("Open Animation Window"))
                    {
                        AnimEditor.animationWindow.Focus();
                    }
                }
                return true;
            }

            if (AnimEditor.animationWindow.animationClip.hideFlags == HideFlags.NotEditable)
            {
                EditorGUILayout.HelpBox($"The '{AnimEditor.animationWindow.animationClip.name}' animation clip is not editable!", MessageType.Error);
                return true;
            }

            if (AnimEditor.animationWindow.rootGameObject == null)
            {
                EditorGUILayout.HelpBox("No Root GameObject!", MessageType.Error);
                return true;
            }

            return false;
        }

        private void OnHeaderGUI()
        {
            using (new GUILayout.HorizontalScope(GUIStyles.header))
            {
                Rect rect = new Rect(0, 0, Screen.width, 50);
                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 1 && rect.Contains(e.mousePosition))
                {
                    e.Use();
                }
                m_EditMode = GUILayout.Toggle(m_EditMode, GUIContents.editMode, GUIStyles.toggleEditMode);
                GUILayout.FlexibleSpace();
                m_ShowSettings = GUILayout.Toggle(m_ShowSettings, GUIContents.showSettings, GUIStyles.toggleSettings);

                m_AnimEditor.editMode = m_EditMode;
            }
        }

        Vector3 scrollPosition;
        private void OnBodyGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition,null, GUI.skin.verticalScrollbar);

            using (new GUILayout.VerticalScope(GUIStyles.body))
            {
                if (m_ShowSettings)
                {
                    Settings.DrawFoldoutWindow();
                }

                if (Settings.useRootOffset)
                {
                    OnRootOffsetGUI();
                }

                if (Settings.useHideHandles)
                {
                    HideHandles.DrawFoldoutWindow();
                }

                if (Settings.useMagnet)
                {
                    Magnet.DrawFoldoutWindow();
                }



                EditorGUILayout.Space();

                if (m_ListRenderer == null && m_AnimEditor.motionPathClip != null)
                {
                    m_ListRenderer = new MotionPathListRenderer(m_AnimEditor.motionPaths);
                }

                if (m_AnimEditor.motionPathClip != null)
                {
                    m_ListRenderer.UpdateList(m_AnimEditor.motionPaths);

                    m_ListRenderer.DoLayoutList();

                    EditorGUILayout.Space();

                    int i = m_ListRenderer.index;
                    if (i >= 0)
                    {
                        if (i > m_AnimEditor.motionPaths.Count - 1)
                        {
                            i = m_AnimEditor.motionPaths.Count - 1;
                        }

                        if (i >= 0)
                        {
                            MotionPath motionPath = m_AnimEditor.motionPaths[i];
                            if (!motionPath.isMissing && motionPath.hasCurveData)
                                motionPath.OnSettingsGUI(m_AnimEditor.motionPathClip);
                        }
                    }

                    if (m_EditMode)
                    {
                        if (HandleSelection.count == 1)
                        {
                            OnSelectedHandleGUI();
                        }
                        else if (HandleSelection.count > 1)
                        {
                            OnSelectionTransformGUI();
                        }
                    }
                }
            }


            EditorGUILayout.EndScrollView();
        }

        private void OnRootOffsetGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_RootOffsetEditor.OnGUI();
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void OnSelectionTransformGUI()
        {
            EditorGUI.BeginChangeCheck();
            m_SelectionTransformEditor.OnGUI();
            if (EditorGUI.EndChangeCheck())
            {
                HandleSelectionTransform.instance.UpdatePositions();
                m_AnimEditor.ApplyChages();
            }
        }


        private void OnSelectedHandleGUI()
        {
            EditorGUI.BeginChangeCheck();
            using (new CustomGUILayout.FoldoutWindowScope("Handle", out bool open))
            {
                if (open)
                {
                    HandleSelection.activeHandle.position = EditorGUILayout.Vector3Field("Position", HandleSelection.activeHandle.position);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                HandleSelection.activeHandle.hasChanged = true;
                m_AnimEditor.ApplyChages();
            }
        }
    }

    static class HideHandles
    {
        static bool s_Controls;
        static bool s_Tangents;

        public static bool controls => s_Controls && Settings.useHideHandles;
        public static bool tangents => s_Tangents && Settings.useHideHandles;

        public static void DrawFoldoutWindow()
        {
            EditorGUI.BeginChangeCheck();
            using (new CustomGUILayout.FoldoutWindowScope("Hide Handles", out bool open))
            {
                if (open)
                {
                    EditorGUILayout.BeginHorizontal();
                    s_Controls = CustomGUILayout.ToggleButton("Hide Controls", s_Controls);
                    s_Tangents = CustomGUILayout.ToggleButton("Hide Tangents", s_Tangents);
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }
    }

    static class Magnet
    {
        static float s_Radius = 5;
        static bool s_Controls = true;
        static bool s_Tangents = true;

        public static float radius => s_Radius;
        public static bool controls => s_Controls;
        public static bool tangents => s_Tangents;

        public static void DrawFoldoutWindow()
        {
            EditorGUI.BeginChangeCheck();
            using (new CustomGUILayout.FoldoutWindowScope("Magnet", out bool open))
            {
                if (open)
                {
                    GUILayout.BeginHorizontal();
                    s_Controls = CustomGUILayout.ToggleButton("Drag Controls", s_Controls);
                    s_Tangents = CustomGUILayout.ToggleButton("Drag Tangents", s_Tangents);
                    GUILayout.EndHorizontal();

                    s_Radius = EditorGUILayout.FloatField("Radius", s_Radius);
                    s_Radius = Mathf.Max(s_Radius, 0);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }
    }
}