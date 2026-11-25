using UnityEngine;
using VirtualLife.Core;
using VirtualLife.Input;

namespace VirtualLife.UI
{
    /// <summary>
    /// Shows the touch controls only on Android and connects them to the player's input sink.
    /// It contains no movement logic: Android drives the same PlayerController as PC.
    /// </summary>
    public sealed class MobileControlsUI : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private MobileCameraTouch cameraTouch;
        [SerializeField] private MobileActionButton[] actionButtons;
        [Tooltip("Editor testing only: show the controls even on PC.")]
        [SerializeField] private bool forceShowInEditor;

        private void Awake()
        {
            bool mobile = CoreServices.Platform?.IsMobile ?? Application.isMobilePlatform;
            bool show = mobile || (Application.isEditor && forceShowInEditor);
            if (visualRoot != null) visualRoot.SetActive(show);
        }

        public void Bind(IVirtualInputSink sink)
        {
            if (joystick != null) joystick.Bind(sink);
            if (cameraTouch != null) cameraTouch.Bind(sink);
            if (actionButtons == null) return;
            foreach (MobileActionButton b in actionButtons) if (b != null) b.Bind(sink);
        }
    }
}
