using UnityEngine;
using UnityEngine.AI;

namespace _Game.Scripts.Core.Features.Enemies.Swarm
{
    public enum HunterRole
    {
        Pursuer,
        Interceptor,
        FlankerLeft,
        FlankerRight,
        Encircler
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class HunterRobotAI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HunterSwarmController _swarmController;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private RobotAnimationController _animationController;

        [Header("Movement")]
        [SerializeField] private bool _disableAutoBraking = true;

        private NavMeshPath _cachedPath;

        private int _formationSlot;
        internal Vector3 CurrentFormationDestination =>
            _hasLastDestination ? _lastDestination : transform.position;
        private int _roleIndex;

        private HunterRole _role;

        private bool _isHunting;
        private bool _hasLastDestination;

        private Vector3 _lastDestination;

        private float _baseAgentSpeed;

        public bool IsHunting => _isHunting;
        public HunterRole Role => _role;

        internal int FormationSlot => _formationSlot;
        internal int RoleIndex => _roleIndex;

        private void Awake()
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            if (_animationController == null)
            {
                _animationController =
                    GetComponentInChildren<RobotAnimationController>(true);
            }

            _cachedPath = new NavMeshPath();

            if (_agent != null)
            {
                _baseAgentSpeed = _agent.speed;

                if (_disableAutoBraking)
                    _agent.autoBraking = false;
            }
        }

        private void OnEnable()
        {
            if (_swarmController == null)
            {
                Debug.LogError(
                    $"[{name}] HunterSwarmController is not assigned.",
                    this);

                return;
            }

            _swarmController.Register(this);
        }

        private void OnDisable()
        {
            if (_swarmController != null)
                _swarmController.Unregister(this);

            StopHunting();
        }

        private void Update()
        {
            if (!_isHunting || _agent == null)
                return;

            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _animationController?.SetMovementSpeed(
                    _agent.velocity.magnitude);
            }
            else
            {
                _animationController?.SetMovementSpeed(0f);
            }
        }

        internal void SetFormationSlot(int slot)
        {
            _formationSlot = slot;

            if (_agent == null)
                return;

            // Сохраняем разные приоритеты локального обхода.
            _agent.avoidancePriority =
                30 + Mathf.Abs(_formationSlot % 60);
        }

        internal void SetTacticalRole(HunterRole role, int roleIndex = 0)
        {
            _role = role;
            _roleIndex = roleIndex;
        }

        internal void StartHunting()
        {
            _isHunting = true;
            _hasLastDestination = false;

            if (_agent == null || !_agent.isActiveAndEnabled)
                return;

            // Возвращаем базовую скорость перед началом новой охоты.
            _agent.speed = _baseAgentSpeed;

            if (!_agent.isOnNavMesh)
                return;

            _agent.isStopped = false;
            _agent.ResetPath();
        }

        internal void SetHuntSpeedMultiplier(float multiplier)
        {
            if (_agent == null)
                return;

            multiplier = Mathf.Max(0.1f, multiplier);

            _agent.speed = _baseAgentSpeed * multiplier;
        }

        /// <summary>
        /// Находит ближайшую точку NavMesh в пределах sampleRadius.
        /// Здесь пока не проверяется связность маршрута.
        /// </summary>
        internal bool TrySampleFormationPosition(
            Vector3 requestedPosition,
            float sampleRadius,
            out Vector3 sampledPosition)
        {
            sampledPosition = default;

            if (!_isHunting ||
                _agent == null ||
                !_agent.isActiveAndEnabled ||
                !_agent.isOnNavMesh)
            {
                return false;
            }

            if (!NavMesh.SamplePosition(
                    requestedPosition,
                    out NavMeshHit hit,
                    sampleRadius,
                    _agent.areaMask))
            {
                return false;
            }

            sampledPosition = hit.position;
            return true;
        }

        /// <summary>
        /// Проверяет, что агент действительно может добраться до точки.
        /// </summary>
        internal bool HasCompletePathTo(Vector3 destination)
        {
            if (!_isHunting ||
                _agent == null ||
                !_agent.isActiveAndEnabled ||
                !_agent.isOnNavMesh)
            {
                return false;
            }

            if (!_agent.CalculatePath(destination, _cachedPath))
                return false;

            return _cachedPath.status == NavMeshPathStatus.PathComplete;
        }

        /// <summary>
        /// Назначает уже проверенную позицию на NavMesh.
        /// </summary>
        internal bool SetFormationDestination(
            Vector3 destination,
            float minimumDestinationChange)
        {
            if (!_isHunting ||
                _agent == null ||
                !_agent.isActiveAndEnabled ||
                !_agent.isOnNavMesh)
            {
                return false;
            }

            float minimumChangeSqr =
                minimumDestinationChange * minimumDestinationChange;

            if (_hasLastDestination &&
                (destination - _lastDestination).sqrMagnitude <
                minimumChangeSqr)
            {
                return false;
            }

            _agent.isStopped = false;

            if (!_agent.SetDestination(destination))
                return false;

            _lastDestination = destination;
            _hasLastDestination = true;

            return true;
        }

        internal void PlayCatchAnimation()
        {
            _animationController?.PlayAttack();
        }

        public void StopHunting()
        {
            _isHunting = false;
            _hasLastDestination = false;

            if (_agent != null && _agent.isActiveAndEnabled)
            {
                if (_agent.isOnNavMesh)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                }
            }

            _animationController?.SetMovementSpeed(0f);
        }
    }
}