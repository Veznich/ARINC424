namespace Arkanoid.Core
{
    #region События состояний

    /// <summary>Смена состояния игры.</summary>
    public readonly struct GameStateChangedEvent
    {
        public readonly GameState Previous;
        public readonly GameState Current;

        public GameStateChangedEvent(GameState previous, GameState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    /// <summary>Запрос перехода в меню.</summary>
    public readonly struct RequestMenuEvent
    {
    }

    /// <summary>Запрос старта геймплея.</summary>
    public readonly struct RequestGameplayEvent
    {
        public readonly int LevelNumber;

        public RequestGameplayEvent(int levelNumber)
        {
            LevelNumber = levelNumber;
        }
    }

    /// <summary>Запрос паузы.</summary>
    public readonly struct RequestPauseEvent
    {
    }

    /// <summary>Запрос снятия паузы.</summary>
    public readonly struct RequestResumeEvent
    {
    }

    /// <summary>Запрос рестарта уровня.</summary>
    public readonly struct RequestRestartLevelEvent
    {
    }

    /// <summary>Запрос Game Over.</summary>
    public readonly struct RequestGameOverEvent
    {
    }

    #endregion

    #region События сохранения

    /// <summary>Сохранение успешно записано на диск.</summary>
    public readonly struct SaveCompletedEvent
    {
    }

    /// <summary>Сохранение загружено (или создано с дефолтами).</summary>
    public readonly struct SaveLoadedEvent
    {
        public readonly bool WasCreatedNew;

        public SaveLoadedEvent(bool wasCreatedNew)
        {
            WasCreatedNew = wasCreatedNew;
        }
    }

    #endregion
}
