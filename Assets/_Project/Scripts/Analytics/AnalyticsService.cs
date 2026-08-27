using System;
using System.Collections.Generic;
using Arkanoid.Core;
using UnityEngine;
using VContainer.Unity;

namespace Arkanoid.Analytics
{
    public interface IAnalyticsService
    {
        void Track(string eventName, Dictionary<string, string> props = null);
        void Flush();
        string FilePath { get; }
    }

    /// <summary>Локальная аналитика: EventBus → буфер → analytics.json.</summary>
    public sealed class AnalyticsService : IAnalyticsService, IStartable, ITickable, IDisposable
    {
        public const int MaxStoredEvents = 400;
        private const float AutosaveIntervalSeconds = 30f;

        private readonly IEventBus _eventBus;
        private readonly AnalyticsStore _store = new AnalyticsStore();
        private AnalyticsData _data;

        private readonly List<IDisposable> _subs = new List<IDisposable>(16);
        private float _timeSinceFlush;
        private bool _dirty;

        public string FilePath => _store.FilePath;

        public AnalyticsService(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Start()
        {
            _data = _store.Load();
            _data.counters.sessions++;
            Track("session_start", new Dictionary<string, string>
            {
                { "sessions", _data.counters.sessions.ToString() },
                { "platform", Application.platform.ToString() }
            });
            Flush();
            Subscribe();
            Debug.Log($"[Analytics] Ready → {FilePath}");
        }

        public void Dispose()
        {
            Track("session_end");
            Flush();
            for (var i = 0; i < _subs.Count; i++)
            {
                _subs[i]?.Dispose();
            }

            _subs.Clear();
        }

        public void Tick()
        {
            if (!_dirty)
            {
                return;
            }

            _timeSinceFlush += Time.unscaledDeltaTime;
            if (_timeSinceFlush >= AutosaveIntervalSeconds)
            {
                Flush();
            }
        }

        public void Track(string eventName, Dictionary<string, string> props = null)
        {
            if (string.IsNullOrEmpty(eventName) || _data == null)
            {
                return;
            }

            _data.events.Add(AnalyticsEventDto.Create(eventName, props));
            while (_data.events.Count > MaxStoredEvents)
            {
                _data.events.RemoveAt(0);
            }

            _dirty = true;
        }

        public void Flush()
        {
            if (_data == null)
            {
                return;
            }

            _store.Save(_data);
            _dirty = false;
            _timeSinceFlush = 0f;
        }

        private void Subscribe()
        {
            if (_eventBus == null)
            {
                return;
            }

            _subs.Add(_eventBus.Subscribe<LevelStartedEvent>(e =>
            {
                _data.counters.levelsStarted++;
                if (e.LevelNumber > _data.counters.maxLevelReached)
                {
                    _data.counters.maxLevelReached = e.LevelNumber;
                }

                Track("level_start", new Dictionary<string, string>
                {
                    { "level", e.LevelNumber.ToString() },
                    { "seed", e.Seed.ToString() },
                    { "archetype", e.Archetype.ToString() }
                });
            }));

            _subs.Add(_eventBus.Subscribe<LevelCompletedEvent>(e =>
            {
                _data.counters.levelsCompleted++;
                Track("level_complete", new Dictionary<string, string>
                {
                    { "level", e.LevelNumber.ToString() },
                    { "seed", e.Seed.ToString() }
                });
                Flush();
            }));

            _subs.Add(_eventBus.Subscribe<BallLostEvent>(_ =>
            {
                _data.counters.ballsLost++;
                Track("ball_lost");
            }));

            _subs.Add(_eventBus.Subscribe<PowerUpCollectedEvent>(e =>
            {
                _data.counters.powerUpsCollected++;
                Track("powerup_collected", new Dictionary<string, string>
                {
                    { "type", e.Type.ToString() }
                });
            }));

            _subs.Add(_eventBus.Subscribe<RequestGameOverEvent>(_ =>
            {
                _data.counters.gameOvers++;
                Track("game_over");
                Flush();
            }));

            _subs.Add(_eventBus.Subscribe<ReplaySavedEvent>(e =>
            {
                _data.counters.replaysSaved++;
                Track("replay_saved", new Dictionary<string, string>
                {
                    { "id", e.Id },
                    { "level", e.LevelNumber.ToString() },
                    { "frames", e.FrameCount.ToString() }
                });
            }));

            _subs.Add(_eventBus.Subscribe<ReplayPlaybackStartedEvent>(e =>
            {
                _data.counters.replaysPlayed++;
                Track("replay_playback", new Dictionary<string, string>
                {
                    { "id", e.Id },
                    { "level", e.LevelNumber.ToString() }
                });
            }));

            _subs.Add(_eventBus.Subscribe<GameStateChangedEvent>(e =>
            {
                Track("state_changed", new Dictionary<string, string>
                {
                    { "from", e.Previous.ToString() },
                    { "to", e.Current.ToString() }
                });
            }));
        }
    }
}
