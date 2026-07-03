using System.Text;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Bellerophon.Core.Ship
{
    public sealed class ShipDeviceHud : MonoBehaviour
    {
        [SerializeField] private ShipDeviceInteractionState interactionState;
        [SerializeField] private Text panelText;
        [SerializeField] private Text transportStatusText;
        [SerializeField] private FirstPersonPlayerInput playerInput;

        private const string ControlRoomButtonRootName = "Control Room Vertical Room Buttons";

        private GameObject controlRoomButtonRoot;
        private Button[] controlRoomRoomButtons;
        private bool ownsCursorSuppression;
        private bool ownsGameplaySuppression;

        public Text PanelText => panelText;

        public Text TransportStatusText => transportStatusText;

        public Button[] ControlRoomRoomButtons => controlRoomRoomButtons ?? new Button[0];

        public void Configure(ShipDeviceInteractionState state, Text label)
        {
            Configure(state, label, transportStatusText);
        }

        public void Configure(ShipDeviceInteractionState state, Text label, Text transportLabel)
        {
            interactionState = state;
            panelText = label;
            transportStatusText = transportLabel;
            DisableTextRaycasts();
            EnsureControlRoomRoomButtons();
            RefreshPanel();
            RefreshTransportStatus();
        }

        private void Update()
        {
            ProcessDeviceInput();
            RefreshPanel();
            RefreshTransportStatus();
        }

        private void OnDisable()
        {
            SetControlRoomInputMode(false);
        }

        private void OnDestroy()
        {
            SetControlRoomInputMode(false);
        }

        public void ProcessDeviceInput()
        {
            if (interactionState == null ||
                interactionState.ActivePanelMode == ShipDevicePanelMode.None)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                interactionState.ExitActiveDevicePanel();
                return;
            }

            if (interactionState.ActivePanelMode != ShipDevicePanelMode.ControlRoom)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
            {
                interactionState.CycleCctv(-1);
            }

            if (Keyboard.current != null && Keyboard.current.dKey.wasPressedThisFrame)
            {
                interactionState.CycleCctv(1);
            }

            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                interactionState.SwitchControlRoomScreenByRightClick();
            }

            ProcessControlRoomSelectionKeys();
        }

        public void RefreshPanel()
        {
            if (panelText == null)
            {
                SetControlRoomInputMode(false);
                SetControlRoomRoomButtonsVisible(false);
                return;
            }

            panelText.raycastTarget = false;
            if (interactionState == null || interactionState.ActivePanelMode == ShipDevicePanelMode.None)
            {
                panelText.enabled = false;
                panelText.text = string.Empty;
                SetControlRoomInputMode(false);
                SetControlRoomRoomButtonsVisible(false);
                return;
            }

            panelText.enabled = true;
            panelText.text = BuildPanelText();
            var isControlRoom = interactionState.ActivePanelMode == ShipDevicePanelMode.ControlRoom;
            SetControlRoomInputMode(isControlRoom);
            UpdateControlRoomRoomButtons(
                isControlRoom &&
                interactionState.CurrentControlRoomScreenMode == ShipControlRoomScreenMode.VerticalRoomList);
        }

        public void RefreshTransportStatus()
        {
            if (transportStatusText == null)
            {
                return;
            }

            transportStatusText.raycastTarget = false;
            if (interactionState == null || !interactionState.HasActiveTransportRun)
            {
                transportStatusText.enabled = false;
                transportStatusText.text = string.Empty;
                return;
            }

            transportStatusText.enabled = true;
            transportStatusText.text = BuildTransportStatusText();
        }

        private string BuildPanelText()
        {
            switch (interactionState.ActivePanelMode)
            {
                case ShipDevicePanelMode.ManualFlight:
                    return BuildManualFlightText();
                case ShipDevicePanelMode.EngineStatus:
                    return BuildEngineStatusText();
                case ShipDevicePanelMode.ControlRoom:
                    return BuildControlRoomText();
                case ShipDevicePanelMode.TurretManual:
                    return BuildManualTurretText();
                case ShipDevicePanelMode.SupplyStorage:
                    return BuildSupplyStorageText();
                case ShipDevicePanelMode.CargoStatus:
                    return BuildCargoStatusText();
                default:
                    return string.Empty;
            }
        }

        private string BuildManualFlightText()
        {
            var ship = interactionState.CurrentShipState;
            return "Manual Flight\n"
                   + "Mode: " + FormatFlightMode(interactionState.CurrentFlightMode) + "\n"
                   + "Input Response: " + FormatPercent(ShipStateRules.CalculateManualFlightInputMultiplier(ship)) + "\n"
                   + "Booster: " + FormatOnline(ShipStateRules.CanUseBooster(ship)) + "\n"
                   + "Vector: " + FormatSigned(interactionState.ManualFlightOffsetX) + ", " + FormatSigned(interactionState.ManualFlightOffsetY) + "\n"
                   + "Status: " + interactionState.LastInteractionSummary;
        }

        private string BuildTransportStatusText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Transport Run");
            builder.AppendLine("Mode: " + FormatFlightMode(interactionState.CurrentFlightMode));
            builder.AppendLine("Progress: " + FormatPercent(interactionState.TransportProgressPercent));
            builder.AppendLine("Remaining: " + Mathf.CeilToInt(interactionState.TransportRemainingSeconds) + "s");
            builder.AppendLine("Duration Multiplier: x" +
                               ShipStateRules.CalculateTransportDurationMultiplier(interactionState.CurrentShipState).ToString("0.##"));
            builder.AppendLine("Manual Input: " +
                               FormatPercent(ShipStateRules.CalculateManualFlightInputMultiplier(interactionState.CurrentShipState)));
            builder.Append("Auto Pilot: ");
            builder.Append(interactionState.IsAutoPilotAvailable ? "Available" : "Unavailable");

            if (interactionState.HasActiveTransportHazard)
            {
                var hazard = interactionState.CurrentTransportHazard;
                builder.AppendLine();
                builder.Append("Hazard: ");
                builder.Append(FormatHazardType(hazard.HazardType));
                builder.Append(" ");
                builder.Append(Mathf.CeilToInt(hazard.RemainingSeconds));
                builder.Append("s");
            }
            else if (interactionState.LastTransportHazardResult.HasResult)
            {
                builder.AppendLine();
                builder.Append("Last Hazard: ");
                builder.Append(FormatHazardResolution(interactionState.LastTransportHazardResult.Resolution));
            }

            AppendSeedIntruderStatus(builder);

            return builder.ToString();
        }

        private string BuildEngineStatusText()
        {
            var ship = interactionState.CurrentShipState;
            var engineRoom = ship.GetRoom(ShipRoomId.EngineRoom);
            return "Engine Room Screen\n"
                   + "Engine Durability: " + FormatRoomStatus(engineRoom) + "\n"
                   + "Duration Multiplier: x" + ShipStateRules.CalculateTransportDurationMultiplier(ship).ToString("0.##") + "\n"
                   + "Booster: " + FormatOnline(ShipStateRules.CanUseBooster(ship)) + "\n"
                   + "Overclock: " + FormatOnline(ShipStateRules.CanUseEngineOverclock(ship)) + "\n"
                   + "Blackout Rooms: " + ShipStateRules.CalculateEngineBlackoutRoomCount(ship) + "\n"
                   + "Offline Rooms: " + ShipStateRules.CalculateEngineOfflineRoomCount(ship) + "\n"
                   + "Overclock Used: " + FormatBool(interactionState.EngineOverclockUsedThisRun) + "\n"
                   + "Overclock Active: " + FormatBool(interactionState.EngineOverclockActive) + "\n"
                   + interactionState.LastInteractionSummary;
        }

        private string BuildManualTurretText()
        {
            var ship = interactionState.CurrentShipState;
            var turret = interactionState.CurrentManualTurret;
            var target = interactionState.CurrentExternalTarget;
            var builder = new StringBuilder();
            builder.AppendLine("Manual Turret");
            builder.AppendLine("Weapon Mode: " + FormatWeaponOperationMode(interactionState.CurrentWeaponOperationMode));
            builder.AppendLine("Manual Turret: " + FormatOnline(ShipStateRules.CanUseManualTurret(ship)));
            builder.AppendLine("Auto Aim: " + FormatOnline(ShipStateRules.IsAutoAimOnline(ship)));
            builder.AppendLine("Plasma: " + FormatOnline(ShipStateRules.IsPlasmaCannonAvailable(
                ship,
                interactionState.CurrentShipUpgradeState)));
            builder.AppendLine("Aim Response: " + FormatPercent(ShipStateRules.CalculateManualTurretAimMultiplier(ship)));
            builder.AppendLine("Ammo: " + turret.AmmoInMagazine + "/" + turret.MagazineCapacity);
            builder.AppendLine("Reloading: " + FormatBool(turret.IsReloading));
            builder.AppendLine("Plasma Active: " + FormatSeconds(turret.PlasmaActiveRemainingSeconds));
            builder.AppendLine("Plasma Cooldown: " + FormatSeconds(turret.PlasmaCooldownRemainingSeconds));
            builder.AppendLine("Intruder Exposure: " + FormatBool(turret.IntruderHitPossible));
            builder.AppendLine(target.IsActive
                ? "Target: " + FormatExternalTargetType(target.TargetType) + " " + target.CurrentHealth + "/" + target.MaxHealth
                : "Target: None");
            builder.Append("Status: " + interactionState.LastInteractionSummary);
            return builder.ToString();
        }

        private string BuildControlRoomText()
        {
            var ship = interactionState.CurrentShipState;
            var builder = new StringBuilder();
            builder.AppendLine("Control Room Screen: " +
                               FormatControlRoomScreenMode(interactionState.CurrentControlRoomScreenMode));
            builder.AppendLine("Corridor Seal: " + ShipStateRules.CalculateControlRoomClosedCorridorPercent(ship) + "%");
            builder.AppendLine("CCTV Channels: " + ShipStateRules.CalculateControlRoomAvailableCctvCount(ship) +
                               "/" + ShipStateRules.DefaultControlRoomCctvCount);
            builder.AppendLine("Intruder Detection: " + FormatOnline(ShipStateRules.IsIntruderDetectionOnline(ship)));
            builder.AppendLine("Cargo Warning: " + FormatOnline(ShipStateRules.IsCargoDamageWarningOnline(ship)));
            builder.AppendLine("Suppression: " + FormatOnline(ShipStateRules.IsIntruderSuppressionOnline(ship)));
            AppendPurificationLine(builder);

            switch (interactionState.CurrentControlRoomScreenMode)
            {
                case ShipControlRoomScreenMode.MainCctv:
                    AppendMainCctvScreen(builder);
                    break;
                case ShipControlRoomScreenMode.VerticalRoomList:
                    AppendVerticalRoomListScreen(builder);
                    break;
                case ShipControlRoomScreenMode.HorizontalShipLayout:
                    AppendHorizontalShipLayoutScreen(builder);
                    break;
            }

            return builder.ToString();
        }

        private void AppendMainCctvScreen(StringBuilder builder)
        {
            builder.AppendLine("CCTV A/D: " +
                               ShipDeviceInteractionState.GetCctvDisplayName(interactionState.CurrentCctvTarget));
            var roomId = ShipDeviceInteractionState.GetRoomForCctvTarget(interactionState.CurrentCctvTarget);
            AppendRoomLine(builder, ShipDeviceInteractionState.GetCctvDisplayName(interactionState.CurrentCctvTarget), roomId);
            AppendRoomPresenceLine(builder, roomId);
        }

        private void AppendVerticalRoomListScreen(StringBuilder builder)
        {
            builder.AppendLine("Vertical Room List");
            builder.AppendLine("Click a listed room or press 1-6.");
            var rooms = ShipDeviceInteractionState.GetControlRoomVerticalRoomOrder();
            for (var i = 0; i < rooms.Length; i++)
            {
                var roomId = rooms[i];
                var room = interactionState.CurrentShipState.GetRoom(roomId);
                builder.AppendLine((i + 1) + ". " + FormatRoomName(roomId) +
                                   " | Sealed: " + FormatBool(room.IsSealed) +
                                   " | Durability: " + FormatRoomStatus(room));
            }
        }

        public Button GetControlRoomRoomButtonForValidation(ShipRoomId roomId)
        {
            EnsureControlRoomRoomButtons();
            var rooms = ShipDeviceInteractionState.GetControlRoomVerticalRoomOrder();
            for (var i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] == roomId && controlRoomRoomButtons != null && i < controlRoomRoomButtons.Length)
                {
                    return controlRoomRoomButtons[i];
                }
            }

            return null;
        }

        private void ProcessControlRoomSelectionKeys()
        {
            if (Keyboard.current == null ||
                interactionState == null ||
                interactionState.CurrentControlRoomScreenMode != ShipControlRoomScreenMode.VerticalRoomList)
            {
                return;
            }

            for (var i = 0; i < 6; i++)
            {
                if (IsDisplayIndexPressed(i + 1))
                {
                    interactionState.SelectControlRoomVerticalRoomByDisplayIndex(i + 1);
                    return;
                }
            }
        }

        private bool IsDisplayIndexPressed(int displayIndex)
        {
            switch (displayIndex)
            {
                case 1:
                    return Keyboard.current[Key.Digit1].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad1].wasPressedThisFrame;
                case 2:
                    return Keyboard.current[Key.Digit2].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad2].wasPressedThisFrame;
                case 3:
                    return Keyboard.current[Key.Digit3].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad3].wasPressedThisFrame;
                case 4:
                    return Keyboard.current[Key.Digit4].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad4].wasPressedThisFrame;
                case 5:
                    return Keyboard.current[Key.Digit5].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad5].wasPressedThisFrame;
                case 6:
                    return Keyboard.current[Key.Digit6].wasPressedThisFrame ||
                           Keyboard.current[Key.Numpad6].wasPressedThisFrame;
                default:
                    return false;
            }
        }

        private void EnsureControlRoomRoomButtons()
        {
            if (controlRoomButtonRoot != null || panelText == null)
            {
                return;
            }

            var parent = panelText.transform.parent;
            if (parent == null)
            {
                return;
            }

            var existing = parent.Find(ControlRoomButtonRootName);
            if (existing != null)
            {
                DestroyUnityObject(existing.gameObject);
            }

            controlRoomButtonRoot = new GameObject(ControlRoomButtonRootName);
            controlRoomButtonRoot.transform.SetParent(parent, false);

            var panelRect = panelText.rectTransform;
            var rootRect = controlRoomButtonRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = panelRect.anchorMin;
            rootRect.anchorMax = panelRect.anchorMax;
            rootRect.pivot = panelRect.pivot;
            rootRect.anchoredPosition = panelRect.anchoredPosition;
            rootRect.sizeDelta = panelRect.sizeDelta;

            var rooms = ShipDeviceInteractionState.GetControlRoomVerticalRoomOrder();
            controlRoomRoomButtons = new Button[rooms.Length];
            for (var i = 0; i < rooms.Length; i++)
            {
                controlRoomRoomButtons[i] = CreateControlRoomRoomButton(rootRect, rooms[i], i + 1);
            }

            SetControlRoomRoomButtonsVisible(false);
        }

        private Button CreateControlRoomRoomButton(RectTransform parent, ShipRoomId roomId, int displayIndex)
        {
            var buttonObject = new GameObject("Control Room " + FormatRoomName(roomId) + " Button");
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-16f, 48f + (displayIndex - 1) * 34f);
            rectTransform.sizeDelta = new Vector2(260f, 30f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.12f, 0.82f);

            var button = buttonObject.AddComponent<Button>();
            var capturedIndex = displayIndex;
            button.onClick.AddListener(() =>
            {
                if (interactionState != null)
                {
                    interactionState.SelectControlRoomVerticalRoomByDisplayIndex(capturedIndex);
                }
            });

            var textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(0.9f, 0.96f, 0.93f, 1f);
            label.raycastTarget = false;
            label.text = displayIndex + ". " + FormatRoomName(roomId);

            return button;
        }

        private void UpdateControlRoomRoomButtons(bool visible)
        {
            EnsureControlRoomRoomButtons();
            SetControlRoomRoomButtonsVisible(visible);
            if (!visible || controlRoomRoomButtons == null)
            {
                return;
            }

            var canUse = interactionState != null &&
                         ShipStateRules.CanUseControlRoomRoomOperation(interactionState.CurrentShipState) &&
                         !interactionState.CurrentControlRoomPurification.IsActive;
            for (var i = 0; i < controlRoomRoomButtons.Length; i++)
            {
                if (controlRoomRoomButtons[i] != null)
                {
                    controlRoomRoomButtons[i].interactable = canUse;
                }
            }
        }

        private void SetControlRoomRoomButtonsVisible(bool visible)
        {
            if (controlRoomButtonRoot != null && controlRoomButtonRoot.activeSelf != visible)
            {
                controlRoomButtonRoot.SetActive(visible);
            }
        }

        private void SetControlRoomInputMode(bool active)
        {
            if (playerInput == null)
            {
                playerInput = Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            }

            if (playerInput == null)
            {
                if (Application.isPlaying && active)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                ownsCursorSuppression = active;
                ownsGameplaySuppression = active;
                return;
            }

            if (active)
            {
                if (!ownsCursorSuppression)
                {
                    playerInput.SetCursorLockSuppressed(true);
                    ownsCursorSuppression = true;
                }

                if (!ownsGameplaySuppression)
                {
                    playerInput.SetGameplayInputSuppressed(true);
                    ownsGameplaySuppression = true;
                }

                return;
            }

            if (ownsGameplaySuppression)
            {
                playerInput.SetGameplayInputSuppressed(false);
                ownsGameplaySuppression = false;
            }

            if (ownsCursorSuppression)
            {
                playerInput.SetCursorLockSuppressed(false);
                ownsCursorSuppression = false;
            }
        }

        private void AppendHorizontalShipLayoutScreen(StringBuilder builder)
        {
            builder.AppendLine("Ship Layout");
            AppendRoomLine(builder, "Cockpit", ShipRoomId.Cockpit);
            AppendRoomPresenceLine(builder, ShipRoomId.Cockpit);
            AppendRoomLine(builder, "Cargo Hold", ShipRoomId.CargoHold);
            AppendRoomPresenceLine(builder, ShipRoomId.CargoHold);
            AppendRoomLine(builder, "Engine Room", ShipRoomId.EngineRoom);
            AppendRoomPresenceLine(builder, ShipRoomId.EngineRoom);
            AppendRoomLine(builder, "Control Room", ShipRoomId.ControlRoom);
            AppendRoomPresenceLine(builder, ShipRoomId.ControlRoom);
            AppendRoomLine(builder, "Armory", ShipRoomId.Armory);
            AppendRoomPresenceLine(builder, ShipRoomId.Armory);
            AppendRoomLine(builder, "Supply Room", ShipRoomId.SupplyRoom);
            AppendRoomPresenceLine(builder, ShipRoomId.SupplyRoom);
        }

        private void AppendPurificationLine(StringBuilder builder)
        {
            var purification = interactionState.CurrentControlRoomPurification;
            if (!purification.IsActive)
            {
                builder.AppendLine("Internal Purification: Offline");
                return;
            }

            builder.AppendLine("Internal Purification: " + FormatRoomName(purification.TargetRoom) +
                               " " + Mathf.CeilToInt(purification.RemainingSeconds) + "s" +
                               " | Fire " + purification.AppliedFireDamage + "/" +
                               ControlRoomPurificationState.TotalFireDamage +
                               " | Player Damage " + interactionState.LastPurificationPlayerDamage);
        }

        private void AppendRoomPresenceLine(StringBuilder builder, ShipRoomId roomId)
        {
            var hostileCount = CountHostileEntities(roomId);
            var friendlyCount = CountFriendlyEntities(roomId);
            builder.AppendLine("  Friendly: " + friendlyCount + " | Hostile: " + hostileCount);
        }

        private string BuildSupplyStorageText()
        {
            var ship = interactionState.CurrentShipState;
            var equipment = interactionState.CurrentEquipmentState;
            var builder = new StringBuilder();
            builder.AppendLine("Supply Storage");
            builder.AppendLine("Usable Slots: " + interactionState.SupplySlotCount +
                               "/" + equipment.UnlockedSupplySlotCount);
            builder.AppendLine("Tabs: " + BuildStorageTabsText());
            builder.AppendLine("Storage Security: " + FormatOnline(ShipStateRules.IsSupplyStorageSecurityOnline(ship)));
            builder.AppendLine("Equipment Durability Risk: " +
                               FormatPercent(ShipStateRules.CalculateSupplyEquipmentDurabilityDamagePercent(ship)));
            builder.AppendLine("Explosion Risk: " +
                               ShipStateRules.CalculateSupplyEquipmentExplosionChancePercent(ship) + "%");
            builder.AppendLine("Suit: " + (equipment.HasBasicProtectiveSuit
                ? "Basic Protective Suit"
                : "None"));
            builder.AppendLine("Hand Slots");
            for (var i = 0; i < interactionState.HandSlotCount; i++)
            {
                builder.AppendLine("Hand " + (i + 1) + ": " + interactionState.GetHandSlotLabel(i));
            }

            builder.AppendLine("Storage Slots");
            for (var i = 0; i < interactionState.SupplySlotCount; i++)
            {
                builder.AppendLine("Slot " + (i + 1) + ": " + interactionState.GetSupplySlotLabel(i));
            }

            var equipmentSummary = interactionState.CurrentEquipmentState.LastActionSummary;
            if (!string.IsNullOrWhiteSpace(equipmentSummary))
            {
                builder.AppendLine("Equipment: " + equipmentSummary);
            }

            return builder.ToString();
        }

        private string BuildCargoStatusText()
        {
            var ship = interactionState.CurrentShipState;
            var cargo = interactionState.CurrentCargoState;
            return "Cargo Hold Cargo\n"
                   + "Hold Capacity: " + FormatPercent(ShipStateRules.CalculateCargoHoldCapacityMultiplier(ship)) + "\n"
                   + "Personal Cargo: " + FormatOnline(ShipStateRules.CanTransportPersonalCargo(ship)) + "\n"
                   + "Cargo Loss Rule: " + FormatPercent(ShipStateRules.CalculateCargoLossPercentFromCargoHold(ship)) + "\n"
                   + "Durability: " + FormatPercent(cargo.DurabilityPercent) + "\n"
                   + "Loss: " + FormatPercent(cargo.LossPercent) + "\n"
                   + "Grade: " + cargo.Grade + "\n"
                   + "Size: " + cargo.SizeUnits;
        }

        private void AppendRoomLine(StringBuilder builder, string label, ShipRoomId roomId)
        {
            var room = interactionState.CurrentShipState.GetRoom(roomId);
            builder.AppendLine(label + ": " + FormatRoomStatus(room) + " | " +
                               ShipStateRules.BuildRoomDamageEffectSummary(interactionState.CurrentShipState, roomId));
        }

        private int CountFriendlyEntities(ShipRoomId roomId)
        {
            var playerStatus = Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            if (playerStatus == null)
            {
                return 0;
            }

            return ShipInteriorMapRules.FindCurrentRoom(playerStatus.transform.position) == roomId ? 1 : 0;
        }

        private int CountHostileEntities(ShipRoomId roomId)
        {
            var seedIntruder = interactionState.CurrentSeedIntruder;
            return seedIntruder.IsActive && seedIntruder.Intruder.CurrentRoom == roomId ? 1 : 0;
        }

        private void AppendSeedIntruderStatus(StringBuilder builder)
        {
            var seedIntruder = interactionState.CurrentSeedIntruder;
            if (seedIntruder.Kind == SeedIntruderKind.None)
            {
                return;
            }

            builder.AppendLine();
            if (seedIntruder.IsActive)
            {
                builder.Append("Intruder: ");
                builder.Append(FormatSeedIntruderKind(seedIntruder.Kind));
                builder.Append(" in ");
                builder.Append(FormatRoomName(seedIntruder.TargetRoom));
                builder.Append(" ");
                builder.Append(seedIntruder.Intruder.CurrentHealth);
                builder.Append("/");
                builder.Append(seedIntruder.Intruder.MaxHealth);
                builder.Append(" HP");
                builder.AppendLine();
                builder.Append("Intruder Damage: ");
                builder.Append(seedIntruder.TotalRoomDamageApplied);
                return;
            }

            if (seedIntruder.IsResolved)
            {
                builder.Append("Last Intruder: ");
                builder.Append(FormatSeedIntruderKind(seedIntruder.Kind));
                builder.Append(" ");
                builder.Append(seedIntruder.Intruder.Resolution);
            }
        }

        private static string FormatRoomStatus(ShipRoomState room)
        {
            return FormatPercent(room.DurabilityPercent) + " " + ColorizeTier(room.DurabilityTier);
        }

        private static string FormatPercent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private static string FormatBool(bool value)
        {
            return value ? "Yes" : "No";
        }

        private static string FormatOnline(bool value)
        {
            return value ? "Online" : "Offline";
        }

        private static string FormatFlightMode(ShipFlightMode mode)
        {
            return mode == ShipFlightMode.AutoPilot ? "Auto Pilot" : "Manual Flight";
        }

        private static string FormatWeaponOperationMode(ShipWeaponOperationMode mode)
        {
            return mode == ShipWeaponOperationMode.ManualTurret ? "Manual Turret" : "Auto Turret";
        }

        private static string FormatControlRoomScreenMode(ShipControlRoomScreenMode mode)
        {
            switch (mode)
            {
                case ShipControlRoomScreenMode.MainCctv:
                    return "Main CCTV";
                case ShipControlRoomScreenMode.VerticalRoomList:
                    return "Vertical Room List";
                case ShipControlRoomScreenMode.HorizontalShipLayout:
                    return "Horizontal Ship Layout";
                default:
                    return mode.ToString();
            }
        }

        private static string FormatSeconds(float seconds)
        {
            return Mathf.CeilToInt(Mathf.Max(0f, seconds)) + "s";
        }

        private static string FormatHazardType(TransportHazardType hazardType)
        {
            return TransportHazardRules.FormatHazardType(hazardType);
        }

        private static string FormatHazardResolution(TransportHazardResolution resolution)
        {
            switch (resolution)
            {
                case TransportHazardResolution.Neutralized:
                    return "Neutralized";
                case TransportHazardResolution.Avoided:
                    return "Avoided";
                case TransportHazardResolution.GlancingHit:
                    return "Glancing Hit";
                case TransportHazardResolution.DirectHit:
                    return "Direct Hit";
                default:
                    return "None";
            }
        }

        private static string FormatExternalTargetType(ExternalTargetType targetType)
        {
            switch (targetType)
            {
                case ExternalTargetType.Asteroid:
                    return "Asteroid";
                case ExternalTargetType.AlienLifeform:
                    return "Alien Lifeform";
                case ExternalTargetType.CargoFreedomLeagueBoardingCraft:
                    return "Cargo Freedom Boarding Craft";
                case ExternalTargetType.SpacePirateBoardingCraft:
                    return "Space Pirate Boarding Craft";
                default:
                    return "None";
            }
        }

        private static string FormatSigned(float value)
        {
            return value >= 0f ? "+" + value.ToString("0.00") : value.ToString("0.00");
        }

        private static string FormatSeedIntruderKind(SeedIntruderKind kind)
        {
            return SeedIntruderRules.FormatSeedIntruderKind(kind);
        }

        private static string FormatRoomName(ShipRoomId roomId)
        {
            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return "Cockpit";
                case ShipRoomId.CargoHold:
                    return "Cargo Hold";
                case ShipRoomId.EngineRoom:
                    return "Engine Room";
                case ShipRoomId.ControlRoom:
                    return "Control Room";
                case ShipRoomId.Armory:
                    return "Armory";
                case ShipRoomId.SupplyRoom:
                    return "Supply Room";
                default:
                    return roomId.ToString();
            }
        }

        private static string BuildStorageTabsText()
        {
            var tabs = EquipmentRules.GetStorageTabOrder();
            var builder = new StringBuilder();
            for (var i = 0; i < tabs.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(EquipmentRules.FormatCategoryTabName(tabs[i]));
            }

            return builder.ToString();
        }

        private static string ColorizeTier(ShipRoomDurabilityTier tier)
        {
            switch (tier)
            {
                case ShipRoomDurabilityTier.Optimal:
                    return "<color=#f0f4f0>Optimal</color>";
                case ShipRoomDurabilityTier.Stable:
                    return "<color=#f2d84b>Stable</color>";
                case ShipRoomDurabilityTier.Damaged:
                    return "<color=#f29a32>Damaged</color>";
                case ShipRoomDurabilityTier.Critical:
                    return "<color=#ef5b42>Critical</color>";
                case ShipRoomDurabilityTier.Destroyed:
                    return "<color=#202020>Destroyed</color>";
                default:
                    return tier.ToString();
            }
        }

        private void DisableTextRaycasts()
        {
            if (panelText != null)
            {
                panelText.raycastTarget = false;
            }

            if (transportStatusText != null)
            {
                transportStatusText.raycastTarget = false;
            }
        }

        private static void DestroyUnityObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (target is GameObject gameObject)
                {
                    gameObject.SetActive(false);
                }

                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
