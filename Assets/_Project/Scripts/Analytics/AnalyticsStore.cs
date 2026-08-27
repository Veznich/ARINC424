using System;
using System.IO;
using Arkanoid.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Arkanoid.Analytics
{
    /// <summary>Чтение/запись analytics.json в persistentDataPath.</summary>
    public sealed class AnalyticsStore
    {
        private readonly string _path;
        private readonly JsonSerializerSettings _json;

        public string FilePath => _path;

        public AnalyticsStore()
        {
            _path = Path.Combine(Application.persistentDataPath, GameDefaults.ANALYTICS_FILE_NAME);
            _json = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        public AnalyticsData Load()
        {
            if (!File.Exists(_path))
            {
                return AnalyticsData.CreateDefault();
            }

            try
            {
                var data = JsonConvert.DeserializeObject<AnalyticsData>(File.ReadAllText(_path), _json);
                if (data == null)
                {
                    return AnalyticsData.CreateDefault();
                }

                data.counters ??= new AnalyticsCounters();
                data.events ??= new System.Collections.Generic.List<AnalyticsEventDto>(128);
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AnalyticsStore] Load failed: {ex.Message}");
                return AnalyticsData.CreateDefault();
            }
        }

        public void Save(AnalyticsData data)
        {
            if (data == null)
            {
                return;
            }

            try
            {
                data.lastUpdatedUtc = DateTime.UtcNow.ToString("o");
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllText(_path, JsonConvert.SerializeObject(data, _json));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AnalyticsStore] Save failed: {ex.Message}");
            }
        }
    }
}
