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

        public float fallSpeed = 2.5f;
        [Tooltip("Запас по времени; дроп не исчезает над платформой раньше времени")]
        public float lifetimeSeconds = 14f;
        public float magnetPullSpeed = 22f;
        public int maxBalls = 5;
        [Tooltip("Сколько доп. мячей даёт один Multi Ball")]
        public int multiBallSpawnCount = 2;

        [Header("Длительности (сек)")]
        public float fireballDuration = 5f;
        public float widePaddleDuration = 6f;
        public float slowTimeDuration = 4f;
        public float laserDuration = 5f;
        public float magnetDuration = 10f;

        [Header("Эффекты")]
        public float slowTimeScale = 0.6f;
        public float laserInterval = 0.5f;
        public int fireballPierceCount = 2;

        [ContextMenu("Reset to MVP Defaults")]
        private void ResetToMvpDefaults() => MvpConfigDefaults.Apply(this);

        private void OnValidate()
        {
            fallSpeed = Mathf.Max(0.1f, fallSpeed);
            lifetimeSeconds = Mathf.Max(1f, lifetimeSeconds);
            maxBalls = Mathf.Max(1, maxBalls);
            multiBallSpawnCount = Mathf.Max(1, multiBallSpawnCount);
            slowTimeScale = Mathf.Clamp(slowTimeScale, 0.1f, 1f);
            laserInterval = Mathf.Max(0.05f, laserInterval);
            fireballPierceCount = Mathf.Max(1, fireballPierceCount);
        }
    }
}
