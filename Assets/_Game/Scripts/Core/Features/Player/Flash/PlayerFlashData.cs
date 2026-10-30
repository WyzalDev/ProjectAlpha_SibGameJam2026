using UnityEngine;

namespace _Game.Scripts.Core.Features.Player.Flash
{
    [CreateAssetMenu(
        fileName = "PlayerFlashData",
        menuName = "Configs/Player/Flash Data")]
    public sealed class PlayerFlashData : ScriptableObject
    {
        [Header("Flash appearance")]
        [SerializeField] private Color _flashColor = Color.white;

        [Min(0.1f)]
        [SerializeField] private float _range = 12f;

        [Range(1f, 179f)]
        [SerializeField] private float _areaAngle = 70f;

        [Min(0f)]
        [SerializeField] private float _maxIntensity = 10f;

        [Header("Flash timing")]
        [Min(0.01f)]
        [SerializeField] private float _duration = 0.1f;

        [Min(0f)]
        [SerializeField] private float _fadeOutDuration = 0.4f;

        [SerializeField]
        private AnimationCurve _fadeOutCurve =
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Cooldown")]
        [Min(0f)]
        [SerializeField] private float _cooldown = 3f;

        public Color FlashColor => _flashColor;
        public float Range => _range;
        public float AreaAngle => _areaAngle;
        public float MaxIntensity => _maxIntensity;
        public float Duration => _duration;
        public float FadeOutDuration => _fadeOutDuration;
        public AnimationCurve FadeOutCurve => _fadeOutCurve;
        public float Cooldown => _cooldown;

        private void OnValidate()
        {
            _range = Mathf.Max(0.1f, _range);
            _areaAngle = Mathf.Clamp(_areaAngle, 1f, 179f);
            _maxIntensity = Mathf.Max(0f, _maxIntensity);

            _duration = Mathf.Max(0.01f, _duration);
            _fadeOutDuration = Mathf.Max(0f, _fadeOutDuration);
            _cooldown = Mathf.Max(0f, _cooldown);

            if (_fadeOutCurve == null || _fadeOutCurve.length == 0)
            {
                _fadeOutCurve =
                    AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            }
        }
    }
}