using System;
using System.Text;
using Bellerophon.Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Session
{
    public sealed class PersonalCargoController : MonoBehaviour
    {
        [SerializeField] private NewGameStartFlowController startFlowController;
        [SerializeField] private PlanetMaintenanceController maintenanceController;
        [SerializeField] private FirstPersonPlayerInput playerInput;
        [SerializeField] private GameObject cargoRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button closeButton;

        private string lastStatus = string.Empty;

        public GameObject CargoRoot => cargoRoot;

        public Text BodyText => bodyText;

        public Text StatusText => statusText;

        public Button CollectButton => collectButton;

        public Button CloseButton => closeButton;

        public bool IsCargoVisible => cargoRoot != null && cargoRoot.activeSelf;

        public void Configure(
            NewGameStartFlowController startController,
            PlanetMaintenanceController maintenance,
            FirstPersonPlayerInput firstPersonInput,
            GameObject root,
            Text titleLabel,
            Text bodyLabel,
            Text statusLabel,
            Button collectActionButton,
            Button closeActionButton)
        {
            startFlowController = startController;
            maintenanceController = maintenance;
            playerInput = firstPersonInput;
            cargoRoot = root;
            titleText = titleLabel;
            bodyText = bodyLabel;
            statusText = statusLabel;
            collectButton = collectActionButton;
            closeButton = closeActionButton;
            DisableTextRaycasts();
            BindButtons();
            HideCargoCollection();
        }

        public void ShowCargoCollection()
        {
            if (cargoRoot == null)
            {
                return;
            }

            if (maintenanceController != null)
            {
                maintenanceController.HideMaintenance();
            }

            cargoRoot.SetActive(true);
            SetCursorLockSuppressed(true);
            DisableTextRaycasts();
            RefreshCargoCollection();
        }

        public void HideCargoCollection()
        {
            if (cargoRoot != null)
            {
                cargoRoot.SetActive(false);
            }

            SetCursorLockSuppressed(false);
        }

        public void ReturnToMaintenance()
        {
            HideCargoCollection();
            if (maintenanceController != null)
            {
                maintenanceController.ShowMaintenance();
            }
        }

        public void RefreshCargoCollection()
        {
            DisableTextRaycasts();
            var session = CurrentSession;
            if (session == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = "Personal Cargo Collection";
            }

            if (bodyText != null)
            {
                bodyText.text = BuildBodyText(session);
            }

            if (collectButton != null)
            {
                collectButton.gameObject.SetActive(true);
                collectButton.interactable = CanCollectCommonCargo(session);
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(true);
                closeButton.interactable = true;
            }

            if (statusText != null)
            {
                statusText.text = string.IsNullOrWhiteSpace(lastStatus)
                    ? BuildStatusText(session)
                    : lastStatus;
            }
        }

        public void CollectCargo()
        {
            var session = CurrentSession;
            if (session == null)
            {
                lastStatus = "No active session is available for personal cargo collection.";
                RefreshCargoCollection();
                return;
            }

            var seed = CreateCollectionSeed(session);
            var result = session.CollectPersonalCargo(seed);
            if (result.Collected)
            {
                startFlowController.ApplySessionState(result.State);
            }

            lastStatus = result.Summary;
            RefreshCargoCollection();
        }

        private void Awake()
        {
            BindButtons();
            HideCargoCollection();
        }

        private void Update()
        {
            ProcessPointerClickFallback();
        }

        private void OnDisable()
        {
            SetCursorLockSuppressed(false);
        }

        private void OnDestroy()
        {
            UnbindButtons();
            SetCursorLockSuppressed(false);
        }

        private GameSessionState CurrentSession => startFlowController != null
            ? startFlowController.CurrentSession
            : null;

        private void BindButtons()
        {
            UnbindButtons();
            if (maintenanceController != null && maintenanceController.PersonalCargoButton != null)
            {
                maintenanceController.PersonalCargoButton.onClick.AddListener(ShowCargoCollection);
            }

            if (collectButton != null)
            {
                collectButton.onClick.AddListener(CollectCargo);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ReturnToMaintenance);
            }
        }

        private void UnbindButtons()
        {
            if (maintenanceController != null && maintenanceController.PersonalCargoButton != null)
            {
                maintenanceController.PersonalCargoButton.onClick.RemoveListener(ShowCargoCollection);
            }

            if (collectButton != null)
            {
                collectButton.onClick.RemoveListener(CollectCargo);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(ReturnToMaintenance);
            }
        }

        private static string BuildBodyText(GameSessionState session)
        {
            var builder = new StringBuilder();
            var capacity = PersonalCargoRules.CalculateCapacityUnits(session.Ship);
            var available = PersonalCargoRules.CalculateAvailableUnits(session.Ship, session.PersonalCargoHold);
            builder.AppendLine("Current planet trait: " + PersonalCargoRules.FormatTraitName(session.CurrentPlanetTrait));
            builder.AppendLine("Cargo hold: " + session.PersonalCargoHold.UsedSizeUnits + "/" + capacity + " units");
            builder.AppendLine("Available: " + available + " units");
            builder.AppendLine("Collection cost: Free");
            builder.AppendLine();
            builder.AppendLine("Stored personal cargo");
            if (!session.PersonalCargoHold.HasCargo)
            {
                builder.AppendLine(" - Empty");
                return builder.ToString();
            }

            var items = session.PersonalCargoHold.Items;
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                builder.AppendLine(
                    " - " + (i + 1) + ". " +
                    item.DisplayName +
                    " | " + item.SizeUnits + " units" +
                    " | Durability " + Mathf.RoundToInt(item.DurabilityPercent * 100f) + "%" +
                    " | Origin " + PersonalCargoRules.FormatTraitName(item.OriginTrait));
            }

            return builder.ToString();
        }

        private static string BuildStatusText(GameSessionState session)
        {
            if (session.Phase != GameSessionPhase.Completed)
            {
                return "Personal cargo can be collected while docked at a planet.";
            }

            if (!CanCollectCommonCargo(session))
            {
                return "Cargo hold capacity is full for the smallest personal cargo.";
            }

            return "Collect performs the location task instantly for this skeleton; visual work comes later.";
        }

        private static bool CanCollectCommonCargo(GameSessionState session)
        {
            return session != null &&
                   session.Phase == GameSessionPhase.Completed &&
                   PersonalCargoRules.CalculateAvailableUnits(session.Ship, session.PersonalCargoHold) >=
                   PersonalCargoRules.CommonCargoSizeUnits;
        }

        private static int CreateCollectionSeed(GameSessionState session)
        {
            return (session.CompletedTransportCount * 97) +
                   (session.PersonalCargoHold.Count * 31) +
                   17;
        }

        private void ProcessPointerClickFallback()
        {
            if (!IsCargoVisible ||
                Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var pointerPosition = Mouse.current.position.ReadValue();
            if (TryClickButtonAtScreenPosition(collectButton, pointerPosition, CollectCargo) ||
                TryClickButtonAtScreenPosition(closeButton, pointerPosition, ReturnToMaintenance))
            {
                return;
            }
        }

        private void SetCursorLockSuppressed(bool suppressed)
        {
            if (playerInput == null)
            {
                playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }

            if (playerInput != null)
            {
                playerInput.SetCursorLockSuppressed(suppressed);
            }
        }

        private static bool TryClickButtonAtScreenPosition(Button button, Vector2 screenPosition, Action action)
        {
            if (button == null ||
                action == null ||
                !button.gameObject.activeInHierarchy ||
                !button.interactable)
            {
                return false;
            }

            var rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, null))
            {
                return false;
            }

            action();
            return true;
        }

        private void DisableTextRaycasts()
        {
            SetTextNonBlocking(titleText);
            SetTextNonBlocking(bodyText);
            SetTextNonBlocking(statusText);
        }

        private static void SetTextNonBlocking(Text text)
        {
            if (text != null)
            {
                text.raycastTarget = false;
            }
        }
    }
}
