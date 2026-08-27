namespace Arkanoid.Core
{
    #region Gameplay — мяч / платформа

    /// <summary>Мяч прикреплён к платформе (ожидает запуска).</summary>
    public readonly struct BallDockedEvent
    {
    }

    /// <summary>Мяч запущен.</summary>
    public readonly struct BallLaunchedEvent
    {
        public readonly UnityEngine.Vector3 Direction;

        public BallLaunchedEvent(UnityEngine.Vector3 direction)
        {
            Direction = direction;
        }
    }

    /// <summary>Мяч ушёл за нижнюю границу.</summary>
    public readonly struct BallLostEvent
    {
    }

    /// <summary>Отскок мяча от платформы.</summary>
    public readonly struct BallHitPaddleEvent
    {
        public readonly float HitFactor;

        public BallHitPaddleEvent(float hitFactor)
        {
            HitFactor = hitFactor;
        }
    }

    /// <summary>Отскок мяча от стены.</summary>
    public readonly struct BallHitWallEvent
    {
    }

    #endregion

    #region Gameplay — уровень / блоки

    /// <summary>Уровень сгенерирован и готов к спавну блоков.</summary>
    public readonly struct LevelStartedEvent
    {
        public readonly int LevelNumber;
        public readonly int Seed;
        public readonly Gameplay.LevelArchetype Archetype;
        public readonly Gameplay.LevelLayout Layout;

        public LevelStartedEvent(
            int levelNumber,
            int seed,
            Gameplay.LevelArchetype archetype,
            Gameplay.LevelLayout layout)
        {
            LevelNumber = levelNumber;
            Seed = seed;
            Archetype = archetype;
            Layout = layout;
        }
    }

    /// <summary>Все блоки уничтожены.</summary>
    public readonly struct LevelCompletedEvent
    {
        public readonly int LevelNumber;
        public readonly int Seed;

        public LevelCompletedEvent(int levelNumber, int seed)
        {
            LevelNumber = levelNumber;
            Seed = seed;
        }
    }

    /// <summary>Сессионная статистика (для Info HUD).</summary>
    public readonly struct SessionStatsChangedEvent
    {
        public readonly int FirstTryClears;

        public SessionStatsChangedEvent(int firstTryClears)
        {
            FirstTryClears = firstTryClears;
        }
    }

    /// <summary>Replay сохранён на диск.</summary>
    public readonly struct ReplaySavedEvent
    {
        public readonly string Id;
        public readonly int LevelNumber;
        public readonly int FrameCount;

        public ReplaySavedEvent(string id, int levelNumber, int frameCount)
        {
            Id = id;
            LevelNumber = levelNumber;
            FrameCount = frameCount;
        }
    }

    /// <summary>Начат playback replay.</summary>
    public readonly struct ReplayPlaybackStartedEvent
    {
        public readonly string Id;
        public readonly int LevelNumber;

        public ReplayPlaybackStartedEvent(string id, int levelNumber)
        {
            Id = id;
            LevelNumber = levelNumber;
        }
    }

    /// <summary>Playback replay завершён.</summary>
    public readonly struct ReplayPlaybackEndedEvent
    {
    }

    /// <summary>Удар по блоку (до уничтожения).</summary>
    public readonly struct BlockHitEvent
    {
        public readonly int GridX;
        public readonly int GridY;
        public readonly Gameplay.BlockType Type;
        public readonly int RemainingHits;

        public BlockHitEvent(int gridX, int gridY, Gameplay.BlockType type, int remainingHits)
        {
            GridX = gridX;
            GridY = gridY;
            Type = type;
            RemainingHits = remainingHits;
        }
    }

    /// <summary>Блок уничтожен.</summary>
    public readonly struct BlockDestroyedEvent
    {
        public readonly int GridX;
        public readonly int GridY;
        public readonly Gameplay.BlockType Type;
        public readonly UnityEngine.Vector3 WorldPosition;

        public BlockDestroyedEvent(int gridX, int gridY, Gameplay.BlockType type, UnityEngine.Vector3 worldPosition)
        {
            GridX = gridX;
            GridY = gridY;
            Type = type;
            WorldPosition = worldPosition;
        }
    }

    /// <summary>Изменилось число жизней.</summary>
    public readonly struct LivesChangedEvent
    {
        public readonly int Current;
        public readonly int Max;

        public LivesChangedEvent(int current, int max)
        {
            Current = current;
            Max = max;
        }
    }

    /// <summary>Подобран / активирован бонус.</summary>
    public readonly struct PowerUpCollectedEvent
    {
        public readonly Gameplay.PowerUpType Type;

        public PowerUpCollectedEvent(Gameplay.PowerUpType type)
        {
            Type = type;
        }
    }

    /// <summary>Истёк timed-бонус.</summary>
    public readonly struct PowerUpExpiredEvent
    {
        public readonly Gameplay.PowerUpType Type;

        public PowerUpExpiredEvent(Gameplay.PowerUpType type)
        {
            Type = type;
        }
    }

    /// <summary>Состояние таймеров для HUD (snapshot).</summary>
    public readonly struct PowerUpTimersChangedEvent
    {
        public readonly Gameplay.PowerUpTimerInfo[] Timers;

        public PowerUpTimersChangedEvent(Gameplay.PowerUpTimerInfo[] timers)
        {
            Timers = timers;
        }
    }

    /// <summary>Изменились модификаторы сложности.</summary>
    public readonly struct DifficultyChangedEvent
    {
        public readonly float DropChance;
        public readonly float BallSpeedMultiplier;
        public readonly int ExtraBlockHits;
        public readonly Arkanoid.Difficulty.DifficultyBias Bias;
        public readonly string ToastMessage;

        public DifficultyChangedEvent(
            float dropChance,
            float ballSpeedMultiplier,
            int extraBlockHits,
            Arkanoid.Difficulty.DifficultyBias bias,
            string toastMessage)
        {
            DropChance = dropChance;
            BallSpeedMultiplier = ballSpeedMultiplier;
            ExtraBlockHits = extraBlockHits;
            Bias = bias;
            ToastMessage = toastMessage;
        }
    }

    #endregion
}
