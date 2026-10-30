using System;
using UnityEditor;
using UnityEngine;

namespace _Game.Scripts.Core.Features.Enemies.Scanner
{
    [CreateAssetMenu(menuName = "Configs/Robots/Scanner Data", fileName = "RobotScannerData")]
    public sealed class RobotScannerData : ScriptableObject
    {
        [Header("Detection")]
        [Min(0.05f)] public float ScanInterval = 0.2f;
        [Min(0f)] public float Range = 8f;
        [Range(1f, 360f)] public float HorizontalAngle = 90f;
        [Range(1f, 180f)] public float VerticalAngle = 60f;

        [Header("Visual")]
        [Range(4, 64)] public int HorizontalSegments = 32;
        [Range(2, 32)] public int VerticalSegments = 16;
        [Range(1, 16)] public int RadialSegments = 4;
        [Range(0f, 1f)] public float VisualAlpha = 0.2f;
        public Color IdleColor = new Color(0.1f, 0.4f, 1f, 1f);
        public Color AttentionColor = new Color(1f, 0.8f, 0.05f, 1f);
        public Color ChaseColor = new Color(1f, 0.05f, 0.02f, 1f);
        
        public static event Action<RobotScannerData> OnDataChanged;

        public Color GetColor(RobotScannerVisualState state)
        {
            switch (state)
            {
                case RobotScannerVisualState.Attention:
                    return AttentionColor;
                case RobotScannerVisualState.Chase:
                    return ChaseColor;
                default:
                    return IdleColor;
            }
        }

        private void OnValidate()
        {
            ScanInterval = Mathf.Max(0.05f, ScanInterval);
            Range = Mathf.Max(0f, Range);
            HorizontalAngle = Mathf.Clamp(HorizontalAngle, 1f, 360f);
            VerticalAngle = Mathf.Clamp(VerticalAngle, 1f, 180f);
            HorizontalSegments = Mathf.Clamp(HorizontalSegments, 4, 64);
            VerticalSegments = Mathf.Clamp(VerticalSegments, 2, 32);
            RadialSegments = Mathf.Clamp(RadialSegments, 1, 16);
            VisualAlpha = Mathf.Clamp01(VisualAlpha);
            
            OnDataChanged?.Invoke(this);
        }
    }
}
