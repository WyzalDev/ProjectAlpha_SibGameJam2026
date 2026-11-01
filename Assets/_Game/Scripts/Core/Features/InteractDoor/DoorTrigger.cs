using UnityEngine;

namespace _Game.Scripts.Core.Features.InteractDoor
{
    public class DoorTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InteractableDoor _door;

        [Header("Settings")]
        [SerializeField] private string _playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag))
                return;

            _door?.Close();
        }
    }
}