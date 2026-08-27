namespace Arkanoid.Core
{
    /// <summary>
    /// Глобальные дефолты приложения (не игровой баланс — он в ScriptableObjects).
    /// </summary>
    public static class GameDefaults
    {
        public const int TARGET_FRAME_RATE = 60;
        public const string SAVE_FILE_NAME = "save.json";
        public const string ANALYTICS_FILE_NAME = "analytics.json";
        public const int DEFAULT_LIVES = 3;
        public const int MAX_LIVES = 5;
        public const int DEFAULT_LEVEL = 1;
        public const string DEFAULT_SKIN_ID = "neon_default";
        public const int SEED_MULTIPLIER = 1337;
        public const int SEED_OFFSET = 42;
        public const int SEED_MODULO = 1000000;
        public const int MAX_STORED_REPLAYS = 10;
    }
}
