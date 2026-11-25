using UnityEngine;

namespace VirtualLife.Player
{
    /// <summary>Tunable movement values. One asset can be shared by every player (and future remote players).</summary>
    [CreateAssetMenu(menuName = "VirtualLife/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Speed (m/s)")]
        [SerializeField, Min(0f)] private float walkSpeed = 3.5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6f;
        [SerializeField, Min(0.1f)] private float acceleration = 25f;
        [SerializeField, Min(0.1f)] private float deceleration = 30f;
        [Tooltip("Multiplier on acceleration while airborne.")]
        [SerializeField, Range(0f, 1f)] private float airControl = 0.35f;
        [Tooltip("Seconds to turn toward the movement direction (smaller = snappier).")]
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.08f;

        [Header("Jump & Gravity")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [Tooltip("Gravity magnitude in m/s^2.")]
        [SerializeField, Min(0.1f)] private float gravity = 20f;
        [SerializeField, Min(1f)] private float terminalVelocity = 40f;
        [SerializeField, Min(0f)] private float groundedStickForce = 2f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("Slopes")]
        [SerializeField, Range(0f, 80f)] private float maxSlopeAngle = 45f;
        [SerializeField, Min(0f)] private float stepOffset = 0.3f;
        [SerializeField, Min(0f)] private float slideSpeed = 6f;

        [Header("Safety")]
        [Tooltip("If the player falls below this world Y, they are respawned.")]
        [SerializeField] private float killPlaneY = -20f;

        public float WalkSpeed => walkSpeed;
        public float SprintSpeed => sprintSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float AirControl => airControl;
        public float RotationSmoothTime => rotationSmoothTime;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float TerminalVelocity => terminalVelocity;
        public float GroundedStickForce => groundedStickForce;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float MaxSlopeAngle => maxSlopeAngle;
        public float StepOffset => stepOffset;
        public float SlideSpeed => slideSpeed;
        public float KillPlaneY => killPlaneY;
    }
}
