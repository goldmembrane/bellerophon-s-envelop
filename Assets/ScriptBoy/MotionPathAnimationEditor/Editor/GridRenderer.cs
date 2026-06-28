using UnityEngine;
using UnityEditor;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class GridRenderer
    {
        public static bool active;

        private static Matrix4x4 s_DefaultMatrix = Matrix4x4.identity;
        private static Vector3 s_DefaultOffset;
        private static readonly Color s_GridColor = new Color(0, 0.75f, 1, 0.2f);
        private static readonly Color s_AxisColor = new Color(0, 0.75f, 1, 1f);

        public static void SetDefaultMatrix(Matrix4x4 matrix, Vector3 offset)
        {
            s_DefaultOffset = offset;
            s_DefaultMatrix = matrix;
            active = false;
        }

        public static void Draw()
        {
            Draw(s_DefaultMatrix, s_DefaultOffset);
        }

        public static void Draw(Matrix4x4 matrix, Vector3 offset)
        {
            Vector3 pivot;
            Vector3 right;
            Vector3 up;
            Vector3 forward;
            Vector3 scale;

            if (matrix == null)
            {
                pivot = Vector3.zero;
                right = Vector3.right;
                up = Vector3.up;
                forward = Vector3.forward;
                scale = Vector3.one;
            }
            else
            {
                offset = matrix.MultiplyVector(offset);
                pivot = matrix.MultiplyPoint(Vector3.zero);
                right = matrix.MultiplyVector(Vector3.right);
                up = matrix.MultiplyVector(Vector3.up);
                forward = matrix.MultiplyVector(Vector3.forward);
                scale = matrix.lossyScale;
            }

            float scaleMax = Mathf.Max(scale.x, scale.y, scale.z);
            float gridSize = OrderOfMagnitude(HandleUtility.GetHandleSize(offset + pivot) / (0.7f)) / scaleMax;

            bool xAxis = true;
            bool yAxis = true;
            bool zAxis = false;

            using (new Handles.DrawingScope(s_GridColor))
            {
                Vector3 center = offset + pivot;

                for (int i = -99; i < 100; i++)
                {
                    float step = i * gridSize;

                    float min = (-100) * gridSize;
                    float max = (100) * gridSize;

                    Vector3 a;
                    Vector3 b;

                    if (xAxis && yAxis)
                    {
                        a = center + right * step + up * min;
                        b = center + right * step + up * max;
                        Handles.DrawLine(a, b);

                        a = center + right * min + up * step;
                        b = center + right * max + up * step;
                        Handles.DrawLine(a, b);
                    }

                    if (zAxis && yAxis)
                    {
                        a = center + forward * step + up * min;
                        b = center + forward * step + up * max;
                        Handles.DrawLine(a, b);

                        a = center + forward * min + up * step;
                        b = center + forward * max + up * step;
                        Handles.DrawLine(a, b);
                    }

                    if (zAxis && xAxis)
                    {
                        a = center + forward * step + right * min;
                        b = center + forward * step + right * max;
                        Handles.DrawLine(a, b);

                        a = center + forward * min + right * step;
                        b = center + forward * max + right * step;
                        Handles.DrawLine(a, b);
                    }
                }
            }

            using (new Handles.DrawingScope(s_AxisColor))
            {
                float min = (-100) * gridSize;
                float max = (100) * gridSize;

                Vector3 a;
                Vector3 b;

                if (yAxis)
                {
                    a = pivot + up * min;
                    b = pivot + up * max;
                    Handles.DrawLine(a, b);
                }

                if (xAxis)
                {
                    a = pivot + right * min;
                    b = pivot + right * max;
                    Handles.DrawLine(a, b);
                }

                if (zAxis)
                {
                    a = pivot + forward * min;
                    b = pivot + forward * max;
                    Handles.DrawLine(a, b);
                }
            }
        }

        private static float OrderOfMagnitude(float v)
        {
            return Mathf.Pow(10, Mathf.Floor(Mathf.Log10(v)));
        }

        private static float Round(float v, float d)
        {
            return Mathf.Round(v / d) * d;
        }
    }
}