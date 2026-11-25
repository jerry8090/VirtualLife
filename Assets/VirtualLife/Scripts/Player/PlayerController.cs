using System;
using UnityEngine;
using VirtualLife.Core;
using VirtualLife.Input;

namespace VirtualLife.Player
{
    /// <summary>
    /// Orchestrates one player: input -> movement command -> movement -> state -> animation.
    /// Also reacts to the global pause state and out-of-world falls. Holds no device logic.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerState playerState;
        [SerializeField] private PlayerAnimationController animationController; // optional
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Transform cameraTarget;

        private Transform cameraTransform;

        public Transform CameraTarget => cameraTarget != null ? cameraTarget : transform;
        public IPlayerInput InputSource => input;
        public IVirtualInputSink VirtualSink => input;
        public PlayerMovement Movement => movement;

        /// <summary>Raised when Interact is pressed. Stage 3+ interactables will listen here.</summary>
        public event Action Interacted;
        public event Action FellOutOfWorld;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputHandler>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (playerState == null) playerState = GetComponent<PlayerState>();
            if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
            if (config == null && movement != null) config = movement.Config;

            if (input == null || movement == null || playerState == null || config == null)
            {
                Debug.LogError("[Player] PlayerController is missing a required reference.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.PausePressed += TogglePause;
            input.InteractPressed += OnInteract;
            if (CoreServices.State != null) CoreServices.State.StateChanged += OnStateChanged;
            ApplyState(CoreServices.State != null ? CoreServices.State.Current : GameState.Playing);
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.PausePressed -= TogglePause;
                input.InteractPressed -= OnInteract;
            }
            if (CoreServices.State != null) CoreServices.State.StateChanged -= OnStateChanged;
            Cursor.lockState = CursorLockMode.None; // always hand the cursor back to menus
            Cursor.visible = true;
        }

        public void SetCameraTransform(Transform value) => cameraTransform = value;

        private void Update()
        {
            float dt = Time.deltaTime;
            var cmd = new PlayerMoveCommand
            {
                Move = input.Move,
                Sprint = input.SprintHeld,
                WantsJump = input.HasBufferedJump(config.JumpBufferTime)
            };

            movement.Tick(cmd, cameraTransform, dt);
            if (movement.JumpedThisTick) input.ClearJumpBuffer();

            playerState.Apply(movement);
            if (animationController != null) animationController.Tick(playerState, dt);

            if (transform.position.y < config.KillPlaneY) FellOutOfWorld?.Invoke();
        }

        private void OnInteract()
        {
            Debug.Log("[Player] Interact pressed (no interactables until a later stage).");
            Interacted?.Invoke();
        }

        private static void TogglePause()
        {
            GameStateManager state = CoreServices.State;
            if (state == null) { Debug.LogWarning("[Player] Pause ignored: core services are not running (start from Bootstrap)."); return; }
            if (state.Current == GameState.Playing) state.TryTransition(GameState.Paused);
            else if (state.Current == GameState.Paused) state.TryTransition(GameState.Playing);
        }

        private void OnStateChanged(GameState previous, GameState next) => ApplyState(next);

        private void ApplyState(GameState state)
        {
            bool paused = state == GameState.Paused;
            input.GameplayInputEnabled = !paused;

            bool mobile = CoreServices.Platform?.IsMobile ?? Application.isMobilePlatform;
            bool lockCursor = !mobile && !paused;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }
    }
}
