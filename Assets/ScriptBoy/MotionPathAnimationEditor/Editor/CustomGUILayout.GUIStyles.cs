using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    static partial class CustomGUILayout
    {
        static class GUIStyles
        {
            public readonly static GUIStyle foldoutWindow;
            public readonly static GUIStyle foldoutWindowBar;
            public readonly static GUIStyle foldoutWindowFoldout;
            public readonly static GUIStyle foldoutWindowOpen;
            public readonly static GUIStyle foldoutWindowClose;
            public readonly static GUIStyle buttonNoStretch;
            public readonly static GUIStyle toggleButton;

            static GUIStyles()
            {
                foldoutWindow = new GUIStyle(GUI.skin.window);
                foldoutWindow.padding = new RectOffset(0, 0, 1, 1);
                foldoutWindow.margin = new RectOffset(0, 0, 5, 5);
                foldoutWindow.stretchHeight = false;

                foldoutWindowBar = new GUIStyle(GUI.skin.box);
                foldoutWindowBar.normal.background = Textures.foldoutWindowTab;


                foldoutWindowFoldout = new GUIStyle(EditorStyles.foldout);
                foldoutWindowFoldout.margin = new RectOffset(5, 5, 0, 0);


                foldoutWindowOpen = new GUIStyle();
                foldoutWindowOpen.margin = new RectOffset(5, 5, 5, 5);


                foldoutWindowClose = new GUIStyle();

                buttonNoStretch = new GUIStyle(GUI.skin.button);
                buttonNoStretch.stretchWidth = false;

                toggleButton = new GUIStyle(GUI.skin.button);
                toggleButton.onNormal.textColor = toggleButton.onHover.textColor = Color.green;
            }
        }
    }
}