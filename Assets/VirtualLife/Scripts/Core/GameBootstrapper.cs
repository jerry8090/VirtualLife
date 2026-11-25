using System;
using UnityEngine;
using VirtualLife.SaveSystem;

namespace VirtualLife.Core
{
    /// <summary>
    /// Application entry point. Lives only in the Bootstrap scene. Creates the persistent core
    /// services once, then loads the main menu.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private SceneCatalog catalog;
        [Tooltip("Editor-only: simulate a platform to test platform-specific UI.")]
        [SerializeField] private PlatformOverride platformOverride = PlatformOverride.None;

        private void Start()
        {
            if (CoreServices.IsReady)
            {
                // Bootstrap was re-entered; never initialize twice.
                Debug.LogWarning("[Bootstrap] Core services already initialized; skipping initialization.");
                CoreServices.Scenes.TryLoad(CoreServices.Catalog.MainMenu);
                Destroy(gameObject);
                return;
            }

            try
            {
                Initialize();
            }
            catch (Exception e)
            {
                // Stay in Bootstrap; a clear console error beats a half-working game.
                Debug.LogError($"[Bootstrap] Initialization failed: {e.Message}\n{e}");
                return;
            }

            CoreServices.Scenes.TryLoad(catalog.MainMenu);
        }

        private void Initialize()
        {
            if (catalog == null)
                throw new InvalidOperationException("SceneCatalog is not assigned on GameBootstrapper.");
            if (!catalog.Validate(out string error))
                throw new InvalidOperationException(error);

            var root = new GameObject("VirtualLife.Core");
            DontDestroyOnLoad(root);
            try
            {
                var loader = root.AddComponent<SceneLoader>();
                var state = new GameStateManager();
                IPlatformService platform = new PlatformService(platformOverride);
                ISaveService save = new JsonFileSaveService();

                CoreServices.Register(state, loader, platform, save, catalog);
                SceneStateBinder.Bind(loader, state, catalog);
                Debug.Log($"[Bootstrap] Initialized. Platform: {platform.Kind}, Touchscreen: {platform.HasTouchscreen}");
            }
            catch
            {
                Destroy(root);
                throw;
            }
        }
    }
}
