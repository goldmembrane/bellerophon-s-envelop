using System;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase12ManualTurretBootstrap
    {
        public const string CargoRunScenePath = Phase11AsteroidHazardBootstrap.CargoRunScenePath;
        public const string Phase12RootName = "Phase 12 Manual Turret";
        public const string ManualTurretRootName = "Phase 12 Manual Turret View";
        public const string ManualTurretBackdropName = "Phase 12 Manual Turret Backdrop";
        public const string ManualTurretReticleName = "Phase 12 Manual Turret Reticle";
        public const string ManualTurretTargetName = "Phase 12 Manual Turret Target";
        public const string ManualTurretStatusTextName = "Phase 12 Manual Turret Status Text";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 12 Manual Turret")]
        public static void EnsurePhase12Assets()
        {
            Phase11AsteroidHazardBootstrap.EnsurePhase11Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase12RootName);
            DeleteGeneratedObject(ManualTurretBackdropName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var manualFlightView = UnityEngine.Object.FindFirstObjectByType<ManualFlightView>();
            if (hud == null ||
                playerInput == null ||
                deviceState == null ||
                manualFlightView == null)
            {
                throw new InvalidOperationException("Phase 12 requires Phase 11 HUD, player input, device state, and manual flight view.");
            }

            var root = CreateRoot(hud.transform);
            var turretRoot = CreateManualTurretRoot(root.transform);
            CreateAsteroidBackdrop(turretRoot.transform);
            var target = CreateTargetMarker(turretRoot.transform);
            var reticle = CreateReticle(turretRoot.transform);
            var status = CreateStatusText(turretRoot.transform);
            turretRoot.SetActive(false);

            var turretView = hud.GetComponent<ManualTurretView>();
            if (turretView == null)
            {
                turretView = hud.gameObject.AddComponent<ManualTurretView>();
            }

            turretView.Configure(deviceState, turretRoot, reticle, target, status, playerInput);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase12ManualTurretEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 12 manual turret assets are ready.");
        }

        private static GameObject CreateRoot(Transform parent)
        {
            var root = new GameObject(Phase12RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30;
            root.AddComponent<GraphicRaycaster>();
            return root;
        }

        private static GameObject CreateManualTurretRoot(Transform parent)
        {
            var root = new GameObject(ManualTurretRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.01f, 0.014f, 0.018f, 1f);
            background.raycastTarget = true;
            return root;
        }

        private static void CreateAsteroidBackdrop(Transform parent)
        {
            var positions = new[]
            {
                new Vector2(-760f, 320f),
                new Vector2(-460f, -250f),
                new Vector2(-180f, 250f),
                new Vector2(250f, -210f),
                new Vector2(590f, 230f),
                new Vector2(790f, -330f),
                new Vector2(40f, 40f)
            };

            for (var i = 0; i < positions.Length; i++)
            {
                var asteroid = new GameObject("Phase 12 Backdrop Asteroid " + (i + 1), typeof(RectTransform));
                asteroid.transform.SetParent(parent, false);
                var rectTransform = asteroid.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = positions[i];
                rectTransform.sizeDelta = new Vector2(42f, 42f);

                var label = asteroid.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = i % 2 == 0 ? 28 : 22;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = new Color(0.45f, 0.52f, 0.5f, 1f);
                label.raycastTarget = false;
                label.text = "O";
            }
        }

        private static RectTransform CreateReticle(Transform parent)
        {
            var reticle = new GameObject(ManualTurretReticleName, typeof(RectTransform));
            reticle.transform.SetParent(parent, false);

            var rectTransform = reticle.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(58f, 58f);

            var label = reticle.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.62f, 0.95f, 1f, 1f);
            label.raycastTarget = false;
            label.text = "X";
            return rectTransform;
        }

        private static RectTransform CreateTargetMarker(Transform parent)
        {
            var target = new GameObject(ManualTurretTargetName, typeof(RectTransform));
            target.transform.SetParent(parent, false);

            var rectTransform = target.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(64f, 64f);

            var label = target.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 42;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.95f, 0.76f, 0.43f, 1f);
            label.raycastTarget = false;
            label.text = "O";
            return rectTransform;
        }

        private static Text CreateStatusText(Transform parent)
        {
            var textObject = new GameObject(ManualTurretStatusTextName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, -22f);
            rectTransform.sizeDelta = new Vector2(360f, 150f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.UpperLeft;
            label.color = new Color(0.9f, 0.96f, 0.92f, 1f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
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

                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    return;
                }
            }
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
