using System;
using System.Collections.Generic;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Pause;
using _Game.Scripts.Core.Features.Player.Death;
using UnityEngine;

namespace _Game.Scripts.Core.Features.Enemies.Swarm
{
    public sealed class HunterSwarmController : MonoBehaviour, IPausable
    {
        [Header("References")]
        [SerializeField] private PlayerTarget _playerTarget;
        
        [Header("Formation")]
        [Min(0.1f)] [SerializeField] private float _initialRadius = 2.8f;
        [Min(0.1f)] [SerializeField] private float _minimumRadius = 0.85f;
        [Min(0f)] [SerializeField] private float _contractionDelay = 0.5f;
        [Min(0f)] [SerializeField] private float _contractionSpeed = 0.55f;

        [SerializeField] private float _startingAngle;
        [SerializeField] private bool _alignWithPlayerForward = true;

        [Header("Tactical Positions")]
        [Min(0f)] [SerializeField] private float _interceptorLeadDistance = 2.2f;
        [Min(0f)] [SerializeField] private float _minimumInterceptorLeadDistance = 0.9f;
        [Min(0f)] [SerializeField] private float _flankerForwardDistance = 0.6f;
        [Min(0f)] [SerializeField] private float _flankerSideDistance = 2f;
        [Min(0f)] [SerializeField] private float _minimumFlankerSideDistance = 1f;

        [Header("Player Prediction")]
        [Min(0f)] [SerializeField] private float _predictionTime = 0.35f;
        [Min(0f)] [SerializeField] private float _maximumPredictionDistance = 2.5f;
        [Min(0.1f)] [SerializeField] private float _maximumTrackedPlayerSpeed = 8f;
        [Min(0.1f)] [SerializeField] private float _playerVelocitySmoothing = 8f;
        [Min(1f)] [SerializeField] private float _formationRotationSpeed = 300f;

        [Header("Navigation")]
        [Min(0.02f)] [SerializeField] private float _formationUpdateInterval = 0.12f;
        [Min(0.01f)] [SerializeField] private float _minimumDestinationChange = 0.12f;
        [Min(0.1f)] [SerializeField] private float _navMeshSampleRadius = 0.8f;
        [Min(0.1f)] [SerializeField] private float _minimumSlotSpacing = 0.85f;
        [Range(1, 9)] [SerializeField] private int _candidateSearchSteps = 5;
        [Min(1f)] [SerializeField] private float _candidateAngleStep = 18f;

        [Header("Pursuit Pressure")]
        [Min(0f)] [SerializeField] private float _pressureRampDuration = 12f;
        [Min(1f)] [SerializeField] private float _maximumPressureSpeedMultiplier = 1.3f;
        [Min(1f)] [SerializeField] private float _maximumContractionSpeedMultiplier = 1.5f;
        [Min(1f)] [SerializeField] private float _interceptorSpeedBonus = 1.08f;
        [Min(1f)] [SerializeField] private float _flankerSpeedBonus = 1.04f;
        [Min(1f)] [SerializeField] private float _pursuerSpeedBonus = 1.08f;

        [Header("Capture")]
        [Min(0.1f)] [SerializeField] private float _catchDistance = 1.15f;
        [Min(0.02f)] [SerializeField] private float _catchCheckInterval = 0.05f;

        private readonly List<HunterRobotAI> _hunters =
            new List<HunterRobotAI>();

        // Точки, которые уже были выбраны для охотников
        // в текущем обновлении формации.
        private readonly List<Vector3> _reservedDestinations =
            new List<Vector3>();

        private PlayerDeathController _deathController;

        private Vector3 _lastPlayerPosition;
        private Vector3 _playerVelocity;
        private Vector3 _formationForward;

        private float _currentRadius;
        private float _contractionTimer;
        private float _formationTimer;
        private float _catchTimer;
        private float _huntElapsedTime;

        private int _nextFormationSlot;

        private bool _hasLastPlayerPosition;
        private bool _isHuntingStopped;
        private bool _isSubscribedToDeathEvent;
        
        private bool _isPaused;
        private IPauseController _pauseController;

