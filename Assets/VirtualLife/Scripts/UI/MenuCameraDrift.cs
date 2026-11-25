using UnityEngine;

namespace VirtualLife.UI
{
    /// <summary>Slow cinematic orbit around a point; used for the main menu background.</summary>
    public sealed class MenuCameraDrift : MonoBehaviour
    {
        [SerializeField] private Transform pivot;
        [SerializeField] private float degreesPerSecond = 3f;

        private void LateUpdate()
        {
            if (pivot == null) return;
            transform.RotateAround(pivot.position, Vector3.up, degreesPerSecond * Time.unscaledDeltaTime);
            transform.LookAt(pivot.position + Vector3.up * 2f);
        }
    }
}
