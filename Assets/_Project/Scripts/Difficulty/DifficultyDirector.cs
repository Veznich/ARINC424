using System;
using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Gameplay;
using UnityEngine;
using VContainer.Unity;

namespace Arkanoid.Difficulty
{
    public enum DifficultyBias : byte
    {
        Neutral = 0,
        Assist = 1,
        Challenge = 2
    }

    /// <summary>
    /// Адаптивная сложность: метрики уровня → drop chance / скорость мяча / extra HP блоков.
    /// «С 1-й попытки» = уровень без реального −жизнь (по LivesChanged).
    /// </summary>
    public sealed class DifficultyDirector : IStartable, ITickable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly DifficultyConfig _config;
        private readonly PowerUpConfig _powerUpConfig;
        private readonly LivesService _lives;

        private IDisposable _livesSub;
        private IDisposable _startedSub;
        private IDisposable _completedSub;
        private IDisposable _gameplaySub;
        private IDisposable _restartSub;

        private bool _levelOpen;
        private int _deathsThisLevel;
        private int _lastLives = -1;
        private int _clearStreak;
        private float _targetDropMod;
        private float _targetSpeedMul = 1f;
        private int _targetExtraHp;
        private float _dropMod;
        private float _speedMul = 1f;
        private int _extraHp;
        private float _levelSpeedMul = 1f;
        private float _levelDropMod;
        private int _levelExtraHp;
        private DifficultyBias _bias = DifficultyBias.Neutral;
        private bool _dirty;

        public float EffectiveDropChance
        {
            get
            {
                var baseChance = _powerUpConfig != null ? _powerUpConfig.dropChance : 0.2f;
                var min = _config != null ? _config.minDropChance : 0.1f;
                var max = _config != null ? _config.maxDropChance : 0.35f;
                return Mathf.Clamp(baseChance + _dropMod + _levelDropMod, min, max);
            }
        }

        public float BallSpeedMultiplier =>
            Mathf.Clamp(
                _speedMul * _levelSpeedMul,
                _config != null ? _config.minBallSpeedMul : 0.75f,
                _config != null ? _config.maxBallSpeedMul : 1.6f);

        public int ExtraBlockHits => Mathf.Max(0, _extraHp + _levelExtraHp);
        public DifficultyBias Bias => _bias;
        /// <summary>Уровни сессии, пройденные без потери жизни.</summary>
        public int FirstTryClears { get; private set; }

        public DifficultyDirector(
            IEventBus eventBus,
            DifficultyConfig config,
            PowerUpConfig powerUpConfig,
            LivesService lives)
        {
            _eventBus = eventBus;
            _config = config;
            _powerUpConfig = powerUpConfig;
            _lives = lives;
        }

        public void Start()
        {
            ResetModifiers();
            if (_eventBus == null)
            {
                return;
            }

            _livesSub = _eventBus.Subscribe<LivesChangedEvent>(OnLivesChanged);
            _startedSub = _eventBus.Subscribe<LevelStartedEvent>(OnLevelStarted);
            _completedSub = _eventBus.Subscribe<LevelCompletedEvent>(_ => OnLevelCompleted());
            _gameplaySub = _eventBus.Subscribe<RequestGameplayEvent>(_ => ResetSession());
            _restartSub = _eventBus.Subscribe<RequestRestartLevelEvent>(_ =>
            {
                _deathsThisLevel = 0;
                SyncLastLives();
            });
            SyncLastLives();
            PublishSessionStats();
        }

        public void Dispose()
        {
            _livesSub?.Dispose();
            _startedSub?.Dispose();
            _completedSub?.Dispose();
            _gameplaySub?.Dispose();
            _restartSub?.Dispose();
        }

        public void Tick()
        {
            if (_config == null)
            {
                return;
            }

            var t = Mathf.Clamp01(_config.lerpSpeed * Time.deltaTime);
            var prevDrop = _dropMod;
            var prevSpeed = _speedMul;
            _dropMod = Mathf.Lerp(_dropMod, _targetDropMod, t);
            _speedMul = Mathf.Lerp(_speedMul, _targetSpeedMul, t);

            if (_extraHp != _targetExtraHp)
            {
                _extraHp = _targetExtraHp;
                _dirty = true;
            }

            if (Mathf.Abs(prevDrop - _dropMod) > 0.0005f ||
                Mathf.Abs(prevSpeed - _speedMul) > 0.0005f ||
                _dirty)
            {
                _dirty = false;
                PublishChanged(forceToast: false);
            }
        }

        private void OnLivesChanged(LivesChangedEvent e)
        {
            if (_levelOpen && _lastLives >= 0 && e.Current < _lastLives)
            {
                _deathsThisLevel += _lastLives - e.Current;
            }

            _lastLives = e.Current;
        }

        private void OnLevelStarted(LevelStartedEvent e)
        {
            _levelOpen = true;
            _deathsThisLevel = 0;
            ApplyLevelBaseline(e.LevelNumber);
            SyncLastLives();
            SnapApplied();
            PublishChanged(forceToast: false);
            Debug.Log(
                $"[Difficulty] Level {e.LevelNumber} · speed×{BallSpeedMultiplier:F2} " +
                $"drop={EffectiveDropChance:F2} +hp={ExtraBlockHits}");
        }

