using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    class AnimationWindowWrapper
    {
        EditorWindow m_Window;
        object m_State;
        List<float> m_TickCache;

        public AnimationWindowWrapper(EditorWindow window)
        {
            m_Window = window;
            m_State = Reflection.AnimationWindow.state.GetValue(m_Window);
            m_TickCache = new List<float>();
        }

        public bool isDisposed => m_Window == null;

        public float time
        {
            get
            {
                return (float)Reflection.AnimationWindowState.currentTime.GetValue(m_State);
            }
        }

        public TimeFormat timeFormat
        {
            get
            {
                return (TimeFormat)Reflection.AnimationWindowState.timeFormat.GetValue(m_State);
            }
        }

        public AnimationClip animationClip
        {
            get
            {
                return (AnimationClip)Reflection.AnimationWindowState.activeAnimationClip.GetValue(m_State);
            }
        }

        public GameObject rootGameObject
        {
            get
            {
                return (GameObject)Reflection.AnimationWindowState.activeRootGameObject.GetValue(m_State);
            }
        }

        public float minVisibleTime
        {
            get
            {
                return (float)Reflection.AnimationWindowState.minVisibleTime.GetValue(m_State);
            }
        }

        public float maxVisibleTime
        {
            get
            {
                return (float)Reflection.AnimationWindowState.maxVisibleTime.GetValue(m_State);
            }
        }

        public Transform root
        {
            get
            {
                return rootGameObject?.transform;
            }
        }

        public bool hasFocus
        {
            get
            {
                return Convert.ToBoolean(Reflection.AnimationWindow.hasFocus.GetValue(m_Window));
            }
        }


        public int maxTickLevel
        {
            get
            {
                object timeArea = Reflection.AnimationWindowState.timeArea.GetValue(m_State);
                object hTicks = Reflection.TimeArea.hTicks.GetValue(timeArea);
                return (int)Reflection.TickHandler.GetLevelWithMinSeparation.Invoke(hTicks, MethodParameterArray.Get(40f));
            }
        }

        public int GetTickFrameRate(int tickLevel)
        {
            object timeArea = Reflection.AnimationWindowState.timeArea.GetValue(m_State);
            object hTicks = Reflection.TimeArea.hTicks.GetValue(timeArea);
            m_TickCache.Clear();

            Reflection.TickHandler.GetTicksAtLevel.Invoke(hTicks, MethodParameterArray.Get(tickLevel, false, m_TickCache));

            float frameRate = animationClip.frameRate;

            int min = int.MaxValue;
            int max = int.MinValue;
            int a = Mathf.RoundToInt(m_TickCache[0] * frameRate);
            int n = Mathf.Min(m_TickCache.Count, 6);
            for (int i = 1; i < n; i++)
            {
                int b = Mathf.RoundToInt(m_TickCache[i] * frameRate);
                int delta = b - a;
                min = Mathf.Min(min, delta);
                max = Mathf.Max(max, delta);
                a = b;
            }

            if (min == max) return max;

            return min + max;
        }


        public string FormatTickTime(float time)
        {
            object timeArea = Reflection.AnimationWindowState.timeArea.GetValue(m_State);
            float frameRate = animationClip.frameRate;
            var timeFormat = this.timeFormat;
            return (string)Reflection.TimeArea.FormatTime.Invoke(timeArea, MethodParameterArray.Get(time, frameRate, (int)timeFormat));
        }

        public void Focus()
        {
            m_Window.Focus();
        }

        public static implicit operator bool(AnimationWindowWrapper obj)
        {
            return obj != null && !obj.isDisposed;
        }

        public void RefreshState()
        {
            m_State = Reflection.AnimationWindow.state.GetValue(m_Window);
        }

        public bool FindAnimationWindowKeyframes(List<PositionCurveBinding> positionCurveBindings)
        {
            if (!Settings.syncSelection) return true;

            IList dopelines = (IList)Reflection.AnimationWindowState.m_dopelinesCache.GetValue(m_State);
            if (dopelines == null) return false;

            foreach (var dopeline in dopelines)
            {
                bool isMasterDopeline = (bool)Reflection.DopeLine.isMasterDopeline.GetValue(dopeline);
                bool hasChildren = (bool)Reflection.DopeLine.hasChildren.GetValue(dopeline);

                if (isMasterDopeline) continue;
                if (!hasChildren) continue;

                var curves = (IList)Reflection.DopeLine.curves.GetValue(dopeline);
                if (curves.Count != 3) continue;

                EditorCurveBinding[] bindings = new EditorCurveBinding[3];

                for (int i = 0; i < 3; i++)
                {
                    bindings[i] = (EditorCurveBinding)Reflection.AnimationWindowCurve.binding.GetValue(curves[0]);
                }
                var valueType = bindings[0].type;

                if (valueType != typeof(Transform) && valueType != typeof(RectTransform)) continue;

                var keys = new IList[3];
                for (int i = 0; i < 3; i++)
                {
                    keys[i] = (IList)Reflection.AnimationWindowCurve.m_Keyframes.GetValue(curves[i]);
                }

                foreach (var positionCurveBinding in positionCurveBindings)
                {
                    if (positionCurveBinding.Compare(bindings))
                    {
                        positionCurveBinding.animationWindowKeyframes = keys;
                        break;
                    }
                }
            }

            return true;
        }

        public void SyncSelection(List<ControlHandle> handles)
        {
            if (!Settings.syncSelection) return;
            bool changed = false;

            foreach (var handle in handles)
            {
                if (AreAllNull(handle.animationWindowKeyframes)) continue;

                bool selected = IsAnyKeySelected(handle.animationWindowKeyframes);
                if (handle.selected != selected)
                {
                    if (selected) HandleSelection.Add(handle);
                    else HandleSelection.Remove(handle);

                    changed = true;
                }
            }

            if (changed)
            {
                if (!(Event.current.shift || Event.current.control))
                {
                    HandleSelection.UnselectTangents();
                }
                HandleSelectionTransform.instance.position = HandleSelection.center;
                HandleSelectionTransform.instance.UpdateDefaultPositions();
                AnimEditorWindow.RepaintWindow();
            }
        }

        bool AreAllNull(object[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] != null) return false;
            }

            return true;
        }

        bool IsAnyKeySelected(object[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == null) continue;
                if ((bool)Reflection.AnimationWindowState.KeyIsSelected.Invoke(m_State, new object[] { keys[i] })) return true;
            }

            return false;
        }

        public void SelectKeyframes(object[] keys)
        {
            if (!Settings.syncSelection) return;

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == null) continue;
                Reflection.AnimationWindowState.SelectKey.Invoke(m_State, new object[] { keys[i] });
            }
        }

        public void UnselectKeyframes(object[] keys)
        {
            if (!Settings.syncSelection) return;

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == null) continue;
                Reflection.AnimationWindowState.UnselectKey.Invoke(m_State, new object[] { keys[i] });
            }
        }

        public static AnimationWindowWrapper GetWindow()
        {
            return new AnimationWindowWrapper(EditorWindow.GetWindow(Reflection.AnimationWindow.type));
        }

        public static AnimationWindowWrapper FindWindow()
        {
            var windows = Resources.FindObjectsOfTypeAll(Reflection.AnimationWindow.type);

            if (windows.Length > 0)
            {
                return new AnimationWindowWrapper(windows[0] as EditorWindow);
            }

            return null;
        }


    }

    public enum TimeFormat { None = 0, TimeFrame = 1, Frame = 2}
}