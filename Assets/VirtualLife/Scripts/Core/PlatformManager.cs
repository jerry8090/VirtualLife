using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualLife.Core
{
    public enum PlatformKind { Unknown, WindowsPC, Android }

    /// <summary>Inspector-only helper to simulate a platform inside the Editor.</summary>
    public enum PlatformOverride { None, WindowsPC, Android }

    /// <summary>Read-only platform info. Gameplay code should branch on capabilities, not on platform.</summary>
    public interface IPlatformService
    {
        PlatformKind Kind { get; }
        bool IsDesktop { get; }
        bool IsMobile { get; }
        bool HasTouchscreen { get; }
    }

    public sealed class PlatformService : IPlatformService
    {
        public PlatformKind Kind { get; }
        public bool IsDesktop => Kind == PlatformKind.WindowsPC;
        public bool IsMobile => Kind == PlatformKind.Android;
        public bool HasTouchscreen => Touchscreen.current != null;

        public PlatformService(PlatformOverride platformOverride = PlatformOverride.None)
        {
            if (platformOverride != PlatformOverride.None && Application.isEditor)
            {
                Kind = platformOverride == PlatformOverride.Android ? PlatformKind.Android : PlatformKind.WindowsPC;
                return;
            }

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    Kind = PlatformKind.WindowsPC;
                    break;
                case RuntimePlatform.Android:
                    Kind = PlatformKind.Android;
                    break;
                default:
                    Kind = PlatformKind.Unknown;
                    break;
            }
        }
    }
}