        private void ApplyLevelBaseline(int levelNumber)
        {
            var tier = Mathf.Max(0, levelNumber - 1);
            if (_config == null)
            {
                _levelSpeedMul = 1f + tier * 0.03f;
                _levelDropMod = -tier * 0.008f;
                _levelExtraHp = 0; // HP/цвета блоков — в LevelGenerator
                return;
            }

            // Лёгкий рост скорости/дропа; плотность и цвета блоков — LevelGenerator
            _levelSpeedMul = 1f + tier * (_config.speedPerLevel * 0.65f);
            _levelDropMod = -tier * (_config.dropChancePerLevel * 0.65f);
            if (_config.useLevelExtraHpOnBlocks && _config.extraHpEveryLevels > 0)
            {
                _levelExtraHp = Mathf.Min(
                    _config.maxLevelExtraHp,
                    tier / _config.extraHpEveryLevels);
            }
            else
            {
                _levelExtraHp = 0;
            }
        }

        private void OnLevelCompleted()
        {
            _levelOpen = false;
            var lost = _deathsThisLevel;
            _deathsThisLevel = 0;

            if (lost == 0)
            {
                FirstTryClears++;
            }

            PublishSessionStats();

            if (_config == null)
            {
                Debug.Log(
                    $"[Difficulty] completed deaths={lost} firstTryClears={FirstTryClears} (no config)");
                return;
            }

            var prevBias = _bias;
            if (lost >= Mathf.Max(1, _config.strugglingLivesLostPerLevel))
            {
                ApplyAssist();
                _clearStreak = 0;
                _bias = DifficultyBias.Assist;
            }
            else if (lost == 0)
            {
                _clearStreak++;
                if (_clearStreak >= Mathf.Max(1, _config.easyLevelsWithoutDeath))
                {
                    ApplyChallenge();
                    _bias = DifficultyBias.Challenge;
                }
                else
                {
                    DriftTowardNeutral(0.25f);
                    _bias = DifficultyBias.Neutral;
                }
            }
            else
            {
                _clearStreak = 0;
                DriftTowardNeutral(0.35f);
                _bias = DifficultyBias.Neutral;
            }

            SnapApplied();
            PublishChanged(forceToast: prevBias != _bias && _bias != DifficultyBias.Neutral);
            Debug.Log(
                $"[Difficulty] completed deaths={lost} streak={_clearStreak} " +
                $"firstTry={FirstTryClears} bias={_bias} drop={EffectiveDropChance:F2} " +
                $"speed×{BallSpeedMultiplier:F2} +hp={ExtraBlockHits}");
        }

        private void SyncLastLives()
        {
            if (_lives != null)
            {
                _lastLives = _lives.Current;
            }
        }

        private void ApplyAssist()
        {
            _targetDropMod += _config.dropChanceBonus;
            _targetSpeedMul *= 1f - _config.ballSpeedPenalty;
            _targetExtraHp = Mathf.Max(0, _targetExtraHp - 1);
            ClampTargets();
        }

        private void ApplyChallenge()
        {
            _targetDropMod -= _config.dropChancePenalty;
            _targetSpeedMul *= 1f + _config.ballSpeedBonus;
            _targetExtraHp = Mathf.Min(
                _config.maxExtraBlockHp,
                _targetExtraHp + Mathf.Max(0, _config.extraBlockHp));
            ClampTargets();
        }

        private void DriftTowardNeutral(float amount)
        {
            _targetDropMod = Mathf.Lerp(_targetDropMod, 0f, amount);
            _targetSpeedMul = Mathf.Lerp(_targetSpeedMul, 1f, amount);
            if (_targetExtraHp > 0 && amount > 0.3f)
            {
                _targetExtraHp--;
            }

            ClampTargets();
        }

        private void ClampTargets()
        {
            var baseChance = _powerUpConfig != null ? _powerUpConfig.dropChance : 0.2f;
            var minMod = _config.minDropChance - baseChance;
            var maxMod = _config.maxDropChance - baseChance;
            _targetDropMod = Mathf.Clamp(_targetDropMod, minMod, maxMod);
            _targetSpeedMul = Mathf.Clamp(
                _targetSpeedMul,
                _config.minBallSpeedMul,
                _config.maxBallSpeedMul);
            _targetExtraHp = Mathf.Clamp(_targetExtraHp, 0, _config.maxExtraBlockHp);
        }

        private void SnapApplied()
        {
            _dropMod = _targetDropMod;
            _speedMul = _targetSpeedMul;
            _extraHp = _targetExtraHp;
        }

        private void ResetSession()
        {
            ResetModifiers();
            PublishChanged(forceToast: false);
            PublishSessionStats();
        }

        private void ResetModifiers()
        {
            _levelOpen = false;
            _deathsThisLevel = 0;
            _clearStreak = 0;
            FirstTryClears = 0;
            _targetDropMod = 0f;
            _targetSpeedMul = 1f;
            _targetExtraHp = 0;
            _dropMod = 0f;
            _speedMul = 1f;
            _extraHp = 0;
            _levelSpeedMul = 1f;
            _levelDropMod = 0f;
            _levelExtraHp = 0;
            _bias = DifficultyBias.Neutral;
            SyncLastLives();
        }

        private void PublishSessionStats()
        {
            _eventBus?.Publish(new SessionStatsChangedEvent(FirstTryClears));
        }

        private void PublishChanged(bool forceToast)
        {
            var showToast = forceToast && _config != null && _config.showNotifications;
            string msg = null;
            if (showToast)
            {
                msg = _bias == DifficultyBias.Assist
                    ? "ASSIST"
                    : _bias == DifficultyBias.Challenge
                        ? "CHALLENGE"
                        : null;
            }

            _eventBus?.Publish(new DifficultyChangedEvent(
                EffectiveDropChance,
                BallSpeedMultiplier,
                ExtraBlockHits,
                _bias,
                msg));
            PublishSessionStats();
        }
    }
}
