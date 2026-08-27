using System;
using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Utils;
using VContainer.Unity;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// Старт/завершение уровня, seed, архетип. Публикует LevelStarted / LevelCompleted.
    /// </summary>
    public sealed class LevelService : IStartable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly LevelConfig _config;
        private readonly LevelGenerator _generator;

        private IDisposable _gameplaySub;
        private IDisposable _restartSub;
        private IDisposable _completedSub;

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

        public void Dispose()
        {
            _gameplaySub?.Dispose();
            _restartSub?.Dispose();
            _completedSub?.Dispose();
        }

        /// <summary>Сгенерировать и опубликовать уровень.</summary>
        public void StartLevel(int levelNumber)
        {
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
            UnityEngine.Debug.Log($"[LevelService] Level {e.LevelNumber} очищен → {next}");
            StartLevel(next);
        }
    }
}
