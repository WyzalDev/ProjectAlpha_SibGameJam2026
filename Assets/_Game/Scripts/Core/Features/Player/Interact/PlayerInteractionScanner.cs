using System.Collections.Generic;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Common.Pause;
using _Game.Scripts.Core.UI.Interact;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Game.Scripts.Core.Features.Player.Interact
{
    public class PlayerInteractionScanner : MonoBehaviour, IPausable
    {
        [Header("References")]
        [SerializeField] private PlayerInteractionConfig _data;
        [SerializeField] private InteractionPromptView _prompt;

        [Header("Settings")]
        [SerializeField] private LayerMask _scanMask = ~0;

        private const int InitialBufferSize = 32;
        private const int MaxBufferSize = 256;
        private const int HitBufferSize = 16;
        private const float MinRayDistance = 0.001f;

        private IInputHandler _inputHandler;
        private IPauseController _pauseController;

        private Collider[] _colliders = new Collider[InitialBufferSize];
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];
        private readonly HashSet<IHoverable> _unique = new HashSet<IHoverable>();
        private readonly List<IHoverable> _candidates = new List<IHoverable>();

        private IHoverable _currentTarget;
        private InteractionContext _context;
        private float _scanTimer;
        private bool _canScan = true;
        private bool _canInteract = true;

        public void Init()
        {
            _inputHandler = ServiceLocator.GetService<IInputHandler>();
            _pauseController = ServiceLocator.GetService<IPauseController>();

            if (_data == null)
            {
                Debug.LogError("PlayerInteractionScanner: Interaction config is not assigned.", this);
                return;
            }

            _context = new InteractionContext(gameObject, _prompt);
            _pauseController?.Add(this);
            _inputHandler.InteractPerformed += HandleInteractPerformed;
        }

        private void OnDestroy()
        {
            if (_inputHandler != null)
                _inputHandler.InteractPerformed -= HandleInteractPerformed;

            _pauseController?.Remove(this);
        }

        private void Update()
        {
            if (!_canScan || _data == null)
                return;

            _scanTimer += Time.deltaTime;

            if (!(_scanTimer >= _data.ScanInterval))
                return;

            _scanTimer = 0f;
            Scan();
        }

        private void Scan()
        {
            var origin = transform.position + Vector3.up * _data.ScanHeight;
            var target = FindNearest(origin);

            if (_currentTarget != target)
                ChangeTarget(target);
        }

        private IHoverable FindNearest(Vector3 origin)
        {
            FillCandidates(origin);

            SortCandidatesByDistance(origin);

            // Берём первый по дистанции объект, который проходит проверку линии видимости.
            // Если ближайший скрыт стеной — берём следующий за ним видимый.
            foreach (var candidate in _candidates)
            {
                if (_data.RequireLineOfSight == false || HasLineOfSight(candidate, origin))
                    return candidate;
            }

            return null;
        }

        private void SortCandidatesByDistance(Vector3 origin)
        {
            // Список кандидатов обычно короткий, поэтому простая сортировка вставками
            // без лишних аллокаций.
            for (int i = 1; i < _candidates.Count; i++)
            {
                var candidate = _candidates[i];
                var distance = GetDistanceTo(candidate, origin);
                int j = i - 1;

                while (j >= 0 && GetDistanceTo(_candidates[j], origin) > distance)
                {
                    _candidates[j + 1] = _candidates[j];
                    j--;
                }

                _candidates[j + 1] = candidate;
            }
        }

        private void FillCandidates(Vector3 origin)
        {
            _unique.Clear();
            _candidates.Clear();

            var count = Physics.OverlapSphereNonAlloc(
                origin,
                _data.InteractionRange,
                _colliders,
                _scanMask,
                QueryTriggerInteraction.Collide);

            if (count > 0 && count == _colliders.Length && _colliders.Length < MaxBufferSize)
            {
                _colliders = new Collider[_colliders.Length * 2];
                count = Physics.OverlapSphereNonAlloc(
                    origin,
                    _data.InteractionRange,
                    _colliders,
                    _scanMask,
                    QueryTriggerInteraction.Collide);
            }

            for (var i = 0; i < count; i++)
            {
                var hoverable = _colliders[i] != null
                    ? _colliders[i].GetComponentInParent<IHoverable>()
                    : null;

                if (!IsValid(hoverable))
                    continue;

                if (_unique.Add(hoverable))
                    _candidates.Add(hoverable);
            }
        }

        private float GetDistanceTo(IHoverable candidate, Vector3 origin)
        {
            if (candidate is Component component &&
                component.TryGetComponent<Collider>(out var ccollider))
                return Vector3.Distance(origin, ccollider.ClosestPoint(origin));

            return float.MaxValue;
        }

        private bool HasLineOfSight(IHoverable target, Vector3 origin)
        {
            if (target is not Component component || component.TryGetComponent(out Collider collider) == false)
                return true;

            Vector3 closestPoint = collider.ClosestPoint(origin);
            Vector3 direction = closestPoint - origin;
            float distance = direction.magnitude;

            if (distance <= MinRayDistance)
                return true;

            direction /= distance;

            int hitCount = Physics.RaycastNonAlloc(
                origin,
                direction,
                _hits,
                distance,
                _scanMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _hits[i].collider;

                if (hitCollider.transform.IsChildOf(component.transform))
                    continue;

                if (hitCollider.transform.IsChildOf(transform))
                    continue;

                return false;
            }

            return true;
        }

        private void ChangeTarget(IHoverable newTarget)
        {
            if (IsValid(_currentTarget) && _currentTarget != newTarget)
                _currentTarget.OnHoverExit(_context);

            _currentTarget = newTarget;
            _currentTarget?.OnHoverEnter(_context);
        }

        private void HandleInteractPerformed()
        {
            if (!_canInteract)
                return;

            if (!IsValid(_currentTarget))
                return;

            if (_currentTarget is IInteractable interactable)
                interactable.Interact(_context);
        }

        private static bool IsValid(IHoverable target)
        {
            return target != null && (target is not Object obj || obj != null);
        }

        public void Stop()
        {
            _canScan = false;
            _canInteract = false;
        }

        public void Resume()
        {
            _canScan = true;
            _canInteract = true;
            _scanTimer = 0f;

            Scan();
        }
    }
}
