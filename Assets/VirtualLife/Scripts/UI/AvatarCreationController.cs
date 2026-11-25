using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VirtualLife.Core;

namespace VirtualLife.UI
{
    /// <summary>Placeholder avatar screen for Stage 1: only the Back button does anything.</summary>
    public sealed class AvatarCreationController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private string fallbackMenuScene = "MainMenu";

        private void Start()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            else Debug.LogWarning("[Avatar] Back button reference is missing.");
        }

        private void OnBack()
        {
            if (CoreServices.IsReady) CoreServices.Scenes.TryLoad(CoreServices.Catalog.MainMenu);
            else SceneManager.LoadScene(fallbackMenuScene); // opened directly in the Editor
        }
    }
}
