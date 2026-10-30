using UnityEngine;

namespace _Game.Scripts.Core.Features.Player.Death
{
    /// <summary>
    /// Marker placed on the player's root object. Put all player colliders on a layer
    /// included in RobotScanner's Player Mask.
    /// </summary>
    public sealed class PlayerTarget : MonoBehaviour
    {
        [SerializeField] private Transform _aimPoint;
        [SerializeField] private PlayerDeathController _deathController;

        public Transform Transform => transform;
        public Transform AimPoint => _aimPoint != null ? _aimPoint : transform;
        public PlayerDeathController DeathController => _deathController;

        private void Awake()
        {
            if (_deathController == null)
                _deathController = GetComponent<PlayerDeathController>();
        }
    }
}
