using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Пороги адаптивной сложности (Difficulty Director).</summary>
    [CreateAssetMenu(fileName = "DifficultyConfig", menuName = "Arkanoid/Configs/DifficultyConfig")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        [Header("Пороги")]
        public int strugglingLivesLostPerLevel = 2;
        public int easyLevelsWithoutDeath = 3;

        [Header("Адаптация — struggling (Assist)")]
        public float dropChanceBonus = 0.1f;
        public float maxDropChance = 0.35f;
        public float ballSpeedPenalty = 0.1f;

        [Header("Адаптация — easy (Challenge)")]
        public int extraBlockHp = 1;
        public int maxExtraBlockHp = 2;
        public float ballSpeedBonus = 0.1f;
        public float dropChancePenalty = 0.05f;
        public float minDropChance = 0.1f;

        [Header("Рост по уровням")]
        [Tooltip("+скорость мяча за каждый уровень после 1")]
        public float speedPerLevel = 0.045f;
        [Tooltip("−шанс дропа за каждый уровень после 1")]
        public float dropChancePerLevel = 0.012f;
        [Tooltip("Каждые N уровней +1 HP к блокам")]
        public int extraHpEveryLevels = 3;
        public int maxLevelExtraHp = 4;

        [Header("Клампы скорости")]
        public float minBallSpeedMul = 0.75f;
        public float maxBallSpeedMul = 1.6f;

        [Header("Плавность")]
        public float lerpSpeed = 2f;
        public bool showNotifications = true;
    }
}
