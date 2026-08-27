using System;
using UnityEngine;

namespace Arkanoid.Core
{
    /// <summary>
    /// Конечный автомат Menu → Gameplay → Pause → GameOver.
    /// Управляет Time.timeScale для паузы.
    /// </summary>
    public interface IGameStateMachine
    {
        GameState CurrentState { get; }

        /// <summary>Сменить состояние (с валидацией переходов).</summary>
        void ChangeState(GameState nextState);

        /// <summary>Можно ли перейти в указанное состояние из текущего.</summary>
        bool CanTransitionTo(GameState nextState);
    }

    /// <summary>
    /// Реализация FSM. Публикует GameStateChangedEvent через EventBus.
    /// </summary>
    public sealed class GameStateMachine : IGameStateMachine, IDisposable
    {
        private readonly IEventBus _eventBus;
        private IDisposable _menuSub;
        private IDisposable _gameplaySub;
        private IDisposable _pauseSub;
        private IDisposable _resumeSub;
        private IDisposable _restartSub;
        private IDisposable _gameOverSub;

        private float _cachedTimeScale = 1f;

        public GameState CurrentState { get; private set; } = GameState.None;

        public GameStateMachine(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            SubscribeToRequests();
        }

        /// <inheritdoc />
        public bool CanTransitionTo(GameState nextState)
        {
            if (nextState == CurrentState)
            {
                return false;
            }

            switch (CurrentState)
            {
                case GameState.None:
                    return nextState == GameState.Menu;
                case GameState.Menu:
                    return nextState == GameState.Gameplay;
                case GameState.Gameplay:
                    return nextState == GameState.Pause || nextState == GameState.GameOver || nextState == GameState.Menu;
                case GameState.Pause:
                    return nextState == GameState.Gameplay || nextState == GameState.Menu;
                case GameState.GameOver:
                    return nextState == GameState.Gameplay || nextState == GameState.Menu;
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public void ChangeState(GameState nextState)
        {
            if (!CanTransitionTo(nextState))
            {
                Debug.LogWarning($"[GameStateMachine] Недопустимый переход: {CurrentState} → {nextState}");
                return;
            }

            var previous = CurrentState;
            ExitState(previous);
            CurrentState = nextState;
            EnterState(nextState);
            _eventBus.Publish(new GameStateChangedEvent(previous, nextState));
        }

        /// <summary>Принудительный старт в Menu (bootstrap).</summary>
        public void Bootstrap()
        {
            if (CurrentState != GameState.None)
            {
                return;
            }

            CurrentState = GameState.Menu;
            EnterState(GameState.Menu);
            _eventBus.Publish(new GameStateChangedEvent(GameState.None, GameState.Menu));
        }

        public void Dispose()
        {
            _menuSub?.Dispose();
            _gameplaySub?.Dispose();
            _pauseSub?.Dispose();
            _resumeSub?.Dispose();
            _restartSub?.Dispose();
            _gameOverSub?.Dispose();
        }

        #region Enter / Exit

        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.Pause:
                    _cachedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                    Time.timeScale = 0f;
                    break;
                case GameState.Gameplay:
                    Time.timeScale = _cachedTimeScale > 0f ? _cachedTimeScale : 1f;
                    break;
                case GameState.Menu:
                case GameState.GameOver:
                    Time.timeScale = 1f;
                    break;
            }
        }

        private void ExitState(GameState state)
        {
            // Хуки выхода при необходимости (аналитика, сохранение — через события)
        }

        #endregion

        #region Подписки на запросы

        private void SubscribeToRequests()
        {
            _menuSub = _eventBus.Subscribe<RequestMenuEvent>(_ => ChangeState(GameState.Menu));
            _gameplaySub = _eventBus.Subscribe<RequestGameplayEvent>(_ =>
            {
                if (CurrentState == GameState.Pause)
                {
                    // Рестарт из паузы: сначала в Gameplay через Resume-путь не подходит —
                    // используем прямой переход Pause → Gameplay, затем уровень перезапустит LevelService.
                    ChangeState(GameState.Gameplay);
                    return;
                }

                ChangeState(GameState.Gameplay);
            });
            _pauseSub = _eventBus.Subscribe<RequestPauseEvent>(_ =>
            {
                if (CurrentState == GameState.Gameplay)
                {
                    ChangeState(GameState.Pause);
                }
            });
            _resumeSub = _eventBus.Subscribe<RequestResumeEvent>(_ =>
            {
                if (CurrentState == GameState.Pause)
                {
                    ChangeState(GameState.Gameplay);
                }
            });
            _restartSub = _eventBus.Subscribe<RequestRestartLevelEvent>(_ =>
            {
                if (CurrentState == GameState.Pause || CurrentState == GameState.GameOver)
                {
                    ChangeState(GameState.Gameplay);
                }
            });
            _gameOverSub = _eventBus.Subscribe<RequestGameOverEvent>(_ =>
            {
                if (CurrentState == GameState.Gameplay)
                {
                    ChangeState(GameState.GameOver);
                }
            });
        }

        #endregion
    }
}
