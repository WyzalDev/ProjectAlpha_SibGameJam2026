using TMPro;
using UnityEngine;

namespace _Game.Scripts.Core.UI.Interact
{
    public class InteractionPromptView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _label;

        private void Awake()
        {
            Hide();
        }

        public void Show(string text)
        {
            if (_label != null)
                _label.text = text;

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }
    }
}
