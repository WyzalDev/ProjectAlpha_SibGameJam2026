using UnityEngine;

namespace _Game.Scripts.Core.Features.ThermalEquipment
{
    [CreateAssetMenu(fileName = "ThermalEquipmentData", menuName = "Configs/Equipment/Thermal Equipment Data")]
    public class ThermalEquipmentData : ScriptableObject
    {
        [Header("Heat Settings")]
        [Min(0.1f)] public float MaxHeat = 100f;
        [Min(0f)] public float HeatPerSecond = 10f;
        [Min(0f)] public float CoolingPerSecond = 15f;

        [Header("Movement Settings")]
        [Range(0.1f, 1f)] public float SpeedMultiplier = 0.85f;
        [Min(0.1f)] public float MaxSpeed = 3f;
        [Min(0.1f)] public float Acceleration = 3f;
        [Min(0.1f)] public float Deceleration = 1.5f;

        [Header("Equipment Transform")]
        public Vector3 LocalPosition;
        public Vector3 LocalRotation;
    }
}