        public int HunterCount => _hunters.Count;

        public float CurrentRadius => _currentRadius;

        public void Init()
        {
            _pauseController = ServiceLocator.GetService<IPauseController>();
            _pauseController?.Add(this);
        }

        private void OnDestroy()
        {
            _pauseController?.Remove(this);
            
            foreach (var hunter in _hunters)
                _pauseController?.Remove(hunter);
        }

        private void Awake()
        {
            _currentRadius = Mathf.Max(
                _initialRadius,
                _minimumRadius);

            _contractionTimer = _contractionDelay;

            if (_playerTarget == null)
                _playerTarget = FindFirstObjectByType<PlayerTarget>();
        }

        private void OnEnable()
        {
            TryBindDeathController();
        }

        private void Start()
        {
            if (_playerTarget == null)
            {
                Debug.LogError(
                    "[HunterSwarmController] PlayerTarget not found.",
                    this);

                return;
            }

            TryBindDeathController();

            Transform playerTransform = _playerTarget.Transform;

            _lastPlayerPosition = playerTransform.position;
            _lastPlayerPosition.y = 0f;

            _hasLastPlayerPosition = true;

            _formationForward = GetPlanarDirection(
                playerTransform.forward,
                Vector3.forward);

            _formationTimer = 0f;
            _catchTimer = 0f;
        }

        private void OnDisable()
        {
            UnsubscribeFromDeathEvent();
        }

        private void Update()
        {
            if (_isPaused ||
                _isHuntingStopped ||
                _playerTarget == null ||
                _hunters.Count == 0)
            {
                return;
            }

            TryBindDeathController();

            // Игрок мог погибнуть не от охотников.
            if (_deathController != null && _deathController.IsDead)
            {
                StopHunting();
                return;
            }

            float deltaTime = Time.deltaTime;

            _huntElapsedTime += deltaTime;

            UpdatePlayerMotion(deltaTime);
            UpdateContraction(deltaTime);
            UpdateFormationTimer(deltaTime);
            UpdateCatchTimer(deltaTime);
        }

        #region Registration

        public void Register(HunterRobotAI hunter)
        {
            if (hunter == null || _hunters.Contains(hunter))
                return;

            bool isFirstHunter = _hunters.Count == 0;

            _hunters.Add(hunter);
            _pauseController?.Add(hunter);

            hunter.SetFormationSlot(_nextFormationSlot++);

            if (isFirstHunter)
            {
                // Новая охота начинается с исходного радиуса.
                _huntElapsedTime = 0f;

                _currentRadius = Mathf.Max(
                    _initialRadius,
                    _minimumRadius);

                _contractionTimer = _contractionDelay;
            }

            ReassignTacticalRoles();

            if (_isHuntingStopped)
            {
                hunter.StopHunting();
                return;
            }

            hunter.StartHunting();

            // Не ждём следующего обычного интервала формации.
            _formationTimer = 0f;
        }

        public void Unregister(HunterRobotAI hunter)
        {
            if (hunter == null)
                return;

            if (!_hunters.Remove(hunter))
                return;

            _pauseController?.Remove(hunter);
            ReassignTacticalRoles();

            _formationTimer = 0f;
        }

        private void ReassignTacticalRoles()
        {
            int count = _hunters.Count;

            if (count == 0)
                return;

            if (count == 1)
            {
                _hunters[0].SetTacticalRole(HunterRole.Pursuer);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                HunterRobotAI hunter = _hunters[i];

                if (hunter == null)
                    continue;

                switch (i)
                {
                    case 0:
                        hunter.SetTacticalRole(HunterRole.FlankerLeft);
                        break;

                    case 1:
                        hunter.SetTacticalRole(HunterRole.FlankerRight);
                        break;

                    case 2:
                        hunter.SetTacticalRole(HunterRole.Interceptor);
                        break;

                    case 3:
                        hunter.SetTacticalRole(HunterRole.Pursuer);
                        break;

                    default:
                        hunter.SetTacticalRole(
                            HunterRole.Encircler,
                            i - 4);
                        break;
                }
            }
        }

        #endregion

        #region Player Motion

