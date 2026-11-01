using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Core.Features.Player.Interact;
using UnityEngine;

namespace _Game.Scripts.Core.Features.ThermalEquipment
{
    public class ThermalEquipmentItem : InteractableBase
    {
        [Header("Settings")]
        [SerializeField] private ThermalEquipmentData _data;

        [Header("References")]
        [SerializeField] private PlayerThermalEquipment _playerEquipment;

        private Rigidbody _rigidbody;
        private Collider _collider;

        private bool _originalIsKinematic;
        private bool _originalUseGravity;

        private Collider[] _ignoredPlayerColliders;
        private Coroutine _restoreCollisionRoutine;

        public ThermalEquipmentData Data => _data;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            if (_rigidbody != null)
            {
                _originalIsKinematic = _rigidbody.isKinematic;
                _originalUseGravity = _rigidbody.useGravity;
            }
        }

        public override void Interact(InteractionContext context)
        {
            if (_playerEquipment == null)
                return;

            _playerEquipment.TryEquip(this);
        }

        public void Attach(Transform anchor)
        {
            if (anchor == null)
                return;

            StopCollisionRestore();
            RestorePlayerCollisions();

            // Убираем остаточную физическую скорость перед надеванием.
            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
            }

            if (_collider != null)
                _collider.enabled = false;

            transform.SetParent(anchor, false);

            transform.localPosition = _data.LocalPosition;
            transform.localRotation = Quaternion.Euler(
                _data.LocalRotation);
        }

        public void Detach(
            Vector3 dropPosition,
            Collider[] playerColliders)
        {
            transform.SetParent(null, true);
            transform.position = dropPosition;

            if (_collider != null)
                _collider.enabled = true;

            // Временно исключаем столкновение с персонажем.
            IgnorePlayerCollisions(playerColliders);

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _originalIsKinematic;
                _rigidbody.useGravity = _originalUseGravity;

                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }

        private void IgnorePlayerCollisions(Collider[] playerColliders)
        {
            if (_collider == null || playerColliders == null)
                return;

            List<Collider> ignoredColliders = new();

            foreach (Collider playerCollider in playerColliders)
            {
                if (playerCollider == null ||
                    playerCollider == _collider ||
                    !playerCollider.enabled ||
                    playerCollider.isTrigger)
                {
                    continue;
                }

                Physics.IgnoreCollision(
                    _collider,
                    playerCollider,
                    true);

                ignoredColliders.Add(playerCollider);
            }

            _ignoredPlayerColliders = ignoredColliders.ToArray();

            if (_ignoredPlayerColliders.Length > 0)
            {
                _restoreCollisionRoutine = StartCoroutine(
                    RestoreCollisionsWhenSeparated());
            }
        }

        private IEnumerator RestoreCollisionsWhenSeparated()
        {
            WaitForFixedUpdate wait = new();

            while (IsOverlappingPlayer())
                yield return wait;

            RestorePlayerCollisions();
            _restoreCollisionRoutine = null;
        }

        private bool IsOverlappingPlayer()
        {
            if (_collider == null || !_collider.enabled)
                return false;

            Physics.SyncTransforms();

            foreach (Collider playerCollider in _ignoredPlayerColliders)
            {
                if (playerCollider == null ||
                    !playerCollider.enabled ||
                    !playerCollider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!_collider.bounds.Intersects(playerCollider.bounds))
                    continue;

                // Проверяем фактическое пересечение.
                if (Physics.ComputePenetration(
                        _collider,
                        _collider.transform.position,
                        _collider.transform.rotation,
                        playerCollider,
                        playerCollider.transform.position,
                        playerCollider.transform.rotation,
                        out _,
                        out _))
                {
                    return true;
                }

                // Резервная проверка для неподдерживаемых типов коллайдеров.
                return true;
            }

            return false;
        }

        private void StopCollisionRestore()
        {
            if (_restoreCollisionRoutine == null)
                return;

            StopCoroutine(_restoreCollisionRoutine);
            _restoreCollisionRoutine = null;
        }

        private void RestorePlayerCollisions()
        {
            if (_ignoredPlayerColliders == null)
                return;

            foreach (Collider playerCollider in _ignoredPlayerColliders)
            {
                if (playerCollider == null || _collider == null)
                    continue;

                Physics.IgnoreCollision(
                    _collider,
                    playerCollider,
                    false);
            }

            _ignoredPlayerColliders = null;
        }

        private void OnDisable()
        {
            StopCollisionRestore();
            RestorePlayerCollisions();
        }
    }
}
