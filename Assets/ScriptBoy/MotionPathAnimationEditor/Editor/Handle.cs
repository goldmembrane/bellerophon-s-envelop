using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TangentMode = UnityEditor.AnimationUtility.TangentMode;

namespace ScriptBoy.MotionPathAnimEditor
{
    abstract class Handle
    {
        public Matrix4x4 matrix = Matrix4x4.identity;
        public Vector3 localPosition;
        public Vector3 position
        {
            get => matrix.MultiplyPoint(localPosition);
            set => localPosition = matrix.inverse.MultiplyPoint(value);
        }
        public Vector3 savedPosition;

        public Quaternion rotation = Quaternion.identity;

        public bool hasChanged;
        public bool selected;
        public bool hide;

        protected Color color => GetColor(selected);
        protected Color GetColor(bool selected) => selected ? Settings.handleColorSelected : Settings.handleColorNormal;

        protected abstract HandleCapShape capShape { get; }

        public Handles.CapFunction capFunction
        {
            get
            {
                switch (capShape)
                {
                    default:
                    case HandleCapShape.Cube: return Handles.CubeHandleCap;
                    case HandleCapShape.Sphere: return Handles.SphereHandleCap;
                    case HandleCapShape.Cone: return Handles.ConeHandleCap;
                }
            }
        }

        public void SavePosition()
        {
            savedPosition = position;
        }

        protected float GetSize()
        {
            return HandleUtility.GetHandleSize(position) * Settings.handleSize;
        }

        public void DoPositionHandle()
        {
            Quaternion q = Tools.pivotRotation == PivotRotation.Global ? Quaternion.identity : matrix.rotation;
            EditorGUI.BeginChangeCheck();
            var newPosition = Handles.DoPositionHandle(position, q);
            if (EditorGUI.EndChangeCheck())
            {
                newPosition = EditorGridUtility.SnapToGrid(newPosition);
                hasChanged = position != newPosition;
                position = newPosition;
            }
        }

        public bool FreeMoveHandle()
        {
            Handles.color = color;
            float size = GetSize();
            EditorGUI.BeginChangeCheck();
            var newPosition = Handles.FreeMoveHandle(position, size, Vector3.zero, HandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                if (Settings.useLocalSnappingIn2D)
                {
                    newPosition = EditorGridUtility.SnapToLocalGrid(newPosition, matrix);
                    hasChanged = position != newPosition;
                    position = newPosition;
                    GridRenderer.SetDefaultMatrix(matrix, Vector3.zero);
                }
                else
                {
                    newPosition = EditorGridUtility.SnapToGrid(newPosition);
                    hasChanged = position != newPosition;
                    position = newPosition;
                }
            }

            if (HandleUtility.DistanceToCircle(position, size) == 0)
            {
                if (MouseRecords.LeftClick)
                {
                    HandleSelection.ShiftSelect(this);
                    AnimEditorWindow.RepaintWindow();
                }

                return true;
            }

            return false;
        }

        private void HandleCap(int controlID, Vector3 position, Quaternion _rotation, float size, EventType eventType)
        {
            capFunction.Invoke(controlID, position, rotation, size, eventType);
        }

        public void DrawButton()
        {
            Handles.color = color;
            float size = GetSize();
            if (Handles.Button(position, rotation, size, size, capFunction))
            {
                HandleSelection.ShiftSelect(this);
                AnimEditorWindow.RepaintWindow();
            }
        }

        public void DrawHandleCap()
        {
            Handles.color = color;
            capFunction.Invoke(0, position, rotation, GetSize(), EventType.Repaint);
        }
    }

    class ControlHandle : Handle
    {
        public TangentHandle leftTangent;
        public TangentHandle rightTangent;

        public object[] animationWindowKeyframes = new object[3];
        public Vector3Int keyframeIndexes;


        public float time;
        public float length;
        public float weight;

        public bool hasParallelTangents;
        public float tangentsRaito;

        public bool hasLeftTangent => leftTangent.mode == TangentMode.Free && !leftTangent.hide;
        public bool hasRightTangent => rightTangent.mode == TangentMode.Free && !rightTangent.hide;


        protected override HandleCapShape capShape => Settings.handleCapControl;

        public ControlHandle()
        {
            leftTangent = new TangentHandle();
            rightTangent = new TangentHandle();
        }

