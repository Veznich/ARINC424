using UnityEngine;

namespace Arkanoid.Core
{
    /// <summary>
    /// Отладочные команды seed (вызываются из скрытого меню на следующих этапах).
    /// Пример: SeedDebugCommands.SetSeed(12345);
    /// </summary>
    public static class SeedDebugCommands
    {
        /// <summary>Установить ручной seed для следующего уровня.</summary>
        public static void SetSeed(int seed)
        {
            Utils.SeedGenerator.SetManualOverride(seed);
            Debug.Log($"[SeedDebug] Override = {seed}");
        }

        /// <summary>Сбросить ручной seed.</summary>
        public static void ClearSeed()
        {
            Utils.SeedGenerator.SetManualOverride(null);
            Debug.Log("[SeedDebug] Override сброшен");
        }

        /// <summary>Показать seed для уровня.</summary>
        public static int Preview(int levelNumber)
        {
            var seed = Utils.SeedGenerator.ComputeSeed(levelNumber);
            Debug.Log($"[SeedDebug] Level {levelNumber} → Seed {seed}");
            return seed;
        }
    }
}
