using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>
    /// Каталог всех конфигов MVP для инжекта через VContainer.
    /// Создать ассет: Create → Arkanoid/Configs/GameConfigCatalog.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfigCatalog", menuName = "Arkanoid/Configs/GameConfigCatalog")]
    public sealed class GameConfigCatalog : ScriptableObject
    {
        public BallConfig ball;
        public PaddleConfig paddle;
        public LevelConfig level;
        public PowerUpConfig powerUp;
        public DifficultyConfig difficulty;
        public ComboConfig combo;
        public PlayerConfig player;

        /// <summary>Проверка, что все ссылки назначены.</summary>
        public bool IsValid(out string error)
        {
            if (ball == null) { error = "BallConfig не назначен"; return false; }
            if (paddle == null) { error = "PaddleConfig не назначен"; return false; }
            if (level == null) { error = "LevelConfig не назначен"; return false; }
            if (powerUp == null) { error = "PowerUpConfig не назначен"; return false; }
            if (difficulty == null) { error = "DifficultyConfig не назначен"; return false; }
            if (combo == null) { error = "ComboConfig не назначен"; return false; }
            if (player == null) { error = "PlayerConfig не назначен"; return false; }
            error = null;
            return true;
        }
    }
}
