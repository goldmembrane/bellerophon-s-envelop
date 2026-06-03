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
        [SerializeField] private Text transportStatusText;

        public Text PanelText => panelText;

        public Text TransportStatusText => transportStatusText;

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
            RefreshPanel();
            RefreshTransportStatus();
        }

        private void Update()
        {
            ProcessDeviceInput();
            RefreshPanel();
            RefreshTransportStatus();
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

            panelText.raycastTarget = false;
            if (interactionState == null || interactionState.ActivePanelMode == ShipDevicePanelMode.None)
            {
                panelText.enabled = false;
                panelText.text = string.Empty;
                return;
            }

            panelText.enabled = true;
            panelText.text = BuildPanelText();
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
            return "Manual Flight\n"
                   + "Mode: " + FormatFlightMode(interactionState.CurrentFlightMode) + "\n"
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
            var engineRoom = interactionState.CurrentShipState.GetRoom(ShipRoomId.EngineRoom);
            return "Engine Room Screen\n"
                   + "Engine Durability: " + FormatRoomStatus(engineRoom) + "\n"
                   + "Overclock Used: " + FormatBool(interactionState.EngineOverclockUsedThisRun) + "\n"
                   + "Overclock Active: " + FormatBool(interactionState.EngineOverclockActive) + "\n"
                   + interactionState.LastInteractionSummary;
        }

        private string BuildManualTurretText()
        {
            var turret = interactionState.CurrentManualTurret;
            var target = interactionState.CurrentExternalTarget;
            var builder = new StringBuilder();
            builder.AppendLine("Manual Turret");
            builder.AppendLine("Ammo: " + turret.AmmoInMagazine + "/" + ManualTurretState.MagazineSize);
            builder.AppendLine("Reloading: " + FormatBool(turret.IsReloading));
            builder.AppendLine("Intruder Exposure: " + FormatBool(turret.IntruderHitPossible));
            builder.AppendLine(target.IsActive
                ? "Target: " + FormatExternalTargetType(target.TargetType) + " " + target.CurrentHealth + "/" + target.MaxHealth
                : "Target: None");
            builder.Append("Status: " + interactionState.LastInteractionSummary);
            return builder.ToString();
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
            builder.AppendLine("Suit: " + (interactionState.CurrentEquipmentState.HasBasicProtectiveSuit
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

        private static string FormatFlightMode(ShipFlightMode mode)
        {
            return mode == ShipFlightMode.AutoPilot ? "Auto Pilot" : "Manual Flight";
        }

        private static string FormatHazardType(TransportHazardType hazardType)
        {
            switch (hazardType)
            {
                case TransportHazardType.AsteroidField:
                    return "Asteroid Field";
                default:
                    return "None";
            }
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
            switch (kind)
            {
                case SeedIntruderKind.Parvum:
                    return "Parvum";
                default:
                    return "None";
            }
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
    }
}
