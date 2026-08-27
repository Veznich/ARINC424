using Arkanoid.Core;

namespace Arkanoid.Utils
{
    /// <summary>
    /// Детерминированный Seed уровней. Поддерживает ручной override для отладки.
    /// </summary>
    public static class SeedGenerator
    {
        private static int? _manualOverride;

        /// <summary>Вычислить seed по номеру уровня.</summary>
        public static int ComputeSeed(int levelNumber)
        {
            if (_manualOverride.HasValue)
            {
                return _manualOverride.Value;
            }

            var level = levelNumber < 1 ? 1 : levelNumber;
            return (level * GameDefaults.SEED_MULTIPLIER + GameDefaults.SEED_OFFSET) % GameDefaults.SEED_MODULO;
        }

        /// <summary>Ручной seed (скрытое меню / консоль). null — сброс override.</summary>
        public static void SetManualOverride(int? seed)
        {
            _manualOverride = seed;
        }

        /// <summary>Есть ли активный ручной override.</summary>
        public static bool HasManualOverride => _manualOverride.HasValue;

        /// <summary>Текущий override или -1.</summary>
        public static int ManualOverrideOrDefault => _manualOverride ?? -1;
    }
}
