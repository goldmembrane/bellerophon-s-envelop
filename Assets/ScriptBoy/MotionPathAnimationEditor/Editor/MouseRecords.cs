using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class MouseRecords
    {
        public static bool LeftClick { get; private set; }
        public static bool RightClick { get; private set; }
        public static Vector3 Position { get; private set; }

        public static void Record()
        {
            Event e = Event.current;
            LeftClick = e.isMouse && e.type == EventType.MouseDown && e.button == 0;
            RightClick = e.isMouse && e.type == EventType.MouseDown && e.button == 1;
            Position = e.mousePosition;
        }
    }
}