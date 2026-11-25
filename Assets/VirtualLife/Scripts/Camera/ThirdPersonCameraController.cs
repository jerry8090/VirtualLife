using UnityEngine;
using VirtualLife.Input;

namespace VirtualLife.CameraRig
{
    /// <summary>
    /// Lightweight custom third-person rig (no Cinemachine dependency). Orbits a target using
    /// look input from IPlayerInput, follows smoothly and avoids clipping. It only reads input and
    /// positions itself; it never moves the player.
    /// </summary>
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        [SerializeField] private CameraCollisionHandler collisionHandler;

        private Transform target;
        private IPlayerInput lookSource;
        private float yaw, pitch, currentDistance, distanceVelocity;
        private Vector3 smoothedPivot, pivotVelocity;

        public float Yaw => yaw;

        private void Awake()
        {
            if (config == null) { Debug.LogError("[Camera] No CameraConfig assigned.", this); enabled = false; return; }
            if (collisionHandler == null) collisionHandler = GetComponent<CameraCollisionHandler>();
            currentDistance = config.Distance;
            pitch = config.StartPitch;
        }

        public void SetTarget(Transform newTarget, IPlayerInput input)
        {
            target = newTarget;
            lookSource = input;
            if (collisionHandler != null && target != null) collisionHandler.SetIgnoreRoot(target.root);
        }

        /// <summary>Instantly place the camera behind the target facing the given yaw.</summary>
        public void Snap(float yawDegrees)
        {
            if (target == null || config == null) return;
            yaw = yawDegrees;
            pitch = config.StartPitch;
            smoothedPivot = Pivot();
            pivotVelocity = Vector3.zero;
            currentDistance = config.Distance;
            Apply(Quaternion.Euler(pitch, yaw, 0f), instantZoom: true);
        }

        private Vector3 Pivot() => target.position + Vector3.up * config.Height;

        private void LateUpdate()
        {
            if (target == null) return;

            if (lookSource != null)
            {
                Vector2 look = lookSource.ConsumeLook();
                yaw += look.x * config.HorizontalSensitivity;
                pitch += (config.InvertY ? look.y : -look.y) * config.VerticalSensitivity;
                pitch = Mathf.Clamp(pitch, config.MinPitch, config.MaxPitch);
            }

            smoothedPivot = Vector3.SmoothDamp(smoothedPivot, Pivot(), ref pivotVelocity, config.FollowSmoothTime);
            Apply(Quaternion.Euler(pitch, yaw, 0f), instantZoom: false);
        }

        private void Apply(Quaternion rotation, bool instantZoom)
        {
            Vector3 desired = smoothedPivot - rotation * Vector3.forward * config.Distance
                              + rotation * Vector3.right * config.ShoulderOffset;

            float free = collisionHandler != null
                ? collisionHandler.ResolveDistance(smoothedPivot, desired)
                : (desired - smoothedPivot).magnitude;
            float full = (desired - smoothedPivot).magnitude;

            // Snap in immediately when blocked; ease back out so the camera does not pop.
            if (instantZoom || free < currentDistance) { currentDistance = free; distanceVelocity = 0f; }
            else currentDistance = Mathf.SmoothDamp(currentDistance, free, ref distanceVelocity, config.ZoomOutSmoothTime);

            Vector3 dir = full > 0.0001f ? (desired - smoothedPivot) / full : -(rotation * Vector3.forward);
            transform.SetPositionAndRotation(smoothedPivot + dir * currentDistance, rotation);
        }
    }
}