        public bool DoFreeMoveHandles()
        {
            if (hide) return false;

            if (!HideHandles.tangents)
            {
                if (hasLeftTangent)
                {
                    Handles.color = GetColor(selected && leftTangent.selected);
                    Handles.DrawLine(position, leftTangent.position);
                    leftTangent.rotation = LookRotation(position - leftTangent.position);
                    leftTangent.FreeMoveHandle();
                }

                if (hasRightTangent)
                {
                    Handles.color = GetColor(selected && rightTangent.selected);
                    Handles.DrawLine(position, rightTangent.position);
                    rightTangent.rotation = LookRotation(rightTangent.position - position);
                    rightTangent.FreeMoveHandle();
                }
            }

            if (!HideHandles.controls)
            {
                return FreeMoveHandle();
            }

            return false;
        }

        public bool DrawSelectableButtons()
        {
            if (hide) return false;

            if (!HideHandles.tangents)
            {
                if (hasLeftTangent)
                {
                    Handles.color = GetColor(selected && leftTangent.selected);
                    Handles.DrawLine(position, leftTangent.position);

                    leftTangent.rotation = LookRotation(position - leftTangent.position);
                    leftTangent.DrawButton();
                }

                if (hasRightTangent)
                {
                    Handles.color = GetColor(selected && rightTangent.selected);
                    Handles.DrawLine(position, rightTangent.position);

                    rightTangent.rotation = LookRotation(rightTangent.position - position);
                    rightTangent.DrawButton();
                }
            }


            if (!HideHandles.controls)
            {
                DrawButton();

                return HandleUtility.DistanceToCircle(position, GetSize()) == 0;
            }

            return false;
        }

        public void DrawHandlesCap()
        {
            if (hide) return;

            if (!HideHandles.tangents)
            {
                if (hasLeftTangent)
                {
                    Handles.color = GetColor(selected && leftTangent.selected);
                    Handles.DrawLine(position, leftTangent.position);
                    leftTangent.DrawHandleCap();
                }

                if (hasRightTangent)
                {
                    Handles.color = GetColor(selected && rightTangent.selected);
                    Handles.DrawLine(position, rightTangent.position);
                    rightTangent.DrawHandleCap();
                }
            }

            if (!HideHandles.controls)
            {
                DrawHandleCap();
            }
        }

        public void CheckBoxSelection()
        {
            if (hide) return;

            if (!HideHandles.tangents)
            {
                if (hasLeftTangent) HandleSelectionRect.Check(leftTangent);
                if (hasRightTangent) HandleSelectionRect.Check(rightTangent);
            }

            if (!HideHandles.controls)
            {
                HandleSelectionRect.Check(this);
            }
        }

        public void SetMatrix(Matrix4x4 matrix)
        {
            this.matrix = matrix;
            leftTangent.matrix = matrix;
            rightTangent.matrix = matrix;
        }

        public void SetDirty()
        {
            hasChanged = leftTangent.hasChanged = rightTangent.hasChanged = true;
        }

        private Quaternion LookRotation(Vector3 dir)
        {
            if (dir == Vector3.zero || float.IsNaN(dir.x + dir.y + dir.z)) return Quaternion.identity;
            return Quaternion.LookRotation(dir);
        }


        public void Reset()
        {
            for (int i = 0; i < 3; i++)
            {
                animationWindowKeyframes[i] = null;
                keyframeIndexes[i] = -1;
            }

            localPosition = Vector3.zero;
            leftTangent.localPosition = Vector3.zero;
            rightTangent.localPosition = Vector3.zero;
        }
    }

    class TangentHandle : Handle
    {
        public TangentMode mode;
        public TangentMode[] modes = new TangentMode[3];
        public bool hasSimilarModes;

        protected override HandleCapShape capShape => Settings.handleCapTangent;
        public float scale;
    }

    class ControlHandleSequence
    {
        List<ControlHandle> m_Handles = new List<ControlHandle>();
        int m_Index;

        public void Reset()
        {
            m_Index = 0;
        }

        public ControlHandle Next()
        {
            if (m_Index >= m_Handles.Count)
            {
                m_Handles.Add(new ControlHandle());
            }

            return m_Handles[m_Index++];
        }
    }
}