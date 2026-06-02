namespace Bellerophon.Core.Player
{
    public interface IPlayerInteractable
    {
        string DisplayName { get; }

        string InteractionPrompt { get; }

        bool CanInteract(PlayerInteractionContext context, out string failureReason);

        void Interact(PlayerInteractionContext context);
    }
}
