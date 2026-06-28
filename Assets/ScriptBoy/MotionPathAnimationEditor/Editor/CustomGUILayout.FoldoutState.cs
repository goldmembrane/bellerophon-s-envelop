using System.Collections.Generic;
using UnityEditor;

namespace ScriptBoy.MotionPathAnimEditor
{
    static partial class CustomGUILayout
    {
        private static class FoldoutState
        {
            private static Dictionary<string, bool> foldoutDictionary;

            static FoldoutState()
            {
                foldoutDictionary = new Dictionary<string, bool>();
            }

            public static bool GetState(string name)
            {
                bool foldout;

                if (!foldoutDictionary.TryGetValue(name, out foldout))
                {
                    foldout = EditorPrefs.GetBool(GetKey(name));
                    foldoutDictionary.Add(name, foldout);
                }

                return foldout;
            }

            public static bool SetState(string name, bool value)
            {
                foldoutDictionary[name] = value;
                EditorPrefs.SetBool(GetKey(name), value);
                return false;
            }

            private static string GetKey(string name)
            {
                return name + " FoldoutState";
            }
        }
    }
}