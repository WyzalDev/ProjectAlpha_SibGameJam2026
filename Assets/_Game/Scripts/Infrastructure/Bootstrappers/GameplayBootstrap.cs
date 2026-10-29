using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Features.Player;
using UnityEngine;

namespace _Game.Scripts.Infrastructure.Bootstrappers
{
    public class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _player;

        private void Start()
        {
            //TODO когда будет занавеска  var curtain = ServiceLocator.GetService<ILoadingCurtain>();
            var input = ServiceLocator.GetService<IInputHandler>();
            
            PlayerInit();

            input.Enable();
            input.SwitchToGameplayMap();
            
            //TODO поменять на корутину и когда будет занавеска await curtain.Hide(2); 
        }

        private void PlayerInit()
        {
            if (_player != null)
                _player.Init();
            else
                Debug.LogError("Could not find any player motor!");
        }
    }
}