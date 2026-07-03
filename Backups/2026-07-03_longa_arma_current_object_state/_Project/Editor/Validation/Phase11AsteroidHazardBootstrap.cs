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
    public static class Phase11AsteroidHazardBootstrap
    {
        public const string CargoRunScenePath = Phase10PlanetMaintenanceBootstrap.CargoRunScenePath;
        public const string Phase11RootName = "Phase 11 Asteroid Hazard";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 11 Asteroid Hazard")]
        public static void EnsurePhase11Assets()
        {
            Phase10PlanetMaintenanceBootstrap.EnsurePhase10Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase11RootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var manualView = UnityEngine.Object.FindFirstObjectByType<ManualFlightView>();
            var startController = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            if (hud == null ||
                playerInput == null ||
                deviceState == null ||
                deviceHud == null ||
                manualView == null ||
                startController == null ||
                settlementController == null ||
                maintenanceController == null)
            {
                throw new InvalidOperationException("Phase 11 requires Phase 10 transport, settlement, maintenance, HUD, and manual flight assets.");
            }

            CreateRoot(hud.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase11AsteroidHazardEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 11 asteroid hazard assets are ready.");
        }

        private static void CreateRoot(Transform parent)
        {
            var root = new GameObject(Phase11RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                    return;
                }

                var child = roots[i].transform.Find(objectName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    return;
                }
            }
        }
    }
}
