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

            // Disable physics.
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
            }

            if (_collider != null)
                _collider.enabled = false;

            // Attach to player.
            transform.SetParent(anchor, false);

            transform.localPosition = _data.LocalPosition;
            transform.localRotation = Quaternion.Euler(
                _data.LocalRotation);
        }

        public void Detach(Vector3 dropPosition)
        {
            transform.SetParent(null, true);

            transform.position = dropPosition;

            // Restore physics.
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = _originalIsKinematic;
                _rigidbody.useGravity = _originalUseGravity;
            }

            if (_collider != null)
                _collider.enabled = true;
        }
    }
}