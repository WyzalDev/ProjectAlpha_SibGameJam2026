using System;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Features.Player;
using _Game.Scripts.Core.Features.Player.Interact;
using UnityEngine;

namespace _Game.Scripts.Core.Features.ThermalEquipment
{
    public class PlayerThermalEquipment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMotor _playerMotor;
        [SerializeField] private PlayerInteractionScanner _interactionScanner;
        [SerializeField] private Transform _equipmentAnchor;
        [SerializeField] private Transform _equipmentDropPoint;

        [Header("State")]
        [SerializeField] private float _currentHeat;

        [Header("Manual Unequip Cooldown")]
        [SerializeField, Min(0f)] private float _manualUnequipCooldown = 1f;

        public bool HasVisibilityResistance { get; private set; }
        public float CurrentHeat => _currentHeat;
        public bool IsEquipped => _currentItem != null;
        public bool IsOverheated => _isOverheated;

        public event Action OnOverheated;
        public event Action<float> OnHeatChanged;
        public event Action<bool> VisibilityResistanceChanged;

        private IInputHandler _inputHandler;

        private ThermalEquipmentItem _currentItem;
        private ThermalEquipmentData _currentData;

        private bool _isOverheated;
        private bool _isInitialized;

        private float _lastCoolingSpeed;

        private float _nextEquipAllowedTime;

        public void Init()
        {
            if (_isInitialized)
                return;

            _inputHandler = ServiceLocator.GetService<IInputHandler>();

            if (_inputHandler == null)
            {
                Debug.LogError(
                    "PlayerThermalEquipment: InputHandler not found.",
                    this);

                return;
            }

            _inputHandler.UnequipPerformed += OnUnequipPerformed;
            _isInitialized = true;
        }

        private void OnUnequipPerformed()
        {
            if (!IsEquipped)
                return;

            TryUnequip();
        }

        private void OnDestroy()
        {
            if (_inputHandler != null)
                _inputHandler.UnequipPerformed -= OnUnequipPerformed;
        }

        private void Update()
        {
            UpdateHeat();
        }

        public bool TryEquip(ThermalEquipmentItem item)
        {
            if (Time.time < _nextEquipAllowedTime)
                return false;
            
            if (item == null || IsEquipped || _isOverheated)
                return false;

            if (!CheckReferences(item))
                return false;

            _currentItem = item;
            _currentData = item.Data;

            _lastCoolingSpeed = _currentData.CoolingPerSecond;

            // Attach item to player.
            item.Attach(_equipmentAnchor);

            OnEquipped();

            return true;
        }

        private bool TryUnequip()
        {
            if (!IsEquipped)
                return false;

            Unequip(false);

            _nextEquipAllowedTime =
                Time.time + _manualUnequipCooldown;

            return true;
        }

        private bool CheckReferences(ThermalEquipmentItem item)
        {
            if (_equipmentDropPoint == null ||
                _playerMotor == null ||
                _interactionScanner == null ||
                _equipmentAnchor == null ||
                item.Data == null)
            {
                Debug.LogError(
                    "PlayerThermalEquipment: Missing references.",
                    this);

                return false;
            }

            return true;
        }

        private void UpdateHeat()
        {
            if (IsEquipped)
            {
                float newHeat = _currentHeat +
                                _currentData.HeatPerSecond * Time.deltaTime;

                newHeat = Mathf.Min(newHeat, _currentData.MaxHeat);

                SetHeat(newHeat);

                if (_currentHeat >= _currentData.MaxHeat)
                    Unequip(true);

                return;
            }

            if (_currentHeat <= 0f)
                return;

            float cooledHeat = Mathf.Max(
                0f,
                _currentHeat - _lastCoolingSpeed * Time.deltaTime);

            SetHeat(cooledHeat);

            if (_isOverheated && _currentHeat <= 0f)
                _isOverheated = false;
        }

        private void Unequip(bool overheated)
        {
            if (!IsEquipped)
                return;

            _lastCoolingSpeed = _currentData.CoolingPerSecond;

            Collider[] playerColliders =
                _playerMotor.GetComponentsInChildren<Collider>(true);

            _currentItem.Detach(
                _equipmentDropPoint.position,
                playerColliders);

            _currentItem = null;
            _currentData = null;

            OnUnequipped();

            if (overheated)
            {
                _isOverheated = true;
                HandleOverheated();
            }
        }

        private void HandleOverheated()
        {
            OnOverheated?.Invoke();

            PlayOverheatFeedback();
        }

        private void SetHeat(float value)
        {
            if (Mathf.Approximately(_currentHeat, value))
                return;

            _currentHeat = value;
            OnHeatChanged?.Invoke(_currentHeat);
        }

        private void SetVisibilityResistance(bool value)
        {
            if (HasVisibilityResistance == value)
                return;

            HasVisibilityResistance = value;
            VisibilityResistanceChanged?.Invoke(value);
        }

        private void OnValidate()
        {
            _manualUnequipCooldown = Mathf.Max(
                0f,
                _manualUnequipCooldown);
        }

        private void PlayOverheatFeedback()
        {
            // TODO:
            // Play overheating sound.
            // Play unequip animation.
            // Play overheating visual effects.
        }

        private void OnEquipped()
        {
            SetVisibilityResistance(true);

            _playerMotor.SetEquipmentMovement(
                _currentData.SpeedMultiplier,
                _currentData.MaxSpeed,
                _currentData.Acceleration,
                _currentData.Deceleration);

            _playerMotor.ResetHorizontalVelocity();
            
            _interactionScanner.SetEquipmentInteractionState(true);

            // TODO:
            // Play equip sound.
            // Play equip animation.
            // Enable equipment visual effects.
        }

        private void OnUnequipped()
        {
            SetVisibilityResistance(false);

            _playerMotor.ResetEquipmentMovement();

            _interactionScanner.SetEquipmentInteractionState(false);

            // TODO:
            // Play unequip sound.
            // Play unequip animation.
            // Disable equipment visual effects.
        }
    }
}
