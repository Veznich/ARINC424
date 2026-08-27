#if UNITY_EDITOR
using Arkanoid.Configs;
using UnityEditor;
using UnityEngine;

namespace Arkanoid.StudioEditor
{
    /// <summary>
    /// Меню: создать / сбросить / валидировать конфиги MVP.
    /// </summary>
    public static class ConfigAssetsMenu
    {
        private const string CONFIGS_PATH = "Assets/_Project/Configs";
        private const string CATALOG_PATH = CONFIGS_PATH + "/GameConfigCatalog.asset";

        [MenuItem("Arkanoid/Configs/Create All Default Configs")]
        public static void CreateAllDefaults()
        {
            EnsureFolder(CONFIGS_PATH);

            var ball = CreateAsset<BallConfig>($"{CONFIGS_PATH}/BallConfig.asset");
            var paddle = CreateAsset<PaddleConfig>($"{CONFIGS_PATH}/PaddleConfig.asset");
            var level = CreateAsset<LevelConfig>($"{CONFIGS_PATH}/LevelConfig.asset");
            var powerUp = CreateAsset<PowerUpConfig>($"{CONFIGS_PATH}/PowerUpConfig.asset");
            var difficulty = CreateAsset<DifficultyConfig>($"{CONFIGS_PATH}/DifficultyConfig.asset");
            var combo = CreateAsset<ComboConfig>($"{CONFIGS_PATH}/ComboConfig.asset");
            var player = CreateAsset<PlayerConfig>($"{CONFIGS_PATH}/PlayerConfig.asset");

            var catalog = CreateAsset<GameConfigCatalog>(CATALOG_PATH);
            catalog.ball = ball;
            catalog.paddle = paddle;
            catalog.level = level;
            catalog.powerUp = powerUp;
            catalog.difficulty = difficulty;
            catalog.combo = combo;
            catalog.player = player;
            EditorUtility.SetDirty(catalog);

            MvpConfigDefaults.ApplyAll(catalog);
            MarkDirty(ball, paddle, level, powerUp, difficulty, combo, player, catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
            Debug.Log("[Arkanoid] Конфиги созданы / обновлены в " + CONFIGS_PATH);
        }

        [MenuItem("Arkanoid/Configs/Apply MVP Defaults")]
        public static void ApplyMvpDefaults()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameConfigCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                Debug.LogError("[Arkanoid] Нет GameConfigCatalog. Сначала Create All Default Configs.");
                return;
            }

            MvpConfigDefaults.ApplyAll(catalog);
            MarkDirty(
                catalog.ball,
                catalog.paddle,
                catalog.level,
                catalog.powerUp,
                catalog.difficulty,
                catalog.combo,
                catalog.player,
                catalog);

            AssetDatabase.SaveAssets();
            Debug.Log("[Arkanoid] MVP defaults применены ко всем SO в каталоге.");
            Selection.activeObject = catalog;
        }

        [MenuItem("Arkanoid/Configs/Validate Catalog")]
        public static void ValidateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameConfigCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                Debug.LogError("[Arkanoid] GameConfigCatalog не найден: " + CATALOG_PATH);
                return;
            }

            if (!catalog.IsValid(out var error))
            {
                Debug.LogError("[Arkanoid] Catalog invalid: " + error);
                return;
            }

            var b = catalog.ball;
            var p = catalog.paddle;
            var l = catalog.level;
            var pu = catalog.powerUp;
            var d = catalog.difficulty;
            var pl = catalog.player;

            Debug.Log(
                "[Arkanoid] Catalog OK · " +
                $"Ball {b.baseSpeed}/{b.maxSpeed} · " +
                $"Paddle maxX={p.maxX} · " +
                $"Level L1={l.startBlockCount}+{l.blocksPerLevel} cap={l.maxBlockCount} tiers={l.maxBlockTier} · " +
                $"Drop={pu.dropChance:F2} multi={pu.multiBallSpawnCount}/{pu.maxBalls} · " +
                $"Diff levelHp={(d.useLevelExtraHpOnBlocks ? "on" : "off")} · " +
                $"Lives {pl.startLives}/{pl.maxLives}");
            Selection.activeObject = catalog;
        }

        private static void MarkDirty(params Object[] objects)
        {
            foreach (var o in objects)
            {
                if (o != null)
                {
                    EditorUtility.SetDirty(o);
                }
            }
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
