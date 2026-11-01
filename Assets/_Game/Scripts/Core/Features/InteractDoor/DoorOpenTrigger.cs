using System.Collections.Generic;
using UnityEngine;

namespace _Game.Scripts.Core.Features.InteractDoor
{
    public class DoorOpenTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private List<InteractableDoor> _doors;
        [SerializeField] private List<GameObject> _activateObjects;

        [Header("Settings")]
        [SerializeField] private string _playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag))
                return;

            if (_doors == null)
            {
                Debug.LogWarning(
                    $"{nameof(DoorOpenTrigger)}: Door is not assigned.",
                    this);

                return;
            }

            foreach (var door in _doors)
                door.Open();

            foreach (var activateObject in _activateObjects)
                activateObject.SetActive(true);
        }
    }
}