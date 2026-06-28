using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    struct ColorHSV
    {
        public float h;
        public float s;
        public float v;

        public ColorHSV(Color color)
        {
            Color.RGBToHSV(color, out h, out s, out v);
        }

        public Color ToRGB => Color.HSVToRGB(h,s,v);
    }
}
