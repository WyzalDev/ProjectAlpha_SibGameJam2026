using System;
using UnityEngine;

namespace _Game.Scripts.Core.Common.Input
{
    public interface IInputHandler
    {
        event Action<Vector2> RotateInputChanged;
        event Action<Vector2> PlayerMoveInputChanged;
        event Action PhotoPerformed;
        event Action InteractPerformed;
        event Action ExitPerformed;
        event Action UnequipPerformed;
        event Action ComicsSkipPerformed;

        void Init();
        void Enable();
        void Disable();

        void SwitchToGameplayMap();
        void SwitchToUIMap();

        Vector2 GetMousePos();
    }
}