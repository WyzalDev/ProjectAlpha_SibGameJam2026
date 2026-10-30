using UnityEngine;

namespace _Game.Scripts.Core.Features.Enemies
{
    /// <summary>
    /// Small wrapper around Animator parameters. Parameter names must match the robot Animator Controller.
    /// </summary>
    public sealed class RobotAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [Header("Parameters")]
        [SerializeField] private string _moveSpeedParameter = "Speed";
        [SerializeField] private string _attentionTrigger = "Attention";
        [SerializeField] private string _attackTrigger = "Attack";
        [SerializeField] private string _stunBool = "Stunned";
        [SerializeField] private string _scanTrigger = "Scan";

        private int _moveSpeedHash;
        private int _attentionHash;
        private int _attackHash;
        private int _stunHash;
        private int _scanHash;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _moveSpeedHash = Hash(_moveSpeedParameter);
            _attentionHash = Hash(_attentionTrigger);
            _attackHash = Hash(_attackTrigger);
            _stunHash = Hash(_stunBool);
            _scanHash = Hash(_scanTrigger);
        }

        public void SetMovementSpeed(float speed)
        {
            if (_animator != null && _moveSpeedHash != 0)
                _animator.SetFloat(_moveSpeedHash, speed);
        }

        public void PlayAttention()
        {
            SetTrigger(_attentionHash);
        }

        public void PlayAttack()
        {
            SetTrigger(_attackHash);
        }

        public void PlayScan()
        {
            SetTrigger(_scanHash);
        }

        public void SetStunned(bool stunned)
        {
            if (_animator != null && _stunHash != 0)
                _animator.SetBool(_stunHash, stunned);
        }

        private void SetTrigger(int hash)
        {
            if (_animator != null && hash != 0)
                _animator.SetTrigger(hash);
        }

        private static int Hash(string parameterName)
        {
            return string.IsNullOrWhiteSpace(parameterName)
                ? 0
                : Animator.StringToHash(parameterName);
        }
    }
}
