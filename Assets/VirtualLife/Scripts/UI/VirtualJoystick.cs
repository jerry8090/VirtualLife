using UnityEngine;
using UnityEngine.EventSystems;
using VirtualLife.Input;

namespace VirtualLife.UI
{
    /// <summary>On-screen movement stick. Tracks one pointer id so other fingers never interfere.</summary>
    public sealed class VirtualJoystick : MonoBehaviour, IInitializePotentialDragHandler, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;
        [SerializeField, Range(0.2f, 1f)] private float handleTravel = 0.8f;

        private IVirtualInputSink sink;
        private int pointerId = NoPointer;

        public void Bind(IVirtualInputSink target) => sink = target;

        public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != NoPointer) return;
            pointerId = e.pointerId;
            UpdateStick(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == pointerId) UpdateStick(e);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) ResetStick();
        }

        private void OnDisable() => ResetStick();

        // Touch-up can be lost when the app loses focus; never leave the player running.
        private void OnApplicationFocus(bool focused) { if (!focused) ResetStick(); }

        private void UpdateStick(PointerEventData e)
        {
            if (background == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out Vector2 local);
            float radius = background.rect.width * 0.5f;
            Vector2 v = Vector2.ClampMagnitude(local / radius, 1f);
            if (handle != null) handle.anchoredPosition = v * (radius * handleTravel);

            float mag = v.magnitude;
            Vector2 output = mag < deadZone ? Vector2.zero : v.normalized * Mathf.InverseLerp(deadZone, 1f, mag);
            sink?.SetMove(output);
        }

        private void ResetStick()
        {
            pointerId = NoPointer;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            sink?.SetMove(Vector2.zero);
        }
    }
}
