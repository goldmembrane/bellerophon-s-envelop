using UnityEngine;

namespace Bellerophon.Core.Player
{
    public sealed class FirstPersonInteractionController : MonoBehaviour
    {
        [SerializeField] private FirstPersonPlayerSettings settings;
        [SerializeField] private FirstPersonPlayerInput input;
        [SerializeField] private Transform interactionOrigin;

        private RaycastHit currentHit;

        public IPlayerInteractable CurrentInteractable { get; private set; }

        public bool HasCurrentTarget => CurrentInteractable != null;

        public bool CurrentTargetCanInteract { get; private set; }

        public string CurrentTargetFailureReason { get; private set; } = string.Empty;

        public string CurrentTargetDisplayName => CurrentInteractable?.DisplayName ?? string.Empty;

        public string CurrentTargetPrompt => CurrentInteractable?.InteractionPrompt ?? string.Empty;

        public string LastFailureReason { get; private set; } = string.Empty;

        public IPlayerInteractable LastInteractable { get; private set; }

        public void Configure(
            FirstPersonPlayerSettings playerSettings,
            FirstPersonPlayerInput playerInput,
            Transform origin)
        {
            settings = playerSettings;
            input = playerInput;
            interactionOrigin = origin;
            SubscribeInput();
        }

        private void Awake()
        {
            if (input == null)
            {
                input = GetComponent<FirstPersonPlayerInput>();
            }
        }

        private void OnEnable()
        {
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        private void Update()
        {
            RefreshCurrentTarget();
        }

        public bool TryInteract()
        {
            LastInteractable = null;
            LastFailureReason = string.Empty;

            RefreshCurrentTarget();

            if (settings == null || interactionOrigin == null)
            {
                LastFailureReason = "Interaction is not configured.";
                return false;
            }

            if (CurrentInteractable == null)
            {
                LastFailureReason = string.IsNullOrWhiteSpace(CurrentTargetFailureReason)
                    ? "No target in range."
                    : CurrentTargetFailureReason;
                return false;
            }

            if (!CurrentTargetCanInteract)
            {
                LastFailureReason = string.IsNullOrWhiteSpace(CurrentTargetFailureReason)
                    ? "Target refused interaction."
                    : CurrentTargetFailureReason;
                return false;
            }

            var context = new PlayerInteractionContext(gameObject, interactionOrigin, currentHit);
            if (!CurrentInteractable.CanInteract(context, out var failureReason))
            {
                LastFailureReason = string.IsNullOrWhiteSpace(failureReason)
                    ? "Target refused interaction."
                    : failureReason;
                CurrentTargetCanInteract = false;
                CurrentTargetFailureReason = LastFailureReason;
                return false;
            }

            LastInteractable = CurrentInteractable;
            CurrentInteractable.Interact(context);
            return true;
        }

        private void RefreshCurrentTarget()
        {
            CurrentInteractable = null;
            CurrentTargetCanInteract = false;
            CurrentTargetFailureReason = string.Empty;

            if (settings == null || interactionOrigin == null)
            {
                CurrentTargetFailureReason = "Interaction is not configured.";
                return;
            }

            var ray = new Ray(interactionOrigin.position, interactionOrigin.forward);
            if (!Physics.Raycast(ray, out currentHit, settings.InteractionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            var interactable = FindInteractable(currentHit.collider);
            if (interactable == null)
            {
                CurrentTargetFailureReason = "Target is not interactable.";
                return;
            }

            CurrentInteractable = interactable;
            var context = new PlayerInteractionContext(gameObject, interactionOrigin, currentHit);
            CurrentTargetCanInteract = interactable.CanInteract(context, out var failureReason);
            CurrentTargetFailureReason = CurrentTargetCanInteract
                ? string.Empty
                : string.IsNullOrWhiteSpace(failureReason)
                    ? "Target refused interaction."
                    : failureReason;
        }

        private void SubscribeInput()
        {
            if (input == null)
            {
                return;
            }

            input.InteractPressed -= HandleInteractPressed;
            input.InteractPressed += HandleInteractPressed;
        }

        private void UnsubscribeInput()
        {
            if (input == null)
            {
                return;
            }

            input.InteractPressed -= HandleInteractPressed;
        }

        private void HandleInteractPressed()
        {
            TryInteract();
        }

        private static IPlayerInteractable FindInteractable(Collider collider)
        {
            var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
            IPlayerInteractable fallbackInteractable = null;
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IPlayerInteractable interactable)
                {
                    if (behaviour is DebugInteractable)
                    {
                        fallbackInteractable = interactable;
                        continue;
                    }

                    return interactable;
                }
            }

            return fallbackInteractable;
        }
    }
}
