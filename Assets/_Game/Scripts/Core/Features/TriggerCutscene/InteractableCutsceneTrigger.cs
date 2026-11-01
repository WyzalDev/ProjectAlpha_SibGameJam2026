using System.Collections;
using System.Collections.Generic;
using _Game.Scripts.Core.Features.InteractDoor;
using _Game.Scripts.Core.Features.Player.Interact;
using UnityEngine;
using UnityEngine.Playables;

namespace _Game.Scripts.Core.Features.TriggerCutscene
{
    public class InteractableCutsceneTrigger : InteractableBase
    {
        [Header("References")]
        [SerializeField] private PlayableDirector _director;

        [Header("Doors")]
        [SerializeField] private List<InteractableDoor> _doors = new();
        
        [Header("Activate/Deactivate")]
        [SerializeField] private List<GameObject> _activateObjects = new();
        [SerializeField] private List<GameObject> _deactivateObjects = new();

        private bool _isCutscenePlaying;

        protected override void Awake()
        {
            base.Awake();

            if (_director == null)
            {
                Debug.LogError(
                    $"{nameof(InteractableCutsceneTrigger)}: Director is not assigned.",
                    this);
            
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_director != null)
                _director.stopped += OnCutsceneStopped;
        }

        private void OnDisable()
        {
            if (_director != null)
                _director.stopped -= OnCutsceneStopped;
        }

        public override void Interact(InteractionContext context)
        {
            base.Interact(context);

            if (_director == null || _isCutscenePlaying)
                return;

            _isCutscenePlaying = true;
            _director.Play();
            StartCoroutine(DeactivateObjectsCoroutine());
        }

        private IEnumerator DeactivateObjectsCoroutine()
        {
            foreach (var deactivateObject in _deactivateObjects)
            {
                deactivateObject.SetActive(false);
                yield return null;
            }
        }

        private void OnCutsceneStopped(PlayableDirector director)
        {
            if (director != _director)
                return;

            _isCutscenePlaying = false;

            OpenDoors();
        }

        private void OpenDoors()
        {
            foreach (InteractableDoor door in _doors)
            {
                if (door == null)
                    continue;

                door.Open();
            }

            foreach (var gObject in _activateObjects)
                gObject.SetActive(true);
        }

        private void OnValidate()
        {
            if (_doors == null)
                return;

            _doors.RemoveAll(door => door == null);
        }
    }
}