using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.MotionPathAnimEditor
{

    [System.Obsolete("MotionPathUtility is deprecated in version 2.7.0. Motion path lists are no longer temporarily stored per root; they are now permanently stored per animation clip. You no longer need to add a motion path every time. You only need to add a motion path once per animation clip.", true)]
    public static class MotionPathUtility
    {
        /// <summary>
        /// Creates motion paths for the specified transforms under the given root transform.
        /// </summary>
        /// <param name="root">The transform of the game object that the Animator component is attached to.</param>
        /// <param name="transforms">The list of transforms for which to create motion paths.</param>
        public static void Create(Transform root, List<Transform> transforms)
        {
            if (AnimEditor.instance == null) return;

            var motionPaths = AnimEditor.instance.GetMotionPathList(root);

            foreach (var transform in transforms)
            {
                if (!motionPaths.Exists(m => m.transform == transform))
                {
                    motionPaths.Add(new MotionPath(transform));
                }
            }
        }

        /// <summary>
        /// Removes motion paths associated with the specified transforms under the given root transform.
        /// </summary>
        /// <param name="root">The transform of the game object that the Animator component is attached to.</param>
        /// <param name="transforms">The list of transforms from which to remove their motion paths.</param>
        public static void Remove(Transform root, List<Transform> transforms)
        {
            if (AnimEditor.instance == null) return;

            var motionPaths = AnimEditor.instance.GetMotionPathList(root);

            motionPaths.RemoveAll(m => transforms.Contains(m.transform));
        }

        /// <summary>
        /// Remove all motion paths under the given root transform.
        /// </summary>
        /// <param name="root">The transform of the game object that the Animator component is attached to.</param>
        public static void Clear(Transform root)
        {
            if (AnimEditor.instance == null) return;

            var motionPaths = AnimEditor.instance.GetMotionPathList(root);

            motionPaths.Clear();
        }

        /// <summary>
        /// Retrieves the transforms of motion paths under the given root transform.
        /// </summary>
        /// <param name="root">The transform of the game object that the Animator component is attached to.</param>
        /// <param name="transforms">The list to populate with the retrieved motion path transforms.</param>
        public static void Get(Transform root, List<Transform> transforms)
        {
            transforms.Clear();

            if (AnimEditor.instance == null) return;

            var motionPaths = AnimEditor.instance.GetMotionPathList(root);

            foreach (var m in motionPaths) transforms.Add(m.transform);
        }
    }
}