namespace _Game.Scripts.Core.Features.Player.Interact
{
    public interface IInteractable : IHoverable
    {
        void Interact(InteractionContext context);
    }
}
