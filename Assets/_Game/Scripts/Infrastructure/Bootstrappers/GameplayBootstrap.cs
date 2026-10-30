using System.Collections.Generic;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Features.Enemies;
using _Game.Scripts.Core.Features.Enemies.Swarm;
using _Game.Scripts.Core.Features.Player;
using _Game.Scripts.Core.Features.Player.Death;
using _Game.Scripts.Core.Features.Player.Flash;
using _Game.Scripts.Core.Features.Player.Interact;
using _Game.Scripts.Core.Features.PressedPlate;
using _Game.Scripts.Core.Features.ThermalEquipment;
using DG.Tweening.Core.Easing;
using UnityEngine;

namespace _Game.Scripts.Infrastructure.Bootstrappers
{
    public class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerMotor _player;
        [SerializeField] private PlayerThermalEquipment _thermalEquipment;
        [SerializeField] private PlayerInteractionScanner _interactionScanner;
        [SerializeField] private PlayerDeathController _playerDeathController;
        [SerializeField] private PlayerFlashController _playerFlashController;
        [SerializeField] private HunterSwarmController _hunterSwarmController;

        [SerializeField] private List<PressurePlate> pressurePlates;
        [SerializeField] private List<PressurePlateDoor> pressurePlateDoors;
        [SerializeField] private List<RobotAI> _robots;
        private void Start()
        {
            //TODO когда будет занавеска  var curtain = ServiceLocator.GetService<ILoadingCurtain>();
            var input = ServiceLocator.GetService<IInputHandler>();
            
            PlayerInit();
            
            _hunterSwarmController.Init();

            foreach (var pressurePlate in pressurePlates)
                pressurePlate.Init();
            
            foreach (var pressurePlateDoor in pressurePlateDoors)
                pressurePlateDoor.Init();

            foreach (var robot in _robots)
                robot.Init();
            
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

            if (_thermalEquipment != null)
                _thermalEquipment.Init();

            if (_interactionScanner != null)
                _interactionScanner.Init();

            if (_playerDeathController != null)
                _playerDeathController.Init();

            if (_playerFlashController != null)
                _playerFlashController.Init();
        }
    }
}