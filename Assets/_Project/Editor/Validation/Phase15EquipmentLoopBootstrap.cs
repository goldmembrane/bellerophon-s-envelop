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
    public static class Phase15EquipmentLoopBootstrap
    {
        public const string CargoRunScenePath = Phase14ParvumIntruderBootstrap.CargoRunScenePath;
        public const string Phase15RootName = "Phase 15 Equipment Loop";
        public const string EquipmentHudTextName = "Phase 15 Equipment HUD Text";
        public const string PrecisionReticleTextName = "Phase 15 Precision Reticle";
        public const string ShopRootName = "Phase 15 Equipment Shop";
        public const string ShopTitleTextName = "Phase 15 Shop Title";
        public const string ShopBodyTextName = "Phase 15 Shop Body";
        public const string ShopStatusTextName = "Phase 15 Shop Status";
        public const string BuyTabButtonName = "Phase 15 Buy Tab Button";
        public const string SellTabButtonName = "Phase 15 Sell Tab Button";
        public const string BuyStickButtonName = "Phase 15 Buy Stick Button";
        public const string BuyMusketButtonName = "Phase 15 Buy Musket Button";
        public const string BuyShotgunButtonName = "Phase 15 Buy Shotgun Button";
        public const string BuyFlashlightButtonName = "Phase 15 Buy Flashlight Button";
        public const string BuyInjuryRelieverButtonName = "Phase 15 Buy Injury Reliever Button";
        public const string BuyProtectiveSuitButtonName = "Phase 15 Buy Protective Suit Button";
        public const string BuyStrengthEnhancerButtonName = "Phase 15 Buy Strength Enhancer Button";
        public const string DisposePurchasedItemButtonName = "Phase 15 Dispose Purchased Item Button";
        public const string SellPersonalCargoButtonName = "Phase 15 Sell Personal Cargo Button";
        public const string SellSelectedItemButtonName = "Phase 15 Sell Selected Item Button";
        public const string SellItemRowButtonPrefix = "Phase 15 Sell Item Row Button ";
        public const string CloseShopButtonName = "Phase 15 Close Shop Button";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 15 Equipment Loop")]
        public static void EnsurePhase15Assets()
        {
            Phase14ParvumIntruderBootstrap.EnsurePhase14Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase15RootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var handInventory = UnityEngine.Object.FindFirstObjectByType<FirstPersonHandInventory>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var startController = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            if (hud == null ||
                playerInput == null ||
                handInventory == null ||
                deviceState == null ||
                startController == null ||
                maintenanceController == null)
            {
                throw new InvalidOperationException("Phase 15 requires Phase 14 HUD, player input, hand inventory, device state, start flow, and maintenance controllers.");
            }

            var root = CreateRoot(hud.transform);
            var equipmentHud = CreateText(
                EquipmentHudTextName,
                root.transform,
                new Vector2(24f, 126f),
                new Vector2(420f, 116f),
                18,
                TextAnchor.LowerLeft);
            var precisionReticle = CreateText(
                PrecisionReticleTextName,
                root.transform,
                Vector2.zero,
                new Vector2(80f, 80f),
                34,
                TextAnchor.MiddleCenter);
            precisionReticle.enabled = false;

            var equipmentController = root.AddComponent<PlayerEquipmentController>();
            equipmentController.Configure(handInventory, playerInput, deviceState, equipmentHud, precisionReticle);

            var shopRoot = CreateShopRoot(root.transform);
            var shopTitle = CreateText(
                ShopTitleTextName,
                shopRoot.transform,
                new Vector2(0f, 220f),
                new Vector2(780f, 44f),
                28,
                TextAnchor.MiddleCenter);
            var shopBody = CreateText(
                ShopBodyTextName,
                shopRoot.transform,
                new Vector2(0f, 15f),
                new Vector2(780f, 330f),
                18,
                TextAnchor.UpperLeft);
            var shopStatus = CreateText(
                ShopStatusTextName,
                shopRoot.transform,
                new Vector2(-90f, -220f),
                new Vector2(560f, 44f),
                17,
                TextAnchor.MiddleCenter);
            var buyTab = CreateButton(BuyTabButtonName, shopRoot.transform, new Vector2(-300f, 175f), "Buy");
            var sellTab = CreateButton(SellTabButtonName, shopRoot.transform, new Vector2(-170f, 175f), "Sell");
            var buyStick = CreateButton(BuyStickButtonName, shopRoot.transform, new Vector2(-40f, 175f), "Buy Stick");
            var buyMusket = CreateButton(BuyMusketButtonName, shopRoot.transform, new Vector2(110f, 175f), "Buy Musket");
            var buyFlashlight = CreateButton(BuyFlashlightButtonName, shopRoot.transform, new Vector2(260f, 175f), "Flashlight");
            var buyShotgun = CreateButton(BuyShotgunButtonName, shopRoot.transform, new Vector2(-40f, 135f), "Shotgun");
            var buyProtectiveSuit = CreateButton(BuyProtectiveSuitButtonName, shopRoot.transform, new Vector2(110f, 135f), "Protect Suit");
            var buyInjuryReliever = CreateButton(BuyInjuryRelieverButtonName, shopRoot.transform, new Vector2(260f, 135f), "Injury Aid");
            var buyStrengthEnhancer = CreateButton(BuyStrengthEnhancerButtonName, shopRoot.transform, new Vector2(260f, 95f), "Strength");
            var sellRows = CreateSellRowButtons(shopRoot.transform);
            var sellSelected = CreateButton(
                SellSelectedItemButtonName,
                shopRoot.transform,
                new Vector2(260f, 175f),
                "Sell Selected",
                new Vector2(150f, 36f),
                14);
            var closeShop = CreateButton(CloseShopButtonName, shopRoot.transform, new Vector2(390f, 220f), "Close");

            var shopController = root.AddComponent<EquipmentShopController>();
            shopController.Configure(
                startController,
                maintenanceController,
                deviceState,
                equipmentController,
                shopRoot,
                shopTitle,
                shopBody,
                shopStatus,
                buyTab,
                sellTab,
                buyStick,
                buyMusket,
                closeShop,
                null,
                buyFlashlight,
                buyInjuryReliever,
                null,
                sellSelected,
                sellRows,
                buyShotgun,
                buyProtectiveSuit,
                buyStrengthEnhancer);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase15EquipmentLoopEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 15 equipment loop assets are ready.");
        }

        private static GameObject CreateRoot(Transform parent)
        {
            var root = new GameObject(Phase15RootName, typeof(RectTransform));
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

        private static GameObject CreateShopRoot(Transform parent)
        {
            var root = new GameObject(ShopRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(860f, 520f);

            var background = root.AddComponent<Image>();
            background.color = new Color(0.025f, 0.032f, 0.034f, 1f);
            root.SetActive(false);
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

        private static Button[] CreateSellRowButtons(Transform parent)
        {
            var buttons = new Button[8];
            for (var i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateButton(
                    SellItemRowButtonPrefix + (i + 1),
                    parent,
                    new Vector2(-382f, 72f - i * 31f),
                    (i + 1).ToString(),
                    new Vector2(44f, 28f),
                    14);
            }

            return buttons;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, string labelText)
        {
            return CreateButton(name, parent, anchoredPosition, labelText, new Vector2(140f, 36f), 15);
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            string labelText,
            Vector2 size,
            int fontSize)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.28f, 0.24f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.28f, 0.24f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.36f, 0.31f, 1f);
            colors.pressedColor = new Color(0.12f, 0.2f, 0.18f, 1f);
            colors.disabledColor = new Color(0.09f, 0.11f, 0.11f, 0.85f);
            button.colors = colors;

            var text = CreateText(
                name + " Label",
                buttonObject.transform,
                Vector2.zero,
                new Vector2(Mathf.Max(20f, size.x - 10f), Mathf.Max(18f, size.y - 8f)),
                fontSize,
                TextAnchor.MiddleCenter);
            text.text = labelText;
            text.color = new Color(0.94f, 0.98f, 0.94f, 1f);
            return button;
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
