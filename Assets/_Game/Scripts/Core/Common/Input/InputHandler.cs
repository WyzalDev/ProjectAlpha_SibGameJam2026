using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Game.Scripts.Core.Common.Input
{
    public class InputHandler : IInputHandler, IDisposable
    {
        private InputSystemActions _input;
        private bool _initialized;

        public void Dispose()
        {
            if (_input != null)
            {
                _input.Disable();
                _input.Dispose();
                _input = null;
            }

            _initialized = false;
        }

        public event Action<Vector2> RotateInputChanged;
        public event Action QuickMenuTrigger;
        public event Action<Vector2> PlayerMoveInputChanged;
        public event Action PhotoPerformed;
        public event Action InteractPerformed;
        public event Action ExitPerformed;
        public event Action ComicsSkipPerformed;

        public InputSystemActions Input => _input ??= new InputSystemActions();

        public void Init()
        {
            if (_initialized)
                return;
            _initialized = true;

            // Gameplay Map
            Input.Gameplay.Move.performed += ctx => OnPlayerMoveInputChanged(ctx.ReadValue<Vector2>());
            Input.Gameplay.Move.canceled += ctx => OnPlayerMoveInputChanged(Vector2.zero);

            Input.Gameplay.Look.performed += ctx => OnRotateInputChanged(ctx.ReadValue<Vector2>());
            Input.Gameplay.Look.canceled += ctx => OnRotateInputChanged(Vector2.zero);
            Input.Gameplay.Interact.performed += _ => InteractPerformed?.Invoke();

            Input.Gameplay.Photo.performed += _ => PhotoPerformed?.Invoke();

            // UI Map
            Input.UI.ComicsSkip.performed += _ => ComicsSkipPerformed?.Invoke();
            Input.UI.Exit.performed += _ => ExitPerformed?.Invoke();

            // Emergency
            // Input.Emergency.QuickMenuTrigger.performed += _ => QuickMenuTrigger?.Invoke();
        }

        public void Enable()
        {
            Input.Enable();
        }

        public void Disable()
        {
            Input.Disable();
        }

        public void SwitchToGameplayMap()
        {
            Input.UI.Disable();
            Input.Gameplay.Enable();
        }

        public void SwitchToUIMap()
        {
            Input.Gameplay.Disable();
            Input.UI.Enable();
        }

        public void EnableOnlyEmergency()
        {
            Input.Gameplay.Disable();
            Input.UI.Disable();
        }

        void OnRotateInputChanged(Vector2 direction)
        {
            RotateInputChanged?.Invoke(direction);
        }

        void OnPlayerMoveInputChanged(Vector2 direction)
        {
            PlayerMoveInputChanged?.Invoke(direction);
        }

        public Vector2 GetMousePos()
        {
            return Mouse.current.position.ReadValue();
        }
    }
}