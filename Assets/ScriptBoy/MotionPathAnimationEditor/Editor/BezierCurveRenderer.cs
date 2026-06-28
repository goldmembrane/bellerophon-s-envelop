using UnityEditor;
using UnityEngine;


namespace ScriptBoy.MotionPathAnimEditor
{
    /// <summary>
    /// Drawing Bezier Curve using GL API.
    /// </summary>
    static class BezierCurveRenderer
    {
        private static Matrix4x4 s_World2Screen;

        private static float s_ScreenWidth;
        private static float s_ScreenHeight;

        private static Material s_Material;

        private static Material material
        {
            get
            {
                if (!s_Material)
                {
                    s_Material = new Material(Shader.Find("Hidden/Internal-Colored"));
                    s_Material.hideFlags = HideFlags.HideAndDontSave;
                    s_Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    s_Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    s_Material.SetInt("_Cull", 0);
                    s_Material.SetInt("_ZWrite", 0);
                    s_Material.SetInt("_ZTest", 0);
                }
                return s_Material;
            }
        }

        public static void SetMaterial(Material material)
        {
            s_Material = material;
        }

        public static void Begin()
        {
            Camera camera = SceneView.currentDrawingSceneView.camera;
            Rect pixelRect = camera.pixelRect;

            s_ScreenWidth = pixelRect.width;
            s_ScreenHeight = pixelRect.height;

            Matrix4x4 world2Viewport = camera.projectionMatrix * camera.worldToCameraMatrix;
            Matrix4x4 viewport2Screen = Matrix4x4.Scale(new Vector3(pixelRect.width, pixelRect.height, 1)) * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) * Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 1));
            Matrix4x4 world2Screen = viewport2Screen * world2Viewport;

            s_World2Screen = world2Screen;


            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Viewport(pixelRect);
            GL.Begin(GL.QUADS);
        }

        public static void End()
        {
            GL.End();
            GL.PopMatrix();
        }




        /// <summary>
        /// Draw a bezier curve!
        /// </summary>
        public static void Draw(Vector3 start, Vector3 end, Vector3 startTangent, Vector3 endTangent, float deltaTime, float minVelocity, float maxVelocity, int segmentCount)
        {
            MinMaxGradient gradient = Settings.pathGradient;

            Vector3 aWorld = start;
            Vector3 a = WorldToScreen(start);

            for (int j = 0; j <= segmentCount; j++)
            {
                float t = (float)j / segmentCount;

                Vector3 bWorld = EvaluateBezierCurve(start, end, startTangent, endTangent, t);
                Vector3 b = WorldToScreen(bWorld);

                float velocity = (bWorld - aWorld).magnitude / deltaTime;
                float colorTime = Mathf.InverseLerp(maxVelocity, minVelocity, velocity);
                GL.Color(gradient.Evaluate(colorTime));
                //GL.Color(Color.Lerp(Color.yellow,Color.red, colorTime));

                float deltaX = b.x - a.x;
                float deltaY = b.y - a.y;

                float lineLen = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (lineLen > 5 || j == segmentCount)
                {
                    if (a.z + b.z >= 1)
                    {
                        DrawLine(a.x, a.y, b.x, b.y, deltaX, deltaY, lineLen, 3f);
                    }

                    a = b;
                }

                aWorld = bWorld;
            }
        }


        public static void Draw(Vector3 start, Vector3 end, Vector3 startTangent, Vector3 endTangent, float startWeight, float endWeight, int segmentCount)
        {
            MinMaxGradient gradient = new MinMaxGradient(Color.black, Color.green);

            Vector3 aWorld = start;
            Vector3 a = WorldToScreen(start);

            for (int j = 0; j <= segmentCount; j++)
            {
                float t = (float)j / segmentCount;

                Vector3 bWorld = EvaluateBezierCurve(start, end, startTangent, endTangent, t);
                Vector3 b = WorldToScreen(bWorld);


                float colorTime = Mathf.Lerp(startWeight, endWeight, t);
                GL.Color(gradient.Evaluate(colorTime));
                //GL.Color(Color.Lerp(Color.yellow,Color.red, colorTime));

                float deltaX = b.x - a.x;
                float deltaY = b.y - a.y;

                float lineLen = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (lineLen > 5 || j == segmentCount)
                {
                    if (a.z + b.z >= 1)
                    {
                        DrawLine(a.x, a.y, b.x, b.y, deltaX, deltaY, lineLen, 3f);
                    }

                    a = b;
                }

                aWorld = bWorld;
            }
        }



        private static void DrawLine(float ax, float ay, float bx, float by, float width)
        {
            float deltaX = bx - ax;
            float deltaY = by - ay;

            float length = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

            float hx = (deltaY / length) * width;
            float hy = -(deltaX / length) * width;

            GL.TexCoord2(0, 1);
            GL.Vertex3(ax - hx, ay - hy, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(bx - hx, by - hy, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(bx + hx, by + hy, 0);
            GL.TexCoord2(0, 0);
            GL.Vertex3(ax + hx, ay + hy, 0);
        }

        private static void DrawLine(float ax, float ay, float bx, float by, float deltaX, float deltaY, float length, float width)
        {
            float hx = (deltaY / length) * width;
            float hy = -(deltaX / length) * width;

            GL.TexCoord2(0, 1);
            GL.Vertex3(ax - hx, ay - hy, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(bx - hx, by - hy, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(bx + hx, by + hy, 0);
            GL.TexCoord2(0, 0);
            GL.Vertex3(ax + hx, ay + hy, 0);
        }

        /// <summary>
        /// <para> if (z == -1) : behind           </para>
        /// <para> if (z ==  0) : outside          </para>
        /// <para> if (z ==  1) : inside (visible) </para>
        /// </summary>
        private static Vector3 WorldToScreen(Vector3 worldPoint)
        {
            Vector3 screenPoint = s_World2Screen.MultiplyPoint(worldPoint);

            float z = screenPoint.z;
            if (Mathf.Abs(z) <= 1)
            {
                float y = screenPoint.y;
                float x = screenPoint.x;
                if (x < 0 || y < 0 || x > s_ScreenWidth || y > s_ScreenHeight)
                {
                    z = 0;
                }
                else
                {
                    z = 1;
                }
            }
            else
            {
                z = -1;
            }
            screenPoint.z = z;
            return screenPoint;
        }


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
    }


    static class TimelineRenderer
    {
        private static Matrix4x4 s_World2Screen;

        private static float s_ScreenWidth;
        private static float s_ScreenHeight;

        private static Material s_Material;

        private static Material material
        {
            get
            {
                if (!s_Material)
                {
                    s_Material = new Material(Shader.Find("Hidden/Internal-Colored"));
                    s_Material.hideFlags = HideFlags.HideAndDontSave;
                    s_Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    s_Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    s_Material.SetInt("_Cull", 0);
                    s_Material.SetInt("_ZWrite", 0);
                    s_Material.SetInt("_ZTest", 0);
                }
                return s_Material;
            }
        }

        public static void SetMaterial(Material material)
        {
            s_Material = material;
        }

        public static void Begin()
        {
            Camera camera = SceneView.currentDrawingSceneView.camera;
            Rect pixelRect = camera.pixelRect;

            s_ScreenWidth = pixelRect.width;
            s_ScreenHeight = pixelRect.height;

            Matrix4x4 world2Viewport = camera.projectionMatrix * camera.worldToCameraMatrix;
            Matrix4x4 viewport2Screen = Matrix4x4.Scale(new Vector3(pixelRect.width, pixelRect.height, 1)) * Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0)) * Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 1));
            Matrix4x4 world2Screen = viewport2Screen * world2Viewport;

            s_World2Screen = world2Screen;


            GL.PushMatrix();
            material.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Viewport(pixelRect);
            GL.Begin(GL.QUADS);
        }

        public static void End()
        {
            GL.End();
            GL.PopMatrix();
        }




        /// <summary>
        /// Draw a bezier curve!
        /// </summary>
        public static void Draw(Vector3 start, Vector3 end, Vector3 startTangent, Vector3 endTangent, float deltaTime, float minVelocity, float maxVelocity, int segmentCount)
        {
            MinMaxGradient gradient = Settings.pathGradient;

            Vector3 aWorld = start;
            Vector3 a = WorldToScreen(start);

            for (int j = 0; j <= segmentCount; j++)
            {
                float t = (float)j / segmentCount;

                Vector3 bWorld = EvaluateBezierCurve(start, end, startTangent, endTangent, t);
                Vector3 b = WorldToScreen(bWorld);

                float velocity = (bWorld - aWorld).magnitude / deltaTime;
                float colorTime = Mathf.InverseLerp(maxVelocity, minVelocity, velocity);
                GL.Color(gradient.Evaluate(colorTime));
                //GL.Color(Color.Lerp(Color.yellow,Color.red, colorTime));

                float deltaX = b.x - a.x;
                float deltaY = b.y - a.y;

                float lineLen = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (lineLen > 5 || j == segmentCount)
                {
                    if (a.z + b.z >= 1)
                    {
                        DrawLine(a.x, a.y, b.x, b.y, deltaX, deltaY, lineLen, 3f);
                    }

                    a = b;
                }

                aWorld = bWorld;
            }
        }


        public static void Draw(Vector3 start, Vector3 end, Vector3 startTangent, Vector3 endTangent, float startWeight, float endWeight, int segmentCount)
        {
            MinMaxGradient gradient = new MinMaxGradient(Color.black, Color.green);

            Vector3 aWorld = start;
            Vector3 a = WorldToScreen(start);

            for (int j = 0; j <= segmentCount; j++)
            {
                float t = (float)j / segmentCount;

                Vector3 bWorld = EvaluateBezierCurve(start, end, startTangent, endTangent, t);
                Vector3 b = WorldToScreen(bWorld);


                float colorTime = Mathf.Lerp(startWeight, endWeight, t);
                GL.Color(gradient.Evaluate(colorTime));
                //GL.Color(Color.Lerp(Color.yellow,Color.red, colorTime));

                float deltaX = b.x - a.x;
                float deltaY = b.y - a.y;

                float lineLen = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

                if (lineLen > 5 || j == segmentCount)
                {
                    if (a.z + b.z >= 1)
                    {
                        DrawLine(a.x, a.y, b.x, b.y, deltaX, deltaY, lineLen, 3f);
                    }

                    a = b;
                }

                aWorld = bWorld;
            }
        }



        private static void DrawLine(float ax, float ay, float bx, float by, float width)
        {
            float deltaX = bx - ax;
            float deltaY = by - ay;

            float length = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

            float hx = (deltaY / length) * width;
            float hy = -(deltaX / length) * width;

            GL.TexCoord2(0, 1);
            GL.Vertex3(ax - hx, ay - hy, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(bx - hx, by - hy, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(bx + hx, by + hy, 0);
            GL.TexCoord2(0, 0);
            GL.Vertex3(ax + hx, ay + hy, 0);
        }

        private static void DrawLine(float ax, float ay, float bx, float by, float deltaX, float deltaY, float length, float width)
        {
            float hx = (deltaY / length) * width;
            float hy = -(deltaX / length) * width;

            GL.TexCoord2(0, 1);
            GL.Vertex3(ax - hx, ay - hy, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(bx - hx, by - hy, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(bx + hx, by + hy, 0);
            GL.TexCoord2(0, 0);
            GL.Vertex3(ax + hx, ay + hy, 0);
        }

        /// <summary>
        /// <para> if (z == -1) : behind           </para>
        /// <para> if (z ==  0) : outside          </para>
        /// <para> if (z ==  1) : inside (visible) </para>
        /// </summary>
        private static Vector3 WorldToScreen(Vector3 worldPoint)
        {
            Vector3 screenPoint = s_World2Screen.MultiplyPoint(worldPoint);

            float z = screenPoint.z;
            if (Mathf.Abs(z) <= 1)
            {
                float y = screenPoint.y;
                float x = screenPoint.x;
                if (x < 0 || y < 0 || x > s_ScreenWidth || y > s_ScreenHeight)
                {
                    z = 0;
                }
                else
                {
                    z = 1;
                }
            }
            else
            {
                z = -1;
            }
            screenPoint.z = z;
            return screenPoint;
        }


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
    }
}