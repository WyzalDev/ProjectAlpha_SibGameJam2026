using System;
using System.Collections;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Events;
using _Game.Scripts.Core.Features.Player.Interact;
using UnityEngine;

namespace _Game.Scripts.Core.Features.Player.Death
{
    public sealed class PlayerDeathController : MonoBehaviour
    {
        [Header("Death presentation")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _deathTrigger = "Death";
        [Min(0f)] [SerializeField] private float _deathSequenceDuration = 2f;
        
        [Header("References")]
        [SerializeField] private PlayerInteractionScanner _interactionScanner;
        [SerializeField] private PlayerMotor _motor;

        public event Action OnPlayerDeathStarted;
        public bool IsDead => _isDead;

        private bool _isDead;

        private EventBus _eventBus;
        private IDisposable _playerDeathEventDisposable;

        public void Init()
        {
            _eventBus = ServiceLocator.GetService<EventBus>();
        }

        public void Die()
        {
            if (_isDead)
                return;

            _isDead = true;
            _interactionScanner.Stop();
            _motor.Stop();
            OnPlayerDeathStarted?.Invoke();
            Debug.Log("PlayerDeathStarted");

            if (_animator != null && !string.IsNullOrWhiteSpace(_deathTrigger))
                _animator.SetTrigger(_deathTrigger);

            StartCoroutine(DeathSequenceRoutine());
        }

        private IEnumerator DeathSequenceRoutine()
        {
            if (_deathSequenceDuration > 0f)
                yield return new WaitForSeconds(_deathSequenceDuration);

            _eventBus.Trigger<PlayerDefeatedEvent>();
            Debug.Log("PlayerDefeatedEvent");
        }
    }
}
