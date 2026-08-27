using System.Collections.Generic;
using Newtonsoft.Json;

namespace Arkanoid.Save
{
    /// <summary>
    /// DTO сохранения игрока. Сериализуется в save.json через Newtonsoft
    /// (не Unity SerializeField — без [Serializable], иначе UAC1009 на Dictionary).
    /// </summary>
    public sealed class SaveData
    {
        public int currentLevel = 1;
        public int lives = 3;
        public int totalCoins;
        public List<string> unlockedSkins = new List<string> { "neon_default" };
        public string currentSkin = "neon_default";
        public int highScore;
        public int totalBlocksDestroyed;
        public List<string> unlockedAchievements = new List<string>();
        public Dictionary<string, int> playerStats = new Dictionary<string, int>();

        /// <summary>Мета-прокачка между забегами.</summary>
        public MetaUpgradeData metaUpgrades = new MetaUpgradeData();

        /// <summary>Настройки ввода и UX.</summary>
        public SettingsData settings = new SettingsData();

        /// <summary>Создать сохранение с дефолтными значениями.</summary>
        public static SaveData CreateDefault()
        {
            return new SaveData();
        }

        /// <summary>Глубокая копия через JSON (безопасно для мутаций сервиса).</summary>
        public SaveData Clone()
        {
            var json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<SaveData>(json) ?? CreateDefault();
        }
    }

    public sealed class MetaUpgradeData
    {
        public int bonusStartLives;
        public int paddleSizeTier;
        public int rareBonusChanceTier;
        public string preferredStartPowerUpId = string.Empty;
    }

    public sealed class SettingsData
    {
        /// <summary>false = drag delta; true = one-hand (лево/право от центра).</summary>
        public bool useOneHandControl;
        public bool hapticsEnabled = true;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
    }
}
