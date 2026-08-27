using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Стартовые параметры игрока и лимиты meta.</summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Arkanoid/Configs/PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Жизни")]
        public int startLives = 3;
        public int maxLives = 5;

        [Header("Meta — лимиты")]
        public int maxBonusStartLives = 2;
        public int maxPaddleSizeTier = 5;
        public float paddleSizePerTier = 0.1f;
        public int maxRareBonusChanceTier = 5;
        public float rareBonusChancePerTier = 0.05f;

        [Header("Стартовые монеты нового сейва")]
        public int startingCoins;
    }
}
