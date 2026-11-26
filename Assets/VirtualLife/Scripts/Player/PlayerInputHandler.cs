using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VirtualLife.Core;

namespace VirtualLife.Input
{
    /// <summary>
    /// The single place that reads the Input Actions "Player" map (keyboard, mouse, gamepad) and
    /// merges on-screen virtual input. Each action is read here only, never from multiple scripts.
    /// </summary>
    public sealed class PlayerInputHandler : MonoBehaviour, IPlayerInput, IVirtualInputSink
    {
        [SerializeField] private InputActionAsset actions;
        [Tooltip("Gamepad right-stick look speed in mouse-pixel units per second.")]
        [SerializeField] private float gamepadLookPixelsPerSecond = 900f;

        private InputAction move, look, jump, sprint, interact, pause;
        private InputActionMap map;
        private Vector2 virtualMove, virtualLook;
        private bool virtualSprint, gameplayEnabled = true;
        private float jumpPressTime = -1f;

        public event Action InteractPressed;
        public event Action PausePressed;

        /// <summary>When false (e.g. paused) movement/look/jump/interact are ignored; Pause still works.</summary>
        public bool GameplayInputEnabled
        {
            get => gameplayEnabled;
            set
            {
                gameplayEnabled = value;
                if (!value) { virtualLook = Vector2.zero; jumpPressTime = -1f; } // held move/sprint are kept so a still-held finger stays correct after resume
            }
        }

        private static bool IsMobile => CoreServices.Platform?.IsMobile ?? Application.isMobilePlatform;

        private void Awake()
        {
            if (actions == null) { Debug.LogError("[Input] PlayerInputHandler has no InputActionAsset assigned.", this); enabled = false; return; }
            map = actions.FindActionMap("Player", false);
            if (map == null) { Debug.LogError("[Input] Action map 'Player' not found.", this); enabled = false; return; }
            move = map.FindAction("Move", false); look = map.FindAction("Look", false);
            jump = map.FindAction("Jump", false); sprint = map.FindAction("Sprint", false);
            interact = map.FindAction("Interact", false); pause = map.FindAction("Pause", false);
            if (move == null || look == null || jump == null || sprint == null || interact == null || pause == null)
            { Debug.LogError("[Input] The Player map is missing a required action.", this); enabled = false; }
        }

        private void OnEnable()
        {
            if (map == null) return;
            jump.performed += OnJump; interact.performed += OnInteract; pause.performed += OnPause;
            map.Enable();
        }

        private void OnDisable()
        {
            if (map == null) return;
            jump.performed -= OnJump; interact.performed -= OnInteract; pause.performed -= OnPause;
            map.Disable();
        }

        // ---- IPlayerInput ----
        public Vector2 Move => !gameplayEnabled || move == null ? Vector2.zero
            : Vector2.ClampMagnitude(move.ReadValue<Vector2>() + virtualMove, 1f);

        public bool SprintHeld => gameplayEnabled && sprint != null && (sprint.IsPressed() || virtualSprint);

        public bool HasBufferedJump(float bufferSeconds) =>
            gameplayEnabled && jumpPressTime >= 0f && Time.time - jumpPressTime <= bufferSeconds;

        public void ClearJumpBuffer() => jumpPressTime = -1f;

        public Vector2 ConsumeLook()
        {
            Vector2 result = virtualLook;
            virtualLook = Vector2.zero;
            if (!gameplayEnabled) return Vector2.zero;
            return result + ReadHardwareLook();
        }

        private Vector2 ReadHardwareLook()
        {
            if (look == null) return Vector2.zero;
            InputControl control = look.activeControl;
            if (control == null) return Vector2.zero;
            InputDevice device = control.device;
            if (device is Touchscreen) return Vector2.zero;            // touch look comes from MobileCameraTouch
            if (device is Gamepad) return look.ReadValue<Vector2>() * (gamepadLookPixelsPerSecond * Time.deltaTime);
            if (IsMobile) return Vector2.zero;                           // ignore stray mouse deltas on Android
            return look.ReadValue<Vector2>();
        }

        // ---- IVirtualInputSink ----
        public void SetMove(Vector2 value) => virtualMove = value; // stored raw; Move getter gates on gameplayEnabled
        public void AddLook(Vector2 pixelDelta) { if (gameplayEnabled) virtualLook += pixelDelta; }
        public void SetSprint(bool held) => virtualSprint = held; // stored raw; SprintHeld getter gates on gameplayEnabled
        public void PressJump() { if (gameplayEnabled) jumpPressTime = Time.time; }
        public void PressInteract() { if (gameplayEnabled) InteractPressed?.Invoke(); }
        public void PressPause() => PausePressed?.Invoke();

        // ---- Action callbacks ----
        private void OnJump(InputAction.CallbackContext _) => PressJump();
        private void OnInteract(InputAction.CallbackContext _) => PressInteract();
        private void OnPause(InputAction.CallbackContext _) => PressPause();
    }
}
