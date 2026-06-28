using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static class TextureUtility
    {
        public static Texture2D CreateOnePixelTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.hideFlags = HideFlags.DontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        public static Texture2D Clone(Texture2D src)
        {
            Texture2D dst = new Texture2D(src.width, src.height, src.format, false);
            dst.hideFlags = HideFlags.DontSave;
            Graphics.CopyTexture(src, dst);
            return dst;
        }

        public static void ChangeColor(Texture2D texture, Color color)
        {
            Color[] colors = texture.GetPixels();

            float alpha = color.a;
            for (int i = 0; i < colors.Length; i++)
            {
                color.a = colors[i].a * alpha;
                colors[i] = color;
            }

            texture.SetPixels(colors);
            texture.Apply();
        }
    }
}