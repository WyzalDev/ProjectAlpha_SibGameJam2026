namespace _Game.Scripts.Core.Features.Player.Interact
{
    public interface IHoverable
    {
        void OnHoverEnter(InteractionContext context);
        void OnHoverExit(InteractionContext context);
    }
}
