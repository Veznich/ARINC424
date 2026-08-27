#if UNITY_EDITOR
using Arkanoid.Configs;
using UnityEditor;
using UnityEngine;

namespace Arkanoid.StudioEditor
{
    /// <summary>
    /// Меню: создать все конфиги MVP и каталог одной командой.
    /// </summary>
    public static class ConfigAssetsMenu
    {
        private const string CONFIGS_PATH = "Assets/_Project/Configs";

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

            var catalog = CreateAsset<GameConfigCatalog>($"{CONFIGS_PATH}/GameConfigCatalog.asset");
            catalog.ball = ball;
            catalog.paddle = paddle;
            catalog.level = level;
            catalog.powerUp = powerUp;
            catalog.difficulty = difficulty;
            catalog.combo = combo;
            catalog.player = player;
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
            Debug.Log("[Arkanoid] Конфиги созданы в " + CONFIGS_PATH);
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
