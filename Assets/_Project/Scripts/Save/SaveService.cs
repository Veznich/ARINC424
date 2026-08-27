using System;
using System.IO;
using Arkanoid.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Arkanoid.Save
{
    /// <summary>
    /// Контракт системы сохранений (JSON в persistentDataPath).
    /// </summary>
    public interface ISaveService
    {
        SaveData Data { get; }

        /// <summary>Загрузить с диска или создать дефолт.</summary>
        void Load();

        /// <summary>Записать текущие данные на диск.</summary>
        void Save();

        /// <summary>Сбросить прогресс к дефолтам и сохранить.</summary>
        void ResetProgress();
    }

    /// <summary>
    /// JSON-сохранения через Newtonsoft. Автосейв при Quit / Pause приложения.
    /// </summary>
    public sealed class SaveService : ISaveService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly string _filePath;
        private readonly JsonSerializerSettings _serializerSettings;

        private bool _isLoaded;

        public SaveData Data { get; private set; } = SaveData.CreateDefault();

        public SaveService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _filePath = Path.Combine(Application.persistentDataPath, GameDefaults.SAVE_FILE_NAME);
            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include
            };
        }

        /// <inheritdoc />
        public void Load()
        {
            if (!File.Exists(_filePath))
            {
                Data = SaveData.CreateDefault();
                // Важно: _isLoaded ДО Save(), иначе Save() снова вызовет Load() → StackOverflow.
                _isLoaded = true;
                Save();
                _eventBus.Publish(new SaveLoadedEvent(wasCreatedNew: true));
                Debug.Log($"[SaveService] Создано новое сохранение: {_filePath}");
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                Data = JsonConvert.DeserializeObject<SaveData>(json, _serializerSettings) ?? SaveData.CreateDefault();
                Sanitize(Data);
                _isLoaded = true;
                _eventBus.Publish(new SaveLoadedEvent(wasCreatedNew: false));
                Debug.Log($"[SaveService] Загружено: {_filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Ошибка загрузки, используем дефолт. {ex.Message}");
                Data = SaveData.CreateDefault();
                _isLoaded = true;
                _eventBus.Publish(new SaveLoadedEvent(wasCreatedNew: true));
            }
        }

        /// <inheritdoc />
        public void Save()
        {
            if (!_isLoaded)
            {
                Data = SaveData.CreateDefault();
                _isLoaded = true;
            }

            try
            {
                Sanitize(Data);
                var json = JsonConvert.SerializeObject(Data, _serializerSettings);
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_filePath, json);
                _eventBus.Publish(new SaveCompletedEvent());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Ошибка записи: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void ResetProgress()
        {
            Data = SaveData.CreateDefault();
            _isLoaded = true;
            Save();
        }

        public void Dispose()
        {
            // Нет долгоживущих подписок; автосейв через SaveLifecycleBehaviour
        }

        #region Валидация

        private static void Sanitize(SaveData data)
        {
            if (data.currentLevel < 1)
            {
                data.currentLevel = GameDefaults.DEFAULT_LEVEL;
            }

            if (data.lives < 0)
            {
                data.lives = 0;
            }

            if (data.lives > GameDefaults.MAX_LIVES)
            {
                data.lives = GameDefaults.MAX_LIVES;
            }

            if (data.unlockedSkins == null || data.unlockedSkins.Count == 0)
            {
                data.unlockedSkins = new System.Collections.Generic.List<string> { GameDefaults.DEFAULT_SKIN_ID };
            }

            if (string.IsNullOrEmpty(data.currentSkin))
            {
                data.currentSkin = GameDefaults.DEFAULT_SKIN_ID;
            }

            if (data.unlockedAchievements == null)
            {
                data.unlockedAchievements = new System.Collections.Generic.List<string>();
            }

            if (data.playerStats == null)
            {
                data.playerStats = new System.Collections.Generic.Dictionary<string, int>();
            }

            if (data.metaUpgrades == null)
            {
                data.metaUpgrades = new MetaUpgradeData();
            }

            if (data.settings == null)
            {
                data.settings = new SettingsData();
            }
        }

        #endregion
    }
}
