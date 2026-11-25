using UnityEngine;

namespace VirtualLife.Core
{
    /// <summary>
    /// Maps scene loads to application states. Lives as long as the core services, so it keeps
    /// working after the scene that started a load has been unloaded.
    /// </summary>
    internal sealed class SceneStateBinder
    {
        private readonly GameStateManager state;
        private readonly SceneCatalog catalog;

        private SceneStateBinder(GameStateManager state, SceneCatalog catalog)
        {
            this.state = state;
            this.catalog = catalog;
        }

        public static void Bind(SceneLoader loader, GameStateManager state, SceneCatalog catalog)
        {
            var binder = new SceneStateBinder(state, catalog);
            loader.LoadStarted += binder.OnLoadStarted;
            loader.LoadCompleted += binder.OnLoadCompleted;
            loader.LoadFailed += binder.OnLoadFailed;
            state.StateChanged += binder.OnStateChanged;
        }

        private void OnLoadStarted(string scene)
        {
            if (scene == catalog.CommunityOne) state.TryTransition(GameState.LoadingWorld);
        }

        private void OnLoadCompleted(string scene)
        {
            if (scene == catalog.MainMenu) state.TryTransition(GameState.MainMenu);
            else if (scene == catalog.CommunityOne) state.TryTransition(GameState.Playing);
        }

        // Pausing freezes time; leaving Paused (resume OR back to menu) always restores it.
        private void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.Paused) Time.timeScale = 0f;
            else if (previous == GameState.Paused) Time.timeScale = 1f;
        }

        private void OnLoadFailed(string scene, string reason)
        {
            if (state.Current == GameState.LoadingWorld) state.TryTransition(GameState.MainMenu);
        }
    }
}
