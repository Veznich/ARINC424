using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Генерация уровней: сетка, архетипы, веса блоков.</summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Arkanoid/Configs/LevelConfig")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Сетка")]
        public int gridWidth = 10;
        public int gridHeight = 8;
        public float cellSize = 1f;
        public Vector3 gridOrigin = new Vector3(-4.5f, 1.5f, 0f);

        [Header("Веса: Green 1HP / Yellow 2HP / Red 3HP")]
        public float weightGreen = 50f;
        public float weightYellow = 30f;
        public float weightRed = 20f;

        [Header("Размер блока (визуал)")]
        public float blockScale = 0.9f;

        // Совместимость со старыми ассетами / будущие спец-блоки (Этап 4+)
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
