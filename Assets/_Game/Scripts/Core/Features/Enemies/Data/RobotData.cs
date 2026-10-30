using UnityEngine;

namespace _Game.Scripts.Core.Features.Enemies.Data
{
    [CreateAssetMenu(menuName = "Configs/Robots/Robot Data", fileName = "RobotData")]
    public sealed class RobotData : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)] public float MoveSpeed = 3f;
        [Min(0f)] public float Acceleration = 8f;
        [Min(0f)] public float AngularSpeed = 360f;
        [Min(0f)] public float StoppingDistance = 1f;

        [Header("States")]
        [Min(0f)] public Vector2 AttentionDuration = new Vector2(1f, 2f);
        [Min(0f)] public float StunDuration = 3f;

        [Header("Chase")]
        [Min(0.01f)] public float ChaseUpdateInterval = 0.2f;
        [Min(0f)] public float AttackDistance = 1.2f;

        [Header("Attack")]
        [Min(0f)] public float AttackHitDelay = 0.35f;

        public float GetRandomAttentionDuration()
        {
            float min = Mathf.Min(AttentionDuration.x, AttentionDuration.y);
            float max = Mathf.Max(AttentionDuration.x, AttentionDuration.y);
            return Random.Range(min, max);
        }

        private void OnValidate()
        {
            MoveSpeed = Mathf.Max(0f, MoveSpeed);
            Acceleration = Mathf.Max(0f, Acceleration);
            AngularSpeed = Mathf.Max(0f, AngularSpeed);
            StoppingDistance = Mathf.Max(0f, StoppingDistance);
            AttentionDuration.x = Mathf.Max(0f, AttentionDuration.x);
            AttentionDuration.y = Mathf.Max(0f, AttentionDuration.y);
            StunDuration = Mathf.Max(0f, StunDuration);
            ChaseUpdateInterval = Mathf.Max(0.01f, ChaseUpdateInterval);
            AttackDistance = Mathf.Max(0f, AttackDistance);
            AttackHitDelay = Mathf.Max(0f, AttackHitDelay);
        }
    }
}
