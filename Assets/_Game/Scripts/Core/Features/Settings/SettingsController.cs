using System;
using _Game.Scripts.CinemachineAdditions;
using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using UnityEngine;
using UnityEngine.UI;

namespace _Game.Scripts.Core.Features.Settings
{
    public class SettingsController : MonoBehaviour
	{
		[SerializeField] private GameObject _settingsPanel;
		[SerializeField] private Button _exitButton;
		[SerializeField] private Slider _sensitivitySlider;
		[SerializeField] private Slider _musicSlider;
		[SerializeField] private Slider _sfxSlider;
		[SerializeField] private GameObject _bg;
		
		public event Action Hidden;

		private IInputHandler _inputHandler;

		public bool IsActive => _settingsPanel.activeSelf;

		private void Start()
		{
			_inputHandler = ServiceLocator.GetService<IInputHandler>();

			_sensitivitySlider.maxValue = 5f;

			_exitButton.onClick.AddListener(Hide);
			_sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
			_musicSlider.onValueChanged.AddListener(OnMusicChanged);
			_sfxSlider.onValueChanged.AddListener(OnSfxChanged);

			_settingsPanel.SetActive(false);

			_inputHandler.ExitPerformed += OnExitPressed;
		}

		private void OnDestroy()
		{
			_exitButton.onClick.RemoveListener(Hide);
			_sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
			_musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
			_sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);

			if (_inputHandler != null)
				_inputHandler.ExitPerformed -= OnExitPressed;
		}

		private void OnExitPressed()
		{
			if (_settingsPanel.activeSelf)
				Hide();
		}

		public void Show()
		{
			_settingsPanel.SetActive(true);
			_sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
			_musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.6f);
			_sfxSlider.value = PlayerPrefs.GetFloat("SfxVolume", 1f);
			
			if (_bg != null)
				_bg.SetActive(true);
		}

		public void Hide()
		{
			_settingsPanel.SetActive(false);
			
			if (_bg != null)
				_bg.SetActive(false);
			
			Hidden?.Invoke();
		}

		private void OnSensitivityChanged(float value)
		{
			PlayerPrefs.SetFloat("MouseSensitivity", value);
			PlayerPrefs.Save();

			var orbitInput = FindFirstObjectByType<CinemachineOrbitInput>();
			if (orbitInput != null)
				orbitInput.Sensitivity = value;
		}

		private void OnMusicChanged(float value)
		{
			PlayerPrefs.SetFloat("MusicVolume", value);
		}

		private void OnSfxChanged(float value)
		{
			PlayerPrefs.SetFloat("SfxVolume", value);
		}
	}
}