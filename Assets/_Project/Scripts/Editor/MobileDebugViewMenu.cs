#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arkanoid.StudioEditor
{
    /// <summary>
    /// Game View + Player Settings под портретный мобильный экран (1080×1920 / 9:16).
    /// </summary>
    public static class MobileDebugViewMenu
    {
        public const int PhoneWidth = 1080;
        public const int PhoneHeight = 1920;
        public const string SizeLabel = "Arkanoid Phone 1080x1920";

        private static Assembly EditorAssembly => typeof(EditorWindow).Assembly;

        /// <summary>Портрет + Game View 1080×1920 + ortho-камера под 9:16.</summary>
        [MenuItem("Arkanoid/Project/Setup Mobile Debug View (Portrait 1080x1920)")]
        public static void SetupAll()
        {
            ApplyPortraitPlayerSettings();
            EnsureAndSelectGameViewSize(PhoneWidth, PhoneHeight, SizeLabel);
            TuneMainCameraForPortrait();
            Debug.Log(
                "[Arkanoid] Mobile debug: Portrait, Game View " +
                $"{PhoneWidth}x{PhoneHeight}, камера подогнана под 9:16. Открой вкладку Game.");
        }

        [MenuItem("Arkanoid/Project/Apply Portrait Orientation Only")]
        public static void ApplyPortraitPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.defaultScreenWidth = PhoneWidth;
            PlayerSettings.defaultScreenHeight = PhoneHeight;

            Debug.Log("[Arkanoid] PlayerSettings: Portrait only, default " +
                      $"{PhoneWidth}x{PhoneHeight}.");
        }

        [MenuItem("Arkanoid/Project/Select Game View Phone Aspect")]
        public static void SelectGameViewOnly()
        {
            EnsureAndSelectGameViewSize(PhoneWidth, PhoneHeight, SizeLabel);
            Debug.Log($"[Arkanoid] Game View → {SizeLabel}.");
        }

        public static void TuneMainCameraForPortrait(Camera cam = null)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam == null)
            {
                Debug.LogWarning("[Arkanoid] Main Camera не найдена — ortho не настроен.");
                return;
            }

            cam.orthographic = true;
            cam.orthographicSize = 9.5f;
            // Согласовано с PlayfieldLayout (лёгкий pitch)
            cam.transform.position = new Vector3(0f, 1.2f, -12f);
            cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
            EditorUtility.SetDirty(cam);
        }

        private static void EnsureAndSelectGameViewSize(int width, int height, string label)
        {
            try
            {
                var sizesType = EditorAssembly.GetType("UnityEditor.GameViewSizes");
                var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instanceProp = singleType.GetProperty("instance");
                var instance = instanceProp.GetValue(null, null);

                var currentGroupProp = sizesType.GetProperty("currentGroup");
                var group = currentGroupProp.GetValue(instance, null);
                var groupType = group.GetType();

                var getTotalCount = groupType.GetMethod("GetTotalCount");
                var getGameViewSize = groupType.GetMethod("GetGameViewSize");
                var addCustomSize = groupType.GetMethod("AddCustomSize");

                var index = FindSizeIndex(group, getTotalCount, getGameViewSize, width, height, label);
                if (index < 0)
                {
                    var gameViewSizeType = EditorAssembly.GetType("UnityEditor.GameViewSize");
                    var gameViewSizeTypeEnum = EditorAssembly.GetType("UnityEditor.GameViewSizeType");
                    var fixedRes = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
                    var ctor = gameViewSizeType.GetConstructor(new[]
                    {
                        gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string)
                    });
                    var newSize = ctor.Invoke(new object[] { fixedRes, width, height, label });
                    addCustomSize.Invoke(group, new[] { newSize });
                    index = FindSizeIndex(group, getTotalCount, getGameViewSize, width, height, label);
                }

                if (index >= 0)
                {
                    SetGameViewSizeIndex(index);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[Arkanoid] Не удалось выставить Game View через reflection: " + ex.Message +
                    ". Вручную: Game → Free Aspect → + → 1080 × 1920.");
            }
        }

        private static int FindSizeIndex(
            object group,
            MethodInfo getTotalCount,
            MethodInfo getGameViewSize,
            int width,
            int height,
            string label)
        {
            var total = (int)getTotalCount.Invoke(group, null);
            for (var i = 0; i < total; i++)
            {
                var size = getGameViewSize.Invoke(group, new object[] { i });
                var sizeType = size.GetType();
                var w = (int)sizeType.GetProperty("width").GetValue(size, null);
                var h = (int)sizeType.GetProperty("height").GetValue(size, null);
                var text = (string)sizeType.GetProperty("baseText").GetValue(size, null);
                if ((w == width && h == height) || text == label)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void SetGameViewSizeIndex(int index)
        {
            var gameViewType = EditorAssembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            var sizeSelectionCallback = gameViewType.GetMethod(
                "SizeSelectionCallback",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (sizeSelectionCallback != null)
            {
                sizeSelectionCallback.Invoke(window, new object[] { index, null });
                window.Repaint();
                return;
            }

            var selectedProp = gameViewType.GetProperty(
                "selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedProp != null && selectedProp.CanWrite)
            {
                selectedProp.SetValue(window, index, null);
                window.Repaint();
            }
        }
    }
}
#endif
