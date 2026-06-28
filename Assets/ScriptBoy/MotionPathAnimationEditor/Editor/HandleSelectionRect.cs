using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class HandleSelectionRect
    {
        private static Rect s_Rect;
        private static Vector3 s_StartPoint;
        private static Vector3 s_EndPoint;
        private static HashSet<Handle> s_List;
        private static Texture2D s_Texture;
        private static bool s_IsActive;

        private static Texture2D texture
        {
            get
            {
                if (s_Texture == null)
                {
                    s_Texture = new Texture2D(1, 1);
                    s_Texture.SetPixel(0, 0, new Color32(0, 160, 235, 100));
                    s_Texture.Apply();
                }

                return s_Texture;
            }
        }

        public static bool IsActive => s_IsActive;

        static HandleSelectionRect()
        {
            s_List = new HashSet<Handle>();
        }

        public static void Start()
        {
            s_IsActive = true;
            s_StartPoint = Event.current.mousePosition;

            if (!Event.current.shift && !Event.current.control)
            {
                HandleSelection.Clear();
            }
        }

        public static void Stop()
        {
            s_IsActive = false;
            if (s_List.Count > 0) s_List.Clear();
            HandleSelectionTransform.instance.RefeshTransform(0);
            HandleSelectionTransform.instance.UpdateDefaultPositions();
        }

        public static void Check(Handle handle)
        {
            bool isInsideRect = s_Rect.Contains(HandleUtility.WorldToGUIPoint(handle.position));
            bool isSelected = handle.selected;

            if (Event.current.shift)
            {
                if (isInsideRect)
                {
                    if (!isSelected)
                    {
                        s_List.Add(handle);
                        HandleSelection.Select(handle);
                    }
                }
                else if (s_List.Contains(handle))
                {
                    if (isSelected)
                    {
                        s_List.Remove(handle);
                        HandleSelection.Unselect(handle);
                    }
                }
            }
            else if (Event.current.control)
            {
                if (isInsideRect)
                {
                    if (isSelected)
                    {
                        s_List.Add(handle);
                        HandleSelection.Unselect(handle);
                    }
                }
                else if (s_List.Contains(handle))
                {
                    if (!isSelected)
                    {
                        s_List.Remove(handle);
                        HandleSelection.Select(handle);
                    }
                }
            }
            else
            {
                if (isInsideRect)
                {
                    if (!isSelected)
                    {
                        HandleSelection.Select(handle);
                    }
                }
                else if (isSelected)
                {
                    HandleSelection.Unselect(handle);
                }
            }
        }

        public static void Update()
        {
            Vector3 oldEndPoint = s_EndPoint;
            s_EndPoint = Event.current.mousePosition;
            if (oldEndPoint != s_EndPoint)
            {
                float xMin = Mathf.Min(s_StartPoint.x, s_EndPoint.x);
                float yMin = Mathf.Min(s_StartPoint.y, s_EndPoint.y);
                float xMax = Mathf.Max(s_StartPoint.x, s_EndPoint.x);
                float yMax = Mathf.Max(s_StartPoint.y, s_EndPoint.y);
                s_Rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                GUI.changed = true;
            }
        }

        public static void Draw()
        {
            Handles.BeginGUI();
            GUI.DrawTexture(s_Rect, texture);
            Handles.EndGUI();
        }
    }
}