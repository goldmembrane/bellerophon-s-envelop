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
    public static class Phase8TransportRunBootstrap
    {
        public const string CargoRunScenePath = Phase7NewGameStartBootstrap.CargoRunScenePath;
        public const string Phase8RootName = "Phase 8 Transport Run";
        public const string TransportStatusTextName = "Phase 8 Transport Status Text";
        public const string ManualFlightRootName = "Phase 8 Manual Flight View";
        public const string ManualFlightPlayerMarkerName = "Phase 8 Manual Flight Player Marker";
        public const string ManualFlightStatusTextName = "Phase 8 Manual Flight Status Text";
        public const string AsteroidFieldName = "Phase 8 Asteroid Field";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 8 Transport Run")]
        public static void EnsurePhase8Assets()
        {
            Phase7NewGameStartBootstrap.EnsurePhase7Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase8RootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            if (hud == null || playerInput == null || deviceState == null || deviceHud == null || deviceHud.PanelText == null)
            {
                throw new InvalidOperationException("Phase 8 requires the Phase 7 HUD, player input, ship device state, and device HUD.");
            }

            var root = CreateRoot(hud.transform);
            var transportText = CreateTransportStatusText(root.transform);
            var manualRoot = CreateManualFlightRoot(root.transform);
            var marker = CreateManualFlightMarker(manualRoot.transform);
            var statusText = CreateManualFlightStatusText(manualRoot.transform);
            CreateAsteroidField(manualRoot.transform);
            manualRoot.SetActive(false);

            deviceHud.Configure(deviceState, deviceHud.PanelText, transportText);

            var manualFlightView = hud.GetComponent<ManualFlightView>();
            if (manualFlightView == null)
            {
                manualFlightView = hud.gameObject.AddComponent<ManualFlightView>();
            }

            manualFlightView.Configure(deviceState, manualRoot, marker, statusText, playerInput);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase8TransportRunEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 8 transport run assets are ready.");
        }

        private static GameObject CreateRoot(Transform parent)
        {
            var root = new GameObject(Phase8RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            return root;
        }

        private static Text CreateTransportStatusText(Transform parent)
        {
            var textObject = new GameObject(TransportStatusTextName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -28f);
            rectTransform.sizeDelta = new Vector2(380f, 130f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.UpperCenter;
            label.color = new Color(0.88f, 0.95f, 0.9f, 1f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = string.Empty;
            label.enabled = false;
            return label;
        }

        private static GameObject CreateManualFlightRoot(Transform parent)
        {
            var root = new GameObject(ManualFlightRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.015f, 0.018f, 0.024f, 1f);
            return root;
        }

        private static RectTransform CreateManualFlightMarker(Transform parent)
        {
            var markerObject = new GameObject(ManualFlightPlayerMarkerName, typeof(RectTransform));
            markerObject.transform.SetParent(parent, false);

            var rectTransform = markerObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(56f, 34f);

            var marker = markerObject.AddComponent<Text>();
            marker.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            marker.fontSize = 30;
            marker.alignment = TextAnchor.MiddleCenter;
            marker.color = new Color(0.75f, 0.96f, 1f, 1f);
            marker.raycastTarget = false;
            marker.text = "A";
            return rectTransform;
        }

        private static Text CreateManualFlightStatusText(Transform parent)
        {
            var textObject = new GameObject(ManualFlightStatusTextName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, -22f);
            rectTransform.sizeDelta = new Vector2(320f, 120f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.UpperLeft;
            label.color = new Color(0.88f, 0.95f, 0.9f, 1f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private static void CreateAsteroidField(Transform parent)
        {
            var field = new GameObject(AsteroidFieldName, typeof(RectTransform));
            field.transform.SetParent(parent, false);

            var rectTransform = field.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var positions = new[]
            {
                new Vector2(-820f, 360f),
                new Vector2(-520f, 250f),
                new Vector2(-210f, 410f),
                new Vector2(150f, 330f),
                new Vector2(520f, 260f),
                new Vector2(830f, 390f),
                new Vector2(-740f, -60f),
                new Vector2(-380f, -310f),
                new Vector2(60f, -230f),
                new Vector2(430f, -120f),
                new Vector2(760f, -360f),
                new Vector2(-80f, 35f)
            };

            for (var i = 0; i < positions.Length; i++)
            {
                CreateAsteroidLabel(field.transform, i + 1, positions[i]);
            }
        }

        private static void CreateAsteroidLabel(Transform parent, int index, Vector2 position)
        {
            var asteroid = new GameObject("Asteroid " + index, typeof(RectTransform));
            asteroid.transform.SetParent(parent, false);

            var rectTransform = asteroid.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(48f, 48f);

            var label = asteroid.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = index % 3 == 0 ? 34 : 24;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.72f, 0.76f, 0.72f, 1f);
            label.raycastTarget = false;
            label.text = "O";
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
            }
        }
    }
}
