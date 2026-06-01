using System.IO;
using System.Linq;
using Bellerophon.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ProjectBootstrap
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        public static void EnsureInitialProjectState()
        {
            EnsureBootstrapScene();
            EnsureSceneInBuildSettings(BootstrapScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Initial project state is ready.");
        }

        private static void EnsureBootstrapScene()
        {
            if (File.Exists(BootstrapScenePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(BootstrapScenePath));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var markerObject = new GameObject("Harness Smoke Marker");
            markerObject.AddComponent<HarnessSmokeMarker>();

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var existingScenes = EditorBuildSettings.scenes.ToList();
            var existingScene = existingScenes.FirstOrDefault(scene => scene.path == scenePath);

            if (existingScene != null)
            {
                existingScene.enabled = true;
                EditorBuildSettings.scenes = existingScenes.ToArray();
                return;
            }

            existingScenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = existingScenes.ToArray();
        }
    }
}
