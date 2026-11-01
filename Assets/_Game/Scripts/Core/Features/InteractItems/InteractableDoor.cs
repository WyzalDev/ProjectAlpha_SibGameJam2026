using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Pause;
using _Game.Scripts.Core.Features.Player.Interact;
using DG.Tweening;
using UnityEngine;

namespace _Game.Scripts.Core.Features.InteractItems
{
    public class InteractableDoor : InteractableBase, IPausable
    {
        [Header("References")]
        [SerializeField] private Transform _doorVisual;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float _openDuration = 1f;
        [SerializeField] private float _openDistance = 3f;
        [SerializeField] private Ease _openEase = Ease.OutCubic;

        private Vector3 _closedLocalPosition;
        private Vector3 _openLocalPosition;

        private Tween _movementTween;

        private bool _isOpen;

        private IPauseController _pauseController;

        public bool IsOpen => _isOpen;

        protected override void Awake()
        {
            base.Awake();

            if (_doorVisual == null)
            {
                Debug.LogError(
                    $"{nameof(InteractableDoor)}: Door Visual is not assigned.",
                    this);

                enabled = false;
                return;
            }

            _closedLocalPosition = _doorVisual.localPosition;

            _openLocalPosition =
                _closedLocalPosition + Vector3.up * _openDistance;
        }

        public void Init()
        {
            _pauseController = ServiceLocator.GetService<IPauseController>();
            _pauseController?.Add(this);
        }

        public override void Interact(InteractionContext context)
        {
            base.Interact(context);

            Open();
        }

        public void Open()
        {
            if (_isOpen)
                return;

            _isOpen = true;

            AnimateDoor();
        }

        private void AnimateDoor()
        {
            _movementTween?.Kill();

            float distance = Vector3.Distance(
                _doorVisual.localPosition,
                _openLocalPosition);

            float normalizedDistance =
                distance / Mathf.Max(_openDistance, 0.001f);

            float actualDuration = _openDuration * normalizedDistance;

            if (actualDuration <= 0.001f)
            {
                _doorVisual.localPosition = _openLocalPosition;
                return;
            }

            _movementTween = _doorVisual
                .DOLocalMove(_openLocalPosition, actualDuration)
                .SetEase(_openEase)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void OnDisable()
        {
            _movementTween?.Kill();
            _movementTween = null;
        }

        private void OnDestroy()
        {
            _pauseController?.Remove(this);
        }

        private void OnValidate()
        {
            _openDuration = Mathf.Max(0.01f, _openDuration);
            _openDistance = Mathf.Max(0f, _openDistance);
        }

        public void Stop()
        {
            _movementTween?.Pause();
        }

        public void Resume()
        {
            _movementTween?.Play();
        }
    }
}