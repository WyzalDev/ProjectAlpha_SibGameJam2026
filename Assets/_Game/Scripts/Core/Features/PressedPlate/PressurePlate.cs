using System;
using _Game.Scripts.Core.Features.Enemies;
using _Game.Scripts.Core.Features.Enemies.Swarm;
using System.Collections.Generic;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Pause;
using DG.Tweening;
using UnityEngine;

namespace _Game.Scripts.Core.Features.PressedPlate
{
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class PressurePlate : MonoBehaviour, IPausable
    {
        [Header("References")]
        [SerializeField] private Transform _plateVisual;
        [SerializeField] private BoxCollider _triggerCollider;
        [SerializeField] private LayerMask _robotLayers;
        [SerializeField] private MonoBehaviour[] _listeners;

        [Header("Movement")]
        [SerializeField, Min(0.01f)] private float _pressDuration = 1.5f;
        [SerializeField, Min(0.01f)] private float _releaseDuration = 0.5f;
        [SerializeField, Min(0f)] private float _pressDepth = 0.15f;

        [SerializeField] private Ease _pressEase = Ease.OutQuad;
        [SerializeField] private Ease _releaseEase = Ease.OutQuad;

        private readonly HashSet<Collider> _occupants = new();
        private readonly List<IPressurePlateListener> _cachedListeners = new();

        private Vector3 _restLocalPosition;
        private float _progress;

        private Tween _movementTween;

        private bool _isPressed;
        private IPauseController _pauseController;
        public bool IsPressed => _isPressed;
        public float Progress => _progress;

        private void Reset()
        {
            _triggerCollider = GetComponent<BoxCollider>();

            int enemiesLayer = LayerMask.NameToLayer("Enemies");

            if (enemiesLayer >= 0)
                _robotLayers = 1 << enemiesLayer;
        }

        public void Init()
        {
            _pauseController = ServiceLocator.GetService<IPauseController>();
            _pauseController?.Add(this);
        }

        private void OnDestroy()
        {
            _pauseController?.Remove(this);
        }

        private void Awake()
        {
            if (_triggerCollider == null)
                _triggerCollider = GetComponent<BoxCollider>();

            _triggerCollider.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if (_plateVisual == null)
            {
                Debug.LogError(
                    $"{nameof(PressurePlate)}: Plate Visual is not assigned.",
                    this);

                enabled = false;
                return;
            }

            _restLocalPosition = _plateVisual.localPosition;

            CacheListeners();
            ApplyProgress(0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsRobot(other))
                return;

            if (!_occupants.Add(other))
                return;

            if (_occupants.Count == 1)
                AnimateTo(1f);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_occupants.Remove(other))
                return;

            if (_occupants.Count == 0)
            {
                // Плита перестаёт считаться нажатой сразу.
                SetPressed(false);

                // Визуально начинает возвращаться.
                AnimateTo(0f);
            }
        }

        private bool IsRobot(Collider other)
        {
            RobotAI regularRobot = other.GetComponentInParent<RobotAI>();
            HunterRobotAI hunterRobot = other.GetComponentInParent<HunterRobotAI>();

            Transform robotTransform = regularRobot != null
                ? regularRobot.transform
                : hunterRobot != null
                    ? hunterRobot.transform
                    : null;

            if (robotTransform == null)
                return false;

            int robotLayer = robotTransform.gameObject.layer;

            return (_robotLayers.value & (1 << robotLayer)) != 0;
        }

        private void AnimateTo(float target)
        {
            _movementTween?.Kill();

            float distance = Mathf.Abs(target - _progress);

            float fullDuration = target > _progress
                ? _pressDuration
                : _releaseDuration;

            // Время зависит от оставшегося расстояния.
            // Поэтому при смене направления прогиб продолжается с текущей позиции.
            float duration = fullDuration * distance;

            if (duration <= 0.001f)
            {
                ApplyProgress(target);
                return;
            }

            Ease ease = target > _progress
                ? _pressEase
                : _releaseEase;

            _movementTween = DOTween.To(
                    () => _progress,
                    ApplyProgress,
                    target,
                    duration)
                .SetEase(ease)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void ApplyProgress(float value)
        {
            _progress = Mathf.Clamp01(value);

            _plateVisual.localPosition =
                _restLocalPosition + Vector3.down * (_pressDepth * _progress);

            // Состояние включается только после полного прогиба.
            if (!_isPressed && _progress >= 0.999f && _occupants.Count > 0)
                SetPressed(true);
        }

        private void SetPressed(bool pressed)
        {
            if (_isPressed == pressed)
                return;

            _isPressed = pressed;

            foreach (IPressurePlateListener listener in _cachedListeners)
            {
                MonoBehaviour component = listener as MonoBehaviour;

                if (component == null)
                    continue;

                if (pressed)
                    listener.OnPressurePlatePressed();
                else
                    listener.OnPressurePlateReleased();
            }
        }

        private void CacheListeners()
        {
            _cachedListeners.Clear();

            foreach (MonoBehaviour listener in _listeners)
            {
                if (listener == null)
                    continue;

                if (listener is IPressurePlateListener pressurePlateListener)
                {
                    _cachedListeners.Add(pressurePlateListener);
                }
                else
                {
                    Debug.LogWarning(
                        $"{listener.name} does not implement IPressurePlateListener.",
                        listener);
                }
            }
        }

        private void OnDisable()
        {
            _movementTween?.Kill();
            _movementTween = null;

            _occupants.Clear();

            SetPressed(false);

            _progress = 0f;

            if (_plateVisual != null)
                _plateVisual.localPosition = _restLocalPosition;
        }

        private void OnValidate()
        {
            _pressDuration = Mathf.Max(0.01f, _pressDuration);
            _releaseDuration = Mathf.Max(0.01f, _releaseDuration);
            _pressDepth = Mathf.Max(0f, _pressDepth);
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