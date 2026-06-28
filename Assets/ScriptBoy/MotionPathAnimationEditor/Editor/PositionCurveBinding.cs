using System.Collections;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    class PositionCurveBinding
    {
        public string path;
        public Transform transform;

        private EditorCurveBinding m_X;
        private EditorCurveBinding m_Y;
        private EditorCurveBinding m_Z;


        public IList[] animationWindowKeyframes = new IList[3];


        public bool HasBinding(int axisIndex)
        {
            if (axisIndex == 0) return hasX;
            if (axisIndex == 1) return hasY;
            if (axisIndex == 2) return hasY;

            throw new System.NotImplementedException();
        }

        public EditorCurveBinding GetBinding(int axisIndex)
        {
            if (axisIndex == 0) return x;
            if (axisIndex == 1) return y;
            if (axisIndex == 2) return z;

            throw new System.NotImplementedException();
        }

        public EditorCurveBinding x
        {
            get
            {
                if (!hasX) throw new UnassignedFieldException("x");
                return m_X;
            }
            set
            {
                m_X = value;
                hasX = true;
            }
        }

        public EditorCurveBinding y
        {
            get
            {
                if (!hasY) throw new UnassignedFieldException("y");
                return m_Y;
            }
            set
            {
                m_Y = value;
                hasY = true;
            }
        }

        public EditorCurveBinding z
        {
            get
            {
                if (!hasZ) throw new UnassignedFieldException("z");
                return m_Z;
            }
            set
            {
                m_Z = value;
                hasZ = true;
            }
        }

        public bool hasX;
        public bool hasY;
        public bool hasZ;


        public bool Compare(EditorCurveBinding[] bindings)
        {
            if (hasX && m_X == bindings[0]) return true;
            if (hasY && m_Y == bindings[1]) return true;
            if (hasZ && m_Z == bindings[2]) return true;

            return false;
        }
    }

    internal class UnassignedFieldException : System.Exception
    {
        public UnassignedFieldException(string name) : base($"The variable ({name}) has been not assigned!")
        {

        }
    }
}