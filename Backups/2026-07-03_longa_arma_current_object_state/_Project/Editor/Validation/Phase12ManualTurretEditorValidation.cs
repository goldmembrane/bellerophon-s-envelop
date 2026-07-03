using System;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase12ManualTurretEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase12ManualTurretBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 12 manual turret validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase12ManualTurretBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase12ManualTurretBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase12ManualTurretBootstrap.Phase12RootName);
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var manualTurretView = UnityEngine.Object.FindFirstObjectByType<ManualTurretView>();
            var manualFlightView = UnityEngine.Object.FindFirstObjectByType<ManualFlightView>();
            if (root == null ||
                deviceState == null ||
                manualTurretView == null ||
                manualFlightView == null)
            {
                throw new InvalidOperationException("Phase 12 requires root, device state, manual turret view, and manual flight view.");
            }

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null ||
                !canvas.overrideSorting ||
                canvas.sortingOrder < 30 ||
                canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                throw new InvalidOperationException("Phase 12 manual turret root must render as a high-priority screen overlay.");
            }

            if (manualTurretView.ViewRoot == null ||
                manualTurretView.ViewRoot.name != Phase12ManualTurretBootstrap.ManualTurretRootName ||
                manualTurretView.ReticleMarker == null ||
                manualTurretView.TargetMarker == null ||
                manualTurretView.StatusText == null)
            {
                throw new InvalidOperationException("Phase 12 manual turret view is missing root, reticle, target, or status references.");
            }

            if (GameObject.Find(Phase12ManualTurretBootstrap.ManualTurretBackdropName) != null ||
                manualTurretView.ViewRoot.transform.Find(Phase12ManualTurretBootstrap.ManualTurretBackdropName) != null)
            {
                throw new InvalidOperationException("Phase 12 manual turret view must not include the legacy center backdrop panel.");
            }

            var turretRootTransform = manualTurretView.ViewRoot.GetComponent<RectTransform>();
            if (turretRootTransform == null ||
                turretRootTransform.anchorMin != Vector2.zero ||
                turretRootTransform.anchorMax != Vector2.one ||
                turretRootTransform.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 12 manual turret view must be a full-screen transition view, not a modal panel.");
            }

            var background = manualTurretView.ViewRoot.GetComponent<Image>();
            if (background == null || background.color.a < 1f || !background.raycastTarget)
            {
                throw new InvalidOperationException("Phase 12 manual turret view background must be fully opaque.");
            }

            if (manualFlightView.ViewRoot == manualTurretView.ViewRoot)
            {
                throw new InvalidOperationException("Phase 12 manual turret view must be separate from the manual flight view.");
            }

            Debug.Log("Phase 12 manual turret editor validation passed.");
        }
    }
}
