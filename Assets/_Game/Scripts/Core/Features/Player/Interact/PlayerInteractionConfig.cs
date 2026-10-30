using UnityEngine;

namespace _Game.Scripts.Core.Features.Player.Interact
{
    [CreateAssetMenu(fileName = "PlayerInteractionConfig", menuName = "Configs/Player/PlayerInteractionConfig")]
    public class PlayerInteractionConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float InteractionRange { get; private set; } = 3f;
        [field: SerializeField, Min(0f)] public float ScanHeight { get; private set; } = 1f;
        [field: SerializeField, Min(0f)] public float ScanInterval { get; private set; } = 0.1f;
        [field: SerializeField] public bool RequireLineOfSight { get; private set; } = false;
    }
}
