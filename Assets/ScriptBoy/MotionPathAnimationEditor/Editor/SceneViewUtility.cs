using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class SceneViewUtility
    {
        public enum GridRenderAxis { X, Y, Z, All }

        private static Type s_SceneViewType = null;
        private static Type sceneViewType
        {
            get
            {
                if (s_SceneViewType == null)
                {
                    s_SceneViewType = typeof(SceneView);
                }
                return s_SceneViewType;
            }
        }

        private static Type s_SceneViewGridType = null;
        private static Type sceneViewGridType
        {
            get
            {
                if (s_SceneViewGridType == null)
                {
                    s_SceneViewGridType = Type.GetType("UnityEditor.SceneViewGrid,UnityEditor");
                }
                return s_SceneViewGridType;
            }
        }

        private static FieldInfo s_SceneViewFieldInfo_m_Grid;
        private static FieldInfo sceneViewFieldInfo_m_Grid
        {
            get
            {
                if (s_SceneViewFieldInfo_m_Grid == null)
                {
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    s_SceneViewFieldInfo_m_Grid = sceneViewType.GetField("m_Grid", flags);
                }

                return s_SceneViewFieldInfo_m_Grid;
            }
        }

        private static FieldInfo s_SceneViewGridFieldInfo_m_GridAxis;
        private static FieldInfo sceneViewGridFieldInfo_m_GridAxis
        {
            get
            {
                if (s_SceneViewGridFieldInfo_m_GridAxis == null)
                {
                    BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    s_SceneViewGridFieldInfo_m_GridAxis = sceneViewGridType.GetField("m_GridAxis", flags);
                }

                return s_SceneViewGridFieldInfo_m_GridAxis;
            }
        }

        public static GridRenderAxis GetGridAxis(SceneView sceneView)
        {
            object sceneViewGrid = sceneViewFieldInfo_m_Grid.GetValue(sceneView);
            return (GridRenderAxis)sceneViewGridFieldInfo_m_GridAxis.GetValue(sceneViewGrid);
        }

        public static void SetGridAxis(SceneView sceneView, GridRenderAxis gridAxis)
        {
            object sceneViewGrid = sceneViewFieldInfo_m_Grid.GetValue(sceneView);
            object enumObject = Enum.ToObject(sceneViewGridFieldInfo_m_GridAxis.FieldType, gridAxis);
            sceneViewGridFieldInfo_m_GridAxis.SetValue(sceneViewGrid, enumObject);
        }

        public static bool IsFreeMode(SceneView sceneView)
        {
            Vector3 q = sceneView.rotation.eulerAngles;

            if (q.x == 000 && q.y == 270 && q.z == 0) return false; // Right
            if (q.x == 090 && q.y == 000 && q.z == 0) return false; // Top
            if (q.x == 270 && q.y == 000 && q.z == 0) return false; // Bottom
            if (q.x == 000 && q.y == 180 && q.z == 0) return false; // Front
            if (q.x == 000 && q.y == 000 && q.z == 0) return false; // Back

            return true;
        }

        public static bool Is2DMode(SceneView sceneView)
        {
            return sceneView.in2DMode || sceneView.orthographic && !IsFreeMode(sceneView);
        }
    }
}


/***************************
 * UnityEditor Source Code *
 ***************************
 
 namespace UnityEditor
{
	[EditorWindowTitle(title = "Scene", useTypeNameAsIconName = true)]
	public class SceneView : SearchableEditorWindow, IHasCustomMenu
	{
		[SerializeField]
		private SceneViewGrid m_Grid;
	}

	[Serializable]
	internal class SceneViewGrid
	{
		[SerializeField]
		private SceneViewGrid.GridRenderAxis m_GridAxis = SceneViewGrid.GridRenderAxis.Y;
		internal enum GridRenderAxis {X, Y, Z, All}
	}
}
*/