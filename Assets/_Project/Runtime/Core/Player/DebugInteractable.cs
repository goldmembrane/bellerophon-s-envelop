using UnityEngine;

namespace Bellerophon.Core.Player
{
    public sealed class DebugInteractable : MonoBehaviour, IPlayerInteractable
    {
        [SerializeField] private string displayName = "Phase 2 Target";
        [SerializeField] private string interactionPrompt = "Inspect";
        [SerializeField] private bool canInteract = true;
        [SerializeField] private string blockedReason = "Interaction is blocked.";
        [SerializeField] private int interactionCount;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

        public string InteractionPrompt => string.IsNullOrWhiteSpace(interactionPrompt) ? "Interact" : interactionPrompt;

        public int InteractionCount => interactionCount;

        public void Configure(string targetName, string prompt, bool isInteractable)
        {
            displayName = targetName;
            interactionPrompt = prompt;
            canInteract = isInteractable;
        }

        public bool CanInteract(PlayerInteractionContext context, out string failureReason)
        {
            failureReason = canInteract ? string.Empty : blockedReason;
            return canInteract;
        }

        public void Interact(PlayerInteractionContext context)
        {
            interactionCount++;
        }
    }
}
