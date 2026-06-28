using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class BezierUtility
    {
        /// <summary>
        /// Evaluate the curve at time.
        /// </summary>
        public static Vector3 EvaluateBezierCurve(Vector3 start, Vector3 end, Vector3 startTangent, Vector3 endTangent, float t)
        {
            float x0 = start.x;
            float y0 = start.y;
            float z0 = start.z;

            float x1 = end.x;
            float y1 = end.y;
            float z1 = end.z;

            float x2 = startTangent.x;
            float y2 = startTangent.y;
            float z2 = startTangent.z;

            float x3 = endTangent.x;
            float y3 = endTangent.y;
            float z3 = endTangent.z;

            float t3 = 3 * t;
            float tt3 = 3 * t * t;
            float ttt = t * t * t;

            Vector3 v;
            v.x = ttt * (x1 - x0 + 3 * (x2 - x3)) + tt3 * (x0 + x3 - 2 * x2) + t3 * (x2 - x0) + x0;
            v.y = ttt * (y1 - y0 + 3 * (y2 - y3)) + tt3 * (y0 + y3 - 2 * y2) + t3 * (y2 - y0) + y0;
            v.z = ttt * (z1 - z0 + 3 * (z2 - z3)) + tt3 * (z0 + z3 - 2 * z2) + t3 * (z2 - z0) + z0;
            return v;
        }


        public static float GetLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            int n = 20;
            Vector3 prev = p0;
            float length = 0;
            for (int j = 1; j <= n; j++)
            {
                float t = (float)j / n;
                float i = (1 - t);
                Vector3 current = i * i * i * p0 + 3 * i * i * t * p1 + 3 * i * t * t * p2 + t * t * t * p3;
                length += Vector3.Distance(prev, current);
                prev = current;
            }
            return length;
        }
    }
}