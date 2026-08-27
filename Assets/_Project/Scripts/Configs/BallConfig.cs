using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Параметры мяча. Баланс только через этот ассет.</summary>
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Arkanoid/Configs/BallConfig")]
    public sealed class BallConfig : ScriptableObject
    {
        [Header("Скорость")]
        [Tooltip("Базовая скорость мяча")]
        public float baseSpeed = 10f;

        [Tooltip("Максимальная скорость (обычно x2 от base)")]
        public float maxSpeed = 20f;

        [Tooltip("Прирост скорости (доля) каждые N секунд")]
        public float speedIncrement = 0.05f;

        [Tooltip("Интервал ускорения в секундах")]
        public float speedIncrementInterval = 5f;

        [Header("Физика")]
        [Tooltip("Множитель импульса от скорости платформы")]
        public float paddleImpactMultiplier = 2f;

        [Tooltip("Максимальный угол отклонения от края платформы (градусы)")]
        public float maxPaddleBounceAngle = 60f;

        [Tooltip("Небольшой разброс при отскоке от стены")]
        public float wallBounceAngle = 15f;

        [ContextMenu("Reset to MVP Defaults")]
        private void ResetToMvpDefaults() => MvpConfigDefaults.Apply(this);

        private void OnValidate()
        {
            baseSpeed = Mathf.Max(0.1f, baseSpeed);
            maxSpeed = Mathf.Max(baseSpeed, maxSpeed);
            speedIncrementInterval = Mathf.Max(0.1f, speedIncrementInterval);
        }
    }
}
