using Arkanoid.Analytics;
using Arkanoid.Core;
using UnityEngine;
using VContainer;

namespace Arkanoid.Save
{
    /// <summary>
    /// Автосохранение при выходе и сворачивании. Также ставит паузу при потере фокуса.
    /// </summary>
    public sealed class SaveLifecycleBehaviour : MonoBehaviour
    {
        private ISaveService _saveService;
        private IAnalyticsService _analytics;
        private IEventBus _eventBus;
        private IGameStateMachine _stateMachine;

        [Inject]
        public void Construct(
            ISaveService saveService,
            IEventBus eventBus,
            IGameStateMachine stateMachine,
            IAnalyticsService analytics)
        {
            _saveService = saveService;
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _analytics = analytics;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Persist();
                TryAutoPause();
            }
        }

        private void OnApplicationQuit()
        {
            Persist();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Persist();
                TryAutoPause();
            }
        }

        private void Persist()
        {
            _saveService?.Save();
            _analytics?.Flush();
        }

        /// <summary>Авто-пауза при звонке / сворачивании во время геймплея.</summary>
        private void TryAutoPause()
        {
            if (_stateMachine == null || _eventBus == null)
            {
                return;
            }

            if (_stateMachine.CurrentState == GameState.Gameplay)
            {
                _eventBus.Publish(new RequestPauseEvent());
            }
        }
    }
}
