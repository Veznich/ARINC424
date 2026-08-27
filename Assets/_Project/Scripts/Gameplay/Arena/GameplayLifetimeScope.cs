using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Difficulty;
using Arkanoid.Input;
using Arkanoid.Replay;
using Arkanoid.Analytics;
using Arkanoid.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid.Gameplay
{
    /// <summary>
    /// LifetimeScope геймплей-арены. Parent = ProjectLifetimeScope.
    /// </summary>
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField]
        private GameplayInputReader inputReader;

        [SerializeField]
        private PaddleController paddle;

        [SerializeField]
        private BallController ball;

        [SerializeField]
        private PlayfieldBounds bounds;

        [SerializeField]
        private BlockField blockField;

        [SerializeField]
        private Camera gameplayCamera;

        [SerializeField]
        private bool autoStartGameplay = true;

        protected override void Configure(IContainerBuilder builder)
        {
            if (inputReader == null)
            {
                inputReader = GetComponentInChildren<GameplayInputReader>(true);
            }

            if (paddle == null)
            {
                paddle = GetComponentInChildren<PaddleController>(true);
            }

            if (ball == null)
            {
                ball = GetComponentInChildren<BallController>(true);
            }

            if (bounds == null)
            {
                bounds = GetComponentInChildren<PlayfieldBounds>(true);
            }

            if (blockField == null)
            {
                blockField = GetComponentInChildren<BlockField>(true);
            }

            if (blockField == null)
            {
                blockField = gameObject.AddComponent<BlockField>();
            }

            var inputRouter = GetComponent<GameplayInputRouter>();
            if (inputRouter == null)
            {
                inputRouter = gameObject.AddComponent<GameplayInputRouter>();
            }

            if (inputReader != null)
            {
                inputRouter.BindLive(inputReader);
                builder.RegisterComponent(inputRouter).As<IGameplayInput>();
            }

            if (paddle != null)
            {
                builder.RegisterComponent(paddle);
            }

            if (ball != null)
            {
                builder.RegisterComponent(ball);
            }

            if (bounds != null)
            {
                builder.RegisterComponent(bounds);
            }

            builder.RegisterComponent(blockField);

            builder.RegisterBuildCallback(WireArena);
        }

        private void WireArena(IObjectResolver container)
        {
            if (!container.TryResolve<PaddleConfig>(out var paddleConfig) ||
                !container.TryResolve<BallConfig>(out var ballConfig) ||
                !container.TryResolve<IGameplayInput>(out var input) ||
                !container.TryResolve<IGameStateMachine>(out var stateMachine) ||
                !container.TryResolve<IEventBus>(out var eventBus))
            {
                Debug.LogError(
                    "[GameplayArena] Не удалось Resolve конфиги/сервисы. " +
                    "Проверь Parent = ProjectLifetimeScope и GameConfigCatalog.");
                return;
            }

            container.TryResolve<LevelConfig>(out var levelConfig);
            container.TryResolve<DifficultyDirector>(out var difficulty);
            container.TryResolve<ReplayService>(out var replay);

            var cam = gameplayCamera != null ? gameplayCamera : Camera.main;

            if (inputReader != null)
            {
                inputReader.Configure(paddleConfig);
                inputReader.SetCamera(cam);
            }

            if (input is GameplayInputRouter router)
            {
                router.BindLive(inputReader);
                router.SetCamera(cam);
                replay?.Bind(router);
                router.BindReplay(replay);
            }

            if (paddle != null)
            {
                paddle.Configure(paddleConfig, input, stateMachine);
            }

            if (blockField != null && levelConfig != null)
            {
                blockField.Configure(levelConfig, eventBus, bounds, difficulty);
            }

            container.TryResolve<PowerUpConfig>(out var powerUpConfig);
            container.TryResolve<PlayerConfig>(out var playerConfig);
            container.TryResolve<LivesService>(out var livesService);

            var powerUps = GetComponent<PowerUpController>();
            if (powerUps == null)
            {
                powerUps = gameObject.AddComponent<PowerUpController>();
            }

            if (powerUpConfig != null)
            {
                powerUps.Configure(
                    powerUpConfig,
                    paddleConfig,
                    playerConfig,
                    eventBus,
                    stateMachine,
                    paddle,
                    ball,
                    blockField,
                    bounds,
                    livesService,
                    difficulty);
            }

            if (ball != null)
            {
                ball.Configure(
                    ballConfig,
                    paddleConfig,
                    input,
                    stateMachine,
                    eventBus,
                    paddle,
                    bounds,
                    blockField,
                    levelConfig,
                    powerUps,
                    difficulty);
            }

            GameplayVisualBootstrap.Apply(
                transform,
                cam,
                paddle,
                ball,
                bounds);

            EnsureHud(container, eventBus, stateMachine, cam);

            var timersHud = GetComponentInChildren<PowerUpTimersHud>(true);
            if (timersHud == null)
            {
                var go = new GameObject("PowerUpTimers");
                go.transform.SetParent(transform, false);
                timersHud = go.AddComponent<PowerUpTimersHud>();
            }

            timersHud.Configure(eventBus);

            if (autoStartGameplay)
            {
                var auto = GetComponent<GameplayAutoStart>();
                if (auto == null)
                {
                    auto = gameObject.AddComponent<GameplayAutoStart>();
                }

                auto.Arm(eventBus, stateMachine, levelNumber: 1);
            }

            Debug.Log("[GameplayArena] Этап 8 готов: SO defaults locked.");
        }

        private void EnsureHud(
            IObjectResolver container,
            IEventBus eventBus,
            IGameStateMachine stateMachine,
            Camera cam)
        {
            var hud = GetComponentInChildren<GameplayHudView>(true);
            if (hud == null)
            {
                var go = new GameObject("HUD");
                go.transform.SetParent(transform, false);
                hud = go.AddComponent<GameplayHudView>();
            }

            var lives = 3;
            var max = 5;
            var level = 1;
            if (container.TryResolve<LivesService>(out var livesService))
            {
                lives = livesService.Current;
                max = livesService.Max;
            }
            else if (container.TryResolve<PlayerConfig>(out var playerConfig) && playerConfig != null)
            {
                lives = playerConfig.startLives;
                max = playerConfig.maxLives;
            }

            if (container.TryResolve<LevelService>(out var levelService))
            {
                level = levelService.CurrentLevel;
            }

            container.TryResolve<DifficultyDirector>(out var difficulty);
            container.TryResolve<ReplayService>(out var replay);
            container.TryResolve<IAnalyticsService>(out var analytics);
            hud.Configure(eventBus, stateMachine, lives, max, level, difficulty, cam, replay, analytics);
            EnsureGameOverButton(eventBus, stateMachine, cam);
        }

        private void EnsureGameOverButton(
            IEventBus eventBus,
            IGameStateMachine stateMachine,
            Camera cam)
        {
            var btn = GetComponentInChildren<GameOverRestartButton>(true);
            if (btn == null)
            {
                var go = new GameObject("GameOverUI");
                go.transform.SetParent(transform, false);
                btn = go.AddComponent<GameOverRestartButton>();
            }

            btn.Configure(eventBus, stateMachine, cam);
        }
    }

    /// <summary>
    /// Ждёт Menu (после GameBootstrap) и публикует RequestGameplay.
    /// Update-poll надёжнее coroutine при AddComponent во время Awake.
    /// </summary>
    public sealed class GameplayAutoStart : MonoBehaviour
    {
        private IEventBus _eventBus;
        private IGameStateMachine _stateMachine;
        private int _level = 1;
        private bool _armed;
        private bool _published;

        public void Arm(IEventBus eventBus, IGameStateMachine stateMachine, int levelNumber)
        {
            _eventBus = eventBus;
            _stateMachine = stateMachine;
            _level = levelNumber;
            _armed = true;
            _published = false;
        }

        private void Update()
        {
            if (!_armed || _published || _eventBus == null || _stateMachine == null)
            {
                return;
            }

            if (_stateMachine.CurrentState != GameState.Menu)
            {
                return;
            }

            _eventBus.Publish(new RequestGameplayEvent(_level));
            _published = true;
            _armed = false;
            Debug.Log("[GameplayArena] AutoStart → Gameplay (level " + _level + ")");
        }
    }
}
