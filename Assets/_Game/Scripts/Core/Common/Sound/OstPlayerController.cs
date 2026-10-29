using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace _Game.Scripts.Core.Common.Sound
{
    public class OstPlayerController : MonoBehaviour
    {
        [SerializeField] private EventReference _ostReference;
		
        private ISoundService _soundService;

        private EventInstance _currentOstInstance;

        public void Awake()
        {
            _soundService = ServiceLocator.GetService<ISoundService>();
        }

        private void OnDestroy()
        {
            StopCurrentOst();
        }

        /// <summary>
        /// Меняем Ost звук. Старый плавно затухает, новый плавно нарастает
        /// </summary>
        /// <param name="type">Ost который нужно запустить</param>
        public void ChangeOst(EOstType type)
        {
            if (_currentOstInstance.isValid())
                _soundService.StopSound(_currentOstInstance, STOP_MODE.ALLOWFADEOUT);

            _currentOstInstance = _soundService.PlaySound(_ostReference);
            _currentOstInstance.setParameterByName("OSTChange", (float)type);
        }

        public void SetParameter(string key, int value)
        {
            if (_currentOstInstance.isValid())
                _currentOstInstance.setParameterByName(key, value);
        }
		
        public void StopCurrentOst()
        {
            if (!_currentOstInstance.isValid())
            {
                return;
            }
			
            _soundService.StopSound(_currentOstInstance, STOP_MODE.ALLOWFADEOUT);
            _currentOstInstance = default;
        }

        public bool IsOstPlaying()
        {
            return _currentOstInstance.isValid();
        }
    }
}