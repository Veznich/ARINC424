using System.IO;
using UnityEngine;

namespace Arkanoid.Core
{
    /// <summary>
    /// Временный smoke-тест Core в Play Mode без UI.
    /// Добавить на любой объект сцены и смотреть Console / persistentDataPath.
    /// </summary>
    public sealed class CoreSmokeTest : MonoBehaviour
    {
        [SerializeField]
        private bool runOnStart = true;

        private void Start()
        {
            if (!runOnStart)
            {
                return;
            }

            Run();
        }

        /// <summary>Проверить seed, путь сейва и targetFrameRate.</summary>
        [ContextMenu("Run Core Smoke Test")]
        public void Run()
        {
            var seed1 = Utils.SeedGenerator.ComputeSeed(1);
            var seed5 = Utils.SeedGenerator.ComputeSeed(5);
            var path = Path.Combine(Application.persistentDataPath, GameDefaults.SAVE_FILE_NAME);

            Debug.Log($"[CoreSmokeTest] FPS target={Application.targetFrameRate}");
            Debug.Log($"[CoreSmokeTest] Seed L1={seed1}, L5={seed5}");
            Debug.Log($"[CoreSmokeTest] Save path={path}, exists={File.Exists(path)}");
        }
    }
}
