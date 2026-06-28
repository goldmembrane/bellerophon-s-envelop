using UnityEditor;
using UnityEngine;


namespace ScriptBoy.MotionPathAnimEditor
{
    partial class AnimEditorWindow
    {
        static class GUIStyles
        {
            static GUIStyles()
            {
                toggleEditMode = new GUIStyle();
                toggleEditMode.fixedHeight = toggleEditMode.fixedWidth = 40;
                toggleEditMode.normal.background = Icons.Instance.editModeOff;
                toggleEditMode.onNormal.background = Icons.Instance.editModeOn;

                toggleSettings = new GUIStyle();
                toggleSettings.fixedHeight = toggleSettings.fixedWidth = 40;
                toggleSettings.normal.background = Icons.Instance.settingsOff;
                toggleSettings.onNormal.background = Icons.Instance.settingsOn;

                RectOffset padding = new RectOffset(5, 5, 5, 5);

                toggleVisibility = new GUIStyle();
                toggleVisibility.padding = padding;
                toggleVisibility.normal.background = Icons.Instance.visibilityOff;
                toggleVisibility.onNormal.background = Icons.Instance.visibilityOn;

                toggleEdit = new GUIStyle();
                toggleEdit.padding = padding;
                toggleEdit.normal.background = Icons.Instance.editOff;
                toggleEdit.onNormal.background = Icons.Instance.editOn;

                toggleLoop = new GUIStyle();
                toggleLoop.padding = padding;
                toggleLoop.normal.background = Icons.Instance.loopOff;
                toggleLoop.onNormal.background = Icons.Instance.loopOn;

                pathListItemRow = new GUIStyle(GUI.skin.box);
                pathListItemRow.padding = new RectOffset(0, 0, 0, 0);
                pathListItemRow.margin = new RectOffset(0, 0, 0, 2);
                pathListItemRow.contentOffset = new Vector2(0, 0);

                windowWithoutBar = new GUIStyle(GUI.skin.window);
                windowWithoutBar.padding = new RectOffset(5, 5, 5, 5);
                windowWithoutBar.margin = new RectOffset(2, 2, 5, 5);
                windowWithoutBar.stretchHeight = false;

                windowBar = new GUIStyle(GUI.skin.box);
                windowBar.padding = new RectOffset(5, 5, 5, 5);


                margin_15_10 = new GUIStyle();
                margin_15_10.margin = new RectOffset(15, 15, 10, 10);
                margin_15_10.stretchHeight = false;

                header = new GUIStyle();
                header.padding = new RectOffset(5, 5, 5, 5);
                header.normal.background = Textures.windowHeader;



                body = new GUIStyle();
                body.margin = new RectOffset(10, 10,10,10);
               // body.padding = new RectOffset(10, 10, 10, 10);
            }

            public static Texture itemRowNormal;
            public static Texture itemRowActive;

            public static GUIStyle header;

            public static GUIStyle margin_15_10;

            public static GUIStyle windowWithoutBar;
            public static GUIStyle windowBar;

            public static GUIStyle pathListItemRow;

            public static GUIStyle toggleEditMode;
            public static GUIStyle toggleSettings;

            public static GUIStyle toggleVisibility;
            public static GUIStyle toggleEdit;
            public static GUIStyle toggleLoop;
            public static GUIStyle body;
        }
    }
}