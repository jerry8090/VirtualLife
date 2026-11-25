using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VirtualLife.Input;

namespace VirtualLife.UI
{
    public enum MobileAction { Jump, Sprint, Interact, Pause }

    /// <summary>On-screen button for one player action, with press feedback.</summary>
    public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private MobileAction action;
        [SerializeField] private Image image;
        [SerializeField] private Color normalColor = new(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color pressedColor = new(0.45f, 0.75f, 1f, 0.8f);

        private IVirtualInputSink sink;
        private int pointerId = NoPointer;

        public void Bind(IVirtualInputSink target) => sink = target;

        private void OnEnable() => SetVisual(false);

        public void OnPointerDown(PointerEventData e)
        {
            if (pointerId != NoPointer) return;
            pointerId = e.pointerId;
            SetVisual(true);
            switch (action)
            {
                case MobileAction.Jump: sink?.PressJump(); break;
                case MobileAction.Sprint: sink?.SetSprint(true); break;
                case MobileAction.Interact: sink?.PressInteract(); break;
                case MobileAction.Pause: sink?.PressPause(); break;
            }
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == pointerId) Release();
        }

        private void OnDisable() => Release();

        private void Release()
        {
            if (pointerId == NoPointer) return;
            pointerId = NoPointer;
            if (action == MobileAction.Sprint) sink?.SetSprint(false);
            SetVisual(false);
        }

        private void SetVisual(bool pressed)
        {
            if (image != null) image.color = pressed ? pressedColor : normalColor;
        }
    }
}
