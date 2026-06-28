using UnityEngine;
using UnityEditor;

namespace ScriptBoy.MotionPathAnimEditor
{

    internal class EditorGridUtility
    {
        public static bool IsSnapActive
        {
            get
            {
                return Event.current != null && EditorGUI.actionKey;
            }
        }

        public static Vector3 SnapToGrid(Vector3 position)
        {
            if (IsSnapActive)
            {
                float snap = OrderOfMagnitude(HandleUtility.GetHandleSize(position) / 0.7f);

                position.x = Snap(position.x, snap);
                position.y = Snap(position.y, snap);
                position.z = Snap(position.z, snap);
            }

            return position;
        }

        public static Vector3 SnapToLocalGrid(Vector3 position, Matrix4x4 local2WorldMatrix)
        {
            if (IsSnapActive)
            {
                position = local2WorldMatrix.inverse.MultiplyPoint(position);

                Vector3 scale = (local2WorldMatrix == null) ? Vector3.one : local2WorldMatrix.lossyScale;
                float scaleMax = Mathf.Max(scale.x, scale.y, scale.z);
                float snap = OrderOfMagnitude(HandleUtility.GetHandleSize(position) / 0.7f) / scaleMax;

                position.x = Snap(position.x, snap);
                position.y = Snap(position.y, snap);
                position.z = Snap(position.z, snap);

                position = local2WorldMatrix.MultiplyPoint(position);
            }

            return position;
        }

        private static float OrderOfMagnitude(float value)
        {
            return Mathf.Pow(10, Mathf.Floor(Mathf.Log10(value)));
        }

        private static float Snap(float value, float snap)
        {
            return Mathf.Round(value / snap) * snap;
        }
    }
}