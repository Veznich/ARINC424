using System;
using Arkanoid.Save;
using UnityEngine;
using VContainer.Unity;

namespace Arkanoid.Core
{
    /// <summary>
    /// Точка входа после построения DI: загрузка сейва и старт FSM в Menu.
    /// </summary>
    public sealed class GameBootstrap : IStartable, IDisposable
    {
        private readonly ISaveService _saveService;
        private readonly GameStateMachine _stateMachine;

        public GameBootstrap(ISaveService saveService, GameStateMachine stateMachine)
        {
            _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        }

        /// <summary>Загрузка сохранения и переход в Menu.</summary>
        public void Start()
        {
            Application.targetFrameRate = GameDefaults.TARGET_FRAME_RATE;
            _saveService.Load();
            _stateMachine.Bootstrap();
            Debug.Log("[GameBootstrap] Core готов: Save загружен, состояние Menu.");
        }

        public void Dispose()
        {
            _stateMachine.Dispose();
        }
    }
}
