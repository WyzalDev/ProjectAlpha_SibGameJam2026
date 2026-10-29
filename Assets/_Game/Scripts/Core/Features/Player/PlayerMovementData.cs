using FMODUnity;
using UnityEngine;

namespace _Game.Scripts.Core.Features.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementData", menuName = "Configs/Player/PlayerMovementData")]
    public class PlayerMovementData : ScriptableObject
    {
        [field: SerializeField] public float MoveSpeed { get; private set; } = 15f;
        [field: SerializeField] public float TurnSpeed { get; private set; } = 720f;
        [field: SerializeField] public float SmoothMoveDeltaTime { get; private set; } = 0.1f;

        [field: SerializeField, Tooltip("Отрицательное число")]
        public float Gravity { get; private set; } = -16f;

        [field: SerializeField] public float GroundCheckDistance { get; private set; } = 2f;
        [field: SerializeField] public EventReference StepSoundEvent { get; private set; }
        [field: SerializeField] public EventReference LandingSoundEvent { get; private set; }
    }
}
