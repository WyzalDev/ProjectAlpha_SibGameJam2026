using UnityEngine;
using _Game.Scripts.Core.UI.Interact;

namespace _Game.Scripts.Core.Features.Player.Interact
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private Outline _outline;

        [Header("Hints")]
        [SerializeField, TextArea] private string _promptText = "[E] Взаимодействовать";

        [Header("Outline")]
        [SerializeField] private Outline.Mode _outlineMode = Outline.Mode.OutlineVisible;
        [SerializeField] private Color _outlineColor = new Color(1f, 0.92f, 0.016f);
        [SerializeField, Range(0f, 10f)] private float _outlineWidth = 2f;

        protected InteractionContext CurrentContext { get; private set; }
        protected Outline Outline => _outline;

        protected virtual void Awake()
        {
            SetupOutline();
        }

        public virtual void OnHoverEnter(InteractionContext context)
        {
            CurrentContext = context;

            if (_outline != null)
                _outline.enabled = true;

            context.Prompt?.Show(_promptText);
        }

        public virtual void OnHoverExit(InteractionContext context)
        {
            if (_outline != null)
                _outline.enabled = false;

            InteractionContext exitContext = context != null ? context : CurrentContext;
            exitContext?.Prompt?.Hide();

            CurrentContext = null;
        }

        public virtual void Interact(InteractionContext context)
        {
        }

        private void SetupOutline()
        {
            if (_outline == null && TryGetComponent(out Outline existing))
                _outline = existing;

            if (_outline == null)
                _outline = gameObject.AddComponent<Outline>();

            _outline.OutlineMode = _outlineMode;
            _outline.OutlineColor = _outlineColor;
            _outline.OutlineWidth = _outlineWidth;
            _outline.enabled = false;
        }
    }
}
