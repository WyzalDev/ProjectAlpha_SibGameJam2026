using System;
using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Features.Enemies;
using UnityEngine;

namespace _Game.Scripts.Core.Features.Player.Flash
{
    [DisallowMultipleComponent]
    public sealed class PlayerFlashController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerFlashData _data;

        [Tooltip("Transform that defines the direction of the flash. Usually the player camera.")]
        [SerializeField] private Transform _flashOrigin;

        [Tooltip("Spot Light used to create the flash.")]
        [SerializeField] private Light _flashLight;

        [Header("Detection")]
        [SerializeField] private LayerMask _robotMask;
        [SerializeField] private LayerMask _obstacleMask;

        [Min(8)]
        [SerializeField] private int _overlapBufferSize = 64;

        private IInputHandler _inputHandler;

        private Collider[] _overlapBuffer;
        private readonly HashSet<RobotAI> _hitRobots = new();

        private Coroutine _flashRoutine;

        private float _nextFlashTime;

        private bool _isInitialized;
        private bool _isSubscribed;
        private bool _bufferWarningShown;

        /// <summary>
        /// Invoked when a flash is successfully activated.
        /// Can be used by UI, camera effects or screenshot systems.
        /// </summary>
        public event Action FlashStarted;

        public bool IsOnCooldown =>
            Time.time < _nextFlashTime;

        public float CooldownRemaining =>
            Mathf.Max(0f, _nextFlashTime - Time.time);

        public float CooldownNormalized =>
            _data == null || _data.Cooldown <= 0f
                ? 0f
                : Mathf.Clamp01(CooldownRemaining / _data.Cooldown);

        private Transform Origin =>
            _flashOrigin != null ? _flashOrigin : transform;

        private void Awake()
        {
            _overlapBufferSize = Mathf.Max(8, _overlapBufferSize);
            _overlapBuffer = new Collider[_overlapBufferSize];

            if (_flashOrigin == null)
                _flashOrigin = transform;

            if (_flashLight != null)
            {
                // This component owns the flash light.
                _flashLight.type = LightType.Spot;
                _flashLight.enabled = false;
                _flashLight.intensity = 0f;
            }
        }

        private void OnEnable()
        {
            // Если Bootstrap уже вызвал Init(),
            // восстанавливаем подписку после повторного включения объекта.
            if (_isInitialized)
                SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            StopFlash();
        }

        private void OnDestroy()
        {
            UnsubscribeInput();
        }

