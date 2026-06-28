using UnityEngine;
using UnityEditor;

namespace ScriptBoy.MotionPathAnimEditor
{
    [System.Serializable]
    struct MinMaxGradient
    {
        public Color minColor;
        public Color maxColor;

        private static Texture2D s_VerticalMap;
        private static Texture2D s_HorizontalMap;

        public Texture staticVerticalMap
        {
            get
            {
                if (s_VerticalMap == null) s_VerticalMap = new Texture2D(100,1);
                for (int i = 0; i < 100; i++) s_VerticalMap.SetPixel(i, 0, Evaluate(i / 99f));
                s_VerticalMap.Apply();
                return s_VerticalMap;
            }
        }


        public Texture staticHorizontalMap
        {
            get
            {
                if (s_VerticalMap == null) s_HorizontalMap = new Texture2D(1, 100);
                for (int i = 0; i < 100; i++) s_HorizontalMap.SetPixel(0, i, Evaluate(i / 99f));
                s_HorizontalMap.Apply();
                return s_VerticalMap;
            }
        }


        public MinMaxGradient(Color min, Color max)
        {
            minColor = min;
            maxColor = max;
        }

        public MinMaxGradient(string mixHex, string maxHex)
        {
            ColorUtility.TryParseHtmlString(mixHex, out minColor);
            ColorUtility.TryParseHtmlString(maxHex, out maxColor);
        }

        public Color Evaluate(float time)
        {
            return Color.Lerp(minColor, maxColor, time);
        }
    }
}