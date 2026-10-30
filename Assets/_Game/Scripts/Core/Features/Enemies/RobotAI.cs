using System.Collections;
using _Game.Scripts.Core.Features.Enemies.Behaviours;
using _Game.Scripts.Core.Features.Enemies.Data;
using _Game.Scripts.Core.Features.Enemies.Scanner;
using _Game.Scripts.Core.Features.Player.Death;
using UnityEngine;
using UnityEngine.AI;

namespace _Game.Scripts.Core.Features.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class RobotAI : MonoBehaviour
    {
        [Header("Required references")]
        [SerializeField] private RobotData _data;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private RobotScanner _scanner;
        [SerializeField] private RobotAnimationController _animationController;
        [SerializeField] private RobotIdleBehaviour _idleBehaviour;

        [Header("Debug")]
        [SerializeField] private RobotState _state;

        private PlayerTarget _target;

        private float _stateTimer;
        private float _nextChaseUpdate;

        private Coroutine _attackRoutine;

        private bool _initialized;

        public RobotState State => _state;
        public PlayerTarget CurrentTarget => _target;
        public bool IsStunned => _state == RobotState.Stunned;

        private void Reset()
        {
            _agent = GetComponent<NavMeshAgent>();
            _scanner = GetComponent<RobotScanner>();
            _animationController = GetComponent<RobotAnimationController>();
            _idleBehaviour = GetComponent<RobotIdleBehaviour>();
        }

        private void Awake()
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            if (_scanner == null)
                _scanner = GetComponent<RobotScanner>();

            if (_animationController == null)
                _animationController = GetComponent<RobotAnimationController>();

            if (_idleBehaviour == null)
                _idleBehaviour = GetComponent<RobotIdleBehaviour>();
        }

        private void Start()
        {
            if (_data == null ||
                _agent == null ||
                _scanner == null ||
                _idleBehaviour == null)
            {
                Debug.LogError(
                    $"{nameof(RobotAI)} on {name}: assign RobotData, NavMeshAgent, RobotScanner " +
                    "and one RobotIdleBehaviour (Patrol or Worker).",
                    this);

                enabled = false;
                return;
            }

            if (!_agent.isOnNavMesh)
            {
                Debug.LogError(
                    $"{nameof(RobotAI)} on {name}: NavMeshAgent is not placed on a baked NavMesh. " +
                    "Move the robot onto the NavMesh and check the Agent Type.",
                    this);

                enabled = false;
                return;
            }

            _agent.speed = _data.MoveSpeed;
            _agent.acceleration = _data.Acceleration;
            _agent.angularSpeed = _data.AngularSpeed;
            _agent.stoppingDistance = _data.StoppingDistance;
            _agent.updateRotation = true;

            _idleBehaviour.Initialize(_agent, _animationController);

            _scanner.PlayerDetected += HandlePlayerDetected;
            _scanner.PlayerLost += HandlePlayerLost;

            _initialized = true;

            EnterIdle();
        }

        private void OnDisable()
        {
            if (_scanner != null)
            {
                _scanner.PlayerDetected -= HandlePlayerDetected;
                _scanner.PlayerLost -= HandlePlayerLost;
            }

            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }
        }

        private void OnEnable()
        {
            // Start() performs initialization and event subscription.
            // Re-enabling an already initialized robot restores subscriptions.
            if (!_initialized || _scanner == null)
                return;

            _scanner.PlayerDetected -= HandlePlayerDetected;
            _scanner.PlayerLost -= HandlePlayerLost;

            _scanner.PlayerDetected += HandlePlayerDetected;
            _scanner.PlayerLost += HandlePlayerLost;
        }

        private void Update()
        {
            if (!_initialized)
                return;

            if (_animationController != null)
                _animationController.SetMovementSpeed(_agent.velocity.magnitude);

            switch (_state)
            {
                case RobotState.Idle:
                    _idleBehaviour.TickIdle();
                    break;

                case RobotState.Chasing:
                    UpdateChasing();
                    break;

                case RobotState.Attention:
                    UpdateAttention();
                    break;

                case RobotState.Returning:
                    UpdateReturning();
                    break;

                case RobotState.Stunned:
                    UpdateStunned();
                    break;
            }
        }

        /// <summary>
        /// Public trigger for traps, EMP weapons, scripted events, etc.
        /// Calling it again while stunned refreshes the stun timer.
        /// </summary>
        public void ApplyStun()
        {
            if (!_initialized)
                return;

            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            _state = RobotState.Stunned;
            _target = null;
            _stateTimer = _data.StunDuration;

            StopAgent(clearPath: true);

            if (_animationController != null)
                _animationController.SetStunned(true);

            // Disabling scanner also clears its current target.
            // State is already Stunned, so PlayerLost will not change the state.
            _scanner.SetScanning(false);
        }

        private void HandlePlayerDetected(PlayerTarget target)
        {
            if (!_initialized ||
                target == null ||
                !target.isActiveAndEnabled ||
                _state == RobotState.Stunned ||
                _state == RobotState.Attacking)
            {
                return;
            }

            if (target.DeathController != null &&
                target.DeathController.IsDead)
            {
                return;
            }

            EnterChase(target);
        }

        private void HandlePlayerLost(PlayerTarget lostTarget)
        {
            if (!_initialized ||
                lostTarget == null ||
                _state != RobotState.Chasing ||
                _target != lostTarget)
            {
                return;
            }

            EnterAttention();
        }

        private void EnterIdle()
        {
            _state = RobotState.Idle;
            _target = null;

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = _data.MoveSpeed;
                _agent.isStopped = false;
            }

            _idleBehaviour.EnterIdle();

            _scanner.SetVisualState(RobotScannerVisualState.Idle);
            _scanner.SetScanning(true);
        }

        private void EnterChase(PlayerTarget target)
        {
            if (target == null)
                return;

            if (_state == RobotState.Idle)
                _idleBehaviour.ExitIdle();

            _state = RobotState.Chasing;
            _target = target;
            _nextChaseUpdate = 0f;

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = _data.MoveSpeed;
                _agent.isStopped = false;
            }

            _scanner.SetVisualState(RobotScannerVisualState.Chase);
            _scanner.SetScanning(true);

            UpdateChaseDestination(force: true);
        }

        private void EnterAttention()
        {
            if (_state == RobotState.Stunned ||
                _state == RobotState.Attacking)
            {
                return;
            }

            _state = RobotState.Attention;
            _target = null;

            _stateTimer = _data.GetRandomAttentionDuration();

            StopAgent(clearPath: true);

            _scanner.SetVisualState(RobotScannerVisualState.Attention);

            if (_animationController != null)
                _animationController.PlayAttention();

            // Keep scanning during attention.
            // A new detection immediately switches the robot back to Chase.
            _scanner.SetScanning(true);
        }

        private void EnterReturning()
        {
            _state = RobotState.Returning;

            _scanner.SetVisualState(RobotScannerVisualState.Attention);

            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = _data.MoveSpeed;
                _agent.isStopped = false;
            }

            _idleBehaviour.BeginReturn();
        }

        private void EnterAttacking()
        {
            if (_target == null)
                return;

            PlayerTarget attackTarget = _target;

            _state = RobotState.Attacking;

            StopAgent(clearPath: true);

            _scanner.SetScanning(false);

            Vector3 direction =
                attackTarget.transform.position - transform.position;

            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
            }

            if (_animationController != null)
                _animationController.PlayAttack();

            _attackRoutine = StartCoroutine(AttackRoutine(attackTarget));
        }

        private void UpdateChasing()
        {
            if (_target == null ||
                (_target.DeathController != null &&
                 _target.DeathController.IsDead))
            {
                EnterAttention();
                return;
            }

            Vector3 difference =
                _target.transform.position - transform.position;

            difference.y = 0f;

            if (difference.sqrMagnitude <=
                _data.AttackDistance * _data.AttackDistance)
            {
                EnterAttacking();
                return;
            }

            if (Time.time >= _nextChaseUpdate)
                UpdateChaseDestination(force: false);
        }

        private void UpdateChaseDestination(bool force)
        {
            if (_target == null ||
                _agent == null ||
                !_agent.isOnNavMesh)
            {
                return;
            }

            if (!force && Time.time < _nextChaseUpdate)
                return;

            _nextChaseUpdate =
                Time.time + _data.ChaseUpdateInterval;

            _agent.SetDestination(_target.transform.position);
        }

        private void UpdateAttention()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer > 0f)
                return;

            EnterReturning();
        }

        private void UpdateReturning()
        {
            _idleBehaviour.TickReturn();

            if (_idleBehaviour.IsReturnComplete)
                EnterIdle();
        }

        private void UpdateStunned()
        {
            _stateTimer -= Time.deltaTime;

            if (_stateTimer > 0f)
                return;

            if (_animationController != null)
                _animationController.SetStunned(false);

            // After recovering, always look around before returning to idle.
            EnterAttention();
        }

        private IEnumerator AttackRoutine(PlayerTarget target)
        {
            if (_data.AttackHitDelay > 0f)
                yield return new WaitForSeconds(_data.AttackHitDelay);

            if (target != null &&
                target.DeathController != null)
            {
                target.DeathController.Die();
            }
            else
            {
                Debug.LogError(
                    $"{nameof(RobotAI)} on {name}: target has no PlayerDeathController. " +
                    "Add one to the player and assign it on PlayerTarget.",
                    this);
            }

            _attackRoutine = null;
        }

        private void StopAgent(bool clearPath)
        {
            if (_agent == null || !_agent.isOnNavMesh)
                return;

            _agent.isStopped = true;

            if (clearPath)
                _agent.ResetPath();
        }

        private void OnDrawGizmosSelected()
        {
            if (_data == null)
                return;

            Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.9f);

            Gizmos.DrawWireSphere(
                transform.position,
                _data.AttackDistance);
        }
    }
}
