using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Шансы и длительности бонусов.</summary>
    [CreateAssetMenu(fileName = "PowerUpConfig", menuName = "Arkanoid/Configs/PowerUpConfig")]
    public sealed class PowerUpConfig : ScriptableObject
    {
        [Header("Дроп")]
        [Range(0f, 1f)]
        public float dropChance = 0.2f;

        [Range(0f, 1f)]
        public float lifeBonusShareOfDrops = 0.05f;

        [Range(0f, 1f)]
        public float coinDropChance = 0.3f;

        public float fallSpeed = 2f;
        public float lifetimeSeconds = 5f;
        public float magnetPullSpeed = 5f;
        public int maxBalls = 3;

        [Header("Длительности (сек)")]
        public float fireballDuration = 5f;
        public float widePaddleDuration = 6f;
        public float slowTimeDuration = 4f;
        public float laserDuration = 5f;
        public float magnetDuration = 7f;

        [Header("Эффекты")]
        public float slowTimeScale = 0.6f;
        public float laserInterval = 0.5f;
        public int fireballPierceCount = 2;
    }
}
