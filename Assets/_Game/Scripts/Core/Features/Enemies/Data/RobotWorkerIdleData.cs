using UnityEngine;

namespace _Game.Scripts.Core.Features.Enemies.Data
{
    [CreateAssetMenu(menuName = "Configs/Robots/Worker Idle Data", fileName = "RobotWorkerIdleData")]
    public sealed class RobotWorkerIdleData : ScriptableObject
    {
        [Header("Scanner animation")]
        [Min(0f)] public Vector2 AnimationInterval = new Vector2(3f, 6f);
        [Min(0f)] public float AnimationDuration = 1.2f;

        public float GetRandomInterval()
        {
            float min = Mathf.Min(AnimationInterval.x, AnimationInterval.y);
            float max = Mathf.Max(AnimationInterval.x, AnimationInterval.y);
            return Random.Range(min, max);
        }

        private void OnValidate()
        {
            AnimationInterval.x = Mathf.Max(0f, AnimationInterval.x);
            AnimationInterval.y = Mathf.Max(0f, AnimationInterval.y);
            AnimationDuration = Mathf.Max(0f, AnimationDuration);
        }
    }
}
