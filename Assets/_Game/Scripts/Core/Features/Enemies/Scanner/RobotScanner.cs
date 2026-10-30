using System;
using _Game.Scripts.Core.Features.Player.Death;
using _Game.Scripts.Core.Features.ThermalEquipment;
using UnityEngine;

namespace _Game.Scripts.Core.Features.Enemies.Scanner
{
    /// <summary>
    /// Performs periodic player detection using a spherical-sector field of view.
    /// Put this component on the robot root and assign Scanner Origin to the head-bone child.
    /// </summary>
    public sealed class RobotScanner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _scannerOrigin;
        [SerializeField] private RobotScannerView _view;

        [Header("Detection")]
        [SerializeField] private LayerMask _playerMask;
        [SerializeField] private LayerMask _obstacleMask;

        [Min(8)]
        [SerializeField] private int _overlapBufferSize = 64;

        [Tooltip("Optional fallback reference. Normally equipment is found through PlayerTarget.")]
        [SerializeField] private PlayerThermalEquipment _thermalEquipment;

        private Collider[] _overlapBuffer;

        private PlayerTarget _currentTarget;
        private PlayerTarget _cachedEquipmentTarget;
        private PlayerThermalEquipment _cachedEquipment;
        private PlayerThermalEquipment _subscribedEquipment;

        private bool _isScanning;
        private float _scanTimer;

        public event Action<PlayerTarget> PlayerDetected;
        public event Action<PlayerTarget> PlayerLost;

        public PlayerTarget CurrentTarget => _currentTarget;
        public bool IsScanning => _isScanning;

        private RobotScannerData Data => _view != null ? _view.Data : null;

        private Transform Origin =>
            _scannerOrigin != null ? _scannerOrigin : transform;

        private void Awake()
        {
            if (_view == null)
                _view = GetComponentInChildren<RobotScannerView>(true);

            _overlapBufferSize = Mathf.Max(8, _overlapBufferSize);
            _overlapBuffer = new Collider[_overlapBufferSize];

            _isScanning = false;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEquipment();
        }

        private void Update()
        {
            if (!_isScanning || Data == null)
                return;

            _scanTimer += Time.deltaTime;

            if (_scanTimer < Data.ScanInterval)
                return;

            _scanTimer = 0f;
            ScanNow();
        }

        public void SetVisualState(RobotScannerVisualState state)
        {
            if (_view != null)
                _view.SetVisualState(state);
        }

        public void SetScanning(bool enabled)
        {
            if (_isScanning == enabled)
            {
                if (enabled)
                {
                    _scanTimer = 0f;
                    ScanNow();
                }

                return;
            }

            _isScanning = enabled;
            _scanTimer = 0f;

            if (_view != null)
                _view.SetVisible(enabled);

            if (!enabled)
            {
                ClearCurrentTarget();
                return;
            }

            // Scan immediately on activation.
            ScanNow();
        }

        private void ScanNow()
        {
            if (!_isScanning || Data == null || _overlapBuffer == null)
                return;

            int hitCount = Physics.OverlapSphereNonAlloc(
                Origin.position,
                Data.Range,
                _overlapBuffer,
                _playerMask,
                QueryTriggerInteraction.Collide);

            PlayerTarget bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _overlapBuffer[i];

                if (hit == null)
                    continue;

                PlayerTarget candidate =
                    hit.GetComponentInParent<PlayerTarget>();

                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;

                if (candidate.DeathController != null &&
                    candidate.DeathController.IsDead)
                {
                    continue;
                }

                // Thermal equipment makes the player invisible to robot scanners.
                if (!CanDetectPlayer(candidate))
                    continue;

                Transform aimPoint = candidate.AimPoint != null
                    ? candidate.AimPoint
                    : candidate.transform;

                Vector3 toTarget = aimPoint.position - Origin.position;
                float distance = toTarget.magnitude;

                if (distance > Data.Range ||
                    distance <= 0.001f ||
                    distance >= bestDistance)
                {
                    continue;
                }

                Vector3 direction = toTarget / distance;

                if (!IsInsideFieldOfView(direction))
                    continue;

                if (IsBlocked(direction, distance, candidate))
                    continue;

                bestTarget = candidate;
                bestDistance = distance;
            }

