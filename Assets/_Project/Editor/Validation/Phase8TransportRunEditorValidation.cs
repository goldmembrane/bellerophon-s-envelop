using System;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase8TransportRunEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase8TransportRunBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 8 transport run validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase8TransportRunBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase8TransportRunBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var state = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var manualView = UnityEngine.Object.FindFirstObjectByType<ManualFlightView>();
            if (state == null || hud == null || playerInput == null || deviceHud == null || manualView == null)
            {
                throw new InvalidOperationException("Phase 8 requires device state, HUD, player input, device HUD, and manual flight view.");
            }

            if (deviceHud.TransportStatusText == null ||
                deviceHud.TransportStatusText.name != Phase8TransportRunBootstrap.TransportStatusTextName)
            {
                throw new InvalidOperationException("Phase 8 transport status text is not configured on ShipDeviceHud.");
            }

            if (manualView.ViewRoot == null ||
                manualView.ViewRoot.name != Phase8TransportRunBootstrap.ManualFlightRootName ||
                manualView.PlayerMarker == null ||
                manualView.StatusText == null)
            {
                throw new InvalidOperationException("Phase 8 manual flight view is missing root, marker, or status text.");
            }

            var manualRootTransform = manualView.ViewRoot.GetComponent<RectTransform>();
            if (manualRootTransform == null ||
                manualRootTransform.anchorMin != Vector2.zero ||
                manualRootTransform.anchorMax != Vector2.one ||
                manualRootTransform.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 8 manual flight view must be a full-screen transition view, not a modal panel.");
            }

            var background = manualView.ViewRoot.GetComponent<UnityEngine.UI.Image>();
            if (background == null || background.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 8 manual flight view background must be fully opaque.");
            }

            var asteroidField = manualView.ViewRoot.transform.Find(Phase8TransportRunBootstrap.AsteroidFieldName);
            if (asteroidField == null || asteroidField.childCount < 8)
            {
                throw new InvalidOperationException("Phase 8 manual flight view must include an asteroid field.");
            }

            if (UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>() == null)
            {
                throw new InvalidOperationException("Phase 8 transport run still requires the Phase 7 new game start flow.");
            }

            Debug.Log("Phase 8 transport run editor validation passed.");
        }
    }
}
