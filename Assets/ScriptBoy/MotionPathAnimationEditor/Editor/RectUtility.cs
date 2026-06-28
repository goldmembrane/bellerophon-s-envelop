using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class RectUtility
    {
        public static Rect DockLeft(Rect rect, float value)
        {
            rect.width = value;
            return rect;
        }

        public static Rect DockRight(Rect rect, float value)
        {
            rect.x += rect.width - value;
            rect.width = value;
            return rect;
        }

        public static Rect Shrink(Rect rect, float value)
        {
            return Shrink(rect, value, value);
        }

        public static Rect Shrink(Rect rect, float horizontal, float vertical)
        {
            return Shrink(rect, horizontal, vertical, horizontal, vertical);
        }

        public static Rect Shrink(Rect rect, float left, float up, float right, float down)
        {
            rect.x += left;
            rect.y += up;
            rect.width -= left + right;
            rect.height -= up + down;
            return rect;
        }
    }
}