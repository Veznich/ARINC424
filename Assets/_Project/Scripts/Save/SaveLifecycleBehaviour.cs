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
        private IEventBus _eventBus;
        private IGameStateMachine _stateMachine;

        [Inject]
        public void Construct(ISaveService saveService, IEventBus eventBus, IGameStateMachine stateMachine)
        {
            _saveService = saveService;
            _eventBus = eventBus;
            _stateMachine = stateMachine;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _saveService?.Save();
                TryAutoPause();
            }
        }

        private void OnApplicationQuit()
        {
            _saveService?.Save();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _saveService?.Save();
                TryAutoPause();
            }
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
