using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualLife.Core
{
    /// <summary>
    /// Asynchronous single-scene loader with progress and a guard against duplicate requests.
    /// A future loading screen only needs to subscribe to the events below.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        public bool IsLoading { get; private set; }
        public float Progress { get; private set; }

        public event Action<string> LoadStarted;
        public event Action<float> ProgressChanged;
        public event Action<string> LoadCompleted;
        /// <summary>Arguments: scene name, reason.</summary>
        public event Action<string, string> LoadFailed;

        public bool TryLoad(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Ignored request for '{sceneName}': a load is already in progress.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                string reason = $"Scene '{sceneName}' cannot be loaded (missing from Build Settings?).";
                Debug.LogError($"[SceneLoader] {reason}");
                LoadFailed?.Invoke(sceneName, reason);
                return false;
            }

            StartCoroutine(LoadRoutine(sceneName));
            return true;
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            Progress = 0f;
            LoadStarted?.Invoke(sceneName);

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                IsLoading = false;
                LoadFailed?.Invoke(sceneName, "LoadSceneAsync returned null.");
                yield break;
            }

            while (!operation.isDone)
            {
                // Unity reports 0..0.9 while loading; normalise to 0..1.
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                ProgressChanged?.Invoke(Progress);
                yield return null;
            }

            Progress = 1f;
            ProgressChanged?.Invoke(Progress);
            IsLoading = false;
            LoadCompleted?.Invoke(sceneName);
        }
    }
}
