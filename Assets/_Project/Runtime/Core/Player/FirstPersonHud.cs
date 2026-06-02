using UnityEngine;
using UnityEngine.UI;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonHud : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayerStatus playerStatus;
        [SerializeField] private FirstPersonInteractionController interactionController;
        [SerializeField] private Text healthText;
        [SerializeField] private Text shieldText;
        [SerializeField] private Text interactionPromptText;

        public void Configure(
            FirstPersonPlayerStatus status,
            Text healthLabel,
            Text shieldLabel,
            FirstPersonInteractionController interaction = null,
            Text promptLabel = null)
        {
            playerStatus = status;
            healthText = healthLabel;
            shieldText = shieldLabel;
            interactionController = interaction;
            interactionPromptText = promptLabel;
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (playerStatus == null)
            {
                RefreshInteractionPrompt();
                return;
            }

            if (healthText != null)
            {
                healthText.text = $"HP {playerStatus.CurrentHealth}/{playerStatus.MaxHealth}";
            }

            if (shieldText != null)
            {
                shieldText.text = $"SH {playerStatus.CurrentShield}/{playerStatus.MaxShield}";
            }

            RefreshInteractionPrompt();
        }

        private void RefreshInteractionPrompt()
        {
            if (interactionPromptText == null)
            {
                return;
            }

            if (interactionController == null || !interactionController.HasCurrentTarget)
            {
                interactionPromptText.enabled = false;
                interactionPromptText.text = string.Empty;
                return;
            }

            interactionPromptText.enabled = true;
            if (interactionController.CurrentTargetCanInteract)
            {
                interactionPromptText.text = $"F - {interactionController.CurrentTargetPrompt} {interactionController.CurrentTargetDisplayName}";
                return;
            }

            interactionPromptText.text = string.IsNullOrWhiteSpace(interactionController.CurrentTargetFailureReason)
                ? interactionController.CurrentTargetDisplayName
                : interactionController.CurrentTargetFailureReason;
        }
    }
}
