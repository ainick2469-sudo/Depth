namespace FrontierDepths.Core
{
    public interface IInteractable
    {
        string DisplayName { get; }
        string Prompt { get; }
        bool CanInteract(PlayerInteractor interactor, out string reason);
        void Interact(PlayerInteractor interactor);
    }
}
