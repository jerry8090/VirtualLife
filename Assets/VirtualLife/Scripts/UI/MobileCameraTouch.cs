using UnityEngine;
using UnityEngine.EventSystems;
using VirtualLife.Input;

namespace VirtualLife.UI
{
    /// <summary>
    /// Invisible touch area that rotates the camera. Sits behind the joystick and buttons in the
    /// hierarchy, so UI touches never reach it. Follows only its own pointer, enabling multitouch.
    /// </summary>
    public sealed class MobileCameraTouch : MonoBehaviour, IInitializePotentialDragHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [Tooltip("Multiplier on the (resolution-normalised) drag delta.")]
        [SerializeField, Min(0.1f)] private float sensitivity = 1.6f;

        private IVirtualInputSink sink;
        private int pointerId = NoPointer;

        public void Bind(IVirtualInputSink target) => sink = target;

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId == NoPointer) pointerId = e.pointerId;
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointerId || sink == null) return;
            // Normalise to a 1080p-tall screen so sensitivity feels the same on every device.
            sink.AddLook(e.delta * (sensitivity * 1080f / Mathf.Max(1, Screen.height)));
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) pointerId = NoPointer;
        }

        private void OnDisable() => pointerId = NoPointer;

        private void OnApplicationFocus(bool focused) { if (!focused) pointerId = NoPointer; }
    }
}
