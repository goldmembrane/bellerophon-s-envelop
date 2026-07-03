using System;
using System.Reflection;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase7NewGameStartBootstrap
    {
        public const string CargoRunScenePath = Phase6RoomInteractionsBootstrap.CargoRunScenePath;
        public const string Phase7RootName = "Phase 7 New Game Start Flow";
        public const string TitleTextName = "Phase 7 Start Title";
        public const string BodyTextName = "Phase 7 Start Body";
        public const string StatusTextName = "Phase 7 Start Status";
        public const string YesButtonName = "Phase 7 Association Yes Button";
        public const string NoButtonName = "Phase 7 Association No Button";
        public const string TutorialButtonName = "Phase 7 Tutorial Contract Button";
        public const string SkipTutorialButtonName = "Phase 7 Skip Tutorial Button";

        private const string ProjectInputActionsPath = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 7 New Game Start")]
        public static void EnsurePhase7Assets()
        {
            Phase6RoomInteractionsBootstrap.EnsurePhase6Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase7RootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if (hud == null || playerInput == null || deviceState == null)
            {
                throw new InvalidOperationException("Phase 7 requires the Phase 6 HUD, player input, and ship device state.");
            }

            var root = CreatePanelRoot(hud.transform);
            var titleText = CreateText(TitleTextName, root.transform, new Vector2(0f, 158f), new Vector2(600f, 44f), 28, TextAnchor.MiddleLeft);
            var bodyText = CreateText(BodyTextName, root.transform, new Vector2(0f, 32f), new Vector2(600f, 190f), 21, TextAnchor.UpperLeft);
            var statusText = CreateText(StatusTextName, root.transform, new Vector2(0f, -126f), new Vector2(600f, 48f), 18, TextAnchor.UpperLeft);
            var noButton = CreateButton(NoButtonName, "No", root.transform, new Vector2(-256f, -178f), new Vector2(104f, 48f));
            var yesButton = CreateButton(YesButtonName, "Yes", root.transform, new Vector2(-136f, -178f), new Vector2(104f, 48f));
            var tutorialButton = CreateButton(TutorialButtonName, "Accept Tutorial", root.transform, new Vector2(56f, -178f), new Vector2(220f, 48f));
            var skipTutorialButton = CreateButton(SkipTutorialButtonName, "Skip Tutorial", root.transform, new Vector2(272f, -178f), new Vector2(180f, 48f));
            EnsureEventSystem();

            var controller = root.AddComponent<NewGameStartFlowController>();
            controller.ConfigurePersistence(true, SaveGameService.DefaultSlotId, false);
            controller.Configure(
                titleText,
                bodyText,
                statusText,
                yesButton,
                tutorialButton,
                deviceState,
                playerInput,
                noButton,
                skipTutorialButton);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase7NewGameStartEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 7 new game start assets are ready.");
        }

        private static GameObject CreatePanelRoot(Transform parent)
        {
            var root = new GameObject(Phase7RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, 70f);
            rectTransform.sizeDelta = new Vector2(680f, 440f);

            var background = root.AddComponent<Image>();
            background.color = new Color(0.06f, 0.075f, 0.08f, 0.92f);
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
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.9f, 0.96f, 0.91f, 1f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private static Button CreateButton(
            string name,
            string label,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2? size = null)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size ?? new Vector2(140f, 48f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.22f, 0.34f, 0.31f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            buttonObject.AddComponent<CanvasGroup>();

            var colors = button.colors;
            colors.normalColor = new Color(0.22f, 0.34f, 0.31f, 1f);
            colors.highlightedColor = new Color(0.32f, 0.48f, 0.42f, 1f);
            colors.pressedColor = new Color(0.14f, 0.22f, 0.2f, 1f);
            button.colors = colors;

            CreateButtonLabel(label, buttonObject.transform);
            return button;
        }

        private static void CreateButtonLabel(string label, Transform parent)
        {
            var textObject = new GameObject("Label");
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.98f, 0.94f, 1f);
            text.raycastTarget = false;
            text.text = label;
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
                AssignDefaultInputActions(eventSystemObject.AddComponent<InputSystemUIInputModule>());
                return;
            }

            AssignDefaultInputActions(ReplaceInputSystemUiModule(eventSystem.gameObject));
        }

        private static void AssignDefaultInputActions(InputSystemUIInputModule inputModule)
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ProjectInputActionsPath);
            if (inputActions != null)
            {
                inputModule.actionsAsset = inputActions;
                inputModule.point = CreateActionReference(inputActions, "UI/Point");
                inputModule.leftClick = CreateActionReference(inputActions, "UI/Click");
                inputModule.rightClick = CreateActionReference(inputActions, "UI/RightClick");
                inputModule.middleClick = CreateActionReference(inputActions, "UI/MiddleClick");
                inputModule.scrollWheel = CreateActionReference(inputActions, "UI/ScrollWheel");
                inputModule.move = CreateActionReference(inputActions, "UI/Navigate");
                inputModule.submit = CreateActionReference(inputActions, "UI/Submit");
                inputModule.cancel = CreateActionReference(inputActions, "UI/Cancel");
                inputModule.trackedDevicePosition = CreateActionReference(inputActions, "UI/TrackedDevicePosition");
                inputModule.trackedDeviceOrientation = CreateActionReference(inputActions, "UI/TrackedDeviceOrientation");
                EditorUtility.SetDirty(inputModule);
                return;
            }

            var method = typeof(InputSystemUIInputModule).GetMethod(
                "AssignDefaultActions",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            method?.Invoke(inputModule, null);
            EditorUtility.SetDirty(inputModule);
        }

        private static InputSystemUIInputModule ReplaceInputSystemUiModule(GameObject eventSystemObject)
        {
            var existingModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            if (existingModule != null)
            {
                UnityEngine.Object.DestroyImmediate(existingModule);
            }

            return eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static InputActionReference CreateActionReference(InputActionAsset inputActions, string actionName)
        {
            var action = inputActions.FindAction(actionName, true);
            return InputActionReference.Create(action);
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
