using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Common.Pause;
using Unity.Cinemachine;
using UnityEngine;

namespace _Game.Scripts.CinemachineAdditions
{
    [RequireComponent(typeof(CinemachineCamera))]
    public class CinemachineOrbitInput : MonoBehaviour, IPausable
    {
        [Header("Orbit")]
        [SerializeField] private float _degreesPerPixel = 0.15f;
        [SerializeField] private bool _invertY;

        private const float MinMoveInputSqr = 0.0001f;

        private CinemachineOrbitalFollow _orbital;
        private IInputHandler _input;
        private IPauseController _pauseController;
        private float _sensitivity = 1f;
        private bool _canRotate = true;
        private bool _isRecenterFrozen;

        public float Sensitivity
        {
            get => _sensitivity;
            set => _sensitivity = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            _orbital = GetComponent<CinemachineOrbitalFollow>();

            if (_orbital == null)
                Debug.LogError($"{nameof(CinemachineOrbitInput)}: CinemachineOrbitalFollow not found.", this);
        }

        private void Start()
        {
            _sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);

            _input = ServiceLocator.GetService<IInputHandler>();
            if (_input != null)
            {
                _input.RotateInputChanged += OnLook;
                _input.PlayerMoveInputChanged += OnMoveInputChanged;
            }

            _pauseController = ServiceLocator.GetService<IPauseController>();
            _pauseController?.Add(this);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.RotateInputChanged -= OnLook;
                _input.PlayerMoveInputChanged -= OnMoveInputChanged;
            }

            _pauseController?.Remove(this);
        }

        private void OnMoveInputChanged(Vector2 moveDirection)
        {
            bool hasMovement = moveDirection.sqrMagnitude > MinMoveInputSqr;

            if (_isRecenterFrozen == hasMovement)
                return;

            if (_orbital == null)
                return;

            _isRecenterFrozen = hasMovement;

            // Рекентеринг центра орбиты динамически следует за игроком, поэтому
            // при движении (особенно назад) он раскачивает камеру — замораживаем его,
            // а после остановки вновь включаем, чтобы камера вернулась «за спину».
            var recentering = _orbital.HorizontalAxis.Recentering;
            recentering.Enabled = hasMovement == false;
            _orbital.HorizontalAxis.Recentering = recentering;

            if (hasMovement)
                _orbital.HorizontalAxis.CancelRecentering();
        }

        private void OnLook(Vector2 delta)
        {
            if (!_canRotate || _orbital == null)
                return;

            float yawDelta = delta.x * _degreesPerPixel * _sensitivity;
            float pitchDelta = delta.y * _degreesPerPixel * _sensitivity;

            if (_invertY)
                pitchDelta = -pitchDelta;

            _orbital.HorizontalAxis.Value = _orbital.HorizontalAxis.ClampValue(
                _orbital.HorizontalAxis.Value + yawDelta);

            _orbital.VerticalAxis.Value = _orbital.VerticalAxis.ClampValue(
                _orbital.VerticalAxis.Value - pitchDelta);
        }

        public void Stop() => _canRotate = false;
        public void Resume() => _canRotate = true;
    }
}