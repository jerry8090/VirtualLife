using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualLife.Input
{
    public enum InputDeviceCategory { Unknown, KeyboardMouse, Touch, Gamepad }

    /// <summary>
    /// Reports which device category the player last used (event-driven, no Update loop).
    /// Later stages can use this to switch on-screen touch controls or button prompts.
    /// </summary>
    public sealed class InputDeviceMonitor : MonoBehaviour
    {
        [SerializeField] private bool logChanges = true;

        public InputDeviceCategory Current { get; private set; } = InputDeviceCategory.Unknown;
        public event Action<InputDeviceCategory> CategoryChanged;

        private void OnEnable() => InputSystem.onActionChange += OnActionChange;
        private void OnDisable() => InputSystem.onActionChange -= OnActionChange;

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (obj is not InputAction action || action.activeControl == null) return;

            InputDeviceCategory category = Classify(action.activeControl.device);
            if (category == InputDeviceCategory.Unknown || category == Current) return;

            Current = category;
            if (logChanges) Debug.Log($"[Input] Active device category: {category}");
            CategoryChanged?.Invoke(category);
        }

        private static InputDeviceCategory Classify(InputDevice device)
        {
            return device switch
            {
                Touchscreen => InputDeviceCategory.Touch,
                Keyboard or Mouse => InputDeviceCategory.KeyboardMouse,
                Gamepad => InputDeviceCategory.Gamepad,
                _ => InputDeviceCategory.Unknown
            };
        }
    }
}
