using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace ScriptBoy.MotionPathAnimEditor
{
    static class Settings
    {
        class EditorPrefs<T>
        {
            public static T GetValue(string key)
            {
                var t = typeof(T);

                if (t == typeof(bool))
                {
                    return (T)(object)EditorPrefs.GetBool(key);
                }
                else if (t == typeof(float))
                {
                    return (T)(object)EditorPrefs.GetFloat(key);
                }
                else if (t == typeof(int))
                {
                    return (T)(object)EditorPrefs.GetInt(key);
                }

                return JsonUtility.FromJson<T>(EditorPrefs.GetString(key));
            }

            public static void SetValue(string key, T value)
            {
                var t = typeof(T);

                if (value is bool)
                {
                    EditorPrefs.SetBool(key, (bool)(object)value);
                }
                else if (value is float)
                {
                    EditorPrefs.SetFloat(key, (float)(object)value);
                }
                else if (value is int)
                {
                    EditorPrefs.SetInt(key, (int)(object)value);
                }
                else
                {
                    EditorPrefs.SetString(key, JsonUtility.ToJson(value));
                }
            }
        }

        abstract class BaseUserSetting
        {
            static List<BaseUserSetting> s_Instances { get; } = new List<BaseUserSetting>();

            public BaseUserSetting()
            {
                s_Instances.Add(this);
            }

            public abstract void Reset();

            public static void ResetAll()
            {
                foreach (var setting in s_Instances)
                {
                    setting.Reset();
                }
            }
        }

        class UserSetting<T> : BaseUserSetting
        {

            public UserSetting(string key, T defaultValue) : base()
            {
                m_Key = key;
                m_Value = m_Default = defaultValue;
                Load();
            }

            private string m_Key;
            private T m_Value;
            private T m_Default;

            public T Value
            {
                get => m_Value;
                set
                {
                    if (!Equals(m_Value, value))
                    {
                        m_Value = value;
                        Save();
                    }
                }
            }

            public override void Reset()
            {
                m_Value = m_Default;
                Save();
            }

            void Load()
            {
                if (EditorPrefs.HasKey(m_Key))
                {
                    m_Value = EditorPrefs<T>.GetValue(m_Key);
                }
            }

            void Save()
            {
                EditorPrefs<T>.SetValue(m_Key, m_Value);
            }
        }

        class GUIContents
        {
            public readonly static GUIContent handleSize = new GUIContent("Size", "Set the size of the editor handles.");
            public readonly static GUIContent handleColorNormal = new GUIContent("Normal Handle Color");
            public readonly static GUIContent handleColorSelected = new GUIContent("Selected Handle Color");
            public readonly static GUIContent handleCapControl = new GUIContent("Control Handle Cap");
            public readonly static GUIContent handleCapTangent = new GUIContent("Tangent Handle Cap");

            public readonly static GUIContent pathSpace = new GUIContent("Space", "Local: The path shows the position of the object through local space.\n\nWorld: The path shows the position of the object through world space.");
            public readonly static GUIContent pathColorMode = new GUIContent("ColorMode", "Color: Draw paths with a single color.\n\nGradient: Draw paths with a gradient.\n(The path color changes based on the object’s velocity.)");
            public readonly static GUIContent pathAccuracy = new GUIContent("Accuracy", "Set the number of path segments between 2 keyframes.");
            public readonly static GUIContent pathColor = new GUIContent("Color");
            public readonly static GUIContent pathGradient = new GUIContent("Gradient");
            public readonly static GUIContent showPathFullName = new GUIContent("Show Full Name", "Show the motion path full name in the MotionPath list.");
            public readonly static GUIContent showPathEditButton = new GUIContent("Show EditPath Button", "Show the EditPath button in the MotionPath list.\nIf you turn this off, all paths will be editable only based on the EditMode button.");
            public readonly static GUIContent showTimeTicks = new GUIContent("Show Time Ticks", "Show time ticks");
            public readonly static GUIContent showTimeLabels = new GUIContent("Show Time Labels", "Show the motion path full name in the MotionPath list.");
            public readonly static GUIContent timeTicksColor = new GUIContent("", "Show time ticks");
            public readonly static GUIContent timeLabelsColor = new GUIContent("", "Show the motion path full name in the MotionPath list.");

            static UserSetting<bool> s_ShowTimeTicks = new UserSetting<bool>(Keys.ShowTimeTicks, Defaults.ShowTimeTicks);
            static UserSetting<bool> s_ShowTimeLabels = new UserSetting<bool>(Keys.ShowTimeLabels, Defaults.ShowTimeLabels);


            public readonly static GUIContent syncSelection = new GUIContent("Sync Selection", "Synchronize handle selection with keyframe selection.");

            public readonly static GUIContent useLocalSnappingIn2D = new GUIContent("Use Local Snapping In 2D", "By default you can only snap a handle to the world grid. You can enable this feature to snap a handle to the local grid related to the parent object.\n(It only works in 2D!)");
            public readonly static GUIContent useRootOffset = new GUIContent("Use Root Offset", "You can manually apply a custom offset to the path of the root object.");
            public readonly static GUIContent useHideHandles = new GUIContent("Use Hide Handles", "This feature allows you to hide controls or tangents.");
            public readonly static GUIContent useTimeRange = new GUIContent("Use Time Range", "This feature allows you to hide all handles and paths outside of a time range. It is useful when you are working on a long animation clip.");
            public readonly static GUIContent useMagnet = new GUIContent("Use Magnet", "This feature allows you to smoothly drag neighboring handles of selected handles.");


            public readonly static GUIContent handleSizeSettings = new GUIContent("Handle Size", handleSize.tooltip);
            public readonly static GUIContent pathSpaceSettings = new GUIContent("Path Space", pathSpace.tooltip);
            public readonly static GUIContent pathColorModeSettings = new GUIContent("Path Color Mode", pathColorMode.tooltip);

        }

        class Defaults
        {
            public const HandleCapShape HandleCapControl = HandleCapShape.Cube;
            public const HandleCapShape HandleCapTangent = HandleCapShape.Sphere;
            public static readonly Color HandleColorNormal = Color.white;
            public static readonly Color HandleColorSelected = Color.yellow;
            public const float HandleSize = 0.1f;

            public const bool SyncSelection = true;

            public const bool UseLocalSnappingIn2D = false;
            public const bool UseRootOffset = false;
            public const bool UseHideHandles = false;
            public const bool UseTimeRange = false;
            public const bool UseMagnet = false;

            public const ColorMode PathColorMode = ColorMode.Color;
            public const Space PathSpace = Space.Local;
            public static readonly Color PathColor;
            public static readonly MinMaxGradient PathGradient = new MinMaxGradient("#009EFF", "#FF0000");
            public const int PathAccuracy = 30;
            public const bool ShowPathFullName = false;
            public const bool ShowPathEditButton = true;
            public const bool ShowTimeTicks = false;
            public const bool ShowTimeLabels = false;

            public static readonly Color timeTicksColor = Color.green;
            public static readonly Color timeLabelsColor = Color.green;

            public static readonly Color WindowColor;

            static Defaults()
            {
                ColorUtility.TryParseHtmlString("#00C0FF", out PathColor);
                ColorUtility.TryParseHtmlString("#0079FF", out WindowColor);
            }
        }

        class Keys
        {
            public const string HandleSize = "MotionPathAnimEditor.Settings.HandleSize";
            public const string HandleColorNormal = "MotionPathAnimEditor.Settings.HandleColorNormal";
            public const string HandleColorSelected = "MotionPathAnimEditor.Settings.HandleColorSelected";
            public const string HandleCapControl = "MotionPathAnimEditor.Settings.HandleCapControl";
            public const string HandleCapTangent = "MotionPathAnimEditor.Settings.HandleCapTangent";

            public const string SyncSelection = "MotionPathAnimEditor.Settings.SyncSelection";

            public const string UseLocalSnappingIn2D = "MotionPathAnimEditor.Settings.UseLocalSnappingIn2D";
            public const string UseRootOffset = "MotionPathAnimEditor.Settings.UseRootOffset";
            public const string UseHideHandles = "MotionPathAnimEditor.Settings.UseHideHandles";
            public const string UseTimeRange = "MotionPathAnimEditor.Settings.UseTimeRange";
            public const string UseMagnet = "MotionPathAnimEditor.Settings.UseMagnet";

            public const string PathColorMode = "MotionPathAnimEditor.Settings.PathColorMode";
            public const string PathColor = "MotionPathAnimEditor.Settings.PathColor";
            public const string PathAccuracy = "MotionPathAnimEditor.Settings.PathAccuracy";
            public const string PathGradient = "MotionPathAnimEditor.Settings.PathGradient";
            public const string PathSpace = "MotionPathAnimEditor.Settings.PathSpace";
            public const string ShowPathFullName = "MotionPathAnimEditor.Settings.PathFullName";
            public const string ShowPathEditButton = "MotionPathAnimEditor.Settings.PathEditButton";
            public const string ShowTimeTicks = "MotionPathAnimEditor.Settings.ShowTimeTicks";
            public const string TimeTicksColor = "MotionPathAnimEditor.Settings.TimeTicksColor";
            public const string ShowTimeLabels = "MotionPathAnimEditor.Settings.ShowTimeLabels";
            public const string TimeLabelsColor = "MotionPathAnimEditor.Settings.TimeLabelsColor";

            public const string WindowColor = "MotionPathAnimEditor.Settings.WindowColor";
        }

        static UserSetting<float> s_HandleSize = new UserSetting<float>(Keys.HandleSize, Defaults.HandleSize);
        static UserSetting<HandleCapShape> s_HandleCapControl = new UserSetting<HandleCapShape>(Keys.HandleCapControl, Defaults.HandleCapControl);
        static UserSetting<HandleCapShape> s_HandleCapTangent = new UserSetting<HandleCapShape>(Keys.HandleCapTangent, Defaults.HandleCapTangent);
        static UserSetting<Color> s_HandleColorNormal = new UserSetting<Color>(Keys.HandleColorNormal, Defaults.HandleColorNormal);
        static UserSetting<Color> s_HandleColorSelected = new UserSetting<Color>(Keys.HandleColorSelected, Defaults.HandleColorSelected);

        static UserSetting<bool> s_SyncSelection = new UserSetting<bool>(Keys.SyncSelection, Defaults.SyncSelection);
        static UserSetting<bool> s_UseLocalSnappingIn2D = new UserSetting<bool>(Keys.UseLocalSnappingIn2D, Defaults.UseLocalSnappingIn2D);
        static UserSetting<bool> s_UseRootOffset = new UserSetting<bool>(Keys.UseRootOffset, Defaults.UseRootOffset);
        static UserSetting<bool> s_UseHideHandles = new UserSetting<bool>(Keys.UseHideHandles, Defaults.UseHideHandles);
        static UserSetting<bool> s_UseTimeRange = new UserSetting<bool>(Keys.UseTimeRange, Defaults.UseTimeRange);
        static UserSetting<bool> s_UseMagnet = new UserSetting<bool>(Keys.UseMagnet, Defaults.UseMagnet);

        static UserSetting<Space> s_PathSpace = new UserSetting<Space>(Keys.PathSpace, Defaults.PathSpace);
        static UserSetting<int> s_PathAccuracy = new UserSetting<int>(Keys.PathAccuracy, Defaults.PathAccuracy);
        static UserSetting<ColorMode> s_PathColorMode = new UserSetting<ColorMode>(Keys.PathColorMode, Defaults.PathColorMode);
        static UserSetting<Color> s_PathColor = new UserSetting<Color>(Keys.PathColor, Defaults.PathColor);
        static UserSetting<MinMaxGradient> s_PathGradient = new UserSetting<MinMaxGradient>(Keys.PathGradient, Defaults.PathGradient);
        static UserSetting<bool> s_ShowPathFullName = new UserSetting<bool>(Keys.ShowPathFullName, Defaults.ShowPathFullName);
        static UserSetting<bool> s_ShowPathEditButton = new UserSetting<bool>(Keys.ShowPathEditButton, Defaults.ShowPathEditButton);
        static UserSetting<bool> s_ShowTimeTicks = new UserSetting<bool>(Keys.ShowTimeTicks, Defaults.ShowTimeTicks);
        static UserSetting<bool> s_ShowTimeLabels = new UserSetting<bool>(Keys.ShowTimeLabels, Defaults.ShowTimeLabels);
        static UserSetting<Color> s_TimeTicksColor = new UserSetting<Color>(Keys.TimeTicksColor, Defaults.timeTicksColor);
        static UserSetting<Color> s_TimeLabelsColor = new UserSetting<Color>(Keys.TimeLabelsColor, Defaults.timeLabelsColor);


        static UserSetting<Color> s_WindowColor = new UserSetting<Color>(Keys.WindowColor, Defaults.WindowColor);



        public static float handleSize { get => s_HandleSize.Value; private set => s_HandleSize.Value = value; }
        public static HandleCapShape handleCapControl { get => s_HandleCapControl.Value; private set => s_HandleCapControl.Value = value; }
        public static HandleCapShape handleCapTangent { get => s_HandleCapTangent.Value; private set => s_HandleCapTangent.Value = value; }
        public static Color handleColorNormal { get => s_HandleColorNormal.Value; private set => s_HandleColorNormal.Value = value; }
        public static Color handleColorSelected { get => s_HandleColorSelected.Value; private set => s_HandleColorSelected.Value = value; }

        public static bool syncSelection { get => s_SyncSelection.Value; private set => s_SyncSelection.Value = value; }

        public static bool useLocalSnappingIn2D { get => s_UseLocalSnappingIn2D.Value; private set => s_UseLocalSnappingIn2D.Value = value; }
        public static bool useRootOffset { get => s_UseRootOffset.Value; private set => s_UseRootOffset.Value = value; }
        public static bool useHideHandles { get => s_UseHideHandles.Value; private set => s_UseHideHandles.Value = value; }
        public static bool useTimeRange { get => s_UseTimeRange.Value; private set => s_UseTimeRange.Value = value; }
        public static bool useMagnet { get => s_UseMagnet.Value; private set => s_UseMagnet.Value = value; }

        public static Space pathSpace { get => s_PathSpace.Value; private set => s_PathSpace.Value = value; }
        public static int pathAccuracy { get => s_PathAccuracy.Value; private set => s_PathAccuracy.Value = value; }
        public static ColorMode pathColorMode { get => s_PathColorMode.Value; private set => s_PathColorMode.Value = value; }
        public static Color pathColor { get => s_PathColor.Value; private set => s_PathColor.Value = value; }
        public static MinMaxGradient pathGradient { get => s_PathGradient.Value; private set => s_PathGradient.Value = value; }
        public static bool showPathFullName { get => s_ShowPathFullName.Value; private set => s_ShowPathFullName.Value = value; }
        public static bool showPathEditButton { get => s_ShowPathEditButton.Value; private set => s_ShowPathEditButton.Value = value; }

        public static bool showTimeTicks { get => s_ShowTimeTicks.Value; private set => s_ShowTimeTicks.Value = value; }
        public static bool showTimeLabels { get => s_ShowTimeLabels.Value; private set => s_ShowTimeLabels.Value = value; }

        public static Color timeTicksColor { get => s_TimeTicksColor.Value; private set => s_TimeTicksColor.Value = value; }
        public static Color timeLabelsColor { get => s_TimeLabelsColor.Value; private set => s_TimeLabelsColor.Value = value; }

        public static Color windowColor { get => s_WindowColor.Value; private set => s_WindowColor.Value = value; }



        [SettingsProvider]
        static SettingsProvider CreateSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Preferences/Motion Path Animation Editor", SettingsScope.User);
            provider.guiHandler += (e) => OnPreferencesGUI();
            return provider;
        }

        static void OnPreferencesGUI()
        {
            EditorGUI.BeginChangeCheck();

            using (new CustomGUILayout.GroupScope("Handle"))
            {
                handleSize = EditorGUILayout.Slider(GUIContents.handleSize, handleSize, 0.075f, 0.4f);
                handleColorNormal = EditorGUILayout.ColorField(GUIContents.handleColorNormal, handleColorNormal);
                handleColorSelected = EditorGUILayout.ColorField(GUIContents.handleColorSelected, handleColorSelected);
                handleCapControl = (HandleCapShape)EditorGUILayout.EnumPopup(GUIContents.handleCapControl, handleCapControl);
                handleCapTangent = (HandleCapShape)EditorGUILayout.EnumPopup(GUIContents.handleCapTangent, handleCapTangent);
                syncSelection = EditorGUILayout.ToggleLeft(GUIContents.syncSelection, syncSelection);
                useLocalSnappingIn2D = EditorGUILayout.ToggleLeft(GUIContents.useLocalSnappingIn2D, useLocalSnappingIn2D);
                useHideHandles = EditorGUILayout.ToggleLeft(GUIContents.useHideHandles, useHideHandles);
                useMagnet = EditorGUILayout.ToggleLeft(GUIContents.useMagnet, useMagnet);
                useTimeRange = EditorGUILayout.ToggleLeft(GUIContents.useTimeRange, useTimeRange);
            }

            using (new CustomGUILayout.GroupScope("Path"))
            {
                pathSpace = (Space)EditorGUILayout.EnumPopup(GUIContents.pathSpace, pathSpace);
                if (pathSpace == Space.Local)
                {
                    pathColorMode = (ColorMode)EditorGUILayout.EnumPopup(GUIContents.pathColorMode, pathColorMode);
                    if (pathColorMode == ColorMode.Color)
                    {
                        pathColor = EditorGUILayout.ColorField(GUIContents.pathColor, pathColor);
                    }
                    else
                    {
                        pathGradient = CustomGUILayout.MinMaxGradientField(GUIContents.pathGradient.text, pathGradient);
                        pathAccuracy = EditorGUILayout.IntSlider(GUIContents.pathAccuracy, pathAccuracy, 0, 100);
                    }
                }
                else pathColor = EditorGUILayout.ColorField(GUIContents.pathColor, pathColor);

                showPathFullName = EditorGUILayout.ToggleLeft(GUIContents.showPathFullName, showPathFullName);
                showPathEditButton = EditorGUILayout.ToggleLeft(GUIContents.showPathEditButton, showPathEditButton);

                EditorGUILayout.BeginHorizontal();
                showTimeTicks = EditorGUILayout.ToggleLeft(GUIContents.showTimeTicks, showTimeTicks);
                if (showTimeTicks)
                {
                    timeTicksColor = EditorGUILayout.ColorField(GUIContents.timeTicksColor, timeTicksColor);
                }
                EditorGUILayout.EndHorizontal();


                EditorGUILayout.BeginHorizontal();
                showTimeLabels = EditorGUILayout.ToggleLeft(GUIContents.showTimeLabels, showTimeLabels);
                if (showTimeLabels)
                {
                    timeLabelsColor = EditorGUILayout.ColorField(GUIContents.timeLabelsColor, timeLabelsColor);
                }
                EditorGUILayout.EndHorizontal();


                useRootOffset = EditorGUILayout.ToggleLeft(GUIContents.useRootOffset, useRootOffset);
            }

            using (new CustomGUILayout.GroupScope("Window"))
            {
                windowColor = EditorGUILayout.ColorField("Color", windowColor);
            }
            if (EditorGUI.EndChangeCheck())
                Refresh();

            if (GUILayout.Button("Reset to default settings"))
                Reset();
        }

        public static void DrawFoldoutWindow()
        {
            using (new CustomGUILayout.FoldoutWindowScope("Settings", out bool open))
            {
                if (open)
                {
                    handleSize = EditorGUILayout.Slider(GUIContents.handleSizeSettings, handleSize, 0.075f, 0.4f);
                    pathSpace = (Space)EditorGUILayout.EnumPopup(GUIContents.pathSpaceSettings, pathSpace);
                    if (pathSpace == Space.Local)
                    {
                        pathColorMode = (ColorMode)EditorGUILayout.EnumPopup(GUIContents.pathColorModeSettings, pathColorMode);
                    }

                    if (GUILayout.Button("Open Preferences"))
                    {
                        SettingsService.OpenUserPreferences("Preferences/Motion Path Animation Editor");
                    }
                }
            }
        }

        static void Refresh()
        {
            Textures.RefreshColor(windowColor);
            SceneView.RepaintAll();
            AnimEditorWindow.RepaintWindow();
        }

        static void Reset()
        {
            BaseUserSetting.ResetAll();
            Refresh();
        }
    }
}