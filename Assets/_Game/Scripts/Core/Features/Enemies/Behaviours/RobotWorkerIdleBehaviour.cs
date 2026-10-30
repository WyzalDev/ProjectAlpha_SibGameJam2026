using _Game.Scripts.Core.Features.Enemies.Data;
using UnityEngine;
using UnityEngine.AI;

namespace _Game.Scripts.Core.Features.Enemies.Behaviours
{
    /// <summary>
    /// Worker idle behaviour: stays at home and periodically plays a head/scanner sweep animation.
    /// After chase it navigates back to its saved home position and rotation.
    /// </summary>
    public sealed class RobotWorkerIdleBehaviour : RobotIdleBehaviour
    {
        [Header("Home")]
        [Tooltip("Optional fixed home marker. Leave empty to capture this robot's position and rotation when the scene starts.")]
        [SerializeField] private Transform _homePoint;

        [Header("Idle animation")]
        [SerializeField] private RobotWorkerIdleData _data;

        private Vector3 _homePosition;
        private Quaternion _homeRotation;
        private float _nextScanTime;
        private bool _returnComplete;

        public override bool IsReturnComplete => _returnComplete;

        public override void Initialize(NavMeshAgent agent, RobotAnimationController animationController)
        {
            base.Initialize(agent, animationController);

            Transform home = _homePoint != null ? _homePoint : transform;
            _homePosition = home.position;
            _homeRotation = home.rotation;
        }

        public override void EnterIdle()
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
            }

            _returnComplete = false;
            ScheduleNextScan();
        }

        public override void TickIdle()
        {
            if (_data == null || AnimationController == null || Time.time < _nextScanTime)
                return;

            AnimationController.PlayScan();
            ScheduleNextScan();
        }

        public override void ExitIdle()
        {
            // No route to pause. RobotAI takes control of the agent during chase.
        }

        public override void BeginReturn()
        {
            _returnComplete = false;

            if (Agent == null || !Agent.isOnNavMesh)
                return;

            if (Vector3.Distance(Agent.transform.position, _homePosition) <= Mathf.Max(Agent.stoppingDistance, 0.15f))
            {
                CompleteReturn();
                return;
            }

            Agent.isStopped = false;
            Agent.SetDestination(_homePosition);
        }

        public override void TickReturn()
        {
            if (_returnComplete || Agent == null || !Agent.isOnNavMesh)
                return;

            if (HasReachedDestination(_homePosition))
                CompleteReturn();
        }

        private void CompleteReturn()
        {
            _returnComplete = true;

            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
                Agent.ResetPath();
                Agent.Warp(_homePosition);
            }

            transform.rotation = _homeRotation;
        }

        private void ScheduleNextScan()
        {
            if (_data == null)
            {
                _nextScanTime = float.PositiveInfinity;
                return;
            }

            _nextScanTime = Time.time + _data.GetRandomInterval() + _data.AnimationDuration;
        }
    }
}
