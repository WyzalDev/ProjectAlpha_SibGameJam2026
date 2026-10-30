using UnityEngine;
using _Game.Scripts.Core.UI.Interact;

namespace _Game.Scripts.Core.Features.Player.Interact
{
    public class InteractionContext
    {
        public GameObject Interactor { get; }
        public InteractionPromptView Prompt { get; }

        public InteractionContext(GameObject interactor, InteractionPromptView prompt)
        {
            Interactor = interactor;
            Prompt = prompt;
        }
    }
}
