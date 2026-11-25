using System;
using System.IO;
using UnityEngine;

namespace VirtualLife.SaveSystem
{
    /// <summary>JSON-file implementation of ISaveService (JsonUtility, persistentDataPath/Saves).</summary>
    public sealed class JsonFileSaveService : ISaveService
    {
        public string RootPath { get; }

        public JsonFileSaveService(string rootDirectory = null)
        {
            RootPath = rootDirectory ?? Path.Combine(Application.persistentDataPath, "Saves");
        }

        public bool Exists(string key) => TryGetPath(key, out string path) && File.Exists(path);

        public bool Save<T>(string key, T data)
        {
            if (!TryGetPath(key, out string path)) return false;
            try
            {
                Directory.CreateDirectory(RootPath);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path); // write-then-move avoids half-written save files
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to save '{key}': {e.Message}");
                return false;
            }
        }

        public bool TryLoad<T>(string key, out T data)
        {
            data = default;
            if (!TryGetPath(key, out string path) || !File.Exists(path)) return false;
            try
            {
                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to load '{key}': {e.Message}");
                return false;
            }
        }

        public bool Delete(string key)
        {
            if (!TryGetPath(key, out string path) || !File.Exists(path)) return false;
            try { File.Delete(path); return true; }
            catch (Exception e) { Debug.LogError($"[Save] Failed to delete '{key}': {e.Message}"); return false; }
        }

        private bool TryGetPath(string key, out string path)
        {
            path = null;
            if (string.IsNullOrWhiteSpace(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Debug.LogError($"[Save] Invalid save key '{key}'.");
                return false;
            }
            path = Path.Combine(RootPath, key + ".json");
            return true;
        }
    }
}
