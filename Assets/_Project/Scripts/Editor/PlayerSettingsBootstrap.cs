using UnityEditor;
using UnityEngine;

namespace Arkanoid.StudioEditor
{
    /// <summary>
    /// При открытии проекта: New Input System only + Dynamic Batching OFF.
    /// Unity 6.5: без PlayerSettings.SetBatchingForPlatform (API удалён).
    /// </summary>
    [InitializeOnLoad]
    public static class PlayerSettingsBootstrap
    {
        private const string SESSION_KEY = "Arkanoid.PlayerSettingsBootstrap.Done";
        private const string PROJECT_SETTINGS_PATH = "ProjectSettings/ProjectSettings.asset";

        static PlayerSettingsBootstrap()
        {
            EditorApplication.delayCall += ApplyOnce;
        }

        private static void ApplyOnce()
        {
            if (SessionState.GetBool(SESSION_KEY, false))
            {
                return;
            }

            SessionState.SetBool(SESSION_KEY, true);
            Apply();
        }

        /// <summary>Применить настройки Player вручную.</summary>
        [MenuItem("Arkanoid/Project/Apply Player Settings (Input + Batching)")]
        public static void Apply()
        {
            var so = LoadPlayerSettings();
            if (so == null)
            {
                return;
            }

            SetActiveInputHandler(so, 1);
            DisableDynamicBatching(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            MobileDebugViewMenu.ApplyPortraitPlayerSettings();
            Debug.Log("[Arkanoid] Active Input Handling = Input System Package; Dynamic Batching = OFF; Portrait.");
        }

        private static SerializedObject LoadPlayerSettings()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(PROJECT_SETTINGS_PATH);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Arkanoid] ProjectSettings.asset не найден.");
                return null;
            }

            return new SerializedObject(assets[0]);
        }

        private static void SetActiveInputHandler(SerializedObject so, int value)
        {
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null)
            {
                Debug.LogWarning("[Arkanoid] Свойство activeInputHandler не найдено.");
                return;
            }

            prop.intValue = value;
        }

        private static void DisableDynamicBatching(SerializedObject so)
        {
            var batching = so.FindProperty("m_BuildTargetBatching");
            if (batching == null || !batching.isArray)
            {
                Debug.LogWarning("[Arkanoid] m_BuildTargetBatching не найден — Dynamic Batching не изменён.");
                return;
            }

            for (var i = 0; i < batching.arraySize; i++)
            {
                var element = batching.GetArrayElementAtIndex(i);
                var dynamicProp = element.FindPropertyRelative("m_DynamicBatching");
                if (dynamicProp != null)
                {
                    dynamicProp.intValue = 0;
                }

                var staticProp = element.FindPropertyRelative("m_StaticBatching");
                if (staticProp != null)
                {
                    staticProp.intValue = 1;
                }
            }
        }
    }
}
