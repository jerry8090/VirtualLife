using System;
using UnityEngine;

namespace VirtualLife.Player
{
    /// <summary>What the player wants to do this frame. Input is separated from execution so a
    /// future network layer can feed commands from somewhere other than local devices.</summary>
    public struct PlayerMoveCommand
    {
        public Vector2 Move;
        public bool Sprint;
        public bool WantsJump;
    }

    /// <summary>
    /// Executes movement on a CharacterController (no Rigidbody involved). Camera-relative movement,
    /// smooth acceleration, gravity, coyote time, jump, slopes. Knows nothing about devices or UI.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float lastGroundedTime = -999f;
        private float yawVelocity;
        private bool jumpConsumed, wasGrounded;
        private Vector3 groundNormal = Vector3.up;

        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool JumpedThisTick { get; private set; }
        public float HorizontalSpeed => horizontalVelocity.magnitude;
        public float VerticalVelocity => verticalVelocity;
        public PlayerConfig Config => config;

        public event Action Jumped;
        /// <summary>Argument: downward impact speed in m/s.</summary>
        public event Action<float> Landed;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (config == null) { Debug.LogError("[Player] PlayerMovement has no PlayerConfig assigned.", this); enabled = false; return; }
            characterController.slopeLimit = config.MaxSlopeAngle;
            characterController.stepOffset = config.StepOffset;
        }

        public void ResetMotion()
        {
            horizontalVelocity = Vector3.zero; verticalVelocity = 0f; yawVelocity = 0f;
            jumpConsumed = false; IsGrounded = false; wasGrounded = false;
        }

        /// <param name="reference">Usually the camera; movement is relative to its yaw.</param>
        public void Tick(in PlayerMoveCommand cmd, Transform reference, float dt)
        {
            JumpedThisTick = false;
            if (dt <= 0f || !characterController.enabled) return;

            Vector2 move = Vector2.ClampMagnitude(cmd.Move, 1f);
            bool hasInput = move.sqrMagnitude > 0.0001f;

            Transform basis = reference != null ? reference : transform;
            Vector3 forward = Vector3.ProjectOnPlane(basis.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.ProjectOnPlane(basis.up, Vector3.up);
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 wishDir = forward * move.y + right * move.x;

            IsSprinting = cmd.Sprint && hasInput;
            float maxSpeed = IsSprinting ? config.SprintSpeed : config.WalkSpeed;
            float rate = hasInput ? config.Acceleration : config.Deceleration;
            if (!IsGrounded) rate *= config.AirControl;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wishDir * maxSpeed, rate * dt);

            if (hasInput)
            {
                float targetYaw = Mathf.Atan2(wishDir.x, wishDir.z) * Mathf.Rad2Deg;
                float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref yawVelocity,
                                                  config.RotationSmoothTime, Mathf.Infinity, dt);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            // --- vertical ---
            bool grounded = IsGrounded;
            if (grounded)
            {
                lastGroundedTime = Time.time;
                if (verticalVelocity <= 0f) { verticalVelocity = -config.GroundedStickForce; jumpConsumed = false; }
            }

            bool canJump = !jumpConsumed && (grounded || Time.time - lastGroundedTime <= config.CoyoteTime);
            if (cmd.WantsJump && canJump)
            {
                verticalVelocity = Mathf.Sqrt(2f * config.Gravity * config.JumpHeight);
                jumpConsumed = true;
                lastGroundedTime = -999f;
                JumpedThisTick = true;
                Jumped?.Invoke();
            }

            verticalVelocity = Mathf.Max(verticalVelocity - config.Gravity * dt, -config.TerminalVelocity);

            // --- slopes ---
            Vector3 planar = horizontalVelocity;
            if (grounded && verticalVelocity <= 0f)
            {
                planar = Vector3.ProjectOnPlane(horizontalVelocity, groundNormal); // stay glued going downhill
                if (Vector3.Angle(groundNormal, Vector3.up) > characterController.slopeLimit)
                    planar += Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized * config.SlideSpeed;
            }

            float preMoveVertical = verticalVelocity;
            groundNormal = Vector3.up;
            characterController.Move((planar + Vector3.up * verticalVelocity) * dt);
            IsGrounded = characterController.isGrounded;

            if (IsGrounded && !wasGrounded && preMoveVertical < -2f) Landed?.Invoke(-preMoveVertical);
            wasGrounded = IsGrounded;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.2f) groundNormal = hit.normal; // cheap ground normal, no extra physics query
        }
    }
}
