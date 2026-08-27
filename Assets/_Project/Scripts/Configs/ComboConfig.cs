using System;
using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>Множители комбо и пороги блоков подряд.</summary>
    [CreateAssetMenu(fileName = "ComboConfig", menuName = "Arkanoid/Configs/ComboConfig")]
    public sealed class ComboConfig : ScriptableObject
    {
        [Serializable]
        public struct ComboTier
        {
            public int blocksRequired;
            public int multiplier;
            public string displayLabel;
        }

        public ComboTier[] tiers =
        {
            new ComboTier { blocksRequired = 3, multiplier = 2, displayLabel = "COMBO x2!" },
            new ComboTier { blocksRequired = 5, multiplier = 3, displayLabel = "COMBO x3!" },
            new ComboTier { blocksRequired = 10, multiplier = 5, displayLabel = "MEGA COMBO x5!" },
            new ComboTier { blocksRequired = 20, multiplier = 10, displayLabel = "ULTRA COMBO x10!" }
        };

        public bool resetOnWallHit = true;
        public bool resetOnLifeLost = true;
    }
}