            UpdateCurrentTarget(bestTarget);

            // Clear references so stale colliders are not retained between scans.
            for (int i = 0; i < hitCount; i++)
                _overlapBuffer[i] = null;
        }

        private bool CanDetectPlayer(PlayerTarget candidate)
        {
            if (candidate == null)
                return false;

            PlayerThermalEquipment equipment = GetEquipment(candidate);

            return equipment == null || !equipment.HasVisibilityResistance;
        }

        private PlayerThermalEquipment GetEquipment(PlayerTarget candidate)
        {
            if (_cachedEquipmentTarget == candidate)
                return _cachedEquipment != null
                    ? _cachedEquipment
                    : _thermalEquipment;

            _cachedEquipmentTarget = candidate;

            _cachedEquipment =
                candidate.GetComponentInParent<PlayerThermalEquipment>();

            if (_cachedEquipment == null)
                _cachedEquipment = _thermalEquipment;

            return _cachedEquipment;
        }

        private bool IsInsideFieldOfView(Vector3 worldDirection)
        {
            Vector3 localDirection =
                Origin.InverseTransformDirection(worldDirection.normalized);

            float horizontalAngle =
                Mathf.Atan2(localDirection.x, localDirection.z) *
                Mathf.Rad2Deg;

            float horizontalMagnitude = Mathf.Sqrt(
                localDirection.x * localDirection.x +
                localDirection.z * localDirection.z);

            float verticalAngle =
                Mathf.Atan2(localDirection.y, horizontalMagnitude) *
                Mathf.Rad2Deg;

            return Mathf.Abs(horizontalAngle) <= Data.HorizontalAngle * 0.5f &&
                   Mathf.Abs(verticalAngle) <= Data.VerticalAngle * 0.5f;
        }

        private bool IsBlocked(
            Vector3 direction,
            float distance,
            PlayerTarget candidate)
        {
            if (_obstacleMask.value == 0)
                return false;

            if (!Physics.Raycast(
                    Origin.position,
                    direction,
                    out RaycastHit hit,
                    distance,
                    _obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // The obstacle mask should normally exclude the player layer.
            // This check also tolerates accidentally including player colliders.
            return hit.transform.GetComponentInParent<PlayerTarget>() != candidate;
        }

        private void UpdateCurrentTarget(PlayerTarget newTarget)
        {
            if (_currentTarget == newTarget)
                return;

            PlayerTarget previous = _currentTarget;
            _currentTarget = newTarget;

            UpdateEquipmentSubscription(newTarget);

            if (previous != null)
                PlayerLost?.Invoke(previous);

            if (newTarget != null)
                PlayerDetected?.Invoke(newTarget);
        }

        private void UpdateEquipmentSubscription(PlayerTarget target)
        {
            UnsubscribeFromEquipment();

            if (target == null)
                return;

            PlayerThermalEquipment equipment = GetEquipment(target);

            if (equipment == null)
                return;

            _subscribedEquipment = equipment;
            _subscribedEquipment.VisibilityResistanceChanged +=
                HandleVisibilityResistanceChanged;
        }

        private void HandleVisibilityResistanceChanged(bool hasResistance)
        {
            if (!_isScanning)
                return;

            // Recheck immediately when equipment is equipped or unequipped.
            _scanTimer = 0f;
            ScanNow();
        }

        private void UnsubscribeFromEquipment()
        {
            if (_subscribedEquipment != null)
            {
                _subscribedEquipment.VisibilityResistanceChanged -=
                    HandleVisibilityResistanceChanged;
            }

            _subscribedEquipment = null;
        }

        private void ClearCurrentTarget()
        {
            if (_currentTarget == null)
            {
                UnsubscribeFromEquipment();
                return;
            }

            PlayerTarget previous = _currentTarget;

            _currentTarget = null;

            UnsubscribeFromEquipment();

            PlayerLost?.Invoke(previous);
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin =
                _scannerOrigin != null ? _scannerOrigin : transform;

            if (Data == null)
                return;

            Gizmos.color = new Color(0.1f, 0.7f, 1f, 0.7f);
            Gizmos.DrawWireSphere(origin.position, Data.Range);
        }
    }
}
