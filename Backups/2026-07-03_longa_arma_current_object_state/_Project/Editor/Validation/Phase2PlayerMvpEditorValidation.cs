using System;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase2PlayerMvpEditorValidation
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";

        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException($"Missing phase 2 scene: {CargoRunScenePath}");
            }

            if (EditorSceneManager.playModeStartScene != sceneAsset)
            {
                throw new InvalidOperationException($"Editor Play Mode Start Scene must be {CargoRunScenePath}.");
            }

            if (SceneManager.GetActiveScene().path != CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            var playerMotor = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            if (playerMotor == null || playerInput == null || hud == null)
            {
                throw new InvalidOperationException("CargoRunMvp must have Player motor, input, and HUD in the open hierarchy.");
            }

            var camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("CargoRunMvp must have a player camera tagged MainCamera.");
            }

            if (!camera.isActiveAndEnabled)
            {
                throw new InvalidOperationException("Player camera must be active and enabled.");
            }

            var visibleRendererCount = CountVisibleRenderers(camera);
            if (visibleRendererCount < 2)
            {
                throw new InvalidOperationException(
                    $"Player camera frustum must contain visible scene renderers. Found: {visibleRendererCount}");
            }

            var canvas = hud.GetComponent<Canvas>();
            if (canvas == null || !canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                throw new InvalidOperationException("HUD must have an active ScreenSpaceOverlay Canvas.");
            }

            if (hud.GetComponentsInChildren<Text>(true).Length < 4)
            {
                throw new InvalidOperationException("HUD must include health, shield, crosshair, and interaction prompt labels.");
            }

            Debug.Log(
                $"Phase 2 editor visual validation passed. ActiveScene={SceneManager.GetActiveScene().path}, VisibleRenderers={visibleRendererCount}");
        }

        private static int CountVisibleRenderers(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var visibleRendererCount = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                {
                    visibleRendererCount++;
                }
            }

            return visibleRendererCount;
        }
    }
}
