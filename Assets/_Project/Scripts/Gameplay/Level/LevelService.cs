using System;
using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Utils;
using VContainer.Unity;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Старт/завершение уровня, seed, архетип. Публикует LevelStarted / LevelCompleted.
    /// Следующий уровень стартует на следующем тике — чтобы все обработчики LevelCompleted
    /// успели прочитать метрики текущего уровня.
    /// </summary>
    public sealed class LevelService : IStartable, ITickable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly LevelConfig _config;
        private readonly LevelGenerator _generator;

        private IDisposable _gameplaySub;
        private IDisposable _restartSub;
        private IDisposable _completedSub;
        private int _queuedLevel = -1;

        public int CurrentLevel { get; private set; } = GameDefaults.DEFAULT_LEVEL;
        public int CurrentSeed { get; private set; }
        public LevelArchetype CurrentArchetype { get; private set; }
        public LevelLayout CurrentLayout { get; private set; }

        public LevelService(IEventBus eventBus, LevelConfig config, LevelGenerator generator)
        {
            _eventBus = eventBus;
            _config = config;
            _generator = generator;
        }

        public void Start()
        {
            _gameplaySub = _eventBus.Subscribe<RequestGameplayEvent>(e => StartLevel(e.LevelNumber));
            _restartSub = _eventBus.Subscribe<RequestRestartLevelEvent>(_ => StartLevel(CurrentLevel));
            _completedSub = _eventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        }

        public void Tick()
        {
            if (_queuedLevel < 1)
            {
                return;
            }

            var next = _queuedLevel;
            _queuedLevel = -1;
            StartLevel(next);
        }

        public void Dispose()
        {
            _gameplaySub?.Dispose();
            _restartSub?.Dispose();
            _completedSub?.Dispose();
        }

        /// <summary>Сгенерировать и опубликовать уровень.</summary>
        public void StartLevel(int levelNumber)
        {
            _queuedLevel = -1;
            CurrentLevel = levelNumber < 1 ? 1 : levelNumber;
            CurrentSeed = SeedGenerator.ComputeSeed(CurrentLevel);
            CurrentArchetype = LevelGenerator.PickArchetype(CurrentSeed);
            CurrentLayout = _generator.Generate(CurrentLevel, CurrentSeed, CurrentArchetype, _config);

            UnityEngine.Debug.Log(
                $"[LevelService] Level {CurrentLevel} · Seed {CurrentSeed} · {CurrentArchetype} · " +
                $"blocks {CurrentLayout.OccupiedCount}");

            _eventBus.Publish(new LevelStartedEvent(
                CurrentLevel,
                CurrentSeed,
                CurrentArchetype,
                CurrentLayout));
        }

        private void OnLevelCompleted(LevelCompletedEvent e)
        {
            var next = e.LevelNumber + 1;
            UnityEngine.Debug.Log($"[LevelService] Level {e.LevelNumber} очищен → queue {next}");
            _queuedLevel = next;
        }
    }
}