        private void UpdatePlayerMotion(float deltaTime)
        {
            if (deltaTime <= 0f || _playerTarget == null)
                return;

            Transform playerTransform = _playerTarget.Transform;

            Vector3 currentPosition = playerTransform.position;
            currentPosition.y = 0f;

            if (!_hasLastPlayerPosition)
            {
                _lastPlayerPosition = currentPosition;
                _hasLastPlayerPosition = true;
                return;
            }

            Vector3 displacement =
                currentPosition - _lastPlayerPosition;

            Vector3 measuredVelocity = displacement / deltaTime;
            measuredVelocity.y = 0f;

            measuredVelocity = Vector3.ClampMagnitude(
                measuredVelocity,
                _maximumTrackedPlayerSpeed);

            // Экспоненциальное сглаживание скорости.
            float smoothing = 1f -
                              Mathf.Exp(-_playerVelocitySmoothing * deltaTime);

            _playerVelocity = Vector3.Lerp(
                _playerVelocity,
                measuredVelocity,
                smoothing);

            _lastPlayerPosition = currentPosition;

            Vector3 desiredForward;

            if (_alignWithPlayerForward)
            {
                if (_playerVelocity.sqrMagnitude > 0.25f)
                {
                    desiredForward = _playerVelocity.normalized;
                }
                else
                {
                    desiredForward = GetPlanarDirection(
                        playerTransform.forward,
                        _formationForward);
                }
            }
            else
            {
                desiredForward = Vector3.forward;
            }

            if (_formationForward.sqrMagnitude < 0.001f)
                _formationForward = desiredForward;

            float rotationStep =
                _formationRotationSpeed * Mathf.Deg2Rad * deltaTime;

            _formationForward = Vector3.RotateTowards(
                _formationForward,
                desiredForward,
                rotationStep,
                0f);

            _formationForward.y = 0f;

            if (_formationForward.sqrMagnitude > 0.001f)
                _formationForward.Normalize();
        }

        private Vector3 GetPredictedPlayerPosition()
        {
            Vector3 prediction = _playerVelocity * _predictionTime;

            prediction = Vector3.ClampMagnitude(
                prediction,
                _maximumPredictionDistance);

            return _playerTarget.Transform.position + prediction;
        }

        private static Vector3 GetPlanarDirection(
            Vector3 direction,
            Vector3 fallback)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                fallback.y = 0f;

                if (fallback.sqrMagnitude < 0.001f)
                    return Vector3.forward;

                return fallback.normalized;
            }

