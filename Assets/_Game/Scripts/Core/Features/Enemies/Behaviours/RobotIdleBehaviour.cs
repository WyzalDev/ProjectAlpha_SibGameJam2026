using UnityEngine;
using UnityEngine.AI;

namespace _Game.Scripts.Core.Features.Enemies.Behaviours
{
    /// <summary>
    /// Type-specific behaviour while the robot is idle, plus how it returns after searching.
    /// </summary>
    public abstract class RobotIdleBehaviour : MonoBehaviour
    {
        protected NavMeshAgent Agent { get; private set; }
        protected RobotAnimationController AnimationController { get; private set; }

        public abstract bool IsReturnComplete { get; }

        public virtual void Initialize(NavMeshAgent agent, RobotAnimationController animationController)
        {
            Agent = agent;
            AnimationController = animationController;
        }

        public abstract void EnterIdle();
        public abstract void TickIdle();
        public abstract void ExitIdle();
        public abstract void BeginReturn();
        public abstract void TickReturn();

        protected bool HasReachedDestination(Vector3 destination, float additionalTolerance = 0f)
        {
            if (Agent == null || !Agent.isOnNavMesh || Agent.pathPending)
                return false;

            float tolerance = Mathf.Max(Agent.stoppingDistance, 0.15f) + 0.1f + additionalTolerance;
            if (Agent.hasPath && Agent.remainingDistance > tolerance)
                return false;

            return Vector3.Distance(Agent.transform.position, destination) <= tolerance;
        }
    }
}
