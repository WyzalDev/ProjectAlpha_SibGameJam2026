using UnityEngine;

namespace _Game.Scripts.Core.Features.Player.Interact
{
    public class TestInteractable : InteractableBase
    {
        private int _interactionCount;

        public override void Interact(InteractionContext context)
        {
            _interactionCount++;
            Debug.Log($"[TestInteractable] {context.Interactor.name} interacted {_interactionCount} time(s).", this);
        }
    }
}
