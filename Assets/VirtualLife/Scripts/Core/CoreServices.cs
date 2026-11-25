using UnityEngine;
using VirtualLife.SaveSystem;

namespace VirtualLife.Core
{
    /// <summary>
    /// Read-only access point to the core services. This is a plain static holder, not a
    /// Singleton MonoBehaviour: the services are created exactly once by GameBootstrapper.
    /// Prefer serialized references or constructor injection in gameplay code; use this holder
    /// mainly from UI/scene glue code.
    /// </summary>
    public static class CoreServices
    {
        public static bool IsReady { get; private set; }
        public static GameStateManager State { get; private set; }
        public static SceneLoader Scenes { get; private set; }
        public static IPlatformService Platform { get; private set; }
        public static ISaveService Save { get; private set; }
        public static SceneCatalog Catalog { get; private set; }

        internal static void Register(GameStateManager state, SceneLoader scenes, IPlatformService platform,
                                      ISaveService save, SceneCatalog catalog)
        {
            State = state; Scenes = scenes; Platform = platform; Save = save; Catalog = catalog;
            IsReady = true;
        }

        // Keeps statics clean when "Enter Play Mode Options" disables domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsReady = false;
            State = null; Scenes = null; Platform = null; Save = null; Catalog = null;
        }
    }
}
