using _Game.Scripts.Core.Features.Player.Interact;
using UnityEngine;

namespace _Game.Scripts.Core.Features.InteractDoor
{
    public class DoorButton : InteractableBase
    {
        [Header("References")]
        [SerializeField] private InteractableDoor _door;

        public override void Interact(InteractionContext context)
        {
            base.Interact(context);

            if (_door == null)
            {
                Debug.LogWarning(
                    $"{nameof(DoorButton)}: Door is not assigned.",
                    this);

                return;
            }

            _door.Open();
        }
    }
}