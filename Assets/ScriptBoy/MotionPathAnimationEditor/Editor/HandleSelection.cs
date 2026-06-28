using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{

    static class HandleSelection
    {
        private static List<Handle> s_Handles = new List<Handle>();

        public static List<Handle> handles => s_Handles;

        public static Handle activeHandle => s_Handles[0];

        public static int count => s_Handles.Count;

        public static Vector3 center
        {
            get
            {
                if (count == 0) return Vector3.zero;

                Vector3 sum = Vector3.zero;
                foreach (var handle in s_Handles)
                {
                    sum += handle.position;
                }

                return sum / count;
            }
        }

        public static Quaternion rotation
        {
            get
            {
                if (count == 0) return Quaternion.identity;

                Quaternion q = activeHandle.matrix.rotation;
                foreach (var handle in handles)
                {
                    Quaternion q2 = handle.matrix.rotation;
                    if (q != q2) return Quaternion.identity;
                }
                return q;
            }
        }

        public static void Clear()
        {
            foreach (var handle in s_Handles.ToArray())
            {
                Unselect(handle);
            }
        }

        public static bool Contains(Handle handle)
        {
            return s_Handles.Contains(handle);
        }

        public static void Remove(Handle handle)
        {
            if (Contains(handle))
            {
                s_Handles.Remove(handle);
            }

            handle.selected = false;
        }

        public static void Add(Handle handle)
        {
            if (!Contains(handle))
            {
                s_Handles.Add(handle);
            }

            handle.selected = true;
        }

        public static void Unselect(Handle handle)
        {
            if (Contains(handle))
            {
                Remove(handle);

                if (handle is ControlHandle)
                {
                    AnimEditor.animationWindow.UnselectKeyframes((handle as ControlHandle).animationWindowKeyframes);
                }
            }
        }

        public static void Select(Handle handle)
        {
            if (!Contains(handle))
            {
                Add(handle);

                if (handle is ControlHandle)
                {
                    AnimEditor.animationWindow.SelectKeyframes((handle as ControlHandle).animationWindowKeyframes);
                }
            }
        }

        public static void ShiftSelect(Handle handle)
        {
            if (!Event.current.shift) Clear();

            if (Contains(handle))
            {
                Unselect(handle);
            }
            else
            {
                Select(handle);
            }

            HandleSelectionTransform.instance.position = center;
            HandleSelectionTransform.instance.UpdateDefaultPositions();
        }

        public static void UnselectTangents()
        {
            foreach (var handle in s_Handles.ToArray())
            {
                if(handle is TangentHandle) Unselect(handle);
            }
        }
    }
}