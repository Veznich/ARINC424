using UnityEngine;

namespace Arkanoid.Configs
{
    /// <summary>
    /// Канонические значения MVP после плейтеста этапов 2–7.
    /// Единый источник для field-defaults и Editor → Apply MVP Defaults.
    /// </summary>
    public static class MvpConfigDefaults
    {
        public static void Apply(BallConfig c)
        {
            if (c == null) return;
            c.baseSpeed = 10f;
            c.maxSpeed = 20f;
            c.speedIncrement = 0.05f;
            c.speedIncrementInterval = 5f;
            c.paddleImpactMultiplier = 2f;
            c.maxPaddleBounceAngle = 60f;
            c.wallBounceAngle = 15f;
        }

        public static void Apply(PaddleConfig c)
        {
            if (c == null) return;
            c.width = 2f;
            c.height = 0.4f;
            c.moveSpeed = 20f;
            c.maxX = 5.2f;
            c.controlZoneScreenFraction = 0.333f;
            c.dragSensitivity = 0.01f;
            c.oneHandMoveSpeed = 12f;
            c.wideScaleMultiplier = 1.5f;
        }

        public static void Apply(LevelConfig c)
        {
            if (c == null) return;
            c.gridWidth = 10;
            c.gridHeight = 8;
            c.cellSize = 1f;
            c.gridOrigin = new Vector3(-4.5f, 2f, 0f);
            c.startBlockCount = 5;
            c.blocksPerLevel = 3;
            c.maxBlockCount = 72;
            c.tierUnlockEveryLevels = 10;
            c.maxBlockTier = 8;
            c.blockScale = 0.9f;
        }

        public static void Apply(PowerUpConfig c)
        {
            if (c == null) return;
            c.dropChance = 0.2f;
            c.lifeBonusShareOfDrops = 0.05f;
            c.coinDropChance = 0.3f;
            c.fallSpeed = 2.5f;
            c.lifetimeSeconds = 14f;
            c.magnetPullSpeed = 22f;
            c.maxBalls = 5;
            c.multiBallSpawnCount = 2;
            c.fireballDuration = 5f;
            c.widePaddleDuration = 6f;
            c.slowTimeDuration = 4f;
            c.laserDuration = 5f;
            c.magnetDuration = 10f;
            c.slowTimeScale = 0.6f;
            c.laserInterval = 0.5f;
            c.fireballPierceCount = 2;
        }

        public static void Apply(DifficultyConfig c)
        {
            if (c == null) return;
            c.strugglingLivesLostPerLevel = 2;
            c.easyLevelsWithoutDeath = 3;
            c.dropChanceBonus = 0.1f;
            c.maxDropChance = 0.35f;
            c.ballSpeedPenalty = 0.1f;
            c.extraBlockHp = 1;
            c.maxExtraBlockHp = 2;
            c.ballSpeedBonus = 0.1f;
            c.dropChancePenalty = 0.05f;
            c.minDropChance = 0.1f;
            c.speedPerLevel = 0.045f;
            c.dropChancePerLevel = 0.012f;
            c.extraHpEveryLevels = 3;
            c.maxLevelExtraHp = 4;
            c.useLevelExtraHpOnBlocks = false;
            c.minBallSpeedMul = 0.75f;
            c.maxBallSpeedMul = 1.6f;
            c.lerpSpeed = 2f;
            c.showNotifications = true;
        }

        public static void Apply(ComboConfig c)
        {
            if (c == null) return;
            c.tiers = new[]
            {
                new ComboConfig.ComboTier { blocksRequired = 3, multiplier = 2, displayLabel = "COMBO x2!" },
                new ComboConfig.ComboTier { blocksRequired = 5, multiplier = 3, displayLabel = "COMBO x3!" },
                new ComboConfig.ComboTier { blocksRequired = 10, multiplier = 5, displayLabel = "MEGA COMBO x5!" },
                new ComboConfig.ComboTier { blocksRequired = 20, multiplier = 10, displayLabel = "ULTRA COMBO x10!" }
            };
            c.resetOnWallHit = true;
            c.resetOnLifeLost = true;
        }

        public static void Apply(PlayerConfig c)
        {
            if (c == null) return;
            c.startLives = 3;
            c.maxLives = 5;
            c.maxBonusStartLives = 2;
            c.maxPaddleSizeTier = 5;
            c.paddleSizePerTier = 0.1f;
            c.maxRareBonusChanceTier = 5;
            c.rareBonusChancePerTier = 0.05f;
            c.startingCoins = 0;
        }

        public static void ApplyAll(GameConfigCatalog catalog)
        {
            if (catalog == null) return;
            Apply(catalog.ball);
            Apply(catalog.paddle);
            Apply(catalog.level);
            Apply(catalog.powerUp);
            Apply(catalog.difficulty);
            Apply(catalog.combo);
            Apply(catalog.player);
        }
    }
}
