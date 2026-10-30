using UnityEngine;

namespace _Game.Scripts.Core.Features.Enemies.Behaviours
{
    /// <summary>
    /// Patrol idle behaviour. The current waypoint index is preserved when chase interrupts
    /// the route; after attention the robot returns to that waypoint, then continues forward.
    /// </summary>
    public sealed class RobotPatrolBehaviour : RobotIdleBehaviour
    {
        [Header("Patrol route")]
        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _pingPong;
        [SerializeField] private float _arrivalTolerance = 0.2f;

        private int _currentPointIndex = -1;
        private int _direction = 1;
        private Vector3 _currentDestination;
        private bool _hasStarted;
        private bool _returnComplete;

        public override bool IsReturnComplete => _returnComplete;

        public override void EnterIdle()
        {
            if (Agent == null || !Agent.isOnNavMesh)
                return;

            if (_patrolPoints == null || _patrolPoints.Length == 0)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
                return;
            }

            if (!_hasStarted)
            {
                _currentPointIndex = FindFirstValidPoint();
                _hasStarted = true;
            }
            else if (_returnComplete)
            {
                _returnComplete = false;
                if (!AdvanceToNextPoint())
                {
                    Agent.isStopped = true;
                    Agent.ResetPath();
                    return;
                }
            }

            MoveToCurrentPoint();
        }

        public override void TickIdle()
        {
            if (Agent == null || !Agent.isOnNavMesh || _currentPointIndex < 0)
                return;

            if (HasReachedDestination(_currentDestination, _arrivalTolerance))
            {
                if (!AdvanceToNextPoint())
                {
                    Agent.isStopped = true;
                    Agent.ResetPath();
                    return;
                }

                MoveToCurrentPoint();
            }
        }

        public override void ExitIdle()
        {
            // RobotAI stops the NavMeshAgent when changing state. Keep the route index intact.
        }

        public override void BeginReturn()
        {
            _returnComplete = false;

            if (Agent == null || !Agent.isOnNavMesh)
                return;

            if (_currentPointIndex < 0 || _patrolPoints == null || _patrolPoints.Length == 0 ||
                _patrolPoints[_currentPointIndex] == null)
            {
                _returnComplete = true;
                return;
            }

            _currentDestination = _patrolPoints[_currentPointIndex].position;
            Agent.isStopped = false;
            Agent.SetDestination(_currentDestination);
        }

        public override void TickReturn()
        {
            if (_returnComplete || Agent == null || !Agent.isOnNavMesh)
                return;

            if (!HasReachedDestination(_currentDestination, _arrivalTolerance))
                return;

            _returnComplete = true;
            Agent.isStopped = true;
            Agent.ResetPath();
        }

        private int FindFirstValidPoint()
        {
            if (_patrolPoints == null)
                return -1;

            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                if (_patrolPoints[i] != null)
                    return i;
            }

            return -1;
        }

        private bool AdvanceToNextPoint()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0)
                return false;

            int previousIndex = _currentPointIndex;

            if (_pingPong && _patrolPoints.Length > 1)
            {
                int next = _currentPointIndex + _direction;
                if (next >= _patrolPoints.Length || next < 0)
                {
                    _direction *= -1;
                    next = _currentPointIndex + _direction;
                }

                _currentPointIndex = FindNextValidFrom(next, _direction);
            }
            else
            {
                int start = _currentPointIndex + 1;
                _currentPointIndex = FindNextValidFrom(start, 1);
            }

            return _currentPointIndex >= 0 && _currentPointIndex != previousIndex;
        }

        private int FindNextValidFrom(int startIndex, int direction)
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0)
                return -1;

            int count = _patrolPoints.Length;
            for (int attempt = 0; attempt < count; attempt++)
            {
                int candidate = startIndex + direction * attempt;

                if (_pingPong)
                {
                    if (candidate < 0 || candidate >= count)
                        break;
                }
                else
                {
                    candidate = (candidate % count + count) % count;
                }

                if (_patrolPoints[candidate] != null)
                    return candidate;
            }

            if (_loop && !_pingPong)
                return FindFirstValidPoint();

            // With loop disabled, stay at the last valid point.
            return _currentPointIndex;
        }

        private void MoveToCurrentPoint()
        {
            if (_currentPointIndex < 0 || _patrolPoints == null ||
                _currentPointIndex >= _patrolPoints.Length || _patrolPoints[_currentPointIndex] == null)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
                return;
            }

            _currentDestination = _patrolPoints[_currentPointIndex].position;
            Agent.isStopped = false;
            Agent.SetDestination(_currentDestination);
        }

        private void OnDrawGizmos()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0)
                return;

            Gizmos.color = new Color(0.15f, 0.9f, 0.35f, 1f);
            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                Transform point = _patrolPoints[i];
                if (point == null)
                    continue;

                Gizmos.DrawSphere(point.position, 0.16f);

                int next = i + 1;
                if (next >= _patrolPoints.Length)
                {
                    if (!_loop || _pingPong)
                        continue;
                    next = 0;
                }

                if (_patrolPoints[next] != null)
                    Gizmos.DrawLine(point.position, _patrolPoints[next].position);
            }
        }

        private void OnValidate()
        {
            _arrivalTolerance = Mathf.Max(0f, _arrivalTolerance);
        }
    }
}