        /// <summary>
        /// Initializes the flash controller.
        /// Should be called from Bootstrap after ServiceLocator initialization.
        /// Can safely be called multiple times.
        /// </summary>
        public void Init()
        {
            if (_isInitialized)
            {
                SubscribeInput();
                return;
            }

            _inputHandler = ServiceLocator.GetService<IInputHandler>();

            if (_inputHandler == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerFlashController)} on {name}: InputHandler not found. " +
                    "Call Init() after ServiceLocator initialization.",
                    this);

                return;
            }

            _isInitialized = true;

            SubscribeInput();

            Debug.Log($"{nameof(PlayerFlashController)} initialized on {name}.", this);
        }
        
        private void SubscribeInput()
        {
            if (!_isInitialized ||
                _isSubscribed ||
                _inputHandler == null ||
                !isActiveAndEnabled)
            {
                return;
            }

            _inputHandler.PhotoPerformed += HandlePhotoPerformed;
            _isSubscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!_isSubscribed || _inputHandler == null)
                return;

            _inputHandler.PhotoPerformed -= HandlePhotoPerformed;
            _isSubscribed = false;
        }

        private void HandlePhotoPerformed()
        {
            TryFlash();
        }

        /// <summary>
        /// Attempts to activate the flash.
        /// Returns false if the flash is unavailable or on cooldown.
        /// </summary>
        public bool TryFlash()
        {
            if (!isActiveAndEnabled)
                return false;

            if (_data == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerFlashController)} on {name}: PlayerFlashData is missing.",
                    this);

                return false;
            }

            if (_flashLight == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerFlashController)} on {name}: Flash Light is missing.",
                    this);

                return false;
            }

            if (IsOnCooldown)
                return false;

            // Start cooldown immediately.
            _nextFlashTime = Time.time + _data.Cooldown;

            // Stop a previous flash if it is still fading out.
            StopFlash();

            ConfigureFlashLight();

            // Detect and stun robots exactly once per flash.
            StunRobotsInFlashArea();

            // Turn on the light.
            _flashLight.enabled = true;
            _flashLight.intensity = _data.MaxIntensity;

            // Notify listeners, for example a screenshot UI.
            FlashStarted?.Invoke();

            _flashRoutine = StartCoroutine(FlashRoutine());

            return true;
        }

        private void ConfigureFlashLight()
        {
            Transform origin = Origin;

            _flashLight.transform.SetPositionAndRotation(
                origin.position,
                origin.rotation);

            _flashLight.type = LightType.Spot;
            _flashLight.color = _data.FlashColor;
            _flashLight.range = _data.Range;
            _flashLight.spotAngle = _data.AreaAngle;
        }

        private IEnumerator FlashRoutine()
        {
            // Keep maximum intensity for the configured duration.
            yield return new WaitForSeconds(_data.Duration);

            float fadeDuration = _data.FadeOutDuration;

            if (fadeDuration > 0f)
            {
                float elapsed = 0f;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;

                    float normalizedTime =
                        Mathf.Clamp01(elapsed / fadeDuration);

                    float intensityMultiplier =
                        Mathf.Clamp01(
                            _data.FadeOutCurve.Evaluate(normalizedTime));

                    _flashLight.intensity =
                        _data.MaxIntensity * intensityMultiplier;

                    yield return null;
                }
            }

            TurnOffFlashLight();
            _flashRoutine = null;
        }

        private void StunRobotsInFlashArea()
        {
            if (_overlapBuffer == null)
                return;

            Transform origin = Origin;

            Vector3 originPosition = origin.position;
            Vector3 forward = origin.forward;

            int hitCount = Physics.OverlapSphereNonAlloc(
                originPosition,
                _data.Range,
                _overlapBuffer,
                _robotMask,
                QueryTriggerInteraction.Collide);

            if (hitCount >= _overlapBuffer.Length && !_bufferWarningShown)
            {
                _bufferWarningShown = true;

                Debug.LogWarning(
                    $"{nameof(PlayerFlashController)} on {name}: " +
                    "Overlap buffer is full. Some robot colliders may not be checked. " +
                    "Increase Overlap Buffer Size.",
                    this);
            }

            _hitRobots.Clear();

            float halfAngle = _data.AreaAngle * 0.5f;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapBuffer[i];

                if (hit == null)
                    continue;

                RobotAI robot = hit.GetComponentInParent<RobotAI>();

                if (robot == null ||
                    !robot.isActiveAndEnabled ||
                    !robot.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // A robot can have several colliders.
                // Do not process the same robot more than once.
                if (_hitRobots.Contains(robot))
                    continue;

                Vector3 targetPosition = hit.bounds.center;
                Vector3 direction = targetPosition - originPosition;

                float distance = direction.magnitude;

                if (distance <= 0.001f || distance > _data.Range)
                    continue;

                direction /= distance;

                // Check whether the robot is inside the flash cone.
                float angle = Vector3.Angle(forward, direction);

                if (angle > halfAngle)
                    continue;

                // Check obstacles between player and robot.
                if (IsBlocked(originPosition, direction, distance, robot))
                    continue;

                _hitRobots.Add(robot);

                robot.ApplyStun();
            }

            // Avoid retaining stale collider references.
            for (int i = 0; i < hitCount; i++)
                _overlapBuffer[i] = null;
        }

        private bool IsBlocked(
            Vector3 origin,
            Vector3 direction,
            float distance,
            RobotAI targetRobot)
        {
            if (_obstacleMask.value == 0)
                return false;

            if (!Physics.Raycast(
                    origin,
                    direction,
                    out RaycastHit hit,
                    distance,
                    _obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // If the ray hits the target robot itself, it is visible.
            RobotAI hitRobot =
                hit.collider.GetComponentInParent<RobotAI>();

            return hitRobot != targetRobot;
        }

        private void StopFlash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            TurnOffFlashLight();
        }

        private void TurnOffFlashLight()
        {
            if (_flashLight == null)
                return;

            _flashLight.intensity = 0f;
            _flashLight.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (_data == null)
                return;

            Transform origin =
                _flashOrigin != null ? _flashOrigin : transform;

            Vector3 position = origin.position;

            float halfAngle = _data.AreaAngle * 0.5f;

            Vector3 leftDirection =
                Quaternion.AngleAxis(-halfAngle, origin.up) * origin.forward;

            Vector3 rightDirection =
                Quaternion.AngleAxis(halfAngle, origin.up) * origin.forward;

            Gizmos.color = Color.yellow;

            Gizmos.DrawLine(
                position,
                position + leftDirection * _data.Range);

            Gizmos.DrawLine(
                position,
                position + rightDirection * _data.Range);

            Gizmos.DrawLine(
                position + leftDirection * _data.Range,
                position + rightDirection * _data.Range);
        }
    }
}
