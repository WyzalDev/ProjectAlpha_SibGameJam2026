using DG.Tweening;
using UnityEngine;

namespace _Game.Scripts.Core.Features.PressedPlate
{
    public class PressurePlateDoor : MonoBehaviour, IPressurePlateListener
    {
        [Header("References")]
        [SerializeField] private Transform _doorVisual;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float _openDuration = 1f;
        [SerializeField, Min(0.01f)] private float _closeDuration = 1f;
        [SerializeField] private float _openDistance = 3f;

        [SerializeField] private Ease _openEase = Ease.OutCubic;
        [SerializeField] private Ease _closeEase = Ease.InCubic;

        private Vector3 _closedLocalPosition;
        private Vector3 _openLocalPosition;

        private Tween _movementTween;

        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (_doorVisual == null)
            {
                Debug.LogError(
                    $"{nameof(PressurePlateDoor)}: Door Visual is not assigned.",
                    this);

                enabled = false;
                return;
            }

            _closedLocalPosition = _doorVisual.localPosition;

            // Дверь открывается вверх.
            _openLocalPosition =
                _closedLocalPosition + Vector3.up * _openDistance;
        }

        public void OnPressurePlatePressed()
        {
            Open();
        }

        public void OnPressurePlateReleased()
        {
            Close();
        }

        public void Open()
        {
            if (_isOpen)
                return;

            _isOpen = true;
            AnimateDoor(_openLocalPosition, _openDuration, _openEase);
        }

        public void Close()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            AnimateDoor(_closedLocalPosition, _closeDuration, _closeEase);
        }

        private void AnimateDoor(Vector3 targetPosition, float duration, Ease ease)
        {
            _movementTween?.Kill();

            float distance = Vector3.Distance(
                _doorVisual.localPosition,
                targetPosition);

            float normalizedDistance = distance / Mathf.Max(_openDistance, 0.001f);
            float actualDuration = duration * normalizedDistance;

            if (actualDuration <= 0.001f)
            {
                _doorVisual.localPosition = targetPosition;
                return;
            }

            _movementTween = _doorVisual
                .DOLocalMove(targetPosition, actualDuration)
                .SetEase(ease)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void OnDisable()
        {
            _movementTween?.Kill();
            _movementTween = null;
        }

        private void OnValidate()
        {
            _openDuration = Mathf.Max(0.01f, _openDuration);
            _closeDuration = Mathf.Max(0.01f, _closeDuration);
            _openDistance = Mathf.Max(0f, _openDistance);
        }
    }
}