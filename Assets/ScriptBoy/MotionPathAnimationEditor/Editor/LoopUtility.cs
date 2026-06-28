using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    internal static class LoopUtility
    {
        public static int Mod(int index, int length)
        {
            return index - Mathf.FloorToInt((float)index / length) * length;
        }

        public static float Mod(float index, float length)
        {
            return index - Mathf.Floor(index / length) * length;
        }
    }
}