using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VirtualLife.Core;

namespace VirtualLife.UI
{
    /// <summary>Wires the main menu buttons to scene navigation and the settings panel.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button enterWorldButton;
        [SerializeField] private Button avatarButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private CanvasGroup fadeGroup;

        [Header("Fallback")]
        [Tooltip("If the menu is opened without core services (e.g. pressing Play in this scene), load this scene first.")]
        [SerializeField] private string bootstrapSceneName = "Bootstrap";
        [SerializeField] private float fadeInSeconds = 0.6f;

        private void Start()
        {
            if (!CoreServices.IsReady)
            {
                Debug.LogWarning("[MainMenu] Core services missing; loading Bootstrap scene first.");
                SceneManager.LoadScene(bootstrapSceneName);
                return;
            }

            Bind(enterWorldButton, OnEnterWorld);
            Bind(avatarButton, OnAvatar);
            Bind(settingsButton, OnSettings);
            Bind(exitButton, OnExit);

            if (settingsPanel != null)
            {
                settingsPanel.BackRequested += CloseSettings;
                settingsPanel.gameObject.SetActive(false);
            }
            if (mainPanel != null) mainPanel.SetActive(true);
            if (fadeGroup != null) StartCoroutine(FadeIn());
        }

        private void OnDestroy()
        {
            if (settingsPanel != null) settingsPanel.BackRequested -= CloseSettings;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) { Debug.LogWarning("[MainMenu] A button reference is missing."); return; }
            button.onClick.AddListener(action);
        }

        private void OnEnterWorld() => Load(CoreServices.Catalog.CommunityOne);
        private void OnAvatar() => Load(CoreServices.Catalog.AvatarCreation);

        private void Load(string sceneName)
        {
            // SceneLoader rejects duplicate requests, so rapid double clicks are harmless.
            if (CoreServices.Scenes.TryLoad(sceneName)) SetInteractable(false);
            else SetInteractable(true);
        }

        private void OnSettings()
        {
            if (settingsPanel == null) return;
            if (mainPanel != null) mainPanel.SetActive(false);
            settingsPanel.gameObject.SetActive(true);
        }

        private void CloseSettings()
        {
            settingsPanel.gameObject.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            // Exit Play Mode only; never closes the Unity Editor itself.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetInteractable(bool value)
        {
            foreach (Button b in new[] { enterWorldButton, avatarButton, settingsButton, exitButton })
                if (b != null) b.interactable = value;
        }

        private IEnumerator FadeIn()
        {
            fadeGroup.alpha = 0f;
            for (float t = 0f; t < fadeInSeconds; t += Time.unscaledDeltaTime)
            {
                fadeGroup.alpha = Mathf.SmoothStep(0f, 1f, t / fadeInSeconds);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }
    }
}
