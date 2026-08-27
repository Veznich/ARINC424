using System;
using System.Collections.Generic;
using System.IO;
using Arkanoid.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Arkanoid.Replay
{
    /// <summary>Хранение до MAX_STORED_REPLAYS replay в persistentDataPath/replays.</summary>
    public sealed class ReplayStore
    {
        private readonly string _dir;
        private readonly string _indexPath;
        private readonly JsonSerializerSettings _json;

        public ReplayStore()
        {
            _dir = Path.Combine(Application.persistentDataPath, "replays");
            _indexPath = Path.Combine(_dir, "index.json");
            _json = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
            Directory.CreateDirectory(_dir);
        }

        public string DirectoryPath => _dir;

        public void Save(ReplayData data)
        {
            if (data == null || data.frames == null || data.frames.Count == 0)
            {
                return;
            }

            Directory.CreateDirectory(_dir);
            var path = Path.Combine(_dir, data.id + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(data, _json));

            var index = LoadIndex();
            index.ids.Remove(data.id);
            index.ids.Insert(0, data.id);
            while (index.ids.Count > GameDefaults.MAX_STORED_REPLAYS)
            {
                var drop = index.ids[index.ids.Count - 1];
                index.ids.RemoveAt(index.ids.Count - 1);
                var dropPath = Path.Combine(_dir, drop + ".json");
                if (File.Exists(dropPath))
                {
                    File.Delete(dropPath);
                }
            }

            File.WriteAllText(_indexPath, JsonConvert.SerializeObject(index, _json));
            Debug.Log($"[ReplayStore] Saved {data.id} · L{data.levelNumber} · frames {data.frames.Count} → {path}");
        }

        public ReplayData LoadLatest()
        {
            var index = LoadIndex();
            if (index.ids.Count == 0)
            {
                return null;
            }

            return LoadById(index.ids[0]);
        }

        public ReplayData LoadById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var path = Path.Combine(_dir, id + ".json");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<ReplayData>(File.ReadAllText(path), _json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayStore] Load failed: {ex.Message}");
                return null;
            }
        }

        public string ExportLatest()
        {
            var data = LoadLatest();
            if (data == null)
            {
                return null;
            }

            var name = $"export_{data.levelNumber}_{data.id}.json";
            var path = Path.Combine(_dir, name);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, _json));
            Debug.Log($"[ReplayStore] Exported → {path}");
            return path;
        }

        public IReadOnlyList<string> ListIds()
        {
            return LoadIndex().ids;
        }

        private ReplayIndex LoadIndex()
        {
            if (!File.Exists(_indexPath))
            {
                return new ReplayIndex();
            }

            try
            {
                return JsonConvert.DeserializeObject<ReplayIndex>(File.ReadAllText(_indexPath), _json)
                       ?? new ReplayIndex();
            }
            catch
            {
                return new ReplayIndex();
            }
        }
    }
}
