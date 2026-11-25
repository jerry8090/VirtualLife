using UnityEngine;

namespace VirtualLife.CameraRig
{
    /// <summary>Tunable third-person camera values.</summary>
    [CreateAssetMenu(menuName = "VirtualLife/Camera Config", fileName = "CameraConfig")]
    public sealed class CameraConfig : ScriptableObject
    {
        [Header("Framing")]
        [SerializeField, Min(0.5f)] private float distance = 4f;
        [Tooltip("Pivot height above the CameraTarget (CameraTarget sits at the player's feet).")]
        [SerializeField] private float height = 1.6f;
        [Tooltip("Sideways shoulder offset; 0 = centered behind the player.")]
        [SerializeField] private float shoulderOffset = 0.35f;

        [Header("Look")]
        [Tooltip("Degrees per input pixel.")]
        [SerializeField, Min(0f)] private float horizontalSensitivity = 0.12f;
        [SerializeField, Min(0f)] private float verticalSensitivity = 0.10f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private bool invertY;
        [SerializeField, Range(-30f, 70f)] private float startPitch = 15f;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float followSmoothTime = 0.06f;
        [SerializeField, Min(0f)] private float zoomOutSmoothTime = 0.25f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField, Min(0.05f)] private float collisionRadius = 0.25f;
        [SerializeField, Min(0f)] private float collisionBuffer = 0.1f;
        [SerializeField, Min(0.1f)] private float minDistance = 0.5f;

        public float Distance => distance;
        public float Height => height;
        public float ShoulderOffset => shoulderOffset;
        public float HorizontalSensitivity => horizontalSensitivity;
        public float VerticalSensitivity => verticalSensitivity;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public bool InvertY => invertY;
        public float StartPitch => startPitch;
        public float FollowSmoothTime => followSmoothTime;
        public float ZoomOutSmoothTime => zoomOutSmoothTime;
        public LayerMask CollisionMask => collisionMask;
        public float CollisionRadius => collisionRadius;
        public float CollisionBuffer => collisionBuffer;
        public float MinDistance => minDistance;
    }
}
