using UnityEngine;
using UnityEngine.UI;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonHud : MonoBehaviour
    {
        private const string Phase16HealthPercentName = "Phase 16 Health Percent";
        private const string Phase16ShieldPercentName = "Phase 16 Shield Percent";
        private const string Phase16HealthFillName = "Phase 16 Health Fill";
        private const string Phase16ShieldFillName = "Phase 16 Shield Fill";
        private const string InteractionPromptName = "Interaction Prompt Text";

        [SerializeField] private FirstPersonPlayerStatus playerStatus;
        [SerializeField] private FirstPersonInteractionController interactionController;
        [SerializeField] private Text healthText;
        [SerializeField] private Text shieldText;
        [SerializeField] private Text interactionPromptText;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image shieldFillImage;

        public Text HealthText => healthText;

        public Text ShieldText => shieldText;

        public Text InteractionPromptText => interactionPromptText;

        public Image HealthFillImage => healthFillImage;

        public Image ShieldFillImage => shieldFillImage;

        public void Configure(
            FirstPersonPlayerStatus status,
            Text healthLabel,
            Text shieldLabel,
            FirstPersonInteractionController interaction = null,
            Text promptLabel = null,
            Image healthFill = null,
            Image shieldFill = null)
        {
            playerStatus = status;
            healthText = healthLabel;
            shieldText = shieldLabel;
            interactionController = interaction;
            interactionPromptText = promptLabel;
            healthFillImage = healthFill;
            shieldFillImage = shieldFill;
            Refresh();
        }

        private void Awake()
        {
            ResolveGeneratedHudReferences();
        }

        private void Update()
        {
            Refresh();
        }

        public void ResolveGeneratedHudReferencesForValidation()
        {
            ResolveGeneratedHudReferences();
        }

        private void Refresh()
        {
            if ((healthFillImage == null || shieldFillImage == null) && transform.childCount > 0)
            {
                ResolveGeneratedHudReferences();
            }

            if (playerStatus == null)
            {
                RefreshInteractionPrompt();
                return;
            }

            if (healthText != null)
            {
                healthText.text = FormatPercent(playerStatus.CurrentHealth, playerStatus.MaxHealth);
            }

            if (shieldText != null)
            {
                shieldText.text = FormatPercent(playerStatus.CurrentShield, playerStatus.MaxShield);
            }

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = CalculateRatio(playerStatus.CurrentHealth, playerStatus.MaxHealth);
            }

            if (shieldFillImage != null)
            {
                shieldFillImage.fillAmount = CalculateRatio(playerStatus.CurrentShield, playerStatus.MaxShield);
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

        private static string FormatPercent(int current, int max)
        {
            return Mathf.RoundToInt(CalculateRatio(current, max) * 100f) + "%";
        }

        private static float CalculateRatio(int current, int max)
        {
            if (max <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)current / max);
        }

        private void ResolveGeneratedHudReferences()
        {
            var phase16HealthText = FindText(Phase16HealthPercentName);
            if (phase16HealthText != null)
            {
                healthText = phase16HealthText;
            }

            var phase16ShieldText = FindText(Phase16ShieldPercentName);
            if (phase16ShieldText != null)
            {
                shieldText = phase16ShieldText;
            }

            var phase16HealthFill = FindImage(Phase16HealthFillName);
            if (phase16HealthFill != null)
            {
                healthFillImage = phase16HealthFill;
            }

            var phase16ShieldFill = FindImage(Phase16ShieldFillName);
            if (phase16ShieldFill != null)
            {
                shieldFillImage = phase16ShieldFill;
            }

            if (interactionPromptText == null)
            {
                interactionPromptText = FindText(InteractionPromptName);
            }
        }

        private Text FindText(string objectName)
        {
            var labels = GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].name == objectName)
                {
                    return labels[i];
                }
            }

            return null;
        }

        private Image FindImage(string objectName)
        {
            var images = GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                if (images[i].name == objectName)
                {
                    return images[i];
                }
            }

            return null;
        }
    }
}
