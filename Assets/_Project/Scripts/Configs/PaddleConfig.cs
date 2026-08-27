using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Параметры платформы и зоны управления.</summary>
    [CreateAssetMenu(fileName = "PaddleConfig", menuName = "Arkanoid/Configs/PaddleConfig")]
    public sealed class PaddleConfig : ScriptableObject
    {
        [Header("Размер и движение")]
        public float width = 2f;
        public float height = 0.4f;
        public float moveSpeed = 20f;
            public float maxX = 5.2f;

        [Header("Ввод")]
        [Tooltip("Доля экрана снизу, где активен drag (1/3)")]
        [Range(0.1f, 0.5f)]
        public float controlZoneScreenFraction = 0.333f;

        [Tooltip("Чувствительность deltaPosition.x")]
        public float dragSensitivity = 0.01f;

        [Tooltip("Скорость one-hand режима")]
        public float oneHandMoveSpeed = 12f;

        [Header("Wide Paddle")]
        public float wideScaleMultiplier = 1.5f;
    }
}
