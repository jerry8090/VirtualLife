using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VirtualLife.Core;

namespace VirtualLife.UI
{
    /// <summary>Minimal pause panel (Resume / Main Menu). Reacts to the global game state, so it works
    /// for keyboard, mobile and any future pause trigger.</summary>
    public sealed class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string fallbackMenuScene = "MainMenu";

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void OnEnable()
        {
            if (CoreServices.State != null) CoreServices.State.StateChanged += OnStateChanged;
            Show(CoreServices.State != null && CoreServices.State.Current == GameState.Paused);
        }

        private void OnDisable()
        {
            if (CoreServices.State != null) CoreServices.State.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState previous, GameState next) => Show(next == GameState.Paused);

        private void Show(bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }

        private static void OnResume() => CoreServices.State?.TryTransition(GameState.Playing);

        private void OnMainMenu()
        {
            if (CoreServices.IsReady) CoreServices.Scenes.TryLoad(CoreServices.Catalog.MainMenu);
            else SceneManager.LoadScene(fallbackMenuScene);
        }
    }
}
