using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    // Scene-view navigation only. Does not move scene objects or save the scene.
    internal static class DaggerSceneViewFocusTools
    {
        private const string Output = "docs/validation/dagger_scene_view_focus_2026-09-07";

        internal static void Focus()
        {
            var scene = SceneManager.GetActiveScene();
            var matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == "Dagger_Idle").ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected one Dagger_Idle; found " + matches.Length);
            var target = matches[0];
            var renderers = target.GetComponentsInChildren<Renderer>()
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Dagger_Idle has no visible renderers.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var view = SceneView.lastActiveSceneView;
            if (view == null) throw new InvalidOperationException("No open Scene view.");
            bool dirtyBefore = scene.isDirty;
            Selection.activeGameObject = target.gameObject;
            view.pivot = bounds.center;
            view.rotation = Quaternion.LookRotation(-target.forward, target.up);
            view.size = Mathf.Max(1f, bounds.extents.magnitude * 1.15f);
            view.orthographic = false;
            view.Focus();
            view.Repaint();
            Debug.Log("Dagger_Idle Scene view focused. pivot=" + view.pivot + " size=" + view.size +
                " targetPosition=" + target.position + " sceneDirtyBefore=" + dirtyBefore + " sceneDirtyAfter=" + scene.isDirty);
        }

        internal static void Capture()
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || Selection.activeGameObject == null || Selection.activeGameObject.name != "Dagger_Idle")
                throw new InvalidOperationException("Focus Dagger_Idle first.");
            Directory.CreateDirectory(Output);
            string path = Output + "/final.png";
            if (File.Exists(path)) throw new InvalidOperationException("Final capture already exists; not overwritten.");
            // Read the Scene view's current camera configuration, without changing that view.
            var temporary = new GameObject("DaggerSceneViewReadback", typeof(Camera));
            temporary.hideFlags = HideFlags.HideAndDontSave;
            var camera = temporary.GetComponent<Camera>();
            camera.CopyFrom(view.camera);
            camera.transform.SetPositionAndRotation(view.camera.transform.position, view.camera.transform.rotation);
            camera.enabled = false;
            int width = 1200, height = Mathf.Max(1, Mathf.RoundToInt(width / view.camera.aspect));
            var render = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                Debug.Log("Dagger_Idle current Scene-view camera render saved: " + path);
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }
    }
}
