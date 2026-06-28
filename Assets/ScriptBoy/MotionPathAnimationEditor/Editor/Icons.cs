using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{
    //[CreateAssetMenu(fileName = "Icons", menuName = "MotionPathAnimEditor/Icons")]
    public class Icons : ScriptableObject
    {
        private static Icons s_Instance;
        public static Icons Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var path = AssetDatabase.GUIDToAssetPath("9a22c71edef346749a6f9288b9499d63");
                    s_Instance = AssetDatabase.LoadAssetAtPath<Icons>(path);
                }

                return s_Instance;
            }
        }

        public Texture2D editModeOff;
        public Texture2D editModeOn;

        public Texture2D settingsOff;
        public Texture2D settingsOn;



        public Texture2D visibilityOff;
        public Texture2D visibilityOn;

        public Texture2D editOff;
        public Texture2D editOn;

        public Texture2D loopOff;
        public Texture2D loopOn;
    }
}
