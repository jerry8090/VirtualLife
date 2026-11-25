using System;
using UnityEngine;

namespace VirtualLife.Player
{
    public enum PlayerLocomotion { Idle, Walking, Running, Jumping, Falling }

    /// <summary>Read-only snapshot of what the player is doing. Separate from visuals and
    /// movement so animation, UI and (later) networking can all read it.</summary>
    public sealed class PlayerState : MonoBehaviour
    {
        public PlayerLocomotion Locomotion { get; private set; } = PlayerLocomotion.Idle;
        public bool IsGrounded { get; private set; }
        public float HorizontalSpeed { get; private set; }
        public float VerticalVelocity { get; private set; }

        public event Action<PlayerLocomotion> LocomotionChanged;

        public void Apply(PlayerMovement movement)
        {
            IsGrounded = movement.IsGrounded;
            HorizontalSpeed = movement.HorizontalSpeed;
            VerticalVelocity = movement.VerticalVelocity;

            PlayerLocomotion next;
            if (!IsGrounded) next = VerticalVelocity > 0.1f ? PlayerLocomotion.Jumping : PlayerLocomotion.Falling;
            else if (HorizontalSpeed < 0.1f) next = PlayerLocomotion.Idle;
            else next = movement.IsSprinting ? PlayerLocomotion.Running : PlayerLocomotion.Walking;

            if (next == Locomotion) return;
            Locomotion = next;
            LocomotionChanged?.Invoke(next);
        }
    }
}
