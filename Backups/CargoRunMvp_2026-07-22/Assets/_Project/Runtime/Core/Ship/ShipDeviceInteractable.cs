using Bellerophon.Core.Player;
using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public sealed class ShipDeviceInteractable : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private ShipDeviceType deviceType;
        [SerializeField] private ShipDeviceInteractionState interactionState;
        [SerializeField] private string displayName = "Ship Device";
        [SerializeField] private string interactionPrompt = "Use";
        [SerializeField] private int interactionCount;

        public ShipDeviceType DeviceType => deviceType;

        public ShipDeviceInteractionState InteractionState => interactionState;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

        public string InteractionPrompt => string.IsNullOrWhiteSpace(interactionPrompt) ? "Use" : interactionPrompt;

        public int InteractionCount => interactionCount;

        public void Configure(
            ShipDeviceType type,
            ShipDeviceInteractionState state,
            string targetName,
            string prompt)
        {
            deviceType = type;
            interactionState = state;
            displayName = targetName;
            interactionPrompt = prompt;
        }

        public bool CanInteract(PlayerInteractionContext context, out string failureReason)
        {
            if (interactionState == null)
            {
                failureReason = "Ship device state is missing.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public void Interact(PlayerInteractionContext context)
        {
            interactionCount++;
            interactionState.ActivateDevice(deviceType);
        }
    }
}
