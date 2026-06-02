using System.Text;
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

        public Text PanelText => panelText;

        public void Configure(ShipDeviceInteractionState state, Text label)
        {
            interactionState = state;
            panelText = label;
            RefreshPanel();
        }

        private void Update()
        {
            ProcessDeviceInput();
            RefreshPanel();
        }

        public void ProcessDeviceInput()
        {
            if (interactionState == null ||
                interactionState.ActivePanelMode != ShipDevicePanelMode.ControlRoom ||
                Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                interactionState.CycleCctv(-1);
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                interactionState.CycleCctv(1);
            }
        }

        public void RefreshPanel()
        {
            if (panelText == null)
            {
                return;
            }

            if (interactionState == null || interactionState.ActivePanelMode == ShipDevicePanelMode.None)
            {
                panelText.enabled = false;
                panelText.text = string.Empty;
                return;
            }

            panelText.enabled = true;
            panelText.text = BuildPanelText();
        }

        private string BuildPanelText()
        {
            switch (interactionState.ActivePanelMode)
            {
                case ShipDevicePanelMode.ManualFlight:
                    return "Manual Flight\nMode frame active\nStatus: " + interactionState.LastInteractionSummary;
                case ShipDevicePanelMode.EngineStatus:
                    return BuildEngineStatusText();
                case ShipDevicePanelMode.ControlRoom:
                    return BuildControlRoomText();
                case ShipDevicePanelMode.TurretManual:
                    return "Manual Turret\nMode frame active\nStatus: " + interactionState.LastInteractionSummary;
                case ShipDevicePanelMode.SupplyStorage:
                    return BuildSupplyStorageText();
                case ShipDevicePanelMode.CargoStatus:
                    return BuildCargoStatusText();
                default:
                    return string.Empty;
            }
        }

        private string BuildEngineStatusText()
        {
            var engineRoom = interactionState.CurrentShipState.GetRoom(ShipRoomId.EngineRoom);
            return "Engine Room Screen\n"
                   + "Engine Durability: " + FormatRoomStatus(engineRoom) + "\n"
                   + "Overclock Used: " + FormatBool(interactionState.EngineOverclockUsedThisRun) + "\n"
                   + "Overclock Active: " + FormatBool(interactionState.EngineOverclockActive) + "\n"
                   + interactionState.LastInteractionSummary;
        }

        private string BuildControlRoomText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Control Room Screen");
            builder.AppendLine("CCTV A/D: " + ShipDeviceInteractionState.GetCctvDisplayName(interactionState.CurrentCctvTarget));
            builder.AppendLine("Ship Layout");
            AppendRoomLine(builder, "Cockpit", ShipRoomId.Cockpit);
            AppendRoomLine(builder, "Cargo Hold", ShipRoomId.CargoHold);
            AppendRoomLine(builder, "Engine Room", ShipRoomId.EngineRoom);
            AppendRoomLine(builder, "Control Room", ShipRoomId.ControlRoom);
            AppendRoomLine(builder, "Armory", ShipRoomId.Armory);
            AppendRoomLine(builder, "Supply Room", ShipRoomId.SupplyRoom);
            return builder.ToString();
        }

        private string BuildSupplyStorageText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Supply Storage");
            for (var i = 0; i < interactionState.SupplySlotCount; i++)
            {
                builder.AppendLine("Slot " + (i + 1) + ": " + interactionState.GetSupplySlotLabel(i));
            }

            return builder.ToString();
        }

        private string BuildCargoStatusText()
        {
            var cargo = interactionState.CurrentCargoState;
            return "Cargo Hold Cargo\n"
                   + "Durability: " + FormatPercent(cargo.DurabilityPercent) + "\n"
                   + "Loss: " + FormatPercent(cargo.LossPercent) + "\n"
                   + "Grade: " + cargo.Grade + "\n"
                   + "Size: " + cargo.SizeUnits;
        }

        private void AppendRoomLine(StringBuilder builder, string label, ShipRoomId roomId)
        {
            var room = interactionState.CurrentShipState.GetRoom(roomId);
            builder.AppendLine(label + ": " + FormatRoomStatus(room));
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
    }
}
