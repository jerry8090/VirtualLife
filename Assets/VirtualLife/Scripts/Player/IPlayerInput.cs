using System;
using UnityEngine;

namespace VirtualLife.Input
{
    /// <summary>
    /// Device-agnostic player input. Gameplay code (PlayerController, camera) depends on this,
    /// never on keyboards, touchscreens or gamepads directly.
    /// </summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool SprintHeld { get; }
        /// <summary>True while a jump press is still inside the buffer window.</summary>
        bool HasBufferedJump(float bufferSeconds);
        void ClearJumpBuffer();
        /// <summary>Look delta since the last call, in "pixels" (mouse-like units). Call once per frame.</summary>
        Vector2 ConsumeLook();
        event Action InteractPressed;
        event Action PausePressed;
    }

    /// <summary>
    /// Entry point for on-screen controls. Mobile UI writes here, so Android drives exactly
    /// the same PlayerController/PlayerMovement as keyboard and mouse.
    /// </summary>
    public interface IVirtualInputSink
    {
        void SetMove(Vector2 value);
        void AddLook(Vector2 pixelDelta);
        void SetSprint(bool held);
        void PressJump();
        void PressInteract();
        void PressPause();
    }
}