            return direction.normalized;
        }

        #endregion

        #region Formation

        private void UpdateContraction(float deltaTime)
        {
            if (_contractionTimer > 0f)
            {
                _contractionTimer = Mathf.Max(
                    0f,
                    _contractionTimer - deltaTime);

                return;
            }

            float pressure = GetPressure01();

            float contractionMultiplier = Mathf.Lerp(
                1f,
                _maximumContractionSpeedMultiplier,
                pressure);

            _currentRadius = Mathf.MoveTowards(
                _currentRadius,
                _minimumRadius,
                _contractionSpeed *
                contractionMultiplier *
                deltaTime);
        }

        private void UpdateFormationTimer(float deltaTime)
        {
            _formationTimer -= deltaTime;

            if (_formationTimer > 0f)
                return;

            _formationTimer = _formationUpdateInterval;

            UpdateFormationDestinations();
        }

        private void UpdateFormationDestinations()
        {
            if (_playerTarget == null || _hunters.Count == 0)
                return;

            Vector3 center = GetPredictedPlayerPosition();

            Vector3 forward = _formationForward;

            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            Vector3 right = Vector3.Cross(
                Vector3.up,
                forward).normalized;

            float radiusProgress = Mathf.InverseLerp(
                _minimumRadius,
                _initialRadius,
                _currentRadius);

            // При сжатии стаи боковые позиции тоже сближаются.
            float flankerSideDistance = Mathf.Lerp(
                _minimumFlankerSideDistance,
                _flankerSideDistance,
                radiusProgress);

            float interceptorLeadDistance = Mathf.Lerp(
                _minimumInterceptorLeadDistance,
                _interceptorLeadDistance,
                radiusProgress);

            int encirclerCount = Mathf.Max(
                1,
                _hunters.Count - 4);

            float pressure = GetPressure01();

            float globalSpeedMultiplier = Mathf.Lerp(
                1f,
                _maximumPressureSpeedMultiplier,
                pressure);

            _reservedDestinations.Clear();

            for (int i = 0; i < _hunters.Count; i++)
            {
                HunterRobotAI hunter = _hunters[i];

                if (hunter == null || !hunter.IsHunting)
                    continue;

                float roleSpeedBonus = GetRoleSpeedBonus(hunter.Role);

                hunter.SetHuntSpeedMultiplier(
                    globalSpeedMultiplier * roleSpeedBonus);

                Vector3 idealDestination = GetIdealDestination(
                    hunter,
                    center,
                    forward,
                    right,
                    flankerSideDistance,
                    interceptorLeadDistance,
                    encirclerCount);

                if (!TryFindSpacedDestination(
                        hunter,
                        idealDestination,
                        center,
                        out Vector3 resolvedDestination))
                {
                    // Если позиция формации недоступна,
                    // охотник продолжает прямое преследование.
                    if (hunter.TrySampleFormationPosition(
                            center,
                            _navMeshSampleRadius,
                            out Vector3 fallbackPosition) &&
                        hunter.HasCompletePathTo(fallbackPosition))
                    {
                        hunter.SetFormationDestination(
                            fallbackPosition,
                            _minimumDestinationChange);

                        _reservedDestinations.Add(fallbackPosition);
                    }
                    else
                    {
                        _reservedDestinations.Add(
                            hunter.CurrentFormationDestination);
                    }

                    continue;
                }

                hunter.SetFormationDestination(
                    resolvedDestination,
                    _minimumDestinationChange);

                _reservedDestinations.Add(resolvedDestination);
            }
        }

        private Vector3 GetIdealDestination(
            HunterRobotAI hunter,
            Vector3 center,
            Vector3 forward,
            Vector3 right,
            float flankerSideDistance,
            float interceptorLeadDistance,
            int encirclerCount)
        {
            switch (hunter.Role)
            {
                case HunterRole.Pursuer:
                    // Прямое преследование прогнозируемой позиции игрока.
                    return center;

                case HunterRole.Interceptor:
                    // Перехватчик стремится оказаться перед игроком.
                    return center + forward * interceptorLeadDistance;

                case HunterRole.FlankerLeft:
                    return center +
                           forward * _flankerForwardDistance -
                           right * flankerSideDistance;

                case HunterRole.FlankerRight:
                    return center +
                           forward * _flankerForwardDistance +
                           right * flankerSideDistance;

                case HunterRole.Encircler:
                {
                    // Дополнительные охотники распределяются по внешнему кольцу.
                    // Смещение начинается с задней полусферы.
                    float angle = _startingAngle +
                                  180f +
                                  360f * hunter.RoleIndex / encirclerCount;

                    Vector3 radialDirection =
                        Quaternion.AngleAxis(angle, Vector3.up) * forward;

                    return center + radialDirection * _currentRadius;
                }

                default:
                    return center;
            }
        }

        private bool TryFindSpacedDestination(
            HunterRobotAI hunter,
            Vector3 idealDestination,
            Vector3 center,
            out Vector3 resolvedDestination)
        {
            resolvedDestination = default;

            Vector3 idealOffset = idealDestination - center;
            idealOffset.y = 0f;

            bool hasOffset = idealOffset.sqrMagnitude > 0.001f;

            int attempts = hasOffset
                ? _candidateSearchSteps
                : 1;

            float minimumSpacingSqr =
                _minimumSlotSpacing * _minimumSlotSpacing;

            for (int i = 0; i < attempts; i++)
            {
                float angle = GetCandidateAngle(i);

                Vector3 candidateOffset =
                    Quaternion.AngleAxis(angle, Vector3.up) *
                    idealOffset;

                Vector3 candidatePosition =
                    center + candidateOffset;

                candidatePosition.y = idealDestination.y;

                // Сначала ищем точку на NavMesh.
                if (!hunter.TrySampleFormationPosition(
                        candidatePosition,
                        _navMeshSampleRadius,
                        out Vector3 sampledPosition))
                {
                    continue;
                }

                // Затем проверяем, что точка не совпадает с уже выбранными.
                if (!HasEnoughDestinationSpacing(
                        sampledPosition,
                        minimumSpacingSqr))
                {
                    continue;
                }

                // И только после этого выполняем более дорогую проверку пути.
                if (!hunter.HasCompletePathTo(sampledPosition))
                    continue;

                resolvedDestination = sampledPosition;
                return true;
            }

            return false;
        }

        private float GetCandidateAngle(int candidateIndex)
        {
            if (candidateIndex == 0)
                return 0f;

            int ring = (candidateIndex + 1) / 2;

            float sign = candidateIndex % 2 == 1
                ? 1f
                : -1f;

            return sign * ring * _candidateAngleStep;
        }

        private bool HasEnoughDestinationSpacing(
            Vector3 candidate,
            float minimumSpacingSqr)
        {
            for (int i = 0; i < _reservedDestinations.Count; i++)
            {
                Vector3 delta =
                    candidate - _reservedDestinations[i];

                delta.y = 0f;

                if (delta.sqrMagnitude < minimumSpacingSqr)
                    return false;
            }

            return true;
        }

        private float GetRoleSpeedBonus(HunterRole role)
        {
            switch (role)
            {
                case HunterRole.Interceptor:
                    return _interceptorSpeedBonus;

                case HunterRole.FlankerLeft:
                case HunterRole.FlankerRight:
                    return _flankerSpeedBonus;

                case HunterRole.Pursuer:
                    return _pursuerSpeedBonus;

                default:
                    return 1f;
            }
        }

        private float GetPressure01()
        {
            if (_pressureRampDuration <= 0f)
                return 1f;

            return Mathf.Clamp01(
                _huntElapsedTime / _pressureRampDuration);
        }

        #endregion

        #region Capture

        private void UpdateCatchTimer(float deltaTime)
        {
            _catchTimer -= deltaTime;

            if (_catchTimer > 0f)
                return;

            _catchTimer = _catchCheckInterval;

            CheckForCatch();
        }

        private void CheckForCatch()
        {
            if (_deathController == null ||
                _deathController.IsDead ||
                _playerTarget == null)
            {
                return;
            }

            Vector3 playerPosition =
                _playerTarget.Transform.position;

            float catchDistanceSqr =
                _catchDistance * _catchDistance;

            for (int i = 0; i < _hunters.Count; i++)
            {
                HunterRobotAI hunter = _hunters[i];

                if (hunter == null || !hunter.IsHunting)
                    continue;

                Vector3 delta =
                    hunter.transform.position - playerPosition;

                // Проверяем расстояние только по горизонтали.
                delta.y = 0f;

                if (delta.sqrMagnitude > catchDistanceSqr)
                    continue;

                TryCatchPlayer(hunter);
                return;
            }
        }

        private void TryCatchPlayer(HunterRobotAI hunter)
        {
            if (_isHuntingStopped)
                return;

            if (_deathController == null || _deathController.IsDead)
                return;

            // Сразу блокируем повторный захват.
            _isHuntingStopped = true;

            StopRegisteredHunters();

            // Охотник, который догнал игрока, играет анимацию захвата.
            hunter?.PlayCatchAnimation();

            // Смерть и PlayerDefeatedEvent остаются ответственностью
            // существующего PlayerDeathController.
            _deathController.Die();
        }

        public void StopHunting()
        {
            if (_isHuntingStopped)
                return;

            _isHuntingStopped = true;

            StopRegisteredHunters();
        }

        private void StopRegisteredHunters()
        {
            for (int i = 0; i < _hunters.Count; i++)
            {
                if (_hunters[i] != null)
                    _hunters[i].StopHunting();
            }
        }

        #endregion

        #region Death Event

        private void TryBindDeathController()
        {
            if (_playerTarget == null)
                return;

            PlayerDeathController candidate =
                _playerTarget.DeathController;

            if (candidate != _deathController)
            {
                UnsubscribeFromDeathEvent();
                _deathController = candidate;
            }

            if (_deathController == null || _isSubscribedToDeathEvent)
                return;

            _deathController.OnPlayerDeathStarted += OnPlayerDeathStarted;
            _isSubscribedToDeathEvent = true;
        }

        private void UnsubscribeFromDeathEvent()
        {
            if (!_isSubscribedToDeathEvent || _deathController == null)
                return;

            _deathController.OnPlayerDeathStarted -= OnPlayerDeathStarted;
            _isSubscribedToDeathEvent = false;
        }

        private void OnPlayerDeathStarted()
        {
            StopHunting();
        }

        #endregion

        private void OnValidate()
        {
            _minimumRadius = Mathf.Max(0.1f, _minimumRadius);

            _initialRadius = Mathf.Max(
                _minimumRadius,
                _initialRadius);

            _contractionDelay = Mathf.Max(0f, _contractionDelay);
            _contractionSpeed = Mathf.Max(0f, _contractionSpeed);

            _minimumInterceptorLeadDistance = Mathf.Max(
                0f,
                _minimumInterceptorLeadDistance);

            _interceptorLeadDistance = Mathf.Max(
                _minimumInterceptorLeadDistance,
                _interceptorLeadDistance);

            _minimumFlankerSideDistance = Mathf.Max(
                0f,
                _minimumFlankerSideDistance);

            _flankerSideDistance = Mathf.Max(
                _minimumFlankerSideDistance,
                _flankerSideDistance);

            _formationUpdateInterval = Mathf.Max(
                0.02f,
                _formationUpdateInterval);

            _minimumDestinationChange = Mathf.Max(
                0.01f,
                _minimumDestinationChange);

            _navMeshSampleRadius = Mathf.Max(
                0.1f,
                _navMeshSampleRadius);

            _minimumSlotSpacing = Mathf.Max(
                0.1f,
                _minimumSlotSpacing);

            _candidateSearchSteps = Mathf.Clamp(
                _candidateSearchSteps,
                1,
                9);

            _candidateAngleStep = Mathf.Max(
                1f,
                _candidateAngleStep);

            _predictionTime = Mathf.Max(0f, _predictionTime);

            _maximumPredictionDistance = Mathf.Max(
                0f,
                _maximumPredictionDistance);

            _maximumTrackedPlayerSpeed = Mathf.Max(
                0.1f,
                _maximumTrackedPlayerSpeed);

            _playerVelocitySmoothing = Mathf.Max(
                0.1f,
                _playerVelocitySmoothing);

            _formationRotationSpeed = Mathf.Max(
                1f,
                _formationRotationSpeed);

            _pressureRampDuration = Mathf.Max(
                0f,
                _pressureRampDuration);

            _maximumPressureSpeedMultiplier = Mathf.Max(
                1f,
                _maximumPressureSpeedMultiplier);

            _maximumContractionSpeedMultiplier = Mathf.Max(
                1f,
                _maximumContractionSpeedMultiplier);

            _interceptorSpeedBonus = Mathf.Max(
                1f,
                _interceptorSpeedBonus);

            _flankerSpeedBonus = Mathf.Max(
                1f,
                _flankerSpeedBonus);

            _pursuerSpeedBonus = Mathf.Max(
                1f,
                _pursuerSpeedBonus);

            _catchDistance = Mathf.Max(
                0.1f,
                _catchDistance);

            _catchCheckInterval = Mathf.Max(
                0.02f,
                _catchCheckInterval);
        }

        public void Stop()
        {
            if (_isPaused)
                return;

            _isPaused = true;

            // Сбрасываем данные движения, чтобы после паузы
            // не получить резкий скачок вычисленной скорости.
            _playerVelocity = Vector3.zero;
            _hasLastPlayerPosition = false;
        }

        public void Resume()
        {
            if (!_isPaused)
                return;

            _isPaused = false;

            // Обновить позиции охотников сразу после возобновления.
            _formationTimer = 0f;
            _catchTimer = 0f;
        }
    }
}