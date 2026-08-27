using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Генерация уровней: сетка, архетипы, прогрессия блоков.</summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Arkanoid/Configs/LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Сетка")]
        public int gridWidth = 10;
        public int gridHeight = 8;
        public float cellSize = 1f;
        public Vector3 gridOrigin = new Vector3(-4.5f, 2f, 0f);

        [Header("Прогрессия блоков")]
        [Tooltip("Сколько блоков на 1 уровне (все green)")]
        public int startBlockCount = 5;
        [Tooltip("Сколько блоков добавлять за каждый следующий уровень")]
        public int blocksPerLevel = 3;
        public int maxBlockCount = 72;
        [Tooltip("Каждые N уровней открывается новый цвет/HP")]
        public int tierUnlockEveryLevels = 10;
        [Tooltip("Макс. HP/цвет (1..8)")]
        public int maxBlockTier = 8;

        [Header("Размер блока (визуал)")]
        public float blockScale = 0.9f;

        // Legacy / совместимость
        [HideInInspector] public float weightGreen = 50f;
        [HideInInspector] public float weightYellow = 30f;
        [HideInInspector] public float weightRed = 20f;
        [HideInInspector] public float weightNormal = 50f;
        [HideInInspector] public float weightHard = 25f;
        [HideInInspector] public float weightExplosive;
        [HideInInspector] public float weightFrozen;
        [HideInInspector] public float weightGenerator;
        [HideInInspector] public int hardMinHits = 2;
        [HideInInspector] public int hardMaxHits = 3;
        [HideInInspector] public float generatorIntervalSeconds = 5f;
        [HideInInspector] public float frozenSlowDuration = 1f;
        [HideInInspector] public float frozenSpeedMultiplier = 0.45f;
    }
}
