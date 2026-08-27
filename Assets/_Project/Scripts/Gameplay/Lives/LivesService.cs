using System;
using Arkanoid.Configs;
using Arkanoid.Core;
using VContainer.Unity;

namespace Arkanoid.Gameplay
{
    /// <summary>Жизни сессии: BallLost → −1, при 0 → GameOver.</summary>
    public sealed class LivesService : IStartable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly PlayerConfig _playerConfig;

        private IDisposable _lostSub;
        private IDisposable _gameplaySub;
        private IDisposable _restartSub;

        public int Current { get; private set; }
        public int Max { get; private set; }

        public LivesService(IEventBus eventBus, PlayerConfig playerConfig)
        {
            _eventBus = eventBus;
            _playerConfig = playerConfig;
            Max = playerConfig != null ? playerConfig.maxLives : GameDefaults.MAX_LIVES;
            Current = playerConfig != null ? playerConfig.startLives : GameDefaults.DEFAULT_LIVES;
        }

        public void Start()
        {
            _lostSub = _eventBus.Subscribe<BallLostEvent>(_ => OnBallLost());
            _gameplaySub = _eventBus.Subscribe<RequestGameplayEvent>(_ => ResetLives());
            _restartSub = _eventBus.Subscribe<RequestRestartLevelEvent>(_ => ResetLives());
            Publish();
        }

        public void Dispose()
        {
            _lostSub?.Dispose();
            _gameplaySub?.Dispose();
            _restartSub?.Dispose();
        }

        public void ResetLives()
        {
            Current = _playerConfig != null ? _playerConfig.startLives : GameDefaults.DEFAULT_LIVES;
            Max = _playerConfig != null ? _playerConfig.maxLives : GameDefaults.MAX_LIVES;
            Publish();
        }

        public bool TryAddLife()
        {
            if (Current >= Max)
            {
                return false;
            }

            Current++;
            Publish();
            return true;
        }

        private void OnBallLost()
        {
            if (Current <= 0)
            {
                return;
            }

            Current--;
            Publish();
            if (Current <= 0)
            {
                _eventBus.Publish(new RequestGameOverEvent());
            }
        }

        private void Publish()
        {
            _eventBus.Publish(new LivesChangedEvent(Current, Max));
        }
    }
}
