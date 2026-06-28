using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ScriptBoy.MotionPathAnimEditor
{
    class MotionPathClip : ScriptableObject
    {
        public List<MotionPath> motionPaths = new List<MotionPath>();

        [ContextMenu("Delete")]
        public void Delete()
        {
            AssetDatabase.RemoveObjectFromAsset(this);
        }
    }
}
