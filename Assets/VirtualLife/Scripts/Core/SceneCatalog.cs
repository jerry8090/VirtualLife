using UnityEngine;

namespace VirtualLife.Core
{
    /// <summary>
    /// Single configurable place for scene names. Scene names are validated against the
    /// Build Settings at startup, so a typo is reported once instead of failing mid-game.
    /// </summary>
    [CreateAssetMenu(menuName = "VirtualLife/Scene Catalog", fileName = "SceneCatalog")]
    public sealed class SceneCatalog : ScriptableObject
    {
        [SerializeField] private string bootstrap = "Bootstrap";
        [SerializeField] private string mainMenu = "MainMenu";
        [SerializeField] private string avatarCreation = "AvatarCreation";
        [SerializeField] private string communityOne = "CommunityOne";
        [SerializeField] private string testEnvironment = "TestEnvironment";

        public string Bootstrap => bootstrap;
        public string MainMenu => mainMenu;
        public string AvatarCreation => avatarCreation;
        public string CommunityOne => communityOne;
        public string TestEnvironment => testEnvironment;

        /// <summary>Checks the scenes the normal game flow depends on (TestEnvironment is optional).</summary>
        public bool Validate(out string error)
        {
            string[] required = { bootstrap, mainMenu, avatarCreation, communityOne };
            foreach (string sceneName in required)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    error = "A scene name in the SceneCatalog is empty.";
                    return false;
                }
                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    error = $"Scene '{sceneName}' is not in Build Settings (or is disabled).";
                    return false;
                }
            }
            error = null;
            return true;
        }
    }
}
