using System.Collections.Generic;
using UnityEngine;

namespace VirtualLife.Player
{
    /// <summary>
    /// Drives Animator parameters from PlayerState. Fully optional: with no Animator, no controller
    /// or missing parameters it quietly does nothing, so the player stays playable with a plain capsule.
    /// Parameters: Speed, IsGrounded, VerticalVelocity, IsFalling (+ triggers Jump, Land).
    /// </summary>
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private Animator animator;
        [SerializeField] private float speedDampTime = 0.08f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int FallingHash = Animator.StringToHash("IsFalling");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int LandHash = Animator.StringToHash("Land");

        private readonly HashSet<int> available = new();
        private bool active;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (AnimatorControllerParameter p in animator.parameters) available.Add(p.nameHash);
            active = available.Count > 0;
        }

        private void OnEnable()
        {
            if (movement == null) return;
            movement.Jumped += OnJumped;
            movement.Landed += OnLanded;
        }

        private void OnDisable()
        {
            if (movement == null) return;
            movement.Jumped -= OnJumped;
            movement.Landed -= OnLanded;
        }

        public void Tick(PlayerState state, float dt)
        {
            if (!active) return;
            if (available.Contains(SpeedHash)) animator.SetFloat(SpeedHash, state.HorizontalSpeed, speedDampTime, dt);
            if (available.Contains(GroundedHash)) animator.SetBool(GroundedHash, state.IsGrounded);
            if (available.Contains(VerticalHash)) animator.SetFloat(VerticalHash, state.VerticalVelocity);
            if (available.Contains(FallingHash))
                animator.SetBool(FallingHash, !state.IsGrounded && state.VerticalVelocity < -0.5f);
        }

        private void OnJumped()
        {
            if (!active) return;
            if (available.Contains(LandHash)) animator.ResetTrigger(LandHash);
            if (available.Contains(JumpHash)) animator.SetTrigger(JumpHash);
        }

        private void OnLanded(float impact)
        {
            if (active && impact > 4f && available.Contains(LandHash)) animator.SetTrigger(LandHash);
        }
    }
}
