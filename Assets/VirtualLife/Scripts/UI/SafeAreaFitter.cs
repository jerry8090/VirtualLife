using UnityEngine;

namespace VirtualLife.UI
{
    /// <summary>Keeps a full-screen RectTransform inside Screen.safeArea (notches, rounded corners).
    /// Event-driven: re-applies only when the canvas size changes (e.g. rotation).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rect;
        private Rect lastSafeArea;
        private Vector2Int lastScreen;

        private void OnEnable() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void Apply()
        {
            if (rect == null) rect = (RectTransform)transform;
            Rect safe = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (safe == lastSafeArea && screen == lastScreen) return;
            lastSafeArea = safe; lastScreen = screen;
            if (screen.x <= 0 || screen.y <= 0) return;

            rect.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            rect.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
