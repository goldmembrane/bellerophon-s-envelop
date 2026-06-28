using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class Reflection
    {
        public static class AnimationWindow
        {
            public static Type type;

            /// <summary>
            /// internal AnimationWindowState state {get;}
            /// </summary>
            public static PropertyInfo state;

            /// <summary>
            /// internal bool hasFocus {get;}
            /// </summary>
            public static PropertyInfo hasFocus;

            static AnimationWindow()
            {
                type = ReflectionUtility.FindTypeInUnityEditor(nameof(AnimationWindow));

                state = ReflectionUtility.FindProperty(type, nameof(state));
                hasFocus = ReflectionUtility.FindProperty(type, nameof(hasFocus));
            }
        }

        public static class AnimationWindowState
        {
            public static Type type;

            /// <summary>
            /// public AnimationClip activeAnimationClip {get;}
            /// </summary>
            public static PropertyInfo activeAnimationClip;

            /// <summary>
            /// public GameObject activeRootGameObject {get;}
            /// </summary>
            public static PropertyInfo activeRootGameObject;

            /// <summary>
            /// public float currentTime {get; set;}
            /// </summary>
            public static PropertyInfo currentTime;

            /// <summary>
            /// private List<DopeLine> m_dopelinesCache;
            /// </summary>
            public static FieldInfo m_dopelinesCache;

            /// <summary>
            /// public bool KeyIsSelected(AnimationWindowKeyframe keyframe) {}
            /// </summary>
            public static MethodInfo KeyIsSelected;

            /// <summary>
            /// public void SelectKey(AnimationWindowKeyframe keyframe) {}
            /// </summary>
            public static MethodInfo SelectKey;

            /// <summary>
            /// public void UnselectKey(AnimationWindowKeyframe keyframe)
            /// </summary>
            public static MethodInfo UnselectKey;

            /// <summary>
            /// public TimeArea.TimeFormat timeFormat { get; set; }
            /// </summary>
            public static PropertyInfo timeFormat;

            /// <summary>
            /// public TimeArea timeArea { get; set; }
            /// </summary>
            public static PropertyInfo timeArea;

            /// <summary>
            /// public float minVisibleTime {get;}
            /// </summary>
            public static PropertyInfo minVisibleTime;

            /// <summary>
            /// public float maxVisibleTime {get;}
            /// </summary>
            public static PropertyInfo maxVisibleTime;

            static AnimationWindowState()
            {
                type = ReflectionUtility.FindTypeInUnityEditorInternal(nameof(AnimationWindowState));

                activeAnimationClip = ReflectionUtility.FindProperty(type, nameof(activeAnimationClip));
                activeRootGameObject = ReflectionUtility.FindProperty(type, nameof(activeRootGameObject));
                currentTime = ReflectionUtility.FindProperty(type, nameof(currentTime));
                timeFormat = ReflectionUtility.FindProperty(type, nameof(timeFormat));
                timeArea = ReflectionUtility.FindProperty(type, nameof(timeArea));
                minVisibleTime = ReflectionUtility.FindProperty(type, nameof(minVisibleTime));
                maxVisibleTime = ReflectionUtility.FindProperty(type, nameof(maxVisibleTime));

                m_dopelinesCache = ReflectionUtility.FindField(type, nameof(m_dopelinesCache));

                KeyIsSelected = ReflectionUtility.FindMethod(type, nameof(KeyIsSelected));
                SelectKey = ReflectionUtility.FindMethod(type, nameof(SelectKey));
                UnselectKey = ReflectionUtility.FindMethod(type, nameof(UnselectKey));
            }
        }

        public static class TimeArea
        {
            public static Type type;
            public static Type TimeFormatType;

            /// <summary>
            /// public TickHandler hTicks {get; set;}
            /// </summary>
            public static PropertyInfo hTicks;

            /// <summary>
            /// public virtual string FormatTickTime(float time, float frameRate, TimeArea.TimeFormat timeFormat)
            /// /// </summary>
            public static MethodInfo FormatTime;


            static TimeArea()
            {
                type = ReflectionUtility.FindTypeInUnityEditor(nameof(TimeArea));
                TimeFormatType = type.GetNestedType("TimeFormat");

                hTicks = ReflectionUtility.FindProperty(type, nameof(hTicks));
                FormatTime = ReflectionUtility.FindMethod(type, nameof(FormatTime));
            }
        }

        public static class TickHandler
        {
            public static Type type;

            /// <summary>
            /// public int GetLevelWithMinSeparation(float pixelSeparation)
            /// </summary>
            public static MethodInfo GetLevelWithMinSeparation;

            /// <summary>
            ///  public void GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels, List<float> list)
            /// </summary>
            public static MethodInfo GetTicksAtLevel;

            static TickHandler()
            {
                type = ReflectionUtility.FindTypeInUnityEditor(nameof(TickHandler));

                GetLevelWithMinSeparation = ReflectionUtility.FindMethod(type, nameof(GetLevelWithMinSeparation));
                GetTicksAtLevel = ReflectionUtility.FindMethod(type, nameof(GetTicksAtLevel), typeof(int), typeof(bool), typeof(List<float>));
            }
        }

        public static class DopeLine
        {
            public static Type type;

            /// <summary>
            /// public AnimationWindowCurve[] curves {get}
            /// </summary>
            public static PropertyInfo curves;

            /// <summary>
            /// public Type valueType {get;}
            /// </summary>
            public static PropertyInfo valueType;

            /// <summary>
            /// public bool hasChildren;
            /// </summary>
            public static FieldInfo hasChildren;

            /// <summary>
            /// public bool isMasterDopeline;
            /// </summary>
            public static FieldInfo isMasterDopeline;


            static DopeLine()
            {
                type = ReflectionUtility.FindTypeInUnityEditorInternal(nameof(DopeLine));

                curves = ReflectionUtility.FindProperty(type, nameof(curves));
                valueType = ReflectionUtility.FindProperty(type, nameof(valueType));

                hasChildren = ReflectionUtility.FindField(type, nameof(hasChildren));
                isMasterDopeline = ReflectionUtility.FindField(type, nameof(isMasterDopeline));
            }
        }

        public static class AnimationWindowCurve
        {
            public static Type type;

            /// <summary>
            /// public EditorCurveBinding binding {get;}
            /// </summary>
            public static PropertyInfo binding;

            /// <summary>
            /// public List<AnimationWindowKeyframe> m_Keyframes;
            /// </summary>
            public static FieldInfo m_Keyframes;

            static AnimationWindowCurve()
            {
                type = ReflectionUtility.FindTypeInUnityEditorInternal(nameof(AnimationWindowCurve));

                binding = ReflectionUtility.FindProperty(type, nameof(binding));

                m_Keyframes = ReflectionUtility.FindField(type, nameof(m_Keyframes));
            }
        }

        public static class AnimationWindowKeyframe
        {
            public static Type type;

            /// <summary>
            /// public float time {get; set;}
            /// </summary>
            public static PropertyInfo time;

            static AnimationWindowKeyframe()
            {
                type = ReflectionUtility.FindTypeInUnityEditorInternal(nameof(AnimationWindowKeyframe));

                time = ReflectionUtility.FindProperty(type, nameof(time));
            }
        }
    }

    static class ReflectionUtility
    {
        static BindingFlags s_Flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Static | BindingFlags.InvokeMethod;

        public static Type FindTypeInUnityEditor(string name)
        {
            return FindType($"UnityEditor.{name},UnityEditor");
        }

        public static Type FindTypeInUnityEditorInternal(string name)
        {
            return FindType($"UnityEditorInternal.{name},UnityEditor");
        }

        public static Type FindType(string name)
        {
            var type = Type.GetType(name);

            if (type == null)
            {
                throw new Exception($"The type '{name}' does not exist! ({Application.unityVersion})");
            }

            return type;
        }

        public static FieldInfo FindField(Type type, string name)
        {
            if (type == null) return null;

            var field = type.GetField(name, s_Flags);

            if (field == null)
            {
                throw new Exception($"The field '{type.Name}.{name}' does not exist! ({Application.unityVersion})");
            }

            return field;
        }

        public static PropertyInfo FindProperty(Type type, string name)
        {
            if (type == null) return null;

            var property = type.GetProperty(name, s_Flags);

            if (property == null)
            {
                throw new Exception($"The property '{type.Name}.{name}' does not exist! ({Application.unityVersion})");
            }

            return property;
        }

        public static MethodInfo FindMethod(Type type, string name)
        {
            if (type == null) return null;

            var method = type.GetMethod(name, s_Flags);

            if (method == null)
            {
                throw new Exception($"The method '{type.Name}.{name}' does not exist! ({Application.unityVersion})");
            }

            return method;
        }


        public static MethodInfo FindMethod(Type type, string name, params Type[] parameterTypes)
        {
            if (type == null) return null;

            MethodInfo[] methods = type.GetMethods(s_Flags);
            int parameterCount = parameterTypes.Length;
            foreach (var method in methods)
            {
                if (method.Name != name) continue;
                if (method.IsGenericMethod) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != parameterTypes.Length) continue;
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i]) continue;
                }
                return method;
            }

            string parameterTypeNames = "";
            for (int i = 0; i < parameterCount; i++)
            {
                if (i != 0) parameterTypeNames += ", ";
                parameterTypeNames += parameterTypes[i].FullName;
            }

            throw new Exception($"The method '{type.Name}.{name}({parameterTypeNames})' does not exist! ({Application.unityVersion})");
        }
    }


    static class MethodParameterArray
    {
        static Dictionary<int, object[]> s_Dictionary = new Dictionary<int, object[]>();

        static object[] GetArray(int parameterCount)
        {
            if (!s_Dictionary.TryGetValue(parameterCount, out object[] array))
            {
                array = new object[parameterCount];
                s_Dictionary.Add(parameterCount, array);
            }
            return array;
        }

        public static object[] Get(object parameter0)
        {
            object[] array = GetArray(1);
            array[0] = parameter0;
            return array;
        }

        public static object[] Get(object parameter0, object parameter1)
        {
            object[] array = GetArray(2);
            array[0] = parameter0;
            array[1] = parameter1;
            return array;
        }

        public static object[] Get(object parameter0, object parameter1, object parameter2)
        {
            object[] array = GetArray(3);
            array[0] = parameter0;
            array[1] = parameter1;
            array[2] = parameter2;
            return array;
        }

        public static object[] Get(object parameter0, object parameter1, object parameter2, object parameter3)
        {
            object[] array = GetArray(3);
            array[0] = parameter0;
            array[1] = parameter1;
            array[2] = parameter2;
            array[3] = parameter3;
            return array;
        }
    }
}