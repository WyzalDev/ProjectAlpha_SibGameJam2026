using _Game.Scripts.Core.Common;
using _Game.Scripts.Core.Common.Input;
using _Game.Scripts.Core.Common.Pause;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

namespace _Game.Scripts.Core.Features.Player
{
    public class PlayerMotor : MonoBehaviour, IPausable
    {
        [Header("References")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _groundCheck;

        [Header("Settings")]
        [SerializeField] private PlayerMovementData _data;

        private const float GroundedStickSpeed = -2f;
        private const float TerminalFallSpeed = -50f;

        private IInputHandler _inputHandler;
        private IPauseController _pauseController;
        private EventInstance _stepInstance;

        private Vector3 _newMoveDirection;
        private Vector3 _horizontalVelocity;
        private Vector3 _horizontalSmoothVelocity;
        private float _verticalVelocity;
        private bool _canMove = true;

        public float SpeedMultiplier { get; set; } = 1f;
        public float GroundCheckDistance => _data.GroundCheckDistance;
        public Transform GroundCheck => _groundCheck;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Init()
        {
            _inputHandler = ServiceLocator.GetService<IInputHandler>();
            _pauseController = ServiceLocator.GetService<IPauseController>();

            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_inputHandler == null || _pauseController == null)
            {
                Debug.LogError(
                    "PlayerMotor: Input/Pause services are not registered. Start the game from Bootstrap.",
                    this);
                return;
            }

            _pauseController.Add(this);
            _inputHandler.PlayerMoveInputChanged += SetMoveDirection;
            _verticalVelocity = GroundedStickSpeed;
        }

        private void OnDestroy()
        {
            if (_inputHandler != null)
                _inputHandler.PlayerMoveInputChanged -= SetMoveDirection;

            _pauseController?.Remove(this);

            if (_stepInstance.isValid())
            {
                _stepInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                _stepInstance.release();
            }
        }

        private void Update()
        {
            if (!_canMove)
                return;

            bool wasGrounded = _controller.isGrounded;

            Vector3 inputDirection = GetInputDirection();
            Vector3 horizontalVelocity = CalculateHorizontalVelocity(inputDirection);
            float verticalVelocity = CalculateVerticalVelocity(wasGrounded);

            RotateTowardsMovement(inputDirection);

            Vector3 velocity = new Vector3(
                horizontalVelocity.x,
                verticalVelocity,
                horizontalVelocity.z);

            _controller.Move(velocity * Time.deltaTime);

            bool isGrounded = _controller.isGrounded;

            HandleLanding(wasGrounded, isGrounded);
            UpdateStepSound(horizontalVelocity, isGrounded);
        }

        private Vector3 GetInputDirection()
        {
            Vector3 forward = _cameraTransform != null
                ? Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up)
                : transform.forward;

            if (forward.sqrMagnitude < 0.0001f)
                forward = transform.forward;

            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 direction = forward * _newMoveDirection.y + right * _newMoveDirection.x;
            return Vector3.ClampMagnitude(direction, 1f);
        }

        private Vector3 CalculateHorizontalVelocity(Vector3 inputDirection)
        {
            Vector3 targetVelocity = _data.MoveSpeed * SpeedMultiplier * inputDirection;

            _horizontalVelocity = Vector3.SmoothDamp(
                _horizontalVelocity,
                targetVelocity,
                ref _horizontalSmoothVelocity,
                _data.SmoothMoveDeltaTime);

            _horizontalVelocity.y = 0f;
            _horizontalSmoothVelocity.y = 0f;

            return _horizontalVelocity;
        }

        private void RotateTowardsMovement(Vector3 inputDirection)
        {
            if (inputDirection.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _data.TurnSpeed * Time.deltaTime);
        }

        private float CalculateVerticalVelocity(bool wasGrounded)
        {
            if (wasGrounded && _verticalVelocity <= 0f)
            {
                _verticalVelocity = GroundedStickSpeed;
                return _verticalVelocity;
            }

            _verticalVelocity += _data.Gravity * Time.deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, TerminalFallSpeed);

            return _verticalVelocity;
        }

        private void HandleLanding(bool wasGrounded, bool isGrounded)
        {
            if (!isGrounded || wasGrounded)
                return;

            AddLandingSound();
        }

        private void UpdateStepSound(Vector3 horizontalVelocity, bool isGrounded)
        {
            bool isMoving = horizontalVelocity.sqrMagnitude > 0.0001f;

            if (isMoving && isGrounded)
                StartStepSound();
            else
                StopStepSound();
        }

        private void SetMoveDirection(Vector2 moveDirection)
        {
            _newMoveDirection = moveDirection;
        }

        private void AddLandingSound()
        {
            if (_data.LandingSoundEvent.IsNull)
                return;

            RuntimeManager.PlayOneShot(_data.LandingSoundEvent, transform.position);
        }

        private void StartStepSound()
        {
            if (_data.StepSoundEvent.IsNull)
                return;

            if (!_stepInstance.isValid())
            {
                _stepInstance = RuntimeManager.CreateInstance(_data.StepSoundEvent);
                _stepInstance.start();
                return;
            }

            _stepInstance.getPlaybackState(out PLAYBACK_STATE state);

            if (state != PLAYBACK_STATE.PLAYING)
                _stepInstance.start();
        }

        private void StopStepSound()
        {
            if (!_stepInstance.isValid())
                return;

            _stepInstance.getPlaybackState(out PLAYBACK_STATE state);

            if (state == PLAYBACK_STATE.PLAYING)
                _stepInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        public void StopMovementSounds()
        {
            if (_stepInstance.isValid())
                _stepInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        }

        public void Stop()
        {
            _canMove = false;
            StopMovementSounds();
        }

        public void Resume()
        {
            _canMove = true;
        }
    }
}
