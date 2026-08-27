using Arkanoid.Configs;
using Arkanoid.Core;
using Arkanoid.Gameplay;
using Arkanoid.Save;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid.Core
{
    /// <summary>
    /// Корневой DI-скоуп проекта. Повесить на GameObject «ProjectContext» в bootstrap-сцене.
    /// На тот же объект добавить SaveLifecycleBehaviour.
    /// </summary>
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [Header("Конфиги")]
        [SerializeField]
        private GameConfigCatalog configCatalog;

        [Header("Lifecycle")]
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        protected override void Awake()
        {
            if (dontDestroyOnLoad && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            // Гарантируем компонент автосейва на этом же объекте
            if (GetComponent<SaveLifecycleBehaviour>() == null)
            {
                gameObject.AddComponent<SaveLifecycleBehaviour>();
            }

            EnsureCatalogAssigned();
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            Application.targetFrameRate = GameDefaults.TARGET_FRAME_RATE;
            QualitySettings.vSyncCount = 0;

            EnsureCatalogAssigned();
            ValidateCatalog();

            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<GameStateMachine>(Lifetime.Singleton)
                .As<IGameStateMachine>()
                .AsSelf();
            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);
            builder.Register<LevelGenerator>(Lifetime.Singleton);
            // Lives до Difficulty — DI + «с 1-й попытки» по реальному −жизнь
            builder.RegisterEntryPoint<LivesService>().AsSelf();
            builder.RegisterEntryPoint<Arkanoid.Difficulty.DifficultyDirector>().AsSelf();
            builder.RegisterEntryPoint<LevelService>().AsSelf();

            RegisterConfigs(builder);

            builder.RegisterEntryPoint<GameBootstrap>();
            builder.RegisterComponentInHierarchy<SaveLifecycleBehaviour>();
        }

        #region Config registration

        private void EnsureCatalogAssigned()
        {
            if (configCatalog != null)
            {
                return;
            }

#if UNITY_EDITOR
            configCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfigCatalog>(
                "Assets/_Project/Configs/GameConfigCatalog.asset");
#endif
            if (configCatalog == null)
            {
                configCatalog = Resources.Load<GameConfigCatalog>("GameConfigCatalog");
            }
        }

        private void ValidateCatalog()
        {
            if (configCatalog == null)
            {
                Debug.LogError(
                    "[ProjectLifetimeScope] GameConfigCatalog не назначен. " +
                    "Создай через Arkanoid → Configs → Create All Default Configs и назначь в инспекторе.");
                return;
            }

            if (!configCatalog.IsValid(out var error))
            {
                Debug.LogError($"[ProjectLifetimeScope] {error}");
            }
        }

        private void RegisterConfigs(IContainerBuilder builder)
        {
            if (configCatalog == null)
            {
                return;
            }

            builder.RegisterInstance(configCatalog);
            RegisterIfNotNull(builder, configCatalog.ball);
            RegisterIfNotNull(builder, configCatalog.paddle);
            RegisterIfNotNull(builder, configCatalog.level);
            RegisterIfNotNull(builder, configCatalog.powerUp);
            RegisterIfNotNull(builder, configCatalog.difficulty);
            RegisterIfNotNull(builder, configCatalog.combo);
            RegisterIfNotNull(builder, configCatalog.player);
        }

        private static void RegisterIfNotNull<T>(IContainerBuilder builder, T instance) where T : class
        {
            if (instance != null)
            {
                builder.RegisterInstance(instance);
            }
        }

        #endregion
    }
}
