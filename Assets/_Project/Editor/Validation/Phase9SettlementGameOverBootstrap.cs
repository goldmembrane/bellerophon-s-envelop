using System;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase9SettlementGameOverBootstrap
    {
        public const string CargoRunScenePath = Phase8TransportRunBootstrap.CargoRunScenePath;
        public const string Phase9RootName = "Phase 9 Settlement Game Over";
        public const string SettlementRootName = "Phase 9 Settlement Panel";
        public const string SettlementTitleTextName = "Phase 9 Settlement Title";
        public const string SettlementBodyTextName = "Phase 9 Settlement Body";
        public const string SettlementStatusTextName = "Phase 9 Settlement Status";
        public const string GameOverRootName = "Phase 9 Game Over Cutscene";
        public const string GameOverTitleTextName = "Phase 9 Game Over Title";
        public const string GameOverBodyTextName = "Phase 9 Game Over Body";
        public const string CargoShipVisualName = "Phase 9 Cargo Ship Visual";
        public const string PodVisualName = "Phase 9 Ejected Pod Visual";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 9 Settlement Game Over")]
        public static void EnsurePhase9Assets()
        {
            Phase8TransportRunBootstrap.EnsurePhase8Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase9RootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var startController = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            if (hud == null || playerInput == null || deviceState == null || startController == null)
            {
                throw new InvalidOperationException("Phase 9 requires the Phase 8 HUD, player input, device state, and start flow controller.");
            }

            var root = CreateRoot(hud.transform);
            var settlementRoot = CreateSettlementRoot(root.transform);
            var settlementTitle = CreateText(
                SettlementTitleTextName,
                settlementRoot.transform,
                new Vector2(0f, 160f),
                new Vector2(650f, 44f),
                28,
                TextAnchor.MiddleLeft);
            var settlementBody = CreateText(
                SettlementBodyTextName,
                settlementRoot.transform,
                new Vector2(0f, 10f),
                new Vector2(650f, 250f),
                18,
                TextAnchor.UpperLeft);
            var settlementStatus = CreateText(
                SettlementStatusTextName,
                settlementRoot.transform,
                new Vector2(0f, -164f),
                new Vector2(650f, 42f),
                18,
                TextAnchor.UpperLeft);

            var gameOverRoot = CreateGameOverRoot(root.transform);
            var cargoShipVisual = CreateImageVisual(
                CargoShipVisualName,
                gameOverRoot.transform,
                new Vector2(-170f, 25f),
                new Vector2(430f, 150f),
                new Color(0.38f, 0.43f, 0.48f, 1f));
            var podVisual = CreateImageVisual(
                PodVisualName,
                gameOverRoot.transform,
                new Vector2(-55f, 6f),
                new Vector2(54f, 36f),
                new Color(0.88f, 0.72f, 0.42f, 1f));
            var gameOverTitle = CreateText(
                GameOverTitleTextName,
                gameOverRoot.transform,
                new Vector2(0f, 250f),
                new Vector2(780f, 58f),
                34,
                TextAnchor.MiddleCenter);
            var gameOverBody = CreateText(
                GameOverBodyTextName,
                gameOverRoot.transform,
                new Vector2(0f, -260f),
                new Vector2(780f, 86f),
                22,
                TextAnchor.UpperCenter);

            settlementRoot.SetActive(false);
            gameOverRoot.SetActive(false);

            var controller = root.AddComponent<TransportSettlementController>();
            controller.Configure(
                startController,
                deviceState,
                playerInput,
                settlementRoot,
                settlementTitle,
                settlementBody,
                settlementStatus,
                gameOverRoot,
                cargoShipVisual,
                podVisual,
                gameOverTitle,
                gameOverBody);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase9SettlementGameOverEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 9 settlement and game over assets are ready.");
        }

        private static GameObject CreateRoot(Transform parent)
        {
            var root = new GameObject(Phase9RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            return root;
        }

        private static GameObject CreateSettlementRoot(Transform parent)
        {
            var root = new GameObject(SettlementRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 35f);
            rectTransform.sizeDelta = new Vector2(730f, 430f);

            var background = root.AddComponent<Image>();
            background.color = new Color(0.045f, 0.055f, 0.06f, 0.96f);
            return root;
        }

        private static GameObject CreateGameOverRoot(Transform parent)
        {
            var root = new GameObject(GameOverRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.01f, 0.012f, 0.018f, 1f);
            return root;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.9f, 0.96f, 0.92f, 1f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private static RectTransform CreateImageVisual(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var visualObject = new GameObject(name, typeof(RectTransform));
            visualObject.transform.SetParent(parent, false);

            var rectTransform = visualObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var image = visualObject.AddComponent<Image>();
            image.color = color;
            return rectTransform;
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
