using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    class AnimEditor
    {
        public static AnimEditor instance { get; private set; }

        public static AnimationWindowWrapper animationWindow;
        public static AnimationClip animationClip;

        public static Transform root;
        public static MotionPath rootMotionPath;

        public MotionPathClip motionPathClip;
        public List<MotionPath> motionPaths => motionPathClip.motionPaths;

        //Dictionary<Transform, List<MotionPath>> motionPathsPerRoot;

        public bool editMode;

        public AnimEditor()
        {
            //motionPathsPerRoot = new Dictionary<Transform, List<MotionPath>>();
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui +=  OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
            Undo.undoRedoPerformed += UndoRedoPerformed;
            instance = this;
        }

        public void Destroy()
        {
            instance = null; 
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -=  OnSceneGUI;
#else
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            SaveMotionPathList();
        }

        public List<MotionPath> GetMotionPathList(Transform root)
        {
            if (AnimEditor.root == root)
            {
                return motionPaths;
            }

            throw new System.Exception("");
            /*
            if (motionPathsPerRoot.ContainsKey(root))
            {
                return motionPathsPerRoot[root];
            }
            else
            {
                var list = new List<MotionPath>();
                motionPathsPerRoot.Add(root, list);
                return list;
            }*/
        }

        private void UndoRedoPerformed()
        {
            if (GetAnimationInfo())
            {
                SampleAnimation(animationWindow.time);
            }
        }

        #region OnSceneGUI
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!GetAnimationInfo()) return;
            if (motionPaths == null) return;
            if (motionPaths.Count == 0) return;

            if (TimeRange.clamp = Settings.useTimeRange)
            {
                TimeRange.min = animationWindow.minVisibleTime;
                TimeRange.max = animationWindow.maxVisibleTime;
            }

            BeginEditor();
            DoEditor(sceneView);
            EndEditor();
        }

        private void BeginEditor()
        {
            if (Settings.pathSpace == Space.World)
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.active)
                    {
                        motionPath.UpdateHandles();
                    }
                }

                if (Settings.showTimeTicks || Settings.showTimeLabels)
                {
                    if (editMode || !editMode && GUIUtility.hotControl == 0)
                    {
                        float defaultTime = animationWindow.time;

                        int motionPathCount = motionPaths.Count;

                        int frameCount = (int)(animationClip.length * animationClip.frameRate);
                        float frameToTime = 1 / animationClip.frameRate;
                        float time2Frame = animationClip.frameRate;

                        for (int i = 0; i < motionPathCount; i++)
                        {
                            var motionPath = motionPaths[i];
                            if (motionPath.isMissing) continue;
                            if (motionPath.active) motionPath.StartCachingWorldFramePositions();
                        }

                        for (int frame = 0; frame <= frameCount; frame++)
                        {
                            float time = frame * frameToTime;

                            //if (!TimeRange.Contains(time)) continue;

                            SampleAnimation(time);

                            for (int i = 0; i < motionPathCount; i++)
                            {
                                var motionPath = motionPaths[i];
                                if (motionPath.isMissing) continue;
                                if (motionPath.active) motionPath.CacheWorldFramePosition();
                            }
                        }

                        SampleAnimation(defaultTime);
                    }
                }

                if (editMode || !editMode && GUIUtility.hotControl == 0)
                {
                    float defaultTime = animationWindow.time;

                    int motionPathCount = motionPaths.Count;
                    float frameRate = animationClip.frameRate;
                    int frameCount = (int)(animationClip.length * frameRate);
                    float frameToTime = 1 / frameRate;
                    float timeToFrame = frameRate;
                    HashSet<int> hotFrames = new HashSet<int>();

                    for (int i = 0; i < motionPathCount; i++)
                    {
                        var motionPath = motionPaths[i];
                        if (motionPath.active) motionPath.StartCachingWorldPath(frameCount, timeToFrame, hotFrames);
                    }

                    int skipFrame = (int)(frameCount / frameRate) / 2;
                    if (skipFrame <= 0) skipFrame = 1;

                    for (int frame = 0; frame <= frameCount; frame++)
                    {
                        float time = frame * frameToTime;

                        if (!TimeRange.Contains(time)) continue;

                        if (frame != 0 && frame != frameCount)
                        {
                            if (frame % skipFrame != 0 && !hotFrames.Contains(frame)) continue;
                        }


                        SampleAnimation(time);

                        for (int i = 0; i < motionPathCount; i++)
                        {
                            var motionPath = motionPaths[i];
                            if (motionPath.active) motionPath.CacheWorldPosition(frame, frameToTime, frameCount);
                        }
                    }

                    SampleAnimation(defaultTime);
                }
            }
            else
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.active)
                    {
                        motionPath.UpdateHandles();
                        motionPath.UpdateHandlesMatrix();
                    }
                }
            }

            foreach (var motionPath in motionPaths)
            {
                if (motionPath.active)
                {
                    motionPath.SyncSelection();
                }
            }
        }

        private void SampleAnimation(float time)
        {
            animationClip.SampleAnimation(root.gameObject, time);
        }

        private void EndEditor()
        {
            if (!GUI.changed) return;

            ApplyChages();
            AnimEditorWindow.RepaintWindow();
        }

        public void ApplyChages()
        {
            animationClip.SampleAnimation(root.gameObject, animationWindow.time);

            foreach (var motionPath in motionPaths)
            {
                if (motionPath.active && motionPath.editable)
                {
                    motionPath.ApplyChages();


                }
            }
        }

        private void DoEditor(SceneView sceneView)
        {
            if (sceneView.in2DMode && Settings.useLocalSnappingIn2D && EditorGridUtility.IsSnapActive && HandleSelection.count < 2)
            {
                GridRenderer.Draw();
            }

            DrawMotionPaths();

            if (editMode) EditMotionPaths();
        }

        private void DrawMotionPaths()
        {
            if (Settings.pathSpace == Space.World)
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.active) motionPath.DrawWorldPath();
                }
            }
            else
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.active) motionPath.DrawCurves();
                }

                if (Settings.showTimeTicks || Settings.showTimeLabels)
                {
                    foreach (var motionPath in motionPaths)
                    {
                        if (motionPath.active) motionPath.CacheLocalFramePositions();
                    }
                }
            }



            if (Settings.showTimeTicks)
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.active) motionPath.DrawTimeTicks();
                }
            }

            if (Settings.showTimeLabels)
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.active) motionPath.DrawTimeLabels();
                }
            }
        }

        private void EditMotionPaths()
        {
            int mouseControlID = GUIUtility.GetControlID(FocusType.Passive);
   
            if (HandleSelectionRect.IsActive)
            {
                DoBoxSelection();
            }

            var EVENT = Event.current;
            bool startMagnet = Settings.useMagnet && (Magnet.controls || Magnet.tangents) && EVENT.isMouse && EVENT.button == 0 && EVENT.type == EventType.MouseDown;

            if (HandleSelection.count > 1)
            {
                EditSelection();
            }
            else
            {
                EditMono();
            }

            if (Settings.useMagnet && (Tools.current != Tool.Rotate || Tools.current != Tool.Scale))
            {
                foreach (var m in motionPaths)
                {
                    if (startMagnet)
                    {
                        m.StartMagnet();
                    }
                    else
                    {
                        m.UpdateMagnet();
                    }
                }
            }

            if (EVENT.isMouse && EVENT.button == 0)
            {
                if (EVENT.type == EventType.MouseDown && EVENT.button == 0)
                {
                    GUIUtility.hotControl = mouseControlID;
                    EVENT.Use();
                    HandleSelectionRect.Start();
                }

                if (EVENT.type == EventType.MouseUp)
                {
                    EVENT.Use();
                    GUIUtility.hotControl = 0;

                    if (HandleSelectionRect.IsActive)
                    {
                        HandleSelectionRect.Stop();
                        AnimEditorWindow.RepaintWindow();
                    }
                    else
                    {
                        HandleSelection.Clear();
                    }
                }
            }

            if (HandleSelectionRect.IsActive)
            {
                HandleSelectionRect.Draw();
                if (GUIUtility.hotControl != mouseControlID)
                {
                    HandleSelectionRect.Stop();
                }
            }
        }

        private void DoBoxSelection()
        {
            HandleSelectionRect.Update();

            foreach (var motionPath in motionPaths)
            {
                if (motionPath.isEditable)
                {
                    motionPath.CheckBoxSelection();
                }
            }
        }

        private void EditMono()
        {
            if (SceneViewUtility.Is2DMode(SceneView.currentDrawingSceneView))
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.isEditable) motionPath.EditCurves2D();
                }
            }
            else
            {
                foreach (var motionPath in motionPaths)
                {
                    if (motionPath.isEditable) motionPath.EditCurves3D();
                }
            }
        }

        private void EditSelection()
        {
            DrawHandleCaps();

            HandleSelectionTransform.instance.CheckTimeChanges(animationWindow.time);

            switch (Tools.current)
            {
                case Tool.Move:
                    HandleSelectionTransform.instance.DoPositionHandle();
                    break;
                case Tool.Rotate:
                    HandleSelectionTransform.instance.DoRotationHandle();
                    break;
                case Tool.Scale:
                    HandleSelectionTransform.instance.DoScaleHandle();
                    break;
            }
        }

        private void DrawHandleCaps()
        {
            foreach (var motionPath in motionPaths)
            {
                if (motionPath.isEditable)
                {
                    motionPath.DrawSelectionButtons();
                }
            }
        }
        #endregion

        #region Get Animation Info

        void SaveMotionPathList()
        {
            if (animationClip == null)
            {
                motionPathClip = null;
                return;
            }

            string path = AssetDatabase.GetAssetPath(animationClip);
            motionPathClip = AssetDatabase.LoadAssetAtPath<MotionPathClip>(path);
            if (motionPathClip == null)
            {
                motionPathClip = ScriptableObject.CreateInstance<MotionPathClip>();
                motionPathClip.hideFlags = HideFlags.DontSaveInBuild | HideFlags.HideInInspector;
                motionPathClip.name = "Motion Path Clip";
                AssetDatabase.AddObjectToAsset(motionPathClip, animationClip);
            }

            EditorUtility.SetDirty(motionPathClip);
            EditorUtility.SetDirty(animationClip);
            AssetDatabase.SaveAssetIfDirty(motionPathClip);
            AssetDatabase.SaveAssetIfDirty(animationClip);
            AssetDatabase.Refresh();

            LoadMotionPathList();
        }


        void LoadMotionPathList()
        {
            if (animationClip == null)
            {
                motionPathClip = null;
                return;
            }

            string path = AssetDatabase.GetAssetPath(animationClip);

            motionPathClip = AssetDatabase.LoadAssetAtPath<MotionPathClip>(path);

            if (motionPathClip != null)
            {
                foreach (var motionPath in motionPaths)
                {
                    motionPath.Reload();
                }
            }


        }

        private bool GetAnimationInfo()
        {
            //Get AnimationWindow
            if (!animationWindow)
            {
                animationWindow = AnimationWindowWrapper.FindWindow();
                AnimEditorWindow.RepaintWindow();
                SaveMotionPathList();
                animationClip = null;
                return false;
            } else animationWindow.RefreshState();

            //Get RootGameObject 
            var newRoot = animationWindow.root;
            if (newRoot == null)
            {
                AnimEditorWindow.RepaintWindow();
                return false;
            }

            if (root != newRoot)
            {
                if (root != null)
                {
                    SaveMotionPathList();
                }

                root = newRoot;
                if (root != null)
                {
                    LoadMotionPathList();
                    /*
                    if (motionPathsPerRoot.ContainsKey(root))
                    {
                        motionPaths.AddRange(motionPathsPerRoot[root]);
                        motionPathsPerRoot.Remove(root);
                    }
                    */
                }

                AnimEditorWindow.RepaintWindow();
            }

            if (root == null) return false;




            //Get AnimationClip
            var newAnimationClip = animationWindow.animationClip;
            if (newAnimationClip != null && newAnimationClip.hideFlags == HideFlags.NotEditable)
            {
                newAnimationClip = null;
            }

            if (animationClip != newAnimationClip)
            {
                SaveMotionPathList();
                animationClip = newAnimationClip;
                LoadMotionPathList();

        

                HandleSelection.Clear();
                AnimEditorWindow.RepaintWindow();
            }
            if (animationClip == null) return false;



            if (motionPathClip == null)
            {
                SaveMotionPathList();

            }

            return UpdateMotionPathsCurvesInfo();
        }

        private bool UpdateMotionPathsCurvesInfo()
        {
            if (motionPathClip == null) return false;

            foreach (var motionPath in motionPaths)
            {
                if (motionPath.isMissing)
                {
                    motionPath.Reload();
                }

                motionPath.ClearAnimationCurves();
            }

            var positionCurveBindings = PositionCurveBindingsCollector.GetList(animationClip, root);

            if (!animationWindow.FindAnimationWindowKeyframes(positionCurveBindings)) return false;

            foreach (var motionPath in motionPaths)
            {
                foreach (var positionCurveBinding in positionCurveBindings)
                {
                    if (motionPath.transform == positionCurveBinding.transform)
                    {
                        motionPath.SetAnimationCurves(positionCurveBinding);
                        break;
                    }
                }
            }

            rootMotionPath = motionPaths.Find(m => m.transform == root);
            return true;
        }
        #endregion
    }

    static class TimeRange
    {
        public static bool clamp;
        public static float min;
        public static float max;

        public static bool Contains(float time)
        {
            if (clamp) return min <= time && time <= max;

            return true;
        }
    }
}
